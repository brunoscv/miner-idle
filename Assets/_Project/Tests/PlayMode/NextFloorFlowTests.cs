using System.Collections;
using MinersIdle.Game;
using MinersIdle.Presentation;
using MinersIdle.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9c E2 (D-269): o Próximo Andar na cena Game. O botão é a barra: enchendo, "PRÓXIMO ANDAR" e "x/y", a madeira acende
    /// com os cristais quebrados e o toque não faz nada; cheio, "DESCER · ANDAR n" e o toque desce, a barra zera. No HUD,
    /// "ANDAR" (P9). DBG → Andar → Transição curta. O runner fica parado: só os cristais que o teste quebra contam.
    /// </summary>
    public class NextFloorFlowTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        MiningRunner _runner;
        NextFloorButton _next;
        DebugPanel _panel;

        IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null; // um quadro de layout
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            _next = Object.FindAnyObjectByType<NextFloorButton>();
            _panel = Object.FindAnyObjectByType<DebugPanel>();
            Assert.That(_next, Is.Not.Null, "a cena precisa do Próximo Andar (rode o setup do projeto)");
            _runner.UseRules(_runner.Rules); // partida nova, sem o que os primeiros quadros quebraram
            _runner.TimeScale = 0f;
            yield return null;
        }

        Button TheButton => _next.transform.Find("Bar/NextFloorButton").GetComponent<Button>();

        void BreakOne()
        {
            var simulation = _runner.Simulation;
            simulation.Tick(0);
            Assert.That(simulation.BreakTarget(), Is.True, "um cristal na mira");
            // Um raro abre a janela das cartas (D-223): escolhe a primeira e fecha, e o jogo segue.
            if (simulation.RareWindow.IsChoosing) simulation.ChooseRareCard(0);
            if (simulation.RareWindow.IsOpen) simulation.CloseRareWindow();
        }

        [UnityTest]
        public IEnumerator FillsWithTheBreaks_ThenDescends_AndStartsOver()
        {
            yield return Load();
            var simulation = _runner.Simulation;
            Assert.That(simulation.State.Cave, Is.EqualTo(1));
            Assert.That(_next.IsShown, Is.True);
            Assert.That(_next.IsReady, Is.False);
            Assert.That(_next.CountText, Is.EqualTo("0/10"), "o andar 1 pede 10 cristais (D-239)");
            Assert.That(_next.FillFraction, Is.EqualTo(0f));

            // Antes de encher, o toque não faz nada.
            Assert.That(TheButton.interactable, Is.False);
            TheButton.onClick.Invoke();
            Assert.That(simulation.State.Cave, Is.EqualTo(1), "o botão não responde antes de encher");

            for (int i = 0; i < 4; i++) BreakOne();
            Assert.That(_next.CountText, Is.EqualTo("4/10"));
            Assert.That(_next.FillFraction, Is.EqualTo(0.4f).Within(1e-4f), "a madeira acende junto");
            Assert.That(_next.IsReady, Is.False);

            for (int i = 0; i < 6; i++) BreakOne();
            Assert.That(_next.IsReady, Is.True, "cheio: o conteúdo vira DESCER");
            Assert.That(_next.TargetText, Is.EqualTo("ANDAR 2"));
            Assert.That(_next.FillFraction, Is.EqualTo(1f));
            Assert.That(TheButton.interactable, Is.True);

            TheButton.onClick.Invoke();
            Assert.That(simulation.State.Cave, Is.EqualTo(2), "o toque desce");
            Assert.That(_next.IsReady, Is.False);
            Assert.That(_next.CountText, Is.EqualTo("0/20"), "a barra zera; o andar 2 pede 20");
            Assert.That(_next.FillFraction, Is.EqualTo(0f));
            Assert.That(TheButton.interactable, Is.False, "na transição, nada");
        }

        [UnityTest]
        public IEnumerator Hud_SaysFloor()
        {
            yield return Load();
            var hud = Object.FindAnyObjectByType<HudView>();
            var label = hud.transform.Find("Caverna/Label").GetComponent<TMP_Text>();
            Assert.That(label.text, Is.EqualTo("ANDAR"), "P9 (rode o setup do projeto)");
        }

        [UnityTest]
        public IEnumerator ShortTransition_TakesAFractionOfASecond()
        {
            yield return Load();
            var simulation = _runner.Simulation;
            var transition = Object.FindAnyObjectByType<FloorTransition>();
            var toggle = _panel.transform.Find("CaveMenu/Content/Buttons/ShortTransition");
            Assert.That(toggle, Is.Not.Null, "DBG → Andar → Transição curta (rode o setup do projeto)");
            var label = toggle.GetComponentInChildren<TMP_Text>();
            Assert.That(label.text, Is.EqualTo("Transição curta: não"));

            toggle.GetComponent<Button>().onClick.Invoke();
            Assert.That(label.text, Is.EqualTo("Transição curta: sim"));
            Assert.That(transition.IsShort, Is.True);

            simulation.FillFloorBar();
            TheButton.onClick.Invoke();
            Assert.That(simulation.IsInFloorTransition, Is.True);
            Assert.That(simulation.FloorTransitionLength, Is.EqualTo(transition.ShortSeconds).Within(1e-6));

            _runner.TimeScale = 1f;
            for (float t = 0; t < 2f && simulation.IsInFloorTransition; t += Time.unscaledDeltaTime) yield return null;
            Assert.That(simulation.IsInFloorTransition, Is.False, "acaba logo");

            toggle.GetComponent<Button>().onClick.Invoke();
            Assert.That(transition.IsShort, Is.False);
            Assert.That(simulation.FloorTransitionSeconds, Is.EqualTo(transition.DurationOf(transition.Style)).Within(1e-6),
                        "desligada, volta a de sempre");
        }
    }
}
