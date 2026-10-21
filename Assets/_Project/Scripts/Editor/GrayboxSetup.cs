using System.IO;
using MinersIdle.Game;
using MinersIdle.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="ProjectSetup"/> para o graybox do Marco 2 (E3): sprites brancos 16×16, o prefab do cristal e o
    /// objeto "Mining" na cena Game (partida, chão, cristais e anão). Como o resto do setup, só cria o que falta.
    /// </summary>
    static class GrayboxSetup
    {
        const string SpriteFolder = "Assets/_Project/Art/Graybox";
        const string SquarePath = SpriteFolder + "/Square.png";
        const string DiamondPath = SpriteFolder + "/Diamond.png";
        const string CirclePath = SpriteFolder + "/Circle.png";
        const string CrystalPrefabPath = "Assets/_Project/Prefabs/Graybox/CrystalView.prefab";
        const string MiningRootName = "Mining";

        const int SpriteSize = 16;
        const float PixelsPerUnit = 16f;
        const int MapSortingOrder = -32000;

        // Cores provisórias do graybox: chão ardósia, cristal magenta (o do mockup), anão âmbar (cor de ação do GDD).
        static readonly Color FloorColor = new Color32(0x23, 0x33, 0x44, 0xFF);
        static readonly Color CrystalColor = new Color32(0xC2, 0x3C, 0xFF, 0xFF);
        static readonly Color MinerColor = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        static readonly Color BarBackColor = new Color32(0x15, 0x19, 0x1C, 0xFF);
        static readonly Color BarFillColor = new Color32(0x4C, 0xD9, 0x64, 0xFF);

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

            if (changed) EditorSceneManager.SaveScene(scene);
        }

        static SpriteRenderer CreateSprite(string name, Transform parent, Sprite sprite, Color color, Vector3 position, Vector3 scale)
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
        static void SetReferences(Object target, params (string field, Object value)[] references)
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
