using System.IO;
using UnityEditor;
using UnityEngine;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="ProjectSetup"/> para o M9g (D-315): a partir do <c>icon_bomb.png</c> do Bruno (48×48 entregue
    /// ampliado 2×, com a sombra embaixo), gera a folha das 8 bombas em 1× (<c>bombs.png</c>, 8 quadros de 48 px, na
    /// ordem da D-240). Só o corpo da bomba muda de cor; o contorno, o brilho, o pavio e a sombra ficam. A Pequena e a
    /// Normal são o desenho do Bruno; as vermelhas, recoloridas de vermelho; o tier 2 (Dinamite, Pacote, Caixa, Barril)
    /// leva cores provisórias até o Bruno desenhar cada um (lista de ajustes, item 1). O tamanho de cada bomba na tela é
    /// dado da apresentação, não do desenho. Só gera o que falta: apagar o arquivo gera de novo.
    /// </summary>
    static class BombArtSetup
    {
        internal const string SourcePath = "Assets/_Project/Art/UI/icon_bomb.png";
        internal const string SheetPath = "Assets/_Project/Art/UI/bombs.png";
        internal const int Frame = 48;

        /// <summary>As duas cores do corpo no desenho do Bruno: o tom médio e o escuro.</summary>
        static readonly Color32 BodyMid = new Color32(57, 74, 80, 255);
        static readonly Color32 BodyDark = new Color32(32, 46, 55, 255);

        /// <summary>As linhas do corpo no quadro de 48 (de cima para baixo): o gargalo de metal fica de fora, e a sombra também.</summary>
        const int BodyTop = 15, BodyBottom = 31;

        /// <summary>A cor de cada bomba (meio, escuro); <c>null</c> = a do Bruno, sem mudar.</summary>
        static readonly (Color32 mid, Color32 dark)?[] Colors =
        {
            null,                                                                     // Pequena
            (new Color32(176, 62, 54, 255), new Color32(112, 34, 38, 255)),           // Pequena Vermelha
            null,                                                                     // Normal
            (new Color32(176, 62, 54, 255), new Color32(112, 34, 38, 255)),           // Normal Vermelha
            (new Color32(214, 104, 44, 255), new Color32(140, 58, 30, 255)),          // Dinamite (provisória)
            (new Color32(176, 138, 86, 255), new Color32(116, 86, 54, 255)),          // Pacote (provisória)
            (new Color32(126, 112, 64, 255), new Color32(82, 72, 42, 255)),           // Caixa (provisória)
            (new Color32(206, 164, 64, 255), new Color32(140, 102, 38, 255)),         // Barril (provisória)
        };

        public static void EnsureAssets()
        {
            if (!File.Exists(SourcePath) || File.Exists(SheetPath)) return;

            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(File.ReadAllBytes(SourcePath));
            int scale = Mathf.Max(1, source.width / Frame); // o Bruno entrega 2×
            var sheet = new Texture2D(Frame * Colors.Length, Frame, TextureFormat.RGBA32, false);
            var clear = new Color32[sheet.width * sheet.height];
            sheet.SetPixels32(clear);

            for (int b = 0; b < Colors.Length; b++)
            {
                for (int y = 0; y < Frame; y++) // y = 0 é a linha de baixo na textura
                {
                    int row = Frame - 1 - y; // de cima para baixo, como no desenho
                    bool body = row >= BodyTop && row <= BodyBottom;
                    for (int x = 0; x < Frame; x++)
                    {
                        Color32 p = source.GetPixel(x * scale, y * scale);
                        if (body && Colors[b].HasValue)
                        {
                            if (Same(p, BodyMid)) p = Colors[b].Value.mid;
                            else if (Same(p, BodyDark)) p = Colors[b].Value.dark;
                        }
                        sheet.SetPixel(b * Frame + x, y, p);
                    }
                }
            }
            File.WriteAllBytes(SheetPath, sheet.EncodeToPNG());
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(sheet);
            AssetDatabase.ImportAsset(SheetPath);
            Debug.Log($"[Setup] Folha das 8 bombas gerada do icon_bomb do Bruno em {SheetPath} (M9g, D-315).");
        }

        static bool Same(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
    }
}
