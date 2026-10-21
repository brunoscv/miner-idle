using System;
using System.Collections.Generic;
using System.Linq;
using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>Marco 3, E2: a simulação usa as melhorias, inclusive compradas no meio de um golpe ou de uma caminhada.</summary>
    public class MiningUpgradesTests
    {
        static readonly UpgradeId[] AllLines = (UpgradeId[])Enum.GetValues(typeof(UpgradeId));

        static MiningRules Rules(int initial = 0, double spawnInterval = 1e9, double health = 10) =>
            new MiningRules(
                new Vec2(-20, -20), new Vec2(20, 20), new Vec2(0, 0),
                initial, maxCrystals: 12, spawnInterval: spawnInterval, minCrystalSpacing: 2, minDistanceFromMiner: 1.5,
                crystalHealth: health, crystalReward: 1, damage: 1, hitInterval: 1, walkSpeed: 4, miningRange: 1);

        /// <summary>Efeitos grandes e custo 1 para os testes lerem fácil.</summary>
        static EconomyRules BigSteps() => new EconomyRules(
            pickaxe: new UpgradeDefinition(1, 1, 10, 2),
            speed: new UpgradeDefinition(1, 1, 1, 0.5),
            mining: new UpgradeDefinition(1, 1, 10, 0.5),
            doublePickaxe: new UpgradeDefinition(1, 1, 1, 2),
            boots: new UpgradeDefinition(1, 1, 10, 1.0),
            minHitInterval: 0.25);

        /// <summary>A tabela de protótipo do plano do Marco 3.</summary>
        static EconomyRules Prototype() => new EconomyRules(
            pickaxe: new UpgradeDefinition(10, 4, 19, 2),
            speed: new UpgradeDefinition(15, 3, 10, 0.05),
            mining: new UpgradeDefinition(10, 1.2, 100, 0.01),
            doublePickaxe: new UpgradeDefinition(500, 1, 1, 2),
            boots: new UpgradeDefinition(10, 2, 10, 0.1),
            minHitInterval: 0.5);

        static MiningSimulation Sim(EconomyRules economy, int initial = 0, double health = 10, ulong seed = 1) =>
            new MiningSimulation(Rules(initial, health: health), new SeededRandom(seed), economy: economy);

        [Test]
        public void WithoutEconomy_StatsAreTheBaseValues_AndNothingSells()
        {
            var sim = new MiningSimulation(Rules(), new SeededRandom(1));
            sim.AddShards(1e9);

            Assert.That(sim.Stats.Damage, Is.EqualTo(1));
            Assert.That(sim.Stats.HitInterval, Is.EqualTo(1));
            Assert.That(sim.Stats.WalkSpeed, Is.EqualTo(4));
            Assert.That(sim.Stats.RewardMultiplier, Is.EqualTo(1));
            foreach (var id in AllLines)
                Assert.That(sim.Purchase(id), Is.EqualTo(PurchaseResult.MaxLevel), id.ToString());
        }

        [Test]
        public void Purchase_UpdatesStats_AndNotifiesInOrder()
        {
            var sim = Sim(Prototype());
            sim.AddShards(10);
            var events = new List<string>();
            sim.UpgradePurchased += (id, level) => events.Add($"comprou {id} {level}");
            sim.StatsChanged += stats => events.Add($"dano {stats.Damage}");
            sim.ShardsChanged += shards => events.Add($"lascas {shards}");

            Assert.That(sim.Purchase(UpgradeId.Pickaxe), Is.EqualTo(PurchaseResult.Purchased));

            Assert.That(sim.Stats.Damage, Is.EqualTo(2));
            Assert.That(events, Is.EqualTo(new[] { "comprou Pickaxe 1", "dano 2", "lascas 0" }));
        }

        [Test]
        public void FailedPurchase_ChangesNothing_AndSendsNoEvent()
        {
            var sim = Sim(Prototype());
            sim.AddShards(9);
            int events = 0;
            sim.UpgradePurchased += (id, level) => events++;
            sim.StatsChanged += stats => events++;
            sim.ShardsChanged += shards => events++;

            Assert.That(sim.Purchase(UpgradeId.Pickaxe), Is.EqualTo(PurchaseResult.NotEnoughShards));
            Assert.That(sim.Stats.Damage, Is.EqualTo(1));
            Assert.That(sim.State.Shards, Is.EqualTo(9));
            Assert.That(events, Is.EqualTo(0));
        }

        [Test]
        public void Pickaxe_MakesCrystalsBreakInFewerHits()
        {
            var sim = Sim(BigSteps(), health: 10);
            sim.AddShards(1);
            sim.Purchase(UpgradeId.Pickaxe); // dano 2
            sim.SpawnCrystalAt(new Vec2(1, 0)); // já no alcance: começa a bater na hora

            sim.Tick(4.99);
            Assert.That(sim.State.Breaks, Is.EqualTo(0));
            sim.Tick(0.01);
            Assert.That(sim.State.Hits, Is.EqualTo(5), "10 de vida ÷ 2 de dano");
            Assert.That(sim.State.Breaks, Is.EqualTo(1));
        }

        [Test]
        public void FasterSpeed_BoughtMidSwing_HitsOnTheNextTick_ExactlyOnce()
        {
            var sim = Sim(BigSteps(), health: 100);
            sim.SpawnCrystalAt(new Vec2(1, 0));
            sim.Tick(0.7); // 0,7 s acumulados de um golpe de 1 s
            Assert.That(sim.State.Hits, Is.EqualTo(0));

            sim.AddShards(1);
            sim.Purchase(UpgradeId.Speed); // intervalo 1 → 0,5: o golpe já venceu
            Assert.That(sim.Stats.HitInterval, Is.EqualTo(0.5));

            sim.Tick(0);
            Assert.That(sim.State.Hits, Is.EqualTo(1), "sai no próximo Tick, uma vez só");
            sim.Tick(0.29);
            Assert.That(sim.State.Hits, Is.EqualTo(1));
            sim.Tick(0.01);
            Assert.That(sim.State.Hits, Is.EqualTo(2), "o resto (0,2 s) conta para o golpe seguinte de 0,5 s");
        }

        [Test]
        public void Boots_BoughtMidWalk_SpeedUpFromWhereTheMinerIs()
        {
            var sim = Sim(BigSteps());
            sim.SpawnCrystalAt(new Vec2(10, 0));
            sim.Tick(1); // 4 unidades andadas, faltam 5 até o alcance
            Assert.That(sim.Miner.Position.X, Is.EqualTo(4).Within(1e-9));

            sim.AddShards(1);
            sim.Purchase(UpgradeId.Boots); // caminhada 4 → 8
            sim.Tick(0.62);
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Walking));
            sim.Tick(0.005); // 5 ÷ 8 = 0,625 s
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Mining));
            Assert.That(sim.Miner.Position.X, Is.EqualTo(9).Within(1e-9));
        }

        [Test]
        public void Mining_MultipliesTheShardsOfEachCrystal()
        {
            var sim = Sim(BigSteps(), health: 1);
            sim.AddShards(1);
            sim.Purchase(UpgradeId.Mining); // ×1,5
            sim.SpawnCrystalAt(new Vec2(1, 0));

            sim.Tick(1);

            Assert.That(sim.State.Breaks, Is.EqualTo(1));
            Assert.That(sim.State.Shards, Is.EqualTo(1.5));
        }

        [Test]
        public void SamePurchasesAtTheSameTimes_GiveTheSameGame_HoweverTimeIsSliced()
        {
            var a = Sim(Prototype(), initial: 10, seed: 77);
            var b = Sim(Prototype(), initial: 10, seed: 77);
            var script = new (double time, UpgradeId id)[]
            {
                (30, UpgradeId.Pickaxe), (90, UpgradeId.Boots), (150, UpgradeId.Speed), (240, UpgradeId.Mining),
            };

            double now = 0;
            foreach (var (time, id) in script)
            {
                for (int i = 0; i < 600; i++) a.Tick((time - now) / 600); // quadros pequenos
                b.Tick(time - now);                                       // um passo só
                now = time;
                a.AddShards(100);
                b.AddShards(100);
                Assert.That(a.Purchase(id), Is.EqualTo(b.Purchase(id)));
            }
            for (int i = 0; i < 600; i++) a.Tick(60.0 / 600);
            b.Tick(60);

            Assert.That(a.State.Hits, Is.EqualTo(b.State.Hits));
            Assert.That(a.State.Breaks, Is.EqualTo(b.State.Breaks));
            Assert.That(a.State.Shards, Is.EqualTo(b.State.Shards).Within(1e-9));
            foreach (var id in AllLines) Assert.That(a.State.LevelOf(id), Is.EqualTo(b.State.LevelOf(id)));
            Assert.That(a.Crystals.Select(c => c.Id), Is.EqualTo(b.Crystals.Select(c => c.Id)));
            Assert.That(Vec2.Distance(a.Miner.Position, b.Miner.Position), Is.LessThan(1e-6));
        }

        /// <summary>Uma hora comprando sempre a mais barata que der: nada quebra e nenhuma regra é violada.</summary>
        [Test]
        public void OneHour_BuyingTheCheapest_KeepsEveryRule()
        {
            var economy = Prototype();
            var sim = new MiningSimulation(
                new MiningRules(new Vec2(-7.5, -11), new Vec2(7.5, 10), new Vec2(0, -0.5), 10, 12, 5, 2, 1.5, 10, 1, 1, 1, 4, 1),
                new SeededRandom(5), economy: economy);
            int purchases = 0;

            for (int second = 0; second < 3600; second++)
            {
                sim.Tick(1);
                var cheapest = AllLines
                    .Select(id => (id, cost: Purchases.NextCost(sim.State, economy, id)))
                    .Where(x => x.cost.HasValue)
                    .OrderBy(x => x.cost.Value)
                    .FirstOrDefault();
                if (cheapest.cost.HasValue && sim.Purchase(cheapest.id) == PurchaseResult.Purchased) purchases++;

                Assert.That(sim.State.Shards, Is.GreaterThanOrEqualTo(0));
                Assert.That(sim.Stats.HitInterval, Is.GreaterThanOrEqualTo(economy.MinHitInterval));
            }

            Assert.That(purchases, Is.GreaterThan(10), "a economia de protótipo deixa comprar várias vezes em 1 h");
            foreach (var id in AllLines)
                Assert.That(sim.State.LevelOf(id), Is.LessThanOrEqualTo(economy.Get(id).MaxLevel), id.ToString());
            Assert.That(sim.State.Breaks, Is.GreaterThan(0));
        }
    }
}
