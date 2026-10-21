using System;
using System.Collections.Generic;
using System.Linq;
using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>O loop de mineração no mapa (Marco 2, E2): andar, bater, quebrar, nascer, determinismo.</summary>
    public class MiningSimulationTests
    {
        /// <summary>Valores próximos da tabela do Marco 2, escritos aqui para os testes não dependerem do asset.</summary>
        static MiningRules Rules(
            double mapHalfWidth = 7.5, double mapBottom = -11, double mapTop = 10, double startX = 0, double startY = -0.5,
            int initial = 10, int max = 12, double spawnInterval = 5, double spacing = 2, double fromMiner = 1.5,
            double health = 10, double reward = 1, double damage = 1, double hitInterval = 1,
            double walkSpeed = 4, double range = 1) =>
            new MiningRules(
                new Vec2(-mapHalfWidth, mapBottom), new Vec2(mapHalfWidth, mapTop), new Vec2(startX, startY),
                initial, max, spawnInterval, spacing, fromMiner, health, reward, damage, hitInterval, walkSpeed, range);

        /// <summary>Mapa vazio e sem nascimento: os testes põem cada cristal à mão.</summary>
        static MiningSimulation EmptyMap(double health = 3) =>
            new MiningSimulation(
                Rules(mapHalfWidth: 20, mapBottom: -20, mapTop: 20, startY: 0, initial: 0, spawnInterval: 1e9, health: health),
                new SeededRandom(1));

        [Test]
        public void Miner_WalksAtWalkSpeed_AndStopsAtMiningRange()
        {
            var sim = EmptyMap();
            sim.SpawnCrystalAt(new Vec2(10, 0));

            sim.Tick(2.0);
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Walking));
            Assert.That(sim.Miner.Position.X, Is.EqualTo(8).Within(1e-9));

            sim.Tick(0.25); // 9 unidades a 4/s = 2,25 s
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Mining));
            Assert.That(sim.Miner.Position.X, Is.EqualTo(9).Within(1e-9));
            Assert.That(sim.Miner.Position.Y, Is.EqualTo(0).Within(1e-9));
        }

        [Test]
        public void Hits_StartOnlyAfterArrival_OneEveryInterval_AndBreakPaysOnce()
        {
            var sim = EmptyMap(health: 3);
            var crystal = sim.SpawnCrystalAt(new Vec2(10, 0));
            int broken = 0, shardsEvents = 0;
            sim.CrystalBroken += _ => broken++;
            sim.ShardsChanged += _ => shardsEvents++;

            sim.Tick(2.25 + 0.99);
            Assert.That(sim.State.Hits, Is.EqualTo(0), "chegou em 2,25 s; o 1º golpe é um intervalo depois");

            sim.Tick(0.01);
            Assert.That(sim.State.Hits, Is.EqualTo(1));
            Assert.That(crystal.Health, Is.EqualTo(2));

            sim.Tick(2.0);
            Assert.That(sim.State.Hits, Is.EqualTo(3));
            Assert.That(crystal.IsBroken, Is.True);
            Assert.That(sim.State.Breaks, Is.EqualTo(1));
            Assert.That(sim.State.Shards, Is.EqualTo(1));
            Assert.That(sim.Crystals, Is.Empty);
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Idle));
            Assert.That(sim.Miner.Target, Is.Null);

            sim.Tick(30);
            Assert.That(sim.State.Hits, Is.EqualTo(3), "sem cristal, sem golpe");
            Assert.That(sim.State.Shards, Is.EqualTo(1), "a recompensa vem uma vez só");
            Assert.That(broken, Is.EqualTo(1));
            Assert.That(shardsEvents, Is.EqualTo(1));
        }

        [Test]
        public void NoCrystals_MinerStaysIdleWhereItIs()
        {
            var sim = EmptyMap();
            sim.Tick(10);

            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Idle));
            Assert.That(sim.Miner.Position, Is.EqualTo(sim.Rules.MinerStart));
            Assert.That(sim.State.Hits, Is.EqualTo(0));
        }

        [Test]
        public void Target_DoesNotChange_WhenACloserCrystalAppearsDuringTheWalk()
        {
            var sim = EmptyMap();
            var first = sim.SpawnCrystalAt(new Vec2(10, 0));
            var targets = new List<Crystal>();
            sim.TargetChanged += t => targets.Add(t);

            sim.Tick(1);
            sim.SpawnCrystalAt(sim.Miner.Position + new Vec2(0.5, 0));
            sim.Tick(1);

            Assert.That(sim.Miner.Target, Is.SameAs(first));
            Assert.That(targets, Is.EqualTo(new[] { first }));
        }

        [Test]
        public void AfterBreaking_GoesToTheNextByBatchThenDistance()
        {
            var sim = EmptyMap(health: 1);
            var far = sim.SpawnCrystalAt(new Vec2(-10, 0));   // lote 1
            var near = sim.SpawnCrystalAt(new Vec2(2, 0));    // lote 2, mais perto
            var order = new List<int>();
            sim.CrystalBroken += c => order.Add(c.Id);

            sim.Tick(60);

            Assert.That(order, Is.EqualTo(new[] { far.Id, near.Id }));
        }

        [Test]
        public void InitialCrystals_AreBatch1_LaterOnesGetTheirOwnBatchInBirthOrder()
        {
            var sim = new MiningSimulation(Rules(initial: 3, max: 10, spawnInterval: 1, health: 1e9), new SeededRandom(3));
            Assert.That(sim.Crystals.Select(c => c.Batch), Is.EqualTo(new[] { 1, 1, 1 }));

            sim.Tick(5.5);

            Assert.That(sim.Crystals.Select(c => c.Id), Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
            Assert.That(sim.Crystals.Select(c => c.Batch), Is.EqualTo(new[] { 1, 1, 1, 2, 3, 4, 5, 6 }));
        }

        [Test]
        public void Spawning_RespectsMaximumAreaAndSpacing()
        {
            var rules = Rules(initial: 10, max: 12, spawnInterval: 0.1, health: 1e9);
            var sim = new MiningSimulation(rules, new SeededRandom(42));
            var tooCloseToMiner = 0;
            sim.CrystalSpawned += c =>
            {
                if (Vec2.Distance(c.Position, sim.Miner.Position) < rules.MinDistanceFromMiner) tooCloseToMiner++;
            };
            Assert.That(sim.Crystals.All(c => Vec2.Distance(c.Position, rules.MinerStart) >= rules.MinDistanceFromMiner));

            sim.Tick(60);

            Assert.That(sim.Crystals.Count, Is.EqualTo(rules.MaxCrystals));
            Assert.That(tooCloseToMiner, Is.EqualTo(0));
            foreach (var c in sim.Crystals)
            {
                Assert.That(c.Position.X, Is.InRange(rules.MapMin.X, rules.MapMax.X));
                Assert.That(c.Position.Y, Is.InRange(rules.MapMin.Y, rules.MapMax.Y));
                foreach (var other in sim.Crystals.Where(o => o != c))
                    Assert.That(Vec2.Distance(c.Position, other.Position), Is.GreaterThanOrEqualTo(rules.MinCrystalSpacing));
            }
        }

        [Test]
        public void CrowdedMap_SkipsSpawnsInsteadOfOverlapping()
        {
            var rules = Rules(mapHalfWidth: 1, mapBottom: -1, mapTop: 1, startY: 0, initial: 5, max: 50,
                              spawnInterval: 0.1, spacing: 2, fromMiner: 0, health: 1e9);
            var sim = new MiningSimulation(rules, new SeededRandom(8));

            Assert.DoesNotThrow(() => sim.Tick(10));
            Assert.That(sim.Crystals.Count, Is.InRange(1, 4), "num quadrado 2×2 com espaçamento 2 cabem no máximo 4");
        }

        [Test]
        public void SameSeed_GivesTheSameGame()
        {
            var a = new MiningSimulation(Rules(), new SeededRandom(99));
            var b = new MiningSimulation(Rules(), new SeededRandom(99));
            for (int i = 0; i < 600 * 60; i++)
            {
                a.Tick(1.0 / 60);
                b.Tick(1.0 / 60);
            }

            AssertSameGame(a, b, positionTolerance: 0);
            Assert.That(a.State.Breaks, Is.GreaterThan(0));
        }

        /// <summary>O coração do D-T2: o resultado não depende de como o tempo foi fatiado.</summary>
        [Test]
        public void Result_DoesNotDependOnHowTimeIsSliced()
        {
            const ulong seed = 2026;
            var frames = new MiningSimulation(Rules(), new SeededRandom(seed));
            for (int i = 0; i < 600 * 60; i++) frames.Tick(1.0 / 60);

            var oneShot = new MiningSimulation(Rules(), new SeededRandom(seed));
            oneShot.Tick(600);

            var irregular = new MiningSimulation(Rules(), new SeededRandom(seed));
            var slicer = new SeededRandom(5);
            double left = 600;
            while (left > 0)
            {
                double dt = Math.Min(left, slicer.Range(0, 0.5));
                irregular.Tick(dt);
                left -= dt;
            }

            AssertSameGame(frames, oneShot, positionTolerance: 1e-6);
            AssertSameGame(frames, irregular, positionTolerance: 1e-6);
        }

        [Test]
        public void OneHour_RunsWithoutErrors_AndInvariantsHold()
        {
            var rules = Rules();
            var sim = new MiningSimulation(rules, new SeededRandom(7));

            for (int i = 0; i < 36000; i++)
            {
                sim.Tick(0.1);
                Assert.That(sim.Crystals.Count, Is.LessThanOrEqualTo(rules.MaxCrystals));
            }

            var state = sim.State;
            Assert.That(state.Breaks, Is.GreaterThan(100));
            Assert.That(state.Shards, Is.EqualTo(state.Breaks * rules.CrystalReward));
            Assert.That(state.Hits, Is.GreaterThanOrEqualTo(state.Breaks * (long)Math.Ceiling(rules.CrystalHealth / rules.Damage)));
            Assert.That(sim.Miner.Position.X, Is.InRange(rules.MapMin.X, rules.MapMax.X));
            Assert.That(sim.Miner.Position.Y, Is.InRange(rules.MapMin.Y, rules.MapMax.Y));
            Assert.That(sim.Elapsed, Is.EqualTo(3600).Within(1e-6));
        }

        [Test]
        public void Tick_RejectsNegativeOrInvalidTime()
        {
            var sim = EmptyMap();
            Assert.Throws<ArgumentOutOfRangeException>(() => sim.Tick(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => sim.Tick(double.NaN));
            Assert.DoesNotThrow(() => sim.Tick(0));
        }

        [Test]
        public void Rules_RejectInvalidValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Rules(initial: 13, max: 12));
            Assert.Throws<ArgumentOutOfRangeException>(() => Rules(hitInterval: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => Rules(walkSpeed: 0));
            Assert.Throws<ArgumentException>(() => Rules(startX: 50));
            Assert.Throws<ArgumentException>(() => Rules(mapBottom: 10, mapTop: 10));
        }

        static void AssertSameGame(MiningSimulation a, MiningSimulation b, double positionTolerance)
        {
            Assert.That(b.State.Hits, Is.EqualTo(a.State.Hits), "golpes");
            Assert.That(b.State.Breaks, Is.EqualTo(a.State.Breaks), "quebras");
            Assert.That(b.State.Shards, Is.EqualTo(a.State.Shards), "lascas");
            Assert.That(b.Crystals.Select(c => c.Id), Is.EqualTo(a.Crystals.Select(c => c.Id)), "cristais vivos");
            for (int i = 0; i < a.Crystals.Count; i++)
            {
                Assert.That(b.Crystals[i].Position.X, Is.EqualTo(a.Crystals[i].Position.X).Within(positionTolerance));
                Assert.That(b.Crystals[i].Position.Y, Is.EqualTo(a.Crystals[i].Position.Y).Within(positionTolerance));
                Assert.That(b.Crystals[i].Health, Is.EqualTo(a.Crystals[i].Health), "vida do cristal " + a.Crystals[i].Id);
            }
            Assert.That(b.Miner.State, Is.EqualTo(a.Miner.State), "estado do anão");
            Assert.That(b.Miner.Target?.Id, Is.EqualTo(a.Miner.Target?.Id), "alvo do anão");
            Assert.That(Vec2.Distance(a.Miner.Position, b.Miner.Position), Is.LessThanOrEqualTo(positionTolerance), "posição do anão");
        }
    }
}
