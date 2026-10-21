using MinersIdle.Core;
using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>Chão do mapa em graybox: um retângulo do tamanho da área onde os cristais nascem (com uma folga).</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class MapView : MiningViewBase
    {
        /// <summary>Folga em volta da área de nascimento, para o cristal na borda não ficar meio fora do chão.</summary>
        [SerializeField] float padding = 1f;

        protected override void Rebuild(MiningSimulation simulation)
        {
            var rules = simulation.Rules;
            var center = (rules.MapMin + rules.MapMax) * 0.5;
            var size = rules.MapMax - rules.MapMin;

            transform.position = ToWorld(center);
            // O sprite de graybox tem 1 unidade (16 px a PPU 16): a escala é o tamanho em unidades.
            transform.localScale = new Vector3((float)size.X + 2 * padding, (float)size.Y + 2 * padding, 1f);
        }
    }
}
