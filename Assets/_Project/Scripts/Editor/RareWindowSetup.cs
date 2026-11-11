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
    /// Parte do <see cref="GrayboxSetup"/> para a E4 do M9b (mockup aprovado pelo Bruno em 2026-11-10,
    /// <c>tools/ci/out/m9b-e4-mockup.png</c>): as 4 cartas do cristal raro e os textos da janela no
    /// <c>Data/BuffCatalog</c>, e na cena Game a janela (<see cref="RareWindowView"/>) na metade de baixo, antes da loja na
    /// hierarquia (a loja aberta fica por cima; o DBG e os modais também). Medidas do mockup, na referência 270 (D-14). Só
    /// cria o que falta; os textos ficam no catálogo.
    /// </summary>
    static class RareWindowSetup
    {
        const string WindowName = "RareWindow";
        const string RareCrystalPath = "Assets/_Project/Art/Crystals/crystal_glass_rare.png";

        // O mockup: janela 236 × 214, cartas 104 × 132 com 8 entre elas, a 118 do pé da tela (metade de baixo, D-223).
        const float PanelWidth = 236f;
        const float PanelHeight = 214f;
        const float PanelBottom = 118f;
        const float CardWidth = 104f;
        const float CardHeight = 132f;
        const float CardGap = 8f;
        const float CardsTop = 36f;
        const float BarLeft = 12f;
        const float BarRight = 38f;
        const float BarHeight = 12f;
        const float ButtonWidth = 100f;
        const float ButtonHeight = 24f;

        const float SmallText = 33f / 4f;
        const float TinyText = 22f / 4f;
        static readonly Color DimColor = new Color(0f, 0f, 0f, 0.35f);
        static readonly Color TextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        static readonly Color AmberText = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        static readonly Color MutedText = new Color32(0xBE, 0xB4, 0xAA, 0xFF);
        static readonly Color RareText = new Color32(0x9B, 0xD8, 0xF0, 0xFF);

        /// <summary>
        /// As cartas no catálogo (D-219, D-225), na ordem de <see cref="RareCard"/>: o ícone (o martelo do kit Anvil, o
        /// relógio do HUD, o cristal do jogo, o raio do kit Anvil), o nome, o texto que sobe, o efeito, para quem vale.
        /// </summary>
        static readonly (RareCard card, string icon, string title, string gain, string text, string note, bool intern)[] Cards =
        {
            (RareCard.Damage, UiSkinSetup.IconsFolder + "/Icon23.png", "DANO ×2", "DANO ×2",
             "Golpe e toque\ncom o dobro\nde dano", "Vale p/ o estagiário", true),
            (RareCard.Speed, "Assets/_Project/Art/UI/icon_speed.png", "RÁPIDO", "RÁPIDO",
             "Golpe 2× mais\nrápido (o tempo\ndo golpe ÷ 2)", "Vale p/ o estagiário", true),
            (RareCard.Shards, "Assets/_Project/Art/Crystals/icon_crystal.png", "LASCAS ×2", "LASCAS ×2",
             "Cada cristal\nquebrado paga\no dobro", "Vale p/ o estagiário", true),
            (RareCard.Critical, UiSkinSetup.IconsFolder + "/Icon18.png", "CRÍTICO +25%", "CRÍTICO +25%",
             "+25 pontos na\nchance de\ncrítico", "Só o anão", false),
        };

        static TMP_FontAsset _font, _numbers;

        public static bool EnsureInScene()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BuffCatalog>(BuffColumnSetup.CatalogPath);
            if (catalog == null) return false;
            bool changed = EnsureCatalog(catalog);

            var safeArea = GameObject.Find("SafeArea")?.transform;
            if (safeArea == null || safeArea.Find(WindowName) != null || Load(Wood("panels/window")) == null) return changed;
            var runner = Object.FindAnyObjectByType<MiningRunner>();
            if (runner == null) return changed;
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.GameFontPath);
            _numbers = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.NumbersFontPath);

            var root = new GameObject(WindowName, typeof(RectTransform));
            root.transform.SetParent(safeArea, false);
            // Antes da loja: a loja aberta, os modais e o DBG ficam por cima; o mapa, o HUD e a coluna, atrás.
            var shop = safeArea.Find("Shop");
            if (shop != null) root.transform.SetSiblingIndex(shop.GetSiblingIndex());
            else root.transform.SetAsLastSibling();
            Stretch((RectTransform)root.transform);

            // O fundo escurece e bloqueia os toques no mapa (D-223): o TapInput não minera com a UI por cima.
            var screen = new GameObject("Screen", typeof(RectTransform), typeof(Image));
            screen.transform.SetParent(root.transform, false);
            Stretch((RectTransform)screen.transform);
            screen.GetComponent<Image>().color = DimColor;

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(screen.transform, false);
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, PanelBottom);
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            var panelImage = panel.GetComponent<Image>();
            panelImage.sprite = Load(Wood("panels/window"));
            panelImage.type = Image.Type.Sliced;

            // A barra do título: o cristal raro, o nome e a fila.
            // O desenho do raro é textura do mundo (o quad da fila), não sprite: na UI, uma RawImage.
            var gem = new GameObject("Gem", typeof(RectTransform), typeof(RawImage));
            gem.transform.SetParent(panel.transform, false);
            var gemImage = gem.GetComponent<RawImage>();
            gemImage.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(RareCrystalPath);
            gemImage.enabled = gemImage.texture != null;
            gemImage.raycastTarget = false;
            Place((RectTransform)gem.transform, 6f, 1f, 16f, 16f);
            var title = Text(panel.transform, "Title", SmallText, TextColor, TextAlignmentOptions.MidlineLeft, _font);
            Place(title.rectTransform, 26f, 3f, PanelWidth - 80f, 12f);
            var queue = Text(panel.transform, "Queue", SmallText, RareText, TextAlignmentOptions.MidlineRight, _font);
            Place(queue.rectTransform, PanelWidth - 70f, 3f, 60f, 12f);
            var subtitle = Text(panel.transform, "Subtitle", SmallText, MutedText, TextAlignmentOptions.Center, _font);
            Place(subtitle.rectTransform, 8f, 21f, PanelWidth - 16f, 12f);

            float left = (PanelWidth - 2f * CardWidth - CardGap) / 2f;
            var cards = new RareWindowView.CardSlot[2];
            for (int i = 0; i < 2; i++) cards[i] = Card(panel.transform, "Card" + (i + 1), left + i * (CardWidth + CardGap));

            float barTop = CardsTop + CardHeight + 10f;
            var choosing = new GameObject("Choosing", typeof(RectTransform));
            choosing.transform.SetParent(panel.transform, false);
            Stretch((RectTransform)choosing.transform);
            var barFrame = Picture(choosing.transform, "Bar", Load(Wood("bars/bar_frame_large")));
            barFrame.type = Image.Type.Sliced;
            Place(barFrame.rectTransform, BarLeft, barTop, PanelWidth - BarLeft - BarRight, BarHeight);
            var fill = Picture(barFrame.transform, "Fill", Load(Wood("bars/bar_fill_xp_large")));
            fill.type = Image.Type.Sliced;
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            var seconds = Text(choosing.transform, "Seconds", UiSkinSetup.NumbersFontSize, TextColor,
                               TextAlignmentOptions.MidlineRight, _numbers);
            Place(seconds.rectTransform, PanelWidth - BarRight, barTop, BarRight - 10f, BarHeight);
            var hint = Text(choosing.transform, "Hint", SmallText, MutedText, TextAlignmentOptions.Center, _font);
            Place(hint.rectTransform, 8f, barTop + 16f, PanelWidth - 16f, 12f);

            var (close, closeLabel) = CloseButton(panel.transform, barTop - 2f);
            close.gameObject.SetActive(false);

            screen.SetActive(false);
            var view = root.AddComponent<RareWindowView>();
            GrayboxSetup.SetReferences(view, ("runner", runner), ("catalog", catalog), ("screen", screen), ("title", title),
                ("queue", queue), ("subtitle", subtitle), ("choosing", choosing), ("timerFill", fillRect),
                ("timerSeconds", seconds), ("hint", hint), ("closeButton", close), ("closeLabel", closeLabel),
                ("cardFrame", Load(Wood("slots/slot_rarity_epic"))), ("chosenFrame", Load(Wood("slots/slot_selected"))));
            var so = new SerializedObject(view);
            var list = so.FindProperty("cards");
            list.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++)
            {
                var item = list.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("button").objectReferenceValue = cards[i].button;
                item.FindPropertyRelative("frame").objectReferenceValue = cards[i].frame;
                item.FindPropertyRelative("icon").objectReferenceValue = cards[i].icon;
                item.FindPropertyRelative("title").objectReferenceValue = cards[i].title;
                item.FindPropertyRelative("text").objectReferenceValue = cards[i].text;
                item.FindPropertyRelative("seconds").objectReferenceValue = cards[i].seconds;
                item.FindPropertyRelative("note").objectReferenceValue = cards[i].note;
                item.FindPropertyRelative("group").objectReferenceValue = cards[i].group;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Setup] Janela do cristal raro na cena Game, na metade de baixo, antes da loja (M9b E4, D-223, D-225).");
            return true;
        }

        /// <summary>Uma carta: moldura de raridade (botão), quadro com o ícone, nome, efeito, "60 s" e para quem vale.</summary>
        static RareWindowView.CardSlot Card(Transform panel, string name, float left)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
            go.transform.SetParent(panel, false);
            Place((RectTransform)go.transform, left, CardsTop, CardWidth, CardHeight);
            var frame = go.GetComponent<Image>();
            frame.sprite = Load(Wood("slots/slot_rarity_epic"));
            frame.type = Image.Type.Sliced;
            var button = go.GetComponent<Button>();
            button.targetGraphic = frame;

            var inset = Picture(go.transform, "Inset", Load(Wood("panels/panel_inset")));
            inset.type = Image.Type.Sliced;
            Place(inset.rectTransform, 8f, 8f, CardWidth - 16f, 40f);
            var icon = Picture(go.transform, "Icon", null);
            icon.preserveAspect = true;
            Place(icon.rectTransform, CardWidth / 2f - 16f, 12f, 32f, 32f);

            var title = Text(go.transform, "Title", SmallText, AmberText, TextAlignmentOptions.Center, _font);
            Place(title.rectTransform, 4f, 52f, CardWidth - 8f, 12f);
            var text = Text(go.transform, "Text", SmallText, TextColor, TextAlignmentOptions.Top, _font);
            text.textWrappingMode = TextWrappingModes.Normal;
            Place(text.rectTransform, 6f, 66f, CardWidth - 12f, 34f);
            var seconds = Text(go.transform, "Seconds", UiSkinSetup.NumbersFontSize, TextColor, TextAlignmentOptions.Center, _numbers);
            Place(seconds.rectTransform, 4f, 102f, CardWidth - 8f, 10f);
            var note = Text(go.transform, "Note", TinyText * 1.5f, MutedText, TextAlignmentOptions.Center, _font);
            Place(note.rectTransform, 4f, 114f, CardWidth - 8f, 12f);

            return new RareWindowView.CardSlot
            {
                button = button, frame = frame, icon = icon, title = title, text = text, seconds = seconds, note = note,
                group = go.GetComponent<CanvasGroup>(),
            };
        }

        static (Button button, TMP_Text label) CloseButton(Transform parent, float top)
        {
            var go = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, (PanelWidth - ButtonWidth) / 2f, top, ButtonWidth, ButtonHeight);
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

        /// <summary>
        /// As 4 cartas e os textos da janela no catálogo que já existe (criado na E2 só com o combo). Só acrescenta o que
        /// falta: uma carta ajustada à mão fica como está.
        /// </summary>
        static bool EnsureCatalog(BuffCatalog catalog)
        {
            var so = new SerializedObject(catalog);
            var list = so.FindProperty("entries");
            int added = 0;
            foreach (var (card, icon, title, gain, text, note, intern) in Cards)
            {
                if (catalog.Find(BonusSource.Rare, (int)card) != null) continue;
                int index = list.arraySize;
                list.arraySize = index + 1;
                var item = list.GetArrayElementAtIndex(index);
                item.FindPropertyRelative("source").enumValueIndex = (int)BonusSource.Rare;
                item.FindPropertyRelative("tier").intValue = (int)card;
                item.FindPropertyRelative("icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(icon);
                item.FindPropertyRelative("title").stringValue = title;
                item.FindPropertyRelative("description").stringValue = text.Replace('\n', ' ');
                item.FindPropertyRelative("headline").stringValue = "";
                item.FindPropertyRelative("gain").stringValue = gain;
                item.FindPropertyRelative("cardText").stringValue = text;
                item.FindPropertyRelative("cardNote").stringValue = note;
                item.FindPropertyRelative("forIntern").boolValue = intern;
                added++;
            }

            bool texts = string.IsNullOrEmpty(catalog.RareTitle);
            if (texts)
            {
                so.FindProperty("rareFooterFormat").stringValue = "Cristal raro · {0}s";
                so.FindProperty("rareTitle").stringValue = "CRISTAL RARO!";
                so.FindProperty("rareChooseFormat").stringValue = "Escolha uma melhoria por {0}";
                so.FindProperty("rarePicked").stringValue = "O jogo escolheu por você";
                so.FindProperty("rareHint").stringValue = "Toque numa carta para escolher";
                so.FindProperty("rareQueueFormat").stringValue = "+{0} raro";
                so.FindProperty("rareClose").stringValue = "FECHAR";
                so.FindProperty("cardSecondsFormat").stringValue = "{0} s";
            }
            if (added == 0 && !texts) return false;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Setup] Catálogo dos bônus: {added} cartas do cristal raro e os textos da janela (M9b E4, D-225).");
            return true;
        }

        static Image Picture(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.enabled = sprite != null || name == "Icon";
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// Um texto com a fonte já escolhida: a do jogo, ou a dos números (D-104) para os que se chamam "Seconds", que o
        /// <see cref="UiSkinSetup"/> reconhece como números e não troca.
        /// </summary>
        static TMP_Text Text(Transform parent, string name, float size, Color color, TextAlignmentOptions alignment,
                             TMP_FontAsset font)
        {
            var text = GrayboxSetup.CreateHudText(parent, name, "", size, color, Vector2.zero, Vector2.one);
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            if (font != null)
            {
                text.font = font;
                text.fontSharedMaterial = font.material;
            }
            return text;
        }

        /// <summary>Posição pelo canto de cima à esquerda do pai, como no mockup (x para a direita, y para baixo).</summary>
        static void Place(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
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
