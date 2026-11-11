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
    /// M9b (E2, D-216): o DANO já vem com os bônus de dano e fica âmbar enquanto houver algum.
    /// </summary>
    public sealed class HudView : MiningViewBase
    {
        [SerializeField] TMP_Text shardsValue;
        [SerializeField] TMP_Text damageValue;
        [SerializeField] TMP_Text caveValue;
        [SerializeField] TMP_Text speedValue;
        [Tooltip("A lasca que voa do cristal até LASCAS (M5).")]
        [SerializeField] ShardFlight shardFlight;
        [Tooltip("A cor do DANO enquanto um bônus de dano vale (M9b, D-216).")]
        [SerializeField] Color boostedDamageColor = new Color32(0xF2, 0xA9, 0x00, 0xFF);

        const float PulseSeconds = 0.2f;
        const float ShakeSeconds = 0.2f;

        RectTransform _rect;
        Vector2 _basePosition;
        Color _damageColor;
        float _pulse;
        float _shake;

        /// <summary>Se LASCAS está pulsando agora (para testes).</summary>
        public bool IsShardsPulsing => _pulse > 0f;

        /// <summary>O texto de LASCAS na tela (para testes).</summary>
        public string ShardsText => shardsValue.text;
        /// <summary>Os textos de DANO e VELOCIDADE na tela (para testes).</summary>
        public string DamageText => damageValue.text;
        public string SpeedText => speedValue.text;
        /// <summary>O texto de CAVERNA na tela (para testes).</summary>
        public string CaveText => caveValue.text;
        /// <summary>O DANO está âmbar (um bônus de dano vale agora; para testes).</summary>
        public bool IsDamageBoosted => damageValue.color == boostedDamageColor;

        /// <summary>Um bônus de dano vale agora: os +% do combo ou a carta "Dano ×2" do raro (D-216).</summary>
        bool DamageBoosted => Simulation != null && (Simulation.Bonuses.DamagePercent > 0 || Simulation.Bonuses.DamageMultiplier > 1);

        void Awake()
        {
            _rect = (RectTransform)transform;
            _basePosition = _rect.anchoredPosition;
            _damageColor = damageValue.color;
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
            simulation.CaveChanged += OnCaveChanged;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.ShardsChanged -= OnShardsChanged;
            simulation.StatsChanged -= OnStatsChanged;
            simulation.ComboMilestone -= OnComboMilestone;
            simulation.CaveChanged -= OnCaveChanged;
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
            OnCaveChanged(simulation.State.Cave);
        }

        void OnCaveChanged(int cave) => caveValue.text = NumberFormatter.Format(cave);

        void OnShardsChanged(double shards) => shardsValue.text = NumberFormatter.FormatShards(shards);

        void OnStatsChanged(MinerStats stats)
        {
            damageValue.text = NumberFormatter.Format(stats.Damage);
            damageValue.color = DamageBoosted ? boostedDamageColor : _damageColor;
            speedValue.text = NumberFormatter.FormatSeconds(stats.HitInterval);
        }
    }
}
