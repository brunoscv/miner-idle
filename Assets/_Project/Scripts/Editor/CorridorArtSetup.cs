using System;
using System.IO;
using MinersIdle.Presentation;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="ProjectSetup"/> para a E4 do M1b (D-74): a arte do corredor da caverna 1. Gera, a partir do
    /// <i>Pixel Fantasy Caves</i> (Szadi art.), texturas grandes de rocha escura e de calçamento montadas com carimbos
    /// sorteados (semente fixa: sai sempre igual), sem o padrão repetido de um tile pequeno, e o portal de madeira sem o
    /// miolo escuro, para servir de esteio. Põe tudo no <see cref="DescentStage"/> da cena Game, com as gemas do Nasha nas
    /// paredes, e passa a câmera da cena para a F1 (D-69).
    ///
    /// O pack e as texturas derivadas ficam fora do git (D-65): sem eles, o corredor continua desenhado por código.
    /// Idempotente: só gera o que falta e não sobrescreve campos preenchidos à mão.
    /// </summary>
    static class CorridorArtSetup
    {
        const string PackFolder = "Assets/_Vendor/AssetStore/PixelFantasyCaves";
        const string SourcePath = PackFolder + "/mainlev_build.png";
        const string GeneratedFolder = PackFolder + "/Generated";
        internal const string RockPath = GeneratedFolder + "/rock_dark.png";
        internal const string CobblePath = GeneratedFolder + "/cobble.png";
        internal const string SupportPath = GeneratedFolder + "/support_portal.png";

        /// <summary>Pedra da beira do caminho (enviada pelo Bruno; origem a confirmar, por isso fora do git).</summary>
        const string EdgeRockPath = "Assets/_Vendor/AssetStore/Misc/rock_t1.png";

        // Peças do mainlev_build.png (x, y a partir do canto de cima, largura, altura), achadas nas prévias.
        static readonly RectInt RockA = new RectInt(224, 736, 96, 64);
        static readonly RectInt RockB = new RectInt(224, 816, 96, 64);
        static readonly RectInt Cobble = new RectInt(624, 848, 32, 32);
        static readonly RectInt Portal = new RectInt(960, 16, 48, 64);

        const int TextureSize = 256;

        /// <summary>Tinta da parede: na câmera F1 ela aparece pouco atrás dos esteios; um pouco mais clara.</summary>
        static readonly Color WallTint = new Color(1.35f, 1.35f, 1.35f, 1f);

        public static void EnsureAssets()
        {
            if (!File.Exists(SourcePath))
            {
                Debug.LogWarning($"[Setup] Pixel Fantasy Caves ausente em {PackFolder}: o corredor continua desenhado por código (D-65).");
                return;
            }

            var source = LoadReadable(SourcePath);
            int made = 0;
            if (!File.Exists(RockPath))
            {
                var pieces = new[] { KeyDark(Crop(source, RockA), 26), KeyDark(Crop(source, RockB), 26) };
                Save(RockPath, Stamps(new Color32(21, 17, 17, 255), pieces, 26, seed: 1));
                made++;
            }
            if (!File.Exists(CobblePath))
            {
                Save(CobblePath, CobbleFloor(Crop(source, Cobble), seed: 3));
                made++;
            }
            if (!File.Exists(SupportPath))
            {
                Save(SupportPath, OpenPortal(Crop(source, Portal)));
                made++;
            }
            UnityEngine.Object.DestroyImmediate(source);

            int configured = Configure(RockPath, TextureWrapMode.Repeat) + Configure(CobblePath, TextureWrapMode.Repeat)
                             + Configure(SupportPath, TextureWrapMode.Clamp);
            if (File.Exists(EdgeRockPath)) configured += Configure(EdgeRockPath, TextureWrapMode.Clamp);

            if (made > 0) Debug.Log($"[Setup] Texturas do corredor geradas em {GeneratedFolder}: {made}.");
            if (configured > 0) Debug.Log($"[Setup] Texturas do corredor configuradas (pixel art): {configured}.");
        }

        /// <summary>No DescentStage da cena Game (já aberta): a arte do corredor e a câmera F1. Devolve se mudou a cena.</summary>
        public static bool EnsureInScene()
        {
            var stage = UnityEngine.Object.FindAnyObjectByType<DescentStage>();
            if (stage == null) return false;

            var so = new SerializedObject(stage);
            bool changed = false;
            changed |= SetIfEmpty(so, "floorTexture", CobblePath);
            changed |= SetIfEmpty(so, "wallTexture", RockPath);
            changed |= SetIfEmpty(so, "ceilingTexture", RockPath);
            changed |= SetIfEmpty(so, "edgeRockTexture", EdgeRockPath);
            if (SetIfEmpty(so, "supportTexture", SupportPath))
            {
                so.FindProperty("supportSize").vector2Value = new Vector2(3.4f, 4.5f);
                changed = true;
            }
            if (changed) so.FindProperty("wallTint").colorValue = WallTint;

            // A pedra encosta no chão: desce as linhas transparentes de baixo do desenho.
            var footGap = so.FindProperty("edgeRockFootGap");
            float gap = File.Exists(EdgeRockPath) ? BottomGap(EdgeRockPath) : 0f;
            if (!Mathf.Approximately(footGap.floatValue, gap))
            {
                footGap.floatValue = gap;
                changed = true;
            }

            // A câmera da cena ainda na D-50 (2,4 m, 15°, 4,8 m) passa para a F1 (D-69).
            var framing = so.FindProperty("framing");
            if (Mathf.Approximately(framing.FindPropertyRelative("height").floatValue, 2.4f)
                && Mathf.Approximately(framing.FindPropertyRelative("tilt").floatValue, 15f))
            {
                var f1 = DescentFraming.Validated;
                framing.FindPropertyRelative("height").floatValue = f1.height;
                framing.FindPropertyRelative("tilt").floatValue = f1.tilt;
                framing.FindPropertyRelative("distance").floatValue = f1.distance;
                Debug.Log("[Setup] Câmera da descida passa para a F1 (D-69).");
                changed = true;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            changed |= RenameResetButton();
            if (changed) Debug.Log("[Setup] Corredor da caverna 1 com a arte do Pixel Fantasy Caves (M1b E4, D-74).");
            return changed;
        }

        /// <summary>O botão do DBG dizia "Padrão D-50"; agora o padrão é a F1.</summary>
        static bool RenameResetButton()
        {
            var panel = UnityEngine.Object.FindAnyObjectByType<MinersIdle.UI.DescentCameraPanel>(FindObjectsInactive.Include);
            var label = panel != null ? panel.transform.Find("Content/Buttons/ResetFraming/Label") : null;
            if (label == null || !label.TryGetComponent<TMP_Text>(out var text) || text.text != "Padrão D-50") return false;
            text.text = "Padrão F1";
            EditorUtility.SetDirty(text);
            return true;
        }

        static bool SetIfEmpty(SerializedObject so, string field, string path)
        {
            var property = so.FindProperty(field);
            if (property.objectReferenceValue != null || !File.Exists(path)) return false;
            property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            return property.objectReferenceValue != null;
        }

        // ── Geração ──────────────────────────────────────────────────────────────────────────────────────────────────

        /// <summary>Fração da altura da imagem abaixo do último pixel visível (as linhas transparentes de baixo).</summary>
        static float BottomGap(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(path));
            int row = 0;
            while (row < texture.height && !RowHasPixels(texture, row)) row++;
            float gap = row < texture.height ? (float)row / texture.height : 0f;
            UnityEngine.Object.DestroyImmediate(texture);
            return gap;
        }

        static bool RowHasPixels(Texture2D texture, int row)
        {
            for (int x = 0; x < texture.width; x++)
                if (texture.GetPixel(x, row).a > 0f) return true;
            return false;
        }

        static Texture2D LoadReadable(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(path));
            return texture;
        }

        /// <summary>Recorte a partir do canto de cima (como no editor de imagem); devolve os pixels de cima para baixo.</summary>
        static Color32[,] Crop(Texture2D source, RectInt rect)
        {
            var pixels = new Color32[rect.width, rect.height];
            for (int y = 0; y < rect.height; y++)
            for (int x = 0; x < rect.width; x++)
                pixels[x, y] = source.GetPixel(rect.x + x, source.height - 1 - (rect.y + y));
            return pixels;
        }

        /// <summary>Fundo escuro do carimbo vira transparente (só a rocha é carimbada).</summary>
        static Color32[,] KeyDark(Color32[,] piece, int limit)
        {
            for (int y = 0; y < piece.GetLength(1); y++)
            for (int x = 0; x < piece.GetLength(0); x++)
            {
                var c = piece[x, y];
                if (c.a > 0 && (c.r + c.g + c.b) / 3 < limit) piece[x, y] = new Color32(c.r, c.g, c.b, 0);
            }
            return piece;
        }

        /// <summary>Carimbos em posições e espelhamentos sorteados, com emenda nas bordas (a textura repete sem costura).</summary>
        static Color32[,] Stamps(Color32 background, Color32[][,] pieces, int count, int seed)
        {
            var random = new System.Random(seed);
            var canvas = new Color32[TextureSize, TextureSize];
            for (int y = 0; y < TextureSize; y++)
            for (int x = 0; x < TextureSize; x++)
                canvas[x, y] = background;

            for (int n = 0; n < count; n++)
            {
                var piece = pieces[random.Next(pieces.Length)];
                bool flipX = random.NextDouble() < 0.5;
                bool flipY = random.NextDouble() < 0.3;
                int ox = random.Next(TextureSize), oy = random.Next(TextureSize);
                int w = piece.GetLength(0), h = piece.GetLength(1);
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var c = piece[flipX ? w - 1 - x : x, flipY ? h - 1 - y : y];
                    if (c.a > 0) canvas[(ox + x) % TextureSize, (oy + y) % TextureSize] = c;
                }
            }
            return canvas;
        }

        /// <summary>Calçamento: o tile em grade, girado e espelhado ao acaso (sem padrão visível).</summary>
        static Color32[,] CobbleFloor(Color32[,] tile, int seed)
        {
            var random = new System.Random(seed);
            int t = tile.GetLength(0);
            var canvas = new Color32[TextureSize, TextureSize];
            for (int gy = 0; gy < TextureSize; gy += t)
            for (int gx = 0; gx < TextureSize; gx += t)
            {
                int turns = random.Next(4);
                bool flip = random.NextDouble() < 0.5;
                for (int y = 0; y < t; y++)
                for (int x = 0; x < t; x++)
                {
                    int sx = flip ? t - 1 - x : x, sy = y;
                    for (int r = 0; r < turns; r++) (sx, sy) = (t - 1 - sy, sx);
                    canvas[gx + x, gy + y] = tile[sx, sy];
                }
            }
            return canvas;
        }

        /// <summary>O portal do pack tem o miolo escuro (é uma porta de fase): sai, fica só a madeira.</summary>
        static Color32[,] OpenPortal(Color32[,] portal)
        {
            int w = portal.GetLength(0), h = portal.GetLength(1);
            for (int y = 6; y < h; y++)
            for (int x = 6; x < w - 6; x++)
            {
                var c = portal[x, y];
                if (c.a > 0 && (c.r + c.g + c.b) / 3 < 32) portal[x, y] = new Color32(c.r, c.g, c.b, 0);
            }
            return portal;
        }

        static void Save(string path, Color32[,] pixels)
        {
            int w = pixels.GetLength(0), h = pixels.GetLength(1);
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var flat = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                flat[(h - 1 - y) * w + x] = pixels[x, y]; // Texture2D começa embaixo
            texture.SetPixels32(flat);
            ProjectSetup.EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
        }

        /// <summary>Textura em pixel art (Point, sem compressão, sem mipmap). Devolve 1 se mudou algo.</summary>
        static int Configure(string path, TextureWrapMode wrap)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return 0;
            if (importer.textureType == TextureImporterType.Default && importer.filterMode == FilterMode.Point
                && importer.textureCompression == TextureImporterCompression.Uncompressed && !importer.mipmapEnabled
                && importer.wrapMode == wrap && importer.alphaIsTransparency && importer.npotScale == TextureImporterNPOTScale.None)
                return 0;

            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = wrap;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
            return 1;
        }
    }
}
