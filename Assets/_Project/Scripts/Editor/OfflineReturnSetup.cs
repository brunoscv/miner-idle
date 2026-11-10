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
    /// Parte do <see cref="GrayboxSetup"/> para o M9 (E2, D-197), com a cena Game aberta e já com a pele: o modal
    /// "Bem-vindo de volta" na janela da loja em tamanho menor, como o de Descansar. Medidas do mockup aprovado pelo Bruno
    /// em 2026-11-10 (<c>tools/ci/out/m9-e2-mockup.png</c>). O painel se ajusta ao conteúdo (a linha "Contou" só aparece
    /// quando o tempo fora passou do teto). Fica por cima de tudo: enquanto houver lascas para coletar, ele manda. Só cria
    /// o que falta; os textos vêm dos <see cref="FeedbackTexts"/>.
    /// </summary>
    static class OfflineReturnSetup
    {
        const string TextsPath = "Assets/_Project/Data/FeedbackTexts.asset";
        const string CatalogPath = "Assets/_Project/Data/ShopCatalog.asset";
        const string CrystalIconPath = "Assets/_Project/Art/Crystals/icon_crystal.png";
        const string ModalName = "OfflineReturn";

        const float ModalWidth = 236f;
        const float ModalRaise = 12f;
        const float TitleBarTop = 6f;
        const float TitleBarHeight = 8f;
        const float LinePad = 6f;
        const float LineHeight = 14f;
        const float BroughtHeight = 38f;
        const float InternIconSize = 28f;
        const float ShardIconSize = 14f;
        const float ButtonWidth = 100f;
        const float ButtonHeight = 26f;

        // As mesmas fontes e tamanhos do modal de Descansar.
        const float SmallText = 33f / 4f;
        const float RowNumber = 6f;
        const float TakeNumber = 10f;

        static readonly Color DimColor = new Color(0f, 0f, 0f, 0.6f);
        static readonly Color TextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        static readonly Color AmberText = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        static readonly Color MutedText = new Color32(0xBE, 0xB4, 0xAA, 0xFF);

        static TMP_FontAsset _font, _numbers;

        public static bool EnsureInScene()
        {
            var safeArea = GameObject.Find("SafeArea")?.transform;
            var texts = AssetDatabase.LoadAssetAtPath<FeedbackTexts>(TextsPath);
            if (safeArea == null || texts == null || Load(Wood("panels/window")) == null) return false;
            if (safeArea.Find(ModalName) != null) return false;
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.GameFontPath);
            _numbers = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.NumbersFontPath);

            var root = new GameObject(ModalName, typeof(RectTransform));
            root.transform.SetParent(safeArea, false);
            root.transform.SetAsLastSibling();
            Stretch((RectTransform)root.transform);

            var screen = new GameObject("Screen", typeof(RectTransform), typeof(Image));
            screen.transform.SetParent(root.transform, false);
            Stretch((RectTransform)screen.transform);
            screen.GetComponent<Image>().color = DimColor; // raycastTarget ligado: bloqueia o mapa, não pausa nada

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup),
                                       typeof(ContentSizeFitter));
            panel.transform.SetParent(screen.transform, false);
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(ModalWidth, 180f);
            panelRect.anchoredPosition = new Vector2(0f, ModalRaise);
            var image = panel.GetComponent<Image>();
            image.sprite = Load(Wood("panels/window"));
            image.type = Image.Type.Sliced;
            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 24, 10);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            panel.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var title = Text(panel.transform, "Title", SmallText, TextColor, TextAlignmentOptions.MidlineLeft, _font);
            title.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = Vector2.one;
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.offsetMin = new Vector2(10f, -TitleBarTop - TitleBarHeight);
            titleRect.offsetMax = new Vector2(0f, -TitleBarTop);

            // O quadro com os números.
            var inset = new GameObject("Inset", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            inset.transform.SetParent(panel.transform, false);
            var insetImage = inset.GetComponent<Image>();
            insetImage.sprite = Load(Wood("panels/panel_inset"));
            insetImage.type = Image.Type.Sliced;
            insetImage.raycastTarget = false;
            var insetLayout = inset.GetComponent<VerticalLayoutGroup>();
            insetLayout.padding = new RectOffset(0, 0, 3, 3);
            insetLayout.childControlWidth = insetLayout.childControlHeight = true;
            insetLayout.childForceExpandWidth = true;
            insetLayout.childForceExpandHeight = false;
            var (_, awayLabel, awayValue) = Row(inset.transform, "Away");
            var (countedRow, countedLabel, countedValue) = Row(inset.transform, "Counted");
            countedValue.color = MutedText;
            var (_, crystalsLabel, crystalsValue) = Row(inset.transform, "Crystals");

            // O estagiário e as lascas que ele trouxe.
            var brought = Block(panel.transform, "Brought", BroughtHeight);
            var internIcon = new GameObject("Intern", typeof(RectTransform), typeof(Image));
            internIcon.transform.SetParent(brought, false);
            var internRect = (RectTransform)internIcon.transform;
            internRect.anchorMin = internRect.anchorMax = internRect.pivot = new Vector2(0f, 1f);
            internRect.anchoredPosition = new Vector2(4f, 0f);
            internRect.sizeDelta = new Vector2(InternIconSize, InternIconSize);
            var internImage = internIcon.GetComponent<Image>();
            internImage.sprite = InternIcon();
            internImage.enabled = internImage.sprite != null;
            internImage.preserveAspect = true;
            internImage.raycastTarget = false;
            var broughtLabel = Text(brought, "Label", SmallText, MutedText, TextAlignmentOptions.MidlineLeft, _font);
            Top(broughtLabel.rectTransform, 0f, LineHeight, left: 38f);
            var shardsValue = ShardsValue(brought);

            var note = Text(panel.transform, "Note", SmallText, MutedText, TextAlignmentOptions.Top, _font);
            note.textWrappingMode = TextWrappingModes.Normal;
            note.margin = new Vector4(LinePad, 0f, LinePad, 0f);

            var buttons = Block(panel.transform, "Buttons", ButtonHeight);
            var (collect, collectLabel) = CollectButton(buttons);

            screen.SetActive(false);
            var view = root.AddComponent<OfflineReturnScreen>();
            GrayboxSetup.SetReferences(view, ("runner", Object.FindAnyObjectByType<MiningRunner>()), ("texts", texts),
                ("screen", screen), ("title", title), ("awayLabel", awayLabel), ("awayValue", awayValue),
                ("countedRow", countedRow), ("countedLabel", countedLabel), ("countedValue", countedValue),
                ("crystalsLabel", crystalsLabel), ("crystalsValue", crystalsValue), ("broughtLabel", broughtLabel),
                ("shardsValue", shardsValue), ("note", note), ("collectButton", collect), ("collectLabel", collectLabel));
            Debug.Log("[Setup] Modal \"Bem-vindo de volta\" na cena Game (M9, E2, D-197).");
            return true;
        }

        /// <summary>Uma linha do quadro: o nome à esquerda e o número à direita, em âmbar.</summary>
        static (GameObject row, TMP_Text label, TMP_Text value) Row(Transform parent, string name)
        {
            var row = Block(parent, name, LineHeight);
            var label = Text(row, "Label", SmallText, TextColor, TextAlignmentOptions.MidlineLeft, _font);
            label.margin = new Vector4(LinePad, 0f, LinePad, 0f);
            Stretch(label.rectTransform);
            var value = Text(row, "Value", RowNumber, AmberText, TextAlignmentOptions.MidlineRight, _numbers);
            value.margin = new Vector4(LinePad, 1f, LinePad, 0f);
            Stretch(value.rectTransform);
            return (row.gameObject, label, value);
        }

        /// <summary>As lascas em âmbar, grandes, à direita, com o cristal do jogo à esquerda do número.</summary>
        static TMP_Text ShardsValue(Transform brought)
        {
            var row = new GameObject("Shards", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(brought, false);
            Top((RectTransform)row.transform, 15f, 18f);
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
            image.preserveAspect = true;
            image.raycastTarget = false;
            var element = icon.GetComponent<LayoutElement>();
            element.minWidth = element.preferredWidth = element.minHeight = element.preferredHeight = ShardIconSize;

            return Text(row.transform, "Value", TakeNumber, AmberText, TextAlignmentOptions.MidlineRight, _numbers);
        }

        static (Button button, TMP_Text label) CollectButton(Transform parent)
        {
            var go = new GameObject("Collect", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
            var image = go.GetComponent<Image>();
            image.sprite = Load(Fantasy("buttons/button_ruby_normal"));
            image.type = Image.Type.Sliced;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { pressedSprite = Load(Fantasy("buttons/button_ruby_pressed")) };
            var label = Text(go.transform, "Label", SmallText, TextColor, TextAlignmentOptions.Center, _font);
            Stretch(label.rectTransform);
            return (button, label);
        }

        /// <summary>O ícone do Estagiário na loja (o mesmo capacete da linha de contratar).</summary>
        static Sprite InternIcon()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(CatalogPath);
            if (catalog == null) return null;
            foreach (var entry in catalog.Entries)
                if (entry.Upgrade == UpgradeId.Intern) return entry.Icon;
            return null;
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

        static void Top(RectTransform rect, float top, float height, float left = 0f)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
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
