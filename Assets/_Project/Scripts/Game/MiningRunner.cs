using System;
using System.Collections.Generic;
using MinersIdle.Core;
using MinersIdle.Persistence;
using UnityEngine;

namespace MinersIdle.Game
{
    /// <summary>
    /// Dono da partida na cena Game (M2; a fila da descida desde o M6): monta a <see cref="MiningSimulation"/> com os valores e as melhorias do asset
    /// (M3) e a avança a cada quadro. As views só leem a simulação e escutam os eventos dela.
    ///
    /// Save (M4): o <c>GameBootstrap</c> entrega o <see cref="SaveService"/> (<see cref="UseSave"/>); daí em diante o
    /// progresso é carregado e salvo depois de cada compra, a cada troca de caverna (M8), a cada 30 s, ao minimizar e ao
    /// sair. Aberta direto (Play na
    /// cena Game, testes PlayMode), a cena roda sem save: os testes nunca mexem no progresso de quem joga.
    ///
    /// Cálculos (M8 E3b-3, D-143): o balanço sai de <see cref="Balances"/> (o primeiro é o do GDD) e a taxa e o incremento
    /// da Picareta podem ser trocados pelo DBG (<see cref="UseCalculation"/>). A escolha só é lida e gravada no aparelho
    /// pelo caminho do <c>GameBootstrap</c>, como o save, e nunca num build de release (lá vale sempre o GDD).
    ///
    /// Dia e melhorias permanentes (Marco 8b): o <see cref="PermanentState"/> fica aqui, de uma partida para a outra;
    /// <see cref="Rest"/> encerra o dia e começa a run nova com ele. Vai para o save junto com a run (save v4, E2): a cada
    /// descanso e a cada compra ou atalho das permanentes, além das gravações de sempre.
    /// </summary>
    public sealed class MiningRunner : MonoBehaviour
    {
        /// <summary>Teto do tempo de um quadro: depois de uma pausa do app não se simulam minutos de uma vez.</summary>
        public const float MaxFrameSeconds = 0.25f;

        /// <summary>Segundos reais entre salvamentos automáticos (reais: o ×20 do debug não multiplica as gravações).</summary>
        public const float AutosaveSeconds = 30f;

        [SerializeField] MiningRulesAsset rules;

        [Tooltip("Os balanços do DBG → Cálculos (E3b-3), na ordem do seletor. O primeiro é o do GDD (o padrão, o mesmo do " +
                 "campo acima): Balance_GDD_v7, depois Balance_GDD_v5.")]
        [SerializeField] MiningRulesAsset[] balances = Array.Empty<MiningRulesAsset>();

        [Tooltip("Ligado: toda partida usa a semente abaixo (para reproduzir um caso). Desligado: semente nova a cada partida.")]
        [SerializeField] bool useFixedSeed;
        [SerializeField] int fixedSeed = 1;

        [Tooltip("Offline (M9, E2, D-195): a partir de quantos segundos contados a volta espera o COLETAR do modal " +
                 "\"Bem-vindo de volta\"; abaixo disso as lascas entram direto, sem aviso. 0 = sempre direto.")]
        [SerializeField] double offlineNoticeSeconds = 60;

        SaveService _save;
        CalculationStore _calculations;
        CalculationChoice _calculation;
        PermanentState _permanent = new PermanentState();
        float _autosaveTimer;

        Func<DateTime> _clock = () => DateTime.UtcNow;
        TimeSpan _clockShift;
        DateTime? _awaySinceUtc;

        /// <summary>
        /// A hora de agora para o jogo (UTC): o relógio do aparelho, ou o dos testes (<see cref="UseClock"/>), mais o
        /// deslocamento do DBG → Offline ("Voltar o relógio 1 h"). O núcleo nunca lê a hora: recebe os segundos.
        /// </summary>
        public DateTime UtcNow => _clock() + _clockShift;

        /// <summary>O último offline (M9): ao abrir, ao voltar do minimizado ou simulado no DBG. <c>null</c> antes do primeiro.</summary>
        public OfflineReport? LastOffline { get; private set; }

        /// <summary>
        /// Um offline foi contado. Com menos de <see cref="OfflineNoticeSeconds"/> as lascas já entraram; a partir dele,
        /// esperam o COLETAR (<see cref="MiningSimulation.PendingOffline"/>, E2).
        /// </summary>
        public event Action<OfflineReport> OfflineApplied;

        /// <summary>A partir de quantos segundos contados a volta mostra o modal (D-195: 60).</summary>
        public double OfflineNoticeSeconds => offlineNoticeSeconds;

        /// <summary>Multiplicador do tempo de jogo (o ×5/×20 do painel de debug).</summary>
        public float TimeScale { get; set; } = 1f;

        bool _forceCritical;

        /// <summary>Atalho do debug: todo golpe e todo toque saem críticos. Vale também depois de reiniciar.</summary>
        public bool ForceCritical
        {
            get => _forceCritical;
            set
            {
                _forceCritical = value;
                if (Simulation != null) Simulation.ForceCritical = value;
            }
        }

        ArrivalStrike? _arrivalStrike;

        /// <summary>
        /// Atalho do debug (D-124): quando sai o primeiro golpe num cristal, no lugar do valor do asset. Vale também depois
        /// de reiniciar; não fica salvo.
        /// </summary>
        public ArrivalStrike ArrivalStrike
        {
            get => Simulation?.ArrivalStrike ?? _arrivalStrike ?? (rules != null ? rules.ToRules().ArrivalStrike : ArrivalStrike.Wait);
            set
            {
                _arrivalStrike = value;
                if (Simulation != null) Simulation.ArrivalStrike = value;
            }
        }

        public MiningSimulation Simulation { get; private set; }
        public ulong Seed { get; private set; }

        /// <summary>O balanço em uso (Balance_GDD_v7 no jogo, D-130).</summary>
        public MiningRulesAsset Rules => rules;

        /// <summary>Os balanços do seletor do DBG → Cálculos; o primeiro é o do GDD.</summary>
        public IReadOnlyList<MiningRulesAsset> Balances => balances;

        /// <summary>O cálculo em uso (o padrão é tudo como o GDD diz).</summary>
        public CalculationChoice Calculation => _calculation;

        /// <summary>Se esta partida grava o progresso (só quando veio pelo <c>GameBootstrap</c>).</summary>
        public bool HasSave => _save != null;

        /// <summary>Uma partida começou (no início da cena ou ao reiniciar). Quem desenha refaz tudo a partir dela.</summary>
        public event Action<MiningSimulation> SimulationStarted;

        /// <summary>
        /// O dia terminou num descanso (Marco 8b), com o resumo (D-154). Sai depois do <see cref="SimulationStarted"/> da
        /// run nova: quem mostra o resumo já encontra a run de amanhã por trás.
        /// </summary>
        public event Action<RestSummary> Rested;

        void Awake()
        {
            Begin(useFixedSeed ? (ulong)fixedSeed : NewSeed(), new GameState());
        }

        void Update()
        {
            if (Simulation == null) return;
            Simulation.Tick(Math.Min(Time.unscaledDeltaTime, MaxFrameSeconds) * (double)TimeScale);

            if (_save == null) return;
            _autosaveTimer += Time.unscaledDeltaTime;
            if (_autosaveTimer < AutosaveSeconds) return;
            _autosaveTimer = 0f;
            SaveNow();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                // D-214: ir para o fundo termina o combo na hora; os bônus dele entram e vão para o save.
                Simulation?.EndCombo();
                SaveNow();
                return;
            }
            // Voltou do minimizado (D-195): o tempo fora é offline, como fechar e abrir. Sem save (cena aberta direto,
            // testes), nada: o offline conta a partir de uma gravação.
            if (_save != null && _awaySinceUtc.HasValue) ReturnFromAway(_awaySinceUtc.Value);
        }

        void OnApplicationQuit()
        {
            Simulation?.EndCombo(); // D-214, como na pausa
            SaveNow();
        }

        void OnDestroy()
        {
            Unsubscribe();
        }

        /// <summary>
        /// Liga o save: carrega o progresso e recomeça a partida com ele (chamado pelo <c>GameBootstrap</c>). Com
        /// <paramref name="calculations"/>, antes aplica o cálculo escolhido no DBG (E3b-3), para o progresso abrir nas
        /// regras em que foi jogado; num build de release a escolha é ignorada.
        /// </summary>
        public void UseSave(SaveService save, CalculationStore calculations = null)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            if (calculations != null && Debug.isDebugBuild)
            {
                _calculations = calculations;
                ApplyCalculation(CalculationChoice.FromData(calculations.Load()));
                if (!_calculation.IsDefault) Debug.Log($"[Cálculos] O jogo abre com o cálculo do DBG (balanço {_calculation.Balance}).");
            }
            var state = save.Load(Economy(), rules.ToPermanents(), out var permanent, out var status, CurrentRules().CaveCount);
            _permanent = permanent;
            Debug.Log($"[Save] {status}: {NumberFormatter.Format(state.Shards)} lascas, caverna {state.Cave}; dia {permanent.Day}, " +
                      $"{NumberFormatter.Format(permanent.RedShards)} lascas vermelhas.");
            Begin(Seed, state);
            // O offline desde a última gravação (save v5); um save até a v4 não tem hora: nada na primeira abertura.
            if (save.LoadedSavedAtUtc.HasValue) ReturnFromAway(save.LoadedSavedAtUtc.Value);
        }

        /// <summary>Troca o relógio do jogo (testes). O deslocamento do DBG continua valendo por cima dele.</summary>
        public void UseClock(Func<DateTime> utcNow) => _clock = utcNow ?? throw new ArgumentNullException(nameof(utcNow));

        /// <summary>
        /// A volta (M9, E1): conta o offline de <paramref name="sinceUtc"/> até agora, credita as lascas do estagiário e grava
        /// na hora, com a hora nova. Relógio voltado: nada entra, sem erro, e a hora nova passa a valer (D-196).
        /// </summary>
        public OfflineReport ReturnFromAway(DateTime sinceUtc) => ApplyAway((UtcNow - sinceUtc).TotalSeconds);

        /// <summary>DBG → Offline: como se o jogo tivesse ficado <paramref name="seconds"/> fechado, sem fechar.</summary>
        public OfflineReport SimulateAway(double seconds) => ApplyAway(seconds);

        /// <summary>
        /// DBG → Offline, "Voltar o relógio": o relógio do jogo volta <paramref name="by"/> e o jogo faz a volta a partir da
        /// última gravação, como ao abrir com o relógio do aparelho atrasado (D-196).
        /// </summary>
        public OfflineReport TurnClockBack(TimeSpan by)
        {
            DateTime since = _awaySinceUtc ?? UtcNow;
            _clockShift -= by;
            return ReturnFromAway(since);
        }

        OfflineReport ApplyAway(double seconds)
        {
            if (Simulation == null) return default;
            var report = Simulation.ApplyOffline(seconds, offlineNoticeSeconds);
            LastOffline = report;
            _awaySinceUtc = UtcNow;
            if (report.Shards > 0 || report.ClockWentBack)
                Debug.Log($"[Offline] {NumberFormatter.Format(report.SecondsAway)} s fora, {NumberFormatter.Format(report.SecondsCounted)} s " +
                          $"contados: {report.Crystals} cristais, {NumberFormatter.FormatShards(report.Shards)} lascas" +
                          (report.ClockWentBack ? " (relógio voltou)" : report.Capped ? " (teto)" : "") + ".");
            SaveNow();
            try { OfflineApplied?.Invoke(report); }
            catch (Exception e) { Debug.LogException(e, this); }
            return report;
        }

        /// <summary>
        /// COLETAR do "Bem-vindo de volta" (M9, E2, D-197): as lascas da volta entram e o jogo grava na hora, para que
        /// fechar logo depois não as traga de novo. Devolve quanto entrou.
        /// </summary>
        public double CollectOffline()
        {
            if (Simulation == null) return 0;
            double shards = Simulation.CollectOffline();
            if (shards > 0) SaveNow();
            return shards;
        }

        /// <summary>Refaz o mapa com a semente dada, <b>mantendo o progresso</b> (o "Reiniciar" do debug, M4).</summary>
        public void StartNew(ulong seed) => Begin(seed, Simulation?.State ?? new GameState());

        /// <summary>
        /// Troca o balanço e recomeça do zero com a mesma semente, <b>sem apagar o save</b>. Usado pelos testes de tela que
        /// precisam de cristais que aguentam vários golpes (v5) e, na E3b-3, pelo DBG → Cálculos.
        /// </summary>
        public void UseRules(MiningRulesAsset asset)
        {
            rules = asset != null ? asset : throw new ArgumentNullException(nameof(asset));
            Begin(Seed, new GameState());
        }

        /// <summary>
        /// Troca o cálculo (o DBG → Cálculos, E3b-3) e grava a escolha no aparelho, se veio pelo <c>GameBootstrap</c>. Outro
        /// balanço recomeça a run do zero (as regras são outras); a taxa e o incremento da Picareta mudam com o progresso
        /// mantido, como uma loja com outra fórmula. Um balanço fora da lista volta ao do GDD.
        /// </summary>
        public void UseCalculation(CalculationChoice choice)
        {
            bool restart = choice.Balance != _calculation.Balance;
            ApplyCalculation(choice);
            try
            {
                _calculations?.Save(_calculation.ToData());
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
            if (restart) RestartRun();
            else Begin(Seed, Simulation?.State ?? new GameState());
        }

        /// <summary>
        /// "Reiniciar run" do DBG (E3b-3): volta ao nível 1, com as lascas e as melhorias base zeradas, numa fila nova. O
        /// save não é apagado: a run nova o substitui na próxima gravação. As melhorias permanentes ficam, e a Picareta já
        /// começa com a Alma dos Anões, como depois de um descanso (mas o dia não avança nem entram lascas vermelhas).
        /// </summary>
        public void RestartRun() => Begin(RunSeed(), NewRun());

        /// <summary>
        /// Descansar (Marco 8b, D-137): leva as lascas vermelhas pendentes para o banco, avança o dia e começa a run nova
        /// (nível 1, sem lascas nem melhorias base, a Picareta com a Alma dos Anões), numa fila nova. As permanentes ficam.
        /// <paramref name="force"/> = o atalho do debug, em qualquer nível. Devolve o resumo do dia, ou <c>null</c> se não dá
        /// para descansar.
        /// </summary>
        public RestSummary? Rest(bool force = false)
        {
            var summary = Simulation?.Rest(force);
            if (summary == null) return null;

            Begin(RunSeed(), NewRun());
            SaveNow();
            try { Rested?.Invoke(summary.Value); }
            catch (Exception e) { Debug.LogException(e, this); }
            return summary;
        }

        GameState NewRun() => Simulation != null ? Simulation.Permanents.NewRun(Economy()) : new GameState();

        ulong RunSeed() => useFixedSeed ? (ulong)fixedSeed : NewSeed();

        void ApplyCalculation(CalculationChoice choice)
        {
            if (choice.Balance < 0 || choice.Balance >= balances.Length || balances[choice.Balance] == null)
                choice = choice.WithBalance(0);
            _calculation = choice;
            if (choice.Balance < balances.Length && balances[choice.Balance] != null) rules = balances[choice.Balance];
        }

        MiningRules CurrentRules() => rules.ToRules(_calculation.PickaxeCaveGrowth, _calculation.PickaxeByCave);
        EconomyRules Economy() => rules.ToEconomy(_calculation.PickaxeStep, _calculation.PickaxeCaveGrowth);

        /// <summary>
        /// Apaga o save (e o backup) e começa do zero no nível 1, as melhorias permanentes, o dia e as lascas vermelhas
        /// também; uma volta do offline ainda não coletada se perde junto. É o "Apagar save" dos AJUSTES (M9, E4) e do DBG.
        /// Os AJUSTES e a escolha do DBG → Cálculos ficam: estão em arquivos à parte (D-200). Grava a partida nova na hora,
        /// para que ela, e não um save velho, seja o ponto de partida do offline.
        /// </summary>
        public void ResetProgress()
        {
            _save?.Delete();
            _permanent = new PermanentState();
            Begin(RunSeed(), new GameState());
            SaveNow();
            Debug.Log("[Save] Save apagado: jogo novo no nível 1.");
        }

        /// <summary>Grava o progresso agora (sem save ligado, não faz nada). Um erro de disco vai para o log, não derruba o jogo.</summary>
        public void SaveNow()
        {
            if (_save == null || Simulation == null) return;
            try
            {
                DateTime now = UtcNow;
                _save.Save(Simulation.State, _permanent, now);
                _awaySinceUtc = now;
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }

        void Begin(ulong seed, GameState state)
        {
            if (rules == null)
            {
                Debug.LogError("[MiningRunner] Sem asset de valores (Balance_Prototipo).", this);
                enabled = false;
                return;
            }

            Unsubscribe();

            Seed = seed;
            Simulation = new MiningSimulation(
                CurrentRules(), new SeededRandom(CritSeed(seed)), state, Economy(), rules.ToStrike(),
                new Permanents(rules.ToPermanents(), _permanent))
            {
                ForceCritical = _forceCritical,
                // M9b E3 (D-217): o acúmulo do DBG → Cálculos sobrevive a cada simulação nova (compra de Picareta trocada,
                // Reiniciar run, descanso, reabrir).
                ComboStacking = _calculation.ComboStacking,
            };
            if (_arrivalStrike.HasValue) Simulation.ArrivalStrike = _arrivalStrike.Value;
            Simulation.UpgradePurchased += OnUpgradePurchased;
            Simulation.CaveChanged += OnCaveChanged;
            Simulation.HeartReached += SaveNow;
            Simulation.PermanentsChanged += SaveNow;
            NotifyStarted();
        }

        void Unsubscribe()
        {
            if (Simulation == null) return;
            Simulation.UpgradePurchased -= OnUpgradePurchased;
            Simulation.CaveChanged -= OnCaveChanged;
            Simulation.HeartReached -= SaveNow;
            Simulation.PermanentsChanged -= SaveNow;
        }

        void OnUpgradePurchased(UpgradeId id, int level) => SaveNow();
        void OnCaveChanged(int cave) => SaveNow();

        /// <summary>
        /// Avisa cada view separadamente: uma view com erro não pode impedir as outras de se conectarem. Sem isso, um erro
        /// no desenho dos cristais deixava o anão e o HUD desconectados (anão parado na tela, achado da E5).
        /// </summary>
        void NotifyStarted()
        {
            var handlers = SimulationStarted;
            if (handlers == null) return;

            foreach (Action<MiningSimulation> handler in handlers.GetInvocationList())
            {
                try { handler(Simulation); }
                catch (Exception e) { Debug.LogException(e, this); }
            }
        }

        public static ulong NewSeed() => (ulong)DateTime.UtcNow.Ticks;

        /// <summary>Semente do sorteio de crítico, derivada da partida (mesma semente = mesmos críticos).</summary>
        static ulong CritSeed(ulong seed) => unchecked(seed * 0x9E3779B97F4A7C15UL + 1);
    }
}
