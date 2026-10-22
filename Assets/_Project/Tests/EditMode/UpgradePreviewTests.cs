using System;
using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>Marco 4, E1: o que a loja mostra de cada melhoria (estado do botão e efeito "de → para").</summary>
    public class UpgradePreviewTests
    {
        static readonly UpgradeId[] AllLines = (UpgradeId[])Enum.GetValues(typeof(UpgradeId));

        static MiningRules Rules() =>
            new MiningRules(
                new Vec2(-7.5, -11), new Vec2(7.5, 10), new Vec2(0, -0.5),
                initialCrystals: 0, maxCrystals: 12, spawnInterval: 1e9, minCrystalSpacing: 2, minDistanceFromMiner: 1.5,
                crystalHealth: 10, crystalReward: 1, damage: 1, hitInterval: 1, walkSpeed: 4, miningRange: 1);

        /// <summary>A tabela de protótipo do plano do Marco 3.</summary>
        static EconomyRules Prototype() => new EconomyRules(
            pickaxe: new UpgradeDefinition(10, 4, 19, 2),
            speed: new UpgradeDefinition(15, 3, 10, 0.05),
            mining: new UpgradeDefinition(10, 1.2, 100, 0.01),
            doublePickaxe: new UpgradeDefinition(500, 1, 1, 2),
            boots: new UpgradeDefinition(10, 2, 10, 0.1),
            minHitInterval: 0.5);

        static MiningSimulation Sim(EconomyRules economy = null) =>
            new MiningSimulation(Rules(), new SeededRandom(1), economy: economy ?? Prototype());

        [Test]
        public void FreshGame_ShowsLevelZero_FirstCost_AndTheEffectOfTheFirstPurchase()
        {
            var sim = Sim();

            var pickaxe = sim.Preview(UpgradeId.Pickaxe);
            Assert.That(pickaxe.Level, Is.EqualTo(0));
            Assert.That(pickaxe.MaxLevel, Is.EqualTo(19));
            Assert.That(pickaxe.NextCost, Is.EqualTo(10));
            Assert.That(pickaxe.State, Is.EqualTo(UpgradeButtonState.NotEnoughShards));
            Assert.That(pickaxe.CurrentValue, Is.EqualTo(1));
            Assert.That(pickaxe.NextValue, Is.EqualTo(2));

            var speed = sim.Preview(UpgradeId.Speed);
            Assert.That(speed.CurrentValue, Is.EqualTo(1));
            Assert.That(speed.NextValue, Is.EqualTo(0.95).Within(1e-12));

            var mining = sim.Preview(UpgradeId.Mining);
            Assert.That(mining.CurrentValue, Is.EqualTo(1));
            Assert.That(mining.NextValue, Is.EqualTo(1.01).Within(1e-12));

            var boots = sim.Preview(UpgradeId.Boots);
            Assert.That(boots.CurrentValue, Is.EqualTo(4));
            Assert.That(boots.NextValue, Is.EqualTo(4.4).Within(1e-12));

            var dupla = sim.Preview(UpgradeId.DoublePickaxe);
            Assert.That(dupla.IsSinglePurchase, Is.True);
            Assert.That(dupla.CurrentValue, Is.EqualTo(1));
            Assert.That(dupla.NextValue, Is.EqualTo(2));
        }

        [Test]
        public void ButtonState_FollowsTheBalance_WithTheSameToleranceAsThePurchase()
        {
            var sim = Sim();
            sim.AddShards(10 - 1e-12);
            Assert.That(sim.Preview(UpgradeId.Pickaxe).State, Is.EqualTo(UpgradeButtonState.Purchasable),
                        "se a compra aceita, a loja não pode mostrar como sem lascas");
            Assert.That(sim.Preview(UpgradeId.Speed).State, Is.EqualTo(UpgradeButtonState.NotEnoughShards));
        }

        [Test]
        public void SinglePurchase_AfterBuying_IsMaxed_AndPromisesNothingMore()
        {
            var sim = Sim();
            sim.AddShards(500);
            sim.Purchase(UpgradeId.DoublePickaxe);

            var dupla = sim.Preview(UpgradeId.DoublePickaxe);
            Assert.That(dupla.State, Is.EqualTo(UpgradeButtonState.Maxed));
            Assert.That(dupla.NextCost, Is.Null);
            Assert.That(dupla.NextValue, Is.EqualTo(dupla.CurrentValue));
            Assert.That(dupla.CurrentValue, Is.EqualTo(2));
        }

        [Test]
        public void SpeedNearTheMinimum_PromisesTheClampedInterval()
        {
            var economy = new EconomyRules(
                new UpgradeDefinition(0, 1, 0, 0), new UpgradeDefinition(1, 1, 30, 0.3),
                new UpgradeDefinition(0, 1, 0, 0), new UpgradeDefinition(0, 1, 0, 0), new UpgradeDefinition(0, 1, 0, 0),
                minHitInterval: 0.5);
            var sim = Sim(economy);
            sim.AddShards(10);
            sim.Purchase(UpgradeId.Speed); // 1,0 → 0,7

            var speed = sim.Preview(UpgradeId.Speed);
            Assert.That(speed.CurrentValue, Is.EqualTo(0.7).Within(1e-12));
            Assert.That(speed.NextValue, Is.EqualTo(0.5), "0,7 − 0,3 = 0,4 fica preso no mínimo");
        }

        [Test]
        public void DisabledLine_IsShownAsMaxed()
        {
            var economy = new EconomyRules(
                new UpgradeDefinition(50, 4, 19, 2), new UpgradeDefinition(200, 3, 45, 0.1),
                new UpgradeDefinition(50, 1.2, 100, 0.01), new UpgradeDefinition(5000, 1, 1, 2),
                new UpgradeDefinition(0, 1, 0, 0), minHitInterval: 0.5);
            var boots = Sim(economy).Preview(UpgradeId.Boots);

            Assert.That(boots.IsEnabled, Is.False);
            Assert.That(boots.State, Is.EqualTo(UpgradeButtonState.Maxed));
        }

        /// <summary>A promessa da loja é cumprida: o "para" da prévia é exatamente o atributo depois da compra, em todos os níveis.</summary>
        [Test]
        public void WhatThePreviewPromises_IsWhatThePurchaseDelivers()
        {
            foreach (var id in AllLines)
            {
                var sim = Sim();
                sim.AddShards(1e12);
                for (int step = 0; step < 25; step++)
                {
                    var before = sim.Preview(id);
                    var result = sim.Purchase(id);
                    var after = sim.Preview(id);

                    if (before.State == UpgradeButtonState.Maxed)
                    {
                        Assert.That(result, Is.EqualTo(PurchaseResult.MaxLevel), $"{id} nível {before.Level}");
                        break;
                    }

                    Assert.That(result, Is.EqualTo(PurchaseResult.Purchased), $"{id} nível {before.Level}");
                    Assert.That(after.Level, Is.EqualTo(before.Level + 1));
                    Assert.That(after.CurrentValue, Is.EqualTo(before.NextValue).Within(1e-12), $"{id} nível {before.Level}");
                }
            }
        }
    }
}
