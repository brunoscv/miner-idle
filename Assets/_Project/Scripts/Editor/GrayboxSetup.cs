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
    /// Parte do <see cref="ProjectSetup"/> para o graybox do Marco 2: sprites brancos 16×16, o prefab do cristal, o objeto
    /// "Mining" na cena Game (partida, chão, cristais e anão; E3), o HUD de cima (E4) e o painel de debug com o "Mostrar
    /// ordem" (E5, ver <see cref="DebugPanelSetup"/>). Como o resto do setup, só cria o que falta.
    /// </summary>
    static class GrayboxSetup
    {
        const string SpriteFolder = "Assets/_Project/Art/Graybox";
        const string SquarePath = SpriteFolder + "/Square.png";
        const string DiamondPath = SpriteFolder + "/Diamond.png";
        const string CirclePath = SpriteFolder + "/Circle.png";
        const string CrystalPrefabPath = "Assets/_Project/Prefabs/Graybox/CrystalView.prefab";
        const string MiningRootName = "Mining";
        const string HudName = "Hud";

        const int SpriteSize = 16;
        const float PixelsPerUnit = 16f;
        const int MapSortingOrder = -32000;
        const int TargetLineSortingOrder = 29000;   // abaixo das barras de vida (30000)
        const int BatchLabelSortingOrder = 30002;   // acima das barras

        // Cores provisórias do graybox: chão ardósia, cristal magenta (o do mockup), anão âmbar (cor de ação do GDD).
        static readonly Color FloorColor = new Color32(0x23, 0x33, 0x44, 0xFF);
        static readonly Color CrystalColor = new Color32(0xC2, 0x3C, 0xFF, 0xFF);
        static readonly Color MinerColor = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        static readonly Color BarBackColor = new Color32(0x15, 0x19, 0x1C, 0xFF);
        static readonly Color BarFillColor = new Color32(0x4C, 0xD9, 0x64, 0xFF);
        static readonly Color TargetLineColor = new Color(0.949f, 0.663f, 0f, 0.7f);
        // HUD: painel de madeira escura do mockup, rótulo âmbar, valor claro.
        static readonly Color HudPanelColor = new Color32(0x2B, 0x1D, 0x14, 0xE6);
        static readonly Color HudLabelColor = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        static readonly Color HudValueColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);

        public static void Ensure()
        {
            var square = EnsureSprite(SquarePath, (x, y) => true);
            var diamond = EnsureSprite(DiamondPath, (x, y) => Mathf.Abs(x - 7.5f) + Mathf.Abs(y - 7.5f) <= 8f);
            EnsureSprite(CirclePath, (x, y) => (x - 7.5f) * (x - 7.5f) + (y - 7.5f) * (y - 7.5f) <= 64f);
            EnsureCrystalPrefab(square, diamond);
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

        static void EnsureCrystalPrefab(Sprite square, Sprite diamond)
        {
            if (!File.Exists(CrystalPrefabPath))
            {
                ProjectSetup.EnsureFolder(Path.GetDirectoryName(CrystalPrefabPath)?.Replace('\\', '/'));

                var root = new GameObject("CrystalView");
                var body = CreateSprite("Body", root.transform, diamond, CrystalColor, Vector3.zero, new Vector3(1.2f, 1.2f, 1f));

                var bar = new GameObject("HealthBar");
                bar.transform.SetParent(root.transform, false);
                bar.transform.localPosition = new Vector3(0f, 0.95f, 0f);
                var back = CreateSprite("Back", bar.transform, square, BarBackColor, Vector3.zero, new Vector3(1.3f, 0.25f, 1f));
                var fill = CreateSprite("Fill", bar.transform, square, BarFillColor, Vector3.zero, new Vector3(1.2f, 0.15f, 1f));

                var view = root.AddComponent<CrystalView>();
                SetReferences(view, ("body", body), ("healthBar", bar), ("healthBack", back), ("healthFill", fill));

                PrefabUtility.SaveAsPrefabAsset(root, CrystalPrefabPath);
                Object.DestroyImmediate(root);
                Debug.Log($"[Setup] Prefab criado: {CrystalPrefabPath}.");
            }

            EnsureBatchLabel();
        }

        /// <summary>E5: o número do lote em cada cristal ("Mostrar ordem"), escondido por padrão.</summary>
        static void EnsureBatchLabel()
        {
            var contents = PrefabUtility.LoadPrefabContents(CrystalPrefabPath);
            try
            {
                if (contents.transform.Find("BatchLabel") != null) return;

                var label = new GameObject("BatchLabel", typeof(TextMeshPro));
                label.transform.SetParent(contents.transform, false);
                var tmp = label.GetComponent<TextMeshPro>();
                tmp.text = "1";
                tmp.fontSize = 6f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.sortingOrder = BatchLabelSortingOrder;
                tmp.rectTransform.sizeDelta = new Vector2(1.5f, 1f);
                label.SetActive(false);

                SetReferences(contents.GetComponent<CrystalView>(), ("batchLabel", tmp));
                PrefabUtility.SaveAsPrefabAsset(contents, CrystalPrefabPath);
                Debug.Log("[Setup] Número do lote adicionado ao prefab do cristal.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
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
                // Carregar só depois de abrir a cena: abrir a cena descarrega assets "sem uso" e as referências
                // carregadas antes viram nulas (o prefab do cristal saía vazio na primeira versão deste setup).
                var square = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
                var circle = AssetDatabase.LoadAssetAtPath<Sprite>(CirclePath);
                var crystalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CrystalPrefabPath).GetComponent<CrystalView>();
                var rules = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(ProjectSetup.PrototypeBalancePath);
                var root = new GameObject(MiningRootName);
                var runner = root.AddComponent<MiningRunner>();
                SetReferences(runner, ("rules", rules));

                var map = CreateSprite("Map", root.transform, square, FloorColor, Vector3.zero, Vector3.one);
                map.sortingOrder = MapSortingOrder;
                SetReferences(map.gameObject.AddComponent<MapView>(), ("runner", runner));

                var crystals = new GameObject("Crystals");
                crystals.transform.SetParent(root.transform, false);
                SetReferences(crystals.AddComponent<CrystalFieldView>(), ("runner", runner), ("crystalPrefab", crystalPrefab));

                var miner = CreateSprite("Miner", root.transform, circle, MinerColor, Vector3.zero, new Vector3(0.9f, 1.2f, 1f));
                SetReferences(miner.gameObject.AddComponent<MinerView>(), ("runner", runner), ("body", miner));

                // O painel âmbar do M1 só mostrava o Safe Area; sobre o mapa ele atrapalha. O componente fica (layout).
                var safeArea = GameObject.Find("SafeArea");
                if (safeArea != null && safeArea.TryGetComponent<Image>(out var tint)) tint.color = Color.clear;

                changed = true;
                Debug.Log("[Setup] Graybox do mapa criado na cena Game.");
            }

            changed |= EnsureTargetLine();
            changed |= EnsureHud();
            changed |= DebugPanelSetup.Ensure();

            if (changed) EditorSceneManager.SaveScene(scene);
        }

        /// <summary>E5: a linha do anão até o alvo ("Mostrar ordem"), desligada por padrão.</summary>
        static bool EnsureTargetLine()
        {
            var root = GameObject.Find(MiningRootName);
            if (root == null || root.transform.Find("TargetLine") != null) return false;

            var square = AssetDatabase.LoadAssetAtPath<Sprite>(SquarePath);
            var line = CreateSprite("TargetLine", root.transform, square, TargetLineColor, Vector3.zero, Vector3.one);
            line.sortingOrder = TargetLineSortingOrder;
            line.enabled = false;
            SetReferences(line.gameObject.AddComponent<TargetLineView>(),
                          ("runner", root.GetComponent<MiningRunner>()), ("line", line));
            Debug.Log("[Setup] Linha até o alvo criada na cena Game.");
            return true;
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
        static TMP_Text CreateHudPanel(Transform parent, string name, string label)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var image = panel.GetComponent<Image>();
            image.color = HudPanelColor;
            image.raycastTarget = false;

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
