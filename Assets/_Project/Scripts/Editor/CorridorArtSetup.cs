using System;
using System.Collections.Generic;
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
    ///
    /// Desde a E9 (D-90), o chão é arte própria: o tile do Bruno (<c>tileset_ground_01</c>, no git) entra no lugar do
    /// calçamento, e os outros tons (e o calçamento, enquanto o pack estiver no PC) ficam no DBG → Câmera → Chão. Desde a
    /// E10 (D-91), o esteio também é arte própria (<c>mine_wood</c>), no lugar do portal do pack.
    /// </summary>
    static class CorridorArtSetup
    {
        const string PackFolder = "Assets/_Vendor/AssetStore/PixelFantasyCaves";
        const string SourcePath = PackFolder + "/mainlev_build.png";
        const string GeneratedFolder = PackFolder + "/Generated";
        internal const string RockPath = GeneratedFolder + "/rock_dark.png";
        internal const string CobblePath = GeneratedFolder + "/cobble.png";
        internal const string SupportPath = GeneratedFolder + "/support_portal.png";

        /// <summary>Tiles do chão desenhados pelo Bruno (M1b E9, D-90): o mesmo desenho em 4 tons, do claro ao escuro.</summary>
        const string TileFolder = "Assets/_Project/Art/Tile";
        const string OwnFloorPath = TileFolder + "/tileset_ground_01.png";

        /// <summary>Os chãos do DBG → Câmera → Chão, na ordem do botão (o nome aparece nele).</summary>
        static readonly (string label, string path)[] FloorOptions =
        {
            ("Chão 01", OwnFloorPath),
            ("Chão 02", TileFolder + "/tileset_ground_02.png"),
            ("Chão 03", TileFolder + "/tileset_ground_03.png"),
            ("Chão 04", TileFolder + "/tileset_ground_04.png"),
            ("Calçamento", CobblePath),
        };

        /// <summary>Esteio de madeira próprio (M1b E10, D-91): entra no lugar do portal do pack.</summary>
        const string OwnSupportPath = "Assets/_Project/Art/Corridor/mine_wood.png";

        internal const string FloorButton = "Floor";
        const string FloorButtonLabel = "Chão";

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
            int tiles = 0;
            if (Directory.Exists(TileFolder))
                foreach (var path in Directory.GetFiles(TileFolder, "tileset_ground_*.png"))
                    tiles += Configure(path.Replace('\\', '/'), TextureWrapMode.Repeat);
            if (tiles > 0) Debug.Log($"[Setup] Tiles do chão (arte própria) configurados (pixel art): {tiles}.");
            if (Configure(OwnSupportPath, TextureWrapMode.Clamp) > 0)
                Debug.Log("[Setup] Esteio de madeira (arte própria) configurado (pixel art).");

            if (!File.Exists(SourcePath))
            {
                Debug.LogWarning($"[Setup] Pixel Fantasy Caves ausente em {PackFolder}: parede, teto e esteios continuam desenhados por código (D-65).");
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
            changed |= SetIfEmpty(so, "wallTexture", RockPath);
            changed |= SetIfEmpty(so, "ceilingTexture", RockPath);
            changed |= SetIfEmpty(so, "edgeRockTexture", EdgeRockPath);
            if (SetIfEmpty(so, "supportTexture", SupportPath))
            {
                so.FindProperty("supportSize").vector2Value = new Vector2(3.4f, 4.5f);
                changed = true;
            }
            if (changed) so.FindProperty("wallTint").colorValue = WallTint;
            changed |= EnsureOwnFloor(so);
            changed |= EnsureOwnSupport(so);
            changed |= EnsureFloorOptions(so);

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
            changed |= EnsureFloorButton();
            if (changed) Debug.Log("[Setup] Corredor da caverna 1 com a arte do Pixel Fantasy Caves (M1b E4, D-74).");
            return changed;
        }

        /// <summary>
        /// O chão passa a ser o tile do Bruno (D-90): no campo vazio ou no lugar do calçamento do pack. Outra textura posta
        /// à mão fica. Devolve se mudou.
        /// </summary>
        static bool EnsureOwnFloor(SerializedObject so)
        {
            var floor = so.FindProperty("floorTexture");
            var current = floor.objectReferenceValue;
            if (current != null && AssetDatabase.GetAssetPath(current) != CobblePath) return false;
            var own = AssetDatabase.LoadAssetAtPath<Texture2D>(OwnFloorPath);
            if (own == null) return false;
            floor.objectReferenceValue = own;
            Debug.Log("[Setup] Chão da caverna 1 = tile do Bruno (tileset_ground_01; M1b E9, D-90).");
            return true;
        }

        /// <summary>
        /// O esteio passa a ser o <c>mine_wood</c> (D-91): no campo vazio ou no lugar do portal do pack. Mesma proporção
        /// 3:4 do portal, então o <c>supportSize</c> fica. Outra textura posta à mão fica. Devolve se mudou.
        /// </summary>
        static bool EnsureOwnSupport(SerializedObject so)
        {
            var support = so.FindProperty("supportTexture");
            var current = support.objectReferenceValue;
            if (current != null && AssetDatabase.GetAssetPath(current) != SupportPath) return false;
            var own = AssetDatabase.LoadAssetAtPath<Texture2D>(OwnSupportPath);
            if (own == null) return false;
            support.objectReferenceValue = own;
            Debug.Log("[Setup] Esteios da caverna 1 = madeira própria (mine_wood; M1b E10, D-91).");
            return true;
        }

        /// <summary>A lista do DBG → Câmera → Chão, só com as texturas que existem neste PC. Devolve se mudou.</summary>
        static bool EnsureFloorOptions(SerializedObject so)
        {
            var wanted = new List<(string label, Texture2D texture)>();
            foreach (var (label, path) in FloorOptions)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null) wanted.Add((label, texture));
            }

            var options = so.FindProperty("floorOptions");
            bool same = options.arraySize == wanted.Count;
            for (int i = 0; same && i < wanted.Count; i++)
            {
                var option = options.GetArrayElementAtIndex(i);
                same = option.FindPropertyRelative("label").stringValue == wanted[i].label
                       && option.FindPropertyRelative("texture").objectReferenceValue == wanted[i].texture;
            }
            if (same) return false;

            options.arraySize = wanted.Count;
            for (int i = 0; i < wanted.Count; i++)
            {
                var option = options.GetArrayElementAtIndex(i);
                option.FindPropertyRelative("label").stringValue = wanted[i].label;
                option.FindPropertyRelative("texture").objectReferenceValue = wanted[i].texture;
            }
            Debug.Log($"[Setup] Chãos do DBG → Câmera → Chão: {wanted.Count}.");
            return true;
        }

        static bool EnsureFloorButton()
        {
            var panel = UnityEngine.Object.FindAnyObjectByType<MinersIdle.UI.DescentCameraPanel>(FindObjectsInactive.Include);
            var buttons = panel != null ? panel.transform.Find("Content/Buttons") : null;
            if (buttons == null || buttons.Find(FloorButton) != null) return false;

            var button = DebugPanelSetup.CreateButton(buttons, FloorButton, FloorButtonLabel);
            GrayboxSetup.SetReferences(panel, ("floor", button));
            Debug.Log("[Setup] Botão \"Chão\" no DBG → Câmera (M1b E9).");
            return true;
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
