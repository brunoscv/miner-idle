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
                initial, firstCrystalDistance: 3, max, spawnInterval: 1e9, minCrystalSpacing: 2, spawnDistance: 12,
                crystalHealth: 10, crystalReward: 1, damage: 1, hitInterval: 1, walkSpeed: 4, miningRange: 1);

        /// <summary>Desde a fila (M6): o cristal do debug entra no fim da fila, como um nascimento normal.</summary>
        [Test]
        public void SpawnCrystalNow_IgnoresTheMaximum_AndJoinsTheEndOfTheQueue()
        {
            var sim = new MiningSimulation(Rules(initial: 3, max: 3), new SeededRandom(11));
            Crystal spawned = null;
            sim.CrystalSpawned += c => spawned = c;

            var crystal = sim.SpawnCrystalNow();

            Assert.That(crystal, Is.Not.Null);
            Assert.That(spawned, Is.SameAs(crystal));
            Assert.That(sim.Crystals.Count, Is.EqualTo(4));
            Assert.That(sim.Crystals.Last(), Is.SameAs(crystal), "no fim da fila");
            Assert.That(crystal.Distance, Is.EqualTo(12), "a 12 un do anão (a fila inicial vai de 3 a 7)");
        }

        [Test]
        public void BreakTarget_PaysLikeANormalBreak_WithoutCountingAHit()
        {
            var sim = new MiningSimulation(Rules(), new SeededRandom(12));
            sim.Tick(0.1); // escolhe o alvo e começa a correr
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
