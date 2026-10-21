using System;
using System.Linq;
using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>Atalhos do painel de debug (Marco 2, E5): passam pelo mesmo caminho das regras.</summary>
    public class DebugCommandsTests
    {
        static MiningRules Rules(int initial = 3, int max = 3) =>
            new MiningRules(
                new Vec2(-7.5, -11), new Vec2(7.5, 10), new Vec2(0, -0.5),
                initial, max, spawnInterval: 1e9, minCrystalSpacing: 2, minDistanceFromMiner: 1.5,
                crystalHealth: 10, crystalReward: 1, damage: 1, hitInterval: 1, walkSpeed: 4, miningRange: 1);

        [Test]
        public void SpawnCrystalNow_IgnoresTheMaximum_ButKeepsSpacingAndGetsItsOwnBatch()
        {
            var sim = new MiningSimulation(Rules(initial: 3, max: 3), new SeededRandom(11));
            Crystal spawned = null;
            sim.CrystalSpawned += c => spawned = c;

            var crystal = sim.SpawnCrystalNow();

            Assert.That(crystal, Is.Not.Null);
            Assert.That(spawned, Is.SameAs(crystal));
            Assert.That(sim.Crystals.Count, Is.EqualTo(4));
            Assert.That(crystal.Batch, Is.EqualTo(2));
            foreach (var other in sim.Crystals.Where(c => c != crystal))
                Assert.That(Vec2.Distance(crystal.Position, other.Position), Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void BreakTarget_PaysLikeANormalBreak_WithoutCountingAHit()
        {
            var sim = new MiningSimulation(Rules(), new SeededRandom(12));
            sim.Tick(0.1); // escolhe o alvo e começa a andar
            var target = sim.Miner.Target;
            int broken = 0;
            sim.CrystalBroken += c => broken++;

            Assert.That(sim.BreakTarget(), Is.True);

            Assert.That(target.IsBroken, Is.True);
            Assert.That(broken, Is.EqualTo(1));
            Assert.That(sim.State.Breaks, Is.EqualTo(1));
            Assert.That(sim.State.Shards, Is.EqualTo(1));
            Assert.That(sim.State.Hits, Is.EqualTo(0));
            Assert.That(sim.Crystals, Has.No.Member(target));

            sim.Tick(0.1);
            Assert.That(sim.Miner.Target, Is.Not.Null.And.Not.SameAs(target), "segue para o próximo");
        }

        [Test]
        public void BreakTarget_WithoutTarget_DoesNothing()
        {
            var sim = new MiningSimulation(Rules(initial: 0), new SeededRandom(13));
            Assert.That(sim.BreakTarget(), Is.False);
            Assert.That(sim.State.Breaks, Is.EqualTo(0));
        }

        [Test]
        public void AddShards_AddsAndNotifies()
        {
            var sim = new MiningSimulation(Rules(), new SeededRandom(14));
            double notified = -1;
            sim.ShardsChanged += total => notified = total;

            sim.AddShards(100);

            Assert.That(sim.State.Shards, Is.EqualTo(100));
            Assert.That(notified, Is.EqualTo(100));
            Assert.Throws<ArgumentOutOfRangeException>(() => sim.AddShards(-1));
        }
    }
}
