using System;
using System.Collections;
using System.Diagnostics;
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
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9, E5 (Definição de Pronto do Roadmap): uma sessão de 2 h de jogo em ×20 na cena Game de verdade, com o save numa
    /// pasta temporária. Um jogador robô toca, compra o que dá, contrata o estagiário, passa de caverna, descansa pelo botão
    /// (com o resumo do dia), compra permanentes, minimiza e volta, simula offlines (COLETAR no modal) e fecha e abre o jogo
    /// no meio. Nada pode travar (um erro no log reprova o teste) nem se perder: a cada reabertura, o progresso é o mesmo de
    /// antes de fechar, e lascas nunca ficam negativas nem viram NaN.
    ///
    /// O tempo anda pelo mesmo caminho do <see cref="MiningRunner"/> (<c>Tick</c>, em pedaços de 1/3 s, como ×20 a 60 qps),
    /// só que vários pedaços por quadro para a sessão caber em cerca de um minuto; as views desenham a cada quadro.
    /// </summary>
    public class LongSessionTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
        const double SessionSeconds = 2 * 3600;
        const double Speed = 20;
        const double StepSeconds = Speed / 60.0;
        const int StepsPerFrame = 6;

        string _dir;
        DateTime _now;
        MiningRunner _runner;
        OfflineReturnScreen _offline;
        RestButton _restButton;
        RestScreen _restModal;
        DaySummaryScreen _summary;

        MiningSimulation Simulation => _runner.Simulation;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "MinersIdleLongSession_" + Guid.NewGuid().ToString("N"));
            _now = new DateTime(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        /// <summary>Abre a cena pelo caminho do <c>GameBootstrap</c> (save na pasta temporária), com o relógio do teste.</summary>
        IEnumerator Open()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            _offline = Object.FindAnyObjectByType<OfflineReturnScreen>();
            _restButton = Object.FindAnyObjectByType<RestButton>();
            _restModal = Object.FindAnyObjectByType<RestScreen>();
            _summary = Object.FindAnyObjectByType<DaySummaryScreen>();
            Assert.That(_offline, Is.Not.Null, "rode o setup do projeto");
            _runner.UseClock(() => _now);
            _runner.TimeScale = 0f; // o teste anda o tempo; o Update do runner só cuida do autosave
            _runner.UseSave(new SaveService(new FileSaveStore(_dir)));
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator TwoHoursAtX20_WithRestsPurchasesOfflinesAndReopening_NothingStuckNorLost()
        {
            yield return Open();
            if (!Simulation.Economy.HasIntern || !Simulation.Permanents.Rules.IsRestEnabled)
                Assert.Ignore("O balanço ativo não tem estagiário ou descanso (rode o setup: GDD v7).");

            var clock = Stopwatch.StartNew();
            double played = 0;
            int frames = 0, rests = 0, offlines = 0, minimized = 0, reopened = 0, purchases = 0, caves = 0, maxCave = 1;
            double nextShop = 0, nextOffline = 15 * 60, nextMinimize = 25 * 60, nextReopen = 40 * 60, nextRest = 30 * 60;
            var random = new System.Random(9);

            while (played < SessionSeconds)
            {
                for (int i = 0; i < StepsPerFrame; i++)
                {
                    Simulation.Tick(StepSeconds);
                    played += StepSeconds;
                    _now = _now.AddSeconds(StepSeconds / Speed);
                }
                Simulation.Tap();
                CheckSane();
                maxCave = Math.Max(maxCave, Simulation.State.Cave);

                if (played >= nextShop)
                {
                    nextShop += 30;
                    purchases += BuyWhatFits();
                    if (Simulation.CanPassCave && Simulation.PassCave()) caves++;
                }

                if (played >= nextOffline)
                {
                    nextOffline += 25 * 60;
                    double away = random.Next(2, 8 * 60) * 60.0; // 2 min a 8 h
                    _runner.SimulateAway(away);
                    Collect();
                    offlines++;
                }

                if (played >= nextMinimize)
                {
                    nextMinimize += 25 * 60;
                    _runner.SendMessage("OnApplicationPause", true);
                    _now = _now.AddMinutes(random.Next(1, 30));
                    _runner.SendMessage("OnApplicationPause", false);
                    Collect();
                    minimized++;
                }

                if (played >= nextRest)
                {
                    nextRest += 30 * 60;
                    if (!Simulation.CanRest) Simulation.SkipToCave(Simulation.Permanents.Rules.RestLevel); // o atalho do DBG
                    RestByTheButton();
                    rests++;
                    purchases += BuyPermanents();
                }

                if (played >= nextReopen)
                {
                    nextReopen += 40 * 60;
                    var before = Snapshot.Of(Simulation);
                    _runner.SaveNow();
                    yield return Open(); // fecha e abre no mesmo instante: nada de offline
                    Snapshot.Of(Simulation).AssertSameAs(before, $"reabertura {reopened + 1}");
                    reopened++;
                }

                frames++;
                yield return null;
            }

            var end = Snapshot.Of(Simulation);
            _runner.SaveNow();
            yield return Open();
            Snapshot.Of(Simulation).AssertSameAs(end, "fim da sessão");

            Debug.Log($"[Sessão longa] {played / 60:0} min de jogo em {clock.Elapsed.TotalSeconds:0} s reais, {frames} quadros: " +
                      $"{purchases} compras, {caves} passagens (nível máximo {maxCave}), {rests} descansos, {offlines} offlines, " +
                      $"{minimized} minimizações, {reopened + 1} reaberturas; dia {Simulation.Permanents.State.Day}, " +
                      $"{NumberFormatter.Format(Simulation.Permanents.State.RedShards)} lascas vermelhas.");
            Assert.That(rests, Is.GreaterThanOrEqualTo(3));
            Assert.That(offlines + minimized, Is.GreaterThanOrEqualTo(8));
            Assert.That(Simulation.Permanents.State.Day, Is.EqualTo(rests + 1), "cada descanso é um dia, sem pular nem repetir");
        }

        void CheckSane()
        {
            var state = Simulation.State;
            Assert.That(double.IsNaN(state.Shards) || double.IsInfinity(state.Shards), Is.False, "lascas viraram NaN/∞");
            Assert.That(state.Shards, Is.GreaterThanOrEqualTo(0), "lascas negativas");
            Assert.That(Simulation.Permanents.State.RedShards, Is.GreaterThanOrEqualTo(0));
        }

        /// <summary>O jogador compra tudo o que dá, da linha mais barata para a mais cara, e contrata o estagiário.</summary>
        int BuyWhatFits()
        {
            int bought = 0;
            if (Simulation.HireIntern()) bought++;
            for (int round = 0; round < 50; round++)
            {
                bool any = false;
                foreach (UpgradeId id in Enum.GetValues(typeof(UpgradeId)))
                    if (Simulation.Purchase(id) == PurchaseResult.Purchased)
                    {
                        bought++;
                        any = true;
                    }
                if (!any) break;
            }
            return bought;
        }

        int BuyPermanents()
        {
            int bought = 0;
            foreach (PermanentId id in Enum.GetValues(typeof(PermanentId)))
                while (Simulation.PurchasePermanent(id) == PurchaseResult.Purchased && bought < 100)
                    bought++;
            return bought;
        }

        /// <summary>O COLETAR do "Bem-vindo de volta", se ele abriu.</summary>
        void Collect()
        {
            if (!_offline.IsOpen) return;
            double before = Simulation.State.Shards;
            double pending = Simulation.PendingOffline.Shards;
            _offline.transform.Find("Screen/Panel/Buttons/Collect").GetComponent<Button>().onClick.Invoke();
            Assert.That(_offline.IsOpen, Is.False);
            Assert.That(Simulation.State.Shards, Is.EqualTo(before + pending).Within(1e-6 * Math.Max(1, pending)),
                        "o COLETAR paga o que mostrou, uma vez");
        }

        /// <summary>Descansar como o jogador: o botão, CONFIRMAR no modal e COMEÇAR no resumo do dia.</summary>
        void RestByTheButton()
        {
            int day = Simulation.Permanents.State.Day;
            _restButton.transform.Find("RestButton").GetComponent<Button>().onClick.Invoke();
            Assert.That(_restModal.IsOpen, Is.True, "o Descansar abre o modal");
            _restModal.transform.Find("Screen/Panel/Confirm").GetComponent<Button>().onClick.Invoke();
            Assert.That(_summary.IsOpen, Is.True, "o resumo do dia");
            _summary.transform.Find("Screen/Panel/Start").GetComponent<Button>().onClick.Invoke();
            Assert.That(Simulation.Permanents.State.Day, Is.EqualTo(day + 1));
            Assert.That(Simulation.State.Cave, Is.EqualTo(1), "a run nova");
        }

        /// <summary>O que não pode se perder ao fechar e abrir.</summary>
        readonly struct Snapshot
        {
            readonly double _shards, _earned, _red, _pending;
            readonly int _cave, _day;
            readonly long _caveBreaks;
            readonly int[] _levels, _permanents;

            Snapshot(MiningSimulation simulation)
            {
                var state = simulation.State;
                _shards = state.Shards;
                _earned = state.ShardsEarned;
                _pending = state.PendingOffline.Shards;
                _cave = state.Cave;
                _caveBreaks = state.CaveBreaks;
                var lines = (UpgradeId[])Enum.GetValues(typeof(UpgradeId));
                _levels = Array.ConvertAll(lines, state.LevelOf);
                var permanent = simulation.Permanents.State;
                _red = permanent.RedShards;
                _day = permanent.Day;
                var permanents = (PermanentId[])Enum.GetValues(typeof(PermanentId));
                _permanents = Array.ConvertAll(permanents, permanent.LevelOf);
            }

            public static Snapshot Of(MiningSimulation simulation) => new Snapshot(simulation);

            public void AssertSameAs(Snapshot before, string when)
            {
                Assert.That(_shards, Is.EqualTo(before._shards).Within(1e-9 * Math.Max(1, before._shards)), when + ": lascas");
                Assert.That(_earned, Is.EqualTo(before._earned).Within(1e-9 * Math.Max(1, before._earned)), when + ": lascas ganhas");
                Assert.That(_pending, Is.EqualTo(before._pending).Within(1e-9), when + ": volta pendente");
                Assert.That(_cave, Is.EqualTo(before._cave), when + ": nível");
                Assert.That(_caveBreaks, Is.EqualTo(before._caveBreaks), when + ": quebras no nível");
                Assert.That(_levels, Is.EqualTo(before._levels), when + ": melhorias");
                Assert.That(_red, Is.EqualTo(before._red).Within(1e-9), when + ": lascas vermelhas");
                Assert.That(_day, Is.EqualTo(before._day), when + ": dia");
                Assert.That(_permanents, Is.EqualTo(before._permanents), when + ": permanentes");
            }
        }
    }
}
