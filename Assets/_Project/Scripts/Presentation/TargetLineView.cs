using MinersIdle.Core;
using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// "Mostrar ordem" do debug: uma linha do anão até o alvo atual (o primeiro da fila). É um sprite quadrado esticado
    /// e girado (sem material extra). Desligada por padrão.
    /// </summary>
    public sealed class TargetLineView : MiningViewBase
    {
        const float Thickness = 0.08f;

        [SerializeField] SpriteRenderer line;

        public bool Visible { get; set; }

        /// <summary>Se a linha está desenhada agora (para testes).</summary>
        public bool IsDrawn => line.enabled;

        protected override void Rebuild(MiningSimulation simulation) => line.enabled = false;

        void LateUpdate()
        {
            var target = Simulation?.Miner.Target;
            line.enabled = Visible && target != null;
            if (!line.enabled) return;

            double miner = Simulation.Miner.Distance;
            var from = TrackToWorld(miner, miner);
            var delta = TrackToWorld(target.Distance, miner) - from;

            transform.position = from + delta * 0.5f;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            transform.localScale = new Vector3(delta.magnitude, Thickness, 1f);
        }
    }
}
