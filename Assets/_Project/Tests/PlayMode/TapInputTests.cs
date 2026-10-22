using System.Collections;
using MinersIdle.Game;
using MinersIdle.Presentation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MinersIdle.Tests
{
    /// <summary>Marco 5, E2: toque no mapa conta; toque em HUD, LOJA ou DBG não conta (D-47).</summary>
    public class TapInputTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        TapInput _input;
        MiningRunner _runner;

        IEnumerator LoadGame()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null; // um quadro de layout para a UI ter posição

            _input = Object.FindAnyObjectByType<TapInput>();
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            Assert.That(_input, Is.Not.Null, "a cena Game precisa do leitor de toque (rode o setup do projeto)");
        }

        /// <summary>O centro de um elemento de UI em coordenadas de tela (Canvas em Screen Space – Overlay).</summary>
        static Vector2 ScreenCenterOf(string name)
        {
            var go = GameObject.Find(name);
            Assert.That(go, Is.Not.Null, name);
            var rect = (RectTransform)go.transform;
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        }

        [UnityTest]
        public IEnumerator TapOnTheMap_Counts()
        {
            yield return LoadGame();
            var simulation = _runner.Simulation;
            int combo = simulation.Combo;

            bool counted = _input.TapAt(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));

            Assert.That(counted, Is.True);
            Assert.That(simulation.Combo, Is.EqualTo(combo + 1));
        }

        [UnityTest]
        public IEnumerator TapOnUi_DoesNotCount([Values("ShopButton", "Lascas", "Toggle")] string element)
        {
            yield return LoadGame();
            var simulation = _runner.Simulation;
            int combo = simulation.Combo;

            bool counted = _input.TapAt(ScreenCenterOf(element));

            Assert.That(counted, Is.False, element + " é interface");
            Assert.That(simulation.Combo, Is.EqualTo(combo));
        }

        [UnityTest]
        public IEnumerator TappingBreaksCrystalsFaster_AndAutoTapRuns()
        {
            yield return LoadGame();
            var simulation = _runner.Simulation;

            _input.AutoTap = true;
            yield return new WaitForSecondsRealtime(1.2f); // ~6 toques
            _input.AutoTap = false;

            Assert.That(simulation.Combo, Is.GreaterThanOrEqualTo(4), "toques automáticos sobem o combo");
        }

        [UnityTest]
        public IEnumerator ForceCritical_SurvivesARestart()
        {
            yield return LoadGame();
            _runner.ForceCritical = true;
            _runner.StartNew(_runner.Seed);

            Assert.That(_runner.Simulation.ForceCritical, Is.True);
            _runner.ForceCritical = false;
            Assert.That(_runner.Simulation.ForceCritical, Is.False);
        }
    }
}
