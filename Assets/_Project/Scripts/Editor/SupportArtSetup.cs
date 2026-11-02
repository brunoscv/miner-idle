using System.IO;
using System.Linq;
using MinersIdle.Presentation;
using UnityEditor;
using UnityEngine;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Prévia do esteio baixo (pedido do Bruno em 2026-10-31): o <c>mine_wood</c> de 4,5 m tem a viga acima da câmera (3,6 m),
    /// então ela nunca aparece, e o lampião desenhado nele fica com ~1 m. Aqui o setup gera o mesmo esteio mais baixo,
    /// tirando linhas do meio dos postes (sem esticar pixel: a viga e o pé ficam iguais), e põe no <see cref="DescentStage"/>
    /// as opções e o lampião solto (<c>lamp_16x16</c>), que vai preso no poste num tamanho fixo. O botão "Esteio" do
    /// DBG → Câmera passa por elas. O padrão é o de 3,0 m, com um lampião em cada esteio (D-117).
    /// </summary>
    static class SupportArtSetup
    {
        const string Folder = "Assets/_Project/Art/Corridor";
        const string SourcePath = Folder + "/mine_wood.png";
        const string LampPath = Folder + "/lamp_16x16.png";

        /// <summary>A altura do esteio de hoje (o <c>supportSize</c>), que dá os pixels por metro na vertical.</summary>
        const float SourceMeters = 4.5f;
        /// <summary>Linhas de cima que ficam inteiras (a viga e o começo dos postes); o corte é logo abaixo delas.</summary>
        const int KeepTop = 24;

        /// <summary>As alturas da prévia, em metros (a de hoje é a primeira opção, fora desta lista).</summary>
        static readonly float[] Heights = { 3.0f, 2.8f, 2.4f };
        /// <summary>A altura escolhida pelo Bruno no Editor em 2026-11-01 (D-117): o padrão do jogo.</summary>
        const float ChosenHeight = 3.0f;

        const string SupportButton = "Support";
        static readonly string[] SupportLabels = { "Esteio 4,5", "Esteio 3,0", "Esteio 2,8", "Esteio 2,4" };

        static string PathOf(float height) => $"{Folder}/mine_wood_h{Mathf.RoundToInt(height * 10f)}.png";

        public static void EnsureAssets()
        {
            if (!File.Exists(SourcePath)) return;

            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(File.ReadAllBytes(SourcePath));
            int made = 0;
            foreach (var height in Heights)
            {
                var png = Shorter(source, Rows(source, height));
                var path = PathOf(height);
                if (File.Exists(path) && File.ReadAllBytes(path).SequenceEqual(png)) continue;
                File.WriteAllBytes(path, png);
                AssetDatabase.ImportAsset(path);
                made++;
            }
            Object.DestroyImmediate(source);

            int configured = 0;
            foreach (var height in Heights) configured += CorridorArtSetup.Configure(PathOf(height), TextureWrapMode.Clamp);
            if (File.Exists(LampPath)) configured += CorridorArtSetup.Configure(LampPath, TextureWrapMode.Clamp);
            if (made > 0) Debug.Log($"[Setup] Esteios baixos gerados em {Folder}: {made} (prévia do esteio).");
            if (configured > 0) Debug.Log($"[Setup] Esteios baixos e lampião configurados (pixel art): {configured}.");
        }

        /// <summary>Linhas da imagem para a altura dada, na mesma densidade de pixels do esteio de hoje.</summary>
        static int Rows(Texture2D source, float height) => Mathf.RoundToInt(height * source.height / SourceMeters);

        /// <summary>O esteio com <paramref name="rows"/> linhas: as de cima e as de baixo ficam, as do meio dos postes saem.</summary>
        static byte[] Shorter(Texture2D source, int rows)
        {
            var pixels = source.GetPixels32();
            int width = source.width;
            var result = new Color32[width * rows];
            int bottom = rows - KeepTop; // linhas de baixo (o Texture2D conta de baixo para cima)
            for (int y = 0; y < rows; y++)
            {
                int from = y < bottom ? y : source.height - rows + y;
                System.Array.Copy(pixels, from * width, result, y * width, width);
            }
            var texture = new Texture2D(width, rows, TextureFormat.RGBA32, false);
            texture.SetPixels32(result);
            var png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            return png;
        }

        /// <summary>No DescentStage da cena Game (já aberta): as opções do esteio, o lampião e o botão do DBG.</summary>
        public static bool EnsureInScene()
        {
            var stage = Object.FindAnyObjectByType<DescentStage>();
            if (stage == null || !File.Exists(SourcePath)) return false;

            var so = new SerializedObject(stage);
            bool changed = false;
            var options = so.FindProperty("supportOptions");
            if (options.arraySize != Heights.Length)
            {
                var source = AssetDatabase.LoadAssetAtPath<Texture2D>(SourcePath);
                options.arraySize = Heights.Length;
                for (int i = 0; i < Heights.Length; i++)
                {
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(PathOf(Heights[i]));
                    var option = options.GetArrayElementAtIndex(i);
                    option.FindPropertyRelative("texture").objectReferenceValue = texture;
                    // A altura exata das linhas que ficaram (mesmos pixels por metro do esteio de hoje).
                    option.FindPropertyRelative("height").floatValue =
                        texture != null ? texture.height * SourceMeters / source.height : Heights[i];
                }
                changed = true;
            }
            var lamp = so.FindProperty("lampTexture");
            if (lamp.objectReferenceValue == null && File.Exists(LampPath))
            {
                lamp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(LampPath);
                changed = true;
            }
            if (changed) Debug.Log("[Setup] Opções do esteio (4,5 / 3,0 / 2,8 / 2,4 m) e lampião solto no DescentStage.");

            // O esteio escolhido pelo Bruno (D-117): o jogo abre nele; o DBG ainda passa pelos outros para comparar.
            var look = so.FindProperty("supportLook");
            int chosen = System.Array.IndexOf(Heights, ChosenHeight) + 1;
            if (look.intValue != chosen)
            {
                look.intValue = chosen;
                Debug.Log($"[Setup] Esteio padrão: {ChosenHeight:0.0} m com o lampião solto (D-117).");
                changed = true;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            return EnsureButton() | changed;
        }

        static bool EnsureButton()
        {
            var panel = Object.FindAnyObjectByType<MinersIdle.UI.DescentCameraPanel>(FindObjectsInactive.Include);
            var buttons = panel != null ? panel.transform.Find("Content/Buttons") : null;
            if (buttons == null) return false;

            bool changed = false;
            var button = buttons.Find(SupportButton);
            if (button == null)
            {
                button = DebugPanelSetup.CreateButton(buttons, SupportButton, SupportLabels[0]).transform;
                GrayboxSetup.SetReferences(panel, ("support", button.GetComponent<UnityEngine.UI.Button>()));
                Debug.Log("[Setup] Botão \"Esteio\" no DBG → Câmera (prévia do esteio baixo).");
                changed = true;
            }

            // Os rótulos acompanham a lista acima (cabem nos 82 px do botão).
            var so = new SerializedObject(panel);
            var labels = so.FindProperty("supportLabels");
            bool same = labels.arraySize == SupportLabels.Length;
            for (int i = 0; same && i < SupportLabels.Length; i++) same = labels.GetArrayElementAtIndex(i).stringValue == SupportLabels[i];
            if (same) return changed;

            labels.arraySize = SupportLabels.Length;
            for (int i = 0; i < SupportLabels.Length; i++) labels.GetArrayElementAtIndex(i).stringValue = SupportLabels[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            var text = button.GetComponentInChildren<TMPro.TMP_Text>();
            text.text = SupportLabels[0];
            EditorUtility.SetDirty(text);
            return true;
        }
    }
}
