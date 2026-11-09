using System.Collections.Generic;
using System.IO;
using MinersIdle.Presentation;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="ProjectSetup"/> para a E8 do M1b (D-72, D-102): os biomas das 10 cavernas. Teste da D-145: o
    /// bioma 1 (níveis 1–10 no GDD v7) usa o chão em faixa <c>ground_01a…e</c>; os outros seguem com o chão gerado abaixo. O chão de cada uma é
    /// o tile do Apellon (<c>tileset_ground_01</c>, desenho de 4 cores) com as 4 cores trocadas pela paleta da caverna, e
    /// com pedra pequena, pedra grande e buraco sorteados (semente fixa: sai sempre igual). Cavernas 1–4 = os tons
    /// <c>01</c>…<c>04</c> dele; 5–10 = paletas novas seguindo o piso do GDD v5 §6. Põe os 10 biomas no
    /// <see cref="DescentStage"/>, o esteio com lampião e o botão "Bioma" no DBG → Câmera (no lugar do "Chão" da E9).
    ///
    /// As texturas são regeneradas quando a tabela abaixo muda (só grava se o PNG sair diferente); os assets dos biomas
    /// só são criados se faltarem, para o que foi ajustado à mão ficar.
    /// </summary>
    static class BiomeArtSetup
    {
        const string TileFolder = "Assets/_Project/Art/Tile";
        const string FloorFolder = TileFolder + "/Biomes";
        const string GroundFolder = TileFolder + "/Ground";
        const string DataFolder = "Assets/_Project/Data/Biomes";
        const string LampSupportPath = "Assets/_Project/Art/Corridor/mine_wood_lamp.png";

        const string BaseTile = "tileset_ground_01";
        static readonly string[] DecorTiles = { "tileset_ground_01_rock_01", "tileset_ground_01_rock_02", "tileset_ground_01_hole" };

        /// <summary>As 4 cores do <c>tileset_ground_01</c>, da mais escura à mais clara.</summary>
        static readonly string[] BaseRamp = { "#AD7757", "#C09473", "#D7B594", "#E7D5B3" };

        const int TilePixels = 32;
        const int GridTiles = 8; // 8 × 8 tiles de 0,5 m: 4 × 4 m de chão por repetição

        /// <summary>Chance de um tile ser pedra ou buraco (D-102: ~1 a cada 10).</summary>
        const double DecorChance = 0.1;

        internal const string BiomeButton = "Biome";
        const string BiomeButtonLabel = "Bioma";
        const string OldFloorButton = "Floor";

        sealed class Cave
        {
            public string Name;
            public string[] Ramp;
            public Color WallTint;
            public string Fog;
            public int LampEvery;
            /// <summary>O cristal da fila (D-116): índice em <see cref="CrystalArtSetup.QueueCrystals"/>.</summary>
            public int Crystal;
            /// <summary>
            /// Chão em faixa (D-145): o conjunto <c>{nome}a</c> … <c>{nome}e</c> em <see cref="GroundFolder"/>. Vazio = o chão
            /// gerado da paleta (como antes).
            /// </summary>
            public string FloorTiles;
        }

        /// <summary>
        /// As 10 cavernas (D-102). Nomes: coluna "Sensação" do GDD v5 §6. Cristal (D-116): um só por caverna, a 1 com o
        /// branco (vidro) e depois azul e amarelo em ciclo, enquanto só existem os 3 desenhos do Bruno (os próprios de cada
        /// caverna são do M10).
        /// </summary>
        static readonly Cave[] Caves =
        {
            new Cave { Name = "Entrada da mina", Ramp = new[] { "#AD7757", "#C09473", "#D7B594", "#E7D5B3" }, WallTint = new Color(1.35f, 1.35f, 1.35f), Fog = "#140D09", LampEvery = 1, Crystal = 0, FloorTiles = "ground_01" },
            new Cave { Name = "Galeria rochosa", Ramp = new[] { "#7A4841", "#AD7757", "#C09473", "#D7B594" }, WallTint = new Color(1.25f, 1.18f, 1.12f), Fog = "#120B09", LampEvery = 1, Crystal = 1 },
            new Cave { Name = "Veio profundo", Ramp = new[] { "#4D2B32", "#7A4841", "#AD7757", "#C09473" }, WallTint = new Color(1.15f, 1.05f, 1f), Fog = "#0F090A", LampEvery = 1, Crystal = 2 },
            new Cave { Name = "Zona metálica", Ramp = new[] { "#241527", "#341C27", "#7A4841", "#AD7757" }, WallTint = new Color(1f, 0.92f, 0.98f), Fog = "#0B060B", LampEvery = 1, Crystal = 0 },
            new Cave { Name = "Câmara subterrânea", Ramp = new[] { "#10201C", "#1C342C", "#2E4E40", "#4F7560" }, WallTint = new Color(0.85f, 1.05f, 0.95f), Fog = "#06100C", LampEvery = 1, Crystal = 1 },
            new Cave { Name = "Fenda cristalina", Ramp = new[] { "#1E1530", "#2E2047", "#463466", "#6A5590" }, WallTint = new Color(0.95f, 0.88f, 1.15f), Fog = "#0C0816", LampEvery = 1, Crystal = 2 },
            new Cave { Name = "Profundidade térmica", Ramp = new[] { "#2A120E", "#4A2016", "#723622", "#9C5A36" }, WallTint = new Color(1.15f, 0.85f, 0.75f), Fog = "#140806", LampEvery = 1, Crystal = 0 },
            new Cave { Name = "Abismo mineral", Ramp = new[] { "#0E0B14", "#1A1524", "#2C2440", "#4A3E66" }, WallTint = new Color(0.8f, 0.75f, 0.95f), Fog = "#07050B", LampEvery = 1, Crystal = 1 },
            new Cave { Name = "Núcleo profundo", Ramp = new[] { "#090B14", "#121628", "#1E2440", "#343E66" }, WallTint = new Color(0.7f, 0.78f, 1f), Fog = "#04060E", LampEvery = 1, Crystal = 2 },
            new Cave { Name = "Coração do vulcão", Ramp = new[] { "#1A0E0C", "#3A1A12", "#7A2E14", "#E0662A" }, WallTint = new Color(1.3f, 0.8f, 0.6f), Fog = "#1C0804", LampEvery = 1, Crystal = 0 },
        };

        static string FloorPath(int cave) => $"{FloorFolder}/floor_cave_{cave + 1:00}.png";
        static string FloorTilePath(string set, int tile) => $"{GroundFolder}/{set}{(char)('a' + tile)}.png";
        static string BiomePath(int cave) => $"{DataFolder}/Caverna_{cave + 1:00}.asset";

        public static void EnsureAssets()
        {
            if (File.Exists(LampSupportPath) && CorridorArtSetup.Configure(LampSupportPath, TextureWrapMode.Clamp) > 0)
                Debug.Log("[Setup] Esteio com lampião configurado (pixel art).");

            var basePath = $"{TileFolder}/{BaseTile}.png";
            if (!File.Exists(basePath)) return;

            var baseTile = Load(basePath);
            var decor = new List<Color32[]>();
            foreach (var name in DecorTiles)
                if (File.Exists($"{TileFolder}/{name}.png")) decor.Add(Load($"{TileFolder}/{name}.png"));

            ProjectSetup.EnsureFolder(FloorFolder);
            int written = 0;
            for (int c = 0; c < Caves.Length; c++)
            {
                var png = Floor(baseTile, decor, Caves[c].Ramp, seed: c + 1);
                var path = FloorPath(c);
                if (File.Exists(path) && SameBytes(File.ReadAllBytes(path), png)) continue;
                File.WriteAllBytes(path, png);
                AssetDatabase.ImportAsset(path);
                written++;
            }
            int configured = 0;
            for (int c = 0; c < Caves.Length; c++) configured += CorridorArtSetup.Configure(FloorPath(c), TextureWrapMode.Repeat);
            if (written > 0) Debug.Log($"[Setup] Chãos das cavernas gerados em {FloorFolder}: {written} (D-102).");
            if (configured > 0) Debug.Log($"[Setup] Chãos das cavernas configurados (pixel art): {configured}.");

            ProjectSetup.EnsureFolder(DataFolder);
            int created = 0;
            for (int c = 0; c < Caves.Length; c++)
            {
                if (File.Exists(BiomePath(c))) continue;
                var biome = ScriptableObject.CreateInstance<BiomeDefinition>();
                var so = new SerializedObject(biome);
                so.FindProperty("displayName").stringValue = Caves[c].Name;
                so.FindProperty("floorTexture").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(FloorPath(c));
                so.FindProperty("wallTint").colorValue = Caves[c].WallTint;
                so.FindProperty("fogColor").colorValue = Hex(Caves[c].Fog);
                so.FindProperty("lampEvery").intValue = Caves[c].LampEvery;
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(biome, BiomePath(c));
                created++;
            }
            if (created > 0) Debug.Log($"[Setup] Biomas das cavernas criados em {DataFolder}: {created} (M1b E8, D-102).");

            int crystals = 0;
            for (int c = 0; c < Caves.Length; c++) crystals += EnsureCrystal(c) ? 1 : 0;
            if (crystals > 0) Debug.Log($"[Setup] Cristal de uma cor por caverna em {crystals} biomas (D-116).");

            int tiled = 0;
            for (int c = 0; c < Caves.Length; c++) tiled += EnsureFloorTiles(c) ? 1 : 0;
            if (tiled > 0) Debug.Log($"[Setup] Chão em faixa de tiles em {tiled} bioma(s) (D-145).");
        }

        /// <summary>
        /// Os 5 tiles do chão em faixa no bioma (D-145), importados como pixel art e legíveis (a faixa é montada no jogo, na
        /// largura do corredor). Só preenche um bioma sem tiles: o que foi posto à mão fica. Devolve se mudou o bioma.
        /// </summary>
        static bool EnsureFloorTiles(int cave)
        {
            var set = Caves[cave].FloorTiles;
            if (string.IsNullOrEmpty(set)) return false;
            var tiles = new Texture2D[DescentArt.FloorStripTiles];
            int configured = 0;
            for (int t = 0; t < tiles.Length; t++)
            {
                var path = FloorTilePath(set, t);
                if (!File.Exists(path)) return false;
                configured += ConfigureReadable(path);
                tiles[t] = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tiles[t] == null) return false;
            }
            if (configured > 0) Debug.Log($"[Setup] Tiles {set}a…e configurados (pixel art, legíveis): {configured}.");

            var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(BiomePath(cave));
            if (biome == null || biome.FloorTiles is { Length: > 0 }) return false;

            var so = new SerializedObject(biome);
            var property = so.FindProperty("floorTiles");
            property.arraySize = tiles.Length;
            for (int t = 0; t < tiles.Length; t++) property.GetArrayElementAtIndex(t).objectReferenceValue = tiles[t];
            so.FindProperty("edgeRocks").boolValue = false; // a faixa já tem a beira desenhada: sem pedras por cima
            so.FindProperty("edgeMound").boolValue = true;  // e a beira sobe em montinho de areia (D-146)
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(biome);
            return true;
        }

        /// <summary>Como os chãos (pixel art, sem compressão nem mipmaps), e legível. Devolve 1 se mudou.</summary>
        static int ConfigureReadable(string path)
        {
            int changed = CorridorArtSetup.Configure(path, TextureWrapMode.Clamp);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer && !importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
                changed = 1;
            }
            return changed;
        }

        /// <summary>
        /// O cristal da caverna, só se o bioma está sem nenhum (um posto à mão no asset fica). Devolve se mudou.
        /// </summary>
        static bool EnsureCrystal(int cave)
        {
            var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(BiomePath(cave));
            var (name, shards) = CrystalArtSetup.QueueCrystals[Caves[cave].Crystal];
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{CrystalArtSetup.Folder}/{name}.png");
            if (biome == null || biome.CrystalTexture != null || texture == null) return false;

            var so = new SerializedObject(biome);
            so.FindProperty("crystalTexture").objectReferenceValue = texture;
            so.FindProperty("crystalShardColor").colorValue = shards;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(biome);
            return true;
        }

        /// <summary>No DescentStage da cena Game (já aberta): os biomas, o esteio com lampião e o botão do DBG.</summary>
        public static bool EnsureInScene()
        {
            var stage = Object.FindAnyObjectByType<DescentStage>();
            if (stage == null) return false;

            var so = new SerializedObject(stage);
            bool changed = false;

            var biomes = so.FindProperty("biomes");
            if (biomes.arraySize == 0 && File.Exists(BiomePath(0)))
            {
                biomes.arraySize = Caves.Length;
                for (int c = 0; c < Caves.Length; c++)
                    biomes.GetArrayElementAtIndex(c).objectReferenceValue = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(BiomePath(c));
                Debug.Log($"[Setup] Biomas das {Caves.Length} cavernas no DescentStage (M1b E8, D-102).");
                changed = true;
            }

            var lamp = so.FindProperty("supportLampTexture");
            if (lamp.objectReferenceValue == null && File.Exists(LampSupportPath))
            {
                lamp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(LampSupportPath);
                Debug.Log("[Setup] Esteio com lampião (mine_wood_lamp) no DescentStage (D-102).");
                changed = true;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            changed |= EnsureBiomeButton();
            changed |= EnsureMoundButton();
            return changed;
        }

        const string MoundButton = "Mound";

        /// <summary>
        /// Na ordem do <c>DescentStage.MoundLook</c>: chão plano, arco (∩), platô. Com "Monte" no nome: só "Arco" não dizia
        /// de que era o botão.
        /// </summary>
        static readonly string[] MoundLabels = { "Sem monte", "Monte arco", "Monte platô" };

        /// <summary>O jogo abre no arco (o moundLook padrão do DescentStage).</summary>
        const int DefaultMound = 1;

        /// <summary>O botão "Monte" do DBG → Câmera (D-146): chão plano, arco (∩) e platô, para comparar.</summary>
        static bool EnsureMoundButton() =>
            DescentSetup.EnsureCameraCycleButton(MoundButton, "mound", "moundLabels", MoundLabels, DefaultMound,
                                                 "Botão \"Monte\" no DBG → Câmera (montinho de areia das beiras, D-146).");

        /// <summary>O botão "Chão" da E9 vira o "Bioma" (mesmo objeto, a referência do painel continua); sem ele, cria.</summary>
        static bool EnsureBiomeButton()
        {
            var panel = Object.FindAnyObjectByType<MinersIdle.UI.DescentCameraPanel>(FindObjectsInactive.Include);
            var buttons = panel != null ? panel.transform.Find("Content/Buttons") : null;
            if (buttons == null || buttons.Find(BiomeButton) != null) return false;

            var old = buttons.Find(OldFloorButton);
            if (old != null)
            {
                old.name = BiomeButton;
                var text = old.GetComponentInChildren<TMP_Text>();
                if (text != null)
                {
                    text.text = BiomeButtonLabel;
                    EditorUtility.SetDirty(text);
                }
                EditorUtility.SetDirty(old.gameObject);
                Debug.Log("[Setup] Botão \"Chão\" do DBG → Câmera vira \"Bioma\" (M1b E8).");
                return true;
            }

            var button = DebugPanelSetup.CreateButton(buttons, BiomeButton, BiomeButtonLabel);
            GrayboxSetup.SetReferences(panel, ("biome", button));
            Debug.Log("[Setup] Botão \"Bioma\" no DBG → Câmera (M1b E8).");
            return true;
        }

        // ── Geração ──────────────────────────────────────────────────────────────────────────────────────────────────

        /// <summary>O chão de uma caverna: o tile em grade, alguns trocados por pedra ou buraco, na paleta da caverna.</summary>
        static byte[] Floor(Color32[] baseTile, List<Color32[]> decor, string[] ramp, int seed)
        {
            var swap = new Dictionary<int, Color32>();
            for (int i = 0; i < BaseRamp.Length; i++) swap[Rgb(Hex(BaseRamp[i]))] = Hex(ramp[i]);

            var random = new System.Random(seed);
            int size = TilePixels * GridTiles;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int gy = 0; gy < GridTiles; gy++)
            for (int gx = 0; gx < GridTiles; gx++)
            {
                var tile = decor.Count > 0 && random.NextDouble() < DecorChance ? decor[random.Next(decor.Count)] : baseTile;
                var pixels = new Color32[tile.Length];
                for (int p = 0; p < tile.Length; p++)
                    pixels[p] = tile[p].a == 255 && swap.TryGetValue(Rgb(tile[p]), out var to) ? to : tile[p];
                texture.SetPixels32(gx * TilePixels, gy * TilePixels, TilePixels, TilePixels, pixels);
            }
            var png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            return png;
        }

        static int Rgb(Color32 c) => c.r << 16 | c.g << 8 | c.b;

        static Color32[] Load(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(path));
            var pixels = texture.GetPixels32();
            Object.DestroyImmediate(texture);
            return pixels;
        }

        static Color32 Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var color);
            return color;
        }

        static bool SameBytes(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;
            return true;
        }
    }
}
