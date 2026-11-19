using System;
using System.Collections;
using System.IO;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Persistence;
using MinersIdle.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9i E5, na cena Game de verdade: o Voltar do Android (#17) fecha a tela de cima, fica parado nas que pedem uma
    /// escolha, e sem tela aberta avisa "Toque de novo para sair" e, no segundo Voltar em até 2 s, abre "Sair do jogo?",
    /// que diz o que o estagiário minera fora; SAIR grava e fecha. E a bomba por toque (#18), escolhida nos AJUSTES.
    /// </summary>
    public class BackAndBombTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        string _dir;
        MiningRunner _runner;
        BackButtonHandler _back;
        ExitGameScreen _exit;
        GameSettings _settings;

        MiningSimulation Simulation => _runner.Simulation;

        [SetUp]
        public void SetUp() => _dir = Path.Combine(Path.GetTempPath(), "MinersIdleBack_" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        IEnumerator Open(bool withFiles = false)
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            _back = Object.FindAnyObjectByType<BackButtonHandler>();
            _exit = Object.FindAnyObjectByType<ExitGameScreen>(FindObjectsInactive.Include);
            _settings = Object.FindAnyObjectByType<GameSettings>();
            Assert.That(_back, Is.Not.Null, "o Voltar na cena (rode o setup do projeto)");
            Assert.That(_exit, Is.Not.Null);
            if (withFiles)
            {
                _settings.UseStore(new SettingsStore(new FileSaveStore(_dir, SettingsStore.FileName)));
                _runner.UseSave(new SaveService(new FileSaveStore(_dir)));
            }
        }

        [UnityTest]
        public IEnumerator BackTwice_ShowsTheNotice_ThenAsksToLeave_AndBackAgainStays()
        {
            yield return Open();
            _back.Back();
            Assert.That(_back.IsNoticeShown, Is.True, "o primeiro só avisa");
            Assert.That(_exit.IsOpen, Is.False);

            _back.Back();
            Assert.That(_back.IsNoticeShown, Is.False);
            Assert.That(_exit.IsOpen, Is.True, "o segundo pergunta");

            _back.Back();
            Assert.That(_exit.IsOpen, Is.False, "com o modal aberto, o Voltar é o FICAR");
            Assert.That(_back.IsNoticeShown, Is.False);
        }

        [UnityTest]
        public IEnumerator TheNotice_LastsTwoSeconds()
        {
            yield return Open();
            _back.Back();
            yield return new WaitForSecondsRealtime(BackButtonHandler.WindowSeconds + 0.3f);
            Assert.That(_back.IsNoticeShown, Is.False, "o aviso some");
            _back.Back();
            Assert.That(_exit.IsOpen, Is.False, "depois dos 2 s, o Voltar recomeça a conta");
            Assert.That(_back.IsNoticeShown, Is.True);
        }

        [UnityTest]
        public IEnumerator Back_ClosesTheOpenScreen_Before_AskingToLeave()
        {
            yield return Open();
            var shop = Object.FindAnyObjectByType<ShopView>(FindObjectsInactive.Include);
            shop.Open();
            _back.Back();
            Assert.That(shop.IsOpen, Is.False, "fecha a LOJA");
            Assert.That(_back.IsNoticeShown, Is.False, "sem aviso");

            var settings = Object.FindAnyObjectByType<SettingsScreen>(FindObjectsInactive.Include);
            settings.Open();
            settings.AskDelete();
            _back.Back();
            Assert.That(settings.IsConfirmOpen, Is.False, "a confirmação cancela primeiro");
            Assert.That(settings.IsOpen, Is.True);
            _back.Back();
            Assert.That(settings.IsOpen, Is.False);
            Assert.That(_runner.Simulation.State.Cave, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Back_StaysOnTheOfflineReturn()
        {
            yield return Open();
            Assert.That(Simulation.HireIntern(), Is.True);
            _runner.SimulateAway(3600);
            var offline = Object.FindAnyObjectByType<OfflineReturnScreen>(FindObjectsInactive.Include);
            Assert.That(offline.IsOpen, Is.True);
            _back.Back();
            _back.Back();
            Assert.That(offline.IsOpen, Is.True, "as lascas esperam o COLETAR");
            Assert.That(_exit.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator TheExitModal_SaysWhatTheInternMinesAway()
        {
            yield return Open();
            _exit.Open();
            Assert.That(_exit.BodyText, Does.StartWith("Sem o estagiário"));
            _exit.Close();

            Assert.That(Simulation.HireIntern(), Is.True);
            _exit.Open();
            Assert.That(_exit.BodyText, Does.Contain("estagiário continua")
                .And.Contain(NumberFormatter.FormatDuration(Simulation.OfflineMaxSeconds, trimWholeHours: true)));
        }

        [UnityTest]
        public IEnumerator Leave_SavesAndQuits()
        {
            yield return Open(withFiles: true);
            bool quit = false;
            _exit.Quit = () => quit = true;
            Simulation.SkipToCave(6);
            _exit.Open();
            _exit.transform.Find("Screen/Panel/Buttons/Leave").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(quit, Is.True);

            yield return Open(withFiles: true);
            Assert.That(Simulation.State.Cave, Is.EqualTo(6), "gravou antes de sair");
        }

        [UnityTest]
        public IEnumerator TapMode_LaunchesTheBomb_WithATapAnywhereOnTheStrip()
        {
            yield return Open(withFiles: true);
            var slider = Object.FindAnyObjectByType<BombSlider>();
            if (!Simulation.HasBomb) Assert.Ignore("O balanço ativo não tem bomba.");
            _runner.TimeScale = 0f;
            for (int i = 0; i < 200 && !Simulation.HasCrystalInBombReach; i++) Simulation.Tick(0.05);
            Simulation.FillBomb();
            yield return null;
            Assert.That(slider.TapMode, Is.False, "de fábrica, deslizar (D-310)");
            Assert.That(slider.ReadyText, Is.EqualTo("ARRASTE"));

            // Deslizando, um toque no meio da faixa (fora do botão) não lança.
            TapOn(slider, slider.Track.rect.center);
            Assert.That(slider.Launches, Is.EqualTo(0));

            var settings = Object.FindAnyObjectByType<SettingsScreen>(FindObjectsInactive.Include);
            settings.Open();
            settings.ToggleBombTap();
            Assert.That(settings.BombText, Is.EqualTo("TOCAR"));
            settings.Close();
            yield return null;
            Assert.That(slider.TapMode, Is.True);
            Assert.That(slider.ReadyText, Is.EqualTo("TOQUE"));

            TapOn(slider, slider.Track.rect.center);
            Assert.That(slider.Launches, Is.EqualTo(1), "um toque lança");

            // O modo fica nos ajustes.
            yield return Open(withFiles: true);
            Assert.That(_settings.BombTap, Is.True);
        }

        static void TapOn(BombSlider slider, Vector2 localInTrack)
        {
            var screen = RectTransformUtility.WorldToScreenPoint(null, slider.Track.TransformPoint(localInTrack));
            var data = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left, position = screen, pressPosition = screen,
            };
            ExecuteEvents.Execute(slider.gameObject, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(slider.gameObject, data, ExecuteEvents.pointerUpHandler);
        }
    }
}
