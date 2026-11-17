using MinersIdle.Game;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// M9c E2 (D-269): o botão Próximo Andar embaixo à direita, no lugar do PASSAR provisório (o objeto "PassCave" sai).
    /// O botão é a barra: a madeira apagada embaixo e a acesa por cima, cortada por uma máscara cuja largura é o progresso;
    /// cheio, o conteúdo vira a seta + "DESCER · ANDAR n" e uma borda âmbar pulsa atrás. A barra de baixo fica mais
    /// estreita (o Descansar também), com o centro livre para a Bomba (Bloco E). E o "CAVERNA" do HUD vira "ANDAR" (P9).
    /// </summary>
    static class NextFloorSetup
    {
        const string ContainerName = "NextFloor";
        const string OldContainerName = "PassCave";
        const string TextsPath = "Assets/_Project/Data/FeedbackTexts.asset";
        const string ArrowPath = "Assets/_Project/Art/UI/icon_floor_down.png";
        const string HudLabel = "ANDAR";
        const string OldHudLabel = "CAVERNA";

        const float GlowOutset = 3f;
        const float ArrowWidth = 18f, ArrowHeight = 20f, ArrowLeft = 9f;
        const float TitleSize = 33f / 4f;   // 8,25: a grade da Pixelify
        const float ReadySize = 11f;
        const float CountSize = 8f;         // a grade da Press Start
        const float RestPadding = 4f;
        const float RestIconSize = 20f;

        static readonly Color DimTint = new Color32(0x8C, 0x80, 0x76, 0xFF);   // a madeira apagada
        static readonly Color LitTint = new Color32(0xFF, 0xD0, 0x6A, 0xFF);   // a madeira acesa (o âmbar do PASSAR)
        static readonly Color GlowTint = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        static readonly Color AmberText = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        static readonly Color WhiteText = new Color32(0xF5, 0xF7, 0xF8, 0xFF);

        static string Wood(string name) => UiSkinSetup.WoodFolder + "/" + name + ".png";
        static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        public static bool EnsureInScene()
        {
            var safeArea = GameObject.Find("SafeArea")?.transform;
            var texts = AssetDatabase.LoadAssetAtPath<FeedbackTexts>(TextsPath);
            var wood = Load(Wood("buttons/button_large_normal"));
            if (safeArea == null || texts == null || wood == null) return false;

            bool changed = EnsureArrowSprite();
            changed |= RemoveOldPassButton(safeArea);
            changed |= EnsureButton(safeArea, texts, wood);
            changed |= NameTheCountValue(safeArea);
            changed |= FitTheRestButton(safeArea);
            changed |= RenameHudLabel(safeArea);
            changed |= EnsureAutoChip(safeArea, texts);
            return changed;
        }

        // ── M9c E3 ──────────────────────────────────────────────────────────────────────────────────────────────────

        const float ChipWidth = 26f, ChipHeight = 11f, ChipRight = 3f, ChipRaise = 5f, ChipBorder = 1f;
        static readonly Color ChipDark = new Color32(0x2A, 0x1A, 0x10, 0xFF);
        static readonly Color ChipGreen = new Color32(0x2E, 0x7D, 0x3A, 0xFF);

        /// <summary>
        /// O selo "AUTO" (P6, o do mockup da E2): verde, no canto de cima à direita da barra, meio para fora. Aparece quando o
        /// jogo desce sozinho.
        /// </summary>
        static bool EnsureAutoChip(Transform safeArea, FeedbackTexts texts)
        {
            var view = safeArea.Find(ContainerName)?.GetComponent<NextFloorButton>();
            var bar = safeArea.Find(ContainerName + "/Bar");
            if (view == null || bar == null) return false;
            var serialized = new SerializedObject(view);
            if (serialized.FindProperty("autoChip").objectReferenceValue != null) return false;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.GameFontPath);

            var chip = new GameObject("Auto", typeof(RectTransform), typeof(Image));
            chip.transform.SetParent(bar, false);
            chip.transform.SetAsLastSibling();
            var rect = (RectTransform)chip.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-ChipRight, ChipRaise);
            rect.sizeDelta = new Vector2(ChipWidth, ChipHeight);
            var dark = chip.GetComponent<Image>();
            dark.color = ChipDark;
            dark.raycastTarget = false;
            var green = new GameObject("Green", typeof(RectTransform), typeof(Image));
            green.transform.SetParent(chip.transform, false);
            Stretch((RectTransform)green.transform, ChipBorder);
            var greenImage = green.GetComponent<Image>();
            greenImage.color = ChipGreen;
            greenImage.raycastTarget = false;
            var label = Text(chip.transform, "Label", texts.NextFloorAuto, TitleSize, WhiteText, font, Vector2.zero, Vector2.one);
            chip.SetActive(false);

            GrayboxSetup.SetReferences(view, ("autoChip", chip), ("autoLabel", label));
            Debug.Log("[Setup] Próximo Andar: o selo AUTO do avanço automático (M9c E3, P6).");
            return true;
        }

        const string RecordButtonName = "FloorRecord";
        const string AutoButtonName = "FloorAuto";

        static Button FindOrCreate(Transform grid, string name, string label)
        {
            var existing = grid.Find(name);
            return existing != null ? existing.GetComponent<Button>() : DebugPanelSetup.CreateButton(grid, name, label);
        }

        /// <summary>
        /// DBG → Andar → "Recorde: n" e "Auto: ligado" (M9c E3), antes do Fechar. Depois dos AJUSTES (usa o
        /// <see cref="GameSettings"/>) e da Transição curta.
        /// </summary>
        public static bool EnsureMilestoneDebug()
        {
            var panel = Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include);
            var settings = Object.FindAnyObjectByType<GameSettings>(FindObjectsInactive.Include);
            var runner = Object.FindAnyObjectByType<MiningRunner>();
            var menu = panel != null ? panel.transform.Find("CaveMenu") : null;
            var grid = menu != null ? menu.Find("Content/Buttons") : null;
            if (grid == null || settings == null || runner == null) return false;
            if (menu.GetComponent<FloorMilestoneDebug>() != null) return false;

            var record = FindOrCreate(grid, RecordButtonName, "Recorde: 1");
            var auto = FindOrCreate(grid, AutoButtonName, "Auto: ligado");
            grid.Find("Close")?.SetAsLastSibling();

            var debug = menu.gameObject.AddComponent<FloorMilestoneDebug>();
            GrayboxSetup.SetReferences(debug, ("runner", runner), ("settings", settings), ("record", record), ("auto", auto));
            Debug.Log("[Setup] Painel de debug: \"Recorde\" e \"Auto\" no menu Andar (M9c E3).");
            return true;
        }

        static bool EnsureArrowSprite()
        {
            if (!System.IO.File.Exists(ArrowPath)) return false;
            if (AssetImporter.GetAtPath(ArrowPath) == null) AssetDatabase.ImportAsset(ArrowPath);
            if (!(AssetImporter.GetAtPath(ArrowPath) is TextureImporter importer)) return false;
            if (importer.textureType == TextureImporterType.Sprite && importer.filterMode == FilterMode.Point
                && !importer.mipmapEnabled && importer.textureCompression == TextureImporterCompression.Uncompressed) return false;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return true;
        }

        static bool RemoveOldPassButton(Transform safeArea)
        {
            var old = safeArea.Find(OldContainerName);
            if (old == null) return false;
            Object.DestroyImmediate(old.gameObject);
            Debug.Log("[Setup] O PASSAR provisório sai; entra o Próximo Andar (M9c E2, D-269).");
            return true;
        }

        static bool EnsureButton(Transform safeArea, FeedbackTexts texts, Sprite wood)
        {
            if (safeArea.Find(ContainerName) != null) return false;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.GameFontPath);
            var numbers = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.NumbersFontPath);

            var container = new GameObject(ContainerName, typeof(RectTransform));
            container.transform.SetParent(safeArea, false);
            Stretch((RectTransform)container.transform, 0f);
            // Antes da loja na hierarquia: a loja aberta fica por cima.
            var shop = safeArea.Find("Shop");
            if (shop != null) container.transform.SetSiblingIndex(shop.GetSiblingIndex());

            // O botão inteiro (a borda que pulsa e o botão), no lado direito da barra de baixo.
            var bar = new GameObject("Bar", typeof(RectTransform));
            bar.transform.SetParent(container.transform, false);
            HudSkinSetup.PlaceOnTheBar((RectTransform)bar.transform, right: true);

            var glow = SlicedImage(bar.transform, "Glow", wood, GlowTint);
            Stretch(glow.rectTransform, -GlowOutset);
            glow.enabled = false;

            var buttonObject = new GameObject("NextFloorButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(bar.transform, false);
            Stretch((RectTransform)buttonObject.transform, 0f);
            var dim = buttonObject.GetComponent<Image>();
            dim.sprite = wood;
            dim.type = Image.Type.Sliced;
            dim.color = DimTint; // raycast ligado: tocar no botão não vira toque no mapa

            // A madeira acesa, cortada pela máscara (a largura dela é a barra).
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(RectMask2D));
            fill.transform.SetParent(buttonObject.transform, false);
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;
            var lit = SlicedImage(fill.transform, "Lit", wood, LitTint);
            var litRect = lit.rectTransform;
            litRect.anchorMin = litRect.anchorMax = litRect.pivot = new Vector2(0f, 0.5f);
            litRect.anchoredPosition = Vector2.zero;
            litRect.sizeDelta = new Vector2(HudSkinSetup.BarButtonWidth, HudSkinSetup.BarButtonHeight);

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = lit;
            button.transition = Selectable.Transition.ColorTint;
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = colors.highlightedColor = colors.selectedColor = colors.disabledColor = Color.white;
            colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            button.colors = colors;

            // Enchendo: o nome em cima, o "x/y" embaixo.
            var filling = Group(buttonObject.transform, "Filling");
            var title = Text(filling, "Title", texts.NextFloorTitle, TitleSize, WhiteText, font,
                             new Vector2(0f, 0.45f), Vector2.one);
            // "Value": a regra das fontes (UiSkinSetup.IsNumberText) põe os números na Press Start.
            var count = Text(filling, "Value", "0/10", CountSize, WhiteText, numbers, Vector2.zero, new Vector2(1f, 0.5f));

            // Cheio: a seta à esquerda, "DESCER" e "ANDAR n".
            var ready = Group(buttonObject.transform, "Ready");
            var arrow = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
            arrow.transform.SetParent(ready, false);
            var arrowRect = (RectTransform)arrow.transform;
            arrowRect.anchorMin = arrowRect.anchorMax = new Vector2(0f, 0.5f);
            arrowRect.pivot = new Vector2(0f, 0.5f);
            arrowRect.anchoredPosition = new Vector2(ArrowLeft, 0f);
            arrowRect.sizeDelta = new Vector2(ArrowWidth, ArrowHeight);
            var arrowImage = arrow.GetComponent<Image>();
            arrowImage.sprite = Load(ArrowPath);
            arrowImage.raycastTarget = false;
            float textLeft = ArrowLeft + ArrowWidth;
            var readyTitle = Text(ready, "Title", texts.NextFloorReady, ReadySize, AmberText, font,
                                  new Vector2(0f, 0.45f), Vector2.one);
            readyTitle.rectTransform.offsetMin = new Vector2(textLeft, 0f);
            var target = Text(ready, "Target", "ANDAR 2", TitleSize, WhiteText, font, Vector2.zero, new Vector2(1f, 0.5f));
            target.rectTransform.offsetMin = new Vector2(textLeft, 0f);
            ready.gameObject.SetActive(false);

            var view = container.AddComponent<NextFloorButton>();
            GrayboxSetup.SetReferences(view, ("runner", Object.FindAnyObjectByType<MiningRunner>()), ("texts", texts),
                ("button", button), ("visual", bar), ("fill", fillRect), ("lit", litRect),
                ("filling", filling.gameObject), ("title", title), ("count", count),
                ("ready", ready.gameObject), ("readyTitle", readyTitle), ("target", target), ("glow", glow));
            Debug.Log("[Setup] Botão Próximo Andar embaixo à direita: o botão é a barra (M9c E2, D-269).");
            return true;
        }

        /// <summary>O "x/y" chama "Value", como todo número da interface (a regra das fontes).</summary>
        static bool NameTheCountValue(Transform safeArea)
        {
            var count = safeArea.Find(ContainerName + "/Bar/NextFloorButton/Filling/Count");
            if (count == null) return false;
            count.name = "Value";
            var numbers = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.NumbersFontPath);
            if (numbers != null && count.TryGetComponent(out TMP_Text text))
            {
                text.font = numbers;
                text.fontSharedMaterial = numbers.material;
            }
            EditorUtility.SetDirty(count.gameObject);
            return true;
        }

        /// <summary>O Descansar na largura nova da barra (o centro fica para a Bomba): ícone e margens menores.</summary>
        static bool FitTheRestButton(Transform safeArea)
        {
            var rect = safeArea.Find("Rest/RestButton") as RectTransform;
            if (rect == null || Mathf.Approximately(rect.sizeDelta.x, HudSkinSetup.BarButtonWidth)) return false;
            HudSkinSetup.PlaceOnTheBar(rect, right: false);
            if (rect.TryGetComponent(out HorizontalLayoutGroup row))
            {
                row.padding = new RectOffset((int)RestPadding, (int)RestPadding, 0, 0);
                row.spacing = 2f;
            }
            if (rect.Find("Icon") is Transform icon && icon.TryGetComponent(out LayoutElement element))
                element.minWidth = element.preferredWidth = element.minHeight = element.preferredHeight = RestIconSize;
            if (rect.Find("Value") is RectTransform value)
                value.offsetMin = new Vector2(RestPadding + RestIconSize + 2f, value.offsetMin.y);
            EditorUtility.SetDirty(rect.gameObject);
            Debug.Log("[Setup] Descansar mais estreito: o centro da barra de baixo fica para a Bomba (M9c E2, D-269).");
            return true;
        }

        /// <summary>P9: o painel do HUD diz "ANDAR" (o objeto continua "Caverna").</summary>
        static bool RenameHudLabel(Transform safeArea)
        {
            var hud = Object.FindAnyObjectByType<HudView>(FindObjectsInactive.Include);
            var label = hud != null ? hud.transform.Find("Caverna/Label")?.GetComponent<TMP_Text>() : null;
            if (label == null || label.text != OldHudLabel) return false;
            label.text = HudLabel;
            EditorUtility.SetDirty(label);
            Debug.Log("[Setup] HUD: CAVERNA vira ANDAR (M9c E2, P9).");
            return true;
        }

        static Image SlicedImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch((RectTransform)go.transform, 0f);
            return go.transform;
        }

        static TMP_Text Text(Transform parent, string name, string value, float size, Color color, TMP_FontAsset font,
                             Vector2 anchorMin, Vector2 anchorMax)
        {
            var text = GrayboxSetup.CreateHudText(parent, name, value, size, color, anchorMin, anchorMax);
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            if (font != null)
            {
                text.font = font;
                text.fontSharedMaterial = font.material;
            }
            return text;
        }

        static void Stretch(RectTransform rect, float outset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(outset, outset);
            rect.offsetMax = new Vector2(-outset, -outset);
        }
    }
}
