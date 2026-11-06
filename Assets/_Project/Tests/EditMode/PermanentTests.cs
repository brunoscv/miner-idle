using System;
using System.Linq;
using MinersIdle.Core;
using MinersIdle.Game;
using NUnit.Framework;
using UnityEditor;

namespace MinersIdle.Tests
{
    /// <summary>
    /// E1 do Marco 8b: as lascas vermelhas (D-138), o descanso (D-137, D-152) e as 5 permanentes do anão (D-158) no
    /// núcleo, e o <c>Balance_GDD_v7</c> com esses números. Os números estão escritos à mão, como no GDD.
    /// </summary>
    public class PermanentTests
    {
        const string GddV7Path = "Assets/_Project/Data/Balance_GDD_v7.asset";
        const string GddV5Path = "Assets/_Project/Data/Balance_GDD_v5.asset";

        static MiningRules Rules(int caves = 100) =>
            new MiningRules(
                initialCrystals: 0, firstCrystalDistance: 3, maxCrystals: 12, spawnInterval: 1e9, minCrystalSpacing: 2,
                spawnDistance: 12, crystalHealth: 1, crystalReward: 1.2, damage: 1, hitInterval: 1, walkSpeed: 4,
                miningRange: 1, caveCount: caves, caveHealthGrowth: 1.25, caveRewardGrowth: 1.25,
                pickaxeFormula: PickaxeFormula.CaveScaled, pickaxeCaveGrowth: 1.2);

        static EconomyRules V7() => new EconomyRules(
            pickaxe: new UpgradeDefinition(2, 2, 49, 1, CostFormula.Linear),
            speed: new UpgradeDefinition(50, 2, 19, 0.1),
            mining: new UpgradeDefinition(20, 5, 10, 1),
            doublePickaxe: new UpgradeDefinition(5000, 1, 0, 2),
            boots: new UpgradeDefinition(0, 1, 0, 0),
            minHitInterval: 0.1,
            letrado: new UpgradeDefinition(0, 1, 100, 1),
            critical: new UpgradeDefinition(50, 2, 10, 0.05));

        static UpgradeDefinition Line(int max, double effect) => new UpgradeDefinition(10, 2, max, effect, CostFormula.OffsetExponential);

        /// <summary>GDD v7 §3.1 e §4.2.</summary>
        static PermanentRules V7Permanents() => new PermanentRules(
            10, new RedShardTable(10, 10, 5, 50, 10, 1),
            Line(10, 10), Line(20, 0.1), Line(20, 0.5), Line(10, 0.05), Line(10, 1));

        static MiningSimulation Sim(PermanentState permanent = null, GameState state = null) =>
            new MiningSimulation(Rules(), new SeededRandom(7), state, V7(), new StrikeRules(0, 2, 0.33, 1, tapCritChance: 0.05),
                                 new Permanents(V7Permanents(), permanent ?? new PermanentState()));

        static PermanentState WithLevel(PermanentId id, int level, double redShards = 0) =>
            PermanentState.Restore(redShards, 1, p => p == id ? level : 0, V7Permanents());

        // ── Lascas vermelhas (D-138) ────────────────────────────────────────────────────────────────────────────

        [Test]
        public void RedShards_FollowTheTableOfTheGdd()
        {
            var table = V7Permanents().RedShards;

            Assert.That(table.At(9), Is.EqualTo(0), "do 1 ao 9, nada");
            Assert.That(table.At(10), Is.EqualTo(5));
            Assert.That(table.At(11), Is.EqualTo(1));
            Assert.That(table.At(40), Is.EqualTo(5));
            Assert.That(table.At(50), Is.EqualTo(10));
            Assert.That(table.At(100), Is.EqualTo(10));
            Assert.That(table.UpTo(9), Is.EqualTo(0));
            Assert.That(table.UpTo(20), Is.EqualTo(19));
            Assert.That(table.UpTo(25), Is.EqualTo(24));
            Assert.That(table.UpTo(50), Is.EqualTo(66));
            Assert.That(table.UpTo(100), Is.EqualTo(161), "uma run do 1 ao 100");
        }

        [Test]
        public void Rest_OnlyFromLevelTen_AndPendingFollowsTheLevel()
        {
            var sim = Sim();
            sim.SkipToCave(9);
            Assert.That(sim.CanRest, Is.False, "D-152: o Descansar aparece a partir do 10");
            Assert.That(sim.PendingRedShards, Is.EqualTo(0));
            Assert.That(sim.Rest(), Is.Null);
            Assert.That(sim.NextLevelRedShards, Is.EqualTo(5), "o 10 daria 5");

            sim.SkipToNextCave();
            Assert.That(sim.CanRest, Is.True);
            Assert.That(sim.PendingRedShards, Is.EqualTo(5));
            sim.SkipToNextCave();
            Assert.That(sim.PendingRedShards, Is.EqualTo(6));
            Assert.That(sim.NextLevelRedShards, Is.EqualTo(1));
        }

        [Test]
        public void Rest_TakesThePendingHome_AndTheDayAdvances()
        {
            var permanent = new PermanentState();
            var sim = Sim(permanent);
            sim.AddShards(1000);
            sim.Purchase(UpgradeId.Speed);
            sim.SkipToCave(25);

            var summary = sim.Rest();

            Assert.That(summary, Is.Not.Null);
            Assert.That(summary.Value.Day, Is.EqualTo(1), "o dia que terminou");
            Assert.That(summary.Value.Level, Is.EqualTo(25));
            Assert.That(summary.Value.RedShardsGained, Is.EqualTo(24));
            Assert.That(summary.Value.RedShardsBank, Is.EqualTo(24));
            Assert.That(permanent.RedShards, Is.EqualTo(24));
            Assert.That(permanent.Day, Is.EqualTo(2));
            Assert.That(sim.CanRest, Is.False, "a run acabou");
            Assert.That(sim.Rest(force: true), Is.Null, "não descansa duas vezes");

            var next = sim.Permanents.NewRun(sim.Economy);
            Assert.That(next.Cave, Is.EqualTo(1), "a run nova começa do nível 1");
            Assert.That(next.Shards, Is.EqualTo(0));
            Assert.That(next.LevelOf(UpgradeId.Speed), Is.EqualTo(0), "as melhorias base zeram");
        }

        [Test]
        public void ForcedRest_WorksAtAnyLevel_WithNothingToTake()
        {
            var permanent = new PermanentState();
            var sim = Sim(permanent);

            var summary = sim.Rest(force: true);

            Assert.That(summary?.RedShardsGained, Is.EqualTo(0));
            Assert.That(permanent.Day, Is.EqualTo(2));
        }

        [Test]
        public void WithoutPermanentRules_ThereIsNoRest()
        {
            var sim = new MiningSimulation(Rules(), new SeededRandom(7), economy: V7());
            sim.SkipToCave(50);
            Assert.That(sim.CanRest, Is.False, "o jogo até o GDD v5");
            Assert.That(sim.PendingRedShards, Is.EqualTo(0));
        }

        // ── Custo e compra ──────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void PermanentCost_IsTenPlusTwoToTheNMinusOne()
        {
            var memory = V7Permanents().Get(PermanentId.Memory);

            Assert.That(Enumerable.Range(0, 10).Select(l => memory.CostAt(l)),
                        Is.EqualTo(new double[] { 11, 12, 14, 18, 26, 42, 74, 138, 266, 522 }));
            Assert.That(Enumerable.Range(0, 10).Sum(l => memory.CostAt(l)), Is.EqualTo(1123));
            var blood = V7Permanents().Get(PermanentId.MountainBlood);
            Assert.That(Enumerable.Range(0, 20).Sum(l => blood.CostAt(l)), Is.EqualTo(10 * 20 + Math.Pow(2, 20) - 1), "~1,05 mi");
        }

        [Test]
        public void PurchasePermanent_SpendsRedShards_UpToTheLimit()
        {
            var permanent = PermanentState.Restore(11, 1, _ => 0, V7Permanents());
            var sim = Sim(permanent);
            int changes = 0;
            sim.PermanentsChanged += () => changes++;

            Assert.That(sim.PurchasePermanent(PermanentId.MountainBlood), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(permanent.RedShards, Is.EqualTo(0));
            Assert.That(permanent.LevelOf(PermanentId.MountainBlood), Is.EqualTo(1));
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(sim.PurchasePermanent(PermanentId.MountainBlood), Is.EqualTo(PurchaseResult.NotEnoughShards));

            var maxed = Sim(WithLevel(PermanentId.Memory, 10, redShards: 10000));
            Assert.That(maxed.PurchasePermanent(PermanentId.Memory), Is.EqualTo(PurchaseResult.MaxLevel));
            Assert.That(maxed.Permanents.NextCost(PermanentId.Memory), Is.Null);
        }

        // ── Efeitos (D-158) ─────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void MountainBlood_MultipliesTheFinalDamage_RightAway()
        {
            var sim = Sim(PermanentState.Restore(11, 1, _ => 0, V7Permanents()));
            double before = sim.Stats.Damage; // 1 + 1,2^0 = 2

            sim.PurchasePermanent(PermanentId.MountainBlood);

            Assert.That(sim.Stats.Damage, Is.EqualTo(before * 1.1).Within(1e-9), "+10% na hora (D-155)");
            Assert.That(Sim(WithLevel(PermanentId.MountainBlood, 20)).Stats.Damage, Is.EqualTo(2 * 3).Within(1e-9), "20 níveis: ×3");
        }

        [Test]
        public void BurningHeart_MultipliesTheShards_UpToAThousandPercent()
        {
            Assert.That(Sim(WithLevel(PermanentId.BurningHeart, 1)).Stats.RewardMultiplier, Is.EqualTo(1.5).Within(1e-9));
            Assert.That(Sim(WithLevel(PermanentId.BurningHeart, 20)).Stats.RewardMultiplier, Is.EqualTo(11).Within(1e-9),
                        "o teto de +1000%");

            var sim = Sim(WithLevel(PermanentId.BurningHeart, 2));
            var crystal = sim.SpawnCrystalAt(5);
            sim.Tick(10);
            Assert.That(sim.State.Shards, Is.EqualTo(crystal.Reward * 2).Within(1e-9), "lascas × (1 + 0,5 × 2)");
        }

        [Test]
        public void NervousFingers_AddsToTheCriticalChance()
        {
            var stats = Sim(WithLevel(PermanentId.NervousFingers, 3)).Stats;
            Assert.That(stats.CritBonus, Is.EqualTo(0.15).Within(1e-9), "+5% por nível, no golpe e no toque");
        }

        [Test]
        public void Memory_LowersTheMapLock_AndAtTenRemovesIt()
        {
            Assert.That(Sim().MapLock, Is.EqualTo(100));
            var sim = Sim(WithLevel(PermanentId.Memory, 3));
            Assert.That(sim.MapLock, Is.EqualTo(70));
            Assert.That(sim.Preview(UpgradeId.Letrado).ProgressGoal, Is.EqualTo(70), "a loja mostra a trava nova");

            var free = Sim(WithLevel(PermanentId.Memory, 10));
            Assert.That(free.MapLock, Is.EqualTo(0));
            Assert.That(free.Purchase(UpgradeId.Letrado), Is.EqualTo(PurchaseResult.Purchased), "sem quebrar nenhum cristal");
        }

        [Test]
        public void DwarfSoul_StartsTheRunWithThePickaxe_AsIfBought()
        {
            var sim = Sim(WithLevel(PermanentId.DwarfSoul, 3));
            var run = sim.Permanents.NewRun(sim.Economy);
            Assert.That(run.LevelOf(UpgradeId.Pickaxe), Is.EqualTo(3));

            var next = Sim(WithLevel(PermanentId.DwarfSoul, 3), run);
            Assert.That(next.Stats.Damage, Is.EqualTo(1 + 3 + 1).Within(1e-9), "Dano Base 4 + 1,2^0");
            Assert.That(next.Preview(UpgradeId.Pickaxe).NextCost, Is.EqualTo(8), "2 × Dano Base 4 (D-133)");
        }

        [Test]
        public void DebugShortcuts_AddRedShards_GrantLevels_AndReset()
        {
            var permanent = new PermanentState();
            var sim = Sim(permanent);

            sim.AddRedShards(100);
            Assert.That(permanent.RedShards, Is.EqualTo(100));
            Assert.That(sim.GrantPermanentLevels(), Is.True);
            // As 5 do anão sobem; as do estagiário (M7) estão desligadas nestas regras e ficam no zero.
            foreach (var id in new[] { PermanentId.Memory, PermanentId.MountainBlood, PermanentId.BurningHeart,
                                       PermanentId.NervousFingers, PermanentId.DwarfSoul })
                Assert.That(permanent.LevelOf(id), Is.EqualTo(1), id.ToString());
            Assert.That(permanent.LevelOf(PermanentId.InternTraining), Is.EqualTo(0), "linha desligada");
            Assert.That(sim.Stats.Damage, Is.EqualTo(2 * 1.1).Within(1e-9), "o Sangue vale na hora");

            sim.Rest(force: true);
            Sim(permanent).ResetPermanents();
            Assert.That(permanent.RedShards, Is.EqualTo(0));
            Assert.That(permanent.Day, Is.EqualTo(1));
            Assert.That(permanent.LevelOf(PermanentId.Memory), Is.EqualTo(0));
        }

        // ── A prévia da aba das permanentes (E4) ────────────────────────────────────────────────────────────────

        [Test]
        public void PermanentPreview_ShowsLevelCostEffect_AndTheButtonState()
        {
            var sim = Sim(WithLevel(PermanentId.MountainBlood, 5, redShards: 41));

            var blood = sim.PreviewPermanent(PermanentId.MountainBlood);
            Assert.That(blood.Level, Is.EqualTo(5));
            Assert.That(blood.MaxLevel, Is.EqualTo(20));
            Assert.That(blood.NextCost, Is.EqualTo(10 + 32), "a 6ª compra: 10 + 2^5");
            Assert.That(blood.State, Is.EqualTo(UpgradeButtonState.NotEnoughShards), "41 não paga 42");
            Assert.That(blood.CurrentValue, Is.EqualTo(0.5).Within(1e-9), "+50%");
            Assert.That(blood.NextValue, Is.EqualTo(0.6).Within(1e-9), "+60%");

            var memory = sim.PreviewPermanent(PermanentId.Memory);
            Assert.That(memory.State, Is.EqualTo(UpgradeButtonState.Purchasable), "41 paga 11");
            Assert.That(memory.CurrentValue, Is.EqualTo(0));
            Assert.That(memory.NextValue, Is.EqualTo(10), "−10 cristais na trava");
        }

        [Test]
        public void PermanentPreview_AtTheLimit_IsMaxed_WithoutCost()
        {
            var sim = Sim(WithLevel(PermanentId.Memory, 10, redShards: 1e6));

            var memory = sim.PreviewPermanent(PermanentId.Memory);
            Assert.That(memory.IsMaxed, Is.True);
            Assert.That(memory.NextCost, Is.Null);
            Assert.That(memory.CurrentValue, Is.EqualTo(100));
            Assert.That(memory.NextValue, Is.EqualTo(100));

            var none = new MiningSimulation(Rules(), new SeededRandom(7), null, V7());
            Assert.That(none.PreviewPermanent(PermanentId.Memory).IsEnabled, Is.False, "sem permanentes, a linha some");
        }

        [Test]
        public void Restore_FixesWhatIsOutOfPlace()
        {
            var state = PermanentState.Restore(-5, 0, id => id == PermanentId.Memory ? 99 : -1, V7Permanents());
            Assert.That(state.RedShards, Is.EqualTo(0));
            Assert.That(state.Day, Is.EqualTo(1));
            Assert.That(state.LevelOf(PermanentId.Memory), Is.EqualTo(10), "no limite da linha");
            Assert.That(state.LevelOf(PermanentId.DwarfSoul), Is.EqualTo(0));
        }

        // ── O asset ─────────────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void BalanceGddV7Asset_HasTheRestAndThePermanentsOfTheGdd()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV7Path);
            Assert.That(asset, Is.Not.Null, "o setup cria o Balance_GDD_v7");
            var rules = asset.ToPermanents();

            Assert.That(rules.RestLevel, Is.EqualTo(10));
            Assert.That(rules.RedShards.UpTo(100), Is.EqualTo(161));
            (PermanentId id, int max, double effect)[] lines =
            {
                (PermanentId.Memory, 10, 10), (PermanentId.MountainBlood, 20, 0.1), (PermanentId.BurningHeart, 20, 0.5),
                (PermanentId.NervousFingers, 10, 0.05), (PermanentId.DwarfSoul, 10, 1),
            };
            foreach (var (id, max, effect) in lines)
            {
                var line = rules.Get(id);
                Assert.That(line.MaxLevel, Is.EqualTo(max), id.ToString());
                Assert.That(line.Effect, Is.EqualTo(effect).Within(1e-9), id.ToString());
                Assert.That(line.CostAt(0), Is.EqualTo(11), id + ": 10 + 2^0");
                Assert.That(line.CostAt(3), Is.EqualTo(18), id + ": 10 + 2^3");
            }

            var v5 = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV5Path);
            Assert.That(v5.ToPermanents().IsRestEnabled, Is.False, "o GDD v5 não tem descanso");
        }
    }
}
