using System.IO;
using MinersIdle.Game;
using MinersIdle.Presentation;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="GrayboxSetup"/> para o feedback de mineração (M5, E3), na cena Game já aberta: os textos em
    /// dados (<c>Data/FeedbackTexts</c>), o tremor na câmera e o objeto "Effects" (faíscas, textos flutuantes, lasca
    /// voadora, "xN" e o <see cref="MiningFeedbackView"/>) e, desde a E4, o som provisório (<see cref="SoundBoard"/>).
    /// Só cria o que falta.
    /// </summary>
    static class FeedbackSetup
    {
        const string TextsPath = "Assets/_Project/Data/FeedbackTexts.asset";
        const string EffectsName = "Effects";
        const string SoundName = "Sound";
        const string FlashName = "Flash";

        /// <summary>O clarão fica por cima de tudo que a câmera da tela desenha.</summary>
        const int FlashSortingOrder = 32000;

        static readonly Color ShardColor = new Color32(0xC2, 0x3C, 0xFF, 0xFF);
        static readonly Color ComboColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);

        public static void EnsureTexts()
        {
            if (File.Exists(TextsPath)) return;

            ProjectSetup.EnsureFolder(Path.GetDirectoryName(TextsPath)?.Replace('\\', '/'));
            var texts = ScriptableObject.CreateInstance<FeedbackTexts>();
            var so = new SerializedObject(texts);
            so.FindProperty("criticalLabel").stringValue = "CRÍTICO!";
            so.FindProperty("comboFormat").stringValue = "x{0}";
            so.FindProperty("milestoneFormat").stringValue = "x{0}!";
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(texts, TextsPath);
            Debug.Log($"[Setup] Textos do feedback criados em {TextsPath}.");
        }

        public static bool EnsureInScene()
        {
            bool changed = false;

            var camera = Camera.main;
            if (camera != null && camera.GetComponent<ScreenShake>() == null)
            {
                camera.gameObject.AddComponent<ScreenShake>();
                changed = true;
            }

            var mining = GameObject.Find("Mining");
            if (mining == null) return changed;

            if (mining.transform.Find(EffectsName) == null)
            {
                var square = AssetDatabase.LoadAssetAtPath<Sprite>(GrayboxSetup.SquarePath);
                var diamond = AssetDatabase.LoadAssetAtPath<Sprite>(GrayboxSetup.DiamondPath);
                var texts = AssetDatabase.LoadAssetAtPath<FeedbackTexts>(TextsPath);

                var effects = new GameObject(EffectsName);
                effects.transform.SetParent(mining.transform, false);

                var sparks = Child(effects, "Sparks").AddComponent<SparkBurst>();
                var sparkTemplate = GrayboxSetup.CreateSprite("SparkTemplate", sparks.transform, square, Color.white, Vector3.zero, new Vector3(0.15f, 0.15f, 1f));
                GrayboxSetup.SetReferences(sparks, ("sparkTemplate", sparkTemplate));

                var floating = Child(effects, "FloatingTexts").AddComponent<FloatingTexts>();
                var textTemplate = WorldText(floating.transform, "TextTemplate", 6f);
                GrayboxSetup.SetReferences(floating, ("textTemplate", textTemplate));

                var flight = Child(effects, "ShardFlight").AddComponent<ShardFlight>();
                var shardTemplate = GrayboxSetup.CreateSprite("ShardTemplate", flight.transform, diamond, ShardColor, Vector3.zero, new Vector3(0.5f, 0.5f, 1f));
                var lascas = GameObject.Find("Hud")?.transform.Find("Lascas") as RectTransform;
                GrayboxSetup.SetReferences(flight, ("shardTemplate", shardTemplate), ("target", lascas), ("worldCamera", camera));

                var combo = WorldText(effects.transform, "Combo", 5f);
                combo.color = ComboColor;
                combo.sortingOrder = 31200;
                combo.gameObject.SetActive(false);

                var feedback = effects.AddComponent<MiningFeedbackView>();
                GrayboxSetup.SetReferences(feedback,
                    ("runner", mining.GetComponent<MiningRunner>()), ("texts", texts),
                    ("tapInput", mining.GetComponent<TapInput>()),
                    ("stage", Object.FindAnyObjectByType<DescentStage>()),
                    ("worldCamera", camera), ("screenShake", camera.GetComponent<ScreenShake>()),
                    ("sparks", sparks), ("floatingTexts", floating), ("shardFlight", flight), ("comboText", combo));

                Debug.Log("[Setup] Efeitos de feedback criados na cena Game.");
                changed = true;
            }

            // E4: o som mora em "Effects/Sound" (cenas da E3 ganham só ele).
            var effectsObject = mining.transform.Find(EffectsName);
            if (effectsObject != null && effectsObject.Find(SoundName) == null)
            {
                var sound = Child(effectsObject.gameObject, SoundName).AddComponent<SoundBoard>();
                GrayboxSetup.SetReferences(sound, ("runner", mining.GetComponent<MiningRunner>()));
                Debug.Log("[Setup] Som provisório criado na cena Game.");
                changed = true;
            }

            // M6 (E4): o clarão do crítico e da quebra, um quadrado do tamanho da tela por cima de tudo.
            if (effectsObject != null && effectsObject.Find(FlashName) == null)
            {
                var square = AssetDatabase.LoadAssetAtPath<Sprite>(GrayboxSetup.SquarePath);
                var flash = GrayboxSetup.CreateSprite(FlashName, effectsObject, square, Color.clear, Vector3.zero, Vector3.one);
                flash.sortingOrder = FlashSortingOrder;
                flash.enabled = false;
                GrayboxSetup.SetReferences(effectsObject.GetComponent<MiningFeedbackView>(), ("flash", flash));
                Debug.Log("[Setup] Clarão do crítico e da quebra criado na cena Game.");
                changed = true;
            }

            // M6 (E4): o ajuste de intensidade dos efeitos mora no painel de câmera do DBG.
            var cameraPanel = Object.FindAnyObjectByType<DescentCameraPanel>();
            if (cameraPanel != null)
            {
                var so = new SerializedObject(cameraPanel);
                var feedbackProperty = so.FindProperty("feedback");
                if (feedbackProperty.objectReferenceValue == null)
                {
                    feedbackProperty.objectReferenceValue = Object.FindAnyObjectByType<MiningFeedbackView>();
                    so.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }
            }

            // M6 (E3): o feedback passa a perguntar ao palco da descida onde estão o cristal e o anão.
            var existingFeedback = Object.FindAnyObjectByType<MiningFeedbackView>();
            if (existingFeedback != null)
            {
                var so = new SerializedObject(existingFeedback);
                var stage = so.FindProperty("stage");
                if (stage.objectReferenceValue == null)
                {
                    stage.objectReferenceValue = Object.FindAnyObjectByType<DescentStage>();
                    so.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log("[Setup] Feedback ligado à descida.");
                    changed = true;
                }
            }

            // O HUD do M4 não conhecia a lasca voadora: liga uma vez.
            var hud = Object.FindAnyObjectByType<HudView>();
            if (hud != null)
            {
                var so = new SerializedObject(hud);
                var property = so.FindProperty("shardFlight");
                if (property.objectReferenceValue == null)
                {
                    property.objectReferenceValue = Object.FindAnyObjectByType<ShardFlight>();
                    so.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }
            }

            return changed;
        }

        static GameObject Child(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        static TextMeshPro WorldText(Transform parent, string name, float size)
        {
            var go = new GameObject(name, typeof(TextMeshPro));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshPro>();
            text.fontSize = size;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = new Vector2(6f, 2f);
            return text;
        }
    }
}
