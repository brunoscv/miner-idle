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
    /// M9, E4 (D-198…D-200): a tela AJUSTES na cena Game. O botão do HUD abre; o volume, o tremor e a vibração vão para o
    /// som, a câmera e o vibrador e ficam no <c>settings.json</c> (desligar o tremor e reabrir: continua desligado); o
    /// Apagar save pede confirmação, volta ao nível 1 e mantém os ajustes; os Créditos abrem e voltam. A vibração (D-199):
    /// curta no crítico, longa na parede, nada no toque comum. Usa uma pasta temporária, nunca os arquivos de quem joga.
    /// </summary>
    public class SettingsFlowTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        string _dir;
        MiningRunner _runner;
        GameSettings _settings;
        SettingsScreen _screen;
        Transform _safeArea;

        [SetUp]
        public void SetUp() => _dir = Path.Combine(Path.GetTempPath(), "MinersIdleSettingsFlow_" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        /// <param name="withFiles">O que o <c>GameBootstrap</c> faz: o save e os ajustes na pasta temporária.</param>
        IEnumerator Load(bool withFiles)
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            _settings = Object.FindAnyObjectByType<GameSettings>();
            _screen = Object.FindAnyObjectByType<SettingsScreen>();
            _safeArea = GameObject.Find("SafeArea").transform;
            Assert.That(_settings, Is.Not.Null, "GameSettings na cena (rode o setup do projeto)");
            Assert.That(_screen, Is.Not.Null, "tela AJUSTES na cena (rode o setup do projeto)");
            if (!withFiles) yield break;
            _runner.UseSave(new SaveService(new FileSaveStore(_dir)));
            _settings.UseStore(new SettingsStore(new FileSaveStore(_dir, SettingsStore.FileName)));
        }

        void Click(string path)
        {
            var target = _screen.transform.Find(path);
            Assert.That(target, Is.Not.Null, "não achei " + path);
            Assert.That(target.gameObject.activeInHierarchy, Is.True, path + " está escondido");
            target.GetComponent<Button>().onClick.Invoke();
        }

        void OpenFromTheHud() => _safeArea.Find("SettingsButton").GetComponent<Button>().onClick.Invoke();

        [UnityTest]
        public IEnumerator HudButton_OpensTheScreen_WithTheFactorySettings_AndTheXCloses()
        {
            yield return Load(withFiles: false);
            Assert.That(_screen.IsOpen, Is.False);

            OpenFromTheHud();

            Assert.That(_screen.IsOpen, Is.True);
            Assert.That(_screen.VolumeText, Is.EqualTo("80%"));
            Assert.That(_screen.ShowsShakeOn, Is.True);
            Assert.That(_screen.ShowsVibrationOn, Is.True);
            Click("Screen/Panel/Close");
            Assert.That(_screen.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator ShakeOffAndVolume_GoToTheEffects_AndSurviveReopening()
        {
            yield return Load(withFiles: true);
            var shake = Object.FindAnyObjectByType<ScreenShake>();
            var sound = Object.FindAnyObjectByType<SoundBoard>();
            var haptics = Object.FindAnyObjectByType<Haptics>();
            OpenFromTheHud();

            Click("Screen/Panel/Inset/Shake/Toggle");
            Click("Screen/Panel/Inset/Vibration/Toggle");
            _screen.SetVolumeStep(10);

            Assert.That(shake.ShakeEnabled, Is.False, "a câmera obedece na hora");
            Assert.That(haptics.Enabled, Is.False);
            Assert.That(sound.Volume, Is.EqualTo(0.5f).Within(1e-6));
            Assert.That(_screen.ShowsShakeOn, Is.False);
            Assert.That(_screen.VolumeText, Is.EqualTo("50%"));
            Click("Screen/Panel/Close"); // grava o volume

            // "Fecha e abre": cena nova, os ajustes lidos do arquivo.
            yield return Load(withFiles: true);
            shake = Object.FindAnyObjectByType<ScreenShake>();
            sound = Object.FindAnyObjectByType<SoundBoard>();
            haptics = Object.FindAnyObjectByType<Haptics>();
            Assert.That(_settings.ScreenShake, Is.False, "o tremor continua desligado");
            Assert.That(shake.ShakeEnabled, Is.False);
            Assert.That(_settings.Vibration, Is.False);
            Assert.That(haptics.Enabled, Is.False);
            Assert.That(sound.Volume, Is.EqualTo(0.5f).Within(1e-6));
            OpenFromTheHud();
            Assert.That(_screen.ShowsShakeOn, Is.False);
            Assert.That(_screen.ShowsVibrationOn, Is.False);
            Assert.That(_screen.VolumeText, Is.EqualTo("50%"));
        }

        [UnityTest]
        public IEnumerator DeleteSave_AsksFirst_ThenStartsAtLevelOne_AndKeepsTheSettings()
        {
            yield return Load(withFiles: true);
            var simulation = _runner.Simulation;
            simulation.AddShards(1000);
            Assert.That(simulation.Purchase(UpgradeId.Pickaxe), Is.EqualTo(PurchaseResult.Purchased));
            simulation.SkipToCave(3);
            simulation.AddRedShards(20);
            _runner.SaveNow();
            OpenFromTheHud();
            Click("Screen/Panel/Inset/Shake/Toggle");

            Click("Screen/Panel/Buttons/DeleteSave");
            Assert.That(_screen.IsConfirmOpen, Is.True, "o primeiro toque só pergunta");
            Assert.That(_runner.Simulation, Is.SameAs(simulation), "nada apagado ainda");
            Click("Confirm/Panel/Buttons/Cancel");
            Assert.That(_screen.IsConfirmOpen, Is.False);
            Assert.That(_screen.IsOpen, Is.True);
            Assert.That(_runner.Simulation.State.Cave, Is.EqualTo(3), "CANCELAR não apaga");

            Click("Screen/Panel/Buttons/DeleteSave");
            Click("Confirm/Panel/Buttons/Delete");

            var state = _runner.Simulation.State;
            Assert.That(state.Cave, Is.EqualTo(1), "volta ao nível 1");
            Assert.That(state.Shards, Is.EqualTo(0));
            Assert.That(state.LevelOf(UpgradeId.Pickaxe), Is.EqualTo(0));
            Assert.That(_runner.Simulation.Permanents.State.RedShards, Is.EqualTo(0), "as permanentes também");
            Assert.That(_screen.IsOpen, Is.False, "a tela fecha no jogo novo");
            Assert.That(_settings.ScreenShake, Is.False, "os ajustes ficam (D-200)");

            yield return Load(withFiles: true);
            Assert.That(_runner.Simulation.State.Cave, Is.EqualTo(1), "o save velho não volta");
            Assert.That(_runner.Simulation.State.LevelOf(UpgradeId.Pickaxe), Is.EqualTo(0));
            Assert.That(_settings.ScreenShake, Is.False, "nem os ajustes somem");
        }

        [UnityTest]
        public IEnumerator Credits_OpenInPlaceOfTheSettings_AndBackReturns()
        {
            yield return Load(withFiles: false);
            OpenFromTheHud();

            Click("Screen/Panel/Buttons/Credits");
            Assert.That(_screen.IsCreditsOpen, Is.True);
            Assert.That(_screen.IsOpen, Is.False, "os Créditos ficam no lugar dos AJUSTES");
            Assert.That(_screen.CreditsText, Does.Contain("Pixelify Sans").And.Contain("REEBOO Labs").And.Contain("Apellon"));

            Click("Credits/Panel/Buttons/Back");
            Assert.That(_screen.IsCreditsOpen, Is.False);
            Assert.That(_screen.IsOpen, Is.True);
        }

        /// <summary>Avança a partida até o alvo do anão estar ao alcance do toque.</summary>
        static void TickUntilInReach(MiningSimulation simulation)
        {
            for (int i = 0; i < 400; i++)
            {
                var target = simulation.Miner.Target;
                if (target != null && target.Distance - simulation.Miner.Distance <= simulation.Rules.TapReach) return;
                simulation.Tick(0.05);
            }
            Assert.Fail("o alvo nunca chegou ao alcance do toque");
        }

        /// <summary>
        /// D-199: o toque comum não vibra; o crítico vibra curto; a quebra da parede vibra longo; desligada, nada. A partida
        /// é avançada à mão (o runner parado), e entre um pulso e outro passa o intervalo mínimo em tempo real.
        /// </summary>
        [UnityTest]
        public IEnumerator Vibration_ShortOnCritical_LongOnTheWall_NothingOnANormalTap()
        {
            yield return Load(withFiles: false);
            var haptics = Object.FindAnyObjectByType<Haptics>();
            var simulation = _runner.Simulation;
            if (!simulation.Economy.Get(UpgradeId.Letrado).IsEnabled)
                Assert.Ignore("O balanço ativo não tem o Letrado (rode o setup do projeto).");
            _runner.enabled = false;
            MiningHit? tapHit = null;
            simulation.HitLanded += hit => { if (hit.IsTap) tapHit = hit; };

            // Um toque comum (o sorteio às vezes dá crítico: tenta até sair um comum).
            bool sawNormal = false;
            for (int i = 0; i < 200 && !sawNormal; i++)
            {
                TickUntilInReach(simulation);
                int before = haptics.PulseCount;
                tapHit = null;
                simulation.Tap();
                if (tapHit is { IsCritical: false })
                {
                    sawNormal = true;
                    Assert.That(haptics.PulseCount, Is.EqualTo(before), "o toque comum não vibra");
                }
            }
            Assert.That(sawNormal, Is.True, "nenhum toque acertou o cristal");

            // Um toque crítico: com o cristal ao alcance, espera o intervalo dos pulsos parado e toca.
            TickUntilInReach(simulation);
            yield return new WaitForSecondsRealtime(0.2f);
            int beforeCritical = haptics.PulseCount;
            _runner.ForceCritical = true;
            tapHit = null;
            simulation.Tap();
            _runner.ForceCritical = false;
            Assert.That(tapHit?.IsCritical, Is.True, "o toque acertou");
            Assert.That(haptics.PulseCount, Is.EqualTo(beforeCritical + 1), "o crítico vibra");
            Assert.That(haptics.LastPulseMilliseconds, Is.EqualTo(haptics.CriticalMilliseconds));

            // A parede da passagem.
            simulation.AddShards(1e12);
            simulation.FillCaveBreaks();
            Assert.That(simulation.Purchase(UpgradeId.Letrado), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(simulation.PassCave(), Is.True);
            int beforeWall = haptics.PulseCount;
            for (int i = 0; i < 30 && simulation.State.Cave == 1; i++)
            {
                simulation.Tick(0);
                simulation.BreakTarget();
            }
            Assert.That(simulation.State.Cave, Is.EqualTo(2), "a parede quebrou");
            Assert.That(haptics.PulseCount, Is.EqualTo(beforeWall + 1), "a parede vibra, os cristais antes dela não");
            Assert.That(haptics.LastPulseMilliseconds, Is.EqualTo(haptics.WallMilliseconds));
            Assert.That(haptics.WallMilliseconds, Is.GreaterThan(haptics.CriticalMilliseconds), "a da parede é mais longa");

            // Desligada nos AJUSTES: nada.
            _settings.Vibration = false;
            yield return new WaitForSecondsRealtime(0.2f);
            int beforeOff = haptics.PulseCount;
            _runner.ForceCritical = true;
            for (int i = 0; i < 20; i++)
            {
                simulation.Tick(0.05);
                simulation.Tap();
            }
            _runner.ForceCritical = false;
            Assert.That(haptics.PulseCount, Is.EqualTo(beforeOff), "vibração desligada");
        }
    }
}
