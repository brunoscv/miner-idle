using System;
using System.Collections.Generic;
using System.Linq;
using MinersIdle.Core;
using MinersIdle.Game;
using NUnit.Framework;
using UnityEditor;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9c E1: os números do GDD v8 no núcleo e o <c>Balance_GDD_v8</c> que o setup cria. Vida <c>1 × 1,5^(n−1)</c> (D-237);
    /// lascas = vida × 0,2 × max(1, andar − 1) (D-238); o Próximo Andar pede <c>andar × 10</c> cristais menos a Memória
    /// (−5 por nível, até 100; nunca abaixo de 0; D-239); crítico ×1,5, e acima de 100% de chance +1 ponto no multiplicador
    /// por 1% (D-249); Picareta a 7 × o dano (P5); offline 4 h, e 6 h depois de alcançar o andar 20 (D-253). Os números
    /// estão escritos à mão, como no GDD; o teste do asset confere que ele traz esses mesmos números.
    /// </summary>
    public class GddV8RulesTests
    {
        const string GddV8Path = "Assets/_Project/Data/Balance_GDD_v8.asset";

        static MiningRules V8Rules() =>
            new MiningRules(
                initialCrystals: 4, firstCrystalDistance: 3, maxCrystals: 12, spawnInterval: 1e9, minCrystalSpacing: 2,
                spawnDistance: 12, crystalHealth: 1, crystalReward: 0.2, damage: 1, hitInterval: 2, walkSpeed: 4,
                miningRange: 1, minCrystals: 4, caveCount: 100, caveHealthGrowth: 1.5, floorBreaks: 10,
                pickaxeFormula: PickaxeFormula.LevelScaled, pickaxeCaveGrowth: 1.2, levelsPerBiome: 10,
                offlineMaxSeconds: 4 * 3600, rewardFormula: RewardFormula.FloorScaled, offlineDeepMaxSeconds: 6 * 3600,
                offlineDeepFloor: 20);

        static UpgradeDefinition Line(int max, double effect) => new UpgradeDefinition(10, 2, max, effect, CostFormula.OffsetExponential);

        /// <summary>As permanentes do anão no v8: a Memória −5 por nível até 100 (D-239); as outras como no v7.</summary>
        static PermanentRules V8Permanents() => new PermanentRules(
            10, new RedShardTable(10, 10, 5, 50, 10, 1),
            Line(100, 5), Line(20, 0.1), Line(20, 0.5), Line(10, 0.05), Line(10, 1));

        static PermanentState WithLevel(PermanentId id, int level) =>
            PermanentState.Restore(0, 1, p => p == id ? level : 0, V8Permanents());

        static MiningSimulation Sim(PermanentState permanent = null, MiningRules rules = null, StrikeRules strike = null) =>
            new MiningSimulation(rules ?? V8Rules(), new SeededRandom(3), strike: strike ?? new StrikeRules(0, 1.5, 0.33, 1),
                                 permanents: new Permanents(V8Permanents(), permanent ?? new PermanentState()));

        static MiningRulesAsset Asset()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV8Path);
            Assert.That(asset, Is.Not.Null, "o setup cria o Balance_GDD_v8");
            return asset;
        }

        // ── Vida e lascas por andar (D-237, D-238) ──────────────────────────────────────────────────────────────

        [TestCase(1, 1.0, 0.2)]
        [TestCase(2, 1.5, 0.3)]
        [TestCase(3, 2.25, 0.9)]
        [TestCase(10, 38.443359375, 69.198046875)]
        public void HealthAndShards_FollowTheFloor(int floor, double health, double shards)
        {
            var rules = V8Rules();
            Assert.That(rules.CrystalHealthIn(floor), Is.EqualTo(health).Within(1e-9), "1 × 1,5^(n−1)");
            Assert.That(rules.CrystalRewardIn(floor), Is.EqualTo(shards).Within(1e-9), "vida × 0,2 × max(1, n−1)");
        }

        [Test]
        public void Floor100_HealthAndShards()
        {
            var rules = V8Rules();
            double health = Math.Pow(1.5, 99);
            Assert.That(rules.CrystalHealthIn(100), Is.EqualTo(health).Within(1e-9).Percent);
            Assert.That(rules.CrystalRewardIn(100), Is.EqualTo(health * 0.2 * 99).Within(1e-9).Percent);
        }

        [Test]
        public void TheQueue_OfANewFloor_HasItsHealthAndShards()
        {
            var sim = Sim();
            sim.SkipToCave(10);
            Assert.That(sim.Crystals.All(c => Math.Abs(c.MaxHealth - Math.Pow(1.5, 9)) < 1e-9), Is.True);
            Assert.That(sim.Crystals.All(c => Math.Abs(c.Reward - Math.Pow(1.5, 9) * 0.2 * 9) < 1e-9), Is.True);
        }

        // ── O requisito do Próximo Andar e a Memória (D-239) ─────────────────────────────────────────────────────

        [TestCase(0, 1, 10)]
        [TestCase(0, 2, 20)]
        [TestCase(0, 100, 1000)]
        [TestCase(2, 1, 0)]
        [TestCase(2, 5, 40)]
        [TestCase(20, 10, 0)]
        [TestCase(20, 30, 200)]
        [TestCase(20, 1, 0)]
        public void Requirement_IsFloorTimesTen_MinusTheMemory_NeverBelowZero(int memory, int floor, long required)
        {
            var sim = Sim(WithLevel(PermanentId.Memory, memory));
            if (floor > 1) sim.SkipToCave(floor);
            Assert.That(sim.RequiredBreaks, Is.EqualTo(required),
                        $"andar {floor} × 10 − Memória {memory} × 5 (o negativo vira 0)");
            Assert.That(sim.CanAdvance, Is.EqualTo(required == 0), "requisito 0: a barra já nasce cheia");
        }

        [Test]
        public void Memory_CostsTenPlusTwoToTheLevel_UpToLevel100()
        {
            var memory = V8Permanents().Get(PermanentId.Memory);
            Assert.That(memory.MaxLevel, Is.EqualTo(100));
            Assert.That(memory.Effect, Is.EqualTo(5));
            Assert.That(memory.CostAt(0), Is.EqualTo(10 + 1), "10 + 2^0");
            Assert.That(memory.CostAt(4), Is.EqualTo(10 + 16), "10 + 2^4");
        }

        // ── Crítico acima de 100% (D-249) ───────────────────────────────────────────────────────────────────────

        /// <summary>O primeiro golpe automático num cristal de vida enorme: o dano e se foi crítico.</summary>
        static MiningHit FirstHit(MiningSimulation sim)
        {
            var hits = new List<MiningHit>();
            sim.HitLanded += hits.Add;
            sim.SpawnCrystalAt(sim.Miner.Distance + 1);
            for (int i = 0; i < 40 && hits.Count == 0; i++) sim.Tick(0.5);
            Assert.That(hits, Is.Not.Empty, "o anão bateu");
            return hits[0];
        }

        static MiningRules Tough() =>
            new MiningRules(
                initialCrystals: 0, firstCrystalDistance: 3, maxCrystals: 12, spawnInterval: 1e9, minCrystalSpacing: 2,
                spawnDistance: 12, crystalHealth: 1e12, crystalReward: 0.2, damage: 10, hitInterval: 1, walkSpeed: 4,
                miningRange: 1, caveCount: 100);

        [TestCase(0.8, 0, 1.5, TestName = "Critical_At80Percent_IsTimesOnePointFive")]
        [TestCase(0.5, 10, 1.5, TestName = "Critical_At100Percent_IsTimesOnePointFive_AndAlways")]
        [TestCase(0.9, 10, 1.9, TestName = "Critical_At140Percent_IsTimesOnePointNine")]
        public void Critical_AboveOneHundredPercent_AddsToTheMultiplier(double baseChance, int nervousFingers, double multiplier)
        {
            var sim = Sim(WithLevel(PermanentId.NervousFingers, nervousFingers), Tough(), new StrikeRules(baseChance, 1.5, 0.33, 1));
            double chance = baseChance + 0.05 * nervousFingers;
            if (chance < 1) sim.ForceCritical = true; // abaixo de 100% o sorteio decide; aqui só o multiplicador importa

            var hit = FirstHit(sim);

            Assert.That(hit.IsCritical, Is.True, chance >= 1 ? "100% ou mais: sempre, sem sortear" : "forçado");
            Assert.That(hit.Damage, Is.EqualTo(10 * multiplier).Within(1e-9), $"chance {chance:P0}: ×{multiplier}");
        }

        [Test]
        public void Critical_AtOrAboveOneHundredPercent_NeverMisses()
        {
            var sim = Sim(WithLevel(PermanentId.NervousFingers, 10), Tough(), new StrikeRules(0.5, 1.5, 0.33, 1));
            var hits = new List<MiningHit>();
            sim.HitLanded += hits.Add;
            sim.SpawnCrystalAt(sim.Miner.Distance + 1);
            sim.Tick(200);

            Assert.That(hits.Count, Is.GreaterThan(100));
            Assert.That(hits.All(h => h.IsCritical), Is.True);
        }

        // ── Picareta a 7 × o dano (P5) ──────────────────────────────────────────────────────────────────────────

        [Test]
        public void Pickaxe_CostsSevenTimesItsDamage()
        {
            var pickaxe = Asset().ToEconomy().Get(UpgradeId.Pickaxe);
            Assert.That(pickaxe.CostAt(0), Is.EqualTo(14).Within(1e-9), "nível 1: dano 2 → 14");
            Assert.That(pickaxe.CostAt(1), Is.EqualTo(15.4).Within(1e-9), "nível 2: dano 2,2 → 15,4");
            Assert.That(pickaxe.CostAt(2), Is.EqualTo(17.08).Within(1e-9), "nível 3: dano 2,44 → 17,08");
        }

        // ── Offline 4 h / 6 h (D-253) ──────────────────────────────────────────────────────────────────────────

        [TestCase(1, 4), TestCase(19, 4), TestCase(20, 6), TestCase(100, 6)]
        public void Offline_IsFourHours_AndSixAfterFloor20(int deepest, double hours)
        {
            Assert.That(V8Rules().OfflineMaxSecondsFor(deepest), Is.EqualTo(hours * 3600));
        }

        [Test]
        public void Offline_CountsSixHours_OnceTheRecordReachesFloor20_EvenBackOnFloor1()
        {
            var sim = Sim();
            var eightHours = OfflineMining.Compute(sim.Rules, sim.Economy, sim.State, sim.Permanents, 8 * 3600);
            Assert.That(eightHours.SecondsCounted, Is.EqualTo(4 * 3600), "andar 1: 4 h");

            sim.SkipToCave(20);
            eightHours = OfflineMining.Compute(sim.Rules, sim.Economy, sim.State, sim.Permanents, 8 * 3600);
            Assert.That(eightHours.SecondsCounted, Is.EqualTo(6 * 3600), "chegou ao 20: 6 h");

            var record = PermanentState.Restore(0, 1, _ => 0, V8Permanents(), deepestFloor: 20);
            var newRun = Sim(record);
            Assert.That(newRun.State.Cave, Is.EqualTo(1));
            eightHours = OfflineMining.Compute(newRun.Rules, newRun.Economy, newRun.State, newRun.Permanents, 8 * 3600);
            Assert.That(eightHours.SecondsCounted, Is.EqualTo(6 * 3600), "o recorde vale na run nova");
        }

        // ── A passagem no v8 ────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void Floor100_TheFullBarLeadsToTheHeart()
        {
            var sim = Sim();
            sim.SkipToCave(100);
            Assert.That(sim.RequiredBreaks, Is.EqualTo(1000));
            sim.FillFloorBar();
            Assert.That(sim.AdvanceFloor(), Is.True);
            Assert.That(sim.State.ReachedHeart, Is.True);
            Assert.That(sim.Permanents.State.DeepestFloor, Is.EqualTo(100));
        }

        [Test]
        public void DeepestFloor_RisesWithEveryNewFloor_AndSurvivesTheRest()
        {
            var sim = Sim();
            sim.SkipToCave(12);
            Assert.That(sim.Permanents.State.DeepestFloor, Is.EqualTo(12));

            var next = new MiningSimulation(V8Rules(), new SeededRandom(3), sim.Permanents.NewRun(EconomyRules.None(2)),
                                            permanents: sim.Permanents);
            Assert.That(next.State.Cave, Is.EqualTo(1));
            Assert.That(next.Permanents.State.DeepestFloor, Is.EqualTo(12), "o recorde é da conta, não da run");
        }

        // ── O asset ─────────────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void BalanceGddV8Asset_HasTheNumbersOfTheGdd()
        {
            var asset = Asset();
            var rules = asset.ToRules();
            var strike = asset.ToStrike();
            var permanents = asset.ToPermanents();

            Assert.That(rules.CaveCount, Is.EqualTo(100), "100 andares");
            Assert.That(rules.LevelsPerBiome, Is.EqualTo(10), "um bioma a cada 10 (D-237)");
            Assert.That(rules.CrystalHealthIn(1), Is.EqualTo(1));
            Assert.That(rules.CrystalHealthIn(10), Is.EqualTo(Math.Pow(1.5, 9)).Within(1e-9), "vida 1 × 1,5^(n−1)");
            Assert.That(rules.RewardFormula, Is.EqualTo(RewardFormula.FloorScaled));
            Assert.That(rules.CrystalRewardIn(10), Is.EqualTo(Math.Pow(1.5, 9) * 0.2 * 9).Within(1e-9), "D-238");
            Assert.That(rules.FloorBreaks, Is.EqualTo(10), "andar × 10 (D-239)");
            Assert.That(rules.PickaxeFormula, Is.EqualTo(PickaxeFormula.LevelScaled));
            Assert.That(rules.OfflineMaxSecondsFor(1), Is.EqualTo(4 * 3600), "4 h (D-253)");
            Assert.That(rules.OfflineMaxSecondsFor(20), Is.EqualTo(6 * 3600), "6 h a partir do andar 20");
            Assert.That(strike.CritMultiplier, Is.EqualTo(1.5), "×1,5 (D-249)");
            Assert.That(strike.CritChance, Is.EqualTo(0));
            Assert.That(strike.TapCritChance, Is.EqualTo(0.05));

            var memory = permanents.Get(PermanentId.Memory);
            Assert.That((memory.Effect, memory.MaxLevel, memory.CostAt(0)), Is.EqualTo((5.0, 100, 11.0)), "−5, até 100, 10 + 2^(n−1)");
            Assert.That(asset.ToEconomy().Get(UpgradeId.Letrado).IsEnabled, Is.False, "sem o Mapa (D-256)");
        }
    }
}
