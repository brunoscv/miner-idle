using MinersIdle.Game;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="GrayboxSetup"/> para o Marco 8b (E3, D-152…D-154, D-157), com a cena Game aberta e já com a
    /// pele (roda depois do <see cref="UiSkinSetup"/>): o botão Descansar no lado esquerdo da barra de baixo, o modal de
    /// Descansar e o resumo do dia na janela da loja em tamanho menor, o DESCANSAR da tela do coração e o botão DBG acima
    /// da barra. Medidas do mockup aprovado pelo Bruno em 2026-11-05. Só cria o que falta; os textos vêm dos
    /// <see cref="FeedbackTexts"/>.
    /// </summary>
    static class RestSetup
    {
        const string TextsPath = "Assets/_Project/Data/FeedbackTexts.asset";
        const string CrystalIconPath = "Assets/_Project/Art/Crystals/icon_crystal.png";
        const string ModalName = "RestScreen";
        const string SummaryName = "DaySummary";
        const string BarName = "Rest";

        const float ModalWidth = 236f;
        const float ModalHeight = 190f;
        const float ModalRaise = 12f;          // um pouco acima do meio: longe da barra de baixo
        const float TitleBarTop = 6f;
        const float TitleBarHeight = 8f;
        const float ModalButtonWidth = 100f;
        const float ModalButtonHeight = 26f;
        const float ModalButtonOffset = 55f;
        const float ModalButtonBottom = 10f;
        const float LinePad = 6f;
        const float LineHeight = 14f;
        const float RedIconSize = 14f;
        const float BarIconSize = 24f;

        // Pixelify no grid da fonte (8,25 e 11); a Press Start em múltiplos de 2 (nítida no aparelho).
        const float SmallText = 33f / 4f;
        const float BigText = 11f;
        const float RowNumber = 6f;
        const float TakeNumber = 10f;
        const float SummaryNumber = 8f;
        const float BarCountSize = 8f;

        static readonly Color DimColor = new Color(0f, 0f, 0f, 0.6f);   // o da loja
        static readonly Color TextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        static readonly Color AmberText = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        static readonly Color MutedText = new Color32(0xBE, 0xB4, 0xAA, 0xFF);
        /// <summary>O cristal do jogo tingido de vermelho faz a lasca vermelha (as fontes não têm o ◆).</summary>
        static readonly Color RedShard = new Color(1f, 0.32f, 0.3f);
        static readonly Color BarCountColor = new Color32(0xFF, 0x8F, 0x80, 0xFF);

        static TMP_FontAsset _font, _numbers;

        public static bool EnsureInScene()
        {
            var safeArea = GameObject.Find("SafeArea")?.transform;
            var texts = AssetDatabase.LoadAssetAtPath<FeedbackTexts>(TextsPath);
            if (safeArea == null || texts == null || Load(Wood("panels/window")) == null) return false;
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.GameFontPath);
            _numbers = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.NumbersFontPath);

            bool changed = EnsureModal(safeArea, texts); // logo depois da tela do coração
            changed |= EnsureBarButton(safeArea, texts);
            changed |= EnsureHeartRest(safeArea);
            changed |= DebugPanelSetup.LiftAboveTheBar(safeArea);
            return changed;
        }

        // ── A tela do descanso (M9d E2, D-265, D-279) ───────────────────────────────────────────────────────────

        const string RestShopName = "RestShop";
        const string CatalogPath = "Assets/_Project/Data/ShopCatalog.asset";
        const string PermanentRowPrefabPath = "Assets/_Project/Prefabs/UI/PermanentRow.prefab";
        const float RestShopMargin = 14f;        // do alto e de baixo da tela
        const float RestHeaderHeight = 52f;      // o quadro do resumo
        const float RestNoteTop = 56f;
        const float RestListTop = 74f;
        static readonly Color BlurTint = new Color(0.42f, 0.42f, 0.42f, 1f);
        static readonly (string text, float size, Vector2 at)[] ZzzLetters =
        {
            ("Z", 12f, new Vector2(-26f, -24f)), ("Z", 18f, new Vector2(2f, 2f)), ("Z", 26f, new Vector2(34f, 34f)),
        };

        /// <summary>
        /// M9d E2: a tela do descanso, no lugar do resumo do dia (que sai, com o COMEÇAR e o PERMANENTES). Fundo preto
        /// (ou o jogo desfocado, D-279), o ZZZ e o painel da largura da LOJA: o dia e o banco na barra de título, o quadro
        /// do resumo, o aviso, a lista das permanentes com rolagem e INICIAR. Por cima de tudo do jogo, abaixo do DBG.
        /// Depois da LOJA e da rolagem dela (usa a linha das permanentes e copia o layout da lista).
        /// </summary>
        public static bool EnsureRestShop()
        {
            var safeArea = GameObject.Find("SafeArea")?.transform;
            var texts = AssetDatabase.LoadAssetAtPath<FeedbackTexts>(TextsPath);
            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(CatalogPath);
            var rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PermanentRowPrefabPath)?.GetComponent<PermanentRowView>();
            var shopPanel = safeArea != null ? safeArea.Find("Shop/Screen/Panel") as RectTransform : null;
            var shopRows = shopPanel != null ? shopPanel.Find("PermanentScroll/PermanentRows") : null;
            if (texts == null || catalog == null || rowPrefab == null || shopRows == null) return false;

            bool changed = RemoveOldSummary(safeArea);
            var existing = safeArea.Find(RestShopName);
            if (existing != null)
                return DayDebugSetup.EnsureRestBackground(existing.GetComponent<RestShopScreen>()) | ZzzInGameFont(existing) |
                       BlurredBackground(existing.GetComponent<RestShopScreen>()) | changed;

            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.GameFontPath);
            _numbers = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.NumbersFontPath);

            var root = new GameObject(RestShopName, typeof(RectTransform));
            root.transform.SetParent(safeArea, false);
            root.transform.SetAsLastSibling();
            Stretch((RectTransform)root.transform);

            // O fundo preto bloqueia o jogo todo; o desfocado fica por cima dele, desligado até ser escolhido.
            var screen = new GameObject("Screen", typeof(RectTransform), typeof(Image));
            screen.transform.SetParent(root.transform, false);
            Stretch((RectTransform)screen.transform);
            screen.GetComponent<Image>().color = Color.black;

            var blurredGo = new GameObject("Blurred", typeof(RectTransform), typeof(RawImage));
            blurredGo.transform.SetParent(screen.transform, false);
            Stretch((RectTransform)blurredGo.transform);
            var blurred = blurredGo.GetComponent<RawImage>();
            blurred.color = BlurTint;
            blurred.raycastTarget = false;
            blurredGo.SetActive(false);

            // O ZZZ: a tela toda é o botão que pula.
            var zzz = new GameObject("Zzz", typeof(RectTransform), typeof(Image), typeof(Button));
            zzz.transform.SetParent(screen.transform, false);
            Stretch((RectTransform)zzz.transform);
            zzz.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            var skip = zzz.GetComponent<Button>();
            skip.transition = Selectable.Transition.None;
            var letters = new TMP_Text[ZzzLetters.Length];
            for (int i = 0; i < ZzzLetters.Length; i++)
            {
                var (letter, size, at) = ZzzLetters[i];
                var text = Text(zzz.transform, "Z" + (i + 1), letter, size, AmberText, TextAlignmentOptions.Center, _font);
                var rect = text.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(40f, 40f);
                rect.anchoredPosition = at;
                text.raycastTarget = false;
                letters[i] = text;
            }
            zzz.SetActive(false);

            // O painel da largura da LOJA, da altura da tela.
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(screen.transform, false);
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = new Vector2(shopPanel.anchorMin.x, 0f);
            panelRect.anchorMax = new Vector2(shopPanel.anchorMax.x, 1f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.offsetMin = new Vector2(shopPanel.offsetMin.x, RestShopMargin);
            panelRect.offsetMax = new Vector2(shopPanel.offsetMax.x, -RestShopMargin);
            var image = panel.GetComponent<Image>();
            image.sprite = Load(Wood("panels/window"));
            image.type = Image.Type.Sliced;

            var title = Text(panel.transform, "Title", "", SmallText, TextColor, TextAlignmentOptions.MidlineLeft, _font);
            TitleBar(title.rectTransform, 10f, 0f);
            var bank = RedValue(panel.transform, "Bank", 0f, RowNumber);
            TitleBar((RectTransform)bank.transform.parent, 0f, 4f);

            var body = new GameObject("Body", typeof(RectTransform));
            body.transform.SetParent(panel.transform, false);
            var bodyRect = (RectTransform)body.transform;
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(7f, 42f);
            bodyRect.offsetMax = new Vector2(-7f, -22f);

            Inset(body.transform, 0f, RestHeaderHeight);
            var (levelLabel, levelValue) = Row(body.transform, 4f);
            var (timeLabel, timeValue) = Row(body.transform, 18f);
            var gainedLabel = Line(body.transform, "GainedLabel", 34f, SmallText, TextColor, TextAlignmentOptions.Left);
            var gainedValue = RedValue(body.transform, "Gained", 32f, RowNumber);
            var note = Line(body.transform, "Note", RestNoteTop, SmallText, MutedText, TextAlignmentOptions.Center);

            // A lista com rolagem, com o layout da lista da LOJA.
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect));
            scrollGo.transform.SetParent(body.transform, false);
            var viewport = (RectTransform)scrollGo.transform;
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = new Vector2(0f, -RestListTop);
            var rows = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            rows.transform.SetParent(viewport, false);
            var rowsRect = (RectTransform)rows.transform;
            rowsRect.anchorMin = new Vector2(0f, 1f);
            rowsRect.anchorMax = Vector2.one;
            rowsRect.pivot = new Vector2(0.5f, 1f);
            rowsRect.offsetMin = rowsRect.offsetMax = Vector2.zero;
            var shopLayout = shopRows.GetComponent<VerticalLayoutGroup>();
            var layout = rows.GetComponent<VerticalLayoutGroup>();
            layout.spacing = shopLayout.spacing;
            layout.padding = new RectOffset(shopLayout.padding.left, shopLayout.padding.right, shopLayout.padding.top,
                                            shopLayout.padding.bottom);
            layout.childAlignment = shopLayout.childAlignment;
            layout.childControlWidth = shopLayout.childControlWidth;
            layout.childControlHeight = shopLayout.childControlHeight;
            layout.childForceExpandWidth = shopLayout.childForceExpandWidth;
            layout.childForceExpandHeight = false;
            rows.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.content = rowsRect;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;

            var (start, startLabel) = ModalButton(panel.transform, "Start", 0f, ruby: true);

            screen.SetActive(false);
            var view = root.AddComponent<RestShopScreen>();
            GrayboxSetup.SetReferences(view, ("runner", Object.FindAnyObjectByType<MiningRunner>()), ("texts", texts),
                ("catalog", catalog), ("rowPrefab", rowPrefab), ("rowsParent", rows.transform), ("screen", screen),
                ("blurred", blurred), ("worldCamera", Camera.main), ("zzz", zzz), ("zzzSkip", skip), ("panel", panel),
                ("title", title), ("bankValue", bank), ("levelLabel", levelLabel), ("levelValue", levelValue),
                ("timeLabel", timeLabel), ("timeValue", timeValue), ("gainedLabel", gainedLabel),
                ("gainedValue", gainedValue), ("note", note), ("startButton", start), ("startLabel", startLabel),
                ("scroll", scroll));
            var so = new SerializedObject(view);
            var array = so.FindProperty("zzzLetters");
            array.arraySize = letters.Length;
            for (int i = 0; i < letters.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = letters[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            // O DBG continua por cima (o botão do fundo troca com a tela aberta).
            safeArea.Find("DebugPanel")?.SetAsLastSibling();
            DayDebugSetup.EnsureRestBackground(view);
            Debug.Log("[Setup] Tela do descanso: ZZZ, fundo preto ou desfocado, resumo, permanentes e INICIAR (M9d E2).");
            return true;
        }

        /// <summary>Os Z do ZZZ na fonte do jogo (não são números): a primeira montagem usou a dos números.</summary>
        static bool ZzzInGameFont(Transform restShop)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.GameFontPath);
            var zzz = restShop.Find("Screen/Zzz");
            if (font == null || zzz == null) return false;
            bool changed = false;
            foreach (var text in zzz.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font == font) continue;
                text.font = font;
                text.fontSharedMaterial = font.material;
                EditorUtility.SetDirty(text);
                changed = true;
            }
            if (changed) Debug.Log("[Setup] ZZZ na fonte do jogo (M9d E2).");
            return changed;
        }

        /// <summary>D-280: o Bruno escolheu o fundo desfocado; a cena montada antes abria na preta.</summary>
        static bool BlurredBackground(RestShopScreen view)
        {
            if (view == null) return false;
            var so = new SerializedObject(view);
            var background = so.FindProperty("background");
            if (background == null || background.enumValueIndex == (int)RestShopScreen.Background.Blurred) return false;
            background.enumValueIndex = (int)RestShopScreen.Background.Blurred;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Setup] Tela do descanso abre com o fundo desfocado (D-280).");
            return true;
        }

        /// <summary>M9d E2: o resumo do dia sai (a tela do descanso faz o papel dele, com o COMEÇAR e o PERMANENTES).</summary>
        static bool RemoveOldSummary(Transform safeArea)
        {
            var summary = safeArea.Find(SummaryName);
            if (summary == null) return false;
            Object.DestroyImmediate(summary.gameObject);
            Debug.Log("[Setup] Resumo do dia antigo removido (M9d E2).");
            return true;
        }

        // ── O modal de Descansar (D-153) ────────────────────────────────────────────────────────────────────────

        static bool EnsureModal(Transform safeArea, FeedbackTexts texts)
        {
            if (safeArea.Find(ModalName) != null) return false;

            var (root, screen, panel, title, day, body) = Window(safeArea, ModalName, ModalHeight);
            Inset(body, 0f, 58f);
            var (levelLabel, levelValue) = Row(body, 4f);
            var (timeLabel, timeValue) = Row(body, 18f);
            var (shardsLabel, shardsValue) = Row(body, 32f);
            var takeLabel = Line(body, "TakeLabel", 66f, BigText, TextColor, TextAlignmentOptions.Left);
            var takeValue = RedValue(body, "Take", 64f, TakeNumber);
            var next = Line(body, "Next", 86f, SmallText, MutedText, TextAlignmentOptions.Left);
            var note = Line(body, "Note", 102f, SmallText, MutedText, TextAlignmentOptions.TopLeft, 30f);
            note.textWrappingMode = TextWrappingModes.Normal;
            var (continueButton, continueLabel) = ModalButton(panel, "Continue", -ModalButtonOffset, ruby: false);
            var (confirm, confirmLabel) = ModalButton(panel, "Confirm", ModalButtonOffset, ruby: true);

            screen.SetActive(false);
            var view = root.AddComponent<RestScreen>();
            GrayboxSetup.SetReferences(view, ("runner", Object.FindAnyObjectByType<MiningRunner>()), ("texts", texts),
                ("screen", screen),
                ("title", title), ("day", day), ("levelLabel", levelLabel), ("levelValue", levelValue),
                ("timeLabel", timeLabel), ("timeValue", timeValue), ("shardsLabel", shardsLabel),
                ("shardsValue", shardsValue), ("takeLabel", takeLabel), ("takeValue", takeValue), ("next", next),
                ("note", note), ("continueButton", continueButton), ("continueLabel", continueLabel),
                ("confirmButton", confirm), ("confirmLabel", confirmLabel));
            Debug.Log("[Setup] Modal de Descansar na cena Game (Marco 8b, E3, D-153).");
            return true;
        }

        /// <summary>
        /// A janela da loja em tamanho menor: o fundo que escurece a tela toda (a barra também) e bloqueia o mapa, o
        /// painel de madeira no meio, o título à esquerda da barra de título e um texto à direita (o dia), e a área do
        /// conteúdo, onde as linhas ficam por posição a partir do topo.
        /// </summary>
        static (GameObject root, GameObject screen, Transform panel, TMP_Text title, TMP_Text right, Transform body)
            Window(Transform safeArea, string name, float height)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(safeArea, false);
            // Por cima da tela do coração (o DESCANSAR dela abre o modal) e de tudo antes dela.
            var heart = safeArea.Find("HeartScreen");
            if (heart != null) root.transform.SetSiblingIndex(heart.GetSiblingIndex() + 1);
            else root.transform.SetAsLastSibling();
            Stretch((RectTransform)root.transform);

            var screen = new GameObject("Screen", typeof(RectTransform), typeof(Image));
            screen.transform.SetParent(root.transform, false);
            Stretch((RectTransform)screen.transform);
            screen.GetComponent<Image>().color = DimColor; // raycastTarget ligado: bloqueia o mapa, não pausa nada

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(screen.transform, false);
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(ModalWidth, height);
            panelRect.anchoredPosition = new Vector2(0f, ModalRaise);
            var image = panel.GetComponent<Image>();
            image.sprite = Load(Wood("panels/window"));
            image.type = Image.Type.Sliced;

            var title = Text(panel.transform, "Title", "", SmallText, TextColor, TextAlignmentOptions.MidlineLeft, _font);
            TitleBar(title.rectTransform, 10f, 0f);
            var right = Text(panel.transform, "Value", "", RowNumber, TextColor, TextAlignmentOptions.MidlineRight, _numbers);
            TitleBar(right.rectTransform, 0f, 10f);

            var body = new GameObject("Body", typeof(RectTransform));
            body.transform.SetParent(panel.transform, false);
            var bodyRect = (RectTransform)body.transform;
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(8f, 42f);
            bodyRect.offsetMax = new Vector2(-8f, -22f);
            return (root, screen, panel.transform, title, right, body.transform);
        }

        static void Inset(Transform body, float top, float height)
        {
            var go = new GameObject("Inset", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(body, false);
            go.transform.SetAsFirstSibling();
            var image = go.GetComponent<Image>();
            image.sprite = Load(Wood("panels/panel_inset"));
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            Top((RectTransform)go.transform, top, height);
        }

        /// <summary>Uma linha do quadro: o nome à esquerda e o número à direita, em âmbar.</summary>
        static (TMP_Text label, TMP_Text value) Row(Transform body, float top)
        {
            var label = Line(body, "Label", top + 2f, SmallText, TextColor, TextAlignmentOptions.Left);
            var value = Text(body, "Value", "", RowNumber, AmberText, TextAlignmentOptions.Right, _numbers);
            value.margin = new Vector4(LinePad, 0f, LinePad, 0f);
            Top(value.rectTransform, top + 4f, LineHeight - 2f);
            return (label, value);
        }

        static TMP_Text Line(Transform body, string name, float top, float size, Color color, TextAlignmentOptions alignment,
                             float height = LineHeight)
        {
            var text = Text(body, name, "", size, color, alignment, _font);
            text.margin = new Vector4(LinePad, 0f, LinePad, 0f);
            Top(text.rectTransform, top, height);
            return text;
        }

        /// <summary>
        /// Um número de lascas vermelhas à direita, com o cristal vermelho à esquerda dele: os dois numa linha alinhada à
        /// direita, que acompanha a largura do número.
        /// </summary>
        static TMP_Text RedValue(Transform body, string name, float top, float size)
        {
            var row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(body, false);
            Top((RectTransform)row.transform, top, 18f);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(0, (int)LinePad, 0, 0);
            layout.spacing = 3f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            icon.transform.SetParent(row.transform, false);
            var image = icon.GetComponent<Image>();
            image.sprite = Load(CrystalIconPath);
            image.color = RedShard;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var element = icon.GetComponent<LayoutElement>();
            element.minWidth = element.preferredWidth = element.minHeight = element.preferredHeight = RedIconSize;

            return Text(row.transform, "Value", "", size, TextColor, TextAlignmentOptions.MidlineRight, _numbers);
        }

        static (Button button, TMP_Text label) ModalButton(Transform panel, string name, float x, bool ruby)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(panel, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(x, ModalButtonBottom);
            rect.sizeDelta = new Vector2(ModalButtonWidth, ModalButtonHeight);
            var image = go.GetComponent<Image>();
            image.sprite = Load(ruby ? Fantasy("buttons/button_ruby_normal") : Wood("buttons/button_large_normal"));
            image.type = Image.Type.Sliced;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                pressedSprite = Load(ruby ? Fantasy("buttons/button_ruby_pressed") : Wood("buttons/button_large_pressed")),
            };
            var label = Text(go.transform, "Label", "", SmallText, TextColor, TextAlignmentOptions.Center, _font);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            return (button, label);
        }

        // ── O botão da barra de baixo (D-152) ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// O Descansar no lado esquerdo da barra, na madeira sem o âmbar do PASSAR, com o cristal vermelho, o nome e,
        /// embaixo dele, as lascas vermelhas que o jogador leva. O objeto "Rest" (sempre ativo) leva a view; o botão dentro
        /// dele só aparece quando dá para descansar.
        /// </summary>
        static bool EnsureBarButton(Transform safeArea, FeedbackTexts texts)
        {
            if (safeArea.Find(BarName) != null) return false;

            var container = new GameObject(BarName, typeof(RectTransform));
            container.transform.SetParent(safeArea, false);
            Stretch((RectTransform)container.transform);
            // Antes da loja na hierarquia: a loja aberta fica por cima.
            var shop = safeArea.Find("Shop");
            if (shop != null) container.transform.SetSiblingIndex(shop.GetSiblingIndex());

            var go = new GameObject("RestButton", typeof(RectTransform), typeof(Image), typeof(Button),
                                    typeof(HorizontalLayoutGroup));
            go.transform.SetParent(container.transform, false);
            var rect = (RectTransform)go.transform;
            HudSkinSetup.PlaceOnTheBar(rect, right: false);
            var image = go.GetComponent<Image>();
            image.sprite = Load(Wood("buttons/button_large_normal"));
            image.type = Image.Type.Sliced;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { pressedSprite = Load(Wood("buttons/button_large_pressed")) };

            var row = go.GetComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(8, 8, 0, 0);
            row.spacing = 4f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            icon.transform.SetParent(go.transform, false);
            var iconImage = icon.GetComponent<Image>();
            iconImage.sprite = Load(CrystalIconPath);
            iconImage.color = RedShard;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            var element = icon.GetComponent<LayoutElement>();
            element.minWidth = element.preferredWidth = element.minHeight = element.preferredHeight = BarIconSize;

            var label = Text(go.transform, "Label", "", HudSkinSetup.BarLabelSize, AmberText, TextAlignmentOptions.Center, _font);
            label.margin = new Vector4(0f, 0f, 0f, 9f); // o nome sobe; as lascas ficam embaixo dele

            var count = Text(go.transform, "Value", "", BarCountSize, BarCountColor, TextAlignmentOptions.Center, _numbers);
            count.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var countRect = count.rectTransform;
            countRect.anchorMin = Vector2.zero;
            countRect.anchorMax = Vector2.right;
            countRect.pivot = new Vector2(0.5f, 0f);
            countRect.offsetMin = new Vector2(8f + BarIconSize + 2f, 6f);
            countRect.offsetMax = new Vector2(-4f, 16f);

            var view = container.AddComponent<RestButton>();
            GrayboxSetup.SetReferences(view, ("runner", Object.FindAnyObjectByType<MiningRunner>()), ("texts", texts),
                ("restScreen", safeArea.Find(ModalName).GetComponent<RestScreen>()), ("button", button), ("visual", go),
                ("label", label), ("count", count));
            go.SetActive(false);
            Debug.Log("[Setup] Botão Descansar no lado esquerdo da barra de baixo (Marco 8b, E3, D-152).");
            return true;
        }

        // ── A tela do coração (D-157) ───────────────────────────────────────────────────────────────────────────

        /// <summary>CONTINUAR vai para a esquerda e DESCANSAR (rubi) entra à direita, no tamanho dos botões do modal.</summary>
        static bool EnsureHeartRest(Transform safeArea)
        {
            var heart = safeArea.Find("HeartScreen");
            var panel = heart != null ? heart.Find("Screen/Panel") : null;
            if (panel == null || panel.Find("Rest") != null) return false;

            var continueRect = (RectTransform)panel.Find("Continue");
            continueRect.anchoredPosition = new Vector2(-ModalButtonOffset, continueRect.anchoredPosition.y);
            continueRect.sizeDelta = new Vector2(ModalButtonWidth, continueRect.sizeDelta.y);

            var (rest, restLabel) = ModalButton(panel, "Rest", ModalButtonOffset, ruby: true);
            var restRect = (RectTransform)rest.transform;
            restRect.anchoredPosition = new Vector2(ModalButtonOffset, continueRect.anchoredPosition.y);
            restRect.sizeDelta = continueRect.sizeDelta;
            rest.gameObject.SetActive(false);

            GrayboxSetup.SetReferences(heart.GetComponent<HeartScreen>(),
                ("restScreen", safeArea.Find(ModalName).GetComponent<RestScreen>()), ("restButton", rest),
                ("restLabel", restLabel));
            Debug.Log("[Setup] DESCANSAR na tela do coração da montanha (Marco 8b, E3, D-157).");
            return true;
        }

        // ── Peças ───────────────────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Um texto com a fonte já escolhida: a do jogo, ou a dos números (D-104) para os que se chamam "Value", que o
        /// <see cref="UiSkinSetup"/> reconhece como números e não troca.
        /// </summary>
        static TMP_Text Text(Transform parent, string name, string value, float size, Color color,
                             TextAlignmentOptions alignment, TMP_FontAsset font)
        {
            var text = GrayboxSetup.CreateHudText(parent, name, value, size, color, Vector2.zero, Vector2.one);
            text.alignment = alignment;
            if (font != null)
            {
                text.font = font;
                text.fontSharedMaterial = font.material;
            }
            return text;
        }

        static void TitleBar(RectTransform rect, float left, float right)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -TitleBarTop - TitleBarHeight);
            rect.offsetMax = new Vector2(-right, -TitleBarTop);
        }

        static void Top(RectTransform rect, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0f, -top - height);
            rect.offsetMax = new Vector2(0f, -top);
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static string Wood(string name) => UiSkinSetup.WoodFolder + "/" + name + ".png";
        static string Fantasy(string name) => UiSkinSetup.FantasyFolder + "/" + name + ".png";
        static Sprite Load(string path) => System.IO.File.Exists(path) ? AssetDatabase.LoadAssetAtPath<Sprite>(path) : null;
    }
}
