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
    /// M9, E1 e E2: o offline na cena Game (GDD v7). O DBG → Offline simula o jogo fechado sem fechar e volta o relógio; ao
    /// abrir pelo caminho do save (save v5, com a hora), o tempo desde a última gravação vira lascas do estagiário. Com 60 s
    /// ou mais, elas esperam o COLETAR do "Bem-vindo de volta" (D-195, D-197) e entram uma vez só, mesmo fechando e abrindo
    /// no meio do modal. Aberta direto, a cena não grava nada.
    /// </summary>
    public class OfflineFlowTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        DebugPanel _panel;
        MiningRunner _runner;
        OfflineReturnScreen _modal;
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
            _modal = Object.FindAnyObjectByType<OfflineReturnScreen>();
            Assert.That(_modal, Is.Not.Null, "modal Bem-vindo de volta (rode o setup do projeto)");
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

        /// <summary>O COLETAR do modal, como o dedo do jogador.</summary>
        void Collect()
        {
            Assert.That(_modal.IsOpen, Is.True, "o modal está aberto");
            _modal.transform.Find("Screen/Panel/Buttons/Collect").GetComponent<Button>().onClick.Invoke();
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
            Assert.That(_modal.IsOpen, Is.False, "sem estagiário, sem modal");
        }

        [UnityTest]
        public IEnumerator WithTheIntern_OneHourAway_ShowsTheModal_AndPaysOnCollect()
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
            Assert.That(report.Shards, Is.EqualTo(7200 * 1.2).Within(1e-6));

            // O "Bem-vindo de volta" (D-197): as lascas esperam o COLETAR.
            Assert.That(_modal.IsOpen, Is.True);
            Assert.That(Simulation.State.Shards, Is.EqualTo(0), "ainda não entrou");
            Assert.That(_modal.AwayText, Is.EqualTo("1 h 00 min"));
            Assert.That(_modal.ShowsCounted, Is.False, "não passou do teto");
            Assert.That(_modal.CrystalsText, Is.EqualTo(NumberFormatter.Format(7200)));
            Assert.That(_modal.ShardsText, Is.EqualTo("+" + NumberFormatter.FormatShards(7200 * 1.2)));
            Assert.That(_modal.NoteText, Is.EqualTo("Ele minerou no nível 1 enquanto você esteve fora."));

            Collect();
            Assert.That(_modal.IsOpen, Is.False);
            Assert.That(Simulation.State.Shards, Is.EqualTo(report.Shards).Within(1e-6), "entrou no COLETAR");
            Assert.That(Simulation.State.ShardsEarned, Is.EqualTo(report.Shards).Within(1e-6), "nas lascas do dia");
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

            // O modal mostra o corte: "Contou 6 h (máx.)".
            Assert.That(_modal.IsOpen, Is.True);
            Assert.That(_modal.AwayText, Is.EqualTo("8 h 00 min"));
            Assert.That(_modal.ShowsCounted, Is.True);
            Assert.That(_modal.CountedText, Is.EqualTo("6 h (máx.)"));
            Assert.That(_modal.NoteText, Is.EqualTo("Ele minera no máximo 6 h com o jogo fechado."));

            double pending = Simulation.PendingOffline.Shards;
            Click("ClockBack");
            Assert.That(_runner.LastOffline.Value.ClockWentBack, Is.True);
            Assert.That(Simulation.PendingOffline.Shards, Is.EqualTo(pending), "relógio voltado: nada, sem erro (D-196)");
            Assert.That(Simulation.State.Shards, Is.EqualTo(0));
            Assert.That(Summary, Does.Contain("relógio voltou"));

            Collect();
            Assert.That(Simulation.State.Shards, Is.EqualTo(pending).Within(1e-6));
        }

        /// <summary>D-195: menos de 60 s fora, as lascas entram direto, sem o modal.</summary>
        [UnityTest]
        public IEnumerator ThirtySecondsAway_PaysDirectly_WithoutTheModal()
        {
            yield return Load();
            Simulation.HireIntern();

            Click("Away30S");

            var report = _runner.LastOffline.Value;
            Assert.That(report.Crystals, Is.EqualTo(60), "nível 1: 30 s a 0,5 s por cristal");
            Assert.That(_modal.IsOpen, Is.False);
            Assert.That(Simulation.State.Shards, Is.EqualTo(report.Shards).Within(1e-6));
            Assert.That(Simulation.PendingOffline.HasShards, Is.False);
        }

        /// <summary>
        /// O caminho do jogo: grava com a hora, "fecha", e abre 10 min depois pelo save; o modal mostra o que o estagiário
        /// trouxe. Fechar e abrir de novo com o modal na tela não paga outra vez nem perde a volta (ela está no save); o
        /// COLETAR paga uma vez, e abrir depois dele não traz o modal de volta.
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
            Assert.That(_modal.IsOpen, Is.True);
            Assert.That(_modal.AwayText, Is.EqualTo("10 min 00 s"));
            Assert.That(_runner.Simulation.State.Shards, Is.EqualTo(before).Within(1e-6), "espera o COLETAR");

            _runner.UseSave(new SaveService(store)); // fecha e abre com o modal na tela, no mesmo instante
            Assert.That(_runner.LastOffline.Value.SecondsAway, Is.EqualTo(0).Within(1e-6));
            Assert.That(_modal.IsOpen, Is.True, "a volta pendente veio do save");
            Assert.That(_runner.Simulation.PendingOffline.Shards, Is.EqualTo(report.Shards).Within(1e-6), "nem dobra nem some");
            Assert.That(_runner.Simulation.State.Shards, Is.EqualTo(before).Within(1e-6));

            Collect();
            Assert.That(_runner.Simulation.State.Shards, Is.EqualTo(before + report.Shards).Within(1e-6));

            _runner.UseSave(new SaveService(store)); // abre de novo depois do COLETAR: já gravou sem a volta
            Assert.That(_modal.IsOpen, Is.False);
            Assert.That(_runner.Simulation.State.Shards, Is.EqualTo(before + report.Shards).Within(1e-6), "não paga duas vezes");
        }

        /// <summary>
        /// M9, E3: minimizar e voltar várias vezes (o Android pausa o app) não duplica lascas nem compras. Cada volta conta
        /// só o tempo desde a última gravação; o que ela trouxe entra uma vez, e o save reaberto tem o mesmo progresso.
        /// </summary>
        [UnityTest]
        public IEnumerator MinimizingAndComingBack_ManyTimes_DuplicatesNothing()
        {
            yield return Load();
            var now = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
            _runner.UseClock(() => now);
            var store = new FileSaveStore(_dir);
            _runner.UseSave(new SaveService(store));
            var simulation = _runner.Simulation;
            Assert.That(simulation.HireIntern(), Is.True);
            simulation.AddShards(1000);
            Assert.That(simulation.Purchase(UpgradeId.Pickaxe), Is.EqualTo(PurchaseResult.Purchased));
            int pickaxe = simulation.State.LevelOf(UpgradeId.Pickaxe);

            _runner.SendMessage("OnApplicationPause", true);   // minimiza: grava
            now = now.AddMinutes(10);
            _runner.SendMessage("OnApplicationPause", false);  // volta 10 min depois
            double brought = simulation.PendingOffline.Shards;
            Assert.That(brought, Is.GreaterThan(0));

            for (int i = 0; i < 3; i++)
            {
                _runner.SendMessage("OnApplicationPause", true);
                _runner.SendMessage("OnApplicationPause", false); // no mesmo instante: nada novo
            }
            Assert.That(simulation.PendingOffline.Shards, Is.EqualTo(brought).Within(1e-6), "a volta não dobra");

            double before = simulation.State.Shards;
            Collect();
            double after = simulation.State.Shards;
            Assert.That(after, Is.EqualTo(before + brought).Within(1e-6));

            for (int i = 0; i < 3; i++)
            {
                _runner.SendMessage("OnApplicationPause", true);
                _runner.SendMessage("OnApplicationPause", false);
            }
            Assert.That(simulation.State.Shards, Is.EqualTo(after).Within(1e-6), "nada entra de novo");
            Assert.That(simulation.State.LevelOf(UpgradeId.Pickaxe), Is.EqualTo(pickaxe), "a compra não dobra");
            Assert.That(_modal.IsOpen, Is.False);

            _runner.UseSave(new SaveService(store)); // fecha e abre: o save tem o mesmo progresso
            Assert.That(_runner.Simulation.State.Shards, Is.EqualTo(after).Within(1e-6));
            Assert.That(_runner.Simulation.State.LevelOf(UpgradeId.Pickaxe), Is.EqualTo(pickaxe));
            Assert.That(_modal.IsOpen, Is.False);
        }
    }
}
