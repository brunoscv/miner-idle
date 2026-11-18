using System.IO;
using MinersIdle.Presentation;
using MinersIdle.UI;
using UnityEditor;
using UnityEngine;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="ProjectSetup"/> para o M9f (cristal rachando): a folha dos cristais do v8 do Bruno
    /// (<c>crystals_v8.png</c>, 4 colunas = inteiro e 3 rachaduras, 5 linhas = azul, amarelo, verde, anil, vermelho, quadros
    /// de 32 px) importada como pixel art; a linha de cada bioma pelo contraste com o fundo (D-299), com a cor dos estilhaços
    /// tirada do desenho; a linha dourada do raro gerada a partir da azul (D-302), em arquivo à parte; e, na cena, o raro
    /// dourado e os botões "Barra de vida" e "Estágios" do DBG → Andar. Só preenche o que falta: uma linha posta à mão no
    /// bioma fica.
    /// </summary>
    static class CrystalSheetSetup
    {
        internal const string SheetPath = CrystalArtSetup.Folder + "/crystals_v8.png";
        internal const string RarePath = CrystalArtSetup.Folder + "/crystal_v8_rare.png";
        const int Frame = 32;

        /// <summary>A linha da folha de cada caverna (D-299): 0 azul, 1 amarelo, 2 verde, 3 anil, 4 vermelho.</summary>
        static readonly int[] RowOfCave = { 0, 2, 3, 1, 4, 1, 0, 2, 4, 3 };

        /// <summary>O dourado do raro (D-302): o matiz da rampa e a cor dos estilhaços.</summary>
        static readonly Color32 Gold = new Color32(0xF2, 0xC1, 0x4E, 0xFF);

        public static void EnsureAssets()
        {
            if (!File.Exists(SheetPath)) return;
            int configured = CrystalArtSetup.Configure(SheetPath) + KeepSize(SheetPath);

            var sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            sheet.LoadImage(File.ReadAllBytes(SheetPath));
            try
            {
                if (!File.Exists(RarePath))
                {
                    var strip = new Texture2D(sheet.width, Frame, TextureFormat.RGBA32, false);
                    strip.SetPixels(sheet.GetPixels(0, sheet.height - Frame, sheet.width, Frame)); // a linha de cima, a azul
                    var gold = CrystalArtSetup.Ramp(strip, Gold);
                    File.WriteAllBytes(RarePath, gold.EncodeToPNG());
                    Object.DestroyImmediate(strip);
                    Object.DestroyImmediate(gold);
                    AssetDatabase.ImportAsset(RarePath);
                    Debug.Log($"[Setup] Linha dourada do raro gerada da azul em {RarePath} (M9f, D-302).");
                }
                configured += CrystalArtSetup.Configure(RarePath);
                if (configured > 0) Debug.Log($"[Setup] Folha dos cristais do v8 configurada (pixel art): {configured}.");

                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(SheetPath);
                int rows = sheet.height / Frame, assigned = 0;
                for (int cave = 0; cave < BiomeArtSetup.CaveCount && cave < RowOfCave.Length; cave++)
                {
                    var biome = AssetDatabase.LoadAssetAtPath<BiomeDefinition>(BiomeArtSetup.BiomePath(cave));
                    if (biome == null || biome.CrystalSheet != null || texture == null || RowOfCave[cave] >= rows) continue;
                    var so = new SerializedObject(biome);
                    so.FindProperty("crystalSheet").objectReferenceValue = texture;
                    so.FindProperty("crystalRow").intValue = RowOfCave[cave];
                    so.FindProperty("crystalShardColor").colorValue = ShardColor(sheet, RowOfCave[cave]);
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(biome);
                    assigned++;
                }
                if (assigned > 0)
                {
                    AssetDatabase.SaveAssets();
                    Debug.Log($"[Setup] Cristal do v8 (folha rachando) em {assigned} biomas (M9f, D-299).");
                }
            }
            finally
            {
                Object.DestroyImmediate(sheet);
            }
        }

        /// <summary>
        /// A folha tem 128 × 160 (5 linhas), fora da potência de 2: sem isto o Unity a reduz para 128 × 128 e os quadros
        /// esticam e a última linha some. Uma 6ª linha (192) também não é potência de 2.
        /// </summary>
        static int KeepSize(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer) || importer.npotScale == TextureImporterNPOTScale.None)
                return 0;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
            return 1;
        }

        /// <summary>
        /// A cor dos estilhaços de uma linha: a média da metade mais clara dos pixels do cristal inteiro (fora o contorno),
        /// para o estilhaço ler como o corpo do cristal e não como a sombra.
        /// </summary>
        static Color ShardColor(Texture2D sheet, int row)
        {
            var pixels = sheet.GetPixels(0, sheet.height - (row + 1) * Frame, Frame, Frame);
            var values = new System.Collections.Generic.List<(float v, Color c)>();
            foreach (var p in pixels)
            {
                if (p.a < 0.5f) continue;
                Color.RGBToHSV(p, out _, out _, out float v);
                if (v >= 0.18f) values.Add((v, p));
            }
            if (values.Count == 0) return Color.white;
            values.Sort((a, b) => b.v.CompareTo(a.v));
            int count = Mathf.Max(1, values.Count / 2);
            Color sum = Color.clear;
            for (int i = 0; i < count; i++) sum += values[i].c;
            var mean = sum / count;
            mean.a = 1f;
            return mean;
        }

        const string HealthBarButton = "CrystalHealthBar";
        const string StagesButton = "CrystalStages";

        /// <summary>Na cena Game (já aberta): o raro dourado no DescentStage e os botões do DBG → Andar.</summary>
        public static bool EnsureInScene()
        {
            var stage = Object.FindAnyObjectByType<DescentStage>(FindObjectsInactive.Include);
            if (stage == null) return false;
            bool changed = false;

            var rare = AssetDatabase.LoadAssetAtPath<Texture2D>(RarePath);
            var so = new SerializedObject(stage);
            var field = so.FindProperty("rareCrystalSheet");
            if (rare != null && field.objectReferenceValue == null)
            {
                field.objectReferenceValue = rare;
                so.FindProperty("rareShardColor").colorValue = Gold;
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[Setup] Cristal raro dourado, rachando, na fila (M9f, D-302).");
                changed = true;
            }

            var debugPanel = Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include);
            var menu = debugPanel != null ? debugPanel.transform.Find("CaveMenu") : null;
            var grid = menu != null ? menu.Find("Content/Buttons") : null;
            if (grid == null || menu.GetComponent<CrystalCrackDebug>() != null) return changed;

            var bar = Button(grid, HealthBarButton, "Barra de vida: não");
            var stages = Button(grid, StagesButton, "Estágios: não");
            grid.Find("Close")?.SetAsLastSibling();
            var debug = menu.gameObject.AddComponent<CrystalCrackDebug>();
            GrayboxSetup.SetReferences(debug, ("stage", stage), ("healthBarToggle", bar), ("stagesToggle", stages));
            Debug.Log("[Setup] Painel de debug: \"Barra de vida\" e \"Estágios\" no menu Andar (M9f).");
            return true;
        }

        static UnityEngine.UI.Button Button(Transform grid, string name, string label)
        {
            var existing = grid.Find(name);
            return existing != null ? existing.GetComponent<UnityEngine.UI.Button>()
                                    : DebugPanelSetup.CreateButton(grid, name, label);
        }
    }
}
