using MinersIdle.Core;
using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// "Mostrar ordem" do debug: uma linha do anão até o alvo atual, para conferir a regra D-27 de olho. É um sprite
    /// quadrado esticado e girado (sem material extra). Desligada por padrão.
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

            var from = Simulation.Miner.Position;
            var delta = target.Position - from;
            var middle = from + delta * 0.5;

            transform.position = new Vector3((float)middle.X, (float)middle.Y, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2((float)delta.Y, (float)delta.X) * Mathf.Rad2Deg);
            transform.localScale = new Vector3((float)delta.Length, Thickness, 1f);
        }
    }
}
