using System.Collections;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M1b (E6, D-75): o anão são as folhas do PixelLab tocadas inteiras. O quadro de impacto aparece no instante do
    /// dano; a corrida nunca mostra a pose parada (quadro 0).
    /// </summary>
    public class DwarfArtTests
    {
        DescentStage _stage;
        MiningRunner _runner;

        IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Project/Scenes/Game.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _stage = Object.FindAnyObjectByType<DescentStage>();
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            yield return null;
            if (_stage.DwarfFrame < 0) Assert.Ignore("Sem as folhas do anão na cena (rode o setup).");
        }

        [UnityTest]
        public IEnumerator ImpactFrame_ShowsAtTheInstantOfTheHit()
        {
            yield return Load();
            var simulation = _runner.Simulation;
            int hits = 0;
            void OnHit(MiningHit hit) => hits++;
            simulation.HitLanded += OnHit;

            int checkedHits = 0;
            float timeout = Time.realtimeSinceStartup + 20f;
            while (checkedHits < 3 && Time.realtimeSinceStartup < timeout)
            {
                int before = hits;
                yield return null; // volta depois do Update deste quadro: o golpe pode ter acontecido agora
                if (hits == before || simulation.Miner.State != MinerState.Mining) continue;
                // O desenho (LateUpdate) do quadro do golpe roda depois desta volta; confere no próximo.
                yield return null;
                Assert.That(_stage.DwarfFrame, Is.EqualTo(_stage.DwarfImpactFrame), "impacto no instante do dano");
                checkedHits++;
            }
            simulation.HitLanded -= OnHit;
            Assert.That(checkedHits, Is.GreaterThan(0), "viu golpes");
        }

        /// <summary>
        /// O toque quebra o cristal antes da picareta descer: o anão faz mesmo assim uma picaretada completa (pedido do
        /// Bruno), com o quadro de impacto, antes de seguir correndo. Só existe com o golpe um intervalo depois da chegada
        /// (Wait); com o golpe na chegada (D-124) ele já bateu, e o caso é o teste seguinte.
        /// </summary>
        [UnityTest]
        public IEnumerator TapBreak_StillShowsAFullSwing()
        {
            yield return Load();
            _runner.ArrivalStrike = ArrivalStrike.Wait;
            var simulation = _runner.Simulation;
            float timeout = Time.realtimeSinceStartup + 20f;
            while (simulation.Miner.State != MinerState.Mining && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(simulation.Miner.State, Is.EqualTo(MinerState.Mining), "chegou num cristal");

            int broken = 0;
            void OnBroken(Crystal crystal) => broken++;
            simulation.CrystalBroken += OnBroken;
            while (broken == 0 && Time.realtimeSinceStartup < timeout) simulation.Tap();
            simulation.CrystalBroken -= OnBroken;
            Assert.That(broken, Is.EqualTo(1), "o toque quebrou o cristal");

            bool finishing = false, impact = false;
            float end = Time.realtimeSinceStartup + 1f;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
                finishing |= _stage.IsFinishing;
                impact |= _stage.IsFinishing && _stage.DwarfFrame == _stage.DwarfImpactFrame;
            }
            Assert.That(finishing, Is.True, "picaretada de despedida");
            Assert.That(impact, Is.True, "com o quadro de impacto");
        }

        /// <summary>
        /// Golpe na chegada (D-124, A2) com o jogador tocando (achado do Bruno em 2026-11-02): o toque quebra cristais o
        /// tempo todo, e a despedida repetia o golpe que o anão já tinha dado, com ele deslizando com a picareta. Agora,
        /// correndo, nunca a despedida: só a passada, o levantar antes da chegada ou a parada da quebra.
        /// </summary>
        [UnityTest]
        public IEnumerator ArrivalStrike_Tapping_RunsStraightWithoutRepeatingTheSwing()
        {
            yield return Load();
            _runner.ArrivalStrike = ArrivalStrike.Immediate;
            _stage.StrikeFeel = true;
            var simulation = _runner.Simulation;

            int breaks = 0;
            void OnBroken(Crystal crystal) => breaks++;
            simulation.CrystalBroken += OnBroken;
            int runningFrames = 0;
            float timeout = Time.realtimeSinceStartup + 20f;
            while ((breaks < 4 || runningFrames < 20) && Time.realtimeSinceStartup < timeout)
            {
                simulation.Tap(); // um toque por quadro: o teto de 15/s segura o resto
                yield return null;
                if (simulation.Miner.State != MinerState.Running) continue;
                runningFrames++;
                Assert.That(_stage.IsFinishing, Is.False, "sem a picaretada de despedida");
                if (_stage.ShowsStrikeWhileRunning)
                    Assert.That(_stage.IsPreparing || _stage.IsHoldingBreak, Is.True, "correndo, só o levantar ou a parada da quebra");
            }
            simulation.CrystalBroken -= OnBroken;

            Assert.That(breaks, Is.GreaterThanOrEqualTo(4), "quebrou cristais tocando");
            Assert.That(runningFrames, Is.GreaterThanOrEqualTo(20), "viu o anão correr entre eles");
        }

        /// <summary>
        /// D-126 (escolha do Bruno em 2026-11-02): o toque quebra o cristal no meio da picaretada e o anão termina o golpe
        /// parado, com a folha de mineração, igual à picaretada no cristal vivo; só depois corre.
        /// </summary>
        [UnityTest]
        public IEnumerator TapBreak_MidSwing_FinishesTheSwingStanding()
        {
            yield return Load();
            _runner.ArrivalStrike = ArrivalStrike.Immediate;
            _stage.StrikeFeel = true;
            var simulation = _runner.Simulation;
            Assume.That(simulation.SwingRecovery, Is.GreaterThan(0), "a volta da picareta ligada nos valores");

            Texture mineSheet = null;
            int recoveringFrames = 0, recoveries = 0;
            double standingAt = double.NaN;
            var last = simulation.Miner.State;
            float timeout = Time.realtimeSinceStartup + 20f;
            while (recoveries < 2 && Time.realtimeSinceStartup < timeout)
            {
                var state = simulation.Miner.State;
                if (state == MinerState.Mining) simulation.Tap(); // só toca depois da picaretada da chegada
                yield return null;
                state = simulation.Miner.State;
                if (state == MinerState.Mining) mineSheet = _stage.DwarfSheet;
                if (state == MinerState.Recovering)
                {
                    if (last != MinerState.Recovering) standingAt = simulation.Miner.Distance;
                    Assert.That(simulation.Miner.Distance, Is.EqualTo(standingAt), "parado terminando o golpe");
                    if (mineSheet != null) Assert.That(_stage.DwarfSheet, Is.SameAs(mineSheet), "o golpe da folha de mineração");
                    recoveringFrames++;
                }
                if (last == MinerState.Recovering && state == MinerState.Running) recoveries++;
                last = state;
            }

            Assert.That(recoveries, Is.GreaterThanOrEqualTo(2), "terminou o golpe e voltou a correr");
            Assert.That(recoveringFrames, Is.GreaterThan(2), "o golpe durou alguns quadros");
        }

        [UnityTest]
        public IEnumerator Running_NeverShowsTheStandingPose()
        {
            yield return Load();
            var simulation = _runner.Simulation;
            _runner.TimeScale = 5f;
            int runningFrames = 0;
            float timeout = Time.realtimeSinceStartup + 20f;
            while (runningFrames < 30 && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
                if (simulation.Miner.State != MinerState.Running) continue;
                if (_stage.IsFinishing) continue; // picaretada de despedida
                if (_stage.ShowsStrikeWhileRunning) // golpe fluido (D-125): levantando a picareta ou parado no impacto da quebra
                {
                    Assert.That(_stage.DwarfFrame, Is.Not.EqualTo(0), "nem no golpe a pose parada");
                    continue;
                }
                // As duas pernas com 3 quadros cada (2-3-4 e 6-7-8): sem a pose parada (0, 1) nem o 5, que mancava.
                Assert.That(new[] { 2, 3, 4, 6, 7, 8 }, Does.Contain(_stage.DwarfFrame), "só a passada, pernas iguais");
                runningFrames++;
            }
            Assert.That(runningFrames, Is.GreaterThan(0), "viu o anão correr");
        }

        /// <summary>
        /// Golpe fluido (D-124, D-125): chegando batendo, o anão levanta a picareta ainda correndo e o impacto aparece na
        /// chegada; depois da parada da quebra a descida desenhada fica atrás da regra e alcança antes do próximo cristal.
        /// </summary>
        [UnityTest]
        public IEnumerator StrikeFeel_RaisesThePickaxeRunning_AndCatchesUpBeforeArriving()
        {
            yield return Load();
            _runner.ArrivalStrike = ArrivalStrike.Immediate;
            _stage.StrikeFeel = true;
            var simulation = _runner.Simulation;

            bool prepared = false, lagged = false;
            int arrivals = 0;
            var last = simulation.Miner.State;
            float timeout = Time.realtimeSinceStartup + 20f;
            while (arrivals < 3 && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
                var state = simulation.Miner.State;
                prepared |= state == MinerState.Running && _stage.ShowsStrikeWhileRunning;
                lagged |= _stage.DrawnLag > 0;
                if (last == MinerState.Running && state == MinerState.Mining)
                {
                    yield return null; // o desenho deste quadro
                    Assert.That(_stage.DrawnLag, Is.LessThan(0.05), "a descida desenhada alcançou a regra na chegada");
                    arrivals++;
                }
                last = simulation.Miner.State;
            }

            Assert.That(arrivals, Is.GreaterThan(0), "viu o anão chegar");
            Assert.That(prepared, Is.True, "levantou a picareta correndo");
            Assert.That(lagged, Is.True, "a quebra segurou a descida um instante");
        }
    }
}
