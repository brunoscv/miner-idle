using System.Collections.Generic;
using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9c E0 (D-266, D-267): a transição de andar no núcleo. Com duração 0 nada muda (o jogo até aqui). Com duração, a
    /// troca de caverna abre a transição: a fila do nível novo nasce na hora, o tempo de jogo anda, mas nada mais (golpe,
    /// corrida, toque); quando ela acaba, o jogo segue com o tempo que sobrou do mesmo Tick.
    /// </summary>
    public class FloorTransitionTests
    {
        static MiningRules Rules() =>
            new MiningRules(
                initialCrystals: 4, firstCrystalDistance: 3, maxCrystals: 12, spawnInterval: 1e9, minCrystalSpacing: 2,
                spawnDistance: 12, crystalHealth: 1e9, crystalReward: 1, damage: 1, hitInterval: 1, walkSpeed: 4,
                miningRange: 1, minCrystals: 4, caveCount: 10, caveHealthGrowth: 2, caveRewardGrowth: 2, floorBreaks: 100);

        static MiningSimulation Sim() =>
            new MiningSimulation(Rules(), new SeededRandom(1), strike: new StrikeRules(0, 2, 0.33, 1));

        [Test]
        public void WithoutDuration_TheNewCaveStartsAtOnce()
        {
            var sim = Sim();
            Assert.That(sim.FloorTransitionSeconds, Is.EqualTo(0));
            Assert.That(sim.SkipToNextCave(), Is.True);
            Assert.That(sim.IsInFloorTransition, Is.False);
            Assert.That(sim.FloorTransitionLeft, Is.EqualTo(0));
        }

        [Test]
        public void CaveChange_OpensTheTransition_BeforeCaveChanged_WithTheQueueRefilled()
        {
            var sim = Sim();
            sim.FloorTransitionSeconds = 2;
            var events = new List<string>();
            sim.FloorTransitionStarted += cave => events.Add("start " + cave);
            sim.CaveChanged += cave => events.Add("cave " + cave);

            sim.Tick(0);
            Assert.That(sim.SkipToNextCave(), Is.True);

            Assert.That(events, Is.EqualTo(new[] { "start 2", "cave 2" }));
            Assert.That(sim.IsInFloorTransition, Is.True);
            Assert.That(sim.FloorTransitionLeft, Is.EqualTo(2));
            Assert.That(sim.FloorTransitionLength, Is.EqualTo(2));
            Assert.That(sim.Crystals.Count, Is.GreaterThanOrEqualTo(sim.Rules.MinCrystals), "a fila no mínimo, já");
        }

        [Test]
        public void DuringTheTransition_OnlyTheClockRuns()
        {
            var sim = Sim();
            sim.FloorTransitionSeconds = 2;
            sim.Tick(0);
            sim.SkipToNextCave();
            int hits = 0;
            sim.HitLanded += _ => hits++;
            double distance = sim.Miner.Distance;
            double elapsed = sim.Elapsed;
            double play = sim.State.PlaySeconds;

            sim.Tick(1.5);

            Assert.That(sim.Elapsed, Is.EqualTo(elapsed + 1.5).Within(1e-9));
            Assert.That(sim.State.PlaySeconds, Is.EqualTo(play + 1.5).Within(1e-9), "o dia conta o tempo da transição");
            Assert.That(sim.Miner.Distance, Is.EqualTo(distance), "o anão não corre");
            Assert.That(hits, Is.EqualTo(0), "nem golpeia");
            Assert.That(sim.Tap(), Is.False, "o toque não vale");
            Assert.That(sim.Combo, Is.EqualTo(0));
            Assert.That(sim.FloorTransitionLeft, Is.EqualTo(0.5).Within(1e-9));
        }

        [Test]
        public void TheTransitionEnds_AndTheRestOfTheTickPlays()
        {
            var sim = Sim();
            sim.FloorTransitionSeconds = 2;
            sim.Tick(0);
            sim.SkipToNextCave();
            int ended = 0;
            sim.FloorTransitionEnded += () => ended++;
            double distance = sim.Miner.Distance;

            sim.Tick(3);

            Assert.That(ended, Is.EqualTo(1));
            Assert.That(sim.IsInFloorTransition, Is.False);
            Assert.That(sim.Miner.Distance, Is.GreaterThan(distance), "o segundo que sobrou foi de jogo");
            Assert.That(sim.Tap(), Is.True, "o toque volta a valer");
        }

        [Test]
        public void TheDurationIsTheOneChosenWhenItStarted()
        {
            var sim = Sim();
            sim.FloorTransitionSeconds = 2;
            sim.Tick(0);
            sim.SkipToNextCave();
            sim.FloorTransitionSeconds = 5; // o DBG trocou no meio: vale para a próxima

            Assert.That(sim.FloorTransitionLength, Is.EqualTo(2));
            sim.Tick(2);
            Assert.That(sim.IsInFloorTransition, Is.False);

            sim.SkipToNextCave();
            Assert.That(sim.FloorTransitionLength, Is.EqualTo(5));
        }

        [Test]
        public void NegativeDuration_IsRefused()
        {
            var sim = Sim();
            Assert.Throws<System.ArgumentOutOfRangeException>(() => sim.FloorTransitionSeconds = -1);
        }
    }
}
