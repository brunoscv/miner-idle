using System;
using MinersIdle.Core;
using UnityEngine;

namespace MinersIdle.Game
{
    /// <summary>
    /// Dono da partida na cena Game (M2): monta a <see cref="MiningSimulation"/> com os valores do asset e a avança a cada
    /// quadro. As views só leem a simulação e escutam os eventos dela. Quando o save chegar (M4), o estado inicial passa a
    /// vir do <c>GameBootstrap</c>.
    /// </summary>
    public sealed class MiningRunner : MonoBehaviour
    {
        /// <summary>Teto do tempo de um quadro: depois de uma pausa do app não se simulam minutos de uma vez.</summary>
        public const float MaxFrameSeconds = 0.25f;

        [SerializeField] MiningRulesAsset rules;

        [Tooltip("Ligado: toda partida usa a semente abaixo (para reproduzir um caso). Desligado: semente nova a cada partida.")]
        [SerializeField] bool useFixedSeed;
        [SerializeField] int fixedSeed = 1;

        /// <summary>Multiplicador do tempo de jogo (o ×5/×20 do painel de debug).</summary>
        public float TimeScale { get; set; } = 1f;

        public MiningSimulation Simulation { get; private set; }
        public ulong Seed { get; private set; }

        /// <summary>Uma partida começou (no início da cena ou ao reiniciar). Quem desenha refaz tudo a partir dela.</summary>
        public event Action<MiningSimulation> SimulationStarted;

        void Awake()
        {
            StartNew(useFixedSeed ? (ulong)fixedSeed : NewSeed());
        }

        void Update()
        {
            if (Simulation == null) return;
            Simulation.Tick(Math.Min(Time.unscaledDeltaTime, MaxFrameSeconds) * (double)TimeScale);
        }

        /// <summary>Começa uma partida nova com a semente dada (a mesma semente repete a partida).</summary>
        public void StartNew(ulong seed)
        {
            if (rules == null)
            {
                Debug.LogError("[MiningRunner] Sem asset de valores (Balance_Prototipo).", this);
                enabled = false;
                return;
            }

            Seed = seed;
            Simulation = new MiningSimulation(rules.ToRules(), new SeededRandom(seed));
            SimulationStarted?.Invoke(Simulation);
        }

        public static ulong NewSeed() => (ulong)DateTime.UtcNow.Ticks;
    }
}
