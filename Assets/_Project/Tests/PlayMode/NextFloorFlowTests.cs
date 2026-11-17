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
    /// M9c E3: os marcos de andar na tela: o selo AUTO e a descida sozinha depois do 5 (P6), a linha dos AJUSTES travada
    /// com "Andar 5" até lá, o Estagiário da LOJA com "Andar 5" no lugar do preço (P7) e o DBG → Andar → Recorde e Auto.
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
        Transform DebugButton(string name) => _panel.transform.Find("CaveMenu/Content/Buttons/" + name);
        static string LabelOf(Transform button) => button.GetComponentInChildren<TMP_Text>().text;
        static void Click(Transform button) => button.GetComponent<Button>().onClick.Invoke();

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

        // ── M9c E3: os marcos de andar ─────────────────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator Record5_ShowsAuto_AndTheFullBarDescendsAlone()
        {
            yield return Load();
            var simulation = _runner.Simulation;
            var settings = Object.FindAnyObjectByType<GameSettings>();
            settings.AutoAdvance = true;
            var record = DebugButton("FloorRecord");
            var auto = DebugButton("FloorAuto");
            Assert.That(record, Is.Not.Null, "DBG → Andar → Recorde (rode o setup do projeto)");
            Assert.That(auto, Is.Not.Null, "DBG → Andar → Auto (rode o setup do projeto)");
            Assert.That(LabelOf(record), Is.EqualTo("Recorde: 1"));
            Assert.That(LabelOf(auto), Is.EqualTo("Auto: ligado"));
            yield return null;
            Assert.That(_next.IsAutoShown, Is.False, "antes do andar 5, sem AUTO");

            Click(record);
            Assert.That(LabelOf(record), Is.EqualTo("Recorde: 5"));
            Assert.That(simulation.DeepestFloor, Is.EqualTo(5));
            yield return null;
            Assert.That(_next.IsAutoShown, Is.True, "o selo AUTO no canto");

            for (int i = 0; i < 10; i++) BreakOne();
            simulation.Tick(0);
            Assert.That(simulation.State.Cave, Is.EqualTo(2), "a barra encheu: desceu sozinho, sem toque");
            Assert.That(simulation.IsInFloorTransition, Is.True, "com a transição de sempre");

            Click(auto);
            Assert.That(LabelOf(auto), Is.EqualTo("Auto: desligado"));
            Assert.That(settings.AutoAdvance, Is.False, "o DBG troca o ajuste dos AJUSTES");
            Assert.That(simulation.AutoAdvance, Is.False);
            yield return null;
            Assert.That(_next.IsAutoShown, Is.False);
            Click(auto);
            Assert.That(simulation.AutoAdvance, Is.True);
        }

        [UnityTest]
        public IEnumerator Settings_AutoAdvanceRow_IsLockedWithFloor5_ThenToggles()
        {
            yield return Load();
            var simulation = _runner.Simulation;
            var settings = Object.FindAnyObjectByType<GameSettings>();
            settings.AutoAdvance = true;
            var screen = Object.FindAnyObjectByType<SettingsScreen>();
            screen.Open();
            Assert.That(screen.IsAutoAdvanceLocked, Is.True, "antes do andar 5, travada");
            Assert.That(screen.AutoAdvanceLockedText, Is.EqualTo("Andar 5"));

            screen.Close();
            simulation.SetDeepestFloor(5);
            screen.Open();
            Assert.That(screen.IsAutoAdvanceLocked, Is.False);
            Assert.That(screen.ShowsAutoAdvanceOn, Is.True, "ligado de fábrica");
            screen.ToggleAutoAdvance();
            Assert.That(screen.ShowsAutoAdvanceOn, Is.False);
            Assert.That(simulation.AutoAdvance, Is.False, "o jogo obedece na hora");
            screen.ToggleAutoAdvance();
            Assert.That(simulation.AutoAdvance, Is.True);
            screen.Close();
        }

        [UnityTest]
        public IEnumerator Shop_TheIntern_SaysFloor5_UntilTheRecordGetsThere()
        {
            yield return Load();
            var simulation = _runner.Simulation;
            if (!simulation.Economy.HasIntern) Assert.Ignore("O balanço ativo não tem estagiário.");
            var shop = Object.FindAnyObjectByType<ShopView>();
            shop.Open();
            var intern = System.Linq.Enumerable.First(shop.Rows, r => r.Upgrade == MinersIdle.Core.UpgradeId.Intern);
            simulation.AddShards(1e6);
            Assert.That(intern.ButtonText, Is.EqualTo("Andar 5"), "P7: o andar no lugar do preço");
            Assert.That(intern.CanBuy, Is.False);

            simulation.SetDeepestFloor(5);
            Assert.That(intern.ButtonText, Is.Not.EqualTo("Andar 5"));
            Assert.That(intern.CanBuy, Is.True, "liberado: o preço volta");
        }
    }
}
