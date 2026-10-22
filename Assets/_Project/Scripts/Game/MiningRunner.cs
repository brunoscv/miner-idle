using System;
using MinersIdle.Core;
using MinersIdle.Persistence;
using UnityEngine;

namespace MinersIdle.Game
{
    /// <summary>
    /// Dono da partida na cena Game (M2): monta a <see cref="MiningSimulation"/> com os valores e as melhorias do asset
    /// (M3) e a avança a cada quadro. As views só leem a simulação e escutam os eventos dela.
    ///
    /// Save (M4): o <c>GameBootstrap</c> entrega o <see cref="SaveService"/> (<see cref="UseSave"/>); daí em diante o
    /// progresso é carregado e salvo depois de cada compra, a cada 30 s, ao minimizar e ao sair. Aberta direto (Play na
    /// cena Game, testes PlayMode), a cena roda sem save: os testes nunca mexem no progresso de quem joga.
    /// </summary>
    public sealed class MiningRunner : MonoBehaviour
    {
        /// <summary>Teto do tempo de um quadro: depois de uma pausa do app não se simulam minutos de uma vez.</summary>
        public const float MaxFrameSeconds = 0.25f;

        /// <summary>Segundos reais entre salvamentos automáticos (reais: o ×20 do debug não multiplica as gravações).</summary>
        public const float AutosaveSeconds = 30f;

        [SerializeField] MiningRulesAsset rules;

        [Tooltip("Ligado: toda partida usa a semente abaixo (para reproduzir um caso). Desligado: semente nova a cada partida.")]
        [SerializeField] bool useFixedSeed;
        [SerializeField] int fixedSeed = 1;

        SaveService _save;
        float _autosaveTimer;

        /// <summary>Multiplicador do tempo de jogo (o ×5/×20 do painel de debug).</summary>
        public float TimeScale { get; set; } = 1f;

        public MiningSimulation Simulation { get; private set; }
        public ulong Seed { get; private set; }

        /// <summary>Se esta partida grava o progresso (só quando veio pelo <c>GameBootstrap</c>).</summary>
        public bool HasSave => _save != null;

        /// <summary>Uma partida começou (no início da cena ou ao reiniciar). Quem desenha refaz tudo a partir dela.</summary>
        public event Action<MiningSimulation> SimulationStarted;

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
            if (paused) SaveNow();
        }

        void OnApplicationQuit() => SaveNow();

        void OnDestroy()
        {
            if (Simulation != null) Simulation.UpgradePurchased -= OnUpgradePurchased;
        }

        /// <summary>Liga o save: carrega o progresso e recomeça a partida com ele (chamado pelo <c>GameBootstrap</c>).</summary>
        public void UseSave(SaveService save)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            var state = save.Load(rules.ToEconomy(), out var status);
            Debug.Log($"[Save] {status}: {NumberFormatter.Format(state.Shards)} lascas.");
            Begin(Seed, state);
        }

        /// <summary>Refaz o mapa com a semente dada, <b>mantendo o progresso</b> (o "Reiniciar" do debug, M4).</summary>
        public void StartNew(ulong seed) => Begin(seed, Simulation?.State ?? new GameState());

        /// <summary>Apaga o save e começa do zero (o "Apagar save" do debug).</summary>
        public void ResetProgress()
        {
            _save?.Delete();
            Begin(useFixedSeed ? (ulong)fixedSeed : NewSeed(), new GameState());
        }

        /// <summary>Grava o progresso agora (sem save ligado, não faz nada). Um erro de disco vai para o log, não derruba o jogo.</summary>
        public void SaveNow()
        {
            if (_save == null || Simulation == null) return;
            try
            {
                _save.Save(Simulation.State);
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

            if (Simulation != null) Simulation.UpgradePurchased -= OnUpgradePurchased;

            Seed = seed;
            Simulation = new MiningSimulation(
                rules.ToRules(), new SeededRandom(seed), state, rules.ToEconomy(),
                rules.ToStrike(), new SeededRandom(CritSeed(seed)));
            Simulation.UpgradePurchased += OnUpgradePurchased;
            NotifyStarted();
        }

        void OnUpgradePurchased(UpgradeId id, int level) => SaveNow();

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
