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
    /// dobra o dano dali em diante (GDD v5). E2: a passagem põe a parede a 12 un (D-107), com 2 × a vida do cristal da
    /// caverna nova (D-98); ela apanha de golpe, toque e crítico, não paga lascas nem conta (D-108), e a caverna muda
    /// quando ela cai.
    /// </summary>
    public class CaveTests
    {
        const int Breaks = 100;

        /// <summary>
        /// Dano 100 contra vida 10: um toque (33%) já quebra o cristal da caverna 1. Sem tempo passando (Tick(0)), o anão
        /// não golpeia: só quebra o que o teste manda.
        /// </summary>
        static MiningRules Rules(int caveCount = 10, double spawnInterval = 1e9) =>
            new MiningRules(
                initialCrystals: 4, firstCrystalDistance: 3, maxCrystals: 12, spawnInterval: spawnInterval, minCrystalSpacing: 2, spawnDistance: 12,
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

        /// <summary>Os 100 cristais, o Letrado comprado e a parede posta.</summary>
        static MiningSimulation WithTheWall(MiningRules rules = null)
        {
            var sim = Sim(rules: rules);
            sim.AddShards(1e6);
            sim.FillCaveBreaks();
            Assert.That(sim.Purchase(UpgradeId.Letrado), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(sim.PassCave(), Is.True);
            return sim;
        }

        /// <summary>Quebra pelo debug os cristais antes da parede, até ela ser o alvo do anão.</summary>
        static void ReachTheWall(MiningSimulation sim)
        {
            for (int i = 0; i < 20; i++)
            {
                sim.Tick(0);
                if (sim.Miner.Target == sim.Wall) return;
                sim.BreakTarget();
            }
            Assert.Fail("a parede não virou o alvo");
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
        public void PassCave_NeedsTheLetrado_ThenMovesOnWhenTheWallFalls_AndTheCountStartsOver()
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
            Assert.That(sim.State.Cave, Is.EqualTo(1), "a caverna só muda quando a parede cai");
            Assert.That(sim.Wall, Is.Not.Null);
            Assert.That(sim.CanPassCave, Is.False, "uma parede por vez");
            Assert.That(sim.PassCave(), Is.False);
            Assert.That(caves, Is.Empty);

            ReachTheWall(sim);
            sim.BreakTarget();
            Assert.That(sim.Wall, Is.Null);
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
        public void TheWall_Stands12Ahead_AndTheCrystalsBeyondItAreGone()
        {
            var sim = Sim();
            sim.AddShards(1e6);
            sim.FillCaveBreaks();
            sim.Purchase(UpgradeId.Letrado);
            sim.Tick(0); // o alvo: o cristal a 3 un
            foreach (double at in new[] { 11.0, 15, 20 }) sim.SpawnCrystalAt(at);
            var spawned = new List<Crystal>();
            sim.CrystalSpawned += spawned.Add;

            Assert.That(sim.PassCave(), Is.True);

            var wall = sim.Wall;
            Assert.That(wall.IsWall, Is.True);
            Assert.That(wall.Distance, Is.EqualTo(sim.Miner.Distance + 12), "onde os cristais nascem (D-107)");
            Assert.That(wall.MaxHealth, Is.EqualTo(2 * 80), "2 × o cristal da caverna 2 (D-98)");
            Assert.That(wall.Reward, Is.EqualTo(0));
            Assert.That(spawned, Is.EqualTo(new[] { wall }), "quem desenha fica sabendo");
            Assert.That(sim.Crystals.Last(), Is.SameAs(wall), "a parede é a última da fila");
            Assert.That(sim.Crystals.Where(c => !c.IsWall).Select(c => c.Distance), Is.EqualTo(new[] { 3.0, 5, 7, 9 }),
                        "os de depois somem (o de 11 ficaria a menos do espaço mínimo dela)");
        }

        [Test]
        public void TheWall_StaysBehindTheTarget()
        {
            var sim = Sim();
            sim.AddShards(1e6);
            sim.FillCaveBreaks();
            sim.Purchase(UpgradeId.Letrado);
            var far = sim.SpawnCrystalAt(30);
            BreakCrystals(sim, 4); // os quatro do começo; a fila se repõe depois do de 30
            sim.Tick(0);
            Assume.That(sim.Miner.Target, Is.SameAs(far), "o alvo está além dos 12 un");

            sim.PassCave();
            Assert.That(sim.Crystals, Does.Contain(far), "o alvo do anão não some");
            Assert.That(sim.Wall.Distance, Is.EqualTo(far.Distance + 2), "a parede fica logo depois dele");
        }

        [Test]
        public void NothingIsBorn_WhileTheWallStands()
        {
            var sim = WithTheWall(Rules(spawnInterval: 1));
            var events = new List<string>();
            sim.CrystalSpawned += c => events.Add("nasce");
            sim.CrystalBroken += c => events.Add(c.IsWall ? "parede" : "quebra");

            Assert.That(sim.SpawnCrystalNow(), Is.Null, "nem pelo debug");
            sim.Tick(60); // golpes de 100: os cristais e a parede (160) caem bem antes

            int wall = events.IndexOf("parede");
            Assert.That(wall, Is.GreaterThan(0), "a parede caiu");
            Assert.That(events.Take(wall), Has.No.Member("nasce"), "nada nasce com ela de pé");
            Assert.That(events.Skip(wall), Has.Member("nasce"), "depois, a fila volta");
            Assert.That(sim.State.Cave, Is.EqualTo(2));
        }

        [Test]
        public void TapAndCritical_HitTheWall()
        {
            var sim = WithTheWall();
            ReachTheWall(sim);
            var hits = new List<MiningHit>();
            sim.HitLanded += hits.Add;

            sim.ForceCritical = true;
            sim.Tap();

            Assert.That(hits.Count, Is.EqualTo(1));
            Assert.That(hits[0].Crystal.IsWall, Is.True);
            Assert.That(hits[0].IsTap && hits[0].IsCritical, Is.True);
            Assert.That(sim.Wall.Health, Is.EqualTo(160 - 100 * 0.33 * 2).Within(1e-9), "toque (33%) × crítico (×2)");
        }

        [Test]
        public void TheWall_PaysNoShards_AndIsNotABreak()
        {
            var sim = WithTheWall();
            ReachTheWall(sim);
            double shards = sim.State.Shards;
            long breaks = sim.State.Breaks;
            int shardEvents = 0;
            sim.ShardsChanged += _ => shardEvents++;

            sim.BreakTarget();

            Assert.That(sim.State.Shards, Is.EqualTo(shards), "sem lascas (D-108)");
            Assert.That(shardEvents, Is.EqualTo(0));
            Assert.That(sim.State.Breaks, Is.EqualTo(breaks), "não conta como cristal");
            Assert.That(sim.State.CaveBreaks, Is.EqualTo(0), "a caverna nova começa do zero");
        }

        [Test]
        public void BreakingTheWall_EntersTheNextCave_WithItsCrystals()
        {
            var sim = WithTheWall();
            var caves = new List<int>();
            sim.CaveChanged += caves.Add;
            ReachTheWall(sim);
            double damage = sim.Stats.Damage;

            sim.Tap(); // com toques: a regra é a mesma do golpe
            sim.Tap();
            sim.Tap();
            sim.Tap();
            sim.Tap();
            Assert.That(sim.Wall, Is.Null, "5 toques de 33 tiram os 160");

            Assert.That(caves, Is.EqualTo(new[] { 2 }));
            Assert.That(sim.State.Cave, Is.EqualTo(2));
            Assert.That(sim.Stats.Damage, Is.EqualTo(damage * 2), "o dano dobra na caverna 2");
            sim.Tick(0);
            Assert.That(sim.Crystals.Count, Is.EqualTo(4), "a fila volta no mínimo");
            Assert.That(sim.Crystals.All(c => c.MaxHealth == 80 && c.Distance >= sim.Miner.Distance + 12), Is.True,
                        "cristais da caverna 2, nascendo fora da tela");
        }

        [Test]
        public void NextCaveShortcut_TakesTheWallAway()
        {
            var sim = WithTheWall();
            ReachTheWall(sim);

            Assert.That(sim.SkipToNextCave(), Is.True);
            Assert.That(sim.Wall, Is.Null);
            Assert.That(sim.Crystals.Any(c => c.IsWall), Is.False);
            Assert.That(sim.Miner.Target, Is.Null);
            Assert.That(sim.State.Cave, Is.EqualTo(2));
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
            Assert.That(a.State.Cave, Is.EqualTo(2), "a parede caiu nos 120 s");
            Assert.That(b.State.Cave, Is.EqualTo(a.State.Cave));
            Assert.That(b.State.Shards, Is.EqualTo(a.State.Shards));
            Assert.That(b.State.CaveBreaks, Is.EqualTo(a.State.CaveBreaks).And.GreaterThan(0));
            Assert.That(b.Crystals.Select(c => c.MaxHealth), Is.EqualTo(a.Crystals.Select(c => c.MaxHealth)));
        }
    }
}
