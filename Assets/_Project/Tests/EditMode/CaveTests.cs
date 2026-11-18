using System;
using System.Collections.Generic;
using System.Linq;
using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>
    /// Os andares no núcleo. Marco 8: vida e lascas crescem por andar (D-92); toda quebra conta para a passagem (D-95);
    /// tempo de jogo e lascas ganhas para a tela do fim; no último andar, o coração da montanha (D-112). M9c E1 (GDD v8): o
    /// Mapa (o Letrado) e a parede saíram (D-256); a passagem é o Próximo Andar, com a barra de <c>andar × 10</c> cristais
    /// (D-239): cheia, fica cheia (D-259), e o Próximo Andar entra no andar seguinte, zera a barra e refaz a fila com os
    /// cristais de lá; no último andar leva ao coração (D-258). Os testes do Mapa e da parede viraram estes.
    /// </summary>
    public class CaveTests
    {
        /// <summary>Cristais por andar na barra (GDD v8: 10; o andar n pede n × 10).</summary>
        const int Breaks = 10;

        /// <summary>
        /// Dano 100 contra vida 10: um golpe já quebra o cristal do andar 1. Sem tempo passando (Tick(0)), o anão
        /// não golpeia: só quebra o que o teste manda.
        /// </summary>
        static MiningRules Rules(int caveCount = 10, double spawnInterval = 1e9) =>
            new MiningRules(
                initialCrystals: 4, firstCrystalDistance: 3, maxCrystals: 12, spawnInterval: spawnInterval, minCrystalSpacing: 2, spawnDistance: 12,
                crystalHealth: 10, crystalReward: 1, damage: 100, hitInterval: 1, walkSpeed: 4, miningRange: 1,
                minCrystals: 4, caveCount: caveCount, caveHealthGrowth: 8, caveRewardGrowth: 3, floorBreaks: Breaks);

        static MiningSimulation Sim(MiningRules rules = null, ulong seed = 1) =>
            new MiningSimulation(rules ?? Rules(), new SeededRandom(seed), strike: new StrikeRules(0, 2));

        /// <summary>Quebra <paramref name="count"/> cristais pelo atalho do debug (o mesmo caminho de uma quebra normal).</summary>
        static void BreakCrystals(MiningSimulation sim, int count)
        {
            for (int i = 0; i < count; i++)
            {
                sim.Tick(0); // escolhe o alvo (e repõe a fila no mínimo)
                Assert.That(sim.BreakTarget(), Is.True, $"quebra {i + 1}");
            }
        }

        [Test]
        public void CrystalValues_GrowPerCave()
        {
            var rules = Rules();
            Assert.That(rules.CrystalHealthIn(1), Is.EqualTo(10));
            Assert.That(rules.CrystalHealthIn(2), Is.EqualTo(80), "10 × 8");
            Assert.That(rules.CrystalHealthIn(10), Is.EqualTo(10 * Math.Pow(8, 9)));
            Assert.That(rules.CrystalRewardIn(1), Is.EqualTo(1));
            Assert.That(rules.CrystalRewardIn(2), Is.EqualTo(3), "1 × 3");
            Assert.That(rules.CrystalRewardIn(10), Is.EqualTo(Math.Pow(3, 9)));
        }

        [Test]
        public void TheBar_FillsWithEveryBreak_AndATapBreakCounts()
        {
            var sim = Sim();
            Assert.That(sim.RequiredBreaks, Is.EqualTo(Breaks), "andar 1: 1 × 10");
            Assert.That(sim.FloorProgress, Is.EqualTo(0));
            Assert.That(sim.CanAdvance, Is.False);
            Assert.That(sim.AdvanceFloor(), Is.False, "sem a barra cheia");

            BreakCrystals(sim, Breaks - 1);
            Assert.That(sim.State.CaveBreaks, Is.EqualTo(Breaks - 1));
            Assert.That(sim.FloorProgress, Is.EqualTo(0.9).Within(1e-9));
            Assert.That(sim.CanAdvance, Is.False, "9 não bastam");

            BreakCrystals(sim, 1);
            Assert.That(sim.State.CaveBreaks, Is.EqualTo(Breaks), "o 10º enche a barra");
            Assert.That(sim.FloorProgress, Is.EqualTo(1));
            Assert.That(sim.CanAdvance, Is.True);
        }

        [Test]
        public void TheBar_StaysFull_AndTheExtraBreaksDoNotCarryOver()
        {
            var sim = Sim();
            BreakCrystals(sim, Breaks + 5);
            Assert.That(sim.FloorProgress, Is.EqualTo(1), "cheia fica em 100% (D-259)");
            Assert.That(sim.CanAdvance, Is.True);

            Assert.That(sim.AdvanceFloor(), Is.True);
            Assert.That(sim.State.CaveBreaks, Is.EqualTo(0), "o andar 2 começa do zero");
            Assert.That(sim.FloorProgress, Is.EqualTo(0));
        }

        [Test]
        public void TheRequirement_GrowsWithTheFloor()
        {
            var sim = Sim();
            foreach (int floor in new[] { 2, 5, 10 })
            {
                sim.SkipToCave(floor);
                Assert.That(sim.RequiredBreaks, Is.EqualTo(floor * Breaks), $"andar {floor}: {floor} × 10 (D-239)");
            }
        }

        [Test]
        public void AdvanceFloor_EntersTheNextFloor_AndTheCountStartsOver()
        {
            var sim = Sim();
            var floors = new List<int>();
            sim.CaveChanged += floors.Add;

            BreakCrystals(sim, Breaks);
            Assert.That(floors, Is.Empty, "a quebra avisa pelo CrystalBroken");

            Assert.That(sim.AdvanceFloor(), Is.True);
            Assert.That(sim.State.Cave, Is.EqualTo(2));
            Assert.That(sim.State.CaveBreaks, Is.EqualTo(0), "a barra recomeça no andar novo");
            Assert.That(floors, Is.EqualTo(new[] { 2 }));
            Assert.That(sim.RequiredBreaks, Is.EqualTo(2 * Breaks));
            Assert.That(sim.CanAdvance, Is.False, "a barra do andar 2 está vazia");
            Assert.That(sim.AdvanceFloor(), Is.False);
        }

        [Test]
        public void AdvanceFloor_RefreshesTheQueue_WithTheNewFloorsCrystals()
        {
            var sim = Sim();
            sim.FillFloorBar();
            sim.Tick(0); // o alvo: o primeiro da fila do andar 1
            Assume.That(sim.Miner.Target, Is.Not.Null);
            var old = sim.Crystals.ToList();
            var spawned = new List<Crystal>();
            var targets = new List<Crystal>();
            sim.CrystalSpawned += spawned.Add;
            sim.TargetChanged += targets.Add;

            Assert.That(sim.AdvanceFloor(), Is.True);

            Assert.That(sim.Crystals.Intersect(old), Is.Empty, "a fila do andar 1 sai");
            Assert.That(sim.Crystals.Count, Is.EqualTo(4), "a do andar 2 nasce como a do começo");
            Assert.That(sim.Crystals.All(c => c.MaxHealth == 80 && c.Reward == 3), Is.True, "vida e lascas do andar 2");
            Assert.That(sim.Crystals.Select(c => c.Distance - sim.Miner.Distance), Is.EqualTo(new[] { 3.0, 5, 7, 9 }),
                        "na tela, à frente do anão (D-60)");
            Assert.That(spawned, Is.EqualTo(sim.Crystals), "quem desenha fica sabendo, cristal por cristal");
            Assert.That(targets, Is.EqualTo(new Crystal[] { null }), "o alvo velho sai");
            Assert.That(sim.Miner.Target, Is.Null);
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Running), "o anão corre até o primeiro do andar novo");
        }

        [Test]
        public void AdvanceFloor_PaysNothing_AndIsNotABreak()
        {
            var sim = Sim();
            sim.FillFloorBar();
            double shards = sim.State.Shards;
            long breaks = sim.State.Breaks;

            sim.AdvanceFloor();

            Assert.That(sim.State.Shards, Is.EqualTo(shards), "passar não paga");
            Assert.That(sim.State.Breaks, Is.EqualTo(breaks), "nem conta como cristal");
        }

        [Test]
        public void TheFloor_DoesNotChangeTheDamage()
        {
            var sim = Sim();
            double damage = sim.Stats.Damage;
            sim.SkipToNextCave();
            Assert.That(sim.Stats.Damage, Is.EqualTo(damage), "sem o ×2 do Letrado (saiu no v8, D-256)");
            sim.SkipToCave(10);
            Assert.That(sim.Stats.Damage, Is.EqualTo(damage));
        }

        [Test]
        public void LastFloor_TheFullBarLeadsToTheHeart()
        {
            var sim = Sim();
            for (int floor = 1; floor < 10; floor++) Assert.That(sim.SkipToNextCave(), Is.True, $"andar {floor}");

            Assert.That(sim.State.Cave, Is.EqualTo(10));
            Assert.That(sim.SkipToNextCave(), Is.False, "não há andar 11");
            Assert.That(sim.CanAdvance, Is.False, "a barra do 10 está vazia");

            int hearts = 0;
            var floors = new List<int>();
            sim.HeartReached += () => hearts++;
            sim.CaveChanged += floors.Add;
            sim.FillFloorBar();
            floors.Clear();
            Assert.That(sim.CanAdvance, Is.True, "no último andar o Próximo Andar leva ao coração (D-258)");

            Assert.That(sim.AdvanceFloor(), Is.True);
            Assert.That(hearts, Is.EqualTo(1));
            Assert.That(sim.State.ReachedHeart, Is.True);
            Assert.That(sim.State.Cave, Is.EqualTo(10), "o jogo segue no andar 10, sem reset");
            Assert.That(floors, Is.Empty, "não muda de andar");
            Assert.That(sim.CanAdvance, Is.False, "depois do coração, não há passagem");
            Assert.That(sim.AdvanceFloor(), Is.False);

            sim.Tick(0);
            Assert.That(sim.Crystals.Count, Is.GreaterThanOrEqualTo(4), "a fila segue");
            Assert.That(sim.Crystals.All(c => c.MaxHealth == 10 * Math.Pow(8, 9)), Is.True, "cristais do andar 10");
            Assert.That(hearts, Is.EqualTo(1), "o fim sai uma vez só");
        }

        [Test]
        public void SkipToCave_GoesStraightThere()
        {
            var sim = Sim();
            var floors = new List<int>();
            sim.CaveChanged += floors.Add;

            Assert.That(sim.SkipToCave(10), Is.True);

            Assert.That(floors, Is.EqualTo(new[] { 10 }), "um aviso só, direto no 10");
            Assert.That(sim.State.Cave, Is.EqualTo(10));
            Assert.That(sim.State.CaveBreaks, Is.EqualTo(0));
            Assert.That(sim.Crystals.All(c => c.MaxHealth == 10 * Math.Pow(8, 9)), Is.True, "a fila do andar 10");
            Assert.That(sim.SkipToCave(5), Is.False, "não volta");
            Assert.That(sim.SkipToCave(15), Is.False, "não passa do último");
        }

        [Test]
        public void GoToFloor_GoesUpOrDown_AndLeavesTheHeartBehind()
        {
            var sim = Sim();
            Assert.That(sim.GoToFloor(10), Is.True);
            sim.FillFloorBar();
            sim.AdvanceFloor();
            Assume.That(sim.State.ReachedHeart, Is.True);

            Assert.That(sim.GoToFloor(10), Is.False, "já está nele");
            Assert.That(sim.GoToFloor(5), Is.True, "o atalho do DBG sobe também");
            Assert.That(sim.State.Cave, Is.EqualTo(5));
            Assert.That(sim.State.ReachedHeart, Is.False, "o coração fica para trás");
            Assert.That(sim.Crystals.All(c => c.MaxHealth == 10 * Math.Pow(8, 4)), Is.True, "a fila do andar 5");
            Assert.That(sim.GoToFloor(0), Is.True, "abaixo de 1 vale o 1");
            Assert.That(sim.State.Cave, Is.EqualTo(1));
            Assert.That(sim.Permanents.State.DeepestFloor, Is.EqualTo(10), "o recorde não desce");
        }

        [Test]
        public void PlayTime_AndShardsEarned_CountForTheEnd()
        {
            var sim = Sim(Rules(spawnInterval: 1));
            sim.Tick(30);
            Assert.That(sim.State.PlaySeconds, Is.EqualTo(30).Within(1e-9), "o tempo de jogo é o da simulação");
            Assume.That(sim.State.Breaks, Is.GreaterThan(0));
            Assert.That(sim.State.ShardsEarned, Is.EqualTo(sim.State.Shards).Within(1e-9), "as lascas das quebras");

            double earned = sim.State.ShardsEarned;
            sim.AddShards(500);
            Assert.That(sim.State.ShardsEarned, Is.EqualTo(earned), "as do debug não contam");
            sim.FillFloorBar();
            sim.AdvanceFloor();
            Assert.That(sim.State.ShardsEarned, Is.EqualTo(earned), "passar de andar não conta");
        }

        [Test]
        public void FillFloorBar_EnablesTheAdvance_AndNotifies()
        {
            var sim = Sim();
            int notified = 0;
            sim.CaveChanged += _ => notified++;

            sim.FillFloorBar();
            Assert.That(sim.State.CaveBreaks, Is.EqualTo(Breaks));
            Assert.That(sim.CanAdvance, Is.True);
            Assert.That(notified, Is.EqualTo(1));

            sim.FillFloorBar();
            Assert.That(notified, Is.EqualTo(1), "já cheia: nada muda");
        }

        [Test]
        public void WithOneFloor_TheFullBarLeadsStraightToTheHeart()
        {
            var sim = Sim(Rules(caveCount: 1));
            sim.FillFloorBar();

            Assert.That(sim.SkipToNextCave(), Is.False);
            Assert.That(sim.AdvanceFloor(), Is.True);
            Assert.That(sim.State.ReachedHeart, Is.True);
            Assert.That(sim.State.Cave, Is.EqualTo(1));
        }

        [Test]
        public void TheNewFloor_KeepsSpawning_LikeAnyOther()
        {
            var sim = Sim(Rules(spawnInterval: 1));
            sim.FillFloorBar();
            sim.AdvanceFloor();
            var events = new List<string>();
            sim.CrystalSpawned += c => events.Add("nasce");
            sim.CrystalBroken += c => events.Add("quebra");

            Assert.That(sim.SpawnCrystalNow(), Is.Not.Null, "o atalho do debug também");
            sim.Tick(60); // golpes de 100 contra cristais de 80: o anão quebra e a fila se repõe

            Assert.That(events, Has.Member("quebra"));
            Assert.That(events.Count(e => e == "nasce"), Is.GreaterThan(1), "a fila se repõe no andar novo");
            Assert.That(sim.State.Cave, Is.EqualTo(2), "sem o Próximo Andar, o andar não muda sozinho");
            Assert.That(sim.State.CaveBreaks, Is.GreaterThan(0), "as quebras do andar 2 enchem a barra dele");
        }

        [Test]
        public void NextFloorShortcut_DoesNotNeedTheBar()
        {
            var sim = Sim();
            sim.Tick(0);

            Assert.That(sim.SkipToNextCave(), Is.True);
            Assert.That(sim.Miner.Target, Is.Null);
            Assert.That(sim.State.Cave, Is.EqualTo(2));
            Assert.That(sim.State.CaveBreaks, Is.EqualTo(0));
        }

        [Test]
        public void SameSeed_SamePassage_SameGame()
        {
            MiningSimulation Play()
            {
                var sim = Sim(seed: 7);
                sim.Tick(30);
                sim.FillFloorBar();
                sim.AdvanceFloor();
                sim.Tick(120);
                return sim;
            }

            var a = Play();
            var b = Play();
            Assert.That(a.State.Cave, Is.EqualTo(2));
            Assert.That(b.State.Cave, Is.EqualTo(a.State.Cave));
            Assert.That(b.State.Shards, Is.EqualTo(a.State.Shards));
            Assert.That(b.State.CaveBreaks, Is.EqualTo(a.State.CaveBreaks).And.GreaterThan(0));
            Assert.That(b.Crystals.Select(c => c.MaxHealth), Is.EqualTo(a.Crystals.Select(c => c.MaxHealth)));
        }
    }
}
