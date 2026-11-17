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
    /// M9c E5 (GDD v8): o robô desce de andar <b>pelo botão</b> do Próximo Andar quando a barra enche; até o recorde 5 só o
    /// botão desce (nem o estagiário se contrata); do 5 em diante o avanço automático (ligado nos AJUSTES, o de fábrica)
    /// desce sozinho, também no offline. Nos descansos ele compra a Memória, que encurta a barra.
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
        NextFloorButton _next;

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
            _next = Object.FindAnyObjectByType<NextFloorButton>();
            Assert.That(_offline, Is.Not.Null, "rode o setup do projeto");
            Assert.That(_next, Is.Not.Null, "a cena precisa do Próximo Andar (rode o setup do projeto)");
            _runner.AutoAdvance = true; // o "Avanço automático" dos AJUSTES, ligado (o de fábrica), sem depender do settings.json da máquina
            _runner.UseClock(() => _now);
            _runner.TimeScale = 0f; // o teste anda o tempo; o Update do runner só cuida do autosave
            _runner.UseSave(new SaveService(new FileSaveStore(_dir)));
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator TwoHoursAtX20_WithRestsPurchasesOfflinesAndReopening_NothingStuckNorLost()
        {
            yield return Open();
            if (!Simulation.Economy.HasIntern || !Simulation.Permanents.Rules.IsRestEnabled || Simulation.Rules.AutoAdvanceFloor <= 0)
                Assert.Ignore("O balanço ativo não tem estagiário, descanso ou avanço automático (rode o setup: GDD v8).");
            Assert.That(Simulation.DeepestFloor, Is.EqualTo(1), "a sessão começa de um save novo");
            int autoFloor = Simulation.Rules.AutoAdvanceFloor;

            var clock = Stopwatch.StartNew();
            double played = 0;
            int frames = 0, rests = 0, offlines = 0, minimized = 0, reopened = 0, purchases = 0, caves = 0, maxCave = 1;
            int autoFloors = 0, offlineFloors = 0, hiredOnFloor = 0;
            double memoryBought = -1;
            double nextShop = 0, nextOffline = 15 * 60, nextMinimize = 25 * 60, nextReopen = 40 * 60, nextRest = 30 * 60;
            var random = new System.Random(9);

            while (played < SessionSeconds)
            {
                // O tempo anda; só o avanço automático pode mudar o andar aqui, e só depois do recorde 5 (P6).
                bool autoBefore = Simulation.IsAutoAdvancing;
                int caveBefore = Simulation.State.Cave;
                for (int i = 0; i < StepsPerFrame; i++)
                {
                    Simulation.Tick(StepSeconds);
                    played += StepSeconds;
                    _now = _now.AddSeconds(StepSeconds / Speed);
                }
                if (Simulation.State.Cave != caveBefore)
                {
                    Assert.That(autoBefore, Is.True, $"o andar {caveBefore} mudou sem o toque antes do recorde {autoFloor}");
                    Assert.That(Simulation.State.Cave, Is.GreaterThan(caveBefore), "o automático só desce");
                    autoFloors += Simulation.State.Cave - caveBefore;
                }
                Simulation.Tap();
                CheckSane();

                // A barra cheia acende o botão; o jogador toca nele (sem o automático, é o único jeito de descer).
                if (Simulation.CanAdvance && Simulation.State.Cave < Simulation.Rules.CaveCount && ClickNextFloor()) caves++;
                maxCave = Math.Max(maxCave, Simulation.State.Cave);

                if (played >= nextShop)
                {
                    nextShop += 30;
                    bool hiredBefore = Simulation.IsInternHired;
                    purchases += BuyWhatFits();
                    if (!hiredBefore && Simulation.IsInternHired)
                    {
                        if (hiredOnFloor == 0) hiredOnFloor = Simulation.DeepestFloor;
                        Assert.That(Simulation.DeepestFloor, Is.GreaterThanOrEqualTo(Simulation.Rules.InternFloor),
                                    "o estagiário só se contrata com o recorde no andar dele (P7)");
                    }
                }

                if (played >= nextOffline)
                {
                    nextOffline += 25 * 60;
                    double away = random.Next(2, 8 * 60) * 60.0; // 2 min a 8 h
                    int from = Simulation.State.Cave;
                    bool auto = Simulation.IsAutoAdvancing;
                    _runner.SimulateAway(away);
                    if (!auto) Assert.That(Simulation.State.Cave, Is.EqualTo(from), "sem o automático, o offline não desce");
                    if (_offline.IsOpen && Simulation.State.Cave != from)
                    {
                        Assert.That(_offline.ShowsFloors, Is.True, "o \"Bem-vindo de volta\" mostra os andares descidos");
                        offlineFloors += Simulation.State.Cave - from;
                    }
                    Collect();
                    maxCave = Math.Max(maxCave, Simulation.State.Cave);
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
                    int deepest = Simulation.DeepestFloor;
                    RestByTheButton();
                    rests++;
                    Assert.That(Simulation.DeepestFloor, Is.EqualTo(deepest), "o recorde sobrevive ao descanso");
                    // A Memória medida no andar do recorde: nos primeiros andares ela já pode ter zerado a barra.
                    long requirement = Simulation.Permanents.FloorRequirement(Simulation.Rules, deepest);
                    purchases += BuyPermanents();
                    double memory = Simulation.Permanents.State.LevelOf(PermanentId.Memory);
                    if (memory > Math.Max(0, memoryBought))
                    {
                        Assert.That(Simulation.Permanents.FloorRequirement(Simulation.Rules, deepest), Is.LessThan(requirement),
                                    $"a Memória encurta a barra do andar {deepest}");
                        memoryBought = memory;
                    }
                }

                if (played >= nextReopen)
                {
                    nextReopen += 40 * 60;
                    // Um tick sem tempo antes da foto: o que vence agora acontece aqui, não no primeiro quadro depois de abrir
                    // (M9c E3: um descanso no mesmo quadro, com a Memória, abre o andar 1 de barra cheia e o automático desce).
                    Simulation.Tick(0);
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
                      $"{purchases} compras, {caves} andares pelo botão, {autoFloors} pelo automático e {offlineFloors} no offline " +
                      $"(andar máximo {maxCave}, recorde {Simulation.DeepestFloor}, estagiário no recorde {hiredOnFloor}), " +
                      $"{rests} descansos, {offlines} offlines, {minimized} minimizações, {reopened + 1} reaberturas; " +
                      $"dia {Simulation.Permanents.State.Day}, Memória {Math.Max(0, memoryBought)}, " +
                      $"{NumberFormatter.Format(Simulation.Permanents.State.RedShards)} lascas vermelhas.");
            Assert.That(caves, Is.GreaterThanOrEqualTo(autoFloor - 1), $"o botão leva do andar 1 ao {autoFloor}");
            Assert.That(Simulation.DeepestFloor, Is.GreaterThanOrEqualTo(autoFloor));
            Assert.That(autoFloors, Is.GreaterThan(0), "depois do recorde, o automático desce sozinho");
            Assert.That(hiredOnFloor, Is.GreaterThanOrEqualTo(Simulation.Rules.InternFloor), "o estagiário foi contratado");
            Assert.That(memoryBought, Is.GreaterThan(0), "a Memória foi comprada");
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

        /// <summary>
        /// O jogador compra tudo o que dá na LOJA, linha a linha, o estagiário junto (pela compra, que respeita o andar 5;
        /// o <c>HireIntern</c> é o atalho do DBG e não olha o marco).
        /// </summary>
        int BuyWhatFits()
        {
            int bought = 0;
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

        /// <summary>O toque no Próximo Andar (o botão é a barra, D-269): <c>true</c> se desceu.</summary>
        bool ClickNextFloor()
        {
            int cave = Simulation.State.Cave;
            _next.transform.Find("Bar/NextFloorButton").GetComponent<Button>().onClick.Invoke();
            Assert.That(Simulation.State.Cave, Is.EqualTo(cave + 1), "o toque com a barra cheia desce um andar");
            Assert.That(Simulation.State.CaveBreaks, Is.EqualTo(0), "a barra zera no andar novo (D-259)");
            return true;
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
            readonly int _cave, _day, _deepest;
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
                _deepest = permanent.DeepestFloor;
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
                Assert.That(_deepest, Is.EqualTo(before._deepest), when + ": recorde");
                Assert.That(_permanents, Is.EqualTo(before._permanents), when + ": permanentes");
            }
        }
    }
}
