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
    /// art e preenche a cena. Também liga as folhas com menos pixels (<c>low-dwarf-*</c>, quadros de 64 px, mesma grade),
    /// que o DBG → Câmera → "Anão 64px" troca na hora para comparar. Idempotente; não sobrescreve o que foi posto à mão.
    /// </summary>
    static class DwarfArtSetup
    {
        const string Folder = "Assets/_Project/Art/Character";
        const string RunPath = Folder + "/dwarf-walking.png";
        const string MinePath = Folder + "/dwarf-mining.png";
        const string LowRunPath = Folder + "/low-dwarf-walking.png";
        const string LowMinePath = Folder + "/low-dwarf-mining.png";

        internal const string LowDwarfButton = "LowDwarf";
        internal const string LowDwarfLabel = "Anão 64px";

        /// <summary>
        /// No quadro de 256 px o anão parado ocupa da linha 49 à 223 (174 px) e precisa medir
        /// <see cref="DescentFraming.DwarfHeight"/> (1,32 m): o quadro inteiro mede 1,94 m, e 33 px (13%) ficam abaixo do pé.
        /// </summary>
        const float FramePixels = 256f, BodyPixels = 174f, FeetRow = 223f;

        /// <summary>No quadro de 64 px o anão enche mais: da linha 1 à 62 (61 px), e só 2 px ficam abaixo do pé.</summary>
        const float LowFramePixels = 64f, LowBodyPixels = 61f, LowFeetRow = 62f;

        public static void EnsureAssets()
        {
            int configured = Configure(RunPath) + Configure(MinePath) + Configure(LowRunPath) + Configure(LowMinePath);
            if (configured > 0) Debug.Log($"[Setup] Folhas do anão configuradas (pixel art): {configured}.");
        }

        /// <summary>O ciclo da corrida é só a passada (3–8): o 1 é a pose parada e o 2, a saída dela (davam a "pausinha").</summary>
        static readonly int[] RunCycle = { 2, 3, 4, 6, 7, 8 };

        public static bool EnsureInScene()
        {
            var stage = Object.FindAnyObjectByType<DescentStage>();
            if (stage == null) return false;

            var so = new SerializedObject(stage);
            bool changed = EnsureSheets(so);
            changed |= EnsureLowSheets(so);
            changed |= EnsureLowDwarfButton();
            return changed;
        }

        static bool EnsureSheets(SerializedObject so)
        {
            if (!File.Exists(RunPath) || !File.Exists(MinePath)) return false;
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

        /// <summary>As folhas com menos pixels, com o tamanho que deixa o anão da mesma altura e o pé no mesmo lugar.</summary>
        static bool EnsureLowSheets(SerializedObject so)
        {
            if (!File.Exists(LowRunPath) || !File.Exists(LowMinePath)) return false;
            var run = so.FindProperty("lowDwarfRunSheet");
            if (run.objectReferenceValue != null) return false;

            run.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(LowRunPath);
            so.FindProperty("lowDwarfMineSheet").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(LowMinePath);
            float size = DescentFraming.DwarfHeight * LowFramePixels / LowBodyPixels;
            so.FindProperty("lowDwarfSize").vector2Value = new Vector2(size, size);
            so.FindProperty("lowDwarfFootGap").floatValue = (LowFramePixels - LowFeetRow) / LowFramePixels;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Setup] Anão com menos pixels ligado ao DBG → Câmera → Anão 64px.");
            return true;
        }

        static bool EnsureLowDwarfButton()
        {
            var panel = Object.FindAnyObjectByType<MinersIdle.UI.DescentCameraPanel>(FindObjectsInactive.Include);
            var buttons = panel != null ? panel.transform.Find("Content/Buttons") : null;
            if (buttons == null || buttons.Find(LowDwarfButton) != null) return false;

            var button = DebugPanelSetup.CreateButton(buttons, LowDwarfButton, LowDwarfLabel);
            GrayboxSetup.SetReferences(panel, ("lowDwarf", button));
            Debug.Log("[Setup] Botão \"Anão 64px\" no DBG → Câmera.");
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
