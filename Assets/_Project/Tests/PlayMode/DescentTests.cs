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
        public void F1Framing_PutsTheDwarfAboveTheQueue()
        {
            var framing = DescentFraming.F1;
            const float aspect = DescentFraming.ReferenceAspect;

            Assert.That(framing.VerticalFieldOfView(aspect), Is.EqualTo(40f).Within(0.01f), "9:16: o campo de visão é o validado");
            Assert.That(framing.FromTop(DescentFraming.DwarfHeight, framing.distance, aspect), Is.EqualTo(0.37f).Within(0.01f), "cabeça (F1, D-69)");
            Assert.That(framing.FromTop(0f, framing.distance, aspect), Is.EqualTo(0.64f).Within(0.01f), "pés (F1, D-69)");
            Assert.That(framing.GroundAhead(aspect), Is.EqualTo(1.98f).Within(0.05f), "chão à frente dos pés");
            Assert.That(framing.VerticalFieldOfView(9f / 20f), Is.GreaterThan(40f), "tela mais alta: mesma largura, mais altura");
        }

        /// <summary>
        /// M9h (D-245): a câmera do GDD v8 (60°, 5,5 m, 3 m, 60°) olha direto para o anão, que fica no meio da tela, menor que
        /// na F1; embaixo dele aparecem 3 m de chão numa tela 9:16 e ~3,6 m no Moto G75 (9:20), um cristal de cada vez no
        /// espaçamento de 3 m (D-277). É a câmera com que o jogo abre.
        /// </summary>
        [Test]
        public void GddV8Framing_PutsTheDwarfInTheMiddle_AndIsTheDefault()
        {
            var framing = DescentFraming.GddV8;
            const float aspect = DescentFraming.ReferenceAspect;
            const float phone = 9f / 20f;

            Assert.That(DescentFraming.Default.SameView(framing), Is.True, "o jogo abre na câmera do v8");
            Assert.That(framing.VerticalFieldOfView(aspect), Is.EqualTo(60f).Within(0.01f));
            Assert.That(framing.FromTop(DescentFraming.DwarfHeight, framing.distance, aspect), Is.EqualTo(0.41f).Within(0.01f), "cabeça");
            Assert.That(framing.FromTop(0f, framing.distance, aspect), Is.EqualTo(0.52f).Within(0.01f), "pés");
            Assert.That(framing.GroundAhead(aspect), Is.EqualTo(3f).Within(0.05f), "chão à frente dos pés, 9:16");
            Assert.That(framing.FromTop(DescentFraming.DwarfHeight, framing.distance, phone), Is.EqualTo(0.43f).Within(0.01f), "cabeça, 9:20");
            Assert.That(framing.FromTop(0f, framing.distance, phone), Is.EqualTo(0.52f).Within(0.01f), "pés, 9:20");
            Assert.That(framing.GroundAhead(phone), Is.EqualTo(3.56f).Within(0.05f), "chão à frente dos pés, 9:20");

            var f1 = DescentFraming.F1;
            float v8Size = framing.FromTop(0f, framing.distance, aspect) - framing.FromTop(DescentFraming.DwarfHeight, framing.distance, aspect);
            float f1Size = f1.FromTop(0f, f1.distance, aspect) - f1.FromTop(DescentFraming.DwarfHeight, f1.distance, aspect);
            Assert.That(v8Size, Is.LessThan(f1Size * 0.5f), "o anão fica com menos da metade da altura da F1");
            Assert.That(framing.Clamped().SameView(framing), Is.True, "os limites do DBG não cortam a câmera do v8");
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
            _runner.UseRules(TestBalance.GddV5); // no v7 o cristal da caverna 1 morre num golpe: o anão nem para
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

            // A corrotina volta depois do Update da partida (que pode criar um cristal) e antes do LateUpdate que desenha:
            // para a partida e espera um quadro, para comparar o desenho com a mesma fila.
            _runner.enabled = false;
            yield return null;
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

        /// <summary>M6 (E3): os controles da câmera da E1 vivem no DBG do jogo (D-61). Desde o M9h o Padrão é a câmera do v8.</summary>
        [UnityTest]
        public IEnumerator CameraPanel_ChangesTheFraming_AndResetsToDefault()
        {
            yield return Load();
            var panel = Object.FindAnyObjectByType<DescentCameraPanel>();
            Assert.That(panel, Is.Not.Null, "painel de câmera (rode o setup do projeto)");

            void Click(string path) => panel.transform.Find(path).GetComponent<Button>().onClick.Invoke();

            panel.Toggle();
            Assert.That(panel.IsOpen, Is.True);

            Click("Content/Rows/Row0/Plus"); // altura + 0,1
            Click("Content/Rows/Row1/Minus"); // inclinação − 1
            var validated = DescentFraming.Default;
            Assert.That(_stage.Framing.height, Is.EqualTo(validated.height + 0.1f).Within(1e-5f));
            Assert.That(_stage.Framing.tilt, Is.EqualTo(validated.tilt - 1f).Within(1e-5f));
            Assert.That(_stage.SceneCamera.transform.localPosition.y, Is.EqualTo(validated.height + 0.1f).Within(1e-5f), "a câmera mudou na hora");

            Click("Content/Buttons/ResetFraming");
            Assert.That(_stage.Framing.height, Is.EqualTo(DescentFraming.Default.height));
            Assert.That(_stage.Framing.tilt, Is.EqualTo(DescentFraming.Default.tilt));

            Click("Content/Buttons/Resolution");
            Assert.That(_stage.InternalWidth, Is.EqualTo(360), "270 → 360");

            Click("Content/Buttons/Close");
            Assert.That(panel.IsOpen, Is.False);
        }

        /// <summary>
        /// D-148: na tela do Moto G75 (9:20), cada recuo põe o anão mais alto na tela e mostra mais chão à frente dele (mais
        /// cristais, um por metro), sem a cabeça sair por cima. Hoje: pés a 61% e ~2,4 m; no recuo de 80%: pés a ~35% e ~6,4 m.
        /// </summary>
        [Test]
        public void Setbacks_PutTheDwarfHigher_AndShowMoreGroundAhead()
        {
            const float phone = 9f / 20f;
            float lastFeet = float.MaxValue, lastGround = -1f;
            for (int i = 0; i < DescentCameraPanel.Setbacks.Length; i++)
            {
                var framing = DescentFraming.F1;
                framing.distance = DescentCameraPanel.SetbackDistance(i);
                float head = framing.FromTop(DescentFraming.DwarfHeight, framing.distance, phone);
                float feet = framing.FromTop(0f, framing.distance, phone);
                float ground = framing.GroundAhead(phone);
                Assert.That(feet, Is.LessThan(lastFeet), $"recuo {i}: o anão sobe na tela");
                Assert.That(ground, Is.GreaterThan(lastGround), $"recuo {i}: mais chão à frente");
                Assert.That(head, Is.GreaterThan(0.15f), $"recuo {i}: a cabeça fica abaixo do HUD");
                lastFeet = feet;
                lastGround = ground;
            }
            Assert.That(DescentCameraPanel.SetbackDistance(DescentCameraPanel.Setbacks.Length - 1), Is.EqualTo(9f), "80% de 5 m");
            Assert.That(lastGround, Is.GreaterThan(6f), "no recuo de 80%, mais de 6 m de chão (6 cristais)");
        }

        /// <summary>D-148: o botão Recuo passa pelos recuos e volta; a névoa recua junto; fora dos recuos, mostra "Recuo ?".</summary>
        [UnityTest]
        public IEnumerator CameraPanel_Setback_CyclesTheDistance_AndTheFogFollows()
        {
            yield return Load();
            var panel = Object.FindAnyObjectByType<DescentCameraPanel>();
            var button = panel.transform.Find("Content/Buttons/Setback");
            Assert.That(button, Is.Not.Null, "botão Recuo (rode o setup do projeto)");
            var label = button.GetComponentInChildren<TMPro.TMP_Text>();
            panel.Toggle();
            _stage.Framing = DescentFraming.F1; // o recuo é uma ferramenta da F1 (D-148); o jogo abre na do v8 (M9h)
            yield return null;

            float fogStart = Shader.GetGlobalFloat("_DescentFogStart");
            float fogEnd = Shader.GetGlobalFloat("_DescentFogEnd");
            Assert.That(panel.SetbackLook, Is.EqualTo(0), "abre sem recuo (a F1)");

            float[] expected = { 6f, 6.5f, 7f, 7.5f, 8f, 8.5f, 9f, 5f };
            string[] labels = { "Recuo 20%", "Recuo 30%", "Recuo 40%", "Recuo 50%", "Recuo 60%", "Recuo 70%", "Recuo 80%", "Recuo 0%" };
            for (int i = 0; i < expected.Length; i++)
            {
                button.GetComponent<Button>().onClick.Invoke();
                Assert.That(_stage.Framing.distance, Is.EqualTo(expected[i]).Within(1e-4f));
                float shift = expected[i] - DescentFraming.F1.distance;
                Assert.That(Shader.GetGlobalFloat("_DescentFogStart"), Is.EqualTo(fogStart + shift).Within(1e-4f), "a névoa recua junto");
                Assert.That(Shader.GetGlobalFloat("_DescentFogEnd"), Is.EqualTo(fogEnd + shift).Within(1e-4f));
                yield return null;
                Assert.That(label.text, Is.EqualTo(labels[i]));
            }

            panel.transform.Find("Content/Rows/Row2/Plus").GetComponent<Button>().onClick.Invoke(); // Distância + 0,1
            yield return null;
            Assert.That(panel.SetbackLook, Is.EqualTo(-1));
            Assert.That(label.text, Is.EqualTo("Recuo ?"), "fora dos recuos");
            button.GetComponent<Button>().onClick.Invoke();
            Assert.That(_stage.Framing.distance, Is.EqualTo(5f).Within(1e-4f), "fora dos recuos, volta ao primeiro");
        }

        /// <summary>
        /// M9h: o jogo abre na câmera do v8 (D-245, o setup migra a cena); o botão Câmera troca para a F1 e volta, e o Padrão
        /// volta para a do v8. A névoa chega 2 m mais perto na do v8, para o anão (a 3 m) ficar com a luz que tinha na F1.
        /// </summary>
        [UnityTest]
        public IEnumerator CameraPanel_Preset_SwitchesBetweenV8AndF1()
        {
            yield return Load();
            Assert.That(_stage.Framing.SameView(DescentFraming.GddV8), Is.True, "a cena abre na câmera do v8 (rode o setup do projeto)");
            Assert.That(_stage.SceneCamera.transform.localPosition.y, Is.EqualTo(5.5f).Within(1e-4f));
            Assert.That(_stage.SceneCamera.transform.localEulerAngles.x, Is.EqualTo(60f).Within(1e-3f));
            Assert.That(_stage.FogShift, Is.EqualTo(-2f).Within(1e-4f), "a névoa acompanha o anão a 3 m");

            var panel = Object.FindAnyObjectByType<DescentCameraPanel>();
            var button = panel.transform.Find("Content/Buttons/Preset");
            Assert.That(button, Is.Not.Null, "botão Câmera (rode o setup do projeto)");
            var label = button.GetComponentInChildren<TMPro.TMP_Text>();
            panel.Toggle();
            yield return null;
            Assert.That(panel.PresetLook, Is.EqualTo(0));
            Assert.That(label.text, Is.EqualTo("Câmera v8"));

            button.GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(_stage.Framing.SameView(DescentFraming.F1), Is.True, "v8 → F1");
            Assert.That(label.text, Is.EqualTo("Câmera F1"));
            Assert.That(_stage.FogShift, Is.EqualTo(0f).Within(1e-4f));

            panel.transform.Find("Content/Rows/Row1/Plus").GetComponent<Button>().onClick.Invoke(); // inclinação + 1
            yield return null;
            Assert.That(panel.PresetLook, Is.EqualTo(-1));
            Assert.That(label.text, Is.EqualTo("Câmera ?"), "mexida no ±");

            button.GetComponent<Button>().onClick.Invoke();
            Assert.That(_stage.Framing.SameView(DescentFraming.GddV8), Is.True, "fora delas, volta para a do v8");

            button.GetComponent<Button>().onClick.Invoke();
            panel.transform.Find("Content/Buttons/ResetFraming").GetComponent<Button>().onClick.Invoke();
            Assert.That(_stage.Framing.SameView(DescentFraming.GddV8), Is.True, "o Padrão é a câmera do v8");
        }
    }
}
