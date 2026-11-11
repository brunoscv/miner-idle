using System;
using System.Collections;
using System.IO;
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
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9b, E5, na cena Game de verdade (save numa pasta temporária, relógio do teste): tocar no AD abre o anúncio simulado
    /// e apaga o botão; com o anúncio na tela o mapa não minera (D-227); sem saída até o fim (D-228); o RECOMPENSA fecha e
    /// dá o boost, que aparece no botão, na coluna, no HUD e no raio acima do anão (D-230, D-231); acabou, o AD fica livre.
    /// </summary>
    public class AdFlowTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        string _dir;
        DateTime _now;
        MiningRunner _runner;
        AdButtonView _button;
        SimulatedAdScreen _screen;

        MiningSimulation Simulation => _runner.Simulation;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "MinersIdleAdFlow_" + Guid.NewGuid().ToString("N"));
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
            _button = Object.FindAnyObjectByType<AdButtonView>();
            _screen = Object.FindAnyObjectByType<SimulatedAdScreen>();
            Assert.That(_button, Is.Not.Null, "rode o setup do projeto (botão AD, M9b E5)");
            Assert.That(_screen, Is.Not.Null, "rode o setup do projeto (anúncio simulado, M9b E5)");
            _runner.UseClock(() => _now);
            _runner.TimeScale = 0f; // o teste anda o tempo
            _runner.UseSave(new SaveService(new FileSaveStore(_dir)));
            if (!Simulation.Strike.Ad.IsEnabled)
                Assert.Ignore("O balanço ativo não tem o boost do AD (rode o setup: GDD v7).");
            Simulation.RareRhythm = RareRhythm.Slow; // nenhuma janela do raro nova no meio do teste
            yield return null;
        }

        void CloseRareWindows()
        {
            while (Simulation.RareWindow.IsOpen)
            {
                if (Simulation.RareWindow.IsChoosing) Simulation.ChooseRareCard(0);
                else Simulation.CloseRareWindow();
            }
        }

        [UnityTest]
        public IEnumerator WatchingTheAd_GivesTheBoost_AndTheButtonCountsDown()
        {
            yield return Open();
            CloseRareWindows();
            yield return null;
            Assert.That(_button.State, Is.EqualTo(AdButtonView.ButtonState.Free));
            Assert.That(_button.LabelText, Is.EqualTo("AD"));

            _button.GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.That(_screen.IsShowing, Is.True, "o anúncio abre");
            Assert.That(_screen.IsDone, Is.False);
            Assert.That(_screen.SecondsText, Is.EqualTo("30s"));
            Assert.That(_screen.RewardText, Does.Contain("2,5").And.Contain("30"));
            Assert.That(_button.State, Is.EqualTo(AdButtonView.ButtonState.Watching), "apagado com o anúncio na tela");
            var input = Object.FindAnyObjectByType<TapInput>();
            Assert.That(input.TapAt(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)), Is.False,
                        "com o anúncio na tela, o toque no mapa não minera (D-227)");

            _screen.Claim();
            Assert.That(_screen.IsShowing, Is.True, "sem saída antes do fim (D-228)");
            Assert.That(Simulation.HasAdBoost, Is.False);

            _screen.SkipWait();
            yield return null;
            Assert.That(_screen.IsDone, Is.True, "o RECOMPENSA aparece");

            _screen.Claim();
            yield return null;

            Assert.That(_screen.IsShowing, Is.False);
            Assert.That(Simulation.HasAdBoost, Is.True, "a recompensa é o boost");
            Assert.That(_button.State, Is.EqualTo(AdButtonView.ButtonState.Boosted));
            Assert.That(_button.LabelText, Is.EqualTo("30s"), "os segundos no lugar do AD (D-230)");
            Assert.That(Object.FindAnyObjectByType<AdBoostBolt>().IsShown, Is.True, "o raio acima do anão (D-231)");
            Assert.That(Object.FindAnyObjectByType<HudView>().IsSpeedBoosted, Is.True, "a VELOCIDADE em âmbar (D-231)");
            Assert.That(Object.FindAnyObjectByType<MiningFeedbackView>().LastGainText, Is.Not.Empty, "o ganho sobe do anão");
            Assert.That(input.TapAt(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)), Is.True, "fechou: o mapa volta");

            Simulation.Tick(12);
            yield return null;
            Assert.That(_button.LabelText, Is.EqualTo("18s"));

            Simulation.Tick(18);
            CloseRareWindows();
            yield return null;
            Assert.That(Simulation.HasAdBoost, Is.False);
            Assert.That(_button.State, Is.EqualTo(AdButtonView.ButtonState.Free), "acabou: o AD fica livre");
            Assert.That(_button.LabelText, Is.EqualTo("AD"));
            Assert.That(Object.FindAnyObjectByType<AdBoostBolt>().IsShown, Is.False);
        }

        [UnityTest]
        public IEnumerator TheDebugShortcut_GivesTheBoostWithoutTheAd()
        {
            yield return Open();
            var panel = Object.FindAnyObjectByType<BonusDebugPanel>();
            Assert.That(panel, Is.Not.Null, "rode o setup do projeto (DBG → Bônus)");

            panel.AdNow();
            yield return null;

            Assert.That(Simulation.HasAdBoost, Is.True);
            Assert.That(_screen.IsShowing, Is.False, "sem o anúncio (D-234)");

            var debug = Object.FindAnyObjectByType<DebugPanel>();
            debug.transform.Find("Content/Buttons/Bonus").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(panel.SummaryText, Does.Contain("AD ×2,5"));

            panel.AdNow();
            Assert.That(Simulation.AdBoostSecondsLeft, Is.EqualTo(30), "com um ativo, nada (D-188)");
        }
    }
}
