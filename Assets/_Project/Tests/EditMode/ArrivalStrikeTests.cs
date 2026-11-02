using System.Collections.Generic;
using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>
    /// Golpe fluido (D-124): quando sai o primeiro golpe num cristal. Wait é o jogo até 2026-11-01 (o relógio zera na
    /// chegada); Charge (A1) enche o relógio também correndo; Immediate (A2) bate no instante da chegada.
    /// Corrida de 4 un/s, alcance 1, intervalo 2 s: um cristal a 3 un fica a 0,5 s de corrida.
    /// </summary>
    public class ArrivalStrikeTests
    {
        const double Interval = 2;

        static MiningSimulation Sim(ArrivalStrike mode, double health = 10, double damage = 5) =>
            new MiningSimulation(
                new MiningRules(0, 3, 12, 1e9, 2, 12, health, 1, damage, Interval, 4, 1, arrivalStrike: mode),
                new SeededRandom(1));

        static List<double> HitTimes(MiningSimulation sim)
        {
            var times = new List<double>();
            sim.HitLanded += _ => times.Add(sim.Elapsed);
            return times;
        }

        [Test]
        public void Wait_FirstHitIsAWholeIntervalAfterArriving()
        {
            var sim = Sim(ArrivalStrike.Wait);
            sim.SpawnCrystalAt(3);
            var hits = HitTimes(sim);

            sim.Tick(5);

            Assert.That(hits, Is.EqualTo(new[] { 2.5, 4.5 }).Within(1e-9));
        }

        [Test]
        public void Charge_ShortRun_WaitsOnlyWhatIsLeft()
        {
            var sim = Sim(ArrivalStrike.Charge);
            sim.SpawnCrystalAt(3); // 0,5 s correndo: o relógio chega com 0,5 dos 2 s
            var hits = HitTimes(sim);

            sim.Tick(5);

            Assert.That(hits, Is.EqualTo(new[] { 2.0, 4.0 }).Within(1e-9));
        }

        [Test]
        public void Charge_LongRun_StrikesOnArrival()
        {
            var sim = Sim(ArrivalStrike.Charge);
            sim.SpawnCrystalAt(13); // 3 s correndo: mais que um intervalo
            var hits = HitTimes(sim);

            sim.Tick(3);

            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Mining));
            Assert.That(hits, Is.EqualTo(new[] { 3.0 }).Within(1e-9), "o golpe sai no instante da chegada");

            sim.Tick(2);
            Assert.That(hits, Is.EqualTo(new[] { 3.0, 5.0 }).Within(1e-9), "o seguinte, um intervalo depois");
        }

        [Test]
        public void Charge_NeverStrikesFasterThanTheInterval()
        {
            var sim = Sim(ArrivalStrike.Charge, health: 5); // um golpe quebra
            for (int i = 0; i < 6; i++) sim.SpawnCrystalAt(3 + 2 * i); // cristais a 0,5 s um do outro
            var hits = HitTimes(sim);

            sim.Tick(13);

            Assert.That(hits.Count, Is.EqualTo(6));
            for (int i = 1; i < hits.Count; i++)
                Assert.That(hits[i] - hits[i - 1], Is.GreaterThanOrEqualTo(Interval - 1e-9), $"golpe {i}");
        }

        [Test]
        public void Immediate_StrikesOnArrival_ThenEveryInterval()
        {
            var sim = Sim(ArrivalStrike.Immediate);
            sim.SpawnCrystalAt(3);
            var hits = HitTimes(sim);

            sim.Tick(5);

            Assert.That(hits, Is.EqualTo(new[] { 0.5, 2.5 }).Within(1e-9));
        }

        [Test]
        public void TheModeCanChangeMidGame()
        {
            var sim = Sim(ArrivalStrike.Wait, health: 5);
            sim.SpawnCrystalAt(3);
            sim.SpawnCrystalAt(5);
            var hits = HitTimes(sim);

            sim.Tick(2.5); // o primeiro, como antes
            sim.ArrivalStrike = ArrivalStrike.Immediate;
            sim.Tick(1);

            Assert.That(hits, Is.EqualTo(new[] { 2.5, 3.0 }).Within(1e-9));
        }

        [TestCase(ArrivalStrike.Wait)]
        [TestCase(ArrivalStrike.Charge)]
        [TestCase(ArrivalStrike.Immediate)]
        public void SecondsToNextHit_IsWhenTheHitLands(ArrivalStrike mode)
        {
            var sim = Sim(mode);
            sim.SpawnCrystalAt(3);
            sim.SpawnCrystalAt(13);
            var hits = HitTimes(sim);
            sim.Tick(0.2); // correndo

            for (int i = 0; i < 4; i++)
            {
                double next = sim.SecondsToNextHit;
                int before = hits.Count;
                sim.Tick(next - 1e-6);
                Assert.That(hits.Count, Is.EqualTo(before), $"golpe {i}: ainda não");
                sim.Tick(1e-6);
                Assert.That(hits.Count, Is.EqualTo(before + 1), $"golpe {i}: agora");
            }
        }

        [Test]
        public void SecondsToNextHit_IsInfiniteWithoutATarget()
        {
            var sim = Sim(ArrivalStrike.Charge);
            Assert.That(sim.SecondsToNextHit, Is.EqualTo(double.PositiveInfinity));
        }

        [TestCase(ArrivalStrike.Charge)]
        [TestCase(ArrivalStrike.Immediate)]
        public void OneBigTick_EqualsManySmallOnes(ArrivalStrike mode)
        {
            MiningSimulation Make()
            {
                var sim = new MiningSimulation(
                    new MiningRules(10, 3, 12, 5, 2, 12, 10, 1, 5, Interval, 4, 1, minCrystals: 10, arrivalStrike: mode),
                    new SeededRandom(7));
                return sim;
            }

            var big = Make();
            var small = Make();
            big.Tick(60);
            for (int i = 0; i < 3600; i++) small.Tick(1.0 / 60);

            Assert.That(small.State.Breaks, Is.EqualTo(big.State.Breaks));
            Assert.That(small.Miner.Distance, Is.EqualTo(big.Miner.Distance).Within(1e-6));
            Assert.That(small.Miner.HitTimer, Is.EqualTo(big.Miner.HitTimer).Within(1e-6));
        }

        [Test]
        public void AMinuteOfPlay_ChargeAndImmediateBreakMoreThanWait()
        {
            long Breaks(ArrivalStrike mode)
            {
                var sim = new MiningSimulation(
                    new MiningRules(10, 3, 12, 5, 2, 12, 10, 1, 5, Interval, 4, 1, minCrystals: 10, arrivalStrike: mode),
                    new SeededRandom(7));
                sim.Tick(59); // longe de uma quebra exata, para a conta não depender do arredondamento
                return sim.State.Breaks;
            }

            // Caverna 1 do GDD v5 sem compras: 2 golpes por cristal, 0,5 s de corrida entre eles.
            // Wait: 0,5 + 2 + 2 = 4,5 s por cristal; Charge: 4,0 s; Immediate: 2,5 s.
            Assert.That(Breaks(ArrivalStrike.Wait), Is.EqualTo(13));
            Assert.That(Breaks(ArrivalStrike.Charge), Is.EqualTo(14));
            Assert.That(Breaks(ArrivalStrike.Immediate), Is.EqualTo(23));
        }
    }
}
