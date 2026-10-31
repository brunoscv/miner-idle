using System;
using System.Collections;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MinersIdle.Tests
{
    /// <summary>
    /// Marco 5, E5 (pendência do M2 pela D-32): o loop de mineração não aloca memória a cada golpe, toque ou crítico, com
    /// a cena inteira escutando (feedback, som, HUD). Conta as alocações (marcador "GC.Alloc" do Profiler) das chamadas
    /// que cada quadro faz, depois de aquecer os pools com quadros de verdade passando.
    ///
    /// Alocações que ficam, de propósito, e por evento (não por quadro): o objeto <see cref="Crystal"/> de cada cristal
    /// que nasce e o texto de LASCAS a cada quebra (o formatador único devolve string). E uma só do Editor: o
    /// <c>SetText</c> do TextMeshPro copia o texto para o Inspector (<c>#if UNITY_EDITOR</c>); no aparelho é zero.
    /// </summary>
    public class AllocationTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        /// <summary>Só no Editor: cada <c>TMP_Text.SetText</c> monta a cópia do texto para o Inspector.</summary>
        const int EditorAllocsPerSetText = 2;

        /// <summary>
        /// Teto por quebra ou nascimento de cristal: a quebra formata LASCAS (a string e o buffer do <c>ToString</c>); o
        /// nascimento cria o <see cref="Crystal"/>. Medido em 2026-10-21: 4 alocações para 1 quebra e 1 nascimento.
        /// </summary>
        const int MaxAllocsPerEvent = 2;

        MiningRunner _runner;
        TapInput _tapInput;

        static int Allocations(Action action)
        {
            var recorder = Recorder.Get("GC.Alloc");
            recorder.FilterToCurrentThread();
            recorder.enabled = false;
            recorder.enabled = true;
            action();
            recorder.enabled = false;
            int count = recorder.sampleBlockCount;
            recorder.CollectFromAllThreads();
            return count;
        }

        /// <summary>Abre a cena e aquece tudo com quadros passando: pools crescem e devolvem, textos e sons já tocaram.</summary>
        IEnumerator LoadAndWarmUp()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;

            _runner = UnityEngine.Object.FindAnyObjectByType<MiningRunner>();
            _tapInput = UnityEngine.Object.FindAnyObjectByType<TapInput>();

            _tapInput.AutoTap = true;
            _runner.TimeScale = 20f;
            _runner.ForceCritical = true;
            yield return new WaitForSecondsRealtime(2f);
            _runner.ForceCritical = false;
            yield return new WaitForSecondsRealtime(1.5f);
            _tapInput.AutoTap = false;
            _runner.TimeScale = 1f;

            // O pool de cristais na lotação máxima (e um pouco mais): um cristal a mais na tela do que no aquecimento
            // criaria uma view nova, que é crescimento do pool, não custo do loop.
            var simulation = _runner.Simulation;
            while (simulation.Crystals.Count < simulation.Rules.MaxCrystals + 3 && simulation.SpawnCrystalNow() != null) { }
            yield return null;
            for (int i = 0; i < 3; i++) simulation.BreakTarget();
            yield return Settle();

            // Ensaio do combo até o x10, do jeito que os testes medem: o primeiro "xN" que reaparece paga custos de
            // primeira vez (51 alocações na medição, 2 na segunda vez).
            for (int i = 0; i < 10; i++) _runner.Simulation.Tap();
            yield return Settle();
        }

        /// <summary>
        /// Alocações de <paramref name="action"/> e quantas quebras e nascimentos aconteceram nela. Com o toque do GDD v5
        /// (33% do golpe, D-86/D-103) um toque pode quebrar o cristal; a quebra e o nascimento alocam de propósito
        /// (<see cref="MaxAllocsPerEvent"/>), então o teto de cada medida desconta esses eventos.
        /// </summary>
        int AllocationsAndEvents(Action action, out int events)
        {
            var simulation = _runner.Simulation;
            int count = 0;
            Action<Crystal> onEvent = _ => count++;
            simulation.CrystalBroken += onEvent;
            simulation.CrystalSpawned += onEvent;
            int allocations = Allocations(action);
            simulation.CrystalBroken -= onEvent;
            simulation.CrystalSpawned -= onEvent;
            events = count;
            return allocations;
        }

        /// <summary>Deixa faíscas, textos e lascas voltarem aos pools e o combo zerar (1 s de jogo sem toque).</summary>
        static IEnumerator Settle() => new WaitForSecondsRealtime(1.5f);

        [UnityTest]
        public IEnumerator TapCriticalAndUiCheck_DoNotAllocate()
        {
            yield return LoadAndWarmUp();
            var simulation = _runner.Simulation;
            Assume.That(simulation.Miner.Target, Is.Not.Null);

            int tap = AllocationsAndEvents(() => simulation.Tap(), out int tapEvents);
            yield return Settle();

            _runner.ForceCritical = true;
            int critical = AllocationsAndEvents(() => simulation.Tap(), out int criticalEvents);
            _runner.ForceCritical = false;
            yield return Settle();

            // O toque de verdade: passa pela checagem "caiu na interface?" (raycast de UI) antes de virar Tap().
            int screenTap = AllocationsAndEvents(
                () => _tapInput.TapAt(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)), out int screenEvents);

            Debug.Log($"[Alloc] toque {tap} · crítico {critical} · toque na tela {screenTap} " +
                      $"(quebras e nascimentos: {tapEvents} · {criticalEvents} · {screenEvents})");
            Assert.That(tap, Is.LessThanOrEqualTo(MaxAllocsPerEvent * tapEvents), "toque (golpe, tremor, faíscas, som)");
            Assert.That(critical, Is.LessThanOrEqualTo(MaxAllocsPerEvent * criticalEvents), "crítico (CRÍTICO!, tremor de tela, som)");
            Assert.That(screenTap, Is.LessThanOrEqualTo(MaxAllocsPerEvent * screenEvents), "toque na tela, com a checagem da interface");
        }

        [UnityTest]
        public IEnumerator ComboAndMilestone_AllocateOnlyTheEditorCopyOfTheText()
        {
            yield return LoadAndWarmUp();
            var simulation = _runner.Simulation;
            simulation.Tap(); // combo 1: o "xN" ainda não aparece

            yield return null;
            int combo = AllocationsAndEvents(() => simulation.Tap(), out int comboEvents); // "x2" aparece
            int comboTaps = AllocationsAndEvents(
                () => { for (int i = 0; i < 7; i++) simulation.Tap(); }, out int comboTapsEvents); // x3…x9
            int milestone = AllocationsAndEvents(() => simulation.Tap(), out int milestoneEvents); // x10 e o "x10!"

            Debug.Log($"[Alloc] x2 {combo} · x3…x9 {comboTaps} · x10 {milestone} " +
                      $"(quebras e nascimentos: {comboEvents} · {comboTapsEvents} · {milestoneEvents})");
            Assert.That(combo, Is.LessThanOrEqualTo(EditorAllocsPerSetText + MaxAllocsPerEvent * comboEvents), "x2");
            Assert.That(comboTaps, Is.LessThanOrEqualTo(7 * EditorAllocsPerSetText + MaxAllocsPerEvent * comboTapsEvents),
                        "x3…x9: só o SetText do \"xN\"");
            Assert.That(milestone, Is.LessThanOrEqualTo(2 * EditorAllocsPerSetText + MaxAllocsPerEvent * milestoneEvents),
                        "x10: \"xN\" e \"x10!\" pelo SetText");
        }

        /// <summary>M8 (E2): com a parede da passagem na tela, o anão batendo nela e o corredor desenhando, nada aloca.</summary>
        [UnityTest]
        public IEnumerator HittingTheWall_DoesNotAllocate()
        {
            yield return LoadAndWarmUp();
            var simulation = _runner.Simulation;
            if (!simulation.Economy.Get(UpgradeId.Letrado).IsEnabled)
                Assert.Ignore("O balanço ativo não tem o Letrado (rode o setup do projeto).");
            var stage = UnityEngine.Object.FindAnyObjectByType<DescentStage>();

            simulation.AddShards(1e6);
            simulation.FillCaveBreaks();
            simulation.Purchase(UpgradeId.Letrado);
            Assert.That(simulation.PassCave(), Is.True);
            for (int i = 0; i < 20 && simulation.Miner.Target != simulation.Wall; i++)
            {
                simulation.Tick(0);
                if (simulation.Miner.Target != simulation.Wall) simulation.BreakTarget();
            }
            _runner.TimeScale = 20f;
            for (float t = 0; t < 3f && simulation.Miner.State != MinerState.Mining; t += Time.unscaledDeltaTime) yield return null;
            _runner.TimeScale = 0f; // só os Tick do teste andam
            yield return Settle();
            Assume.That(simulation.Wall, Is.Not.Null, "a parede ainda de pé");
            Assume.That(simulation.Miner.State, Is.EqualTo(MinerState.Mining), "o anão na parede");

            // Um quadro de ensaio (tremor, faíscas e barra da parede pela primeira vez), depois um intervalo de golpe
            // inteiro em quadros de 60 fps (2 s no GDD v5): pelo menos um golpe cai dentro da medida.
            simulation.Tick(simulation.Stats.HitInterval);
            stage.Draw();
            yield return Settle();
            int frames = (int)Math.Ceiling(simulation.Stats.HitInterval * 60) + 1;
            int hits = 0;
            Action<MiningHit> onHit = _ => hits++;
            simulation.HitLanded += onHit;
            int total = AllocationsAndEvents(() =>
            {
                for (int f = 0; f < frames; f++)
                {
                    simulation.Tick(1.0 / 60);
                    stage.Draw();
                }
            }, out int events);
            simulation.HitLanded -= onHit;

            Debug.Log($"[Alloc] parede: {frames} quadros · {hits} golpes · {total} alocações (quebras e nascimentos: {events})");
            Assume.That(simulation.Wall, Is.Not.Null, "a parede aguentou a medida");
            Assert.That(stage.IsWallShown, Is.True);
            Assert.That(hits, Is.GreaterThan(0), "o anão bateu na parede");
            Assert.That(total, Is.LessThanOrEqualTo(MaxAllocsPerEvent * events), "nada por golpe nem por quadro");
        }

        [UnityTest]
        public IEnumerator WalkingAndHitting_AllocateOnlyWhenACrystalBreaksOrIsBorn()
        {
            yield return LoadAndWarmUp();
            var simulation = _runner.Simulation;
            int breaks = 0, spawns = 0, hits = 0;
            Action<Crystal> onBreak = _ => breaks++;
            Action<Crystal> onSpawn = _ => spawns++;
            Action<MiningHit> onHit = _ => hits++;
            simulation.CrystalBroken += onBreak;
            simulation.CrystalSpawned += onSpawn;
            simulation.HitLanded += onHit;

            // 20 s de jogo em passos de quadro (60 fps), em blocos de 1 s. Entre os blocos, tempo real para as faíscas
            // (0,35 s) e as lascas voltarem aos pools; sem isso o pool cresce e a conta mede o teste, não o jogo.
            _runner.TimeScale = 0f; // só os Tick do teste andam
            int total = 0;
            for (int second = 0; second < 20; second++)
            {
                total += Allocations(() => { for (int f = 0; f < 60; f++) simulation.Tick(1.0 / 60); });
                yield return new WaitForSecondsRealtime(0.5f);
            }

            simulation.CrystalBroken -= onBreak;
            simulation.CrystalSpawned -= onSpawn;
            simulation.HitLanded -= onHit;

            Debug.Log($"[Alloc] 20 s de jogo:{hits} golpes · {breaks} quebras · {spawns} nascimentos · {total} alocações");
            Assert.That(hits, Is.GreaterThan(5), "o anão bateu");
            Assert.That(total, Is.LessThanOrEqualTo(MaxAllocsPerEvent * (breaks + spawns)),
                        "só nas quebras (texto de LASCAS) e nos nascimentos (o Crystal); nada por golpe nem por quadro");
        }
    }
}
