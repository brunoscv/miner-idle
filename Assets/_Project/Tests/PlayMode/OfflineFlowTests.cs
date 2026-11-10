using System;
using System.Collections;
using System.IO;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Persistence;
using MinersIdle.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9, E1: o offline na cena Game (GDD v7). O DBG → Offline simula o jogo fechado sem fechar e volta o relógio; ao
    /// abrir pelo caminho do save (save v5, com a hora), o tempo desde a última gravação vira lascas do estagiário, uma
    /// vez só. Aberta direto, a cena não grava nada.
    /// </summary>
    public class OfflineFlowTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        DebugPanel _panel;
        MiningRunner _runner;
        string _dir;

        MiningSimulation Simulation => _runner.Simulation;

        [SetUp]
        public void SetUp() => _dir = Path.Combine(Path.GetTempPath(), "MinersIdleOffline_" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _panel = Object.FindAnyObjectByType<DebugPanel>();
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            Assert.That(_panel.transform.Find("OfflineMenu")?.GetComponent<OfflineDebugPanel>(), Is.Not.Null,
                        "menu Offline (rode o setup do projeto)");
            if (!Simulation.Economy.HasIntern) Assert.Ignore("O balanço ativo não tem estagiário (rode o setup: GDD v7).");
        }

        void Click(string name)
        {
            var target = _panel.transform.Find("OfflineMenu/Content/Buttons/" + name);
            Assert.That(target, Is.Not.Null, "botão não encontrado: " + name);
            target.GetComponent<Button>().onClick.Invoke();
        }

        string Summary => _panel.transform.Find("OfflineMenu/Content/Summary").GetComponent<TMP_Text>().text;

        [UnityTest]
        public IEnumerator Opens_FromTheDbg_AndWithoutTheIntern_GivesNothing()
        {
            yield return Load();
            _panel.transform.Find("Toggle").GetComponent<Button>().onClick.Invoke();
            _panel.transform.Find("Content/Buttons/Offline").GetComponent<Button>().onClick.Invoke();
            Assert.That(_panel.transform.Find("OfflineMenu").GetComponent<DebugMenu>().IsOpen, Is.True, "Offline abre em cima");
            Assert.That(Summary, Does.StartWith("Nenhum offline ainda"));

            Click("Away1H");
            Assert.That(Simulation.State.Shards, Is.EqualTo(0), "sem estagiário, nada (D-139)");
            Assert.That(Summary, Does.Contain("sem estagiário"));
        }

        [UnityTest]
        public IEnumerator WithTheIntern_OneHourAway_PaysTheShards_AndShowsTheSum()
        {
            yield return Load();
            Assert.That(Simulation.HireIntern(), Is.True);
            long caveBreaks = Simulation.State.CaveBreaks;
            int cave = Simulation.State.Cave;

            Click("Away1H");

            var report = _runner.LastOffline.Value;
            Assert.That(report.Hired, Is.True);
            Assert.That(report.SecondsCounted, Is.EqualTo(3600));
            // Nível 1: vida 1, dano 1 → 1 golpe; 0,5 s de corrida por cristal (A2) → 7.200 cristais de 1,2 lasca.
            Assert.That(report.HitsPerCrystal, Is.EqualTo(1));
            Assert.That(report.Crystals, Is.EqualTo(7200));
            Assert.That(Simulation.State.Shards, Is.EqualTo(report.Shards).Within(1e-6));
            Assert.That(report.Shards, Is.EqualTo(7200 * 1.2).Within(1e-6));
            Assert.That(Simulation.State.CaveBreaks, Is.EqualTo(caveBreaks), "não conta na trava do Mapa (D-189)");
            Assert.That(Simulation.State.Cave, Is.EqualTo(cave), "não passa de nível");
            Assert.That(Summary, Does.Contain(NumberFormatter.Format(7200) + " cristais"));
        }

        [UnityTest]
        public IEnumerator EightHoursAway_CountSix_AndTheClockBack_GivesNothing()
        {
            yield return Load();
            Simulation.HireIntern();

            Click("Away8H");
            Assert.That(_runner.LastOffline.Value.SecondsCounted, Is.EqualTo(6 * 3600));
            Assert.That(_runner.LastOffline.Value.Capped, Is.True);
            Assert.That(Summary, Does.Contain("(teto)"));

            double shards = Simulation.State.Shards;
            Click("ClockBack");
            Assert.That(_runner.LastOffline.Value.ClockWentBack, Is.True);
            Assert.That(Simulation.State.Shards, Is.EqualTo(shards), "relógio voltado: nada, sem erro (D-196)");
            Assert.That(Summary, Does.Contain("relógio voltou"));
        }

        /// <summary>
        /// O caminho do jogo: grava com a hora, "fecha", e abre 10 min depois pelo save; o estagiário trouxe as lascas uma
        /// vez só (abrir de novo logo em seguida não paga outra vez).
        /// </summary>
        [UnityTest]
        public IEnumerator ReopeningThroughTheSave_PaysTheTimeAway_Once()
        {
            yield return Load();
            var now = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
            _runner.UseClock(() => now);
            var store = new FileSaveStore(_dir);
            _runner.UseSave(new SaveService(store));
            Assert.That(_runner.Simulation.HireIntern(), Is.True);
            _runner.SaveNow();
            double before = _runner.Simulation.State.Shards;

            now = now.AddMinutes(10);
            _runner.UseSave(new SaveService(store)); // abrir de novo: carrega o save e conta o tempo fora
            var report = _runner.LastOffline.Value;
            Assert.That(report.SecondsAway, Is.EqualTo(600).Within(1e-6));
            Assert.That(report.Crystals, Is.EqualTo(1200), "10 min a 0,5 s por cristal");
            Assert.That(_runner.Simulation.State.Shards, Is.EqualTo(before + report.Shards).Within(1e-6));

            double afterFirst = _runner.Simulation.State.Shards;
            _runner.UseSave(new SaveService(store)); // abre de novo no mesmo instante: já gravou com a hora nova
            Assert.That(_runner.LastOffline.Value.SecondsAway, Is.EqualTo(0).Within(1e-6));
            Assert.That(_runner.Simulation.State.Shards, Is.EqualTo(afterFirst).Within(1e-6), "não paga duas vezes");
        }
    }
}
