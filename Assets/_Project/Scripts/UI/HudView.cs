using MinersIdle.Core;
using MinersIdle.Presentation;
using TMPro;
using UnityEngine;

namespace MinersIdle.UI
{
    /// <summary>
    /// HUD superior (S02): LASCAS, DANO, CAVERNA e VELOCIDADE. Os rótulos ficam na cena (dados); aqui só os valores, todos
    /// pelo <see cref="NumberFormatter"/>. DANO e VELOCIDADE são os atributos atuais (com melhorias, M3). Atualiza por
    /// evento, nunca por quadro. M5: LASCAS pulsa quando a lasca voadora chega; o HUD treme nos marcos do combo.
    /// </summary>
    public sealed class HudView : MiningViewBase
    {
        /// <summary>Só existe a caverna 1 até o M7 (passagem de caverna).</summary>
        const int CurrentCave = 1;

        [SerializeField] TMP_Text shardsValue;
        [SerializeField] TMP_Text damageValue;
        [SerializeField] TMP_Text caveValue;
        [SerializeField] TMP_Text speedValue;
        [Tooltip("A lasca que voa do cristal até LASCAS (M5).")]
        [SerializeField] ShardFlight shardFlight;

        const float PulseSeconds = 0.2f;
        const float ShakeSeconds = 0.2f;

        RectTransform _rect;
        Vector2 _basePosition;
        float _pulse;
        float _shake;

        /// <summary>Se LASCAS está pulsando agora (para testes).</summary>
        public bool IsShardsPulsing => _pulse > 0f;

        /// <summary>O texto de LASCAS na tela (para testes).</summary>
        public string ShardsText => shardsValue.text;
        /// <summary>Os textos de DANO e VELOCIDADE na tela (para testes).</summary>
        public string DamageText => damageValue.text;
        public string SpeedText => speedValue.text;

        void Awake()
        {
            _rect = (RectTransform)transform;
            _basePosition = _rect.anchoredPosition;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (shardFlight != null) shardFlight.Arrived += OnShardArrived;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (shardFlight != null) shardFlight.Arrived -= OnShardArrived;
        }

        protected override void Subscribe(MiningSimulation simulation)
        {
            simulation.ShardsChanged += OnShardsChanged;
            simulation.StatsChanged += OnStatsChanged;
            simulation.ComboMilestone += OnComboMilestone;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.ShardsChanged -= OnShardsChanged;
            simulation.StatsChanged -= OnStatsChanged;
            simulation.ComboMilestone -= OnComboMilestone;
        }

        void Update()
        {
            if (_pulse > 0f)
            {
                _pulse = Mathf.Max(0f, _pulse - Time.unscaledDeltaTime / PulseSeconds);
                shardsValue.transform.localScale = Vector3.one * (1f + 0.3f * _pulse);
            }

            if (_shake > 0f)
            {
                _shake = Mathf.Max(0f, _shake - Time.unscaledDeltaTime / ShakeSeconds);
                _rect.anchoredPosition = _basePosition + Random.insideUnitCircle * (3f * _shake);
            }
        }

        void OnShardArrived() => _pulse = 1f;
        void OnComboMilestone(int combo) => _shake = 1f;

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
