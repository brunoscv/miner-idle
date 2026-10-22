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
        public IEnumerator TapAndCritical_ShowSparksAndTheCriticalText()
        {
            yield return LoadGame();
            _runner.ForceCritical = true;

            _runner.Simulation.Tap();

            Assert.That(_sparks.ActiveCount, Is.GreaterThan(0), "faíscas");
            Assert.That(_floating.ActiveCount, Is.GreaterThan(0), "texto do crítico");
            Assert.That(_floating.LastText, Is.Not.Empty);
            Assert.That(_shake.IsShaking, Is.True, "crítico treme a tela");

            yield return new WaitForSecondsRealtime(1f);
            Assert.That(_floating.ActiveCount, Is.EqualTo(0), "o texto some sozinho");
            Assert.That(_sparks.ActiveCount, Is.EqualTo(0), "as faíscas voltam ao pool");
        }

        [UnityTest]
        public IEnumerator Combo_AppearsFromTheSecondTap_AndHidesAfterOneSecond()
        {
            yield return LoadGame();
            var simulation = _runner.Simulation;

            simulation.Tap();
            Assert.That(_feedback.IsComboShown, Is.False, "x1 não aparece");
            simulation.Tap();
            simulation.Tap();
            Assert.That(_feedback.IsComboShown, Is.True);
            Assert.That(_feedback.ComboShown, Does.Contain("3"));

            yield return new WaitForSecondsRealtime(1.3f); // a janela do combo é de 1 s de jogo
            Assert.That(_feedback.IsComboShown, Is.False);
        }

        [UnityTest]
        public IEnumerator ComboMilestone_PopsABigText()
        {
            yield return LoadGame();
            for (int i = 0; i < 10; i++) _runner.Simulation.Tap();

            Assert.That(_floating.ActiveCount, Is.GreaterThan(0));
            Assert.That(_floating.LastText, Does.Contain("10"));
        }

        [UnityTest]
        public IEnumerator Break_SendsAShardToTheHud_WhichPulses()
        {
            yield return LoadGame();
            var hud = Object.FindAnyObjectByType<HudView>();
            bool arrived = false;
            _flight.Arrived += () => arrived = true;

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
            _runner.Simulation.Tap();
            _runner.Simulation.BreakTarget();
            yield return null;

            Assert.That(_shake.IsShaking, Is.False);
            Assert.That(camera.localPosition, Is.EqualTo(before));
        }
    }
}
