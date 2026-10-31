using System.IO;
using MinersIdle.Game;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="UiSkinSetup"/> para a E2 do M1b, na cena Game já aberta: os 4 painéis do HUD em madeira (nome
    /// em cima; ícone e valor embaixo, centralizados juntos) e a coluna da direita com LOJA, AD e AJUSTES do mesmo tamanho
    /// (D-67). AD e AJUSTES só mostram "Em breve" até o M9. Os textos são dados da cena.
    ///
    /// Sem o kit de madeira (fora do git, D-65) nada muda: fica o graybox. Idempotente.
    /// </summary>
    static class HudSkinSetup
    {
        const string CatalogPath = "Assets/_Project/Data/ShopCatalog.asset";
        const string ArtFolder = "Assets/_Project/Art";

        // Layout na referência 270 (D-14). O HUD vai de 4 a 42 px do topo; a loja já começa em 42 (ShopSetup.HudBottom).
        const float HudTop = 4f;
        const float HudHeight = 38f;
        const float HudSpacing = 2f;
        const float SideSize = 46f;    // cabe "AJUSTES"; ≥ 30 px, o mínimo de toque do Android no Moto G75
        const float SideGap = 4f;
        const float SideTop = HudTop + HudHeight + SideGap;
        const float Margin = 4f;

        // Ícones de 32 px de arte (salvos ampliados 2×): 16 px no HUD e 24 px nos botões = 2 e 3 px de tela por pixel de
        // arte no Moto G75. A engrenagem do kit já está na escala da UI: fica com 16 px (ampliada 2×, invadia o nome).
        const float HudIconSize = 16f;
        const float SideIconSize = 24f;
        const float KitIconSize = 16f;

        /// <summary>Nos botões da coluna, o ícone fica centralizado entre o topo (4 px) e o nome (a 29 px do topo).</summary>
        const float SideIconAreaTop = 4f;
        const float SideLabelTop = SideSize - 17f;

        const float LabelSize = 33f / 4f;   // 8,25: grid da Pixelify (UiSkinSetup.FontPixelStep × 3)
        const float ValueSize = 11f;

        static readonly Color LabelColor = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        static readonly Color DisabledLabelColor = new Color32(0xBE, 0xB4, 0xAA, 0xFF);
        static readonly Color TextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);

        /// <summary>O nome do botão de passar de caverna (D-110; "Descer na Caverna" no GDD, curto para caber no quadrado).</summary>
        const string PassCaveTitle = "DESCER";
        /// <summary>Âmbar sobre a madeira (D-110): destaca o botão dos outros da coluna.</summary>
        static readonly Color AmberTint = new Color32(0xFF, 0xD0, 0x6A, 0xFF);

        static readonly (string panel, string icon)[] HudPanels =
        {
            ("Lascas", ArtFolder + "/Crystals/icon_crystal.png"),
            ("Dano", ArtFolder + "/UI/icon_pickaxe-export.png"),
            ("Caverna", ArtFolder + "/UI/icon_cave.png"),
            ("Velocidade", ArtFolder + "/UI/icon_speed.png"),
        };

        // Os ícones que o Bruno vai desenhar (D-66); enquanto não existem, o botão mostra só o nome.
        const string ShopIconPath = ArtFolder + "/UI/icon_shop.png";
        const string AdIconPath = ArtFolder + "/UI/icon_ad.png";
        const string SettingsIconPath = ArtFolder + "/UI/icon_settings.png";

        static string Wood(string name) => UiSkinSetup.WoodFolder + "/" + name + ".png";

        // Botões desenhados pelo Bruno (D-78): o ícone (com moldura) e o nome em PNG viram um botão só, gerado aqui: a
        // moldura do ícone esticada (cantos copiados), o desenho em cima e o nome embaixo, dentro do quadrado. A arte é
        // 2× (32 px salvos em 64); o botão de 104×80 vai a 52×40 px da referência = 2 px de tela por pixel no Moto G75.
        const string ShopNamePath = ArtFolder + "/UI/name_shop.png";
        const string SettingsNamePath = ArtFolder + "/UI/name_settings.png";
        internal const string ShopButtonPath = ArtFolder + "/UI/button_shop.png";
        internal const string SettingsButtonPath = ArtFolder + "/UI/button_settings.png";
        const int ArtWidth = 104, ArtHeight = 80, ArtBorder = 6, IconBottom = 44, NameTop = 52;
        const float ArtButtonWidth = ArtWidth / 2f, ArtButtonHeight = ArtHeight / 2f;
        static readonly Color PressedTint = new Color(0.72f, 0.72f, 0.72f, 1f);
        static string Fantasy(string name) => UiSkinSetup.FantasyFolder + "/" + name + ".png";
        static Sprite Load(string path) => File.Exists(path) ? AssetDatabase.LoadAssetAtPath<Sprite>(path) : null;

        public static bool EnsureInScene()
        {
            var panel = Load(Wood("panels/panel"));
            if (panel == null) return false;

            var safeArea = GameObject.Find("SafeArea");
            if (safeArea == null) return false;

            bool changed = SkinHud(safeArea.transform, panel);
            changed |= SkinShopButton(safeArea.transform);
            changed |= EnsureComingSoon(safeArea.transform, "AdButton", "AD", 1, Load(AdIconPath),
                                        Load(Wood("buttons/button_square_disabled")), null, DisabledLabelColor);
            changed |= EnsureComingSoon(safeArea.transform, "SettingsButton", "AJUSTES", 2, SettingsIcon(),
                                        Load(Wood("buttons/button_square_normal")),
                                        Load(Wood("buttons/button_square_pressed")), LabelColor);
            if (changed) Debug.Log("[Setup] HUD e botões da direita com a pele de madeira (M1b E2).");
            changed |= EnsurePassCaveButton(safeArea.transform);
            changed |= PlaceSideIcons(safeArea.transform);
            return ApplyArtButtons(safeArea.transform) | changed;
        }

        // ── HUD ──────────────────────────────────────────────────────────────────────────────────────────────────────

        static bool SkinHud(Transform safeArea, Sprite panel)
        {
            var hud = safeArea.Find("Hud");
            if (hud == null || hud.Find("Lascas/Row") != null) return false;

            var rect = (RectTransform)hud;
            rect.anchoredPosition = new Vector2(0f, -HudTop);
            rect.sizeDelta = new Vector2(-2f * Margin, HudHeight);
            hud.GetComponent<HorizontalLayoutGroup>().spacing = HudSpacing;

            foreach (var (name, iconPath) in HudPanels)
            {
                var node = hud.Find(name);
                if (node == null) continue;

                var image = node.GetComponent<Image>();
                image.sprite = panel;
                image.type = Image.Type.Sliced;
                image.color = Color.white;

                // Nome sozinho em cima.
                var label = node.Find("Label").GetComponent<TMP_Text>();
                Band(label.rectTransform, top: 5f, height: 10f);
                label.fontSize = LabelSize;

                // Embaixo, ícone e valor lado a lado, centralizados juntos (o grupo acompanha a largura do número).
                var row = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                row.transform.SetParent(node, false);
                Band((RectTransform)row.transform, top: 17f, height: 16f);
                var layout = row.GetComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.spacing = 3f;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = layout.childForceExpandHeight = false;

                var icon = Load(iconPath);
                if (icon != null) Icon(row.transform, icon, HudIconSize, layout: true);

                var value = node.Find("Value").GetComponent<TMP_Text>();
                value.transform.SetParent(row.transform, false);
                value.rectTransform.sizeDelta = new Vector2(0f, 16f);
                value.fontSize = ValueSize;
                EditorUtility.SetDirty(value);
            }
            return true;
        }

        /// <summary>Faixa horizontal a <paramref name="top"/> px do topo do pai, com <paramref name="height"/> de altura.</summary>
        static void Band(RectTransform rect, float top, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(2f, -top - height);
            rect.offsetMax = new Vector2(-2f, -top);
        }

        static Image Icon(Transform parent, Sprite sprite, float size, bool layout)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            ((RectTransform)go.transform).sizeDelta = new Vector2(size, size);
            if (layout)
            {
                var element = go.AddComponent<LayoutElement>();
                element.minWidth = element.preferredWidth = size;
            }
            return image;
        }

        // ── Coluna da direita ────────────────────────────────────────────────────────────────────────────────────────

        static bool SkinShopButton(Transform safeArea)
        {
            var button = safeArea.Find("Shop/ShopButton");
            // Já vestido (madeira ou o botão desenhado pelo Bruno) = tem sprite; o graybox não tinha.
            if (button == null || button.GetComponent<Image>().sprite != null) return false;

            SideButton((RectTransform)button, 0, Load(Wood("buttons/button_square_normal")),
                       Load(Wood("buttons/button_square_pressed")), Load(ShopIconPath),
                       button.Find("Label").GetComponent<TMP_Text>(), LabelColor);

            // O "!" do fantasy no lugar da bolinha vermelha (o desenho já tem o "!").
            var badge = button.Find("Badge");
            var marker = Load(Fantasy("markers/quest_available"));
            if (marker != null)
            {
                var image = badge.GetComponent<Image>();
                image.sprite = marker;
                image.color = Color.white;
                var rect = (RectTransform)badge;
                rect.anchoredPosition = new Vector2(-1f, -1f);
                rect.sizeDelta = new Vector2(marker.rect.width, marker.rect.height);
                badge.Find("Mark").gameObject.SetActive(false);
            }
            return true;
        }

        /// <summary>Botão quadrado da coluna (LOJA, AD, AJUSTES): ícone em cima e nome embaixo; sem ícone, só o nome.</summary>
        static void SideButton(RectTransform rect, int slot, Sprite normal, Sprite pressed, Sprite icon, TMP_Text label,
                               Color labelColor)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.anchoredPosition = new Vector2(-Margin, -SideTop - slot * (SideSize + SideGap));
            rect.sizeDelta = new Vector2(SideSize, SideSize);

            var image = rect.GetComponent<Image>();
            image.sprite = normal;
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var button = rect.GetComponent<Button>();
            button.targetGraphic = image;
            if (pressed != null)
            {
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState { pressedSprite = pressed };
            }
            else button.transition = Selectable.Transition.None;

            label.fontSize = LabelSize;
            label.color = labelColor;
            if (icon != null)
            {
                PlaceSideIcon(Icon(rect, icon, IconSize(icon), layout: false).rectTransform);
                Band(label.rectTransform, top: SideLabelTop, height: 12f);
            }
            else
            {
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            }
        }

        /// <summary>Gera os botões LOJA e AJUSTES (ícone e nome do Bruno num quadrado só), se os PNGs existirem.</summary>
        public static void EnsureArtButtonAssets()
        {
            int made = BuildArtButton(ShopIconPath, ShopNamePath, ShopButtonPath)
                       + BuildArtButton(SettingsIconPath, SettingsNamePath, SettingsButtonPath);
            if (made > 0) Debug.Log($"[Setup] Botões LOJA/AJUSTES gerados com o nome dentro do quadrado: {made} (D-78).");
        }

        static int BuildArtButton(string iconPath, string namePath, string outPath)
        {
            if (!File.Exists(iconPath) || !File.Exists(namePath)) return 0;
            if (File.Exists(outPath) && File.GetLastWriteTimeUtc(outPath) >= File.GetLastWriteTimeUtc(iconPath)
                && File.GetLastWriteTimeUtc(outPath) >= File.GetLastWriteTimeUtc(namePath))
                return 0;

            var icon = Read(iconPath);
            var name = Read(namePath);
            int size = icon.width, b = ArtBorder;
            var out_ = new Texture2D(ArtWidth, ArtHeight, TextureFormat.RGBA32, false);
            var fill = icon.GetPixel(b + 2, b + 2); // o fundo de dentro da moldura
            var pixels = new Color[ArtWidth * ArtHeight];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = fill;
            out_.SetPixels(pixels);

            // Moldura em 9 partes: cantos copiados; bordas repetem a linha/coluna do meio (são lisas).
            for (int y = 0; y < ArtHeight; y++)
            for (int x = 0; x < ArtWidth; x++)
            {
                bool edgeX = x < b || x >= ArtWidth - b, edgeY = y < b || y >= ArtHeight - b;
                if (!edgeX && !edgeY) continue;
                int sx = x < b ? x : x >= ArtWidth - b ? size - (ArtWidth - x) : size / 2;
                int sy = y < b ? y : y >= ArtHeight - b ? size - (ArtHeight - y) : size / 2;
                out_.SetPixel(x, y, icon.GetPixel(sx, sy));
            }

            // O desenho do ícone (sem a moldura), em cima e centralizado. Coordenadas de textura começam embaixo.
            int artWidth = size - 2 * b, artHeight = IconBottom - b;
            int ax = (ArtWidth - artWidth) / 2 / 2 * 2;
            for (int y = 0; y < artHeight; y++)
            for (int x = 0; x < artWidth; x++)
                out_.SetPixel(ax + x, ArtHeight - 1 - (b + y), icon.GetPixel(b + x, size - 1 - (b + y)));

            // O nome (só a parte escrita), centralizado embaixo do desenho.
            var bounds = Bounds(name);
            int nx = (ArtWidth - bounds.width) / 2 / 2 * 2;
            for (int y = 0; y < bounds.height; y++)
            for (int x = 0; x < bounds.width; x++)
            {
                var c = name.GetPixel(bounds.x + x, bounds.y + y);
                if (c.a <= 0f) continue;
                out_.SetPixel(nx + x, ArtHeight - NameTop - bounds.height + y, c);
            }

            out_.Apply();
            File.WriteAllBytes(outPath, out_.EncodeToPNG());
            Object.DestroyImmediate(icon);
            Object.DestroyImmediate(name);
            Object.DestroyImmediate(out_);
            AssetDatabase.ImportAsset(outPath);
            return 1;
        }

        static Texture2D Read(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(path));
            return texture;
        }

        /// <summary>Retângulo com pixels visíveis (em coordenadas de textura, que começam embaixo).</summary>
        static RectInt Bounds(Texture2D texture)
        {
            int x0 = texture.width, y0 = texture.height, x1 = -1, y1 = -1;
            for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < texture.width; x++)
            {
                if (texture.GetPixel(x, y).a <= 0f) continue;
                x0 = Mathf.Min(x0, x); x1 = Mathf.Max(x1, x);
                y0 = Mathf.Min(y0, y); y1 = Mathf.Max(y1, y);
            }
            return x1 < 0 ? new RectInt(0, 0, 0, 0) : new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
        }

        /// <summary>
        /// LOJA e AJUSTES com os botões gerados acima (ícone e nome dentro do mesmo quadrado); o texto some; ao tocar,
        /// escurece. A coluna (LOJA, AD, AJUSTES) é refeita com esse tamanho. Idempotente. Devolve se mudou algo.
        /// </summary>
        static bool ApplyArtButtons(Transform safeArea)
        {
            var shop = safeArea.Find("Shop/ShopButton") as RectTransform;
            var ad = safeArea.Find("AdButton") as RectTransform;
            var settings = safeArea.Find("SettingsButton") as RectTransform;
            if (shop == null || ad == null || settings == null) return false;

            bool changed = false;
            changed |= ArtButton(shop, Load(ShopButtonPath));
            changed |= ArtButton(settings, Load(SettingsButtonPath));
            if (!changed) return false;

            var column = new[] { shop, ad, settings };
            for (int slot = 0; slot < column.Length; slot++)
            {
                var rect = column[slot];
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
                rect.sizeDelta = new Vector2(ArtButtonWidth, ArtButtonHeight);
                rect.anchoredPosition = new Vector2(-Margin, -SideTop - slot * (ArtButtonHeight + SideGap));
            }
            Debug.Log("[Setup] LOJA e AJUSTES com os botões do Bruno (nome dentro do quadrado).");
            return true;
        }

        static bool IsOldFace(Transform child) => child.name == "Label" || child.name == "Icon";

        static bool HasVisibleFace(Transform button)
        {
            foreach (Transform child in button)
                if (IsOldFace(child) && child.gameObject.activeSelf) return true;
            return false;
        }

        static bool ArtButton(RectTransform button, Sprite face)
        {
            if (face == null) return false;
            var image = button.GetComponent<Image>();
            if (image.sprite == face && button.Find("Name") == null && !HasVisibleFace(button)) return false;

            image.sprite = face;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            var target = button.GetComponent<Button>();
            target.transition = Selectable.Transition.ColorTint;
            var colors = ColorBlock.defaultColorBlock;
            colors.pressedColor = PressedTint;
            colors.highlightedColor = colors.selectedColor = Color.white;
            target.colors = colors;

            // Todos os "Label" e "Icon", não só o primeiro: cenas antigas ficaram com um ícone repetido (setup de antes
            // vestia o botão de novo), e ele aparecia com a moldura dentro do botão novo.
            foreach (Transform child in button)
                if (IsOldFace(child)) child.gameObject.SetActive(false);
            if (button.Find("Name") is Transform oldName) Object.DestroyImmediate(oldName.gameObject); // versão anterior
            return true;
        }

        static float IconSize(Sprite icon) => icon.rect.width <= 16f ? KitIconSize : SideIconSize;

        /// <summary>Tamanho e lugar do ícone de um botão da coluna. Devolve se mudou algo.</summary>
        static bool PlaceSideIcon(RectTransform icon)
        {
            float size = IconSize(icon.GetComponent<Image>().sprite);
            float top = SideIconAreaTop + Mathf.Floor((SideLabelTop - SideIconAreaTop - size) / 2f);
            var position = new Vector2(0f, -top);
            if (icon.sizeDelta == new Vector2(size, size) && icon.anchoredPosition == position) return false;

            icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0.5f, 1f);
            icon.sizeDelta = new Vector2(size, size);
            icon.anchoredPosition = position;
            return true;
        }

        /// <summary>Refaz o lugar dos ícones da coluna a cada setup (corrige cenas geradas com outro tamanho).</summary>
        static bool PlaceSideIcons(Transform safeArea)
        {
            bool changed = false;
            foreach (var path in new[] { "Shop/ShopButton/Icon", "AdButton/Icon", "SettingsButton/Icon" })
                if (safeArea.Find(path) is RectTransform icon) changed |= PlaceSideIcon(icon);
            return changed;
        }

        static Sprite SettingsIcon() => Load(SettingsIconPath) ?? Load(Wood("icons/icon_settings"));

        /// <summary>
        /// M8 (D-110): o botão de passar de caverna, na coluna abaixo de AJUSTES, em madeira tingida de âmbar com o livro do
        /// Letrado. O objeto "PassCave" (sempre ativo) leva a view; o botão dentro dele só aparece quando dá para passar.
        /// </summary>
        static bool EnsurePassCaveButton(Transform safeArea)
        {
            var normal = Load(Wood("buttons/button_square_normal"));
            if (safeArea.Find("PassCave") != null || normal == null) return false;

            var container = new GameObject("PassCave", typeof(RectTransform));
            container.transform.SetParent(safeArea, false);
            var containerRect = (RectTransform)container.transform;
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = containerRect.offsetMax = Vector2.zero;
            // Antes da loja na hierarquia: a loja aberta fica por cima.
            var shop = safeArea.Find("Shop");
            if (shop != null) container.transform.SetSiblingIndex(shop.GetSiblingIndex());

            var go = new GameObject("PassCaveButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(container.transform, false);
            var label = GrayboxSetup.CreateHudText(go.transform, "Label", PassCaveTitle, LabelSize, LabelColor,
                                                   Vector2.zero, Vector2.one);
            SideButton((RectTransform)go.transform, 3, normal, Load(Wood("buttons/button_square_pressed")),
                       Load(UiSkinSetup.IconsFolder + "/Icon299.png"), label, LabelColor);
            go.GetComponent<Image>().color = AmberTint;

            var view = container.AddComponent<CavePassButton>();
            GrayboxSetup.SetReferences(view, ("runner", Object.FindAnyObjectByType<MiningRunner>()),
                                       ("button", go.GetComponent<Button>()), ("visual", go));
            go.SetActive(false);
            Debug.Log("[Setup] Botão DESCER (passar de caverna, M8) na coluna da direita.");
            return true;
        }

        /// <summary>AD e AJUSTES (D-67): aparecem já, abaixo da LOJA, e ao tocar mostram "Em breve".</summary>
        static bool EnsureComingSoon(Transform safeArea, string name, string title, int slot, Sprite icon, Sprite normal,
                                     Sprite pressed, Color labelColor)
        {
            if (safeArea.Find(name) != null || normal == null) return false;

            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(safeArea, false);
            // Antes da loja na hierarquia: a loja aberta fica por cima destes botões.
            var shop = safeArea.Find("Shop");
            if (shop != null) go.transform.SetSiblingIndex(shop.GetSiblingIndex());

            var label = GrayboxSetup.CreateHudText(go.transform, "Label", title, LabelSize, labelColor, Vector2.zero, Vector2.one);
            SideButton((RectTransform)go.transform, slot, normal, pressed, icon, label, labelColor);

            // O aviso, à esquerda do botão, numa plaquinha de madeira.
            var notice = new GameObject("Notice", typeof(RectTransform), typeof(Image));
            notice.transform.SetParent(go.transform, false);
            var noticeRect = (RectTransform)notice.transform;
            noticeRect.anchorMin = noticeRect.anchorMax = new Vector2(0f, 0.5f);
            noticeRect.pivot = new Vector2(1f, 0.5f);
            noticeRect.anchoredPosition = new Vector2(-3f, 0f);
            noticeRect.sizeDelta = new Vector2(52f, 18f);
            var noticeImage = notice.GetComponent<Image>();
            noticeImage.sprite = Load(Wood("panels/tooltip"));
            noticeImage.type = Image.Type.Sliced;
            noticeImage.raycastTarget = false;
            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(CatalogPath);
            GrayboxSetup.CreateHudText(notice.transform, "Text", catalog.LockedLabel, LabelSize, TextColor,
                                       Vector2.zero, Vector2.one);
            notice.SetActive(false);

            var view = go.AddComponent<ComingSoonButton>();
            GrayboxSetup.SetReferences(view, ("button", go.GetComponent<Button>()), ("notice", notice));
            return true;
        }
    }
}
