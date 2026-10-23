using System;
using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// Enquadramento da câmera da descida (M6, D-50; desde o M1b, D-69): a câmera fica à frente do anão, acima dele,
    /// olhando para baixo de modo que a fila de cristais apareça em coluna, sem esconder o anão. Valores de apresentação
    /// (não são regra), ajustáveis no DBG.
    ///
    /// Coordenadas: a câmera está em (0, altura, 0) olhando para +z; o anão corre parado em (x, 0, distância), de frente
    /// para ela. O campo de visão vale para uma tela 9:16 (a do simulador); numa tela mais alta a largura vista é a mesma
    /// e a sobra vai para cima e para baixo, como no jogo (<see cref="CameraWidthFit"/>).
    /// </summary>
    [Serializable]
    public struct DescentFraming
    {
        /// <summary>Proporção (largura/altura) em que o campo de visão foi validado.</summary>
        public const float ReferenceAspect = 9f / 16f;

        /// <summary>Altura do anão nos sprites, do pé ao alto do capacete (para medir onde ele fica na tela).</summary>
        public const float DwarfHeight = 1.32f;

        [Tooltip("Altura da câmera acima do chão, em metros.")]
        public float height;
        [Tooltip("Inclinação da câmera para baixo, em graus.")]
        public float tilt;
        [Tooltip("Distância da câmera até o anão, em metros.")]
        public float distance;
        [Tooltip("Campo de visão vertical numa tela 9:16, em graus.")]
        public float fieldOfView;
        [Tooltip("Largura do corredor, em metros.")]
        public float corridorWidth;

        /// <summary>
        /// A câmera F1, escolhida pelo Bruno sobre prévias do jogo com arte (D-69, 2026-10-23). Supera a D-50 (2,4 m, 15°,
        /// 4,8 m), em que os cristais da fila escondiam o anão.
        /// </summary>
        public static DescentFraming Validated => new DescentFraming
        {
            height = 3.6f,
            tilt = 30f,
            distance = 5f,
            fieldOfView = 40f,
            corridorWidth = 3f,
        };

        /// <summary>Os mesmos limites dos controles do simulador.</summary>
        public DescentFraming Clamped() => new DescentFraming
        {
            height = Mathf.Clamp(height, 0.8f, 9f),
            tilt = Mathf.Clamp(tilt, 0f, 65f),
            distance = Mathf.Clamp(distance, 3f, 12f),
            fieldOfView = Mathf.Clamp(fieldOfView, 30f, 85f),
            corridorWidth = Mathf.Clamp(corridorWidth, 2f, 5f),
        };

        /// <summary>Campo de visão vertical para a proporção dada, mantendo a largura vista numa tela 9:16.</summary>
        public float VerticalFieldOfView(float aspect)
        {
            float halfTan = Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad) * ReferenceAspect / Mathf.Max(0.01f, aspect);
            return 2f * Mathf.Atan(halfTan) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Onde fica na tela um ponto do plano central (altura <paramref name="y"/>, distância <paramref name="z"/> da
        /// câmera): 0 = topo, 1 = base.
        /// </summary>
        public float FromTop(float y, float z, float aspect)
        {
            float t = tilt * Mathf.Deg2Rad;
            float dy = y - height;
            float depth = -dy * Mathf.Sin(t) + z * Mathf.Cos(t);
            float up = dy * Mathf.Cos(t) + z * Mathf.Sin(t);
            float halfTan = Mathf.Tan(VerticalFieldOfView(aspect) * 0.5f * Mathf.Deg2Rad);
            return 0.5f - up / depth / halfTan * 0.5f;
        }

        /// <summary>Metros de chão visíveis à frente dos pés do anão, até a borda de baixo da tela.</summary>
        public float GroundAhead(float aspect)
        {
            float bottom = (tilt + VerticalFieldOfView(aspect) * 0.5f) * Mathf.Deg2Rad;
            if (bottom <= 0f) return 0f;
            return Mathf.Max(0f, distance - height / Mathf.Tan(bottom));
        }
    }
}
