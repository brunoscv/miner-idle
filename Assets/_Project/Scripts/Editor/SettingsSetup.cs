using MinersIdle.Game;
using MinersIdle.Presentation;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="GrayboxSetup"/> para o M9 (E4, D-198…D-200), com a cena Game aberta e já com a pele: os AJUSTES
    /// (<see cref="GameSettings"/>, quem obedece a eles e a vibração), a tela AJUSTES com a confirmação do Apagar save e os
    /// Créditos, e o botão AJUSTES do HUD, que deixa de ser "Em breve" (D-67). Medidas do mockup aprovado pelo Bruno em
    /// 2026-11-10 (<c>tools/ci/out/m9-e4-mockup.png</c>), na mesma janela dos modais de Descansar e do "Bem-vindo de volta".
    /// Só cria o que falta; os textos vêm dos <see cref="FeedbackTexts"/>.
    /// </summary>
    static class SettingsSetup
    {
        const string TextsPath = "Assets/_Project/Data/FeedbackTexts.asset";
        const string SettingsObjectName = "GameSettings";
        const string ScreenName = "SettingsScreen";

        const float ModalWidth = 236f;
        const float ModalRaise = 12f;
        const float TitleBarTop = 6f;
        const float TitleBarHeight = 8f;
        const float CloseSize = 16f;
        const float RowHeight = 22f;
        const float RowPad = 6f;
        const float IconSize = 16f;
        const float TrackWidth = 84f;
        const float TrackHeight = 6f;
        const float KnobWidth = 10f;
        const float KnobHeight = 14f;
        const float PercentWidth = 26f;
        const float ToggleWidth = 28f;
        const float ToggleHeight = 14f;
        const float ButtonWidth = 100f;
        const float ButtonHeight = 26f;
        const float ButtonOffset = 55f;

        // As mesmas fontes e tamanhos dos outros modais.
        const float SmallText = 33f / 4f;
        const float RowNumber = 6f;

        static readonly Color DimColor = new Color(0f, 0f, 0f, 0.6f);
        static readonly Color TextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        static readonly Color AmberText = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        static readonly Color MutedText = new Color32(0xBE, 0xB4, 0xAA, 0xFF);

        static TMP_FontAsset _font, _numbers;

        public static bool EnsureInScene()
        {
            bool changed = EnsureSettingsObject(out var settings);
            if (settings == null) return changed;
            changed |= EnsureDebugReference(settings);
            changed |= EnsureScreen(settings);
            changed |= EnsureThinFill();
            changed |= EnsureHudButton();
            return changed;
        }

        // ── Os ajustes e quem obedece ─────────────────────────────────────────────────────────────────────────────────

        /// <summary>O objeto "GameSettings" na raiz da cena: os ajustes, a vibração e quem passa os ajustes ao som e à câmera.</summary>
        static bool EnsureSettingsObject(out GameSettings settings)
        {
            settings = Object.FindAnyObjectByType<GameSettings>(FindObjectsInactive.Include);
            if (settings != null) return false;
            var runner = Object.FindAnyObjectByType<MiningRunner>();
            if (runner == null) return false;

            var go = new GameObject(SettingsObjectName);
            settings = go.AddComponent<GameSettings>();
            var haptics = go.AddComponent<Haptics>();
            GrayboxSetup.SetReferences(haptics, ("runner", runner));
            var effects = go.AddComponent<SettingsEffects>();
            GrayboxSetup.SetReferences(effects, ("settings", settings),
                ("soundBoard", Object.FindAnyObjectByType<SoundBoard>(FindObjectsInactive.Include)),
                ("screenShake", Object.FindAnyObjectByType<ScreenShake>(FindObjectsInactive.Include)),
                ("haptics", haptics));
            Debug.Log("[Setup] Ajustes na cena Game: som, tremor e vibração obedecem a eles (M9, E4).");
            return true;
        }

        /// <summary>O "Tremor on/off" do DBG passa a trocar o ajuste (senão a tela AJUSTES mostraria outra coisa).</summary>
        static bool EnsureDebugReference(GameSettings settings)
        {
            var panel = Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include);
            if (panel == null) return false;
            var serialized = new SerializedObject(panel);
            var property = serialized.FindProperty("settings");
            if (property == null || property.objectReferenceValue == settings) return false;
            property.objectReferenceValue = settings;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        /// <summary>O botão AJUSTES do HUD perde o "Em breve": quem cuida do toque dele é a tela AJUSTES.</summary>
        static bool EnsureHudButton()
        {
            var button = GameObject.Find("SafeArea")?.transform.Find("SettingsButton");
            if (button == null || !button.TryGetComponent<ComingSoonButton>(out var comingSoon)) return false;
            Object.DestroyImmediate(comingSoon);
            var notice = button.Find("Notice");
            if (notice != null) Object.DestroyImmediate(notice.gameObject);
            Debug.Log("[Setup] Botão AJUSTES abre a tela (sai o \"Em breve\", D-67).");
            return true;
        }

        // ── A tela ──────────────────────────────────────────────────────────────────────────────────────────────────

        static bool EnsureScreen(GameSettings settings)
        {
            var safeArea = GameObject.Find("SafeArea")?.transform;
            var texts = AssetDatabase.LoadAssetAtPath<FeedbackTexts>(TextsPath);
            var openButton = safeArea != null ? safeArea.Find("SettingsButton")?.GetComponent<Button>() : null;
            if (safeArea == null || texts == null || openButton == null || Load(Wood("panels/window")) == null) return false;
            if (safeArea.Find(ScreenName) != null) return false;
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.GameFontPath);
            _numbers = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.NumbersFontPath);

            var root = new GameObject(ScreenName, typeof(RectTransform));
            root.transform.SetParent(safeArea, false);
            // Por cima do HUD e da loja, mas abaixo do "Bem-vindo de volta", que manda quando há lascas para coletar.
            var offline = safeArea.Find("OfflineReturn");
            if (offline != null) root.transform.SetSiblingIndex(offline.GetSiblingIndex());
            else root.transform.SetAsLastSibling();
            Stretch((RectTransform)root.transform);

            // AJUSTES
            var (screen, panel) = Modal(root.transform, "Screen");
            var title = Title(panel);
            var close = CloseButton(panel);
            var inset = Inset(panel, "Inset");
            var volumeRow = Block(inset, "Volume", RowHeight);
            Icon(volumeRow, Load(Wood("icons/icon_sound_on")));
            var volumeLabel = Label(volumeRow, 24f);
            var slider = VolumeSlider(volumeRow);
            var volumeValue = Text(volumeRow, "Value", RowNumber, AmberText, TextAlignmentOptions.MidlineRight, _numbers);
            volumeValue.margin = new Vector4(0f, 1f, RowPad, 0f);
            Stretch(volumeValue.rectTransform);
            var shakeRow = Block(inset, "Shake", RowHeight);
            var shakeLabel = Label(shakeRow, RowPad);
            var shake = Toggle(shakeRow);
            var vibrationRow = Block(inset, "Vibration", RowHeight);
            var vibrationLabel = Label(vibrationRow, RowPad);
            var vibration = Toggle(vibrationRow);
            var buttons = Block(panel, "Buttons", ButtonHeight);
            var (credits, creditsLabel) = WindowButton(buttons, "Credits", -ButtonOffset, ruby: false);
            var (delete, deleteLabel) = WindowButton(buttons, "DeleteSave", ButtonOffset, ruby: true);

            // A confirmação do Apagar save, por cima dos AJUSTES.
            var (confirm, confirmPanel) = Modal(root.transform, "Confirm");
            var confirmTitle = Title(confirmPanel);
            var body = Paragraph(confirmPanel, "Body", MutedText);
            var warning = Paragraph(confirmPanel, "Warning", AmberText);
            var confirmButtons = Block(confirmPanel, "Buttons", ButtonHeight);
            var (cancel, cancelLabel) = WindowButton(confirmButtons, "Cancel", -ButtonOffset, ruby: false);
            var (confirmDelete, confirmLabel) = WindowButton(confirmButtons, "Delete", ButtonOffset, ruby: true);

            // Os Créditos, no lugar dos AJUSTES.
            var (creditsScreen, creditsPanel) = Modal(root.transform, "Credits");
            var creditsTitle = Title(creditsPanel);
            var creditsClose = CloseButton(creditsPanel);
            var creditsInset = Inset(creditsPanel, "Inset");
            ((VerticalLayoutGroup)creditsInset.GetComponent<LayoutGroup>()).padding = new RectOffset((int)RowPad, (int)RowPad, 4, 4);
            var list = Text(creditsInset, "List", SmallText, TextColor, TextAlignmentOptions.TopLeft, _font);
            list.textWrappingMode = TextWrappingModes.Normal;
            var creditsButtons = Block(creditsPanel, "Buttons", ButtonHeight);
            var (back, backLabel) = WindowButton(creditsButtons, "Back", 0f, ruby: true);

            screen.SetActive(false);
            confirm.SetActive(false);
            creditsScreen.SetActive(false);
            var view = root.AddComponent<SettingsScreen>();
            GrayboxSetup.SetReferences(view, ("runner", Object.FindAnyObjectByType<MiningRunner>()), ("settings", settings),
                ("texts", texts), ("soundBoard", Object.FindAnyObjectByType<SoundBoard>(FindObjectsInactive.Include)),
                ("openButton", openButton),
                ("screen", screen), ("title", title), ("closeButton", close), ("volumeLabel", volumeLabel),
                ("volumeSlider", slider), ("volumeValue", volumeValue), ("shakeLabel", shakeLabel), ("shakeButton", shake),
                ("vibrationLabel", vibrationLabel), ("vibrationButton", vibration),
                ("toggleOn", Load(Wood("controls/toggle_on"))), ("toggleOff", Load(Wood("controls/toggle_off"))),
                ("creditsButton", credits), ("creditsLabel", creditsLabel), ("deleteButton", delete), ("deleteLabel", deleteLabel),
                ("confirm", confirm), ("confirmTitle", confirmTitle), ("confirmBody", body), ("confirmWarning", warning),
                ("cancelButton", cancel), ("cancelLabel", cancelLabel), ("confirmButton", confirmDelete),
                ("confirmLabel", confirmLabel),
                ("credits", creditsScreen), ("creditsTitle", creditsTitle), ("creditsCloseButton", creditsClose),
                ("creditsList", list), ("backButton", back), ("backLabel", backLabel));
            Debug.Log("[Setup] Tela AJUSTES na cena Game, com a confirmação do Apagar save e os Créditos (M9, E4, D-198).");
            return true;
        }

        /// <summary>O fundo que escurece (bloqueia o mapa, não pausa nada) e a janela de madeira, que cresce com o conteúdo.</summary>
        static (GameObject screen, Transform panel) Modal(Transform root, string name)
        {
            var screen = new GameObject(name, typeof(RectTransform), typeof(Image));
            screen.transform.SetParent(root, false);
            Stretch((RectTransform)screen.transform);
            screen.GetComponent<Image>().color = DimColor;

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup),
                                       typeof(ContentSizeFitter));
            panel.transform.SetParent(screen.transform, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(ModalWidth, 160f);
            rect.anchoredPosition = new Vector2(0f, ModalRaise);
            var image = panel.GetComponent<Image>();
            image.sprite = Load(Wood("panels/window"));
            image.type = Image.Type.Sliced;
            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 24, 10);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            panel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return (screen, panel.transform);
        }

        /// <summary>O título na barra da janela, fora do layout.</summary>
        static TMP_Text Title(Transform panel)
        {
            var title = Text(panel, "Title", SmallText, TextColor, TextAlignmentOptions.MidlineLeft, _font);
            title.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var rect = title.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(10f, -TitleBarTop - TitleBarHeight);
            rect.offsetMax = new Vector2(-CloseSize - 8f, -TitleBarTop);
            return title;
        }

        /// <summary>O X no canto da barra da janela.</summary>
        static Button CloseButton(Transform panel)
        {
            var go = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            go.transform.SetParent(panel, false);
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-8f, -2f);
            rect.sizeDelta = new Vector2(CloseSize, CloseSize);
            var image = go.GetComponent<Image>();
            image.sprite = Load(Wood("icons/icon_close"));
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        static Transform Inset(Transform panel, string name)
        {
            var inset = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            inset.transform.SetParent(panel, false);
            var image = inset.GetComponent<Image>();
            image.sprite = Load(Wood("panels/panel_inset"));
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            var layout = inset.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 3, 3);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return inset.transform;
        }

        static void Icon(Transform row, Sprite sprite)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(row, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(5f, 0f);
            rect.sizeDelta = new Vector2(IconSize, IconSize);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
        }

        static TMP_Text Label(Transform row, float left)
        {
            var label = Text(row, "Label", SmallText, TextColor, TextAlignmentOptions.MidlineLeft, _font);
            label.margin = new Vector4(left, 0f, 0f, 0f);
            Stretch(label.rectTransform);
            return label;
        }

        /// <summary>
        /// O deslizador do volume: o trilho e o preenchimento do kit de madeira, com o botão centrado no fim do
        /// preenchimento (como no mockup, ele passa meio botão das pontas). À esquerda do número.
        /// </summary>
        static Slider VolumeSlider(Transform row)
        {
            var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(row, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-RowPad - PercentWidth, 0f);
            rect.sizeDelta = new Vector2(TrackWidth, KnobHeight);

            var track = Bar(go.transform, "Track", Load(Wood("controls/slider_track")));
            track.raycastTarget = true; // tocar no trilho também move
            var fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            ThinBar((RectTransform)fillArea.transform);
            var fill = Bar(fillArea.transform, "Fill", Load(Wood("controls/slider_fill")));
            Stretch(fill.rectTransform); // o Slider estica o preenchimento na altura da área: a área é que é fina

            var handleArea = new GameObject("HandleArea", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            Stretch((RectTransform)handleArea.transform);
            var handle = new GameObject("Knob", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var handleRect = (RectTransform)handle.transform;
            handleRect.anchorMin = Vector2.zero; // o Slider só mexe no x; a altura é a do deslizador
            handleRect.anchorMax = new Vector2(0f, 1f);
            handleRect.sizeDelta = new Vector2(KnobWidth, 0f);
            var handleImage = handle.GetComponent<Image>();
            handleImage.sprite = Load(Wood("controls/slider_knob_normal"));

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.transition = Selectable.Transition.None;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = SettingsScreen.VolumeSteps;
            slider.wholeNumbers = true;
            slider.value = SettingsScreen.VolumeSteps * 0.8f;
            return slider;
        }

        /// <summary>
        /// A primeira versão da tela (2026-11-10) deixou a área do preenchimento com a altura toda do deslizador, e o
        /// <c>Slider</c> estica o preenchimento nela: a barra saía grossa. Afina a área (idempotente).
        /// </summary>
        static bool EnsureThinFill()
        {
            var area = GameObject.Find("SafeArea")?.transform.Find(ScreenName + "/Screen/Panel/Inset/Volume/Slider/FillArea") as RectTransform;
            var fill = area != null ? area.Find("Fill") as RectTransform : null;
            if (area == null || fill == null) return false;
            if (Mathf.Approximately(area.sizeDelta.y, TrackHeight) && fill.sizeDelta == Vector2.zero) return false;
            ThinBar(area);
            Stretch(fill);
            Debug.Log("[Setup] Preenchimento do volume na espessura do trilho (M9, E4).");
            return true;
        }

        /// <summary>Na largura toda e na altura do trilho, centrado.</summary>
        static void ThinBar(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, TrackHeight);
        }

        /// <summary>Uma barra fina do deslizador, centrada na altura.</summary>
        static Image Bar(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(0f, TrackHeight);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>O liga/desliga do kit de madeira, à direita da linha; a tela troca a imagem.</summary>
        static Button Toggle(Transform row)
        {
            var go = new GameObject("Toggle", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(row, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-RowPad, 0f);
            rect.sizeDelta = new Vector2(ToggleWidth, ToggleHeight);
            var image = go.GetComponent<Image>();
            image.sprite = Load(Wood("controls/toggle_on"));
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            return button;
        }

        /// <summary>Um texto que quebra linha, centrado, na largura da janela.</summary>
        static TMP_Text Paragraph(Transform panel, string name, Color color)
        {
            var text = Text(panel, name, SmallText, color, TextAlignmentOptions.Top, _font);
            text.textWrappingMode = TextWrappingModes.Normal;
            text.margin = new Vector4(RowPad, 0f, RowPad, 0f);
            return text;
        }

        /// <summary>Um botão da janela: madeira com o nome em âmbar, ou rubi com o nome em branco (o da ação forte).</summary>
        static (Button button, TMP_Text label) WindowButton(Transform parent, string name, float x, bool ruby)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
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
            var label = Text(go.transform, "Label", SmallText, ruby ? TextColor : AmberText, TextAlignmentOptions.Center, _font);
            Stretch(label.rectTransform);
            return (button, label);
        }

        /// <summary>Um pedaço do painel com altura fixa (o layout cuida da largura).</summary>
        static Transform Block(Transform parent, string name, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            var element = go.GetComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = height;
            return go.transform;
        }

        /// <summary>
        /// Um texto com a fonte já escolhida: a do jogo, ou a dos números (D-104) para os que se chamam "Value", que o
        /// <see cref="UiSkinSetup"/> reconhece como números e não troca.
        /// </summary>
        static TMP_Text Text(Transform parent, string name, float size, Color color, TextAlignmentOptions alignment,
                             TMP_FontAsset font)
        {
            var text = GrayboxSetup.CreateHudText(parent, name, "", size, color, Vector2.zero, Vector2.one);
            text.alignment = alignment;
            text.raycastTarget = false;
            if (font != null)
            {
                text.font = font;
                text.fontSharedMaterial = font.material;
            }
            return text;
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
