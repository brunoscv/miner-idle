using System;
using System.Collections.Generic;
using System.Linq;
using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>
    /// O loop de mineração (Marco 2, E2), na fila da descida desde o M6 (E2, D-52…D-55, D-60): correr, bater, quebrar,
    /// nascer, determinismo. Até o M5 estes testes mediam o mapa aberto; foram reescritos para medir a fila.
    /// </summary>
    public class MiningSimulationTests
    {
        /// <summary>Os valores do asset de protótipo, escritos aqui para os testes não dependerem dele.</summary>
        static MiningRules Rules(
            int initial = 10, double first = 3, int max = 12, double spawnInterval = 5, double spacing = 2,
            double spawnDistance = 12, double health = 10, double reward = 1, double damage = 1, double hitInterval = 1,
            double walkSpeed = 4, double range = 1, int min = 0) =>
            new MiningRules(initial, first, max, spawnInterval, spacing, spawnDistance, health, reward, damage, hitInterval,
                            walkSpeed, range, min);

        /// <summary>Fila vazia e sem nascimento: os testes põem cada cristal à mão.</summary>
        static MiningSimulation EmptyTrack(double health = 3) =>
            new MiningSimulation(Rules(initial: 0, spawnInterval: 1e9, health: health), new SeededRandom(1));

        [Test]
        public void Miner_RunsAtWalkSpeed_AndStopsAtMiningRange()
        {
            var sim = EmptyTrack();
            sim.SpawnCrystalAt(10);

            sim.Tick(2.0);
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Running));
            Assert.That(sim.Miner.Distance, Is.EqualTo(8).Within(1e-9));

            sim.Tick(0.25); // 9 unidades a 4/s = 2,25 s
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Mining));
            Assert.That(sim.Miner.Distance, Is.EqualTo(9).Within(1e-9));
        }

        [Test]
        public void Hits_StartOnlyAfterArrival_OneEveryInterval_AndBreakPaysOnce()
        {
            var sim = EmptyTrack(health: 3);
            var crystal = sim.SpawnCrystalAt(10);
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
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Running), "volta a correr");
            Assert.That(sim.Miner.Target, Is.Null);

            sim.Tick(30);
            Assert.That(sim.State.Hits, Is.EqualTo(3), "sem cristal, sem golpe");
            Assert.That(sim.State.Shards, Is.EqualTo(1), "a recompensa vem uma vez só");
            Assert.That(broken, Is.EqualTo(1));
            Assert.That(shardsEvents, Is.EqualTo(1));
        }

        /// <summary>D-55: a descida nunca para; sem cristal, o anão corre na velocidade de corrida, sem ganhar nada.</summary>
        [Test]
        public void NoCrystals_MinerKeepsRunning()
        {
            var sim = EmptyTrack();
            sim.Tick(10);

            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Running));
            Assert.That(sim.Miner.Distance, Is.EqualTo(40).Within(1e-9));
            Assert.That(sim.State.Hits, Is.EqualTo(0));
            Assert.That(sim.State.Shards, Is.EqualTo(0));
        }

        /// <summary>
        /// D-80: o anão forte quebra mais rápido do que o relógio repõe; com o mínimo, a fila nunca fica abaixo dele, nem
        /// numa partida de 50 minutos, e cada cristal novo nasce fora da tela (a pelo menos 12 un do anão).
        /// </summary>
        [Test]
        public void StrongMiner_NeverRunsOutOfCrystals_InALongRun()
        {
            var sim = new MiningSimulation(Rules(damage: 1e9, hitInterval: 0.5, min: 4), new SeededRandom(1));
            int broken = 0;
            sim.CrystalBroken += _ => broken++;
            sim.CrystalSpawned += c => Assert.That(c.Distance - sim.Miner.Distance, Is.GreaterThanOrEqualTo(12 - 1e-9));

            for (int second = 0; second < 50 * 60; second++)
            {
                sim.Tick(1);
                Assert.That(sim.Crystals.Count, Is.GreaterThanOrEqualTo(4), $"fila no segundo {second}");
            }
            // Sem o mínimo, o relógio (1 a cada 5 s) daria ~600 em 50 min; com ele, o ritmo é o do anão.
            Assert.That(broken, Is.GreaterThan(1500));
        }

        [Test]
        public void WithoutMinimum_TheQueueDrains()
        {
            var sim = new MiningSimulation(Rules(damage: 1e9, hitInterval: 0.5), new SeededRandom(1));
            sim.Tick(60);
            Assert.That(sim.Crystals.Count, Is.LessThan(4), "sem o mínimo, o anão forte esvazia a fila");
        }

        [Test]
        public void RunningWithoutCrystals_StopsAtTheOneThatIsBorn()
        {
            var sim = new MiningSimulation(Rules(initial: 0, spawnInterval: 5, health: 1e9), new SeededRandom(1));

            sim.Tick(5); // 20 un corridas; nasce um cristal a 20 + 12 = 32
            Assert.That(sim.Crystals.Single().Distance, Is.EqualTo(32).Within(1e-9));

            sim.Tick(11 / 4.0); // faltam 32 − 1 − 20 = 11 un
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Mining));
            Assert.That(sim.Miner.Distance, Is.EqualTo(31).Within(1e-9));
        }

        [Test]
        public void Target_DoesNotChange_WhenACrystalIsPutInFrontOfIt()
        {
            var sim = EmptyTrack();
            var first = sim.SpawnCrystalAt(10);
            var targets = new List<Crystal>();
            sim.TargetChanged += t => targets.Add(t);

            sim.Tick(1);
            var inFront = sim.SpawnCrystalAt(sim.Miner.Distance + 0.5);
            sim.Tick(1);

            Assert.That(sim.Crystals.First(), Is.SameAs(inFront), "o novo entra no seu lugar da fila");
            Assert.That(sim.Miner.Target, Is.SameAs(first), "mas o alvo só muda depois de quebrar");
            Assert.That(targets, Is.EqualTo(new[] { first }));
        }

        [Test]
        public void AfterBreaking_GoesToTheNextOfTheQueue_InDistanceOrder()
        {
            var sim = EmptyTrack(health: 1);
            var far = sim.SpawnCrystalAt(10);  // nasceu primeiro, mais longe
            var near = sim.SpawnCrystalAt(4);  // nasceu depois, mais perto
            var order = new List<int>();
            sim.CrystalBroken += c => order.Add(c.Id);

            sim.Tick(60);

            Assert.That(order, Is.EqualTo(new[] { near.Id, far.Id }), "a fila vai do mais perto ao mais longe");
        }

        /// <summary>D-60: a fila inicial já está na tela (a partir de 3 un, a cada 2 un); os novos entram no fim, a 12 un ou mais.</summary>
        [Test]
        public void InitialCrystals_StartOnScreen_AndNewOnesJoinTheEndOfTheQueue()
        {
            var sim = new MiningSimulation(Rules(initial: 3, max: 10, spawnInterval: 1, health: 1e9), new SeededRandom(3));
            Assert.That(sim.Crystals.Select(c => c.Distance), Is.EqualTo(new[] { 3.0, 5.0, 7.0 }));

            sim.Tick(5.5); // o anão chega ao 1º (para em 2) em 0,5 s e bate; nascem 5

            Assert.That(sim.Crystals.Select(c => c.Id), Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
            Assert.That(sim.Crystals.Select(c => c.Distance), Is.EqualTo(new[] { 3.0, 5.0, 7.0, 14.0, 16.0, 18.0, 20.0, 22.0 }),
                        "o 1º novo a 2 + 12 = 14; os seguintes a 2 un do último");
        }

        [Test]
        public void Spawning_RespectsMaximum_SpacingAndDistanceAhead()
        {
            var rules = Rules(initial: 10, max: 12, spawnInterval: 0.1, health: 1e9);
            var sim = new MiningSimulation(rules, new SeededRandom(42));
            int tooClose = 0;
            sim.CrystalSpawned += c =>
            {
                if (c.Distance < sim.Miner.Distance + rules.SpawnDistance - 1e-9) tooClose++;
            };

            sim.Tick(60);

            Assert.That(sim.Crystals.Count, Is.EqualTo(rules.MaxCrystals));
            Assert.That(tooClose, Is.EqualTo(0), "nenhum nasce à vista (perto demais do anão)");
            for (int i = 1; i < sim.Crystals.Count; i++)
                Assert.That(sim.Crystals[i].Distance - sim.Crystals[i - 1].Distance, Is.GreaterThanOrEqualTo(rules.MinCrystalSpacing - 1e-9));
        }

        [Test]
        public void FullQueue_WaitsAndSpawnsRightAfterABreak()
        {
            var rules = Rules(initial: 12, max: 12, spawnInterval: 5, health: 1e9);
            var sim = new MiningSimulation(rules, new SeededRandom(8));

            sim.Tick(20);
            Assert.That(sim.Crystals.Count, Is.EqualTo(12), "cheia: não nasce");

            double last = sim.Crystals.Last().Distance;
            sim.BreakTarget();
            sim.Tick(0);

            Assert.That(sim.Crystals.Count, Is.EqualTo(12), "o relógio esperou no limite e nasce logo depois da quebra");
            Assert.That(sim.Crystals.Last().Distance, Is.EqualTo(last + rules.MinCrystalSpacing).Within(1e-9));
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

            AssertSameGame(a, b, distanceTolerance: 0);
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

            AssertSameGame(frames, oneShot, distanceTolerance: 1e-6);
            AssertSameGame(frames, irregular, distanceTolerance: 1e-6);
        }

        [Test]
        public void OneHour_RunsWithoutErrors_AndInvariantsHold()
        {
            var rules = Rules();
            var sim = new MiningSimulation(rules, new SeededRandom(7));
            double lastDistance = 0;

            for (int i = 0; i < 36000; i++)
            {
                sim.Tick(0.1);
                Assert.That(sim.Crystals.Count, Is.LessThanOrEqualTo(rules.MaxCrystals));
                Assert.That(sim.Miner.Distance, Is.GreaterThanOrEqualTo(lastDistance), "o anão nunca volta");
                lastDistance = sim.Miner.Distance;
            }

            var state = sim.State;
            Assert.That(state.Breaks, Is.GreaterThan(100));
            Assert.That(state.Shards, Is.EqualTo(state.Breaks * rules.CrystalReward));
            Assert.That(state.Hits, Is.GreaterThanOrEqualTo(state.Breaks * (long)Math.Ceiling(rules.CrystalHealth / rules.Damage)));
            for (int i = 1; i < sim.Crystals.Count; i++)
                Assert.That(sim.Crystals[i].Distance, Is.GreaterThan(sim.Crystals[i - 1].Distance), "a fila continua em ordem");
            Assert.That(sim.Elapsed, Is.EqualTo(3600).Within(1e-6));
        }

        [Test]
        public void Tick_RejectsNegativeOrInvalidTime()
        {
            var sim = EmptyTrack();
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
            Assert.Throws<ArgumentOutOfRangeException>(() => Rules(first: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Rules(spawnDistance: -1));
        }

        static void AssertSameGame(MiningSimulation a, MiningSimulation b, double distanceTolerance)
        {
            Assert.That(b.State.Hits, Is.EqualTo(a.State.Hits), "golpes");
            Assert.That(b.State.Breaks, Is.EqualTo(a.State.Breaks), "quebras");
            Assert.That(b.State.Shards, Is.EqualTo(a.State.Shards), "lascas");
            Assert.That(b.Crystals.Select(c => c.Id), Is.EqualTo(a.Crystals.Select(c => c.Id)), "cristais vivos");
            for (int i = 0; i < a.Crystals.Count; i++)
            {
                Assert.That(b.Crystals[i].Distance, Is.EqualTo(a.Crystals[i].Distance).Within(distanceTolerance));
                Assert.That(b.Crystals[i].Health, Is.EqualTo(a.Crystals[i].Health), "vida do cristal " + a.Crystals[i].Id);
            }
            Assert.That(b.Miner.State, Is.EqualTo(a.Miner.State), "estado do anão");
            Assert.That(b.Miner.Target?.Id, Is.EqualTo(a.Miner.Target?.Id), "alvo do anão");
            Assert.That(Math.Abs(a.Miner.Distance - b.Miner.Distance), Is.LessThanOrEqualTo(distanceTolerance), "distância do anão");
        }
    }
}
