using System.IO;
using MinersIdle.Game;
using MinersIdle.Presentation;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="ProjectSetup"/> para a cena Game: sprites brancos 16×16 (efeitos e loja), o objeto "Mining"
    /// (a partida), o HUD de cima, a descida (M6, <see cref="DescentSetup"/>), os efeitos, o painel de debug
    /// (<see cref="DebugPanelSetup"/>) e a loja. Como o resto do setup, só cria o que falta. (A vista de cima do mapa,
    /// do M2 ao M6 E2, saiu na E3 do M6, D-61.)
    /// </summary>
    static class GrayboxSetup
    {
        const string SpriteFolder = "Assets/_Project/Art/Graybox";
        internal const string SquarePath = SpriteFolder + "/Square.png";
        internal const string DiamondPath = SpriteFolder + "/Diamond.png";
        internal const string CirclePath = SpriteFolder + "/Circle.png";
        const string MiningRootName = "Mining";
        const string HudName = "Hud";

        const int SpriteSize = 16;
        const float PixelsPerUnit = 16f;
        // HUD: painel de madeira escura do mockup, rótulo âmbar, valor claro.
        static readonly Color HudPanelColor = new Color32(0x2B, 0x1D, 0x14, 0xE6);
        static readonly Color HudLabelColor = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        static readonly Color HudValueColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);

        public static void Ensure()
        {
            var square = EnsureSprite(SquarePath, (x, y) => true);
            EnsureSprite(DiamondPath, (x, y) => Mathf.Abs(x - 7.5f) + Mathf.Abs(y - 7.5f) <= 8f);
            EnsureSprite(CirclePath, (x, y) => (x - 7.5f) * (x - 7.5f) + (y - 7.5f) * (y - 7.5f) <= 64f);
            ShopSetup.EnsureAssets(square);
            FeedbackSetup.EnsureTexts();
            EnsureMiningInGameScene();
        }

        static Sprite EnsureSprite(string path, System.Func<int, int, bool> inside)
        {
            if (!File.Exists(path))
            {
                ProjectSetup.EnsureFolder(SpriteFolder);
                var texture = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false);
                for (int y = 0; y < SpriteSize; y++)
                for (int x = 0; x < SpriteSize; x++)
                    texture.SetPixel(x, y, inside(x, y) ? Color.white : Color.clear);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);

                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = PixelsPerUnit;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
                Debug.Log($"[Setup] Sprite de graybox criado: {path}.");
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void EnsureMiningInGameScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[Setup] Cena Game não alterada: havia mudanças não salvas.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(ProjectSetup.GameScenePath, OpenSceneMode.Single);
            bool changed = false;

            var camera = Camera.main;
            if (camera != null && camera.GetComponent<CameraWidthFit>() == null)
            {
                camera.gameObject.AddComponent<CameraWidthFit>();
                changed = true;
            }

            if (GameObject.Find(MiningRootName) == null)
            {
                var rules = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(ProjectSetup.ActiveBalancePath);
                var root = new GameObject(MiningRootName);
                SetReferences(root.AddComponent<MiningRunner>(), ("rules", rules));

                // O painel âmbar do M1 só mostrava o Safe Area; sobre o jogo ele atrapalha. O componente fica (layout).
                var safeArea = GameObject.Find("SafeArea");
                if (safeArea != null && safeArea.TryGetComponent<Image>(out var tint)) tint.color = Color.clear;

                changed = true;
                Debug.Log("[Setup] Partida (Mining) criada na cena Game.");
            }

            changed |= EnsureActiveBalance();
            changed |= EnsureTapInput();
            changed |= EnsureHud();
            changed |= EnsureHudBlocksTaps();
            changed |= DescentSetup.EnsureInGameScene();
            changed |= CorridorArtSetup.EnsureInScene();
            changed |= BiomeArtSetup.EnsureInScene();
            changed |= CrystalArtSetup.EnsureInScene();
            changed |= DwarfArtSetup.EnsureInScene();
            changed |= DwarfVariantsSetup.EnsureInScene();
            changed |= FeedbackSetup.EnsureInScene();
            changed |= DebugPanelSetup.Ensure();
            changed |= ShopSetup.EnsureInScene();
            changed |= UiSkinSetup.EnsureInScene();

            if (changed) EditorSceneManager.SaveScene(scene);
        }

        /// <summary>M5 (E2): o leitor de toque, no objeto "Mining".</summary>
        static bool EnsureTapInput()
        {
            var root = GameObject.Find(MiningRootName);
            if (root == null || root.GetComponent<TapInput>() != null) return false;

            SetReferences(root.AddComponent<TapInput>(), ("runner", root.GetComponent<MiningRunner>()));
            Debug.Log("[Setup] Leitor de toque criado na cena Game.");
            return true;
        }

        /// <summary>
        /// M5 (E2): os painéis do HUD passam a receber raycast, para o toque neles não contar no mapa. O HUD do M4 foi
        /// criado com o raycast desligado; isto o atualiza uma vez (depois, não muda nada).
        /// </summary>
        static bool EnsureHudBlocksTaps()
        {
            var hud = GameObject.Find(HudName);
            if (hud == null) return false;

            bool changed = false;
            foreach (Transform panel in hud.transform)
            {
                if (!panel.TryGetComponent<Image>(out var image) || image.raycastTarget) continue;
                image.raycastTarget = true;
                changed = true;
            }
            if (changed) Debug.Log("[Setup] HUD passa a bloquear o toque no mapa.");
            return changed;
        }

        /// <summary>
        /// HUD de cima (S02, E4) dentro do Safe Area: quatro painéis com rótulo e valor. Os rótulos são dados da cena
        /// (o código do jogo não escreve texto de interface). O rótulo de diagnóstico do M1 sai da tela (volta no debug, E5).
        /// </summary>
        static bool EnsureHud()
        {
            var safeArea = GameObject.Find("SafeArea");
            if (safeArea == null || safeArea.transform.Find(HudName) != null) return false;

            var runner = Object.FindAnyObjectByType<MiningRunner>();
            var hudObject = new GameObject(HudName, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            hudObject.transform.SetParent(safeArea.transform, false);
            var rect = (RectTransform)hudObject.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -4f);
            rect.sizeDelta = new Vector2(-8f, 34f); // 4 px de margem dos lados, 34 px de altura (referência 270×480)

            var layout = hudObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 3f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;

            var shards = CreateHudPanel(hudObject.transform, "Lascas", "LASCAS");
            var damage = CreateHudPanel(hudObject.transform, "Dano", "DANO");
            var cave = CreateHudPanel(hudObject.transform, "Caverna", "CAVERNA");
            var speed = CreateHudPanel(hudObject.transform, "Velocidade", "VELOCIDADE");

            var hud = hudObject.AddComponent<HudView>();
            SetReferences(hud, ("runner", runner), ("shardsValue", shards), ("damageValue", damage),
                          ("caveValue", cave), ("speedValue", speed));

            var buildInfo = safeArea.transform.Find("BuildInfo");
            if (buildInfo != null) buildInfo.gameObject.SetActive(false);

            Debug.Log("[Setup] HUD criado na cena Game.");
            return true;
        }

        /// <summary>Um painel do HUD; devolve o texto do valor.</summary>
        internal static TMP_Text CreateHudPanel(Transform parent, string name, string label)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var image = panel.GetComponent<Image>();
            image.color = HudPanelColor; // raycastTarget ligado: tocar no HUD não vira toque no mapa (M5)

            CreateHudText(panel.transform, "Label", label, 7f, HudLabelColor, new Vector2(0f, 0.55f), new Vector2(1f, 1f));
            return CreateHudText(panel.transform, "Value", "0", 12f, HudValueColor, new Vector2(0f, 0f), new Vector2(1f, 0.62f));
        }

        internal static TMP_Text CreateHudText(Transform parent, string name, string text, float size, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            return tmp;
        }

        internal static SpriteRenderer CreateSprite(string name, Transform parent, Sprite sprite, Color color, Vector3 position, Vector3 scale)
        {
            var go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            return renderer;
        }

        /// <summary>
        /// O balanço do jogo passa do protótipo para o GDD v5 (D-103). Só troca se a partida ainda estiver no protótipo;
        /// outro asset posto à mão fica. Devolve se mudou.
        /// </summary>
        static bool EnsureActiveBalance()
        {
            var runner = Object.FindAnyObjectByType<MiningRunner>();
            if (runner == null) return false;

            var so = new SerializedObject(runner);
            var rules = so.FindProperty("rules");
            if (rules.objectReferenceValue != null
                && AssetDatabase.GetAssetPath(rules.objectReferenceValue) != ProjectSetup.PrototypeBalancePath) return false;

            var active = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(ProjectSetup.ActiveBalancePath);
            if (active == null || rules.objectReferenceValue == active) return false;
            rules.objectReferenceValue = active;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[Setup] Balanço do jogo = {active.name} (D-103).");
            return true;
        }

        /// <summary>Preenche campos privados com [SerializeField] (o mesmo que arrastar no Inspector).</summary>
        internal static void SetReferences(Object target, params (string field, Object value)[] references)
        {
            var serialized = new SerializedObject(target);
            foreach (var (field, value) in references)
            {
                var property = serialized.FindProperty(field);
                if (property == null)
                {
                    Debug.LogError($"[Setup] Campo '{field}' não existe em {target.GetType().Name}.");
                    continue;
                }
                property.objectReferenceValue = value;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
