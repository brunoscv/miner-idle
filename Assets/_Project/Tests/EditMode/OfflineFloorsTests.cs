using System;
using System.Collections.Generic;
using System.Linq;
using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9c E4 (D-246): o offline que desce andares. Os cristais do estagiário contam na barra do andar; com o avanço
    /// automático, ao encher ele passa ao andar seguinte e continua lá, com a vida e as lascas de lá; sem ele, a barra
    /// enche e fica cheia (P4). O andar 100 espera o toque (D-270). A conta fechada é comparada com uma de cristal em
    /// cristal (a corrida e cada golpe), escrita à parte aqui.
    /// </summary>
    public class OfflineFloorsTests
    {
        const double Hour = 3600;

        /// <summary>Os números do v8: vida 1,5^(n−1), lascas do D-238, andar × 10; offline 4 h, 6 h depois do 20.</summary>
        static MiningRules Rules(double healthGrowth = 1.5) =>
            new MiningRules(
                initialCrystals: 4, firstCrystalDistance: 3, maxCrystals: 12, spawnInterval: 1e9, minCrystalSpacing: 2,
                spawnDistance: 12, crystalHealth: 1, crystalReward: 0.2, damage: 1, hitInterval: 2, walkSpeed: 4,
                miningRange: 1, minCrystals: 4, caveCount: 100, caveHealthGrowth: healthGrowth, floorBreaks: 10,
                arrivalStrike: ArrivalStrike.Immediate, pickaxeFormula: PickaxeFormula.LevelScaled, levelsPerBiome: 10,
                offlineMaxSeconds: 4 * Hour, rewardFormula: RewardFormula.FloorScaled, offlineDeepMaxSeconds: 6 * Hour,
                offlineDeepFloor: 20, autoAdvanceFloor: 5, internFloor: 5, rareFloor: 10);

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

        static PermanentRules PermanentRules() => new PermanentRules(
            10, new RedShardTable(10, 10, 5, 50, 10, 1),
            new UpgradeDefinition(10, 2, 100, 5, CostFormula.OffsetExponential),
            new UpgradeDefinition(10, 2, 20, 0.1, CostFormula.OffsetExponential),
            new UpgradeDefinition(10, 2, 20, 0.5, CostFormula.OffsetExponential),
            new UpgradeDefinition(10, 2, 10, 0.05, CostFormula.OffsetExponential),
            new UpgradeDefinition(10, 2, 10, 1, CostFormula.OffsetExponential));

        static Permanents Permanents(int deepest = 5, int memory = 0) => new Permanents(PermanentRules(),
            PermanentState.Restore(0, 1, id => id == PermanentId.Memory ? memory : 0, PermanentRules(), deepest));

        /// <summary>No andar <paramref name="floor"/>, com o estagiário contratado e <paramref name="levels"/> da árvore dele.</summary>
        static GameState At(int floor, long breaks = 0, params (UpgradeId id, int level)[] levels)
        {
            var map = new Dictionary<UpgradeId, int> { [UpgradeId.Intern] = 1 };
            foreach (var (id, level) in levels) map[id] = level;
            return GameState.Restore(0, 0, 0, id => map.TryGetValue(id, out int l) ? l : 0, Economy(), 100, floor, breaks);
        }

        /// <summary>
        /// A conta de cristal em cristal: a corrida (2 ÷ 4 = 0,5 s) e os golpes um a um (o 1º na chegada, A2), andar por
        /// andar; com o automático, a barra cheia passa ao seguinte (menos no último).
        /// </summary>
        static (long crystals, double shards, int floor, long breaks) CrystalByCrystal(
            MiningRules rules, GameState state, Permanents permanents, double seconds, bool auto)
        {
            var intern = InternStats.Compute(rules, Economy(), state, permanents);
            double left = Math.Min(seconds, OfflineMining.MaxSeconds(rules, state, permanents));
            int floor = state.Cave;
            long breaks = state.CaveBreaks, crystals = 0;
            double shards = 0;
            while (true)
            {
                if (auto && floor < rules.CaveCount && breaks >= permanents.FloorRequirement(rules, floor))
                {
                    floor++;
                    breaks = 0;
                    continue;
                }
                double health = rules.CrystalHealthIn(floor);
                double time = rules.MinCrystalSpacing / rules.WalkSpeed;
                for (int hit = 0; health > 1e-9; hit++)
                {
                    if (hit > 0) time += intern.HitInterval;
                    health -= intern.Damage;
                }
                if (time > left + 1e-9) break;
                left -= time;
                crystals++;
                breaks++;
                shards += rules.CrystalRewardIn(floor) * intern.OfflineRewardMultiplier;
            }
            return (crystals, shards, floor, breaks);
        }

        static void AssertSameAsCrystalByCrystal(OfflineReport report, MiningRules rules, GameState state,
                                                 Permanents permanents, double seconds, bool auto)
        {
            var expected = CrystalByCrystal(rules, state, permanents, seconds, auto);
            Assert.That(report.Crystals, Is.EqualTo(expected.crystals), "cristais");
            Assert.That(report.Shards, Is.EqualTo(expected.shards).Within(1e-9).Percent, "lascas");
            Assert.That(report.ToFloor, Is.EqualTo(expected.floor), "andar da volta");
            Assert.That(report.EndBreaks, Is.EqualTo(expected.breaks), "barra da volta");
        }

        // ── A conta ────────────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void WithoutTheIntern_Nothing()
        {
            var state = GameState.Restore(0, 0, 0, _ => 0, Economy(), 100, 7, 3);
            var report = OfflineMining.Compute(Rules(), Economy(), state, Permanents(), 4 * Hour, autoAdvance: true);
            Assert.That(report.Hired, Is.False);
            Assert.That(report.Crystals, Is.EqualTo(0));
            Assert.That((report.FromFloor, report.ToFloor, report.EndBreaks), Is.EqualTo((7, 7, 3L)), "nada muda");
        }

        [Test]
        public void AutoOff_TheBarFills_AndStaysFull()
        {
            var rules = Rules();
            var state = At(5);
            var report = OfflineMining.Compute(rules, Economy(), state, Permanents(), 4 * Hour, autoAdvance: false);
            Assert.That(report.ToFloor, Is.EqualTo(5), "sem o automático, o andar não muda");
            Assert.That(report.FloorsDescended, Is.EqualTo(0));
            Assert.That(report.EndBreaks, Is.EqualTo(report.Crystals), "todos os cristais contam na barra");
            Assert.That(report.EndBreaks, Is.GreaterThan(50), "a barra do andar 5 (50) enche e fica cheia (P4)");
            AssertSameAsCrystalByCrystal(report, rules, state, Permanents(), 4 * Hour, auto: false);
        }

        [Test]
        public void AutoOn_DescendsFloors_EachWithItsHealthAndShards()
        {
            var rules = Rules();
            var state = At(5, 0, (UpgradeId.InternPickaxe, 10));
            var report = OfflineMining.Compute(rules, Economy(), state, Permanents(), 4 * Hour, autoAdvance: true);
            Assert.That(report.FromFloor, Is.EqualTo(5));
            Assert.That(report.FloorsDescended, Is.GreaterThan(1), "desce mais de um andar");
            Assert.That(report.EndBreaks, Is.LessThan(report.ToFloor * 10), "no último, a barra ainda enchendo");
            AssertSameAsCrystalByCrystal(report, rules, state, Permanents(), 4 * Hour, auto: true);
        }

        [TestCase(0, 0)]
        [TestCase(30, 0)]
        [TestCase(30, 2)]
        [TestCase(0, 9)]
        public void AutoOn_FromAPartBar_AndWithTheMemory_MatchesCrystalByCrystal(long breaks, int memory)
        {
            var rules = Rules();
            var state = At(6, breaks, (UpgradeId.InternPickaxe, 6), (UpgradeId.InternSpeed, 4));
            var permanents = Permanents(6, memory);
            foreach (double seconds in new[] { 61.0, 15 * 60, 1 * Hour, 4 * Hour })
            {
                var report = OfflineMining.Compute(rules, Economy(), state, permanents, seconds, autoAdvance: true);
                AssertSameAsCrystalByCrystal(report, rules, state, permanents, seconds, auto: true);
            }
        }

        [Test]
        public void AutoOn_Floor100_WaitsForTheTap()
        {
            var rules = Rules(healthGrowth: 1);
            var state = At(99);
            var report = OfflineMining.Compute(rules, Economy(), state, Permanents(99), 4 * Hour, autoAdvance: true);
            Assert.That(report.ToFloor, Is.EqualTo(100), "chega ao último andar");
            Assert.That(report.EndBreaks, Is.GreaterThan(1000), "e a barra dele enche e espera (D-270)");
            AssertSameAsCrystalByCrystal(report, rules, state, Permanents(99), 4 * Hour, auto: true);
        }

        [Test]
        public void TheCap_IsFourHours_AndSixAfterFloor20()
        {
            var rules = Rules();
            var four = OfflineMining.Compute(rules, Economy(), At(5), Permanents(5), 8 * Hour, autoAdvance: true);
            Assert.That(four.SecondsCounted, Is.EqualTo(4 * Hour));
            var six = OfflineMining.Compute(rules, Economy(), At(5), Permanents(20), 8 * Hour, autoAdvance: true);
            Assert.That(six.SecondsCounted, Is.EqualTo(6 * Hour));
            Assert.That(six.Crystals, Is.GreaterThan(four.Crystals));
        }

        [Test]
        public void ARareCard_StillCounts_AcrossTheFloors()
        {
            var rules = Rules();
            var withCard = GameState.Restore(0, 0, 0, id => id == UpgradeId.Intern ? 1 : 0, Economy(), 100, 5,
                bonuses: new[] { new ActiveBonus(BonusKind.DamageMultiplier, 2, 600, 600, BonusSource.Rare, 0) });
            var plain = OfflineMining.Compute(rules, Economy(), At(5), Permanents(), 2 * Hour, autoAdvance: true);
            var boosted = OfflineMining.Compute(rules, Economy(), withCard, Permanents(), 2 * Hour, autoAdvance: true);
            Assert.That(boosted.Crystals, Is.GreaterThanOrEqualTo(plain.Crystals), "10 min de dano ×2 só ajudam");
            Assert.That(boosted.ToFloor, Is.GreaterThanOrEqualTo(plain.ToFloor));
        }

        // ── Na simulação ───────────────────────────────────────────────────────────────────────────────────────

        static MiningSimulation Sim(GameState state, Permanents permanents) =>
            new MiningSimulation(Rules(), new SeededRandom(1), state, Economy(), permanents: permanents);

        [Test]
        public void ApplyOffline_WithAuto_ComesBackDeeper_WithThatFloorsQueue_AndNoTransition()
        {
            var sim = Sim(At(5, 0, (UpgradeId.InternPickaxe, 10)), Permanents());
            sim.FloorTransitionSeconds = 2.5;
            int changes = 0;
            sim.CaveChanged += _ => changes++;

            var report = sim.ApplyOffline(4 * Hour);

            Assert.That(report.FloorsDescended, Is.GreaterThan(0));
            Assert.That(sim.State.Cave, Is.EqualTo(report.ToFloor));
            Assert.That(sim.State.CaveBreaks, Is.EqualTo(report.EndBreaks));
            Assert.That(sim.Permanents.State.DeepestFloor, Is.EqualTo(report.ToFloor), "o recorde sobe");
            Assert.That(sim.IsInFloorTransition, Is.False, "offline não tem transição (D-191)");
            double health = Rules().CrystalHealthIn(report.ToFloor);
            Assert.That(sim.Crystals.All(c => Math.Abs(c.MaxHealth - health) < 1e-9), Is.True, "a fila do andar novo");
            Assert.That(changes, Is.GreaterThan(0), "o HUD e o botão ficam sabendo");
        }

        [Test]
        public void ApplyOffline_WithAutoOff_StaysWithTheBarFull()
        {
            var sim = Sim(At(5), Permanents());
            sim.AutoAdvance = false;
            var report = sim.ApplyOffline(4 * Hour);
            Assert.That(sim.State.Cave, Is.EqualTo(5));
            Assert.That(sim.FloorProgress, Is.EqualTo(1), "barra cheia, esperando o toque");
            Assert.That(sim.State.CaveBreaks, Is.EqualTo(report.Crystals));
        }

        [Test]
        public void ApplyOffline_BeforeFloor5_TheAutoDoesNotExistYet()
        {
            var sim = Sim(At(2), Permanents(2));
            var report = sim.ApplyOffline(4 * Hour);
            Assert.That(report.ToFloor, Is.EqualTo(2), "o automático só existe depois do andar 5");
            Assert.That(sim.State.Cave, Is.EqualTo(2));
            Assert.That(sim.CanAdvance, Is.True, "a barra encheu");
        }

        [Test]
        public void TheReturnWaitingForCollect_RemembersWhereItStarted()
        {
            var sim = Sim(At(5, 0, (UpgradeId.InternPickaxe, 10)), Permanents());
            sim.ApplyOffline(Hour, noticeSeconds: 60);
            var first = sim.PendingOffline;
            Assert.That(first.FromLevel, Is.EqualTo(5));
            Assert.That(first.Level, Is.EqualTo(sim.State.Cave));
            Assert.That(first.Descended, Is.True);

            sim.ApplyOffline(Hour, noticeSeconds: 60); // outra volta sem COLETAR
            var both = sim.PendingOffline;
            Assert.That(both.FromLevel, Is.EqualTo(5), "o andar de partida é o da primeira volta");
            Assert.That(both.Level, Is.EqualTo(sim.State.Cave));
            Assert.That(both.Level, Is.GreaterThanOrEqualTo(first.Level));

            sim.CollectOffline();
            sim.ApplyOffline(Hour, noticeSeconds: 60);
            Assert.That(sim.PendingOffline.FromLevel, Is.EqualTo(both.Level), "depois do COLETAR, começa de onde está");
        }
    }
}
