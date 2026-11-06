using System.IO;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="GrayboxSetup"/> para o Marco 8b, E4 (D-155, D-154), com a cena Game aberta e já com a pele
    /// (roda depois do <see cref="RestSetup"/>): no catálogo, os nomes das abas e as 5 linhas das permanentes; o prefab da
    /// linha das permanentes (a da loja com o preço em lascas vermelhas e o botão mais estreito); na loja, a barra de abas,
    /// a lista das permanentes e o banco na barra de título; no resumo do dia, o PERMANENTES. Medidas do mockup aprovado
    /// pelo Bruno em 2026-11-05. Só cria o que falta; não sobrescreve textos, ícones ou molduras mudados à mão.
    /// </summary>
    static class PermanentShopSetup
    {
        const string CatalogPath = "Assets/_Project/Data/ShopCatalog.asset";
        const string TextsPath = "Assets/_Project/Data/FeedbackTexts.asset";
        const string RowPrefabPath = "Assets/_Project/Prefabs/UI/UpgradeRow.prefab";
        const string PermanentRowPrefabPath = "Assets/_Project/Prefabs/UI/PermanentRow.prefab";
        const string CrystalIconPath = "Assets/_Project/Art/Crystals/icon_crystal.png";

        // A barra de abas fica entre a barra de título e a lista (que desce de 20 para 40 px do topo do painel).
        const float TabsTop = 19f;
        const float TabsHeight = 19f;
        const float RowsTop = 40f;
        const float TabsSide = 7f;
        const float TabSpacing = 4f;
        const float TabBadgeSize = 10f;
        const float TabBadgeOffset = 3f;
        const float TabLabelSize = 33f / 4f;

        // A linha das permanentes: o preço tem no máximo 3 algarismos (522), então o botão perde 16 px e os textos ganham.
        const float NarrowerBuy = 16f;
        const float PriceIconSize = 10f;
        const float BalanceIconSize = 11f;

        const float SummaryButtonOffset = 55f;

        static readonly Color TextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        static readonly Color MutedText = new Color32(0xBE, 0xB4, 0xAA, 0xFF);
        /// <summary>O cristal do jogo tingido de vermelho faz a lasca vermelha (o mesmo do <see cref="RestSetup"/>).</summary>
        static readonly Color RedShard = new Color(1f, 0.32f, 0.3f);
        static readonly Color DimRedShard = new Color(0.55f, 0.35f, 0.33f);

        public static bool EnsureInScene()
        {
            var safeArea = GameObject.Find("SafeArea")?.transform;
            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(CatalogPath);
            if (safeArea == null || catalog == null) return false;

            EnsureCatalog(catalog);
            var rowPrefab = EnsureRowPrefab();
            if (rowPrefab == null) return false;

            bool changed = EnsureTabs(safeArea, catalog, rowPrefab);
            changed |= EnsureSummaryButton(safeArea);
            return changed;
        }

        // ── Catálogo ─────────────────────────────────────────────────────────────────────────────────────────────

        static void EnsureCatalog(ShopCatalog catalog)
        {
            var so = new SerializedObject(catalog);
            bool changed = false;
            if (string.IsNullOrEmpty(so.FindProperty("runTabLabel").stringValue))
            {
                so.FindProperty("runTabLabel").stringValue = "MELHORIAS";
                so.FindProperty("permanentsTabLabel").stringValue = "PERMANENTES";
                changed = true;
            }

            var entries = so.FindProperty("permanentEntries");
            if (entries.arraySize == 0)
            {
                // A ordem da tabela do GDD v7 §4.2 (D-158). Os valores são o que a linha soma: cristais, fração, níveis.
                Add(entries, PermanentId.Memory, "Memória", "Mapa −{0} » −{1} cristais", "Mapa −{0} cristais",
                    ShopCatalog.EffectStyle.Number, 298, "slot_rarity_rare");
                Add(entries, PermanentId.MountainBlood, "Sangue da Montanha", "Dano {0} » {1}", "Dano {0}",
                    ShopCatalog.EffectStyle.ChancePercent, 302, "slot_rarity_legendary");
                Add(entries, PermanentId.BurningHeart, "Coração Ardente", "Lascas {0} » {1}", "Lascas {0}",
                    ShopCatalog.EffectStyle.ChancePercent, 257, "slot_rarity_legendary");
                Add(entries, PermanentId.NervousFingers, "Dedos Nervosos", "Crítico {0} » {1}", "Crítico {0}",
                    ShopCatalog.EffectStyle.ChancePercent, 202, "slot_rarity_epic");
                Add(entries, PermanentId.DwarfSoul, "Alma dos Anões", "Picareta começa no {0} » {1}", "Picareta começa no {0}",
                    ShopCatalog.EffectStyle.Number, 166, "slot_rarity_epic");
                changed = true;
            }

            // Os ícones do pacote (fora do git, D-65) entram quando o pacote está no projeto.
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                var (icon, frame) = ArtFor((PermanentId)entry.FindPropertyRelative("permanent").enumValueIndex);
                changed |= SetIfEmpty(entry.FindPropertyRelative("icon"), icon);
                changed |= SetIfEmpty(entry.FindPropertyRelative("frame"), frame);
            }

            if (!changed) return;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Abas e linhas das permanentes no catálogo da loja (Marco 8b, E4, D-155).");
        }

        static void Add(SerializedProperty entries, PermanentId id, string title, string effect, string maxed,
                        ShopCatalog.EffectStyle style, int icon, string frame)
        {
            int i = entries.arraySize;
            entries.InsertArrayElementAtIndex(i);
            var entry = entries.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("permanent").enumValueIndex = (int)id;
            entry.FindPropertyRelative("title").stringValue = title;
            entry.FindPropertyRelative("effectFormat").stringValue = effect;
            entry.FindPropertyRelative("maxedFormat").stringValue = maxed;
            entry.FindPropertyRelative("style").enumValueIndex = (int)style;
            entry.FindPropertyRelative("icon").objectReferenceValue = null;
            entry.FindPropertyRelative("frame").objectReferenceValue = null;
        }

        /// <summary>
        /// Ícone e moldura de cada linha (do mockup): Memória livro verde (raro), Sangue poção vermelha e Coração colar
        /// de coração (lendários), Dedos anéis e Alma elmo com chifres (épicos).
        /// </summary>
        internal static (Sprite icon, Sprite frame) ArtFor(PermanentId id)
        {
            switch (id)
            {
                case PermanentId.Memory: return (Icon(298), Frame("slot_rarity_rare"));
                case PermanentId.MountainBlood: return (Icon(302), Frame("slot_rarity_legendary"));
                case PermanentId.BurningHeart: return (Icon(257), Frame("slot_rarity_legendary"));
                case PermanentId.NervousFingers: return (Icon(202), Frame("slot_rarity_epic"));
                case PermanentId.DwarfSoul: return (Icon(166), Frame("slot_rarity_epic"));
                // M7 (E3, D-174): Treinar a manopla (raro), Promoção o elmo dourado (lendário).
                case PermanentId.InternTraining: return (Icon(212), Frame("slot_rarity_rare"));
                case PermanentId.InternPromotion: return (Icon(155), Frame("slot_rarity_legendary"));
                default: return (null, null);
            }
        }

        static bool SetIfEmpty(SerializedProperty property, Object value)
        {
            if (value == null || property.objectReferenceValue != null) return false;
            property.objectReferenceValue = value;
            return true;
        }

        // ── Linha das permanentes (prefab) ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Uma cópia da linha da loja (já com a pele e as fontes) com a <see cref="PermanentRowView"/> no lugar da
        /// <see cref="UpgradeRowView"/>, o botão 16 px mais estreito e o cristal vermelho ao lado do preço.
        /// </summary>
        static PermanentRowView EnsureRowPrefab()
        {
            if (!File.Exists(PermanentRowPrefabPath))
            {
                if (!File.Exists(RowPrefabPath) || !AssetDatabase.CopyAsset(RowPrefabPath, PermanentRowPrefabPath)) return null;
                BuildRowPrefab();
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(PermanentRowPrefabPath).GetComponent<PermanentRowView>();
        }

        static void BuildRowPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(PermanentRowPrefabPath);
            try
            {
                root.name = "PermanentRow";
                var old = root.GetComponent<UpgradeRowView>();
                var oldSo = new SerializedObject(old);
                var buyableColor = oldSo.FindProperty("buyableLabelColor").colorValue;
                var unavailableColor = oldSo.FindProperty("unavailableLabelColor").colorValue;
                Object.DestroyImmediate(old);

                var buy = (RectTransform)root.transform.Find("Buy");
                buy.sizeDelta = new Vector2(buy.sizeDelta.x - NarrowerBuy, buy.sizeDelta.y);
                foreach (var name in new[] { "Title", "Level", "Effect" })
                {
                    var rect = (RectTransform)root.transform.Find(name);
                    rect.offsetMax = new Vector2(rect.offsetMax.x + NarrowerBuy, rect.offsetMax.y);
                }

                // O preço e o cristal numa linha centrada no botão; o texto continua "Buy/Label" (a fonte dos números).
                var layout = buy.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 2f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = layout.childForceExpandHeight = false;

                var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                icon.transform.SetParent(buy, false);
                icon.transform.SetAsFirstSibling();
                var iconImage = icon.GetComponent<Image>();
                iconImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CrystalIconPath);
                iconImage.color = RedShard;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                var element = icon.GetComponent<LayoutElement>();
                element.minWidth = element.preferredWidth = element.minHeight = element.preferredHeight = PriceIconSize;

                var view = root.AddComponent<PermanentRowView>();
                GrayboxSetup.SetReferences(view,
                    ("icon", root.transform.Find("Icon").GetComponent<Image>()),
                    ("frame", root.transform.Find("Frame").GetComponent<Image>()),
                    ("title", root.transform.Find("Title").GetComponent<TMP_Text>()),
                    ("level", root.transform.Find("Level").GetComponent<TMP_Text>()),
                    ("effect", root.transform.Find("Effect").GetComponent<TMP_Text>()),
                    ("buyButton", buy.GetComponent<Button>()),
                    ("buyLabel", buy.Find("Label").GetComponent<TMP_Text>()),
                    ("priceIcon", iconImage),
                    ("group", root.GetComponent<CanvasGroup>()),
                    ("maxedFrame", Frame("slot_disabled")));
                var so = new SerializedObject(view);
                so.FindProperty("buyableLabelColor").colorValue = buyableColor;
                so.FindProperty("unavailableLabelColor").colorValue = unavailableColor;
                so.FindProperty("buyableIconColor").colorValue = RedShard;
                so.FindProperty("unavailableIconColor").colorValue = DimRedShard;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PermanentRowPrefabPath);
                Debug.Log($"[Setup] Prefab da linha das permanentes criado em {PermanentRowPrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ── As abas na loja (cena) ──────────────────────────────────────────────────────────────────────────────

        static bool EnsureTabs(Transform safeArea, ShopCatalog catalog, PermanentRowView rowPrefab)
        {
            var shop = safeArea.Find("Shop");
            var panel = shop != null ? shop.Find("Screen/Panel") : null;
            if (panel == null || panel.Find("Tabs") != null) return false;

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.GameFontPath);
            var numbers = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.NumbersFontPath);
            var shopBadge = shop.Find("ShopButton/Badge");

            var rows = (RectTransform)panel.Find("Rows");
            rows.offsetMax = new Vector2(rows.offsetMax.x, -RowsTop);

            var tabs = new GameObject("Tabs", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabs.transform.SetParent(panel, false);
            tabs.transform.SetSiblingIndex(rows.GetSiblingIndex());
            var tabsRect = (RectTransform)tabs.transform;
            tabsRect.anchorMin = new Vector2(0f, 1f);
            tabsRect.anchorMax = Vector2.one;
            tabsRect.pivot = new Vector2(0.5f, 1f);
            tabsRect.offsetMin = new Vector2(TabsSide, -TabsTop - TabsHeight);
            tabsRect.offsetMax = new Vector2(-TabsSide, -TabsTop);
            var layout = tabs.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = TabSpacing;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;

            var (runTab, runLabel, runBadge) = Tab(tabs.transform, "RunTab", catalog.RunTabLabel, font, shopBadge);
            var (permanentsTab, permanentsLabel, permanentsBadge) =
                Tab(tabs.transform, "PermanentsTab", catalog.PermanentsTabLabel, font, shopBadge);

            // A lista das permanentes: no mesmo lugar e com o mesmo layout da lista da run.
            var permanentRows = new GameObject("PermanentRows", typeof(RectTransform), typeof(VerticalLayoutGroup));
            permanentRows.transform.SetParent(panel, false);
            permanentRows.transform.SetSiblingIndex(rows.GetSiblingIndex() + 1);
            var permanentRect = (RectTransform)permanentRows.transform;
            permanentRect.anchorMin = rows.anchorMin;
            permanentRect.anchorMax = rows.anchorMax;
            permanentRect.pivot = rows.pivot;
            permanentRect.offsetMin = rows.offsetMin;
            permanentRect.offsetMax = rows.offsetMax;
            var runLayout = rows.GetComponent<VerticalLayoutGroup>();
            var permanentLayout = permanentRows.GetComponent<VerticalLayoutGroup>();
            permanentLayout.spacing = runLayout.spacing;
            permanentLayout.padding = runLayout.padding;
            permanentLayout.childAlignment = runLayout.childAlignment;
            permanentLayout.childControlWidth = runLayout.childControlWidth;
            permanentLayout.childForceExpandWidth = runLayout.childForceExpandWidth;
            permanentLayout.childControlHeight = runLayout.childControlHeight;
            permanentLayout.childForceExpandHeight = runLayout.childForceExpandHeight;
            permanentRows.SetActive(false);

            // O banco na barra de título, no lugar das lascas: o cristal vermelho e o número, alinhados à direita.
            var balance = panel.Find("Balance").GetComponent<TMP_Text>();
            var redBalance = new GameObject("RedBalance", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            redBalance.transform.SetParent(panel, false);
            redBalance.transform.SetSiblingIndex(balance.transform.GetSiblingIndex() + 1);
            var redRect = (RectTransform)redBalance.transform;
            var balanceRect = balance.rectTransform;
            redRect.anchorMin = balanceRect.anchorMin;
            redRect.anchorMax = balanceRect.anchorMax;
            redRect.pivot = balanceRect.pivot;
            redRect.offsetMin = balanceRect.offsetMin;
            redRect.offsetMax = balanceRect.offsetMax;
            var redLayout = redBalance.GetComponent<HorizontalLayoutGroup>();
            redLayout.spacing = 2f;
            redLayout.childAlignment = TextAnchor.MiddleRight;
            redLayout.childControlWidth = redLayout.childControlHeight = true;
            redLayout.childForceExpandWidth = redLayout.childForceExpandHeight = false;

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            icon.transform.SetParent(redBalance.transform, false);
            var iconImage = icon.GetComponent<Image>();
            iconImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CrystalIconPath);
            iconImage.color = RedShard;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            var element = icon.GetComponent<LayoutElement>();
            element.minWidth = element.preferredWidth = element.minHeight = element.preferredHeight = BalanceIconSize;

            var value = Text(redBalance.transform, "Value", balance.fontSize, balance.color, TextAlignmentOptions.MidlineRight,
                             numbers != null ? numbers : balance.font);
            redBalance.SetActive(false);

            var view = shop.GetComponent<ShopView>();
            GrayboxSetup.SetReferences(view,
                ("runTabButton", runTab), ("runTabLabel", runLabel), ("runTabBadge", runBadge),
                ("permanentsTabButton", permanentsTab), ("permanentsTabLabel", permanentsLabel),
                ("permanentsTabBadge", permanentsBadge),
                ("activeTabSprite", Wood("buttons/button_large_normal")),
                ("inactiveTabSprite", Wood("buttons/button_large_disabled")),
                ("permanentRowPrefab", rowPrefab), ("permanentRowsParent", permanentRows.transform),
                ("redBalance", redBalance), ("redBalanceValue", value));
            var so = new SerializedObject(view);
            so.FindProperty("activeTabTextColor").colorValue = TextColor;
            so.FindProperty("inactiveTabTextColor").colorValue = MutedText;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[Setup] Abas MELHORIAS e PERMANENTES na loja (Marco 8b, E4, D-155).");
            return true;
        }

        /// <summary>Uma aba: a madeira (o sprite muda com a aba aberta), o nome e um "!" como o do botão LOJA, no canto.</summary>
        internal static (Button button, TMP_Text label, GameObject badge) Tab(Transform parent, string name, string text,
                                                                       TMP_FontAsset font, Transform shopBadge)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = Wood("buttons/button_large_disabled");
            image.type = Image.Type.Sliced;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None; // a aba aberta é o retorno do toque

            var label = Text(go.transform, "Label", TabLabelSize, TextColor, TextAlignmentOptions.Center, font);
            label.text = text;

            GameObject badge;
            if (shopBadge != null)
            {
                badge = Object.Instantiate(shopBadge.gameObject, go.transform, false);
                badge.name = "Badge";
            }
            else
            {
                badge = new GameObject("Badge", typeof(RectTransform), typeof(Image));
                badge.transform.SetParent(go.transform, false);
                badge.GetComponent<Image>().raycastTarget = false;
            }
            var badgeRect = (RectTransform)badge.transform;
            badgeRect.anchorMin = badgeRect.anchorMax = badgeRect.pivot = Vector2.one;
            badgeRect.sizeDelta = new Vector2(TabBadgeSize, TabBadgeSize);
            badgeRect.anchoredPosition = new Vector2(TabBadgeOffset, TabBadgeOffset);
            badge.SetActive(false);
            return (button, label, badge);
        }

        // ── O PERMANENTES do resumo do dia (D-154) ──────────────────────────────────────────────────────────────

        /// <summary>COMEÇAR vai para a direita e PERMANENTES (madeira, uma cópia dele) entra à esquerda.</summary>
        static bool EnsureSummaryButton(Transform safeArea)
        {
            var summary = safeArea.Find("DaySummary");
            var panel = summary != null ? summary.Find("Screen/Panel") : null;
            var shop = safeArea.Find("Shop");
            var texts = AssetDatabase.LoadAssetAtPath<FeedbackTexts>(TextsPath);
            if (panel == null || shop == null || panel.Find("Permanents") != null) return false;

            var start = (RectTransform)panel.Find("Start");
            start.anchoredPosition = new Vector2(SummaryButtonOffset, start.anchoredPosition.y);

            var go = Object.Instantiate(start.gameObject, panel, false);
            go.name = "Permanents";
            go.transform.SetSiblingIndex(start.GetSiblingIndex());
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = new Vector2(-SummaryButtonOffset, start.anchoredPosition.y);
            go.GetComponent<Image>().sprite = Wood("buttons/button_large_normal");
            var button = go.GetComponent<Button>();
            button.spriteState = new SpriteState { pressedSprite = Wood("buttons/button_large_pressed") };
            var label = go.transform.Find("Label").GetComponent<TMP_Text>();
            label.text = texts != null ? texts.SummaryPermanents : "";

            GrayboxSetup.SetReferences(summary.GetComponent<DaySummaryScreen>(),
                ("shop", shop.GetComponent<ShopView>()), ("permanentsButton", button), ("permanentsLabel", label));
            Debug.Log("[Setup] PERMANENTES no resumo do dia (Marco 8b, E4, D-154).");
            return true;
        }

        // ── Peças ───────────────────────────────────────────────────────────────────────────────────────────────

        static TMP_Text Text(Transform parent, string name, float size, Color color, TextAlignmentOptions alignment,
                             TMP_FontAsset font)
        {
            var text = GrayboxSetup.CreateHudText(parent, name, "", size, color, Vector2.zero, Vector2.one);
            text.alignment = alignment;
            if (font != null)
            {
                text.font = font;
                text.fontSharedMaterial = font.material;
            }
            return text;
        }

        static Sprite Wood(string name) => Load(UiSkinSetup.WoodFolder + "/" + name + ".png");
        internal static Sprite Frame(string name) => Wood("slots/" + name);
        static Sprite Icon(int number) => Load(UiSkinSetup.IconsFolder + "/Icon" + number + ".png");
        static Sprite Load(string path) => File.Exists(path) ? AssetDatabase.LoadAssetAtPath<Sprite>(path) : null;
    }
}
