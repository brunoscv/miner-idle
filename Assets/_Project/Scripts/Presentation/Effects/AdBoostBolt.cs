using MinersIdle.Core;
using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// O raio pequeno que pulsa acima do anão enquanto o boost do AD vale (M9b E5; GDD v7 §8, D-231). Só ilustra: quem
    /// diz se há boost é a simulação (<see cref="MiningSimulation.HasAdBoost"/>).
    /// </summary>
    public sealed class AdBoostBolt : MiningViewBase
    {
        [SerializeField] DescentStage stage;
        [SerializeField] SpriteRenderer bolt;
        [Tooltip("Quanto acima do anão, em unidades da tela.")]
        [SerializeField] float heightAbove = 1.4f;
        [Tooltip("A largura do raio, em unidades da tela.")]
        [SerializeField, Min(0.05f)] float size = 0.8f;
        [Tooltip("Pulsos por segundo.")]
        [SerializeField, Min(0f)] float pulseSpeed = 1.5f;
        [Tooltip("Quanto o raio cresce e apaga no pulso.")]
        [SerializeField, Range(0f, 0.5f)] float pulse = 0.15f;

        float _baseScale = 1f;

        public bool IsShown => bolt.gameObject.activeSelf;

        void Awake()
        {
            if (bolt.sprite != null && bolt.sprite.bounds.size.x > 0f) _baseScale = size / bolt.sprite.bounds.size.x;
            bolt.gameObject.SetActive(false);
        }

        protected override void Subscribe(MiningSimulation simulation) => simulation.BonusesChanged += OnBonusesChanged;
        protected override void Unsubscribe(MiningSimulation simulation) => simulation.BonusesChanged -= OnBonusesChanged;
        protected override void Rebuild(MiningSimulation simulation) => OnBonusesChanged();

        void OnBonusesChanged()
        {
            bool shown = Simulation != null && Simulation.HasAdBoost;
            if (bolt.gameObject.activeSelf != shown) bolt.gameObject.SetActive(shown);
        }

        void LateUpdate()
        {
            if (!bolt.gameObject.activeSelf || stage == null) return;
            var at = stage.MinerOnScreen + Vector3.up * heightAbove;
            bolt.transform.position = new Vector3(at.x, at.y, 0f);
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed * Mathf.PI * 2f);
            float scale = _baseScale * (1f + pulse * wave);
            bolt.transform.localScale = new Vector3(scale, scale, 1f);
            var color = bolt.color;
            color.a = 1f - pulse * (1f - wave);
            bolt.color = color;
        }
    }
}
