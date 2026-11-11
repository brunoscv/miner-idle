using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static MinersIdle.EditorTools.RareWindowSetup;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="GrayboxSetup"/> para a E5 do M9b (mockup aprovado pelo Bruno em 2026-11-10,
    /// <c>tools/ci/out/m9b-e5-mockup.png</c>; D-218, D-227…D-235):
    /// <list type="bullet">
    /// <item>os ícones gerados (<c>Art/UI/icon_ad.png</c>, <c>icon_bolt.png</c>, D-233) como sprites sem filtro;</item>
    /// <item>o boost do AD e os textos do anúncio no <c>Data/BuffCatalog</c>;</item>
    /// <item>o anúncio simulado ("AdScreen", <see cref="SimulatedAdScreen"/>) antes da loja e depois da janela do raro (cobre
    /// a janela; a loja, os modais e o DBG ficam por cima);</item>
    /// <item>o botão AD da coluna deixa o "Em breve" e ganha o <see cref="AdButtonView"/>;</item>
    /// <item>o raio acima do anão em "Mining/Effects/AdBolt" (<see cref="AdBoostBolt"/>).</item>
    /// </list>
    /// Só cria o que falta; os textos ficam no catálogo.
    /// </summary>
    static class AdSetup
    {
        const string ScreenName = "AdScreen";
        const string BoltName = "AdBolt";
        internal const string AdIconPath = "Assets/_Project/Art/UI/icon_ad.png";
        internal const string BoltIconPath = "Assets/_Project/Art/UI/icon_bolt.png";

        // O mockup: painel 236 × 236 no meio da tela (32 acima do centro), a telinha 150 × 92, a barra e o RECOMPENSA.
        const float PanelWidth = 236f;
        const float PanelHeight = 236f;
        const float PanelRaise = 32f;
        const float InsetLeft = 43f;
        const float InsetTop = 26f;
        const float InsetWidth = 150f;
        const float InsetHeight = 92f;
        const float InsetIcon = 56f;
        const float BarLeft = 12f;
        const float BarTop = 164f;
        const float BarHeight = 12f;
        const float RewardTop = 180f;
        const float ButtonWidth = 120f;
        const float ButtonHeight = 26f;
        const float ButtonTop = 198f;

        const float SmallText = 33f / 4f;
        static readonly Color DimColor = new Color(0f, 0f, 0f, 0.6f);
        static readonly Color TextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        static readonly Color AmberText = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        static readonly Color MutedText = new Color32(0xBE, 0xB4, 0xAA, 0xFF);

        static TMP_FontAsset _font, _numbers;

        public static bool EnsureInScene()
        {
            bool changed = EnsureSprites();
            var catalog = AssetDatabase.LoadAssetAtPath<BuffCatalog>(BuffColumnSetup.CatalogPath);
            if (catalog == null) return changed;
            changed |= EnsureCatalog(catalog);

            var safeArea = GameObject.Find("SafeArea")?.transform;
            var runner = Object.FindAnyObjectByType<MiningRunner>();
            if (safeArea == null || runner == null) return changed;
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.GameFontPath);
            _numbers = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.NumbersFontPath);

            changed |= EnsureScreen(safeArea, runner, catalog);
            changed |= EnsureButton(safeArea, runner, catalog);
            changed |= EnsureBolt(runner);
            return changed;
        }

        /// <summary>Os dois ícones de 32 px (D-233) como sprites de UI e do mundo, sem filtro (PPU 16, D-14).</summary>
        static bool EnsureSprites()
        {
            bool changed = false;
            foreach (var path in new[] { AdIconPath, BoltIconPath })
            {
                if (!System.IO.File.Exists(path)) continue;
                if (AssetImporter.GetAtPath(path) == null) AssetDatabase.ImportAsset(path);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
                if (importer.textureType == TextureImporterType.Sprite && importer.filterMode == FilterMode.Point
                    && importer.textureShape == TextureImporterShape.Texture2D && !importer.mipmapEnabled
                    && importer.textureCompression == TextureImporterCompression.Uncompressed) continue;
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 16f;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
                changed = true;
            }
            return changed;
        }

        /// <summary>
        /// O boost do AD (origem Ad, tier 0) e os textos do anúncio e do botão (D-230, D-231). Só acrescenta o que falta:
        /// o que foi ajustado à mão fica como está.
        /// </summary>
        static bool EnsureCatalog(BuffCatalog catalog)
        {
            var so = new SerializedObject(catalog);
            bool entry = catalog.Find(BonusSource.Ad, 0) == null;
            if (entry)
            {
                var list = so.FindProperty("entries");
                int index = list.arraySize;
                list.arraySize = index + 1;
                var item = list.GetArrayElementAtIndex(index);
                item.FindPropertyRelative("source").enumValueIndex = (int)BonusSource.Ad;
                item.FindPropertyRelative("tier").intValue = 0;
                item.FindPropertyRelative("icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(BoltIconPath);
                item.FindPropertyRelative("title").stringValue = "VELOCIDADE ×2,5";
                item.FindPropertyRelative("description").stringValue = "Golpe 2,5× mais rápido";
                item.FindPropertyRelative("headline").stringValue = "";
                item.FindPropertyRelative("gain").stringValue = "VELOCIDADE ×2,5";
                item.FindPropertyRelative("cardText").stringValue = "";
                item.FindPropertyRelative("cardNote").stringValue = "";
                item.FindPropertyRelative("forIntern").boolValue = false;
            }

            bool texts = string.IsNullOrEmpty(catalog.AdTitle);
            if (texts)
            {
                so.FindProperty("adFooterFormat").stringValue = "AD · {0}s";
                so.FindProperty("adTitle").stringValue = "VISUALIZANDO AD";
                so.FindProperty("adWaitLabel").stringValue = "O anúncio termina em";
                so.FindProperty("adDone").stringValue = "Anúncio concluído!";
                so.FindProperty("adRewardFormat").stringValue = "Recompensa: Velocidade ×{0} por {1} s";
                so.FindProperty("adReward").stringValue = "RECOMPENSA";
                so.FindProperty("adButton").stringValue = "AD";
            }
            if (!entry && !texts) return false;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Catálogo dos bônus: o boost do AD e os textos do anúncio (M9b E5, D-230, D-231).");
            return true;
        }

        /// <summary>O modal "VISUALIZANDO AD" (D-227, D-228): escurece e bloqueia o mapa; sem saída até o RECOMPENSA.</summary>
        static bool EnsureScreen(Transform safeArea, MiningRunner runner, BuffCatalog catalog)
        {
            if (safeArea.Find(ScreenName) != null || Load(Wood("panels/window")) == null) return false;

            var root = new GameObject(ScreenName, typeof(RectTransform));
            root.transform.SetParent(safeArea, false);
            // Antes da loja (a loja, os modais e o DBG ficam por cima) e depois da janela do raro (o anúncio a cobre).
            var shop = safeArea.Find("Shop");
            if (shop != null) root.transform.SetSiblingIndex(shop.GetSiblingIndex());
            else root.transform.SetAsLastSibling();
            Stretch((RectTransform)root.transform);

            var screen = new GameObject("Screen", typeof(RectTransform), typeof(Image));
            screen.transform.SetParent(root.transform, false);
            Stretch((RectTransform)screen.transform);
            screen.GetComponent<Image>().color = DimColor;

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(screen.transform, false);
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, PanelRaise);
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            var panelImage = panel.GetComponent<Image>();
            panelImage.sprite = Load(Wood("panels/window"));
            panelImage.type = Image.Type.Sliced;

            // A barra do título: a TV pequena e "VISUALIZANDO AD".
            var adIcon = AssetDatabase.LoadAssetAtPath<Sprite>(AdIconPath);
            var titleIcon = Picture(panel.transform, "TitleIcon", adIcon);
            titleIcon.preserveAspect = true;
            Place(titleIcon.rectTransform, 6f, 1f, 16f, 16f);
            var title = Text(panel.transform, "Title", SmallText, TextColor, TextAlignmentOptions.MidlineLeft, _font);
            Place(title.rectTransform, 26f, 3f, PanelWidth - 36f, 12f);

            // A telinha do centro, com a TV grande que pulsa (pivô no meio: cresce no lugar).
            var inset = Picture(panel.transform, "Inset", Load(Wood("panels/panel_inset")));
            inset.type = Image.Type.Sliced;
            Place(inset.rectTransform, InsetLeft, InsetTop, InsetWidth, InsetHeight);
            var icon = Picture(inset.transform, "Icon", adIcon);
            icon.preserveAspect = true;
            var iconRect = icon.rectTransform;
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(InsetIcon, InsetIcon);

            // Enquanto passa: "O anúncio termina em" e o número; ao fim, "Anúncio concluído!".
            var waiting = new GameObject("Waiting", typeof(RectTransform));
            waiting.transform.SetParent(panel.transform, false);
            Stretch((RectTransform)waiting.transform);
            var waitLabel = Text(waiting.transform, "WaitLabel", SmallText, MutedText, TextAlignmentOptions.Center, _font);
            Place(waitLabel.rectTransform, 8f, 124f, PanelWidth - 16f, 12f);
            var seconds = Text(waiting.transform, "Seconds", UiSkinSetup.NumbersFontSize * 2f, TextColor,
                               TextAlignmentOptions.Center, _numbers);
            Place(seconds.rectTransform, 8f, 140f, PanelWidth - 16f, 16f);
            var done = Text(panel.transform, "Done", SmallText, AmberText, TextAlignmentOptions.Center, _font);
            Place(done.rectTransform, 8f, 136f, PanelWidth - 16f, 14f);
            done.gameObject.SetActive(false);

            // A barra: o preenchimento anda dentro do trilho (a âncora da direita é o tempo que passou).
            var barFrame = Picture(panel.transform, "Bar", Load(Wood("bars/bar_frame_large")));
            barFrame.type = Image.Type.Sliced;
            Place(barFrame.rectTransform, BarLeft, BarTop, PanelWidth - 2f * BarLeft, BarHeight);
            var track = new GameObject("Track", typeof(RectTransform));
            track.transform.SetParent(barFrame.transform, false);
            var trackRect = (RectTransform)track.transform;
            trackRect.anchorMin = Vector2.zero;
            trackRect.anchorMax = Vector2.one;
            trackRect.offsetMin = new Vector2(3f, 3f);
            trackRect.offsetMax = new Vector2(-3f, -3f);
            var fill = Picture(track.transform, "Fill", Load(Wood("bars/bar_fill_xp_large")));
            fill.type = Image.Type.Sliced;
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;

            // A recompensa: o raio e "Recompensa: Velocidade ×2,5 por 30 s", centralizados juntos.
            var reward = new GameObject("Reward", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            reward.transform.SetParent(panel.transform, false);
            Place((RectTransform)reward.transform, 8f, RewardTop, PanelWidth - 16f, 12f);
            var layout = reward.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 3f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            HudSkinSetup.Icon(reward.transform, AssetDatabase.LoadAssetAtPath<Sprite>(BoltIconPath), 12f, layout: true);
            var rewardLine = Text(reward.transform, "Text", SmallText, AmberText, TextAlignmentOptions.MidlineLeft, _font);

            // RECOMPENSA, em rubi, só ao fim (D-228: sem Cancelar).
            var buttonGo = new GameObject("RewardButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(panel.transform, false);
            Place((RectTransform)buttonGo.transform, (PanelWidth - ButtonWidth) / 2f, ButtonTop, ButtonWidth, ButtonHeight);
            var buttonImage = buttonGo.GetComponent<Image>();
            buttonImage.sprite = Load(Fantasy("buttons/button_ruby_normal"));
            buttonImage.type = Image.Type.Sliced;
            var button = buttonGo.GetComponent<Button>();
            button.targetGraphic = buttonImage;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { pressedSprite = Load(Fantasy("buttons/button_ruby_pressed")) };
            var rewardLabel = Text(buttonGo.transform, "Label", SmallText, TextColor, TextAlignmentOptions.Center, _font);
            Stretch(rewardLabel.rectTransform);
            buttonGo.SetActive(false);

            screen.SetActive(false);
            var view = root.AddComponent<SimulatedAdScreen>();
            GrayboxSetup.SetReferences(view, ("runner", runner), ("catalog", catalog), ("screen", screen), ("title", title),
                ("icon", iconRect), ("waiting", waiting), ("waitLabel", waitLabel), ("seconds", seconds),
                ("doneLabel", done), ("barFill", fillRect), ("rewardLine", rewardLine), ("rewardButton", button),
                ("rewardLabel", rewardLabel));
            Debug.Log("[Setup] Anúncio simulado na cena Game, por cima da janela do raro e antes da loja (M9b E5, D-227, D-228).");
            return true;
        }

        /// <summary>O AD da coluna (D-230): sai o "Em breve"; madeira, a TV e "AD"; o toque abre o anúncio.</summary>
        static bool EnsureButton(Transform safeArea, MiningRunner runner, BuffCatalog catalog)
        {
            var ad = safeArea.Find("AdButton") as RectTransform;
            var screen = safeArea.Find(ScreenName)?.GetComponent<SimulatedAdScreen>();
            if (ad == null || screen == null || ad.GetComponent<AdButtonView>() != null) return false;
            var normal = Load(Wood("buttons/button_square_normal"));
            if (normal == null) return false;

            if (ad.TryGetComponent<ComingSoonButton>(out var comingSoon)) Object.DestroyImmediate(comingSoon);
            var notice = ad.Find("Notice");
            if (notice != null) Object.DestroyImmediate(notice.gameObject);

            var background = ad.GetComponent<Image>();
            background.sprite = normal;
            var button = ad.GetComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { pressedSprite = Load(Wood("buttons/button_square_pressed")) };

            var label = ad.Find("Label")?.GetComponent<TMP_Text>();
            if (label == null) return false;
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AdIconPath);
            var icon = ad.Find("Icon")?.GetComponent<Image>();
            if (icon == null && sprite != null) icon = HudSkinSetup.Icon(ad, sprite, HudSkinSetup.IconSize(sprite), layout: false);
            if (icon == null) return false;
            icon.sprite = sprite;
            icon.gameObject.SetActive(true);
            HudSkinSetup.PlaceSideIcon(icon.rectTransform);
            HudSkinSetup.Band(label.rectTransform, top: HudSkinSetup.SideLabelTop, height: 12f);
            label.color = HudSkinSetup.LabelColor;
            label.text = catalog.AdButton;

            var view = ad.gameObject.AddComponent<AdButtonView>();
            GrayboxSetup.SetReferences(view, ("runner", runner), ("catalog", catalog), ("button", button),
                ("background", background), ("icon", icon), ("label", label), ("rewardedAd", screen),
                ("normalSprite", normal), ("disabledSprite", Load(Wood("buttons/button_square_disabled"))));
            EditorUtility.SetDirty(ad.gameObject);
            Debug.Log("[Setup] Botão AD abre o anúncio simulado (sai o \"Em breve\", M9b E5, D-230).");
            return true;
        }

        /// <summary>O raio acima do anão enquanto o boost vale (D-231), entre o combo e as lascas que voam.</summary>
        static bool EnsureBolt(MiningRunner runner)
        {
            var effects = GameObject.Find("Mining")?.transform.Find("Effects");
            var stage = Object.FindAnyObjectByType<DescentStage>(FindObjectsInactive.Include);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BoltIconPath);
            if (effects == null || stage == null || sprite == null || effects.Find(BoltName) != null) return false;

            var root = new GameObject(BoltName);
            root.transform.SetParent(effects, false);
            var bolt = GrayboxSetup.CreateSprite("Bolt", root.transform, sprite, Color.white, Vector3.zero, Vector3.one);
            bolt.sortingOrder = 31100;
            var view = root.AddComponent<AdBoostBolt>();
            GrayboxSetup.SetReferences(view, ("runner", runner), ("stage", stage), ("bolt", bolt));
            Debug.Log("[Setup] Raio do boost do AD acima do anão, em Mining/Effects (M9b E5, D-231).");
            return true;
        }
    }
}
