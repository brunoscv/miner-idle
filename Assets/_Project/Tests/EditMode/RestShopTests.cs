using System.Collections.Generic;
using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9d E1 (D-265, D-272…D-278): o descanso do v8 no núcleo. A run nova fica parada até o INICIAR (nada corre, nem o
    /// offline), as permanentes só se compram descansando, a Promoção pede o Treinar no 5, o Dedo Fogoso soma no dano do
    /// crítico e o Atalho encurta a distância entre cristais (3 m menos 0,5 m por nível, 1 m = 2 unidades). Os números
    /// estão escritos à mão, como no GDD v8.2.
    /// </summary>
    public class RestShopTests
    {
        /// <summary>3 m entre cristais (6 unidades, D-277); fila parada, cristais com muita vida.</summary>
        static MiningRules Rules(double health = 1e12, double damage = 10, int initialCrystals = 0) =>
            new MiningRules(
                initialCrystals: initialCrystals, firstCrystalDistance: 3, maxCrystals: 12, spawnInterval: 1e9, minCrystalSpacing: 6,
                spawnDistance: 40, crystalHealth: health, crystalReward: 1, damage: damage, hitInterval: 1, walkSpeed: 4,
                miningRange: 1, caveCount: 100);

        static EconomyRules Economy() => new EconomyRules(
            pickaxe: new UpgradeDefinition(2, 2, 49, 1, CostFormula.Linear),
            speed: new UpgradeDefinition(50, 2, 19, 0.1),
            mining: new UpgradeDefinition(20, 5, 10, 1),
            doublePickaxe: new UpgradeDefinition(5000, 1, 0, 2),
            boots: new UpgradeDefinition(0, 1, 0, 0),
            minHitInterval: 0.1,
            critical: new UpgradeDefinition(50, 2, 10, 0.05),
            intern: new UpgradeDefinition(500, 1, 1, 1),
            internPickaxe: new UpgradeDefinition(50, 4, 19, 5),
            internSpeed: new UpgradeDefinition(50, 5, 19, 0.1),
            internMining: new UpgradeDefinition(50, 1.2, 10, 1),
            internPickaxeGrowth: 2);

        static UpgradeDefinition Line(int max, double effect) => new UpgradeDefinition(10, 2, max, effect, CostFormula.OffsetExponential);

        /// <summary>GDD v8.2 §4.2: as 5 do anão, Treinar e Promoção (no 5), Dedo Fogoso (10 × 0,5) e Atalho (5 × 0,5 m).</summary>
        static PermanentRules V82Permanents(int promotionTrainingLevel = 5) => new PermanentRules(
            10, new RedShardTable(10, 10, 5, 50, 10, 1), promotionTrainingLevel,
            Line(100, 5), Line(20, 0.1), Line(20, 0.5), Line(10, 0.05), Line(10, 1),
            Line(10, 0.1), new UpgradeDefinition(50, 1, 1, 1),
            new UpgradeDefinition(10, 2, 10, 0.5, CostFormula.OffsetExponential),
            new UpgradeDefinition(50, 2, 5, 0.5, CostFormula.OffsetExponential));

        static PermanentState With(params (PermanentId id, int level)[] levels)
        {
            var map = new Dictionary<PermanentId, int>();
            foreach (var (id, level) in levels) map[id] = level;
            return PermanentState.Restore(0, 1, p => map.TryGetValue(p, out var l) ? l : 0, V82Permanents());
        }

        static MiningSimulation Sim(PermanentState permanent = null, StrikeRules strike = null, PermanentRules rules = null,
                                    GameState state = null) =>
            new MiningSimulation(Rules(), new SeededRandom(5), state, Economy(), strike ?? new StrikeRules(0, 1.5),
                                 new Permanents(rules ?? V82Permanents(), permanent ?? new PermanentState()));

        // ── A run parada até o INICIAR (D-265, D-275) ───────────────────────────────────────────────────────────

        [Test]
        public void Resting_NothingRuns_NorTheRunShop_NorTheOffline()
        {
            var sim = Sim();
            sim.AddShards(1000);
            sim.SpawnCrystalAt(sim.Miner.Distance + 5);
            sim.IsResting = true;
            double distance = sim.Miner.Distance;

            sim.Tick(30);

            Assert.That(sim.State.PlaySeconds, Is.EqualTo(0), "o tempo do dia não anda");
            Assert.That(sim.Miner.Distance, Is.EqualTo(distance), "o anão não anda");
            Assert.That(sim.Purchase(UpgradeId.Pickaxe), Is.EqualTo(PurchaseResult.Locked), "a loja da run espera o INICIAR");
            Assert.That(sim.State.Shards, Is.EqualTo(1000));
            Assert.That(sim.ApplyOffline(3600).SecondsCounted, Is.EqualTo(0), "D-275: o offline não conta");
            Assert.That(sim.Rest(force: true), Is.Null, "já está descansando");

            sim.IsResting = false;
            sim.Tick(1);
            Assert.That(sim.State.PlaySeconds, Is.EqualTo(1), "depois do INICIAR, corre");
        }

        [Test]
        public void Permanents_AreBoughtOnlyWhileResting()
        {
            var sim = Sim();
            sim.AddRedShards(100);

            Assert.That(sim.PurchasePermanent(PermanentId.MountainBlood), Is.EqualTo(PurchaseResult.Locked), "§4.2: só no descanso");
            Assert.That(sim.PreviewPermanent(PermanentId.MountainBlood).State, Is.Not.EqualTo(UpgradeButtonState.Purchasable),
                        "a linha mostra o preço, mas não compra");
            Assert.That(sim.Permanents.State.RedShards, Is.EqualTo(100));

            sim.IsResting = true;
            Assert.That(sim.PreviewPermanent(PermanentId.MountainBlood).State, Is.EqualTo(UpgradeButtonState.Purchasable));
            Assert.That(sim.PurchasePermanent(PermanentId.MountainBlood), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(sim.Permanents.State.LevelOf(PermanentId.MountainBlood), Is.EqualTo(1));
        }

        [Test]
        public void TheRunOfTheStart_IsBuiltWithWhatWasBoughtWhileResting()
        {
            var economy = Economy();
            var permanent = With((PermanentId.InternTraining, 5));
            var resting = Sim(permanent, state: new GameState());
            resting.IsResting = true;
            resting.AddRedShards(200);
            Assert.That(resting.PurchasePermanent(PermanentId.DwarfSoul), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(resting.PurchasePermanent(PermanentId.InternPromotion), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(resting.PurchasePermanent(PermanentId.Shortcut), Is.EqualTo(PurchaseResult.Purchased));

            // O INICIAR: a run montada de novo com as permanentes de agora (o que o MiningRunner.StartRun faz).
            var run = new MiningSimulation(Rules(), new SeededRandom(9), resting.Permanents.NewRun(economy), economy,
                                           permanents: resting.Permanents);

            Assert.That(run.IsResting, Is.False);
            Assert.That(run.State.LevelOf(UpgradeId.Pickaxe), Is.EqualTo(1), "a Alma dos Anões 1");
            Assert.That(run.IsInternHired, Is.True, "a Promoção: a run começa com ele");
            Assert.That(run.CrystalSpacing, Is.EqualTo(5), "o Atalho 1: 3 m − 0,5 m = 2,5 m = 5 unidades");
        }

        // ── Promoção no Treinar 5 (GDD v8.2) ────────────────────────────────────────────────────────────────────

        [TestCase(4, true)]
        [TestCase(5, false)]
        public void Promotion_NeedsTrainingFive(int training, bool locked)
        {
            var sim = Sim(With((PermanentId.InternTraining, training)));
            sim.IsResting = true;
            sim.AddRedShards(100);

            Assert.That(sim.Permanents.RequiredLevel(PermanentId.InternPromotion), Is.EqualTo(5));
            Assert.That(sim.Permanents.IsLocked(PermanentId.InternPromotion), Is.EqualTo(locked));
            Assert.That(sim.PurchasePermanent(PermanentId.InternPromotion),
                        Is.EqualTo(locked ? PurchaseResult.Locked : PurchaseResult.Purchased));
        }

        [Test]
        public void WithoutTheTrainingLevel_ThePromotionWaitsForTheMax_AsInV7()
        {
            var permanents = new Permanents(V82Permanents(promotionTrainingLevel: 0), With((PermanentId.InternTraining, 5)));

            Assert.That(permanents.RequiredLevel(PermanentId.InternPromotion), Is.EqualTo(10), "D-169: o Treinar no limite");
            Assert.That(permanents.IsLocked(PermanentId.InternPromotion), Is.True);
        }

        // ── Dedo Fogoso (§4.2) ──────────────────────────────────────────────────────────────────────────────────

        static MiningHit FirstHit(MiningSimulation sim)
        {
            var hits = new List<MiningHit>();
            sim.HitLanded += hits.Add;
            sim.SpawnCrystalAt(sim.Miner.Distance + 1);
            for (int i = 0; i < 40 && hits.Count == 0; i++) sim.Tick(0.5);
            Assert.That(hits, Is.Not.Empty, "o anão bateu");
            return hits[0];
        }

        [TestCase(0.5, 0, 0, 1.5, TestName = "FieryFinger_Zero_IsTimesOnePointFive")]
        [TestCase(0.5, 0, 1, 2.0, TestName = "FieryFinger_One_IsTimesTwo")]
        [TestCase(0.5, 0, 10, 6.5, TestName = "FieryFinger_Ten_IsTimesSixPointFive")]
        [TestCase(0.9, 10, 2, 2.9, TestName = "FieryFinger_Two_AndTheExcessOfFortyPercent")]
        public void FieryFinger_AddsToTheCriticalDamage(double baseChance, int nervous, int fiery, double multiplier)
        {
            var sim = Sim(With((PermanentId.NervousFingers, nervous), (PermanentId.FieryFinger, fiery)),
                          new StrikeRules(baseChance, 1.5));
            sim.ForceCritical = true;

            var hit = FirstHit(sim);

            Assert.That(hit.IsCritical, Is.True);
            Assert.That(hit.Damage, Is.EqualTo(10 * multiplier).Within(1e-9), $"×1,5 + 0,5 × {fiery} + o excedente (D-249)");
        }

        // ── Atalho (D-252, D-277) ───────────────────────────────────────────────────────────────────────────────

        [TestCase(0, 6)]
        [TestCase(1, 5)]
        [TestCase(3, 3)]
        [TestCase(5, 1)]
        public void Shortcut_TakesHalfAMeterPerLevel(int level, double units)
        {
            var sim = Sim(With((PermanentId.Shortcut, level)));

            Assert.That(sim.Permanents.CrystalSpacing(sim.Rules), Is.EqualTo(units));
            Assert.That(sim.CrystalSpacing, Is.EqualTo(units), $"3 m − {level} × 0,5 m, em unidades (1 m = 2)");
        }

        [TestCase(0, 6)]
        [TestCase(5, 1)]
        public void Shortcut_SpacesTheCrystalsOfTheQueue(int level, double units)
        {
            var sim = new MiningSimulation(Rules(initialCrystals: 3), new SeededRandom(5), null, Economy(),
                                           new StrikeRules(0, 1.5),
                                           new Permanents(V82Permanents(), With((PermanentId.Shortcut, level))));

            Assert.That(sim.Crystals.Count, Is.EqualTo(3));
            Assert.That(sim.Crystals[1].Distance - sim.Crystals[0].Distance, Is.EqualTo(units).Within(1e-9));
            Assert.That(sim.Crystals[2].Distance - sim.Crystals[1].Distance, Is.EqualTo(units).Within(1e-9));
        }

        [Test]
        public void Cycle_TheDebugShortcut_GoesUpAndBackToZero()
        {
            var sim = Sim();
            for (int i = 1; i <= 5; i++) Assert.That(sim.CyclePermanent(PermanentId.Shortcut), Is.EqualTo(i));
            Assert.That(sim.CrystalSpacing, Is.EqualTo(1));
            Assert.That(sim.CyclePermanent(PermanentId.Shortcut), Is.EqualTo(0), "no limite, volta ao 0");
            Assert.That(sim.CrystalSpacing, Is.EqualTo(6));
        }

        [Test]
        public void Offline_WithTheShortcut_WalksLess()
        {
            var rules = Rules();
            double far = OfflineMining.SecondsPerCrystal(rules, 3, 1);
            double near = OfflineMining.SecondsPerCrystal(rules, 3, 1, spacing: 1);

            Assert.That(far - near, Is.EqualTo((6 - 1) / 4.0).Within(1e-9), "5 unidades a menos, a 4 por segundo");
        }
    }
}
