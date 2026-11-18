using MinersIdle.Core;
using MinersIdle.Presentation;
using TMPro;
using UnityEngine;

namespace MinersIdle.UI
{
    /// <summary>
    /// Os números da partida: LASCAS e VERMELHAS no HUD do alto, o andar na etiqueta da barra do andar e DANO e VELOCIDADE
    /// no alto da LOJA (M9d E3, D-278: o HUD do v8 só tem as lascas). Os rótulos ficam na cena (dados); aqui só os valores,
    /// todos pelo <see cref="NumberFormatter"/>. DANO e VELOCIDADE são os atributos atuais (com melhorias, M3). Atualiza
    /// por evento, nunca por quadro. M5: LASCAS pulsa quando a lasca voadora chega (o HUD tremia nos marcos do combo, que saiu no M9g).
    /// M9b (E2, D-216): o DANO já vem com os bônus de dano e fica âmbar enquanto houver algum.
    /// </summary>
    public sealed class HudView : MiningViewBase
    {
        [SerializeField] TMP_Text shardsValue;
        [Tooltip("As lascas vermelhas, o banco das permanentes (M9d E3).")]
        [SerializeField] TMP_Text redShardsValue;
        [SerializeField] TMP_Text damageValue;
        [SerializeField] TMP_Text caveValue;
        [SerializeField] TMP_Text speedValue;
        [Tooltip("A lasca que voa do cristal até LASCAS (M5).")]
        [SerializeField] ShardFlight shardFlight;
        [Tooltip("A cor do DANO enquanto um bônus de dano vale (M9b, D-216).")]
        [SerializeField] Color boostedDamageColor = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        [Tooltip("A cor da VELOCIDADE enquanto o golpe está acelerado: boost do AD ou carta Rápido (M9b, D-231).")]
        [SerializeField] Color boostedSpeedColor = new Color32(0xF2, 0xA9, 0x00, 0xFF);

        const float PulseSeconds = 0.2f;

        Color _damageColor;
        Color _speedColor;
        float _pulse;

        /// <summary>Se LASCAS está pulsando agora (para testes).</summary>
        public bool IsShardsPulsing => _pulse > 0f;

        /// <summary>O texto de LASCAS na tela (para testes).</summary>
        public string ShardsText => shardsValue.text;
        /// <summary>O texto de VERMELHAS na tela (para testes).</summary>
        public string RedShardsText => redShardsValue != null ? redShardsValue.text : null;
        /// <summary>Os textos de DANO e VELOCIDADE na tela (para testes).</summary>
        public string DamageText => damageValue.text;
        public string SpeedText => speedValue.text;
        /// <summary>O texto de CAVERNA na tela (para testes).</summary>
        public string CaveText => caveValue.text;
        /// <summary>O DANO está âmbar (um bônus de dano vale agora; para testes).</summary>
        public bool IsDamageBoosted => damageValue.color == boostedDamageColor;
        /// <summary>A VELOCIDADE está âmbar (boost do AD ou carta Rápido; para testes).</summary>
        public bool IsSpeedBoosted => speedValue.color == boostedSpeedColor && _speedColor != boostedSpeedColor;

        /// <summary>Um bônus de dano vale agora: um +% de dano ou a carta "Dano ×2" do raro (D-216).</summary>
        bool DamageBoosted => Simulation != null && (Simulation.Bonuses.DamagePercent > 0 || Simulation.Bonuses.DamageMultiplier > 1);

        void Awake()
        {
            _damageColor = damageValue.color;
            _speedColor = speedValue.color;
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
            simulation.CaveChanged += OnCaveChanged;
            simulation.PermanentsChanged += OnPermanentsChanged;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.ShardsChanged -= OnShardsChanged;
            simulation.StatsChanged -= OnStatsChanged;
            simulation.CaveChanged -= OnCaveChanged;
            simulation.PermanentsChanged -= OnPermanentsChanged;
        }

        void Update()
        {
            if (_pulse > 0f)
            {
                _pulse = Mathf.Max(0f, _pulse - Time.unscaledDeltaTime / PulseSeconds);
                shardsValue.transform.localScale = Vector3.one * (1f + 0.3f * _pulse);
            }
        }

        void OnShardArrived() => _pulse = 1f;

        protected override void Rebuild(MiningSimulation simulation)
        {
            OnShardsChanged(simulation.State.Shards);
            OnStatsChanged(simulation.Stats);
            OnCaveChanged(simulation.State.Cave);
            OnPermanentsChanged();
        }

        void OnCaveChanged(int cave) => caveValue.text = NumberFormatter.Format(cave);

        void OnPermanentsChanged()
        {
            if (redShardsValue != null && Simulation != null)
                redShardsValue.text = NumberFormatter.FormatShards(Simulation.Permanents.State.RedShards);
        }

        void OnShardsChanged(double shards) => shardsValue.text = NumberFormatter.FormatShards(shards);

        void OnStatsChanged(MinerStats stats)
        {
            damageValue.text = NumberFormatter.Format(stats.Damage);
            damageValue.color = DamageBoosted ? boostedDamageColor : _damageColor;
            speedValue.text = NumberFormatter.FormatSeconds(stats.HitInterval);
            speedValue.color = Simulation != null && Simulation.Bonuses.IntervalDivisor > 1 ? boostedSpeedColor : _speedColor;
        }
    }
}
