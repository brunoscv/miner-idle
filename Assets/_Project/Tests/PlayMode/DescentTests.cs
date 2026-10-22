using System;
using System.Collections;
using MinersIdle.Core;
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
    /// Marco 6: a descida na cena Game. Desde a E3 ela é dirigida pela simulação de verdade (na E1 era uma cena separada
    /// com um loop só visual, que saiu pela D-61): a câmera de frente (D-50) desenha numa textura de baixa resolução pelo
    /// renderer Universal, mostrada atrás de tudo pela câmera da tela; o anão corre, para, quebra e volta a correr; um
    /// quadro não aloca memória; os controles da câmera ficam no DBG.
    /// </summary>
    public class DescentTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        DescentStage _stage;
        MiningRunner _runner;

        IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;
            _stage = Object.FindAnyObjectByType<DescentStage>();
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            Assert.That(_stage, Is.Not.Null, "a cena Game precisa da descida (rode o setup do projeto)");
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

            var camera = _stage.SceneCamera;
            Assert.That(_stage.Output, Is.Not.Null);
            Assert.That(camera.targetTexture, Is.SameAs(_stage.Output));
            Assert.That(_stage.Output.width, Is.EqualTo(Mathf.Min(270, Screen.width)), "270 px de largura (D-14)");
            Assert.That(_stage.Output.filterMode, Is.EqualTo(FilterMode.Point), "ampliada sem suavizar");
            Assert.That(camera.orthographic, Is.False, "câmera de perspectiva");
            Assert.That(camera.GetUniversalAdditionalCameraData().scriptableRenderer, Is.InstanceOf<UniversalRenderer>(),
                        "a perspectiva da descida precisa do renderer Universal");

            var screen = _stage.ScreenQuad.GetComponent<MeshRenderer>();
            Assert.That(screen.sharedMaterial.GetTexture("_BaseMap"), Is.SameAs(_stage.Output), "o quadro na tela mostra a descida");
            Assert.That(_stage.ScreenQuad.position.z, Is.GreaterThan(0f), "atrás dos sprites da câmera da tela");

            _stage.InternalWidth = 0;
            yield return null;
            Assert.That(_stage.Output.width, Is.EqualTo(Screen.width), "Resolução: a do aparelho");
            Assert.That(screen.sharedMaterial.GetTexture("_BaseMap"), Is.SameAs(_stage.Output));
        }

        [UnityTest]
        public IEnumerator Dwarf_RunsStopsBreaksAndRunsAgain()
        {
            yield return Load();
            var simulation = _runner.Simulation;
            double start = simulation.Miner.Distance;
            _runner.TimeScale = 20f;

            bool sawMining = false;
            float timeout = Time.realtimeSinceStartup + 10f;
            while (simulation.State.Breaks < 2 && Time.realtimeSinceStartup < timeout)
            {
                sawMining |= simulation.Miner.State == MinerState.Mining;
                yield return null;
            }

            Assert.That(sawMining, Is.True, "parou para bater");
            Assert.That(simulation.State.Breaks, Is.GreaterThanOrEqualTo(2), "quebrou cristais");
            Assert.That(simulation.Miner.Distance, Is.GreaterThan(start), "correu entre eles");
            Assert.That(_stage.DrawnCrystals, Is.EqualTo(Mathf.Min(simulation.Crystals.Count, 20)), "a fila inteira desenhada");
        }

        /// <summary>
        /// A descida só ilustra a simulação: com a partida parada, nada anda. (Na E1, este teste media o "Pausar" do loop
        /// só visual, que saiu com a cena de teste pela D-61.)
        /// </summary>
        [UnityTest]
        public IEnumerator StoppedSimulation_StopsTheDescent()
        {
            yield return Load();
            _runner.enabled = false;
            double distance = _runner.Simulation.Miner.Distance;
            var front = _runner.Simulation.Crystals[0];
            float z = _stage.CrystalLocalPosition(front).z;

            yield return new WaitForSecondsRealtime(0.3f);

            Assert.That(_runner.Simulation.Miner.Distance, Is.EqualTo(distance));
            Assert.That(_stage.CrystalLocalPosition(front).z, Is.EqualTo(z));
        }

        [UnityTest]
        public IEnumerator AFrame_DoesNotAllocate()
        {
            yield return Load();
            var simulation = _runner.Simulation;
            _runner.TimeScale = 0f; // só os Tick do teste andam
            for (int i = 0; i < 300; i++)
            {
                simulation.Tick(0.05);
                _stage.Draw(); // aquece: golpes, quebras e cristais novos já passaram pelo desenho
            }
            long breaks = simulation.State.Breaks;

            int allocations = 0;
            for (int i = 0; i < 600; i++) // 10 s de jogo: corrida, golpes, quebras e cristais novos
            {
                simulation.Tick(1.0 / 60);
                allocations += Allocations(_stage.Draw);
            }

            Assert.That(simulation.State.Breaks, Is.GreaterThan(breaks), "o trecho medido teve quebra");
            Assert.That(allocations, Is.Zero, "desenhar a descida num quadro não aloca");
        }

        /// <summary>M6 (E3): os controles da câmera da E1 vivem no DBG do jogo (D-61).</summary>
        [UnityTest]
        public IEnumerator CameraPanel_ChangesTheFraming_AndResetsToD50()
        {
            yield return Load();
            var panel = Object.FindAnyObjectByType<DescentCameraPanel>();
            Assert.That(panel, Is.Not.Null, "painel de câmera (rode o setup do projeto)");

            void Click(string path) => panel.transform.Find(path).GetComponent<Button>().onClick.Invoke();

            panel.Toggle();
            Assert.That(panel.IsOpen, Is.True);

            Click("Content/Rows/Row0/Plus"); // altura + 0,1
            Click("Content/Rows/Row1/Minus"); // inclinação − 1
            Assert.That(_stage.Framing.height, Is.EqualTo(2.5f).Within(1e-5f));
            Assert.That(_stage.Framing.tilt, Is.EqualTo(14f).Within(1e-5f));
            Assert.That(_stage.SceneCamera.transform.localPosition.y, Is.EqualTo(2.5f).Within(1e-5f), "a câmera mudou na hora");

            Click("Content/Buttons/ResetFraming");
            Assert.That(_stage.Framing.height, Is.EqualTo(DescentFraming.Validated.height));
            Assert.That(_stage.Framing.tilt, Is.EqualTo(DescentFraming.Validated.tilt));

            Click("Content/Buttons/Resolution");
            Assert.That(_stage.InternalWidth, Is.EqualTo(360), "270 → 360");

            Click("Content/Buttons/Close");
            Assert.That(panel.IsOpen, Is.False);
        }
    }
}
