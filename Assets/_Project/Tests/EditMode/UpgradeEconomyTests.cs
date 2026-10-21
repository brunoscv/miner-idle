using System;
using System.Linq;
using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>
    /// Melhorias do M3 (E1): custo e efeito de todos os níveis de todas as linhas, limites e compra (REQ-ECO-02…08,
    /// D-35, D-38). Os valores aqui são os da tabela do plano do Marco 3, escritos à mão para não depender do asset.
    /// </summary>
    public class UpgradeEconomyTests
    {
        static readonly UpgradeId[] AllLines = (UpgradeId[])Enum.GetValues(typeof(UpgradeId));

        static MiningRules BaseRules(double damage = 1, double hitInterval = 1) =>
            new MiningRules(
                new Vec2(-7.5, -11), new Vec2(7.5, 10), new Vec2(0, -0.5),
                initialCrystals: 0, maxCrystals: 12, spawnInterval: 1e9, minCrystalSpacing: 2, minDistanceFromMiner: 1.5,
                crystalHealth: 10, crystalReward: 1, damage: damage, hitInterval: hitInterval, walkSpeed: 4, miningRange: 1);

        static EconomyRules Prototype() => new EconomyRules(
            pickaxe: new UpgradeDefinition(10, 4, 19, 2),
            speed: new UpgradeDefinition(15, 3, 10, 0.05),
            mining: new UpgradeDefinition(10, 1.2, 100, 0.01),
            doublePickaxe: new UpgradeDefinition(500, 1, 1, 2),
            boots: new UpgradeDefinition(10, 2, 10, 0.1),
            minHitInterval: 0.5);

        static EconomyRules GddV1() => new EconomyRules(
            pickaxe: new UpgradeDefinition(50, 4, 19, 2),
            speed: new UpgradeDefinition(200, 3, 45, 0.1),
            mining: new UpgradeDefinition(50, 1.2, 100, 0.01),
            doublePickaxe: new UpgradeDefinition(5000, 1, 1, 2),
            boots: new UpgradeDefinition(0, 1, 0, 0),
            minHitInterval: 0.5);

        /// <summary>Lascas entram pelo mesmo caminho do jogo (o atalho do debug), sem expor o estado.</summary>
        static GameState StateWith(double shards)
        {
            var state = new GameState();
            if (shards > 0) new MiningSimulation(BaseRules(), new SeededRandom(1), state).AddShards(shards);
            return state;
        }

        static double SumOfCosts(UpgradeDefinition line)
        {
            double sum = 0;
            for (int level = 0; level < line.MaxLevel; level++) sum += line.CostAt(level);
            return sum;
        }

        [Test]
        public void Cost_OfEveryLevel_IsBaseTimesGrowthToTheLevel([Values] bool gdd)
        {
            var economy = gdd ? GddV1() : Prototype();
            foreach (var id in AllLines)
            {
                var line = economy.Get(id);
                for (int level = 0; level < line.MaxLevel; level++)
                    Assert.That(line.CostAt(level), Is.EqualTo(line.BaseCost * Math.Pow(line.CostGrowth, level)), $"{id} nível {level}");
                if (line.IsEnabled)
                    Assert.That(line.CostAt(0), Is.EqualTo(line.BaseCost), "a 1ª compra custa o custo inicial (D-38)");
            }
        }

        [Test]
        public void BuyingEveryLevel_StopsExactlyAtTheLimit_AndSpendsTheSumOfCosts([Values] bool gdd)
        {
            var economy = gdd ? GddV1() : Prototype();
            foreach (var id in AllLines.Where(i => economy.Get(i).IsEnabled))
            {
                var line = economy.Get(id);
                double sum = SumOfCosts(line);
                var state = StateWith(sum);

                for (int level = 0; level < line.MaxLevel; level++)
                {
                    Assert.That(Purchases.NextCost(state, economy, id), Is.EqualTo(line.CostAt(level)));
                    Assert.That(Purchases.TryPurchase(state, economy, id), Is.EqualTo(PurchaseResult.Purchased), $"{id} compra {level + 1}");
                    Assert.That(state.LevelOf(id), Is.EqualTo(level + 1));
                }

                Assert.That(state.Shards, Is.GreaterThanOrEqualTo(0).And.LessThanOrEqualTo(sum * 1e-12), $"{id}: gastou a soma dos custos");
                Assert.That(Purchases.NextCost(state, economy, id), Is.Null);
                Assert.That(Purchases.TryPurchase(state, economy, id), Is.EqualTo(PurchaseResult.MaxLevel));
                Assert.That(state.LevelOf(id), Is.EqualTo(line.MaxLevel), "nunca passa do limite");
            }
        }

        [Test]
        public void NotEnoughShards_ChangesNothing()
        {
            var economy = Prototype();
            var state = StateWith(9);

            Assert.That(Purchases.TryPurchase(state, economy, UpgradeId.Pickaxe), Is.EqualTo(PurchaseResult.NotEnoughShards));
            Assert.That(state.Shards, Is.EqualTo(9));
            Assert.That(state.LevelOf(UpgradeId.Pickaxe), Is.EqualTo(0));
        }

        [Test]
        public void CostMissedOnlyByRounding_StillBuys_AndBalanceNeverGoesNegative()
        {
            var economy = Prototype();
            var state = StateWith(10 - 1e-12);

            Assert.That(Purchases.TryPurchase(state, economy, UpgradeId.Pickaxe), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(state.Shards, Is.EqualTo(0));
        }

        [Test]
        public void Pickaxe_DoublesTheDamageEveryLevel()
        {
            var economy = Prototype();
            var rules = BaseRules(damage: 1);
            var state = StateWith(SumOfCosts(economy.Get(UpgradeId.Pickaxe)));

            for (int level = 0; level <= 19; level++)
            {
                Assert.That(MinerStats.Compute(rules, economy, state).Damage, Is.EqualTo(Math.Pow(2, level)), $"nível {level}");
                Purchases.TryPurchase(state, economy, UpgradeId.Pickaxe);
            }
        }

        [Test]
        public void Speed_TakesAStepOffTheInterval_ButNeverGoesBelowTheMinimum()
        {
            // Limite de propósito maior que o necessário: a partir do 10º nível o intervalo fica preso no mínimo.
            var economy = new EconomyRules(
                new UpgradeDefinition(0, 1, 0, 0), new UpgradeDefinition(1, 1, 30, 0.05),
                new UpgradeDefinition(0, 1, 0, 0), new UpgradeDefinition(0, 1, 0, 0), new UpgradeDefinition(0, 1, 0, 0),
                minHitInterval: 0.5);
            var rules = BaseRules(hitInterval: 1);
            var state = StateWith(30);

            for (int level = 0; level <= 30; level++)
            {
                double interval = MinerStats.Compute(rules, economy, state).HitInterval;
                Assert.That(interval, Is.EqualTo(Math.Max(0.5, 1 - 0.05 * level)).Within(1e-12), $"nível {level}");
                Assert.That(interval, Is.GreaterThanOrEqualTo(0.5));
                Purchases.TryPurchase(state, economy, UpgradeId.Speed);
            }
        }

        [Test]
        public void Mining_AddsOnePercentPerLevel_UpToDouble()
        {
            var economy = Prototype();
            var state = StateWith(SumOfCosts(economy.Get(UpgradeId.Mining)));

            for (int level = 0; level <= 100; level++)
            {
                Assert.That(MinerStats.Compute(BaseRules(), economy, state).RewardMultiplier, Is.EqualTo(1 + 0.01 * level).Within(1e-12));
                Purchases.TryPurchase(state, economy, UpgradeId.Mining);
            }
            Assert.That(MinerStats.Compute(BaseRules(), economy, state).RewardMultiplier, Is.EqualTo(2).Within(1e-12), "+100% no máximo");
        }

        [Test]
        public void Boots_AddTenPercentOfWalkSpeedPerLevel()
        {
            var economy = Prototype();
            var state = StateWith(SumOfCosts(economy.Get(UpgradeId.Boots)));

            for (int level = 0; level <= 10; level++)
            {
                Assert.That(MinerStats.Compute(BaseRules(), economy, state).WalkSpeed, Is.EqualTo(4 * (1 + 0.1 * level)).Within(1e-12));
                Purchases.TryPurchase(state, economy, UpgradeId.Boots);
            }
        }

        [Test]
        public void DoublePickaxe_DoublesTheDamageOnce_OnTopOfThePickaxe()
        {
            var economy = Prototype();
            var state = StateWith(10 + 40 + 160 + 500 + 500);
            for (int i = 0; i < 3; i++) Purchases.TryPurchase(state, economy, UpgradeId.Pickaxe);

            Assert.That(Purchases.TryPurchase(state, economy, UpgradeId.DoublePickaxe), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(MinerStats.Compute(BaseRules(damage: 1), economy, state).Damage, Is.EqualTo(8 * 2));
            Assert.That(Purchases.TryPurchase(state, economy, UpgradeId.DoublePickaxe), Is.EqualTo(PurchaseResult.MaxLevel), "compra única");
            Assert.That(state.Shards, Is.EqualTo(500), "a 2ª tentativa não cobra");
        }

        [Test]
        public void DisabledLine_NeverSells()
        {
            var economy = GddV1(); // Botas não existem no GDD
            var state = StateWith(1e9);

            Assert.That(Purchases.TryPurchase(state, economy, UpgradeId.Boots), Is.EqualTo(PurchaseResult.MaxLevel));
            Assert.That(state.Shards, Is.EqualTo(1e9));
            Assert.That(MinerStats.Compute(BaseRules(), economy, state).WalkSpeed, Is.EqualTo(4));
        }

        /// <summary>Confere a leitura do GDD com os números da análise (Economia e Progressão §2).</summary>
        [Test]
        public void GddV1_MatchesTheEconomyAnalysis()
        {
            var economy = GddV1();
            Assert.That(economy.Get(UpgradeId.Pickaxe).CostAt(18), Is.EqualTo(3.4e12).Within(0.05e12), "19ª compra da Picareta ≈ 3,4·10¹²");

            var state = StateWith(SumOfCosts(economy.Get(UpgradeId.Pickaxe)) + 5000);
            for (int i = 0; i < 19; i++) Purchases.TryPurchase(state, economy, UpgradeId.Pickaxe);
            Purchases.TryPurchase(state, economy, UpgradeId.DoublePickaxe);
            Assert.That(MinerStats.Compute(BaseRules(damage: 0.5, hitInterval: 5), economy, state).Damage,
                        Is.EqualTo(0.5 * Math.Pow(2, 19) * 2), "dano máximo sem Letrado");
        }

        [Test]
        public void InvalidDefinitions_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new UpgradeDefinition(-1, 2, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UpgradeDefinition(1, 0, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UpgradeDefinition(1, 2, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UpgradeDefinition(1, 2, 1, 1).CostAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => EconomyRules.None(minHitInterval: 0));
        }
    }
}
