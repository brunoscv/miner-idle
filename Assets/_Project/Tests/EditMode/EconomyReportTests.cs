using MinersIdle.Core;
using MinersIdle.Game;
using NUnit.Framework;
using UnityEditor;

namespace MinersIdle.Tests
{
    /// <summary>Relatório de economia (Marco 3, E4) e a meta do protótipo: 1ª compra em menos de 3 min (D-35).</summary>
    public class EconomyReportTests
    {
        const string PrototypePath = "Assets/_Project/Data/Balance_Prototipo.asset";
        const string GddPath = "Assets/_Project/Data/Balance_GDD_v1.asset";
        static readonly double[] Checkpoints = { 300, 600 };

        static MiningRulesAsset Load(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(path);
            Assert.That(asset, Is.Not.Null, path + " não existe (rode o setup do projeto)");
            return asset;
        }

        [Test]
        public void SameSeed_GivesTheSameReport()
        {
            var asset = Load(PrototypePath);
            var a = EconomyReport.Run(asset.ToRules(), asset.ToEconomy(), 9, 600, Checkpoints);
            var b = EconomyReport.Run(asset.ToRules(), asset.ToEconomy(), 9, 600, Checkpoints);

            Assert.That(b.FirstPurchaseAt, Is.EqualTo(a.FirstPurchaseAt));
            Assert.That(b.Purchases, Is.EqualTo(a.Purchases));
            Assert.That(b.Earned, Is.EqualTo(a.Earned));
            Assert.That(b.ShardsPerMinute, Is.EqualTo(a.ShardsPerMinute));
            Assert.That(b.Levels, Is.EqualTo(a.Levels));
        }

        [Test]
        public void WithoutUpgrades_NothingIsBought_AndTheRateIsThePlainLoop()
        {
            var asset = Load(PrototypePath);
            var result = EconomyReport.Run(asset.ToRules(), EconomyRules.None(asset.ToRules().HitInterval), 1, 600, Checkpoints);

            Assert.That(result.FirstPurchaseAt, Is.Null);
            Assert.That(result.Purchases, Is.EqualTo(0));
            Assert.That(result.Spent, Is.EqualTo(0));
            Assert.That(result.ShardsPerMinute[1], Is.InRange(3, 7), "o loop do M2 rende ~5 lascas/min");
        }

        [Test]
        public void Accounting_EarnedIsBalancePlusSpent_AndUpgradesSpeedUpTheRate()
        {
            var asset = Load(PrototypePath);
            var plain = EconomyReport.Run(asset.ToRules(), EconomyRules.None(asset.ToRules().HitInterval), 1, 600, Checkpoints);
            var upgraded = EconomyReport.Run(asset.ToRules(), asset.ToEconomy(), 1, 600, Checkpoints);

            Assert.That(upgraded.Spent, Is.GreaterThan(0));
            Assert.That(upgraded.Earned, Is.GreaterThanOrEqualTo(upgraded.Spent));
            Assert.That(upgraded.ShardsPerMinute[1], Is.GreaterThan(plain.ShardsPerMinute[1]), "comprar faz render mais aos 10 min");
        }

        /// <summary>A meta de D-35. Se falhar, os valores do asset de protótipo precisam de ajuste (com decisão registrada).</summary>
        [Test]
        public void Prototype_FirstPurchaseComesInUnderThreeMinutes([Values(1UL, 2UL, 3UL)] ulong seed)
        {
            var asset = Load(PrototypePath);
            var result = EconomyReport.Run(asset.ToRules(), asset.ToEconomy(), seed, 600, Checkpoints);

            Assert.That(result.FirstPurchaseAt, Is.Not.Null);
            Assert.That(result.FirstPurchaseAt.Value, Is.LessThan(180));
        }

        [Test]
        public void GddV1_IsMuchSlowerToStart()
        {
            var prototype = Load(PrototypePath);
            var gdd = Load(GddPath);
            var fast = EconomyReport.Run(prototype.ToRules(), prototype.ToEconomy(), 1, 1800, Checkpoints);
            var slow = EconomyReport.Run(gdd.ToRules(), gdd.ToEconomy(), 1, 1800, Checkpoints);

            Assert.That(slow.Earned, Is.LessThan(fast.Earned));
            if (slow.FirstPurchaseAt.HasValue)
                Assert.That(slow.FirstPurchaseAt.Value, Is.GreaterThan(fast.FirstPurchaseAt.Value));
        }
    }
}
