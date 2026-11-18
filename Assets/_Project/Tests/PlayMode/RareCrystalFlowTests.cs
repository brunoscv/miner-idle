using System;
using System.Collections;
using System.IO;
using System.Linq;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Persistence;
using MinersIdle.Presentation;
using MinersIdle.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9b, E4, na cena Game de verdade (save numa pasta temporária, relógio do teste): o raro aparece na fila e, quebrado,
    /// abre a janela; tocar numa carta aplica e fecha; com a janela aberta o mapa não minera (D-223); sem escolha o jogo
    /// sorteia e espera o FECHAR (D-186); a fila (D-221); fechar e abrir o app traz a janela (D-222).
    /// </summary>
    public class RareCrystalFlowTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        string _dir;
        DateTime _now;
        MiningRunner _runner;
        BonusDebugPanel _panel;
        RareWindowView _view;

        MiningSimulation Simulation => _runner.Simulation;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "MinersIdleRareFlow_" + Guid.NewGuid().ToString("N"));
            _now = new DateTime(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        IEnumerator Open()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            _panel = Object.FindAnyObjectByType<BonusDebugPanel>();
            _view = Object.FindAnyObjectByType<RareWindowView>();
            Assert.That(_view, Is.Not.Null, "rode o setup do projeto (janela do raro, M9b E4)");
            Assert.That(_panel, Is.Not.Null, "rode o setup do projeto (DBG → Bônus)");
            _runner.UseClock(() => _now);
            _runner.TimeScale = 0f; // o teste anda o tempo
            _runner.UseSave(new SaveService(new FileSaveStore(_dir)));
            if (!Simulation.Strike.Rare.IsEnabled)
                Assert.Ignore("O balanço ativo não tem o cristal raro (rode o setup: GDD v7).");
            // O ritmo lento (D-185): nenhum raro novo com janela aberta ou carta ativa. Os cristais do começo ainda podem
            // ter nascido raros: quebrá-los põe a janela na fila, e os testes contam com isso.
            Simulation.RareRhythm = RareRhythm.Slow;
        }

        static RareCard[] ActiveCards(MiningSimulation simulation) =>
            simulation.Bonuses.All.Where(b => b.Source == BonusSource.Rare).Select(b => (RareCard)b.Tier).ToArray();

        [UnityTest]
        public IEnumerator NextRare_ShowsOnTheStage_AndBreakingItOpensTheWindow()
        {
            yield return Open();
            var stage = Object.FindAnyObjectByType<DescentStage>();
            _panel.NextRare();
            bool seen = false;
            for (int i = 0; i < 600 && !Simulation.RareWindow.IsOpen; i++)
            {
                Simulation.Tick(0.1);
                yield return null;
                seen |= stage.RareCrystalsShown > 0;
            }

            Assert.That(seen, Is.True, "o raro aparece na fila com o desenho dele (D-220)");
            Assert.That(Simulation.RareWindow.IsChoosing, Is.True, "quebrou: a janela abre (D-184)");
            Assert.That(_view.IsOpen && _view.IsChoosing, Is.True);
        }

        [UnityTest]
        public IEnumerator Choosing_AppliesTheCard_TheGainRises_AndTheMapDoesNotMineMeanwhile()
        {
            yield return Open();
            _panel.OpenWindow();
            yield return null;

            Assert.That(_view.IsOpen && _view.IsChoosing, Is.True);
            Assert.That(_view.CardTitle(0), Is.Not.Empty.And.Not.EqualTo(_view.CardTitle(1)));
            Assert.That(_view.SecondsText, Is.EqualTo("30s"));
            Simulation.Tick(10);
            yield return null;
            Assert.That(_view.SecondsText, Is.EqualTo("20s"));

            var input = Object.FindAnyObjectByType<TapInput>();
            Assert.That(input.TapAt(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)), Is.False,
                        "com a janela aberta, o toque no mapa não minera (D-223)");

            var card = Simulation.RareWindow.First;
            int queued = Simulation.RareWindow.Queued;
            _view.Choose(0);
            yield return null;

            Assert.That(ActiveCards(Simulation), Is.EqualTo(new[] { card }));
            Assert.That(_view.IsOpen, Is.EqualTo(queued > 0), "fecha, ou abre a próxima da fila (D-221)");
            Assert.That(Object.FindAnyObjectByType<MiningFeedbackView>().LastGainText, Is.Not.Empty, "a carta sobe do anão (D-225)");
            while (Simulation.RareWindow.IsOpen) Simulation.ChooseRareCard(0);
            yield return null;
            Assert.That(_view.IsOpen, Is.False);
            Assert.That(input.TapAt(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)), Is.True, "fechou: o mapa volta");
        }

        [UnityTest]
        public IEnumerator WithoutChoice_TheGamePicks_ShowsIt_AndCloseCloses()
        {
            yield return Open();
            _panel.OpenWindow();
            Simulation.Tick(30.5);
            yield return null;

            Assert.That(_view.IsOpen, Is.True, "mostra o sorteio");
            Assert.That(_view.IsChoosing, Is.False);
            Assert.That(_view.ShowsClose, Is.True);
            int chosen = Simulation.RareWindow.Chosen;
            Assert.That(_view.IsCardDimmed(chosen), Is.False);
            Assert.That(_view.IsCardDimmed(1 - chosen), Is.True, "a outra apagada");
            Assert.That(ActiveCards(Simulation), Has.Length.EqualTo(1));

            _view.Close();
            yield return null;
            Assert.That(_view.IsOpen, Is.EqualTo(Simulation.RareWindow.IsOpen), "fecha, ou abre a próxima da fila");
            Assert.That(_view.IsOpen, Is.EqualTo(_view.IsChoosing));
        }

        [UnityTest]
        public IEnumerator ARareWhileOpen_ShowsTheQueue_AndOpensNext()
        {
            yield return Open();
            _panel.OpenWindow();
            _panel.OpenWindow();
            yield return null;
            Assert.That(_view.QueueText, Is.EqualTo("+" + Simulation.RareWindow.Queued + " raro"));

            _view.Choose(1);
            yield return null;
            Assert.That(_view.IsOpen && _view.IsChoosing, Is.True, "a próxima abre");
            Assert.That(_view.QueueText, Is.EqualTo(Simulation.RareWindow.Queued > 0 ? "+" + Simulation.RareWindow.Queued + " raro" : ""));
        }

        [UnityTest]
        public IEnumerator ReopeningTheApp_BringsTheWindowBack_WithTheTimeLeft()
        {
            yield return Open();
            _panel.OpenWindow();
            Simulation.Tick(8);
            var window = Simulation.RareWindow;
            _runner.SaveNow();

            _now = _now.AddHours(1); // o tempo fora não corre a janela (D-222)
            yield return Open();

            Assert.That(Simulation.RareWindow.First, Is.EqualTo(window.First));
            Assert.That(Simulation.RareWindow.SecondsLeft, Is.EqualTo(22).Within(1e-6));
            Assert.That(_view.IsOpen && _view.IsChoosing, Is.True);
        }

        [UnityTest]
        public IEnumerator DebugCards_GrantEachCard_AndTheMenuListsThem()
        {
            yield return Open();
            // M9e E1: as cartas do balanço (no v8, Dano, Lascas, Crítico e Dano de Crítico; o Rápido saiu, D-243).
            var balance = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(Simulation.Strike.Rare.Cards, c => c.Card));
            foreach (RareCard card in Enum.GetValues(typeof(RareCard))) _panel.GrantCard(card);
            Assert.That(ActiveCards(Simulation), Is.EquivalentTo(balance), "a carta que o balanço não tem não entra");

            var debug = Object.FindAnyObjectByType<DebugPanel>();
            debug.transform.Find("Content/Buttons/Bonus").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That(_panel.SummaryText, Does.Contain("Dano ×2").And.Contain("Crítico +10%").And.Contain("Dano de crítico ×2"));
            var speed = _panel.transform.Find("Content/Buttons/CardSpeed");
            Assert.That(speed.gameObject.activeSelf, Is.False, "o botão da carta que o balanço não tem some");
        }

        /// <summary>
        /// M9e E1 (D-285): DBG → Bônus → Contador 5 s (o raro a cada 5 s), Raro agora (um raro nasce já e o contador
        /// recomeça) e Tier 2 (as cartas ×4); no alto, os segundos até o raro.
        /// </summary>
        [UnityTest]
        public IEnumerator DebugCounter_RareNow_AndTier2()
        {
            yield return Open();
            if (!Simulation.Strike.Rare.UsesCounter) Assert.Ignore("O balanço ativo não tem o contador do raro (GDD v8).");
            Transform Button(string name) => _panel.transform.Find("Content/Buttons/" + name);
            Assert.That(Button("RareCounter"), Is.Not.Null, "rode o setup do projeto (M9e E1)");

            Simulation.SetDeepestFloor(10);
            Assert.That(Simulation.IsRareCounting, Is.True, "o recorde 10 liga o contador");
            Assert.That(_panel.CounterButtonText, Is.EqualTo("Contador: 60 s"));
            Button("RareCounter").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(_panel.CounterButtonText, Is.EqualTo("Contador: 5 s"));
            Assert.That(Simulation.RareInterval, Is.EqualTo(5));
            Assert.That(Simulation.RareCountdown, Is.LessThanOrEqualTo(5));

            Button("RareNow").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(Simulation.Crystals.Any(c => c.IsRare), Is.True, "Raro agora: nasce já");
            Assert.That(Simulation.State.RareTimer, Is.EqualTo(0), "e o contador recomeça");

            Assert.That(_panel.Tier2ButtonText, Is.EqualTo("Tier 2: não"));
            Button("RareTier2").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(_panel.Tier2ButtonText, Is.EqualTo("Tier 2: sim"));
            _panel.GrantCard(RareCard.Damage);
            Assert.That(Simulation.Bonuses.DamageMultiplier, Is.EqualTo(4), "tier 2: ×4");

            var debug = Object.FindAnyObjectByType<DebugPanel>();
            debug.transform.Find("Content/Buttons/Bonus").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That(_panel.SummaryText, Does.Contain("Raro em").And.Contain("tier 2").And.Contain("Dano ×4"));

            Button("RareCounter").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(Simulation.RareInterval, Is.EqualTo(60), "de novo: o das regras");
        }
    }
}
