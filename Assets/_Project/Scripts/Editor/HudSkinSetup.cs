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
            return PlaceSideIcons(safeArea.transform) | changed;
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
            if (button == null || Mathf.Approximately(((RectTransform)button).sizeDelta.x, SideSize)) return false;

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
