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
    /// Parte do <see cref="UiSkinSetup"/> para a E3 do M1b: a loja em madeira. Ícone e moldura de cada melhoria no
    /// catálogo (dados, D-66), a linha da loja (prefab) com fundo afundado, moldura colorida e o botão comprar rubi/cinza,
    /// e a janela com barra de título e VOLTAR. Gera também o ícone da Picareta Dupla (duas picaretas do Bruno cruzadas).
    ///
    /// Sem o kit de madeira (fora do git, D-65) nada muda: fica o graybox. Idempotente; não sobrescreve ícone ou moldura
    /// escolhidos à mão no catálogo.
    /// </summary>
    static class ShopSkinSetup
    {
        const string CatalogPath = "Assets/_Project/Data/ShopCatalog.asset";
        const string RowPrefabPath = "Assets/_Project/Prefabs/UI/UpgradeRow.prefab";
        const string PickaxeIconPath = "Assets/_Project/Art/UI/icon_pickaxe-export.png";
        const string DoublePickaxeIconPath = "Assets/_Project/Art/UI/icon_double_pickaxe.png";
        const string SpeedIconPath = "Assets/_Project/Art/UI/icon_speed.png";

        // Linha da loja (referência 270): moldura 38 com o ícone de 32 px de arte em 32 (4 px de tela por pixel de arte
        // no Moto G75); textos depois da moldura; botão de 30 px de altura (mínimo de toque).
        const float RowHeight = 46f;
        const float FrameSize = 38f;
        const float FrameLeft = 4f;
        const float IconSize = 32f;
        const float TextLeft = FrameLeft + FrameSize + 6f;
        const float BuyAreaWidth = 6f + 66f + 4f;   // margem + botão + folga

        /// <summary>A janela começa 4 px abaixo do HUD (que termina a 42 px do topo).</summary>
        const float WindowTop = 46f;

        // Janela: a tábua da barra de título do kit vai da linha 6 à 13 do sprite (medido); texto de 8,25 px cabe nela.
        const float TitleBarTop = 6f;
        const float TitleBarHeight = 8f;
        const float TitleSize = 33f / 4f;

        static readonly Color TextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        static readonly Color UnavailableTextColor = new Color32(0xBE, 0xB4, 0xAA, 0xFF);

        static string Wood(string name) => UiSkinSetup.WoodFolder + "/" + name + ".png";
        static string Fantasy(string name) => UiSkinSetup.FantasyFolder + "/" + name + ".png";
        static string Ponk(int number) => UiSkinSetup.IconsFolder + "/Icon" + number + ".png";
        static Sprite Load(string path) => File.Exists(path) ? AssetDatabase.LoadAssetAtPath<Sprite>(path) : null;

        /// <summary>
        /// A Picareta Dupla: a picareta do Bruno com uma cópia espelhada por cima (arte do projeto, vai no git). Antes da
        /// importação dos sprites, para sair como pixel art também.
        /// </summary>
        public static void EnsureGeneratedIcons()
        {
            if (File.Exists(DoublePickaxeIconPath) || !File.Exists(PickaxeIconPath)) return;

            var pickaxe = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            pickaxe.LoadImage(File.ReadAllBytes(PickaxeIconPath));
            int w = pickaxe.width, h = pickaxe.height;
            var source = pickaxe.GetPixels32();
            var result = (Color32[])source.Clone();
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var mirrored = source[y * w + (w - 1 - x)];
                if (mirrored.a > 0) result[y * w + x] = mirrored;
            }

            var output = new Texture2D(w, h, TextureFormat.RGBA32, false);
            output.SetPixels32(result);
            File.WriteAllBytes(DoublePickaxeIconPath, output.EncodeToPNG());
            Object.DestroyImmediate(pickaxe);
            Object.DestroyImmediate(output);
            AssetDatabase.ImportAsset(DoublePickaxeIconPath);
            Debug.Log($"[Setup] Ícone da Picareta Dupla gerado em {DoublePickaxeIconPath}.");
        }

        /// <summary>Catálogo, prefab da linha e a loja na cena Game (já aberta). Devolve se mudou a cena.</summary>
        public static bool EnsureInScene()
        {
            if (Load(Wood("panels/panel_inset")) == null) return false;

            EnsureCatalogArt();
            EnsureRowPrefab();
            return SkinShopWindow() | RubyBackButton();
        }

        // ── Catálogo ─────────────────────────────────────────────────────────────────────────────────────────────────

        static void EnsureCatalogArt()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(CatalogPath);
            if (catalog == null) return;

            var serialized = new SerializedObject(catalog);
            var entries = serialized.FindProperty("entries");
            int lockedIndex = 0;
            bool changed = false;
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                var (icon, frame) = ArtFor(entry.FindPropertyRelative("locked").boolValue,
                                           (UpgradeId)entry.FindPropertyRelative("upgrade").enumValueIndex,
                                           ref lockedIndex);
                changed |= SetIfEmpty(entry.FindPropertyRelative("icon"), icon);
                changed |= SetIfEmpty(entry.FindPropertyRelative("frame"), frame);
            }

            if (!changed) return;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Ícones e molduras da loja no catálogo (D-66).");
        }

        /// <summary>
        /// Ícone e moldura (cor de raridade) de cada linha. As bloqueadas, pela ordem: Estagiário (capacete), Letrado
        /// (livro). Picareta comum, Velocidade raro, Mineração épico, Picareta Dupla lendário, Botas incomum.
        /// </summary>
        static (Sprite icon, Sprite frame) ArtFor(bool locked, UpgradeId upgrade, ref int lockedIndex)
        {
            if (locked)
                return (Load(Ponk(lockedIndex++ == 0 ? 165 : 299)), Load(Wood("slots/slot_disabled")));

            switch (upgrade)
            {
                case UpgradeId.Pickaxe: return (Load(PickaxeIconPath), Load(Wood("slots/slot_rarity_common")));
                case UpgradeId.Speed: return (Load(SpeedIconPath), Load(Wood("slots/slot_rarity_rare")));
                case UpgradeId.Mining: return (Load(Ponk(288)), Load(Wood("slots/slot_rarity_epic")));
                case UpgradeId.DoublePickaxe: return (Load(DoublePickaxeIconPath), Load(Wood("slots/slot_rarity_legendary")));
                case UpgradeId.Boots: return (Load(Ponk(224)), Load(Wood("slots/slot_rarity_uncommon")));
                default: return (null, null);
            }
        }

        static bool SetIfEmpty(SerializedProperty property, Object value)
        {
            if (value == null || property.objectReferenceValue != null) return false;
            property.objectReferenceValue = value;
            return true;
        }

        // ── Linha da loja (prefab) ───────────────────────────────────────────────────────────────────────────────────

        static void EnsureRowPrefab()
        {
            if (!File.Exists(RowPrefabPath)) return;
            var root = PrefabUtility.LoadPrefabContents(RowPrefabPath);
            try
            {
                var effect = root.transform.Find("Effect").GetComponent<TMP_Text>();
                var currentFrame = root.transform.Find("Frame");
                if (currentFrame != null && effect.textWrappingMode == TextWrappingModes.Normal
                    && currentFrame.GetSiblingIndex() < root.transform.Find("Icon").GetSiblingIndex()) return;

                var background = root.GetComponent<Image>();
                background.sprite = Load(Wood("panels/panel_inset"));
                background.type = Image.Type.Sliced;
                background.color = Color.white;
                root.GetComponent<LayoutElement>().preferredHeight = RowHeight;

                // Moldura atrás do ícone.
                var existing = root.transform.Find("Frame");
                var frame = existing != null
                    ? existing.gameObject
                    : new GameObject("Frame", typeof(RectTransform), typeof(Image));
                frame.transform.SetParent(root.transform, false);
                // A moldura vem logo antes do ícone (desenhada atrás dele).
                var iconTransform = root.transform.Find("Icon");
                frame.transform.SetSiblingIndex(iconTransform.GetSiblingIndex());
                if (frame.transform.GetSiblingIndex() > iconTransform.GetSiblingIndex())
                    iconTransform.SetSiblingIndex(frame.transform.GetSiblingIndex());
                var frameRect = (RectTransform)frame.transform;
                LeftMiddle(frameRect, FrameLeft, FrameSize);
                var frameImage = frame.GetComponent<Image>();
                frameImage.type = Image.Type.Sliced;
                frameImage.raycastTarget = false;

                LeftMiddle((RectTransform)root.transform.Find("Icon"), FrameLeft + (FrameSize - IconSize) / 2f, IconSize);

                foreach (var name in new[] { "Title", "Level", "Effect" })
                {
                    var rect = (RectTransform)root.transform.Find(name);
                    rect.offsetMin = new Vector2(TextLeft, rect.offsetMin.y);
                }
                // A descrição das linhas bloqueadas é longa: quebra em duas linhas antes do botão.
                effect.textWrappingMode = TextWrappingModes.Normal;
                effect.rectTransform.offsetMax = new Vector2(-BuyAreaWidth, effect.rectTransform.offsetMax.y);

                // Comprar: rubi quando dá; madeira cinza quando não dá (o Button troca o sprite sozinho).
                var buy = root.transform.Find("Buy");
                var buyImage = buy.GetComponent<Image>();
                buyImage.sprite = Load(Fantasy("buttons/button_ruby_normal"));
                buyImage.type = Image.Type.Sliced;
                buyImage.color = Color.white;
                var button = buy.GetComponent<Button>();
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState
                {
                    pressedSprite = Load(Fantasy("buttons/button_ruby_pressed")),
                    disabledSprite = Load(Wood("buttons/button_medium_disabled")),
                };

                var view = root.GetComponent<UpgradeRowView>();
                GrayboxSetup.SetReferences(view, ("frame", frameImage));
                var so = new SerializedObject(view);
                so.FindProperty("buyableLabelColor").colorValue = TextColor;
                so.FindProperty("unavailableLabelColor").colorValue = UnavailableTextColor;
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, RowPrefabPath);
                Debug.Log($"[Setup] Linha da loja com a pele de madeira: {RowPrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void LeftMiddle(RectTransform rect, float left, float size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(left, 0f);
            rect.sizeDelta = new Vector2(size, size);
        }

        // ── Janela da loja (cena) ────────────────────────────────────────────────────────────────────────────────────

        static bool SkinShopWindow()
        {
            var window = Load(Wood("panels/window"));
            var panel = GameObject.Find("SafeArea")?.transform.Find("Shop/Screen/Panel");
            if (panel == null || window == null) return false;

            var image = panel.GetComponent<Image>();
            var title = panel.Find("Title").GetComponent<TMP_Text>();
            if (image.sprite == window && Mathf.Approximately(title.fontSize, TitleSize)) return false;
            image.sprite = window;
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var panelRect = (RectTransform)panel;
            panelRect.offsetMax = new Vector2(panelRect.offsetMax.x, -WindowTop);

            // "LOJA" e o saldo centralizados na barra de título (o "Left" do TMP também alinha em cima).
            var balance = panel.Find("Balance").GetComponent<TMP_Text>();
            TitleBar(title.rectTransform, left: 10f, right: 0f);
            TitleBar(balance.rectTransform, left: 0f, right: 10f);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            balance.alignment = TextAlignmentOptions.MidlineRight;
            // Texto claro: o âmbar sobre a madeira média da tábua tem pouco contraste.
            title.fontSize = balance.fontSize = TitleSize;
            title.color = balance.color = TextColor;

            var rows = (RectTransform)panel.Find("Rows");
            rows.offsetMin = new Vector2(7f, 44f);
            rows.offsetMax = new Vector2(-7f, -20f);

            var back = (RectTransform)panel.Find("Back");
            back.anchoredPosition = new Vector2(0f, 8f);
            back.sizeDelta = new Vector2(120f, 30f);
            var backImage = back.GetComponent<Image>();
            backImage.sprite = Load(Wood("buttons/button_large_normal"));
            backImage.type = Image.Type.Sliced;
            backImage.color = Color.white;
            var backButton = back.GetComponent<Button>();
            backButton.transition = Selectable.Transition.SpriteSwap;
            backButton.spriteState = new SpriteState { pressedSprite = Load(Wood("buttons/button_large_pressed")) };
            back.Find("Label").GetComponent<TMP_Text>().color = TextColor;

            Debug.Log("[Setup] Janela da loja com a pele de madeira (M1b E3).");
            return true;
        }

        /// <summary>VOLTAR no rubi, como o botão de comprar quando dá (pedido do Bruno). Idempotente.</summary>
        static bool RubyBackButton()
        {
            var back = GameObject.Find("SafeArea")?.transform.Find("Shop/Screen/Panel/Back");
            var normal = Load(Fantasy("buttons/button_ruby_normal"));
            if (back == null || normal == null) return false;
            var image = back.GetComponent<Image>();
            if (image.sprite == normal) return false;

            image.sprite = normal;
            image.type = Image.Type.Sliced;
            back.GetComponent<Button>().spriteState = new SpriteState { pressedSprite = Load(Fantasy("buttons/button_ruby_pressed")) };
            Debug.Log("[Setup] VOLTAR da loja no botão rubi.");
            return true;
        }

        static void TitleBar(RectTransform rect, float left, float right)
        {
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -TitleBarTop - TitleBarHeight);
            rect.offsetMax = new Vector2(-right, -TitleBarTop);
        }
    }
}
