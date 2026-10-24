using System.IO;
using MinersIdle.Presentation;
using UnityEditor;
using UnityEngine;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="ProjectSetup"/> para a E5 do M1b (D-70): o cristal da fila é o <c>icon_crystal</c> do Bruno em
    /// 5 cores, na ordem de <see cref="DescentArt.CrystalColors"/> (a mesma dos estilhaços da quebra). Cada cor é uma rampa
    /// de um matiz só que mantém o claro/escuro do desenho (girar o matiz embaralhava o rosa e o ciano do original). Os
    /// PNGs são arte do projeto e vão no git. Idempotente; não sobrescreve o que foi posto à mão na cena.
    /// </summary>
    static class CrystalArtSetup
    {
        const string SourcePath = "Assets/_Project/Art/Crystals/icon_crystal.png";
        const string Folder = "Assets/_Project/Art/Crystals";
        static readonly string[] Names = { "white", "gold", "blue", "purple", "red" };

        /// <summary>Tamanho do cristal na fila, em metros (o das prévias aprovadas).</summary>
        static readonly Vector2 CrystalSize = new Vector2(0.7f, 0.7f);

        static string PathOf(int color) => $"{Folder}/crystal_{Names[color]}.png";

        public static void EnsureAssets()
        {
            if (!File.Exists(SourcePath)) return;

            Texture2D source = null;
            int made = 0;
            for (int c = 0; c < DescentArt.CrystalColorCount; c++)
            {
                if (File.Exists(PathOf(c))) continue;
                if (source == null)
                {
                    source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    source.LoadImage(File.ReadAllBytes(SourcePath));
                }
                File.WriteAllBytes(PathOf(c), Ramp(source, DescentArt.CrystalColors[c, 0]).EncodeToPNG());
                AssetDatabase.ImportAsset(PathOf(c));
                made++;
            }
            if (source != null) Object.DestroyImmediate(source);

            int configured = 0;
            for (int c = 0; c < DescentArt.CrystalColorCount; c++) configured += Configure(PathOf(c));
            foreach (var name in BrunoCrystals)
                if (File.Exists($"{Folder}/{name}.png")) configured += Configure($"{Folder}/{name}.png");
            if (made > 0) Debug.Log($"[Setup] Cristais da fila em 5 cores gerados em {Folder} (D-70).");
            if (configured > 0) Debug.Log($"[Setup] Cristais da fila configurados (pixel art): {configured}.");
        }

        /// <summary>
        /// Os cristais desenhados pelo Bruno (D-77), em revezamento na fila, com a cor dos estilhaços de cada um. O raro
        /// (<c>crystal_glass_rare</c>) fica guardado para um futuro cristal raro (regra nova, precisa de decisão).
        /// </summary>
        static readonly (string name, Color32 shards)[] QueueCrystals =
        {
            ("crystal_glass", new Color32(0xD8, 0xE2, 0xDF, 0xFF)),
            ("crystal_sapphire", new Color32(0x6A, 0xA8, 0xDA, 0xFF)),
            ("crystal_yellow", new Color32(0xF0, 0xB3, 0x5A, 0xFF)),
        };
        static readonly string[] BrunoCrystals = { "crystal_glass", "crystal_glass_rare", "crystal_sapphire", "crystal_yellow" };

        /// <summary>
        /// No DescentStage da cena Game (já aberta): os cristais do Bruno na fila e o tamanho. Troca também a lista das 5
        /// cores geradas da E5 (D-70), que eles substituem. Devolve se mudou a cena.
        /// </summary>
        public static bool EnsureInScene()
        {
            var stage = Object.FindAnyObjectByType<DescentStage>();
            if (stage == null || !File.Exists($"{Folder}/{QueueCrystals[0].name}.png")) return false;

            var so = new SerializedObject(stage);
            var textures = so.FindProperty("crystalTextures");
            var first = textures.arraySize > 0 ? textures.GetArrayElementAtIndex(0).objectReferenceValue : null;
            bool generated = first != null && first.name.StartsWith("crystal_white");
            if (textures.arraySize > 0 && !generated) return false;

            var shards = so.FindProperty("crystalShardColors");
            textures.arraySize = shards.arraySize = QueueCrystals.Length;
            for (int i = 0; i < QueueCrystals.Length; i++)
            {
                textures.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Texture2D>($"{Folder}/{QueueCrystals[i].name}.png");
                shards.GetArrayElementAtIndex(i).colorValue = QueueCrystals[i].shards;
            }
            so.FindProperty("crystalSize").vector2Value = CrystalSize;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Setup] Fila com os cristais do Bruno: glass, sapphire, yellow (D-77).");
            return true;
        }

        /// <summary>
        /// Rampa de uma cor: o contorno escuro fica; o resto ganha o matiz e a saturação da cor, com o brilho do original
        /// (os brilhos mais fortes perdem saturação, como reflexo).
        /// </summary>
        static Texture2D Ramp(Texture2D source, Color32 target)
        {
            Color.RGBToHSV(target, out float hue, out float saturation, out _);
            var pixels = source.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                if (p.a == 0) continue;
                Color.RGBToHSV(p, out _, out _, out float v);
                if (v < 0.18f) continue; // contorno
                Color c = Color.HSVToRGB(hue, Mathf.Lerp(saturation, 0f, Mathf.Pow(v, 4f)), Mathf.Lerp(0.25f, 1f, v));
                pixels[i] = new Color32((byte)(c.r * 255f), (byte)(c.g * 255f), (byte)(c.b * 255f), p.a);
            }
            var result = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            result.SetPixels32(pixels);
            return result;
        }

        static int Configure(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return 0;
            if (importer.textureType == TextureImporterType.Default && importer.filterMode == FilterMode.Point
                && importer.textureCompression == TextureImporterCompression.Uncompressed && !importer.mipmapEnabled
                && importer.wrapMode == TextureWrapMode.Clamp && importer.alphaIsTransparency)
                return 0;

            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return 1;
        }
    }
}
