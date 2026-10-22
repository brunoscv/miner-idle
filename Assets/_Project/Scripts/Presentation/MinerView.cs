using MinersIdle.Core;
using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// O anão em graybox. A posição vem do Núcleo a cada quadro (a view não anda sozinha); o "soco" de escala no golpe
    /// só ilustra o evento <see cref="MiningSimulation.HitLanded"/>.
    /// </summary>
    public sealed class MinerView : MiningViewBase
    {
        const float PunchScale = 0.2f;
        const float PunchSeconds = 0.15f;

        [SerializeField] SpriteRenderer body;

        Vector3 _baseScale;
        float _punch;

        void Awake() => _baseScale = transform.localScale;

        protected override void Subscribe(MiningSimulation simulation) => simulation.HitLanded += OnHit;
        protected override void Unsubscribe(MiningSimulation simulation) => simulation.HitLanded -= OnHit;

        protected override void Rebuild(MiningSimulation simulation)
        {
            _punch = 0f;
            Place();
        }

        void LateUpdate()
        {
            if (Simulation == null) return;

            Place();

            if (_punch > 0f) _punch = Mathf.Max(0f, _punch - Time.unscaledDeltaTime / PunchSeconds);
            transform.localScale = _baseScale * (1f + PunchScale * _punch);
        }

        /// <summary>O anão corre parado: a fila é que se move em relação a ele (M6).</summary>
        void Place()
        {
            transform.position = TrackToWorld(0, 0);
            body.sortingOrder = SortingOrderFor(MinerY);
        }

        void OnHit(MiningHit hit)
        {
            if (!hit.IsTap) _punch = 1f; // o "soco" é do golpe do anão; o toque ganha a própria reação no M5 (E3)
        }
    }
}
