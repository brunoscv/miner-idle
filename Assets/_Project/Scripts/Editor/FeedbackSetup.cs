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

        /// <summary>O clarão da M6 E4, que saiu (D-83): cenas antigas perdem o objeto.</summary>
        const string FlashName = "Flash";

        static readonly Color ShardColor = new Color32(0xC2, 0x3C, 0xFF, 0xFF);
        static readonly Color ComboColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        static readonly Color CaveBannerColor = new Color32(0xFF, 0xD0, 0x6A, 0xFF);
        const string CaveBannerName = "CaveBanner";

        public static void EnsureTexts()
        {
            if (File.Exists(TextsPath))
            {
                EnsureCaveBannerText();
                return;
            }

            ProjectSetup.EnsureFolder(Path.GetDirectoryName(TextsPath)?.Replace('\\', '/'));
            var texts = ScriptableObject.CreateInstance<FeedbackTexts>();
            var so = new SerializedObject(texts);
            so.FindProperty("criticalLabel").stringValue = "CRÍTICO!";
            so.FindProperty("comboFormat").stringValue = "x{0}";
            so.FindProperty("milestoneFormat").stringValue = "x{0}!";
            so.FindProperty("caveBannerFormat").stringValue = CaveBannerFormat;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(texts, TextsPath);
            Debug.Log($"[Setup] Textos do feedback criados em {TextsPath}.");
        }

        const string CaveBannerFormat = "Caverna {0} · {1}";

        /// <summary>M8 (E2): o texto da faixa da caverna nova nos textos que já existem.</summary>
        static void EnsureCaveBannerText()
        {
            var texts = AssetDatabase.LoadAssetAtPath<FeedbackTexts>(TextsPath);
            if (texts == null || !string.IsNullOrEmpty(texts.CaveBannerFormat)) return;

            var so = new SerializedObject(texts);
            so.FindProperty("caveBannerFormat").stringValue = CaveBannerFormat;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(texts);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Texto da faixa da caverna nos textos do feedback (M8).");
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

            // D-83: a tela não pisca mais na quebra; o quadrado do clarão (M6 E4) sai das cenas que o têm.
            var oldFlash = effectsObject != null ? effectsObject.Find(FlashName) : null;
            if (oldFlash != null)
            {
                Object.DestroyImmediate(oldFlash.gameObject);
                Debug.Log("[Setup] Clarão da quebra removido da cena Game (D-83).");
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

            // M8 (E2): a faixa da caverna nova, em "Effects/CaveBanner".
            if (effectsObject != null && effectsObject.Find(CaveBannerName) == null)
            {
                var bannerObject = Child(effectsObject.gameObject, CaveBannerName);
                var label = WorldText(bannerObject.transform, "Label", 4.5f);
                label.color = CaveBannerColor;
                label.sortingOrder = 31600;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.rectTransform.sizeDelta = new Vector2(15f, 4f);
                var banner = bannerObject.AddComponent<CaveBanner>();
                GrayboxSetup.SetReferences(banner,
                    ("runner", mining.GetComponent<MiningRunner>()),
                    ("texts", AssetDatabase.LoadAssetAtPath<FeedbackTexts>(TextsPath)),
                    ("stage", Object.FindAnyObjectByType<DescentStage>()), ("worldCamera", camera), ("label", label));
                Debug.Log("[Setup] Faixa da caverna nova criada na cena Game (M8).");
                changed = true;
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
