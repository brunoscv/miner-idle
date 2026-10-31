using System;
using System.Collections.Generic;
using System.Linq;
using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>
    /// Marco 8, E1: as cavernas e o Letrado no núcleo. Vida × 8 e lascas × 3 por caverna (D-92); o Letrado só com 100
    /// cristais quebrados na caverna, contando toda quebra (D-93, D-95), um por caverna; o nível n leva à caverna n+1 e
    /// dobra o dano dali em diante (GDD v5). Na E1 a passagem é direta (a parede é da E2).
    /// </summary>
    public class CaveTests
    {
        const int Breaks = 100;

        /// <summary>
        /// Dano 100 contra vida 10: um toque (33%) já quebra o cristal da caverna 1. Sem tempo passando (Tick(0)), o anão
        /// não golpeia: só quebra o que o teste manda.
        /// </summary>
        static MiningRules Rules(int caveCount = 10) =>
            new MiningRules(
                initialCrystals: 4, firstCrystalDistance: 3, maxCrystals: 12, spawnInterval: 1e9, minCrystalSpacing: 2, spawnDistance: 12,
                crystalHealth: 10, crystalReward: 1, damage: 100, hitInterval: 1, walkSpeed: 4, miningRange: 1,
                minCrystals: 4, caveCount: caveCount, caveHealthGrowth: 8, caveRewardGrowth: 3, letradoBreaks: Breaks);

        /// <summary>Só o Letrado à venda (GDD v5: 100 × 10^(n−1), 10 níveis, dano × 2).</summary>
        static EconomyRules WithLetrado()
        {
            var off = new UpgradeDefinition(0, 1, 0, 0);
            return new EconomyRules(off, off, off, off, off, 0.1, letrado: new UpgradeDefinition(100, 10, 10, 2));
        }

        static MiningSimulation Sim(EconomyRules economy = null, MiningRules rules = null, ulong seed = 1) =>
            new MiningSimulation(rules ?? Rules(), new SeededRandom(seed), economy: economy ?? WithLetrado(),
                                 strike: new StrikeRules(0, 2, 0.33, 1));

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
        public void Letrado_IsLockedUntil100Breaks_AndATapBreakCounts()
        {
            var sim = Sim();
            sim.AddShards(1e6);

            Assert.That(sim.Purchase(UpgradeId.Letrado), Is.EqualTo(PurchaseResult.Locked));
            var locked = sim.Preview(UpgradeId.Letrado);
            Assert.That(locked.State, Is.EqualTo(UpgradeButtonState.Locked));
            Assert.That(locked.Progress, Is.EqualTo(0));
            Assert.That(locked.ProgressGoal, Is.EqualTo(Breaks));
            Assert.That(locked.NextCost, Is.EqualTo(100), "o preço aparece apagado");

            BreakCrystals(sim, Breaks - 1);
            Assert.That(sim.State.CaveBreaks, Is.EqualTo(Breaks - 1));
            Assert.That(sim.Purchase(UpgradeId.Letrado), Is.EqualTo(PurchaseResult.Locked), "99 não bastam");
            Assert.That(sim.Preview(UpgradeId.Letrado).Progress, Is.EqualTo(Breaks - 1));

            // O 100º pelo toque (D-95: toda quebra conta).
            sim.Tick(0);
            sim.Tap();
            Assert.That(sim.State.CaveBreaks, Is.EqualTo(Breaks), "a quebra do toque conta");
            Assert.That(sim.Preview(UpgradeId.Letrado).State, Is.EqualTo(UpgradeButtonState.Purchasable));

            Assert.That(sim.Purchase(UpgradeId.Letrado), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(sim.State.LevelOf(UpgradeId.Letrado), Is.EqualTo(1));
            Assert.That(sim.State.Shards, Is.EqualTo(1e6 + Breaks - 100).Within(1e-6), "custou 100");
        }

        [Test]
        public void Letrado_IsOnePerCave_AndShowsTheCaveItOpens()
        {
            var sim = Sim();
            sim.AddShards(1e9);
            sim.FillCaveBreaks();

            var before = sim.Preview(UpgradeId.Letrado);
            Assert.That(before.CurrentValue, Is.EqualTo(1), "Caverna 1 »");
            Assert.That(before.NextValue, Is.EqualTo(2), "» 2");

            Assert.That(sim.Purchase(UpgradeId.Letrado), Is.EqualTo(PurchaseResult.Purchased));
            BreakCrystals(sim, Breaks);
            Assert.That(sim.Purchase(UpgradeId.Letrado), Is.EqualTo(PurchaseResult.PassFirst), "o 2º só na caverna 2");
            Assert.That(sim.Preview(UpgradeId.Letrado).State, Is.EqualTo(UpgradeButtonState.PassFirst));
            Assert.That(sim.State.LevelOf(UpgradeId.Letrado), Is.EqualTo(1));
        }

        [Test]
        public void PassCave_NeedsTheLetrado_ThenMovesOn_AndTheCountStartsOver()
        {
            var sim = Sim();
            var caves = new List<int>();
            sim.CaveChanged += caves.Add;
            sim.AddShards(1e6);

            Assert.That(sim.CanPassCave, Is.False, "sem Letrado");
            Assert.That(sim.PassCave(), Is.False);

            BreakCrystals(sim, Breaks);
            sim.Purchase(UpgradeId.Letrado);
            Assert.That(sim.CanPassCave, Is.True);

            Assert.That(sim.PassCave(), Is.True);
            Assert.That(sim.State.Cave, Is.EqualTo(2));
            Assert.That(sim.State.CaveBreaks, Is.EqualTo(0), "a trava recomeça na caverna nova");
            Assert.That(caves, Is.EqualTo(new[] { 2 }));
            Assert.That(sim.CanPassCave, Is.False, "falta o Letrado 2");
            Assert.That(sim.Purchase(UpgradeId.Letrado), Is.EqualTo(PurchaseResult.Locked));
        }

        [Test]
        public void NewCave_SpawnsTougherCrystals_AndTheOldOnesStayAsTheyWere()
        {
            var sim = Sim();
            sim.SkipToNextCave();

            Assert.That(sim.Crystals.All(c => c.MaxHealth == 10), Is.True, "os da fila são da caverna 1");
            var crystal = sim.SpawnCrystalNow();
            Assert.That(crystal.MaxHealth, Is.EqualTo(80));
            Assert.That(crystal.Reward, Is.EqualTo(3));

            // A quebra paga as lascas da caverna do cristal.
            sim.Tick(0);
            double shards = sim.State.Shards;
            sim.BreakTarget();
            Assert.That(sim.State.Shards - shards, Is.EqualTo(1), "o da frente ainda é da caverna 1");
        }

        [Test]
        public void Letrado_DoublesTheDamage_FromCave2On()
        {
            var sim = Sim();
            double damage = sim.Stats.Damage;
            var stats = new List<double>();
            sim.StatsChanged += s => stats.Add(s.Damage);

            sim.SkipToNextCave();
            Assert.That(sim.Stats.Damage, Is.EqualTo(damage * 2));
            Assert.That(stats, Is.EqualTo(new[] { damage * 2 }), "o HUD fica sabendo");

            sim.SkipToNextCave();
            Assert.That(sim.Stats.Damage, Is.EqualTo(damage * 2), "não acumula: só o da caverna");
        }

        [Test]
        public void LastCave_HasNoPassage()
        {
            var sim = Sim();
            for (int cave = 1; cave < 10; cave++) Assert.That(sim.SkipToNextCave(), Is.True, $"caverna {cave}");

            Assert.That(sim.State.Cave, Is.EqualTo(10));
            Assert.That(sim.State.LevelOf(UpgradeId.Letrado), Is.EqualTo(9), "o atalho dá o Letrado de cada caverna");
            Assert.That(sim.SkipToNextCave(), Is.False);

            sim.AddShards(1e12);
            sim.FillCaveBreaks();
            Assert.That(sim.Purchase(UpgradeId.Letrado), Is.EqualTo(PurchaseResult.Purchased), "o Letrado 10 existe");
            Assert.That(sim.CanPassCave, Is.False, "o fim da descida é da E3");
            Assert.That(sim.State.Cave, Is.EqualTo(10));
        }

        [Test]
        public void FillCaveBreaks_UnlocksTheLetrado_AndNotifies()
        {
            var sim = Sim();
            int notified = 0;
            sim.CaveChanged += _ => notified++;

            sim.FillCaveBreaks();
            Assert.That(sim.State.CaveBreaks, Is.EqualTo(Breaks));
            Assert.That(notified, Is.EqualTo(1));

            sim.FillCaveBreaks();
            Assert.That(notified, Is.EqualTo(1), "já cheio: nada muda");
        }

        [Test]
        public void WithoutTheLetrado_TheGameStaysInCave1()
        {
            var off = new UpgradeDefinition(0, 1, 0, 0);
            var sim = Sim(new EconomyRules(new UpgradeDefinition(10, 4, 19, 2), off, off, off, off, 0.5), Rules(caveCount: 1));
            sim.AddShards(1e6);
            sim.FillCaveBreaks();

            Assert.That(sim.Purchase(UpgradeId.Letrado), Is.EqualTo(PurchaseResult.MaxLevel));
            Assert.That(sim.CanPassCave, Is.False);
            Assert.That(sim.SkipToNextCave(), Is.False);
            Assert.That(sim.State.Cave, Is.EqualTo(1));
        }

        [Test]
        public void SameSeed_SamePassage_SameGame()
        {
            MiningSimulation Play()
            {
                var sim = Sim(seed: 7);
                sim.AddShards(1e6);
                sim.Tick(30);
                sim.FillCaveBreaks();
                sim.Purchase(UpgradeId.Letrado);
                sim.PassCave();
                sim.Tick(120);
                return sim;
            }

            var a = Play();
            var b = Play();
            Assert.That(b.State.Cave, Is.EqualTo(a.State.Cave));
            Assert.That(b.State.Shards, Is.EqualTo(a.State.Shards));
            Assert.That(b.State.CaveBreaks, Is.EqualTo(a.State.CaveBreaks).And.GreaterThan(0));
            Assert.That(b.Crystals.Select(c => c.MaxHealth), Is.EqualTo(a.Crystals.Select(c => c.MaxHealth)));
        }
    }
}
