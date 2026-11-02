using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>
    /// D-126: o toque quebra o cristal no meio da picaretada e o anão termina o golpe parado antes de correr. Golpe na
    /// chegada (A2, D-124), corrida de 4 un/s, alcance 1, intervalo 2 s, volta da picareta 0,72 s, dano 5, toque 33%.
    /// Um cristal a 3 un fica a 0,5 s de corrida.
    /// </summary>
    public class SwingRecoveryTests
    {
        const double Recovery = 0.72;

        static MiningSimulation Sim(double recovery = Recovery, double interval = 2, double health = 10) =>
            new MiningSimulation(
                new MiningRules(0, 3, 12, 1e9, 2, 12, health, 1, 5, interval, 4, 1,
                                arrivalStrike: ArrivalStrike.Immediate, swingRecovery: recovery),
                new SeededRandom(1), strike: new StrikeRules(0, 2, 0.33, 1));

        /// <summary>Toca até o alvo quebrar (o anão parado nele: sem alcance, todo toque acerta).</summary>
        static void TapUntilBroken(MiningSimulation sim)
        {
            var target = sim.Miner.Target;
            for (int i = 0; i < 100 && sim.Miner.Target == target; i++) sim.Tap();
            Assert.That(sim.Miner.Target, Is.Not.SameAs(target), "o toque quebrou o alvo");
        }

        [Test]
        public void TapBreakMidSwing_FinishesTheSwingStanding_ThenRuns()
        {
            var sim = Sim();
            sim.SpawnCrystalAt(3);
            sim.SpawnCrystalAt(5);
            sim.Tick(0.5); // chega e bate (A2): 5 de 10
            sim.Tick(0.2);
            TapUntilBroken(sim);
            sim.Tick(0); // escolhe o próximo alvo

            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Recovering));
            Assert.That(sim.SecondsToNextHit, Is.EqualTo(Recovery - 0.2 + 0.5).Within(1e-9), "termina o golpe, corre, bate na chegada");
            double at = sim.Miner.Distance;

            sim.Tick(Recovery - 0.2 - 1e-6);
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Recovering), "ainda terminando o golpe");
            Assert.That(sim.Miner.Distance, Is.EqualTo(at), "parado");

            sim.Tick(2e-6);
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Running), "o golpe acabou: corre");
        }

        [Test]
        public void TapBreakAfterTheSwingEnded_RunsAtOnce()
        {
            var sim = Sim();
            sim.SpawnCrystalAt(3);
            sim.SpawnCrystalAt(5);
            sim.Tick(0.5 + Recovery + 0.1); // a volta da picareta já acabou

            TapUntilBroken(sim);
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Running));
        }

        [Test]
        public void TapBreakBeforeArriving_KeepsRunning()
        {
            var sim = Sim();
            sim.SpawnCrystalAt(3);
            sim.SpawnCrystalAt(5);
            sim.Tick(0.2); // correndo; o alvo está no alcance do toque (sem limite nestes valores)

            TapUntilBroken(sim);
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Running), "a picareta não desceu nele");
        }

        [Test]
        public void PickaxeBreak_RunsAtOnce()
        {
            var sim = Sim(health: 5); // a picareta da chegada quebra
            sim.SpawnCrystalAt(3);
            sim.SpawnCrystalAt(5);

            sim.Tick(0.5);
            Assert.That(sim.State.Breaks, Is.EqualTo(1));
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Running));
        }

        [Test]
        public void WhileFinishingTheSwing_TapsHitTheNextCrystal_AndBreakingItDoesNotStopTheSwing()
        {
            var sim = Sim();
            sim.SpawnCrystalAt(3);
            var next = sim.SpawnCrystalAt(5);
            sim.Tick(0.5);
            TapUntilBroken(sim);
            sim.Tick(0); // escolhe o próximo alvo
            Assert.That(sim.Miner.Target, Is.SameAs(next));

            TapUntilBroken(sim); // o próximo, a 2 un, também cai
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Recovering), "continua terminando o golpe");
            Assert.That(sim.State.Breaks, Is.EqualTo(2));
        }

        [Test]
        public void TheSwingNeverLastsMoreThanTheInterval()
        {
            var sim = Sim(interval: 0.5);
            sim.SpawnCrystalAt(3);
            sim.SpawnCrystalAt(5);
            Assert.That(sim.SwingRecovery, Is.EqualTo(0.5));

            sim.Tick(0.5);
            sim.Tick(0.1);
            TapUntilBroken(sim);
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Recovering));
            sim.Tick(0.4 + 1e-6);
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Running));
        }

        [Test]
        public void ZeroRecovery_RunsAtOnce_AsBefore()
        {
            var sim = Sim(recovery: 0);
            sim.SpawnCrystalAt(3);
            sim.SpawnCrystalAt(5);
            sim.Tick(0.6);

            TapUntilBroken(sim);
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Running));
        }

        [Test]
        public void OneBigTick_EqualsManySmallOnes_WithTaps()
        {
            MiningSimulation Make() => new MiningSimulation(
                new MiningRules(10, 3, 12, 5, 2, 12, 10, 1, 5, 2, 4, 1, minCrystals: 10,
                                arrivalStrike: ArrivalStrike.Immediate, swingRecovery: Recovery),
                new SeededRandom(3), strike: new StrikeRules(0, 2, 0.33, 1));

            // O mesmo toque nos mesmos instantes: a cada 0,25 s de jogo.
            var a = Make();
            var b = Make();
            for (int i = 0; i < 240; i++)
            {
                a.Tap();
                b.Tap();
                a.Tick(0.25);
                for (int k = 0; k < 15; k++) b.Tick(0.25 / 15);
            }

            Assert.That(b.State.Breaks, Is.EqualTo(a.State.Breaks));
            Assert.That(b.Miner.Distance, Is.EqualTo(a.Miner.Distance).Within(1e-6));
            Assert.That(b.Miner.State, Is.EqualTo(a.Miner.State));
        }
    }
}
