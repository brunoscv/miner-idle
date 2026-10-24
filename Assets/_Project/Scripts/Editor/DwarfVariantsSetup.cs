using System.IO;
using UnityEditor;
using UnityEngine;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="ProjectSetup"/> para a E7 do M1b (D-76): variações de cor do anão do Bruno, guardadas no jogo
    /// para um futuro seletor de cor do jogador. Cada variação recolore só a barba (vermelha) e o capacete (dourado) das
    /// folhas do PixelLab; pele, couro e picareta ficam iguais. A grisalha com capacete de aço é a do estagiário. Os PNGs
    /// são arte do projeto (derivada da do Bruno) e vão no git. Só gera o que falta.
    /// </summary>
    static class DwarfVariantsSetup
    {
        const string Folder = "Assets/_Project/Art/Character";
        internal const string VariantsFolder = Folder + "/Variants";
        static readonly string[] Sheets = { "dwarf-idle", "dwarf-walking", "dwarf-mining" };

        /// <summary>Nome, matiz da barba e quanto da saturação dela fica (capacete sempre de aço).</summary>
        static readonly (string name, float beardHue, float beardSaturation)[] Variants =
        {
            ("castanho", 0.07f, 0.75f),
            ("loiro", 0.11f, 0.6f),
            ("grisalho", 0f, 0.15f),
        };

        const float SteelHue = 0.6f, SteelSaturation = 0.12f;

        internal static string PathOf(string sheet, string variant) => $"{VariantsFolder}/{sheet}-{variant}.png";

        public static void EnsureAssets()
        {
            int made = 0;
            foreach (var sheet in Sheets)
            {
                var sourcePath = $"{Folder}/{sheet}.png";
                if (!File.Exists(sourcePath)) continue;

                Texture2D source = null;
                foreach (var (name, hue, saturation) in Variants)
                {
                    var path = PathOf(sheet, name);
                    if (File.Exists(path)) continue;
                    if (source == null)
                    {
                        source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        source.LoadImage(File.ReadAllBytes(sourcePath));
                    }
                    ProjectSetup.EnsureFolder(VariantsFolder);
                    var recolored = Recolor(source, hue, saturation);
                    File.WriteAllBytes(path, recolored.EncodeToPNG());
                    Object.DestroyImmediate(recolored);
                    AssetDatabase.ImportAsset(path);
                    made++;
                }
                if (source != null) Object.DestroyImmediate(source);
            }

            int configured = 0;
            foreach (var sheet in Sheets)
            foreach (var (name, _, _) in Variants)
                configured += DwarfArtSetup.Configure(PathOf(sheet, name));
            if (made > 0) Debug.Log($"[Setup] Variações de cor do anão geradas em {VariantsFolder}: {made} (D-76).");
            if (configured > 0) Debug.Log($"[Setup] Variações do anão configuradas (pixel art): {configured}.");
        }

        /// <summary>
        /// Na cena Game (já aberta): o estagiário com as folhas grisalhas e o botão "Estagiário" no painel de câmera do DBG.
        /// Devolve se mudou a cena.
        /// </summary>
        /// <summary>Botões do painel de câmera do DBG: Resolução, Padrão F1, Fechar, Estagiário e Anão 64px, numa linha.</summary>
        internal const int CameraButtonColumns = 5;
        internal static readonly Vector2 CameraButtonCell = new Vector2(48f, 16f);

        public static bool EnsureInScene()
        {
            bool changed = false;
            var stage = Object.FindAnyObjectByType<MinersIdle.Presentation.DescentStage>();
            var run = PathOf("dwarf-walking", "grisalho");
            if (stage != null && File.Exists(run))
            {
                var so = new SerializedObject(stage);
                var internRun = so.FindProperty("internRunSheet");
                if (internRun.objectReferenceValue == null)
                {
                    internRun.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(run);
                    so.FindProperty("internMineSheet").objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<Texture2D>(PathOf("dwarf-mining", "grisalho"));
                    so.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }
            }

            var panel = Object.FindAnyObjectByType<MinersIdle.UI.DescentCameraPanel>(FindObjectsInactive.Include);
            var buttons = panel != null ? panel.transform.Find("Content/Buttons") : null;
            if (buttons != null && buttons.Find("Intern") == null)
            {
                var button = DebugPanelSetup.CreateButton(buttons, "Intern", "Estagiário");
                GrayboxSetup.SetReferences(panel, ("intern", button));
                changed = true;
            }
            // Os botões numa linha só: numa 2ª linha eles ficam fora do painel.
            var grid = buttons != null ? buttons.GetComponent<UnityEngine.UI.GridLayoutGroup>() : null;
            if (grid != null && grid.constraintCount != CameraButtonColumns)
            {
                grid.constraintCount = CameraButtonColumns;
                grid.cellSize = CameraButtonCell;
                changed = true;
            }

            if (changed) Debug.Log("[Setup] Estagiário grisalho na descida, ligado pelo DBG → Câmera → Estagiário (M1b E7).");
            return changed;
        }

        /// <summary>Barba vermelha → a cor da variação; capacete dourado → aço. O resto do desenho não muda.</summary>
        static Texture2D Recolor(Texture2D source, float beardHue, float beardSaturation)
        {
            var pixels = source.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                if (p.a == 0) continue;
                Color.RGBToHSV(p, out float h, out float s, out float v);
                Color c;
                if (s > 0.55f && (h < 0.035f || h > 0.95f) && v > 0.25f) // barba
                    c = Color.HSVToRGB(beardHue, s * beardSaturation, v * 0.85f);
                else if (h > 0.09f && h < 0.17f && s > 0.45f && v > 0.3f) // capacete (e detalhes dourados)
                    c = Color.HSVToRGB(SteelHue, SteelSaturation, v * 0.95f);
                else continue;
                pixels[i] = new Color32((byte)(c.r * 255f), (byte)(c.g * 255f), (byte)(c.b * 255f), p.a);
            }
            var result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            result.SetPixels32(pixels);
            return result;
        }
    }
}
