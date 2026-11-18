using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>
    /// D-126: a bomba quebra o cristal no meio da picaretada e o anão termina o golpe parado antes de correr (era o toque,
    /// que saiu no M9g, D-311; a bomba herdou a regra). Golpe na chegada (A2, D-124), corrida de 4 un/s, alcance 1,
    /// intervalo 2 s, volta da picareta 0,72 s, dano 5. A bomba destes testes tira 100% e chega na hora (voo 0), com
    /// alcance de 1,5 un: o anão parado a 2 só pega o cristal a 3, não o de 5. Um cristal a 3 un fica a 0,5 s de corrida.
    /// </summary>
    public class SwingRecoveryTests
    {
        const double Recovery = 0.72;

        static StrikeRules Strike(double reach = 1.5) =>
            new StrikeRules(0, 2, bombs: new BombRules(new[] { 1.0 }, 5, 0, reach));

        static MiningSimulation Sim(double recovery = Recovery, double interval = 2, double health = 10, double reach = 1.5) =>
            new MiningSimulation(
                new MiningRules(0, 3, 12, 1e9, 2, 12, health, 1, 5, interval, 4, 1,
                                arrivalStrike: ArrivalStrike.Immediate, swingRecovery: recovery),
                new SeededRandom(1), strike: Strike(reach));

        /// <summary>Enche e lança a bomba (voo 0: chega na hora) e confere que o alvo caiu.</summary>
        static void BombTheTarget(MiningSimulation sim)
        {
            var target = sim.Miner.Target;
            sim.FillBomb();
            Assert.That(sim.ThrowBomb(), Is.True, "a bomba sai");
            Assert.That(sim.Miner.Target, Is.Not.SameAs(target), "a bomba quebrou o alvo");
        }

        [Test]
        public void BombBreakMidSwing_FinishesTheSwingStanding_ThenRuns()
        {
            var sim = Sim();
            sim.SpawnCrystalAt(3);
            sim.SpawnCrystalAt(5);
            sim.Tick(0.5); // chega e bate (A2): 5 de 10
            sim.Tick(0.2);
            BombTheTarget(sim);
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
        public void BombBreakAfterTheSwingEnded_RunsAtOnce()
        {
            var sim = Sim();
            sim.SpawnCrystalAt(3);
            sim.SpawnCrystalAt(5);
            sim.Tick(0.5 + Recovery + 0.1); // a volta da picareta já acabou

            BombTheTarget(sim);
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Running));
        }

        [Test]
        public void BombBreakBeforeArriving_KeepsRunning()
        {
            var sim = Sim(reach: 2.5);
            sim.SpawnCrystalAt(3);
            sim.SpawnCrystalAt(5);
            sim.Tick(0.2); // correndo; o alvo (a 2,2 un) está no alcance da bomba, o de 5 não

            BombTheTarget(sim);
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
        public void ABombThatAlsoBreaksTheNextCrystal_DoesNotStopTheSwing()
        {
            var sim = Sim(reach: 3.5); // pega o de 3 e o de 5
            sim.SpawnCrystalAt(3);
            sim.SpawnCrystalAt(5);
            sim.Tick(0.5);

            BombTheTarget(sim);
            Assert.That(sim.State.Breaks, Is.EqualTo(2), "os dois caem");
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Recovering), "continua terminando o golpe");
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
            BombTheTarget(sim);
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

            BombTheTarget(sim);
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Running));
        }

        /// <summary>A bomba automática (carga de 1 s, voo de 0,4 s, 30% da vida, alcance 4) no passo grande e nos pequenos.</summary>
        [Test]
        public void OneBigTick_EqualsManySmallOnes_WithTheAutomaticBomb()
        {
            MiningSimulation Make()
            {
                var sim = new MiningSimulation(
                    new MiningRules(10, 3, 12, 5, 2, 12, 10, 1, 5, 2, 4, 1, minCrystals: 10,
                                    arrivalStrike: ArrivalStrike.Immediate, swingRecovery: Recovery),
                    new SeededRandom(3), strike: new StrikeRules(0, 2, bombs: new BombRules(new[] { 0.3 }, 1, 0.4, 4)));
                sim.AutoBomb = true;
                return sim;
            }

            var a = Make();
            var b = Make();
            for (int i = 0; i < 240; i++)
            {
                a.Tick(0.25);
                for (int k = 0; k < 15; k++) b.Tick(0.25 / 15);
            }

            Assert.That(a.State.Breaks, Is.GreaterThan(0));
            Assert.That(b.State.Breaks, Is.EqualTo(a.State.Breaks));
            Assert.That(b.Miner.Distance, Is.EqualTo(a.Miner.Distance).Within(1e-6));
            Assert.That(b.Miner.State, Is.EqualTo(a.Miner.State));
        }
    }
}
