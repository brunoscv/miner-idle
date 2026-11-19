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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9i E4 (D-333, D-337), na cena Game de verdade, numa pasta temporária (nunca os arquivos de quem joga):
    /// <list type="bullet">
    /// <item>o Apagar save dos AJUSTES depois de jogar um pouco de tudo (compras, recorde, descanso, permanentes,
    /// estagiário, offline esperando o COLETAR) volta tudo ao começo, apaga o backup e mantém os ajustes (D-200);</item>
    /// <item>um save de uma versão mais velha do app (ou de antes da E4, sem versão) abre com o aviso de versão nova;
    /// CONTINUAR segue com o save e não avisa mais; RECOMEÇAR apaga, como o Apagar save.</item>
    /// </list>
    /// </summary>
    public class SaveVersionTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
        const string NewApp = "0.1.0.2611191530";
        const string OldApp = "0.1.0.2611181200";

        string _dir;
        MiningRunner _runner;
        GameSettings _settings;
        VersionNoticeScreen _notice;

        MiningSimulation Simulation => _runner.Simulation;
        string SavePath => Path.Combine(_dir, FileSaveStore.FileName);
        string BackupPath => SavePath + ".bak";

        [SetUp]
        public void SetUp() => _dir = Path.Combine(Path.GetTempPath(), "MinersIdleSaveVersion_" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        /// <summary>Abre o jogo como o <c>GameBootstrap</c>, com o app na versão <paramref name="app"/>.</summary>
        IEnumerator Open(string app)
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            _settings = Object.FindAnyObjectByType<GameSettings>();
            _notice = Object.FindAnyObjectByType<VersionNoticeScreen>(FindObjectsInactive.Include);
            Assert.That(_notice, Is.Not.Null, "o aviso de versão na cena (rode o setup do projeto)");
            _settings.UseStore(new SettingsStore(new FileSaveStore(_dir, SettingsStore.FileName)));
            _runner.UseSave(new SaveService(new FileSaveStore(_dir)) { AppVersion = app });
            yield return null;
        }

        [UnityTest]
        public IEnumerator DeleteSave_FromTheSettings_ResetsEverything_AndTheBackup_ButKeepsTheSettings()
        {
            yield return Open(NewApp);
            // Um pouco de tudo: o recorde, o descanso, as permanentes, a run nova com compra e estagiário, o offline.
            Simulation.SkipToCave(12);
            Assert.That(_runner.Rest(force: true), Is.Not.Null);
            Assert.That(_runner.StartRun(), Is.True);
            Simulation.AddRedShards(1000);
            foreach (PermanentId id in Enum.GetValues(typeof(PermanentId))) Simulation.PurchasePermanent(id);
            Simulation.AddShards(1e6);
            Assert.That(Simulation.Purchase(UpgradeId.Pickaxe), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(Simulation.HireIntern(), Is.True);
            _runner.SimulateAway(3600);
            Assert.That(Simulation.PendingOffline.HasShards, Is.True, "a volta do offline espera o COLETAR");
            _runner.SaveNow();
            _runner.SaveNow();
            Assert.That(File.Exists(BackupPath), Is.True, "o backup existe antes de apagar");
            Assume.That(Simulation.Permanents.State.Day, Is.GreaterThan(1));
            _settings.ScreenShake = false;

            // AJUSTES → APAGAR SAVE → APAGAR.
            var settings = Object.FindAnyObjectByType<SettingsScreen>();
            settings.Open();
            settings.AskDelete();
            settings.ConfirmDelete();

            var state = Simulation.State;
            var permanents = Simulation.Permanents.State;
            Assert.That(state.Cave, Is.EqualTo(1));
            Assert.That(state.Shards, Is.EqualTo(0));
            foreach (UpgradeId id in Enum.GetValues(typeof(UpgradeId)))
                Assert.That(state.LevelOf(id), Is.EqualTo(0), id + " zerada");
            Assert.That(Simulation.IsInternHired, Is.False, "o estagiário vai embora");
            Assert.That(Simulation.PendingOffline.HasShards, Is.False, "a volta do offline some");
            Assert.That(Simulation.Bonuses.Count, Is.EqualTo(0));
            Assert.That(_runner.IsResting, Is.False);
            Assert.That(permanents.RedShards, Is.EqualTo(0));
            Assert.That(permanents.Day, Is.EqualTo(1));
            Assert.That(permanents.DeepestFloor, Is.EqualTo(1), "o recorde também");
            foreach (PermanentId id in Enum.GetValues(typeof(PermanentId)))
                Assert.That(permanents.LevelOf(id), Is.EqualTo(0), id + " zerada");
            Assert.That(File.Exists(BackupPath), Is.False, "o backup foi junto");
            Assert.That(File.Exists(SavePath), Is.True, "o jogo novo já gravou o save dele");

            // "Fecha e abre": nada do jogo velho volta; os ajustes ficam (D-200).
            yield return Open(NewApp);
            Assert.That(Simulation.State.Cave, Is.EqualTo(1));
            Assert.That(Simulation.State.LevelOf(UpgradeId.Pickaxe), Is.EqualTo(0));
            Assert.That(Simulation.Permanents.State.Day, Is.EqualTo(1));
            Assert.That(Simulation.Permanents.State.DeepestFloor, Is.EqualTo(1));
            Assert.That(_settings.ScreenShake, Is.False, "os ajustes ficam");
            Assert.That(_notice.IsOpen, Is.False, "o save novo é desta versão: sem aviso");
        }

        [UnityTest]
        public IEnumerator ASaveFromTheSameApp_OpensWithoutTheNotice()
        {
            yield return Open(NewApp);
            Simulation.SkipToCave(5);
            _runner.SaveNow();
            yield return Open(NewApp);
            Assert.That(_runner.IsSaveFromOlderApp, Is.False);
            Assert.That(_notice.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator ANewGame_HasNoNotice()
        {
            yield return Open(NewApp);
            Assert.That(_notice.IsOpen, Is.False, "sem save, nada a avisar");
        }

        [UnityTest]
        public IEnumerator AnOlderSave_ShowsTheNotice_AndContinueKeepsTheSave_AndStopsAsking()
        {
            yield return Open(OldApp);
            Simulation.SkipToCave(7);
            _runner.SaveNow();

            yield return Open(NewApp);
            Assert.That(_runner.IsSaveFromOlderApp, Is.True);
            Assert.That(_notice.IsOpen, Is.True, "o aviso abre sozinho");
            Assert.That(_notice.NumbersText, Does.Contain(OldApp).And.Contain(NewApp));

            Click("Continue");
            Assert.That(_notice.IsOpen, Is.False);
            Assert.That(Simulation.State.Cave, Is.EqualTo(7), "o save continua");

            yield return Open(NewApp);
            Assert.That(_notice.IsOpen, Is.False, "gravado com a versão nova: não avisa de novo");
            Assert.That(Simulation.State.Cave, Is.EqualTo(7));
        }

        [UnityTest]
        public IEnumerator AnOlderSave_RestartDeletesIt_LikeTheDeleteSave()
        {
            yield return Open(OldApp);
            Simulation.SkipToCave(7);
            Simulation.AddRedShards(50);
            _runner.SaveNow();
            _runner.SaveNow();

            yield return Open(NewApp);
            Assert.That(_notice.IsOpen, Is.True);
            Click("Restart");

            Assert.That(_notice.IsOpen, Is.False);
            Assert.That(Simulation.State.Cave, Is.EqualTo(1));
            Assert.That(Simulation.Permanents.State.RedShards, Is.EqualTo(0));
            Assert.That(File.Exists(BackupPath), Is.False);

            yield return Open(NewApp);
            Assert.That(_notice.IsOpen, Is.False);
            Assert.That(Simulation.State.Cave, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ASaveFromBeforeTheVersions_CountsAsOlder()
        {
            yield return Open(NewApp);
            Simulation.SkipToCave(4);
            _runner.SaveNow();
            // Um save de antes da E4: sem o campo da versão (e sem o backup, que é desta versão).
            string json = System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(SavePath), @",\s*""appVersion"":\s*""[^""]*""", "");
            Assert.That(json, Does.Not.Contain("appVersion"));
            File.WriteAllText(SavePath, json);
            if (File.Exists(BackupPath)) File.Delete(BackupPath);

            yield return Open(NewApp);
            Assert.That(_notice.IsOpen, Is.True);
            Assert.That(_notice.NumbersText, Does.Contain("anterior"));
        }

        [UnityTest]
        public IEnumerator TheDebugShortcut_OpensTheNotice()
        {
            yield return Open(NewApp);
            var button = Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include).transform
                .Find("OfflineMenu/Content/Buttons/VersionNotice");
            Assert.That(button, Is.Not.Null, "DBG → Offline → Aviso de versão");
            button.GetComponent<Button>().onClick.Invoke();
            Assert.That(_notice.IsOpen, Is.True);
        }

        void Click(string button)
        {
            var target = _notice.transform.Find("Screen/Panel/Buttons/" + button);
            Assert.That(target, Is.Not.Null, button);
            Assert.That(target.gameObject.activeInHierarchy, Is.True, button + " está escondido");
            target.GetComponent<Button>().onClick.Invoke();
        }
    }
}
