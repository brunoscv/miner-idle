using System;
using System.Collections;
using MinersIdle.Game;
using MinersIdle.Presentation;
using MinersIdle.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MinersIdle.Tests
{
    /// <summary>
    /// Marco 6, E1: a cena da descida abre com a câmera de frente (D-50) desenhando numa textura de baixa resolução pelo
    /// renderer Universal, o loop visual anda (corre, para, quebra, volta a correr) e um quadro não aloca memória.
    /// </summary>
    public class DescentTests
    {
        const string ScenePath = "Assets/_Project/Scenes/Descida.unity";

        DescentStage _stage;

        IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            _stage = Object.FindAnyObjectByType<DescentStage>();
            Assert.That(_stage, Is.Not.Null, "a cena Descida precisa do DescentStage (rode o setup do projeto)");
        }

        static int Allocations(Action action)
        {
            var recorder = Recorder.Get("GC.Alloc");
            recorder.FilterToCurrentThread();
            recorder.enabled = false;
            recorder.enabled = true;
            action();
            recorder.enabled = false;
            int count = recorder.sampleBlockCount;
            recorder.CollectFromAllThreads();
            return count;
        }

        [Test]
        public void ValidatedFraming_PutsTheDwarfWhereTheSimulatorDid()
        {
            var framing = DescentFraming.Validated;
            const float aspect = DescentFraming.ReferenceAspect;

            Assert.That(framing.VerticalFieldOfView(aspect), Is.EqualTo(40f).Within(0.01f), "9:16: o campo de visão é o validado");
            Assert.That(framing.FromTop(DescentFraming.DwarfHeight, framing.distance, aspect), Is.EqualTo(0.44f).Within(0.01f), "cabeça");
            Assert.That(framing.FromTop(0f, framing.distance, aspect), Is.EqualTo(0.78f).Within(0.01f), "pés");
            Assert.That(framing.GroundAhead(aspect), Is.EqualTo(1.37f).Within(0.05f), "chão à frente dos pés");
            Assert.That(framing.VerticalFieldOfView(9f / 20f), Is.GreaterThan(40f), "tela mais alta: mesma largura, mais altura");
        }

        [UnityTest]
        public IEnumerator Scene_RendersLowResolution_WithTheUniversalRenderer()
        {
            yield return Load();
            yield return null; // a imagem da tela pega a textura no LateUpdate

            var camera = _stage.SceneCamera;
            Assert.That(_stage.Output, Is.Not.Null);
            Assert.That(camera.targetTexture, Is.SameAs(_stage.Output));
            Assert.That(_stage.Output.width, Is.EqualTo(Mathf.Min(270, Screen.width)), "270 px de largura (D-14)");
            Assert.That(_stage.Output.filterMode, Is.EqualTo(FilterMode.Point), "ampliada sem suavizar");
            Assert.That(camera.orthographic, Is.False, "câmera de perspectiva");
            Assert.That(camera.GetUniversalAdditionalCameraData().scriptableRenderer, Is.InstanceOf<UniversalRenderer>(),
                        "a perspectiva da descida precisa do renderer Universal");

            var screen = Object.FindAnyObjectByType<DescentScreen>();
            Assert.That(screen.GetComponent<RawImage>().texture, Is.SameAs(_stage.Output), "a imagem cobre a tela");

            _stage.InternalWidth = 0;
            yield return null;
            Assert.That(_stage.Output.width, Is.EqualTo(Screen.width), "Resolução: a do aparelho");
        }

        [UnityTest]
        public IEnumerator Dwarf_RunsStopsBreaksAndRunsAgain()
        {
            yield return Load();
            double start = _stage.Preview.Progress;
            _stage.TimeScale = 10f;

            bool sawMining = false;
            float timeout = Time.realtimeSinceStartup + 10f;
            while (_stage.Preview.Broken < 2 && Time.realtimeSinceStartup < timeout)
            {
                sawMining |= _stage.Preview.Mining;
                yield return null;
            }

            Assert.That(sawMining, Is.True, "parou para bater");
            Assert.That(_stage.Preview.Broken, Is.GreaterThanOrEqualTo(2), "quebrou cristais");
            Assert.That(_stage.Preview.Progress, Is.GreaterThan(start + 4), "correu entre eles");
        }

        [UnityTest]
        public IEnumerator Paused_StopsTheDescent()
        {
            yield return Load();
            _stage.Paused = true;
            double progress = _stage.Preview.Progress;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(_stage.Preview.Progress, Is.EqualTo(progress));
        }

        [UnityTest]
        public IEnumerator AFrame_DoesNotAllocate()
        {
            yield return Load();
            _stage.Paused = true; // só os passos do teste andam
            for (int i = 0; i < 300; i++) _stage.Advance(0.05f); // aquece: golpes, quebras e partículas já aconteceram
            int broken = _stage.Preview.Broken;

            int allocations = Allocations(() =>
            {
                for (int i = 0; i < 600; i++) _stage.Advance(1f / 60f); // 10 s: corrida, golpes, quebras, cristais novos
            });

            Assert.That(_stage.Preview.Broken, Is.GreaterThan(broken), "o trecho medido teve quebra");
            Assert.That(allocations, Is.Zero, "um quadro da descida não aloca");
        }

        [UnityTest]
        public IEnumerator GameDebugPanel_OpensTheDescent()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Project/Scenes/Game.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var panel = Object.FindAnyObjectByType<DebugPanel>();
            var button = panel.transform.Find("Content/Buttons/Descent");
            Assert.That(button, Is.Not.Null, "botão Descida (teste) no DBG do jogo");
            button.GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return null;

            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(GameBootstrap.DescentSceneName));
            Assert.That(Object.FindAnyObjectByType<DescentStage>(), Is.Not.Null);
        }
    }
}
