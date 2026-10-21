using MinersIdle.Core;
using MinersIdle.Presentation;
using TMPro;
using UnityEngine;

namespace MinersIdle.UI
{
    /// <summary>
    /// HUD superior (S02): LASCAS, DANO, CAVERNA e VELOCIDADE. Os rótulos ficam na cena (dados); aqui só os valores, todos
    /// pelo <see cref="NumberFormatter"/>. DANO e VELOCIDADE são os atributos atuais (com melhorias, M3). Atualiza por
    /// evento, nunca por quadro.
    /// </summary>
    public sealed class HudView : MiningViewBase
    {
        /// <summary>Só existe a caverna 1 até o M7 (passagem de caverna).</summary>
        const int CurrentCave = 1;

        [SerializeField] TMP_Text shardsValue;
        [SerializeField] TMP_Text damageValue;
        [SerializeField] TMP_Text caveValue;
        [SerializeField] TMP_Text speedValue;

        /// <summary>O texto de LASCAS na tela (para testes).</summary>
        public string ShardsText => shardsValue.text;
        /// <summary>Os textos de DANO e VELOCIDADE na tela (para testes).</summary>
        public string DamageText => damageValue.text;
        public string SpeedText => speedValue.text;

        protected override void Subscribe(MiningSimulation simulation)
        {
            simulation.ShardsChanged += OnShardsChanged;
            simulation.StatsChanged += OnStatsChanged;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.ShardsChanged -= OnShardsChanged;
            simulation.StatsChanged -= OnStatsChanged;
        }

        protected override void Rebuild(MiningSimulation simulation)
        {
            OnShardsChanged(simulation.State.Shards);
            OnStatsChanged(simulation.Stats);
            caveValue.text = NumberFormatter.Format(CurrentCave);
        }

        void OnShardsChanged(double shards) => shardsValue.text = NumberFormatter.Format(shards);

        void OnStatsChanged(MinerStats stats)
        {
            damageValue.text = NumberFormatter.Format(stats.Damage);
            speedValue.text = NumberFormatter.FormatSeconds(stats.HitInterval);
        }
    }
}
