using System.IO;
using MinersIdle.Presentation;
using UnityEditor;
using UnityEngine;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="ProjectSetup"/> para a E6 do M1b (D-75): o anão do jogo são as folhas do PixelLab do Bruno
    /// (<c>dwarf-walking</c>, 3×3 quadros de 256 px; <c>dwarf-mining</c>, 4×4 com 13 quadros), tocadas inteiras pelo
    /// <see cref="DescentStage"/>: corrida em ciclo, golpe com o impacto no instante do dano. Importa as folhas como pixel
    /// art e preenche a cena. Idempotente; não sobrescreve o que foi posto à mão.
    /// </summary>
    static class DwarfArtSetup
    {
        const string Folder = "Assets/_Project/Art/Character";
        const string RunPath = Folder + "/dwarf-walking.png";
        const string MinePath = Folder + "/dwarf-mining.png";

        /// <summary>
        /// No quadro de 256 px o anão parado ocupa da linha 49 à 223 (174 px) e precisa medir
        /// <see cref="DescentFraming.DwarfHeight"/> (1,32 m): o quadro inteiro mede 1,94 m, e 33 px (13%) ficam abaixo do pé.
        /// </summary>
        const float FramePixels = 256f, BodyPixels = 174f, FeetRow = 223f;

        public static void EnsureAssets()
        {
            int configured = Configure(RunPath) + Configure(MinePath);
            if (configured > 0) Debug.Log($"[Setup] Folhas do anão configuradas (pixel art): {configured}.");
        }

        /// <summary>O ciclo da corrida é só a passada (3–8): o 1 é a pose parada e o 2, a saída dela (davam a "pausinha").</summary>
        static readonly int[] RunCycle = { 2, 3, 4, 6, 7, 8 };

        public static bool EnsureInScene()
        {
            var stage = Object.FindAnyObjectByType<DescentStage>();
            if (stage == null || !File.Exists(RunPath) || !File.Exists(MinePath)) return false;

            var so = new SerializedObject(stage);
            var run = so.FindProperty("dwarfRunSheet");
            if (run.objectReferenceValue != null) return FixRunCycle(so);

            run.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(RunPath);
            so.FindProperty("dwarfMineSheet").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(MinePath);
            float size = DescentFraming.DwarfHeight * FramePixels / BodyPixels;
            so.FindProperty("dwarfSize").vector2Value = new Vector2(size, size);
            so.FindProperty("dwarfFootGap").floatValue = (FramePixels - FeetRow) / FramePixels;
            so.FindProperty("runBounceHeight").floatValue = 0f; // a corrida do PixelLab já tem o balanço
            so.FindProperty("runAnimationFps").floatValue = 10f;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Setup] Anão com as folhas do PixelLab (corrida e mineração; M1b E6, D-75).");
            return true;
        }

        /// <summary>Cena com o ciclo antigo (1–8, passava pela pose parada): troca pelo da passada. Devolve se mudou.</summary>
        static bool FixRunCycle(SerializedObject so)
        {
            var cycle = so.FindProperty("dwarfRunCycle");
            bool same = cycle.arraySize == RunCycle.Length;
            for (int i = 0; same && i < RunCycle.Length; i++) same = cycle.GetArrayElementAtIndex(i).intValue == RunCycle[i];
            if (same) return false;
            cycle.arraySize = RunCycle.Length;
            for (int i = 0; i < RunCycle.Length; i++) cycle.GetArrayElementAtIndex(i).intValue = RunCycle[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Setup] Corrida do anão com as duas pernas iguais (2-3-4 e 6-7-8), sem a pose parada.");
            return true;
        }

        internal static int Configure(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return 0;
            if (importer.textureType == TextureImporterType.Default && importer.filterMode == FilterMode.Point
                && importer.textureCompression == TextureImporterCompression.Uncompressed && !importer.mipmapEnabled
                && importer.wrapMode == TextureWrapMode.Clamp && importer.alphaIsTransparency
                && importer.npotScale == TextureImporterNPOTScale.None && importer.maxTextureSize >= 1024)
                return 0;

            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = Mathf.Max(importer.maxTextureSize, 1024);
            importer.SaveAndReimport();
            return 1;
        }
    }
}
