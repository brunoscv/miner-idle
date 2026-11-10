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
                EnsureHeartTexts();
                EnsureRestTexts();
                EnsurePermanentsText();
                EnsureOfflineTexts();
                EnsureSettingsTexts();
                return;
            }

            ProjectSetup.EnsureFolder(Path.GetDirectoryName(TextsPath)?.Replace('\\', '/'));
            var texts = ScriptableObject.CreateInstance<FeedbackTexts>();
            var so = new SerializedObject(texts);
            so.FindProperty("criticalLabel").stringValue = "CRÍTICO!";
            so.FindProperty("comboFormat").stringValue = "x{0}";
            so.FindProperty("milestoneFormat").stringValue = "x{0}!";
            so.FindProperty("caveBannerFormat").stringValue = CaveBannerFormat;
            SetHeartTexts(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(texts, TextsPath);
            Debug.Log($"[Setup] Textos do feedback criados em {TextsPath}.");
            EnsureRestTexts();
            EnsurePermanentsText();
            EnsureOfflineTexts();
            EnsureSettingsTexts();
        }

        const string CaveBannerFormat = "Nível {0} · {1}";
        const string OldCaveBannerFormat = "Caverna {0} · {1}";

        /// <summary>
        /// M8 (E2): o texto da faixa da caverna nova nos textos que já existem. E3b-2 (D-129): os 100 níveis do GDD v7,
        /// "Nível 12 · Galeria rochosa" (o texto de antes, "Caverna 2 · …", é trocado).
        /// </summary>
        static void EnsureCaveBannerText()
        {
            var texts = AssetDatabase.LoadAssetAtPath<FeedbackTexts>(TextsPath);
            if (texts == null) return;
            if (!string.IsNullOrEmpty(texts.CaveBannerFormat) && texts.CaveBannerFormat != OldCaveBannerFormat) return;

            var so = new SerializedObject(texts);
            so.FindProperty("caveBannerFormat").stringValue = CaveBannerFormat;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(texts);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Texto da faixa da caverna nos textos do feedback (M8).");
        }

        /// <summary>M8 (E3): os textos da tela do coração da montanha nos textos que já existem.</summary>
        static void EnsureHeartTexts()
        {
            var texts = AssetDatabase.LoadAssetAtPath<FeedbackTexts>(TextsPath);
            if (texts == null || !string.IsNullOrEmpty(texts.HeartTitle)) return;

            var so = new SerializedObject(texts);
            SetHeartTexts(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(texts);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Textos do coração da montanha nos textos do feedback (M8).");
        }

        /// <summary>Marco 8b (E3): os textos do descanso e do resumo do dia (D-151…D-154), nos textos que já existem.</summary>
        static void EnsureRestTexts()
        {
            var texts = AssetDatabase.LoadAssetAtPath<FeedbackTexts>(TextsPath);
            if (texts == null || !string.IsNullOrEmpty(texts.RestLabel)) return;

            var so = new SerializedObject(texts);
            so.FindProperty("restLabel").stringValue = "DESCANSAR";
            so.FindProperty("restTitle").stringValue = "DESCANSAR";
            so.FindProperty("dayFormat").stringValue = "DIA {0}";
            so.FindProperty("restLevelLabel").stringValue = "Nível alcançado";
            so.FindProperty("restTimeLabel").stringValue = "Tempo do dia";
            so.FindProperty("restShardsLabel").stringValue = "Lascas da run";
            so.FindProperty("restTakeLabel").stringValue = "Você leva";
            so.FindProperty("restNextFormat").stringValue = "No próximo nível: +{0}";
            so.FindProperty("restNoNext").stringValue = "Você chegou ao último nível.";
            so.FindProperty("restNote").stringValue =
                "Amanhã você recomeça do nível 1, sem as lascas e as melhorias desta run. As permanentes ficam.";
            so.FindProperty("restContinue").stringValue = "CONTINUAR";
            so.FindProperty("summaryTitleFormat").stringValue = "DIA {0} ENCERRADO";
            so.FindProperty("summaryLevelLabel").stringValue = "Chegou ao nível";
            so.FindProperty("summaryTimeLabel").stringValue = "Tempo do dia";
            so.FindProperty("summaryGainedLabel").stringValue = "Lascas vermelhas";
            so.FindProperty("summaryGainedFormat").stringValue = "+{0}";
            so.FindProperty("summaryBankLabel").stringValue = "No banco";
            so.FindProperty("summaryStart").stringValue = "COMEÇAR";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(texts);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Textos do descanso e do resumo do dia nos textos do feedback (Marco 8b, E3).");
        }

        /// <summary>Marco 8b (E4): o botão PERMANENTES do resumo do dia (D-154), nos textos que já existem.</summary>
        static void EnsurePermanentsText()
        {
            var texts = AssetDatabase.LoadAssetAtPath<FeedbackTexts>(TextsPath);
            if (texts == null || !string.IsNullOrEmpty(texts.SummaryPermanents)) return;

            var so = new SerializedObject(texts);
            so.FindProperty("summaryPermanents").stringValue = "PERMANENTES";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(texts);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Texto do PERMANENTES do resumo do dia (Marco 8b, E4).");
        }

        /// <summary>M9 (E2): os textos do "Bem-vindo de volta" (D-197), nos textos que já existem.</summary>
        static void EnsureOfflineTexts()
        {
            var texts = AssetDatabase.LoadAssetAtPath<FeedbackTexts>(TextsPath);
            if (texts == null || !string.IsNullOrEmpty(texts.OfflineTitle)) return;

            var so = new SerializedObject(texts);
            so.FindProperty("offlineTitle").stringValue = "BEM-VINDO DE VOLTA";
            so.FindProperty("offlineAwayLabel").stringValue = "Tempo fora";
            so.FindProperty("offlineCountedLabel").stringValue = "Contou";
            so.FindProperty("offlineCountedFormat").stringValue = "{0} (máx.)";
            so.FindProperty("offlineCrystalsLabel").stringValue = "Cristais quebrados";
            so.FindProperty("offlineBroughtLabel").stringValue = "O estagiário trouxe";
            so.FindProperty("offlineShardsFormat").stringValue = "+{0}";
            so.FindProperty("offlineNoteFormat").stringValue = "Ele minerou no nível {0} enquanto você esteve fora.";
            so.FindProperty("offlineCappedNoteFormat").stringValue = "Ele minera no máximo {0} com o jogo fechado.";
            so.FindProperty("offlineCollect").stringValue = "COLETAR";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(texts);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Textos do Bem-vindo de volta nos textos do feedback (M9, E2).");
        }

        /// <summary>
        /// Os créditos da tela AJUSTES (M9, E4), a partir do <c>CREDITS.md</c>: o que está no jogo hoje (o que saiu ou só foi
        /// avaliado não entra). Mockup aprovado pelo Bruno em 2026-11-10.
        /// </summary>
        static readonly (bool header, string text)[] Credits =
        {
            (true, "Jogo"), (false, "Bruno Carvalho"),
            (true, "Design e arte"), (false, "Apellon · Bruno Carvalho"),
            (true, "Interface"), (false, "Pixel UI Kit · REEBOO Labs"), (false, "Fantasy UI Kit Lite · Willosuna"),
            (false, "Anvil's RPG icons · Ponk (CC BY 4.0)"),
            (true, "Fontes"), (false, "Press Start 2P · CodeMan38 (OFL 1.1)"),
            (false, "Pixelify Sans · Stefie Justprince (OFL 1.1)"),
            (true, "Caverna"), (false, "Pixel Fantasy Caves · Szadi art."), (false, "MineAssets (pedras da beira)"),
        };

        /// <summary>M9 (E4, D-198): os textos da tela AJUSTES, da confirmação do Apagar save e dos Créditos.</summary>
        static void EnsureSettingsTexts()
        {
            var texts = AssetDatabase.LoadAssetAtPath<FeedbackTexts>(TextsPath);
            if (texts == null || !string.IsNullOrEmpty(texts.SettingsTitle)) return;

            var so = new SerializedObject(texts);
            so.FindProperty("settingsTitle").stringValue = "AJUSTES";
            so.FindProperty("settingsVolumeLabel").stringValue = "Efeitos";
            so.FindProperty("settingsVolumeFormat").stringValue = "{0}%";
            so.FindProperty("settingsShakeLabel").stringValue = "Tremor da tela";
            so.FindProperty("settingsVibrationLabel").stringValue = "Vibração";
            so.FindProperty("settingsCredits").stringValue = "CRÉDITOS";
            so.FindProperty("settingsDeleteSave").stringValue = "APAGAR SAVE";
            so.FindProperty("deleteTitle").stringValue = "APAGAR O SAVE?";
            so.FindProperty("deleteBody").stringValue =
                "Você perde todo o progresso: lascas, níveis, melhorias, lascas vermelhas e permanentes. Os ajustes ficam.";
            so.FindProperty("deleteWarning").stringValue = "Não dá para desfazer.";
            so.FindProperty("deleteCancel").stringValue = "CANCELAR";
            so.FindProperty("deleteConfirm").stringValue = "APAGAR";
            so.FindProperty("creditsTitle").stringValue = "CRÉDITOS";
            so.FindProperty("creditsBack").stringValue = "VOLTAR";
            var list = so.FindProperty("credits");
            list.arraySize = Credits.Length;
            for (int i = 0; i < Credits.Length; i++)
            {
                var line = list.GetArrayElementAtIndex(i);
                line.FindPropertyRelative("header").boolValue = Credits[i].header;
                line.FindPropertyRelative("text").stringValue = Credits[i].text;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(texts);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Textos dos AJUSTES e os Créditos nos textos do feedback (M9, E4).");
        }

        static void SetHeartTexts(SerializedObject so)
        {
            so.FindProperty("heartTitle").stringValue = "Coração da montanha";
            so.FindProperty("heartBodyFormat").stringValue =
                "Você desceu as dez cavernas.\n\nTempo de jogo: {0}\nLascas coletadas: {1}";
            so.FindProperty("heartContinue").stringValue = "CONTINUAR";
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
