using System.Collections;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using MinersIdle.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MinersIdle.Tests
{
    /// <summary>Marco 5, E3: cada evento tem a sua reação (REQ-UI-05), e o tremor desliga pelo DBG.</summary>
    public class FeedbackTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        MiningRunner _runner;
        MiningFeedbackView _feedback;
        FloatingTexts _floating;
        SparkBurst _sparks;
        ShardFlight _flight;
        ScreenShake _shake;

        IEnumerator LoadGame()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null; // a partida já escolheu um alvo

            _runner = Object.FindAnyObjectByType<MiningRunner>();
            _feedback = Object.FindAnyObjectByType<MiningFeedbackView>();
            _floating = Object.FindAnyObjectByType<FloatingTexts>();
            _sparks = Object.FindAnyObjectByType<SparkBurst>();
            _flight = Object.FindAnyObjectByType<ShardFlight>();
            _shake = Object.FindAnyObjectByType<ScreenShake>();
            Assert.That(_feedback, Is.Not.Null, "a cena Game precisa dos efeitos (rode o setup do projeto)");
            Assert.That(_runner.Simulation.Miner.Target, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator CriticalHit_ShowsSparksAndTheCriticalText()
        {
            yield return LoadGame();
            _runner.ForceCritical = true;
            // O teste anda o tempo até o golpe (era o toque, que saiu no M9g, D-311).
            _runner.TimeScale = 0f;

            _runner.Simulation.Tick(_runner.Simulation.SecondsToNextHit + 1e-6);

            Assert.That(_sparks.ActiveCount, Is.GreaterThan(0), "faíscas");
            Assert.That(_floating.ActiveCount, Is.GreaterThan(0), "texto do crítico");
            Assert.That(_floating.LastText, Is.Not.Empty);
            Assert.That(_shake.IsShaking, Is.True, "crítico treme a tela");

            yield return new WaitForSecondsRealtime(1f);
            Assert.That(_floating.ActiveCount, Is.EqualTo(0), "o texto some sozinho");
            Assert.That(_sparks.ActiveCount, Is.EqualTo(0), "as faíscas voltam ao pool");
        }

        [UnityTest]
        public IEnumerator Break_SendsAShardToTheHud_WhichPulses()
        {
            yield return LoadGame();
            var hud = Object.FindAnyObjectByType<HudView>();
            bool arrived = false;
            _flight.Arrived += () => arrived = true;
            _runner.TimeScale = 0f; // só a quebra do teste: no v7 o anão quebra um cristal a cada golpe

            _runner.Simulation.BreakTarget();
            Assert.That(_flight.ActiveCount, Is.EqualTo(1), "uma lasca a caminho");

            float waited = 0f;
            while (!arrived && waited < 2f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.That(arrived, Is.True, "a lasca chegou ao HUD");
            Assert.That(hud.IsShardsPulsing, Is.True, "LASCAS pulsa na chegada");
            Assert.That(_flight.ActiveCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator ShakeOff_KeepsTheCameraStill()
        {
            yield return LoadGame();
            var camera = _shake.transform;
            Vector3 before = camera.localPosition;
            _shake.ShakeEnabled = false;

            _runner.ForceCritical = true;
            _runner.TimeScale = 0f;
            _runner.Simulation.Tick(_runner.Simulation.SecondsToNextHit + 1e-6); // um golpe crítico
            _runner.Simulation.BreakTarget();
            yield return null;

            Assert.That(_shake.IsShaking, Is.False);
            Assert.That(camera.localPosition, Is.EqualTo(before));
        }

        /// <summary>
        /// M6 (E4): na vista de frente a quebra ganha estilhaços e tremor, e o ajuste "Intensidade dos efeitos" do DBG
        /// multiplica as peças. Sem clarão de tela (D-83).
        /// </summary>
        [UnityTest]
        public IEnumerator Break_ShakesAndBursts_StrongerWithIntensity()
        {
            yield return LoadGame();
            _runner.TimeScale = 0f; // o anão parado: o golpe da chegada (D-124) soltaria faíscas no meio da conta

            _feedback.Intensity = 1f;
            _runner.Simulation.BreakTarget();
            int normal = _sparks.ActiveCount;
            Assert.That(_shake.IsShaking, Is.True, "a tela treme");
            Assert.That(_feedback.transform.Find("Flash"), Is.Null, "a tela não pisca (D-83)");

            yield return new WaitForSecondsRealtime(1f); // as peças voltam ao pool
            Assert.That(_sparks.ActiveCount, Is.EqualTo(0));

            _feedback.Intensity = 2f;
            _runner.Simulation.BreakTarget();
            Assert.That(_sparks.ActiveCount, Is.EqualTo(normal * 2), "intensidade ×2, o dobro de peças");

            _feedback.Intensity = 10f;
            Assert.That(_feedback.Intensity, Is.EqualTo(3f), "a intensidade tem teto");
        }
    }
}
