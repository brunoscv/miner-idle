using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="ProjectSetup"/> para a cara de jogo da interface (M1b, Art Spike, parte 1). Na E1: importa os
    /// sprites de UI em pixel art (filtro Point, sem compressão, 1 px do sprite = 1 px da referência 270, bordas de
    /// 9-slice), cria o font asset da Pixelify Sans (D-64) e põe essa fonte nos textos do jogo. O painel DBG e o BuildInfo
    /// ficam na fonte de antes (ferramenta de desenvolvimento).
    ///
    /// Os packs de terceiros ficam fora do git (D-65): sem eles, o setup avisa e o jogo continua com o visual graybox.
    /// Idempotente: só reimporta ou salva o que estiver diferente.
    /// </summary>
    static class UiSkinSetup
    {
        const string VendorFolder = "Assets/_Vendor/AssetStore";
        internal const string WoodFolder = VendorFolder + "/PixelUIKit/themes/wood/png";
        const string WoodAtlasJson = VendorFolder + "/PixelUIKit/themes/wood/atlas_sample.json";
        internal const string FantasyFolder = VendorFolder + "/FantasyUIKitLite/x1";
        internal const string IconsFolder = VendorFolder + "/AnvilRpgIcons";
        const string ProjectIconsFolder = "Assets/_Project/Art/UI";
        const string CrystalIconPath = "Assets/_Project/Art/Crystals/icon_crystal.png";

        const string FontFilePath = "Assets/_Vendor/Fonts/PixelifySans/PixelifySans-Regular.ttf";
        internal const string GameFontPath = "Assets/_Project/Art/Fonts/PixelifySans Bitmap.asset";
        const string FallbackFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        const string RowPrefabPath = "Assets/_Project/Prefabs/UI/UpgradeRow.prefab";

        /// <summary>Canvas.referencePixelsPerUnit (padrão 100): com o mesmo PPU, 1 px do sprite = 1 px da referência.</summary>
        const float UiPixelsPerUnit = 100f;

        /// <summary>Borda de 9-slice das peças do Fantasy UI Kit (manifest.json: nine_slice_border_px_x1).</summary>
        const int FantasyBorder = 6;
        static readonly string[] FantasySliced = { "buttons/button_ruby_", "panels/", "inventory/", "bars/bar_frame" };

        /// <summary>
        /// A Pixelify Sans é desenhada num grid de 11 "pixels" por em (medido nos contornos: 1000 / 11 unidades). Um tamanho
        /// múltiplo de 11/4 px da referência põe cada pixel da fonte num número inteiro de pixels de tela no Moto G75
        /// (4 px de tela por px da referência): 5,5 = 2 px, 8,25 = 3 px, 11 = 4 px.
        /// </summary>
        const float FontPixelStep = FontGridPixels / 4f;
        const int FontGridPixels = 11;

        /// <summary>Os caracteres dos textos do jogo em pt-BR (o atlas é estático; o que faltar cai na fonte reserva).</summary>
        const string GameCharacters =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "ÁÀÂÃÄÇÉÈÊËÍÌÎÏÑÓÒÔÕÖÚÙÛÜáàâãäçéèêëíìîïñóòôõöúùûü«»‹›·×°ºª…–—";

        /// <summary>Objetos (e seus filhos) que ficam com a fonte de antes.</summary>
        static readonly string[] KeepOldFont = { "DebugPanel", "BuildInfo" };

        public static void EnsureAssets()
        {
            ConfigureUiSprites();
            EnsureGameFont();
        }

        // ── Sprites ──────────────────────────────────────────────────────────────────────────────────────────────────

        static void ConfigureUiSprites()
        {
            var missing = new List<string>();
            int changed = 0;

            if (AssetDatabase.IsValidFolder(WoodFolder))
            {
                var borders = ReadWoodBorders();
                foreach (var path in Pngs(WoodFolder))
                {
                    borders.TryGetValue(Path.GetFileNameWithoutExtension(path), out var border);
                    changed += ConfigureSprite(path, border);
                }
            }
            else missing.Add("PixelUIKit");

            if (AssetDatabase.IsValidFolder(FantasyFolder))
            {
                foreach (var path in Pngs(FantasyFolder))
                {
                    var relative = path.Substring(FantasyFolder.Length + 1);
                    bool sliced = System.Array.Exists(FantasySliced, relative.StartsWith);
                    changed += ConfigureSprite(path, sliced ? Vector4.one * FantasyBorder : Vector4.zero);
                }
            }
            else missing.Add("FantasyUIKitLite");

            if (AssetDatabase.IsValidFolder(IconsFolder))
                foreach (var path in Pngs(IconsFolder)) changed += ConfigureSprite(path, Vector4.zero);
            else missing.Add("AnvilRpgIcons");

            foreach (var path in Pngs(ProjectIconsFolder)) changed += ConfigureSprite(path, Vector4.zero);
            if (File.Exists(CrystalIconPath)) changed += ConfigureSprite(CrystalIconPath, Vector4.zero);

            if (changed > 0) Debug.Log($"[Setup] Sprites de UI configurados (pixel art): {changed}.");
            if (missing.Count > 0)
                Debug.LogWarning($"[Setup] Packs de UI ausentes em {VendorFolder}: {string.Join(", ", missing)}. " +
                                 "A interface continua no visual graybox (D-65).");
        }

        static IEnumerable<string> Pngs(string folder)
        {
            if (!Directory.Exists(folder)) yield break;
            foreach (var file in Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories))
                yield return file.Replace('\\', '/');
        }

        /// <summary>Sprite em pixel art para a UI. Devolve 1 se mudou algo (e reimportou), 0 se já estava certo.</summary>
        static int ConfigureSprite(string path, Vector4 border)
        {
            if (AssetImporter.GetAtPath(path) == null) AssetDatabase.ImportAsset(path);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return 0;

            bool same = importer.textureType == TextureImporterType.Sprite
                        && importer.spriteImportMode == SpriteImportMode.Single
                        && Mathf.Approximately(importer.spritePixelsPerUnit, UiPixelsPerUnit)
                        && importer.filterMode == FilterMode.Point
                        && importer.textureCompression == TextureImporterCompression.Uncompressed
                        && !importer.mipmapEnabled
                        && importer.alphaIsTransparency
                        && importer.wrapMode == TextureWrapMode.Clamp
                        && importer.spriteBorder == border;
            if (same) return 0;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = UiPixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spriteBorder = border;
            importer.SaveAndReimport();
            return 1;
        }

        /// <summary>
        /// Bordas de 9-slice do kit de madeira, do atlas_sample.json ("slice": left, top, right, bottom). No Unity a ordem é
        /// (esquerda, baixo, direita, cima).
        /// </summary>
        static Dictionary<string, Vector4> ReadWoodBorders()
        {
            var borders = new Dictionary<string, Vector4>();
            if (!File.Exists(WoodAtlasJson)) return borders;

            var json = File.ReadAllText(WoodAtlasJson);
            var frame = new Regex(@"""file"":\s*""png/[^""]+/(?<name>[^""/]+)\.png""[^{}]*?""slice"":\s*\{\s*""left"":\s*(?<l>\d+),\s*""top"":\s*(?<t>\d+),\s*""right"":\s*(?<r>\d+),\s*""bottom"":\s*(?<b>\d+)");
            foreach (Match m in frame.Matches(json))
                borders[m.Groups["name"].Value] = new Vector4(
                    int.Parse(m.Groups["l"].Value), int.Parse(m.Groups["b"].Value),
                    int.Parse(m.Groups["r"].Value), int.Parse(m.Groups["t"].Value));
            return borders;
        }

        // ── Fonte ────────────────────────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Font asset em bitmap com atlas estático (os caracteres do jogo), a partir da Pixelify Sans, amostrada a 4 pixels
        /// de atlas por pixel da fonte e ampliada sem suavizar (Point). Achados da E1: em SDF os quadradinhos da fonte, que
        /// se sobrepõem, saíam ocos e fechavam letras ("C" virava "O"); amostrada a 1:1 (11), o espaçamento entre letras
        /// ficava irregular; com hinting, as letras se deformavam. Reserva: LiberationSans.
        /// </summary>
        static void EnsureGameFont()
        {
            if (File.Exists(GameFontPath) || !File.Exists(FontFilePath)) return;

            if (AssetImporter.GetAtPath(FontFilePath) == null) AssetDatabase.ImportAsset(FontFilePath);
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontFilePath);
            if (font == null)
            {
                Debug.LogError($"[Setup] Fonte não carregou: {FontFilePath}.");
                return;
            }

            var asset = TMP_FontAsset.CreateFontAsset(font, FontGridPixels * 4, 4, GlyphRenderMode.RASTER, 1024, 1024,
                                                      AtlasPopulationMode.Dynamic, false);
            asset.name = Path.GetFileNameWithoutExtension(GameFontPath);
            if (!asset.TryAddCharacters(GameCharacters, out var missing))
                Debug.LogWarning($"[Setup] A Pixelify Sans não tem: {missing} (usa a fonte reserva).");

            var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackFontPath);
            if (fallback != null) asset.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };

            ProjectSetup.EnsureFolder(Path.GetDirectoryName(GameFontPath)?.Replace('\\', '/'));
            AssetDatabase.CreateAsset(asset, GameFontPath);
            asset.atlasTexture.name = asset.name + " Atlas";
            asset.material.name = asset.name + " Material";

            asset.atlasTexture.filterMode = FilterMode.Point;
            AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
            AssetDatabase.AddObjectToAsset(asset.material, asset);

            // Estático: o atlas não muda mais em tempo de jogo (nem suja o asset no Editor).
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("m_AtlasPopulationMode").intValue = (int)AtlasPopulationMode.Static;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Setup] Fonte do jogo criada em {GameFontPath} ({asset.characterTable.Count} caracteres).");
        }

        /// <summary>Na cena Game (já aberta) e no prefab da linha da loja: a fonte do jogo nos textos. Devolve se mudou a cena.</summary>
        public static bool EnsureInScene()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(GameFontPath);
            if (font == null) return false;

            int changed = 0;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                changed += ApplyFont(root.transform, font);
            if (changed > 0) Debug.Log($"[Setup] Fonte do jogo aplicada a {changed} texto(s) da cena Game.");

            if (!File.Exists(RowPrefabPath)) return changed > 0;
            var prefab = PrefabUtility.LoadPrefabContents(RowPrefabPath);
            int rowChanged = ApplyFont(prefab.transform, font);
            if (rowChanged > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(prefab, RowPrefabPath);
                Debug.Log($"[Setup] Fonte do jogo aplicada ao prefab {RowPrefabPath}.");
            }
            PrefabUtility.UnloadPrefabContents(prefab);

            return changed > 0;
        }

        static int ApplyFont(Transform node, TMP_FontAsset font)
        {
            if (System.Array.IndexOf(KeepOldFont, node.name) >= 0) return 0;

            int changed = 0;
            if (node.TryGetComponent<TMP_Text>(out var text) && text.font != font)
            {
                text.font = font;
                text.fontSharedMaterial = font.material;
                // Na UI, o tamanho vai para o múltiplo mais próximo do grid da fonte (nítido no aparelho). Os textos do
                // mundo (flutuantes) mudam de escala o tempo todo; ficam com o tamanho que têm.
                if (text is TextMeshProUGUI) text.fontSize = SnapFontSize(text.fontSize);
                EditorUtility.SetDirty(text);
                changed++;
            }

            foreach (Transform child in node) changed += ApplyFont(child, font);
            return changed;
        }

        internal static float SnapFontSize(float size) =>
            Mathf.Max(2f, Mathf.Round(size / FontPixelStep)) * FontPixelStep;
    }
}
