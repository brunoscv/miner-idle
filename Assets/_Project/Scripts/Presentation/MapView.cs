using MinersIdle.Core;
using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// Chão em graybox: desde o M6 (E2), uma faixa vertical por onde a fila desce, debaixo do anão. Some na E3, quando a
    /// cena vira o corredor 3D (D-61).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class MapView : MiningViewBase
    {
        [Tooltip("Largura e altura da faixa, em unidades de mundo.")]
        [SerializeField] Vector2 size = new Vector2(4f, 34f);

        protected override void Rebuild(MiningSimulation simulation)
        {
            transform.position = Vector3.zero;
            // O sprite de graybox tem 1 unidade (16 px a PPU 16): a escala é o tamanho em unidades.
            transform.localScale = new Vector3(size.x, size.y, 1f);
        }
    }
}
