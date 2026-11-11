using System;
using System.IO;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Persistence;
using NUnit.Framework;
using UnityEditor;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9b, E5: o boost do AD (GDD v7 §8; D-187, D-188, D-218, D-229, D-232, D-235). Assistir o anúncio dá velocidade ×2,5
    /// por 30 s só ao anão, depois do piso; com um ativo, não dá outro; com a carta Rápido, multiplica (÷5). O tempo corre
    /// com o app fechado e não muda o ganho offline (o estagiário não ganha); vai para o save; o descanso o apaga.
    /// </summary>
    public class AdBoostTests
    {
        const string GddV7Path = "Assets/_Project/Data/Balance_GDD_v7.asset";

        static MiningRules Rules(double hitInterval = 1, double health = 1e9) =>
            new MiningRules(
                initialCrystals: 0, firstCrystalDistance: 3, maxCrystals: 12, spawnInterval: 1e9,
                minCrystalSpacing: 2, spawnDistance: 12, crystalHealth: health, crystalReward: 1, damage: 1,
                hitInterval: hitInterval, walkSpeed: 4, miningRange: 1);

        static readonly RareCardRule[] Cards =
        {
            new RareCardRule(RareCard.Damage, 2, 60),
            new RareCardRule(RareCard.Speed, 2, 60),
        };

        static StrikeRules Strike(AdRules ad = null) =>
            new StrikeRules(0, 2, 0.1, 1, rare: new RareRules(0, 0, 30, Cards), ad: ad ?? new AdRules(2.5, 30));

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

        static MiningSimulation Sim(MiningRules rules = null, StrikeRules strike = null, GameState state = null) =>
            new MiningSimulation(rules ?? Rules(), new SeededRandom(7), state, WithIntern(), strike ?? Strike(),
                                 new Permanents(V7Permanents(), new PermanentState()), new SeededRandom(11));

        [Test]
        public void TheBoost_DividesTheDwarfIntervalByTwoAndAHalf_NotTheIntern()
        {
            var sim = Sim();
            sim.HireIntern();
            double dwarf = sim.Stats.HitInterval, intern = sim.InternStats.HitInterval;
            int granted = 0, changed = 0;
            sim.AdBoostGranted += () => granted++;
            sim.BonusesChanged += () => changed++;

            Assert.That(sim.CanWatchAd, Is.True);
            Assert.That(sim.GrantAdBoost(), Is.True);

            Assert.That(sim.Stats.HitInterval, Is.EqualTo(dwarf / 2.5).Within(1e-12));
            Assert.That(sim.InternStats.HitInterval, Is.EqualTo(intern).Within(1e-12), "só o anão (D-187)");
            Assert.That(sim.HasAdBoost, Is.True);
            Assert.That(sim.AdBoostSecondsLeft, Is.EqualTo(30));
            Assert.That(granted, Is.EqualTo(1));
            Assert.That(changed, Is.EqualTo(1), "a coluna fica sabendo");
        }

        [Test]
        public void TheBoost_WorksBelowTheFloor()
        {
            var sim = Sim(Rules(hitInterval: 0.1)); // já no piso (0,1 s)
            Assert.That(sim.Stats.HitInterval, Is.EqualTo(0.1).Within(1e-12));

            sim.GrantAdBoost();

            Assert.That(sim.Stats.HitInterval, Is.EqualTo(0.04).Within(1e-12), "o boost vem depois do piso (D-187)");
        }

        [Test]
        public void TheBoost_EndsAtThirtySeconds()
        {
            var sim = Sim();
            double plain = sim.Stats.HitInterval;
            sim.GrantAdBoost();

            sim.Tick(29.5);
            Assert.That(sim.HasAdBoost, Is.True);
            Assert.That(sim.AdBoostSecondsLeft, Is.EqualTo(0.5).Within(1e-9));

            sim.Tick(0.5);
            Assert.That(sim.HasAdBoost, Is.False);
            Assert.That(sim.Stats.HitInterval, Is.EqualTo(plain).Within(1e-12), "acabou: volta");
            Assert.That(sim.CanWatchAd, Is.True, "e o AD fica livre de novo");
        }

        [Test]
        public void WithABoostActive_TheAdIsNotFree_AndNothingChanges()
        {
            var sim = Sim();
            sim.GrantAdBoost();
            sim.Tick(10);
            int granted = 0;
            sim.AdBoostGranted += () => granted++;

            Assert.That(sim.CanWatchAd, Is.False, "um por vez (D-188)");
            Assert.That(sim.GrantAdBoost(), Is.False);
            Assert.That(sim.AdBoostSecondsLeft, Is.EqualTo(20).Within(1e-9), "não renova");
            Assert.That(granted, Is.Zero);
        }

        [Test]
        public void TheBoostAndTheFastCard_Multiply()
        {
            var sim = Sim();
            double plain = sim.Stats.HitInterval;
            sim.GrantRareCard(RareCard.Speed);
            sim.GrantAdBoost();

            Assert.That(sim.Stats.HitInterval, Is.EqualTo(plain / 5).Within(1e-12), "÷2 × ÷2,5 (D-232)");
            Assert.That(sim.Bonuses.IntervalDivisor, Is.EqualTo(5).Within(1e-12));
        }

        [Test]
        public void Save_KeepsTheBoost()
        {
            string dir = Path.Combine(Path.GetTempPath(), "MinersIdleAdSave_" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new FileSaveStore(dir);
                var sim = Sim();
                sim.GrantAdBoost();
                sim.Tick(12);
                new SaveService(store).Save(sim.State);

                var loaded = new SaveService(store).Load(WithIntern(), out var status);
                Assert.That(status, Is.EqualTo(SaveLoadStatus.Loaded));
                var reopened = Sim(state: loaded);
                Assert.That(reopened.HasAdBoost, Is.True);
                Assert.That(reopened.AdBoostSecondsLeft, Is.EqualTo(18).Within(1e-9));
                Assert.That(reopened.Stats.HitInterval, Is.EqualTo(sim.Stats.HitInterval).Within(1e-12));
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
            }
        }

        /// <summary>D-235: o tempo corre com o app fechado; o ganho offline (do estagiário) não muda.</summary>
        [Test]
        public void ClosedApp_TheBoostRunsOut_AndTheOfflineGainIsTheSame()
        {
            OfflineReport Away(bool boost)
            {
                var sim = Sim(Rules(health: 1));
                sim.HireIntern();
                if (boost) sim.GrantAdBoost();
                return sim.ApplyOffline(60);
            }

            Assert.That(Away(false).Shards, Is.GreaterThan(0));
            Assert.That(Away(true).Shards, Is.EqualTo(Away(false).Shards).Within(1e-9));

            var sim2 = Sim();
            sim2.GrantAdBoost();
            sim2.ApplyOffline(45);
            Assert.That(sim2.HasAdBoost, Is.False);
        }

        [Test]
        public void Rest_TheNewRunHasNoBoost()
        {
            var sim = Sim();
            sim.GrantAdBoost();
            Assert.That(sim.Rest(force: true), Is.Not.Null);

            var next = sim.Permanents.NewRun(WithIntern());

            Assert.That(next.Bonuses.AdBoost.HasValue, Is.False);
            Assert.That(Sim(state: next).CanWatchAd, Is.True);
        }

        [Test]
        public void WithoutTheAdInTheBalance_TheAdIsNeverFree()
        {
            var sim = Sim(strike: Strike(AdRules.None()));
            Assert.That(sim.CanWatchAd, Is.False);
            Assert.That(sim.GrantAdBoost(), Is.False);
            Assert.That(new AdRules(1, 30).IsEnabled, Is.False, "×1 não acelera");
            Assert.That(new AdRules(2.5, 0).IsEnabled, Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => new AdRules(double.NaN, 30));
        }

        [Test]
        public void GddV7Balance_HasTheAdBoost()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV7Path);
            Assert.That(asset, Is.Not.Null);
            var ad = asset.ToStrike().Ad;
            const string hint = "rode o setup do projeto (Tools → Miners Idle → Projeto → Aplicar configuração do projeto)";
            Assert.That(ad.SpeedDivisor, Is.EqualTo(2.5), hint);
            Assert.That(ad.Seconds, Is.EqualTo(30), hint);
        }
    }
}
