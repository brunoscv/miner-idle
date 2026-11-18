using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static MinersIdle.EditorTools.RareWindowSetup;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="GrayboxSetup"/> para a E3 do M9e (mockups da E0, <c>tools/ci/out/m9e-e0-*.png</c>; P6, P7, P4):
    /// <list type="bullet">
    /// <item>o ícone do Raro Dourado (<c>Art/UI/icon_golden_rare.png</c>, o cristal da carta em dourado) como sprite;</item>
    /// <item>no <c>Data/BuffCatalog</c>, os anúncios do Boost (origem Ad, tier = o <see cref="AdKind"/>), os textos do menu
    /// e o ícone próprio da carta Dano de Crítico (a estrela laranja, Icon332, no lugar do do Crítico);</item>
    /// <item>o menu do Boost ("BoostScreen", <see cref="BoostScreen"/>) antes do anúncio simulado (o anúncio abre por cima);</item>
    /// <item>o botão AD da coluna abre o menu (<see cref="AdButtonView"/>, quando o balanço tem os anúncios);</item>
    /// <item>o selo "×4" nos ícones da coluna dos bônus (<see cref="BuffSlot"/>);</item>
    /// <item>a linha Raro Dourado na tela do descanso (<c>Data/ShopCatalog</c>, depois da Promoção).</item>
    /// </list>
    /// Só cria o que falta; os textos ficam nos catálogos.
    /// </summary>
    static class BoostSetup
    {
        const string ScreenName = "BoostScreen";
        internal const string GoldenIconPath = "Assets/_Project/Art/UI/icon_golden_rare.png";
        const string ShopCatalogPath = "Assets/_Project/Data/ShopCatalog.asset";

        // O mockup (900 × 2000 → 270 × 480): o painel no meio, um pouco acima; quatro linhas de 38; VOLTAR embaixo.
        const float PanelWidth = 252f;
        const float PanelHeight = 236f;
        const float PanelRaise = 48f;
        const float RowLeft = 6f;
        const float RowTop = 34f;
        const float RowWidth = PanelWidth - 2f * RowLeft;
        const float RowHeight = 38f;
        const float RowGap = 2f;
        const float IconBox = 32f;
        const float TextLeft = 40f;
        const float ButtonWidth = 66f;
        const float ButtonHeight = 18f;
        const float BackWidth = 110f;
        const float BackHeight = 24f;

        const float SmallText = 33f / 4f;
        const float TinyText = 7f;
        static readonly Color DimColor = new Color(0f, 0f, 0f, 0.6f);
        static readonly Color TextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        static readonly Color AmberText = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        static readonly Color MutedText = new Color32(0xBE, 0xB4, 0xAA, 0xFF);
        static readonly Color BadgeBack = new Color32(0x18, 0x10, 0x0A, 0xE6);

        static readonly (AdKind kind, string title, string description, string gain)[] Entries =
        {
            (AdKind.Short, "Anúncio curto", "{0} dano, lascas e crítico", "{0} em dano, lascas e crítico"),
            (AdKind.Medium, "Anúncio médio", "{0} dano, lascas e crítico", "{0} em dano, lascas e crítico"),
            (AdKind.Big, "Anúncio grande", "{0} dano, lascas e crítico", "{0} em dano, lascas e crítico"),
            (AdKind.RareTier2, "Raro tier 2", "Cartas do raro ×4 e +20%", "cartas do raro no tier 2"),
        };

        static TMP_FontAsset _font, _numbers;

        public static bool EnsureInScene()
        {
            bool changed = EnsureSprite();
            var catalog = AssetDatabase.LoadAssetAtPath<BuffCatalog>(BuffColumnSetup.CatalogPath);
            if (catalog == null) return changed;
            changed |= EnsureCatalog(catalog);
            changed |= EnsureGoldenRareRow();

            var safeArea = GameObject.Find("SafeArea")?.transform;
            var runner = Object.FindAnyObjectByType<MiningRunner>();
            if (safeArea == null || runner == null) return changed;
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.GameFontPath);
            _numbers = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.NumbersFontPath);

            changed |= EnsureScreen(safeArea, runner, catalog);
            changed |= EnsureButton(safeArea);
            changed |= EnsureBadge();
            return changed;
        }

        static bool EnsureSprite()
        {
            if (!System.IO.File.Exists(GoldenIconPath)) return false;
            if (AssetImporter.GetAtPath(GoldenIconPath) == null) AssetDatabase.ImportAsset(GoldenIconPath);
            if (!(AssetImporter.GetAtPath(GoldenIconPath) is TextureImporter importer)) return false;
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

        static Sprite Golden => AssetDatabase.LoadAssetAtPath<Sprite>(GoldenIconPath);
        static Sprite VendorIcon(int number) => Load(UiSkinSetup.IconsFolder + "/Icon" + number + ".png");

        /// <summary>As linhas dos anúncios, os textos do menu e o ícone da carta Dano de Crítico; só o que falta.</summary>
        static bool EnsureCatalog(BuffCatalog catalog)
        {
            var so = new SerializedObject(catalog);
            var list = so.FindProperty("entries");
            int added = 0;
            foreach (var (kind, title, description, gain) in Entries)
            {
                if (catalog.Find(BonusSource.Ad, (int)kind) != null) continue;
                int index = list.arraySize;
                list.arraySize = index + 1;
                var item = list.GetArrayElementAtIndex(index);
                item.FindPropertyRelative("source").intValue = (int)BonusSource.Ad;
                item.FindPropertyRelative("tier").intValue = (int)kind;
                item.FindPropertyRelative("icon").objectReferenceValue =
                    kind == AdKind.RareTier2 ? Golden : AssetDatabase.LoadAssetAtPath<Sprite>(AdSetup.BoltIconPath);
                item.FindPropertyRelative("title").stringValue = title;
                item.FindPropertyRelative("description").stringValue = description;
                item.FindPropertyRelative("headline").stringValue = "";
                item.FindPropertyRelative("gain").stringValue = gain;
                item.FindPropertyRelative("cardText").stringValue = "";
                item.FindPropertyRelative("cardNote").stringValue = "";
                item.FindPropertyRelative("forIntern").boolValue = kind != AdKind.RareTier2;
                added++;
            }

            // A carta Dano de Crítico ganha o próprio ícone (no M9e E1 usava o do Crítico, Icon18).
            bool icon = false;
            var crit = VendorIcon(18);
            var critDamage = VendorIcon(332);
            for (int i = 0; i < list.arraySize && critDamage != null; i++)
            {
                var item = list.GetArrayElementAtIndex(i);
                if (item.FindPropertyRelative("source").intValue != (int)BonusSource.Rare
                    || item.FindPropertyRelative("tier").intValue != (int)RareCard.CritDamage) continue;
                var field = item.FindPropertyRelative("icon");
                if (field.objectReferenceValue != null && field.objectReferenceValue != crit) continue;
                field.objectReferenceValue = critDamage;
                icon = true;
            }

            bool texts = string.IsNullOrEmpty(catalog.BoostTitle);
            if (texts)
            {
                so.FindProperty("boostTitle").stringValue = "BOOST";
                so.FindProperty("boostNote").stringValue = "Tipos diferentes juntos: vale o maior.";
                so.FindProperty("boostWatch").stringValue = "ASSISTIR";
                so.FindProperty("boostRenew").stringValue = "RENOVAR";
                so.FindProperty("boostLeftFormat").stringValue = "falta {0}";
                so.FindProperty("boostDetailFormat").stringValue = "Crítico {0} · offline +{1}";
                so.FindProperty("boostTier2Note").stringValue = "Some com o Raro Dourado";
                so.FindProperty("boostBack").stringValue = "VOLTAR";
                so.FindProperty("adOfferRewardFormat").stringValue = "Recompensa: {0} por {1}";
                so.FindProperty("adOfferFooterFormat").stringValue = "Anúncio · falta {0}";
            }
            if (added == 0 && !icon && !texts) return false;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Setup] Catálogo dos bônus: {added} anúncios do Boost, os textos do menu e o ícone do Dano de Crítico (M9e E3).");
            return true;
        }

        /// <summary>A linha Raro Dourado (P4) depois da Promoção, com o cristal dourado na moldura lendária.</summary>
        static bool EnsureGoldenRareRow()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(ShopCatalogPath);
            if (catalog == null) return false;
            var so = new SerializedObject(catalog);
            var entries = so.FindProperty("permanentEntries");
            if (entries.arraySize == 0) return false;
            int index = entries.arraySize;
            for (int i = 0; i < entries.arraySize; i++)
            {
                var existing = (PermanentId)entries.GetArrayElementAtIndex(i).FindPropertyRelative("permanent").enumValueIndex;
                if (existing == PermanentId.GoldenRare) return false;
                if (existing == PermanentId.InternPromotion) index = i + 1;
            }
            entries.InsertArrayElementAtIndex(index);
            var entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("permanent").enumValueIndex = (int)PermanentId.GoldenRare;
            entry.FindPropertyRelative("title").stringValue = "Raro Dourado";
            entry.FindPropertyRelative("effectFormat").stringValue = "Cartas do raro ×2 » ×4";
            entry.FindPropertyRelative("maxedFormat").stringValue = "Cartas do raro ×4";
            entry.FindPropertyRelative("lockedFormat").stringValue = "Recorde no andar {0}";
            entry.FindPropertyRelative("style").enumValueIndex = (int)ShopCatalog.EffectStyle.Number;
            var (icon, frame) = PermanentShopSetup.ArtFor(PermanentId.GoldenRare);
            entry.FindPropertyRelative("icon").objectReferenceValue = icon;
            entry.FindPropertyRelative("frame").objectReferenceValue = frame;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Descanso: a linha Raro Dourado depois da Promoção (M9e E3, P4).");
            return true;
        }

        /// <summary>O menu do Boost (P6): escurece e bloqueia o mapa; o anúncio simulado abre por cima.</summary>
        static bool EnsureScreen(Transform safeArea, MiningRunner runner, BuffCatalog catalog)
        {
            var ad = safeArea.Find("AdScreen")?.GetComponent<SimulatedAdScreen>();
            if (safeArea.Find(ScreenName) != null || ad == null || Load(Wood("panels/window")) == null) return false;

            var root = new GameObject(ScreenName, typeof(RectTransform));
            root.transform.SetParent(safeArea, false);
            root.transform.SetSiblingIndex(ad.transform.GetSiblingIndex()); // logo antes do anúncio
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

            var title = Text(panel.transform, "Title", SmallText, TextColor, TextAlignmentOptions.MidlineLeft, _font);
            Place(title.rectTransform, 8f, 3f, PanelWidth - 16f, 12f);
            var note = Text(panel.transform, "Note", TinyText, MutedText, TextAlignmentOptions.Center, _font);
            Place(note.rectTransform, 8f, 20f, PanelWidth - 16f, 10f);

            var adIcon = AssetDatabase.LoadAssetAtPath<Sprite>(AdSetup.AdIconPath);
            var kinds = new[] { AdKind.Short, AdKind.Medium, AdKind.Big, AdKind.RareTier2 };
            var rows = new BoostRowView[kinds.Length];
            for (int i = 0; i < kinds.Length; i++)
                rows[i] = Row(panel.transform, kinds[i], RowTop + i * (RowHeight + RowGap),
                              kinds[i] == AdKind.RareTier2 ? Golden : adIcon);

            var (back, backLabel) = RubyButton(panel.transform, "Back", (PanelWidth - BackWidth) / 2f,
                                               PanelHeight - BackHeight - 10f, BackWidth, BackHeight);

            screen.SetActive(false);
            var view = root.AddComponent<BoostScreen>();
            GrayboxSetup.SetReferences(view, ("runner", runner), ("catalog", catalog), ("rewardedAd", ad),
                ("screen", screen), ("title", title), ("note", note), ("backButton", back), ("backLabel", backLabel));
            var so = new SerializedObject(view);
            var list = so.FindProperty("rows");
            list.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = rows[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Setup] Menu do Boost na cena Game, logo antes do anúncio simulado (M9e E3, P6).");
            return true;
        }

        /// <summary>Uma linha: ícone na moldura, nome e duração, o bônus, o crítico e as horas, ASSISTIR e o "falta".</summary>
        static BoostRowView Row(Transform panel, AdKind kind, float top, Sprite iconSprite)
        {
            var row = Picture(panel, "Row" + kind, Load(Wood("panels/panel_inset")));
            row.type = Image.Type.Sliced;
            Place(row.rectTransform, RowLeft, top, RowWidth, RowHeight);

            var frame = Picture(row.transform, "Frame",
                                PermanentShopSetup.Frame(kind == AdKind.RareTier2 ? "slot_rarity_legendary" : "slot_rarity_common"));
            frame.type = Image.Type.Sliced;
            Place(frame.rectTransform, 3f, 3f, IconBox, IconBox);
            var icon = Picture(frame.transform, "Icon", iconSprite);
            icon.preserveAspect = true;
            Place(icon.rectTransform, 4f, 4f, IconBox - 8f, IconBox - 8f);

            float textWidth = RowWidth - TextLeft - ButtonWidth - 6f;
            var title = Text(row.transform, "Title", SmallText, AmberText, TextAlignmentOptions.MidlineLeft, _font);
            Place(title.rectTransform, TextLeft, 2f, textWidth, 11f);
            var duration = Text(row.transform, "Duration", TinyText, TextColor, TextAlignmentOptions.MidlineRight, _font);
            Place(duration.rectTransform, TextLeft, 2f, textWidth, 11f);
            var effect = Text(row.transform, "Effect", TinyText, TextColor, TextAlignmentOptions.MidlineLeft, _font);
            Place(effect.rectTransform, TextLeft, 14f, textWidth, 10f);
            var detail = Text(row.transform, "Detail", TinyText, MutedText, TextAlignmentOptions.MidlineLeft, _font);
            Place(detail.rectTransform, TextLeft, 25f, textWidth, 10f);
            foreach (var text in new[] { effect, detail })
            {
                text.enableAutoSizing = true;
                text.fontSizeMin = 5f;
                text.fontSizeMax = TinyText;
            }

            var (button, label) = RubyButton(row.transform, "Watch", RowWidth - ButtonWidth - 3f, 3f, ButtonWidth, ButtonHeight);
            var left = Text(row.transform, "Left", UiSkinSetup.NumbersFontSize, AmberText, TextAlignmentOptions.Center, _numbers);
            Place(left.rectTransform, RowWidth - ButtonWidth - 3f, 24f, ButtonWidth, 11f);
            left.gameObject.SetActive(false);

            var view = row.gameObject.AddComponent<BoostRowView>();
            GrayboxSetup.SetReferences(view, ("title", title), ("duration", duration), ("effect", effect), ("detail", detail),
                ("button", button), ("buttonLabel", label), ("left", left));
            var so = new SerializedObject(view);
            so.FindProperty("kind").intValue = (int)kind;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        /// <summary>O botão rubi (como o RECOMPENSA do anúncio); apagado, a madeira cinza (como o comprar da loja).</summary>
        static (Button, TMP_Text) RubyButton(Transform parent, string name, float left, float top, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Place((RectTransform)go.transform, left, top, width, height);
            var image = go.GetComponent<Image>();
            image.sprite = Load(Fantasy("buttons/button_ruby_normal"));
            image.type = Image.Type.Sliced;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState
            {
                pressedSprite = Load(Fantasy("buttons/button_ruby_pressed")),
                disabledSprite = Load(Wood("buttons/button_medium_disabled")),
            };
            var label = Text(go.transform, "Label", SmallText, TextColor, TextAlignmentOptions.Center, _font);
            Stretch(label.rectTransform);
            return (button, label);
        }

        /// <summary>O AD da coluna abre o menu quando o balanço tem os anúncios do Boost (no v7, o anúncio de antes).</summary>
        static bool EnsureButton(Transform safeArea)
        {
            var view = safeArea.Find("AdButton")?.GetComponent<AdButtonView>();
            var boost = safeArea.Find(ScreenName)?.GetComponent<BoostScreen>();
            if (view == null || boost == null) return false;
            var so = new SerializedObject(view);
            if (so.FindProperty("boost").objectReferenceValue != null) return false;
            GrayboxSetup.SetReferences(view, ("boost", boost));
            Debug.Log("[Setup] Botão AD abre o menu do Boost no v8 (M9e E3, P6).");
            return true;
        }

        /// <summary>O selo "×4" no canto de baixo à direita de cada ícone da coluna (P7), no modelo das cópias.</summary>
        static bool EnsureBadge()
        {
            var column = Object.FindAnyObjectByType<BuffColumn>(FindObjectsInactive.Include);
            if (column == null) return false;
            var template = new SerializedObject(column).FindProperty("slotTemplate").objectReferenceValue as BuffSlot;
            if (template == null || new SerializedObject(template).FindProperty("badge").objectReferenceValue != null) return false;

            var root = new GameObject("Badge", typeof(RectTransform));
            root.transform.SetParent(template.transform, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(2f, -1f);
            rect.sizeDelta = new Vector2(12f, 7f);
            var back = new GameObject("Back", typeof(RectTransform), typeof(Image));
            back.transform.SetParent(root.transform, false);
            Stretch((RectTransform)back.transform);
            var image = back.GetComponent<Image>();
            image.color = BadgeBack;
            image.raycastTarget = false;
            var text = GrayboxSetup.CreateHudText(root.transform, "Text", "", 5f, AmberText, Vector2.zero, Vector2.one);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            if (_numbers != null)
            {
                text.font = _numbers;
                text.fontSharedMaterial = _numbers.material;
            }
            root.SetActive(false);
            GrayboxSetup.SetReferences(template, ("badge", text), ("badgeRoot", root));
            EditorUtility.SetDirty(template);
            Debug.Log("[Setup] Coluna dos bônus: o selo do anúncio (\"×4\") nos ícones (M9e E3, P7).");
            return true;
        }
    }
}
