using System;
using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// Enquadramento da câmera da descida (M6, D-50; M1b, D-69; desde o M9h, a do GDD v8, D-245): a câmera fica à frente do
    /// anão, acima dele, olhando para baixo de modo que a fila de cristais apareça em coluna, sem esconder o anão. Valores de apresentação
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
        /// A câmera do GDD v8.2 (§7.5, D-245; Marco 9h): inclinação 60°, 5,5 m de altura, 3 m até o anão (na horizontal: com
        /// a altura, a câmera olha direto para ele) e campo de visão de 60°. Substitui a F1 como padrão do jogo. O anão fica
        /// no meio da tela, menor que na F1, e embaixo dele aparecem de 3 m (9:16) a 3,6 m (9:20) de chão à frente.
        /// </summary>
        public static DescentFraming GddV8 => new DescentFraming
        {
            height = 5.5f,
            tilt = 60f,
            distance = 3f,
            fieldOfView = 60f,
            corridorWidth = 3f,
        };

        /// <summary>
        /// A câmera F1, escolhida pelo Bruno sobre prévias do jogo com arte (D-69, 2026-10-23). Supera a D-50 (2,4 m, 15°,
        /// 4,8 m), em que os cristais da fila escondiam o anão. Foi o padrão até o Marco 9h; continua no DBG → Câmera para
        /// comparar, e é a base do recuo (D-148), da névoa e do limite dos esteios.
        /// </summary>
        public static DescentFraming F1 => new DescentFraming
        {
            height = 3.6f,
            tilt = 30f,
            distance = 5f,
            fieldOfView = 40f,
            corridorWidth = 3f,
        };

        /// <summary>A câmera com que o jogo abre e para onde o "Padrão" do DBG volta: a do v8 (D-245).</summary>
        public static DescentFraming Default => GddV8;

        /// <summary>Se a câmera é a mesma de <paramref name="other"/> (altura, inclinação, distância e campo de visão).</summary>
        public bool SameView(DescentFraming other) =>
            Mathf.Abs(height - other.height) < 0.01f && Mathf.Abs(tilt - other.tilt) < 0.01f
            && Mathf.Abs(distance - other.distance) < 0.01f && Mathf.Abs(fieldOfView - other.fieldOfView) < 0.01f;

        /// <summary>Os limites dos controles do DBG (os do simulador, com espaço em volta da câmera do v8).</summary>
        public DescentFraming Clamped() => new DescentFraming
        {
            height = Mathf.Clamp(height, 0.8f, 9f),
            tilt = Mathf.Clamp(tilt, 0f, 75f),
            distance = Mathf.Clamp(distance, 1.5f, 12f),
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
