using MinersIdle.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// Mostra a imagem de baixa resolução da descida (M6, E1) na tela inteira, ampliada sem suavizar (a textura usa filtro
    /// "point"). A textura é trocada quando o <see cref="DescentStage"/> a recria (tela ou resolução nova).
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class DescentScreen : MonoBehaviour
    {
        [SerializeField] DescentStage stage;

        RawImage _image;

        void Awake() => _image = GetComponent<RawImage>();

        void LateUpdate()
        {
            var output = stage.Output;
            if (_image.texture != output) _image.texture = output;
        }
    }
}
