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
            Place(simulation.Miner.Position);
        }

        void LateUpdate()
        {
            if (Simulation == null) return;

            Place(Simulation.Miner.Position);

            if (_punch > 0f) _punch = Mathf.Max(0f, _punch - Time.unscaledDeltaTime / PunchSeconds);
            transform.localScale = _baseScale * (1f + PunchScale * _punch);
        }

        void Place(Vec2 position)
        {
            transform.position = ToWorld(position);
            body.sortingOrder = SortingOrderFor(position);
        }

        void OnHit(Crystal crystal, double damage) => _punch = 1f;
    }
}
