using MinersIdle.Game;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// A E1 do M9i (HUD e coluna da direita, mockup aprovado em 2026-11-19, D-334), na cena Game já montada:
    /// <list type="bullet">
    /// <item>LASCAS e VERMELHAS 4 px mais altas (2 em cima do nome, 2 embaixo do número); o que começava abaixo do HUD
    /// (a barra do andar, o painel da LOJA) desce os mesmos 4 px;</item>
    /// <item>LOJA, AD e AJUSTES viram os tiles do Apellon (<c>tile_shop</c>, <c>tile_boost</c>, <c>tile_settings</c>,
    /// 48 px de arte = 48 px na tela de 270), com o nome em marrom escuro na parte de baixo do tile;</item>
    /// <item>a coluna dos bônus vai para a direita, embaixo do AJUSTES, com os segundos embaixo de cada ícone e o
    /// detalhe abrindo para a esquerda;</item>
    /// <item>o Descansar ganha o jeito trancado: cadeado, "ANDAR 10" e o aviso ao tocar (<see cref="RestButton"/>).</item>
    /// </list>
    /// Roda por último no setup: os setups antigos não refazem nada disso (cada um checa se já fez). Idempotente.
    /// </summary>
    static class HudColumnV8Setup
    {
        const string ArtFolder = "Assets/_Project/Art/UI";
        const string ShopTilePath = ArtFolder + "/tile_shop.png";
        const string BoostTilePath = ArtFolder + "/tile_boost.png";
        const string SettingsTilePath = ArtFolder + "/tile_settings.png";
        const string LockPath = ArtFolder + "/icon_lock.png";
        const string TextsPath = "Assets/_Project/Data/FeedbackTexts.asset";

        // Referência 270 (D-14). O HUD ia de 4 a 42; agora vai até 46.
        const float OldHudHeight = 38f;
        const float HudHeight = 42f;
        const float HudGrow = HudHeight - OldHudHeight;
        const float HudLabelTop = 7f;   // era 5
        const float HudRowTop = 19f;    // era 17
        const float OldHudBottom = 42f;
        const float HudBottom = OldHudBottom + HudGrow;
        const float OldFloorBarTop = 48f;

        // A coluna: três tiles de 48, 4 px entre eles, 4 px da borda direita e do HUD.
        const float Margin = 4f;
        const float TileSize = 48f;
        const float TileGap = 4f;
        const float ColumnTop = HudBottom + 4f;
        const float TileLabelTop = 30f; // a faixa do nome, na parte lisa de baixo do tile
        const float TileLabelHeight = 12f;
        static readonly Color TileLabelColor = new Color32(0x3A, 0x20, 0x10, 0xFF);
        static readonly Color PressedTint = new Color(0.72f, 0.72f, 0.72f, 1f);

        // Os bônus: centrados embaixo da coluna, os segundos embaixo de cada ícone.
        const float SlotSize = 24f;
        const float SlotSpacing = 11f;
        const int MaxShown = 7;
        const float BuffTop = ColumnTop + 3f * TileSize + 2f * TileGap + 8f;
        const float BuffRight = Margin + (TileSize - SlotSize) / 2f;
        const float SecondsHeight = 8f;

        // O aviso do Descansar trancado, logo acima do botão.
        const float NoticeWidth = 150f;
        const float NoticeHeight = 18f;
        const float NoticeLabelSize = 33f / 4f;
        static readonly Color NoticeTextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);

        public static bool EnsureInScene()
        {
            bool changed = EnsureSprites();
            changed |= EnsureTexts();
            var safeArea = GameObject.Find("SafeArea")?.transform;
            if (safeArea == null) return changed;

            changed |= TallerHud(safeArea);
            changed |= EnsureColumn(safeArea);
            changed |= MoveBuffs(safeArea);
            changed |= EnsureRestLock(safeArea);
            return changed;
        }

        /// <summary>O botão já tem o tile do Apellon (os setups antigos não trocam mais a cara dele).</summary>
        internal static bool HasTile(Transform button) =>
            button != null && button.GetComponent<Image>()?.sprite is Sprite sprite && sprite.name.StartsWith("tile_");

        // ── Os sprites e os textos ───────────────────────────────────────────────────────────────────────────────

        static bool EnsureSprites()
        {
            bool changed = false;
            foreach (var path in new[] { ShopTilePath, BoostTilePath, SettingsTilePath, LockPath })
            {
                if (AssetImporter.GetAtPath(path) == null) AssetDatabase.ImportAsset(path);
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
                if (importer.textureType == TextureImporterType.Sprite && importer.filterMode == FilterMode.Point
                    && importer.textureCompression == TextureImporterCompression.Uncompressed && !importer.mipmapEnabled)
                    continue;
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
                changed = true;
            }
            return changed;
        }

        static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        static bool EnsureTexts()
        {
            var texts = AssetDatabase.LoadAssetAtPath<FeedbackTexts>(TextsPath);
            if (texts == null || !string.IsNullOrEmpty(texts.RestLockedFormat)) return false;
            var so = new SerializedObject(texts);
            so.FindProperty("restLockedFormat").stringValue = "ANDAR {0}";
            so.FindProperty("restLockedNotice").stringValue = "Descanse a partir do andar {0}";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(texts);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Textos do Descansar trancado (M9i E1).");
            return true;
        }

        // ── O HUD mais alto ──────────────────────────────────────────────────────────────────────────────────────

        static bool TallerHud(Transform safeArea)
        {
            var hud = safeArea.Find("Hud") as RectTransform;
            if (hud == null || !Mathf.Approximately(hud.sizeDelta.y, OldHudHeight)) return false;

            hud.sizeDelta = new Vector2(hud.sizeDelta.x, HudHeight);
            foreach (Transform panel in hud)
            {
                if (panel.Find("Label") is RectTransform label) HudSkinSetup.Band(label, HudLabelTop, 10f);
                if (panel.Find("Row") is RectTransform row) HudSkinSetup.Band(row, HudRowTop, 16f);
            }

            // O que começava logo abaixo do HUD desce o mesmo tanto.
            if (safeArea.Find("FloorBar") is RectTransform bar && Mathf.Approximately(bar.offsetMax.y, -OldFloorBarTop))
                bar.offsetMax = new Vector2(bar.offsetMax.x, -OldFloorBarTop - HudGrow);
            if (safeArea.Find("Shop/Screen/Panel") is RectTransform shop && Mathf.Approximately(shop.offsetMax.y, -OldHudBottom))
                shop.offsetMax = new Vector2(shop.offsetMax.x, -HudBottom);
            Debug.Log("[Setup] HUD 4 px mais alto (LASCAS e VERMELHAS, M9i E1); a barra do andar e a LOJA descem junto.");
            return true;
        }

        // ── A coluna com os tiles do Apellon ─────────────────────────────────────────────────────────────────────

        static bool EnsureColumn(Transform safeArea)
        {
            var column = new[]
            {
                (safeArea.Find("Shop/ShopButton") as RectTransform, Load(ShopTilePath)),
                (safeArea.Find("AdButton") as RectTransform, Load(BoostTilePath)),
                (safeArea.Find("SettingsButton") as RectTransform, Load(SettingsTilePath)),
            };
            bool changed = false;
            for (int slot = 0; slot < column.Length; slot++)
            {
                var (button, tile) = column[slot];
                if (button != null && tile != null) changed |= Tile(button, tile, slot);
            }
            if (changed) Debug.Log("[Setup] LOJA, AD e AJUSTES com os tiles do Apellon (M9i E1, D-334).");
            return changed;
        }

        static bool Tile(RectTransform button, Sprite tile, int slot)
        {
            var image = button.GetComponent<Image>();
            var label = button.Find("Label")?.GetComponent<TMP_Text>();
            var position = new Vector2(-Margin, -ColumnTop - slot * (TileSize + TileGap));
            var size = new Vector2(TileSize, TileSize);
            if (image.sprite == tile && button.anchoredPosition == position && button.sizeDelta == size
                && (label == null || (label.gameObject.activeSelf && label.color == TileLabelColor)))
                return false;

            button.anchorMin = button.anchorMax = button.pivot = Vector2.one;
            button.anchoredPosition = position;
            button.sizeDelta = size;
            image.sprite = tile;
            image.type = Image.Type.Simple;
            image.color = Color.white;

            var target = button.GetComponent<Button>();
            target.targetGraphic = image;
            target.transition = Selectable.Transition.ColorTint;
            var colors = ColorBlock.defaultColorBlock;
            colors.pressedColor = PressedTint;
            colors.highlightedColor = colors.selectedColor = Color.white;
            target.colors = colors;

            // O desenho já está no tile: os ícones de antes saem (todos, cenas antigas tinham um repetido).
            foreach (Transform child in button)
                if (child.name == "Icon") child.gameObject.SetActive(false);

            if (label != null)
            {
                label.gameObject.SetActive(true);
                HudSkinSetup.Band(label.rectTransform, TileLabelTop, TileLabelHeight);
                label.alignment = TextAlignmentOptions.Center;
                label.color = TileLabelColor;
                EditorUtility.SetDirty(label);
            }

            // O AD troca de sprite e de cor sozinho (livre/apagado): passa a usar o tile e o marrom.
            if (button.GetComponent<AdButtonView>() is AdButtonView ad)
            {
                var so = new SerializedObject(ad);
                so.FindProperty("normalSprite").objectReferenceValue = tile;
                so.FindProperty("disabledSprite").objectReferenceValue = tile;
                so.FindProperty("labelColor").colorValue = TileLabelColor;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(button.gameObject);
            return true;
        }

        // ── Os bônus à direita ───────────────────────────────────────────────────────────────────────────────────

        static bool MoveBuffs(Transform safeArea)
        {
            var root = safeArea.Find("BuffColumn") as RectTransform;
            var column = root != null ? root.GetComponent<BuffColumn>() : null;
            var target = new Vector2(-BuffRight, -BuffTop);
            if (column == null || (root.anchorMin == Vector2.one && root.anchoredPosition == target)) return false;

            root.anchorMin = root.anchorMax = root.pivot = Vector2.one;
            root.anchoredPosition = target;
            root.sizeDelta = new Vector2(SlotSize, MaxShown * (SlotSize + SlotSpacing));

            var so = new SerializedObject(column);
            if (so.FindProperty("slots").objectReferenceValue is RectTransform slots
                && slots.GetComponent<VerticalLayoutGroup>() is VerticalLayoutGroup layout)
            {
                layout.spacing = SlotSpacing;
                layout.childAlignment = TextAnchor.UpperCenter;
            }

            // O detalhe abre à esquerda do ícone (a coluna encosta na borda direita).
            if (so.FindProperty("detail").objectReferenceValue is RectTransform detail)
            {
                detail.anchorMin = detail.anchorMax = new Vector2(0f, 1f);
                detail.pivot = new Vector2(1f, 0.5f);
                detail.anchoredPosition = new Vector2(-6f, -SlotSize / 2f);
            }

            if (so.FindProperty("slotTemplate").objectReferenceValue is BuffSlot template)
            {
                var slot = new SerializedObject(template);
                // Os segundos embaixo do ícone, centralizados.
                if (slot.FindProperty("seconds").objectReferenceValue is TMP_Text seconds)
                {
                    var rect = seconds.rectTransform;
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(1f, 0f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.offsetMin = new Vector2(-8f, -SecondsHeight - 1f);
                    rect.offsetMax = new Vector2(8f, -1f);
                    seconds.alignment = TextAlignmentOptions.Center;
                    EditorUtility.SetDirty(seconds);
                }
                // O selo do anúncio ("×4") sobe para o canto de cima, para não cobrir os segundos.
                if (slot.FindProperty("badgeRoot").objectReferenceValue is GameObject badge)
                {
                    var rect = (RectTransform)badge.transform;
                    rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
                    rect.anchoredPosition = new Vector2(2f, 1f);
                }
                EditorUtility.SetDirty(template);
            }
            EditorUtility.SetDirty(root);
            Debug.Log("[Setup] Os bônus vão para a direita, embaixo do AJUSTES (M9i E1, #7).");
            return true;
        }

        // ── O Descansar trancado ─────────────────────────────────────────────────────────────────────────────────

        static bool EnsureRestLock(Transform safeArea)
        {
            var container = safeArea.Find("Rest");
            var view = container != null ? container.GetComponent<RestButton>() : null;
            var button = container != null ? container.Find("RestButton") : null;
            if (view == null || button == null) return false;
            var so = new SerializedObject(view);
            if (so.FindProperty("notice").objectReferenceValue != null) return false;

            // O aviso fica no objeto "Rest" (a tela toda), logo acima do botão: o botão tem layout próprio.
            var notice = new GameObject("Notice", typeof(RectTransform), typeof(Image));
            notice.transform.SetParent(container, false);
            var rect = (RectTransform)notice.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(HudSkinSetup.BarMargin,
                                                HudSkinSetup.BarBottom + HudSkinSetup.BarButtonHeight + 4f);
            rect.sizeDelta = new Vector2(NoticeWidth, NoticeHeight);
            var image = notice.GetComponent<Image>();
            image.sprite = Load(UiSkinSetup.WoodFolder + "/panels/tooltip.png");
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            if (image.sprite == null) image.color = new Color32(0x18, 0x10, 0x0A, 0xE6);
            var text = GrayboxSetup.CreateHudText(notice.transform, "Text", "", NoticeLabelSize, NoticeTextColor,
                                                  Vector2.zero, Vector2.one);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            UiSkinSetup.ApplyGameFont(notice.transform);
            notice.SetActive(false);

            GrayboxSetup.SetReferences(view, ("background", button.GetComponent<Image>()),
                ("icon", button.Find("Icon")?.GetComponent<Image>()), ("lockSprite", Load(LockPath)),
                ("notice", notice), ("noticeText", text));
            EditorUtility.SetDirty(view);
            Debug.Log("[Setup] Descansar sempre na barra: trancado (cadeado e \"ANDAR 10\") antes do andar do descanso (M9i E1, #15).");
            return true;
        }
    }
}
