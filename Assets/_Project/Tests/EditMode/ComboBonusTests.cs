using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Persistence;
using NUnit.Framework;
using UnityEditor;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9b, E1: os bônus com tempo e o combo (GDD v7 §3.4; D-96, D-177…D-181, D-211…D-214). Parar de tocar depois de um
    /// marco dá o bônus dele; o dano soma os +% e multiplica depois do Sangue da Montanha; o ×50 faz todo golpe crítico; o
    /// fim é um evento do <c>Tick</c>; o tempo corre com o app fechado; o save leva os bônus; o descanso os termina.
    /// </summary>
    public class ComboBonusTests
    {
        const string GddV7Path = "Assets/_Project/Data/Balance_GDD_v7.asset";

        /// <summary>Os quatro marcos do GDD v7.</summary>
        static readonly ComboBonusRule[] Tiers =
        {
            new ComboBonusRule(10, BonusKind.DamagePercent, 0.10, 5),
            new ComboBonusRule(30, BonusKind.DamagePercent, 0.15, 10),
            new ComboBonusRule(50, BonusKind.GuaranteedCritical, 0, 20),
            new ComboBonusRule(100, BonusKind.DamagePercent, 0.50, 30),
        };

        static MiningRules Rules(double health = 1e9) =>
            new MiningRules(
                initialCrystals: 0, firstCrystalDistance: 3, maxCrystals: 12, spawnInterval: 1e9, minCrystalSpacing: 2,
                spawnDistance: 12, crystalHealth: health, crystalReward: 1, damage: 1, hitInterval: 1, walkSpeed: 4, miningRange: 1);

        static StrikeRules Strike(double critChance = 0) =>
            new StrikeRules(critChance, 2, 0.1, 1, tapCritChance: critChance, comboBonuses: Tiers);

        static EconomyRules WithIntern() => new EconomyRules(
            pickaxe: new UpgradeDefinition(2, 2, 49, 1, CostFormula.Linear),
            speed: new UpgradeDefinition(50, 2, 19, 0.1),
            mining: new UpgradeDefinition(20, 5, 10, 1),
            doublePickaxe: new UpgradeDefinition(5000, 1, 0, 2),
            boots: new UpgradeDefinition(0, 1, 0, 0),
            minHitInterval: 0.1,
            intern: new UpgradeDefinition(500, 1, 1, 1),
            internPickaxe: new UpgradeDefinition(50, 4, 19, 5),
            internSpeed: new UpgradeDefinition(50, 5, 19, 0.1),
            internMining: new UpgradeDefinition(50, 1.2, 10, 1),
            internPickaxeGrowth: 2);

        static UpgradeDefinition Line(int max, double effect) => new UpgradeDefinition(10, 2, max, effect, CostFormula.OffsetExponential);

        static PermanentRules V7Permanents() => new PermanentRules(
            10, new RedShardTable(10, 10, 5, 50, 10, 1),
            Line(10, 10), Line(20, 0.1), Line(20, 0.5), Line(10, 0.05), Line(10, 1),
            Line(10, 0.1), new UpgradeDefinition(50, 1, 1, 1));

        static MiningSimulation Sim(ComboStacking stacking = ComboStacking.Cumulative, GameState state = null,
                                    PermanentState permanent = null) =>
            new MiningSimulation(Rules(), new SeededRandom(7), state, WithIntern(), Strike(),
                                 new Permanents(V7Permanents(), permanent ?? new PermanentState()))
            {
                ComboStacking = stacking,
            };

        /// <summary><paramref name="taps"/> toques no mesmo instante (sem teto nestas regras) e 1 s sem tocar: o combo acaba.</summary>
        static void TapAndStop(MiningSimulation sim, int taps)
        {
            for (int i = 0; i < taps; i++) sim.Tap();
            sim.Tick(1);
        }

        static double TapDamage(MiningSimulation sim)
        {
            double damage = double.NaN;
            void OnHit(MiningHit hit) => damage = hit.Damage;
            sim.HitLanded += OnHit;
            sim.Tap();
            sim.HitLanded -= OnHit;
            return damage;
        }

        static double[] TierList(MiningSimulation sim) => sim.Bonuses.All.Select(b => (double)b.Tier).OrderBy(t => t).ToArray();

        // ── Os marcos ───────────────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void StoppingBelowTen_GivesNothing()
        {
            var sim = Sim();
            TapAndStop(sim, 9);
            Assert.That(sim.Combo, Is.EqualTo(0));
            Assert.That(sim.Bonuses.Count, Is.EqualTo(0));
        }

        [Test]
        public void StoppingAtTen_GivesTenPercentForFiveSeconds_WhenTheComboEnds()
        {
            var sim = Sim();
            var granted = new List<(int combo, int[] tiers)>();
            sim.ComboBonusesGranted += (combo, rules) => granted.Add((combo, rules.Select(r => r.Combo).ToArray()));

            for (int i = 0; i < 12; i++) sim.Tap();
            Assert.That(sim.Bonuses.Count, Is.EqualTo(0), "ainda tocando: o bônus só vem ao parar (D-96)");
            sim.Tick(1);

            Assert.That(granted.Single().combo, Is.EqualTo(12));
            Assert.That(granted.Single().tiers, Is.EqualTo(new[] { 10 }));
            var bonus = sim.Bonuses.All.Single();
            Assert.That(bonus.Kind, Is.EqualTo(BonusKind.DamagePercent));
            Assert.That(bonus.Value, Is.EqualTo(0.10));
            Assert.That(bonus.SecondsLeft, Is.EqualTo(5).Within(1e-9));
            Assert.That(bonus.Source, Is.EqualTo(BonusSource.Combo));
            Assert.That(sim.Stats.Damage, Is.EqualTo(1.1).Within(1e-12));
        }

        [Test]
        public void Cumulative_FiftySeven_GivesTheThreeTiersReached_AndTheTapHitsTwoAndAHalfTimes()
        {
            var sim = Sim();
            sim.SpawnCrystalAt(1);
            sim.Tick(0);
            double plain = TapDamage(sim);

            sim.GrantComboBonuses(57);

            Assert.That(TierList(sim), Is.EqualTo(new double[] { 10, 30, 50 }));
            Assert.That(sim.Bonuses.DamagePercent, Is.EqualTo(0.25).Within(1e-12));
            Assert.That(sim.Bonuses.GuaranteedCritical, Is.True);
            Assert.That(TapDamage(sim) / plain, Is.EqualTo(2.5).Within(1e-9), "×1,25 e crítico ×2 (o exemplo do plano)");
        }

        [Test]
        public void Cumulative_HundredAndFour_HitsThreeAndAHalfTimes()
        {
            var sim = Sim();
            sim.SpawnCrystalAt(1);
            sim.Tick(0);
            double plain = TapDamage(sim);

            sim.GrantComboBonuses(104);

            Assert.That(sim.Bonuses.DamagePercent, Is.EqualTo(0.75).Within(1e-12));
            Assert.That(TapDamage(sim) / plain, Is.EqualTo(3.5).Within(1e-9));
        }

        [Test]
        public void HighestOnly_GivesOnlyTheHighestTierReached()
        {
            var sim = Sim(ComboStacking.HighestOnly);

            TapAndStop(sim, 35);
            Assert.That(TierList(sim), Is.EqualTo(new double[] { 30 }));
            Assert.That(sim.Stats.Damage, Is.EqualTo(1.15).Within(1e-12));

            sim.ClearBonuses();
            sim.GrantComboBonuses(57);
            Assert.That(sim.Bonuses.All.Single().Kind, Is.EqualTo(BonusKind.GuaranteedCritical));
            Assert.That(sim.Stats.Damage, Is.EqualTo(1).Within(1e-12), "o ×50 não dá dano a mais");
        }

        /// <summary>D-212: cada marco vale uma vez por combo. ×230 não dá o ×100 duas vezes.</summary>
        [Test]
        public void TwoHundredThirty_GivesEachTierOnce()
        {
            var sim = Sim();
            TapAndStop(sim, 230);
            Assert.That(TierList(sim), Is.EqualTo(new double[] { 10, 30, 50, 100 }));
            Assert.That(sim.Bonuses.DamagePercent, Is.EqualTo(0.75).Within(1e-12));
        }

        // ── Dois bônus iguais (D-211) ───────────────────────────────────────────────────────────────────────────────

        [Test]
        public void Cumulative_TwoEqualDamageBonuses_CountTogether_EachWithItsOwnTime()
        {
            var sim = Sim();
            sim.GrantComboBonuses(10);
            sim.Tick(3);
            sim.GrantComboBonuses(10);

            Assert.That(sim.Bonuses.Count, Is.EqualTo(2));
            Assert.That(sim.Bonuses.DamagePercent, Is.EqualTo(0.20).Within(1e-12));
            sim.Tick(2); // o primeiro acaba aos 5 s
            Assert.That(sim.Bonuses.Count, Is.EqualTo(1));
            Assert.That(sim.Bonuses.All.Single().SecondsLeft, Is.EqualTo(3).Within(1e-9));
        }

        [Test]
        public void Cumulative_GuaranteedCritical_AddsTheTimeLeft()
        {
            var sim = Sim();
            sim.GrantComboBonuses(50);
            sim.Tick(15); // o ×10 e o ×30 já acabaram; do crítico sobram 5 s
            sim.GrantComboBonuses(50);

            var critical = sim.Bonuses.All.Single(b => b.Kind == BonusKind.GuaranteedCritical);
            Assert.That(critical.SecondsLeft, Is.EqualTo(25).Within(1e-9), "5 que sobravam + 20");
            Assert.That(critical.Duration, Is.EqualTo(25).Within(1e-9), "o anel começa cheio de novo");
            Assert.That(sim.Bonuses.All.Count(b => b.Kind == BonusKind.GuaranteedCritical), Is.EqualTo(1));
        }

        [Test]
        public void HighestOnly_SameTierAgain_RenewsTheTime_WithoutAddingTheEffect()
        {
            var sim = Sim(ComboStacking.HighestOnly);
            sim.GrantComboBonuses(10);
            sim.Tick(3);
            sim.GrantComboBonuses(10);

            var bonus = sim.Bonuses.All.Single();
            Assert.That(bonus.SecondsLeft, Is.EqualTo(5).Within(1e-9));
            Assert.That(sim.Bonuses.DamagePercent, Is.EqualTo(0.10).Within(1e-12));
        }

        // ── Efeitos ─────────────────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void GuaranteedCritical_MakesTheAutomaticHitCritical_EvenWithZeroChance()
        {
            var sim = Sim();
            sim.SpawnCrystalAt(1);
            var hits = new List<MiningHit>();
            sim.HitLanded += hits.Add;
            sim.GrantComboBonuses(50);

            sim.Tick(5);

            Assert.That(hits.Count, Is.GreaterThan(1));
            Assert.That(hits.All(h => h.IsCritical), Is.True);
        }

        [Test]
        public void DamageBonus_MultipliesAfterMountainBlood()
        {
            var sim = Sim();
            sim.AddRedShards(1000);
            sim.PurchasePermanent(PermanentId.MountainBlood); // +10%
            double withBlood = sim.Stats.Damage;
            Assert.That(withBlood, Is.EqualTo(1.1).Within(1e-12));

            sim.GrantComboBonuses(30); // +25%

            Assert.That(sim.Stats.Damage, Is.EqualTo(1.1 * 1.25).Within(1e-12), "base × Sangue × (1 + Σ%), D-179");
        }

        /// <summary>D-177: o estagiário não recebe o bônus do combo.</summary>
        [Test]
        public void Intern_DoesNotGetTheComboBonus()
        {
            var sim = Sim();
            Assert.That(sim.HireIntern(), Is.True);
            double intern = sim.InternStats.Damage;

            sim.GrantComboBonuses(104);

            Assert.That(sim.InternStats.Damage, Is.EqualTo(intern));
        }

        [Test]
        public void TheShopPreview_ComparesWithoutTheBonus()
        {
            var sim = Sim();
            var before = sim.Preview(UpgradeId.Pickaxe);
            sim.GrantComboBonuses(104);
            var after = sim.Preview(UpgradeId.Pickaxe);

            Assert.That(after.CurrentValue, Is.EqualTo(before.CurrentValue));
            Assert.That(after.NextValue, Is.EqualTo(before.NextValue));
        }

        // ── O fim pelo Tick ─────────────────────────────────────────────────────────────────────────────────────────

        static List<double> EndTimes(MiningSimulation sim)
        {
            var ends = new List<double>();
            int count = sim.Bonuses.Count;
            sim.BonusesChanged += () =>
            {
                if (sim.Bonuses.Count < count) ends.Add(sim.Elapsed);
                count = sim.Bonuses.Count;
            };
            return ends;
        }

        [Test]
        public void TheBonusesEnd_AtTheExactTime_HoweverTimeIsSliced()
        {
            var big = Sim();
            var sliced = Sim();
            big.GrantComboBonuses(104);
            sliced.GrantComboBonuses(104);
            var bigEnds = EndTimes(big);
            var slicedEnds = EndTimes(sliced);

            big.Tick(60);
            for (int i = 0; i < 180; i++) sliced.Tick(1.0 / 3); // ×20 a 60 qps

            Assert.That(bigEnds, Is.EqualTo(new double[] { 5, 10, 20, 30 }).Within(1e-9));
            Assert.That(slicedEnds, Is.EqualTo(new double[] { 5, 10, 20, 30 }).Within(1e-9));
            Assert.That(big.Bonuses.Count, Is.EqualTo(0));
            Assert.That(big.Stats.Damage, Is.EqualTo(1).Within(1e-12), "o dano volta ao normal");
        }

        [Test]
        public void TheComboEnd_GrantsAtTheExactTimeTheWindowCloses()
        {
            var sim = Sim();
            double grantedAt = double.NaN;
            sim.ComboBonusesGranted += (_, __) => grantedAt = sim.Elapsed;
            sim.Tick(0.5);
            for (int i = 0; i < 10; i++) sim.Tap();

            sim.Tick(7);

            Assert.That(grantedAt, Is.EqualTo(1.5).Within(1e-9));
            Assert.That(sim.Bonuses.Count, Is.EqualTo(0), "1,5 + 5 s < 7,5 s: já acabou");
        }

        // ── Pausa, app fechado, save e descanso ─────────────────────────────────────────────────────────────────────

        /// <summary>D-214: ir para o fundo termina o combo na hora, com os bônus.</summary>
        [Test]
        public void EndCombo_GrantsNow_AndIsHarmlessWithoutCombo()
        {
            var sim = Sim();
            Assert.That(sim.EndCombo(), Is.False);

            for (int i = 0; i < 31; i++) sim.Tap();
            Assert.That(sim.EndCombo(), Is.True);

            Assert.That(sim.Combo, Is.EqualTo(0));
            Assert.That(TierList(sim), Is.EqualTo(new double[] { 10, 30 }));
        }

        [Test]
        public void ClosedApp_TheTimeRuns_ShorterOrLongerThanTheBonus()
        {
            var sim = Sim();
            sim.GrantComboBonuses(104);

            sim.ApplyOffline(12);
            Assert.That(TierList(sim), Is.EqualTo(new double[] { 50, 100 }), "o ×10 (5 s) e o ×30 (10 s) acabaram fora");
            Assert.That(sim.Bonuses.All.Single(b => b.Tier == 100).SecondsLeft, Is.EqualTo(18).Within(1e-9));
            Assert.That(sim.Stats.Damage, Is.EqualTo(1.5).Within(1e-12));

            sim.ApplyOffline(3600);
            Assert.That(sim.Bonuses.Count, Is.EqualTo(0));
        }

        [Test]
        public void ClosedApp_WithTheClockBack_KeepsTheTime()
        {
            var sim = Sim();
            sim.GrantComboBonuses(10);
            sim.ApplyOffline(-3600);
            Assert.That(sim.Bonuses.All.Single().SecondsLeft, Is.EqualTo(5).Within(1e-9));
        }

        /// <summary>D-213: o descanso começa uma run nova, sem os bônus.</summary>
        [Test]
        public void Rest_TheNewRunHasNoBonuses()
        {
            var sim = Sim();
            sim.GrantComboBonuses(104);
            Assert.That(sim.Rest(force: true), Is.Not.Null);

            var next = sim.Permanents.NewRun(WithIntern());

            Assert.That(next.Bonuses.Count, Is.EqualTo(0));
        }

        [Test]
        public void Save_KeepsTheBonusesAndTheTimeLeft_AndAVersion5SaveOpensWithout()
        {
            string dir = Path.Combine(Path.GetTempPath(), "MinersIdleBonusSave_" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new FileSaveStore(dir);
                var sim = Sim();
                sim.GrantComboBonuses(50);
                sim.Tick(3);
                sim.GrantComboBonuses(10);
                new SaveService(store).Save(sim.State);

                var loaded = new SaveService(store).Load(WithIntern(), out var status);
                Assert.That(status, Is.EqualTo(SaveLoadStatus.Loaded));
                var expected = sim.Bonuses.All.Select(b => (b.Kind, b.Tier, b.Value, b.SecondsLeft, b.Duration)).ToArray();
                var actual = loaded.Bonuses.All.Select(b => (b.Kind, b.Tier, b.Value, b.SecondsLeft, b.Duration)).ToArray();
                Assert.That(actual, Is.EqualTo(expected));

                var reopened = Sim(state: loaded);
                Assert.That(reopened.Stats.Damage, Is.EqualTo(sim.Stats.Damage).Within(1e-12), "o dano abre com os bônus");
                Assert.That(reopened.Bonuses.GuaranteedCritical, Is.True);

                File.WriteAllText(store.FilePath, "{ \"version\": 5, \"shards\": 3.0, \"bonuses\": [ { \"kind\": \"DamagePercent\", " +
                                                  "\"source\": \"Combo\", \"tier\": 10, \"value\": 0.1, \"duration\": 5, \"secondsLeft\": 4 } ] }");
                var v5 = new SaveService(store).Load(WithIntern(), out _);
                Assert.That(v5.Bonuses.Count, Is.EqualTo(0), "o v5 não tinha bônus");
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
            }
        }

        [Test]
        public void Restore_DropsExpiredBrokenAndUnknownBonuses()
        {
            var state = GameState.Restore(0, 0, 0, _ => 0, WithIntern(), bonuses: new[]
            {
                new ActiveBonus(BonusKind.DamagePercent, 0.1, 5, 4, BonusSource.Combo, 10),
                new ActiveBonus(BonusKind.DamagePercent, 0.1, 5, 0, BonusSource.Combo, 10),
                new ActiveBonus(BonusKind.DamagePercent, double.NaN, 5, 4, BonusSource.Combo, 10),
                new ActiveBonus((BonusKind)99, 0.1, 5, 4, BonusSource.Combo, 10),
                new ActiveBonus(BonusKind.GuaranteedCritical, 0, 1, 15, BonusSource.Combo, 50),
            });

            Assert.That(state.Bonuses.Count, Is.EqualTo(2));
            Assert.That(state.Bonuses.All[1].Duration, Is.EqualTo(15), "a duração nunca menor que o que falta");
        }

        // ── Regras e dados ──────────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void StrikeRules_SortsTheTiers_AndRefusesARepeatedOne()
        {
            var strike = new StrikeRules(0, 2, 0.1, 1, comboBonuses: Tiers.Reverse());
            Assert.That(strike.ComboBonuses.Select(r => r.Combo), Is.EqualTo(new[] { 10, 30, 50, 100 }));
            Assert.That(StrikeRules.NoCritical().ComboBonuses, Is.Empty);
            Assert.Throws<ArgumentException>(() => new StrikeRules(0, 2, 0.1, 1, comboBonuses: new[] { Tiers[0], Tiers[0] }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ComboBonusRule(10, BonusKind.DamagePercent, 0.1, 0));
        }

        [Test]
        public void GddV7Balance_HasTheFourTiers()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV7Path);
            Assert.That(asset, Is.Not.Null);
            var tiers = asset.ToStrike().ComboBonuses
                             .Select(r => (r.Combo, r.Kind, r.Value, r.Seconds)).ToArray();
            Assert.That(tiers, Is.EqualTo(Tiers.Select(r => (r.Combo, r.Kind, r.Value, r.Seconds)).ToArray()),
                        "rode o setup do projeto (Tools → Miners Idle → Projeto → Aplicar configuração do projeto)");
        }

        [Test]
        public void FormatPercent_OneDecimalAtMost()
        {
            Assert.That(NumberFormatter.FormatPercent(0.1), Is.EqualTo("10%"));
            Assert.That(NumberFormatter.FormatPercent(0.25), Is.EqualTo("25%"));
            Assert.That(NumberFormatter.FormatPercent(0.125), Is.EqualTo("12,5%"));
            Assert.That(NumberFormatter.FormatPercent(0), Is.EqualTo("0%"));
            Assert.That(NumberFormatter.FormatPercent(double.NaN), Is.EqualTo("0%"));
        }
    }
}
