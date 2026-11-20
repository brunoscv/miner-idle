using System.Collections.Generic;
using MinersIdle.Core;
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
    /// A loja rápida da M9i E8 (D-341, mockup 2 aprovado em 2026-11-19): a proposta do Bruno para o Apellon, ligada só
    /// pelo DBG (<c>DBG → Loja → Loja rápida</c>). Monta na cena Game:
    /// <list type="bullet">
    /// <item>o tile vazio do Apellon (<c>tile_blank</c>, o do AJUSTES sem a engrenagem) em 9-slice;</item>
    /// <item>a fileira de quatro cards (Picareta, Velocidade, Mineração, Crítico), 61×58 com 6 entre eles, logo acima do
    /// Descansar e do DESCER, desligada;</item>
    /// <item>o <see cref="QuickShopLayout"/> com as poses "ligada": Descansar e DESCER com 128 cada, alinhados com os
    /// cards; a barra do andar de volta ao alto; a janela do raro na altura da LOJA; o DBG acima dos cards;</item>
    /// <item>o botão no menu Loja do DBG.</item>
    /// </list>
    /// Desligada, a tela é a da E7: o setup não mexe em nada que já existia. Roda por último. Idempotente (só monta se a
    /// loja rápida ainda não existe).
    /// </summary>
    static class QuickShopSetup
    {
        const string RootName = "QuickShop";
        const string LayoutName = "QuickShopLayout";
        const string DebugButtonName = "QuickShop";
        const string TilePath = "Assets/_Project/Art/UI/tile_blank.png";
        const string ShardPath = "Assets/_Project/Art/Crystals/icon_crystal.png";
        const string CatalogPath = "Assets/_Project/Data/ShopCatalog.asset";
        internal const string CardPrefix = "QuickCard_";

        // O tile tem 96 px de arte para 48 na tela: as bordas do 9-slice (em px de arte) valem a metade na tela.
        static readonly Vector4 TileBorder = new Vector4(8f, 14f, 10f, 8f); // esquerda, baixo, direita, cima
        const float TilePixelsPerUnitMultiplier = 2f;

        // Referência 270 (D-14). Quatro colunas de 61 com 6 entre elas, de 4 a 266 (a borda de fora da coluna da direita).
        internal const float CardWidth = 61f, CardHeight = 58f, CardGap = 6f;
        internal const float RowWidth = 4f * CardWidth + 3f * CardGap; // 262
        internal const float RowBottom = HudSkinSetup.BarTop + 6f;      // 105: 6 px acima do Descansar e do DESCER
        const float IconSize = 32f, IconTop = 3f;
        const float LevelTop = 34f, LevelHeight = 12f, LevelSize = 33f / 4f; // 8,25: o grid da Pixelify
        const float CostTop = 44f, CostHeight = 10f, ShardSize = 8f, CostGap = 2f;
        const float PriceSize = 8f;                                          // a Press Start no tamanho dela
        static readonly Color TextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        static readonly Color PressedTint = new Color(0.72f, 0.72f, 0.72f, 1f);

        // Ligada: Descansar e DESCER com duas colunas cada (128), 6 entre eles, alinhados com os cards.
        internal const float WideButtonWidth = 2f * CardWidth + CardGap; // 128
        const float NarrowButtonWidth = HudSkinSetup.BarButtonWidth;      // 94
        const float WideGap = CardGap;
        const float FloorBarTop = 50f;  // o alto de antes da E2: 4 px abaixo do HUD (46)
        const float FloorBarLeft = 4f;
        const float RareTop = 50f;      // a janela do raro na altura do tile da LOJA
        // O DBG no alto, logo abaixo do HUD (4 px), encostado na coluna da direita (4 px antes do tile, que começa em 218):
        // o painel aberto do DBG (de 122 a 386 do fundo) nunca chega lá, nem no 9:16. Antes ficava acima dos cards, por
        // baixo do painel aberto, e não dava para fechar o DBG (Bruno, 2026-11-19).
        const float DebugTop = FloorBarTop;
        const float DebugToggleLeft = 184f;                          // 184..214 (o botão tem 30)
        const float DebugWarningLeft = 120f;                         // 120..180, o aviso do cálculo à esquerda do botão

        public static bool EnsureInScene()
        {
            bool changed = UiSkinSetup.ConfigureSprite(TilePath, TileBorder) > 0;
            var safeArea = GameObject.Find("SafeArea")?.transform;
            if (safeArea == null) return changed;
            if (safeArea.Find(RootName) == null) changed |= Build(safeArea);
            changed |= KeepBelowTheModals(safeArea);
            // As poses "ligada" saem da cena como ela está (desligada): conferidas em toda rodada.
            if (safeArea.Find(LayoutName)?.GetComponent<QuickShopLayout>() is QuickShopLayout layout)
                changed |= UpdatePoses(layout, safeArea);
            return changed;
        }

        static bool Build(Transform safeArea)
        {
            var runner = Object.FindAnyObjectByType<MiningRunner>();
            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(CatalogPath);
            var tile = AssetDatabase.LoadAssetAtPath<Sprite>(TilePath);
            var shop = safeArea.Find("Shop");
            var rare = safeArea.Find("RareWindow");
            var debugMenu = safeArea.Find("DebugPanel/ShopMenu/Content/Buttons");
            if (runner == null || catalog == null || tile == null || shop == null || rare == null || debugMenu == null)
            {
                Debug.LogWarning("[Setup] Loja rápida (M9i E8) não montada: falta o MiningRunner, o ShopCatalog, o tile vazio, a LOJA, a janela do raro ou o menu Loja do DBG.");
                return false;
            }

            EnsureCatalog(catalog);
            var quickShop = BuildRow(safeArea, shop, runner, catalog, tile);
            BuildLayout(safeArea, quickShop, shop, rare, debugMenu);
            Debug.Log("[Setup] Loja rápida no DBG (M9i E8, D-341): quatro cards acima do Descansar e do DESCER, desligada.");
            return true;
        }

        /// <summary>
        /// A loja rápida (e o objeto das poses) logo antes da janela do raro na <c>SafeArea</c>: assim o menu do AD, a tela
        /// do anúncio, a LOJA e os outros modais, que vêm depois, ficam por cima dos cards. Na primeira montagem ela ficava
        /// antes da LOJA, mas depois do AD e do anúncio, e os cards apareciam por cima deles (Bruno, 2026-11-19). Com o
        /// raro aberto, o Canvas dos cards os põe por cima do escurecido (P10). Devolve se mudou algo.
        /// </summary>
        static bool KeepBelowTheModals(Transform safeArea)
        {
            var quick = safeArea.Find(RootName);
            var layout = safeArea.Find(LayoutName);
            var rare = safeArea.Find("RareWindow");
            if (quick == null || rare == null || quick.GetSiblingIndex() < rare.GetSiblingIndex()) return false;
            quick.SetSiblingIndex(rare.GetSiblingIndex()); // o raro desce uma casa, logo depois dos cards
            if (layout != null) layout.SetSiblingIndex(quick.GetSiblingIndex());
            EditorUtility.SetDirty(safeArea.gameObject);
            Debug.Log("[Setup] Loja rápida antes da janela do raro: o menu do AD, o anúncio e os modais ficam por cima dos cards (M9i E8).");
            return true;
        }

        // ── Os textos no catálogo ────────────────────────────────────────────────────────────────────────────────

        static void EnsureCatalog(ShopCatalog catalog)
        {
            var so = new SerializedObject(catalog);
            var format = so.FindProperty("quickLevelFormat");
            if (string.IsNullOrEmpty(format.stringValue)) format.stringValue = "Nv {0}";
            var upgrades = so.FindProperty("quickUpgrades");
            if (upgrades.arraySize == 0)
            {
                var ids = new[] { UpgradeId.Pickaxe, UpgradeId.Speed, UpgradeId.Mining, UpgradeId.Critical };
                upgrades.arraySize = ids.Length;
                for (int i = 0; i < ids.Length; i++) upgrades.GetArrayElementAtIndex(i).intValue = (int)ids[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog); // grava os dois campos novos no .asset
            AssetDatabase.SaveAssets();
        }

        // ── A fileira de cards ───────────────────────────────────────────────────────────────────────────────────

        static GameObject BuildRow(Transform safeArea, Transform shop, MiningRunner runner, ShopCatalog catalog, Sprite tile)
        {
            // O Canvas próprio: com a janela do raro aberta, os cards ordenam por cima do escurecido dela (P10).
            var root = new GameObject(RootName, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            root.transform.SetParent(safeArea, false);
            // Antes da LOJA (com ela aberta, o painel cobre os cards); o KeepBelowTheModals a leva para antes do raro.
            root.transform.SetSiblingIndex(shop.GetSiblingIndex());
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, RowBottom);
            rect.sizeDelta = new Vector2(RowWidth, CardHeight);

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.GameFontPath);
            var numbers = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiSkinSetup.NumbersFontPath);
            var shard = AssetDatabase.LoadAssetAtPath<Sprite>(ShardPath);
            var upgrades = catalog.QuickUpgrades;
            var cards = new List<QuickShopCard>();
            for (int i = 0; i < upgrades.Count; i++)
                cards.Add(BuildCard(root.transform, upgrades[i], i, tile, shard, font, numbers));

            var view = root.AddComponent<QuickShopView>();
            var so = new SerializedObject(view);
            so.FindProperty("runner").objectReferenceValue = runner;
            so.FindProperty("catalog").objectReferenceValue = catalog;
            so.FindProperty("overlay").objectReferenceValue = root.GetComponent<Canvas>();
            var array = so.FindProperty("cards");
            array.arraySize = cards.Count;
            for (int i = 0; i < cards.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);
            EditorUtility.SetDirty(root);
            return root;
        }

        static QuickShopCard BuildCard(Transform parent, UpgradeId upgrade, int column, Sprite tile, Sprite shard,
                                       TMP_FontAsset font, TMP_FontAsset numbers)
        {
            var go = new GameObject(CardPrefix + upgrade, typeof(RectTransform), typeof(Image), typeof(Button),
                                    typeof(QuickShopCard));
            go.transform.SetParent(parent, false);
            RareWindowSetup.Place((RectTransform)go.transform, column * (CardWidth + CardGap), 0f, CardWidth, CardHeight);
            var background = go.GetComponent<Image>();
            background.sprite = tile;
            background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = TilePixelsPerUnitMultiplier;

            var button = go.GetComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.ColorTint;
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = colors.highlightedColor = colors.selectedColor = colors.disabledColor = Color.white;
            colors.pressedColor = PressedTint; // apagado, quem pinta é o card (a mesma tinta da LOJA)
            button.colors = colors;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;

            var icon = RareWindowSetup.Picture(go.transform, "Icon", null);
            RareWindowSetup.Place(icon.rectTransform, (CardWidth - IconSize) / 2f, IconTop, IconSize, IconSize);
            icon.enabled = true;

            var level = RareWindowSetup.Text(go.transform, "Level", LevelSize, TextColor, TextAlignmentOptions.Center, font);
            RareWindowSetup.Place(level.rectTransform, 0f, LevelTop, CardWidth, LevelHeight);

            // O preço: a lasca e o número, centrados juntos (o número muda de largura).
            var cost = new GameObject("Cost", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            cost.transform.SetParent(go.transform, false);
            RareWindowSetup.Place((RectTransform)cost.transform, 0f, CostTop, CardWidth, CostHeight);
            var row = cost.GetComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.MiddleCenter;
            row.spacing = CostGap;
            row.childControlWidth = true;
            row.childControlHeight = false;
            row.childForceExpandWidth = row.childForceExpandHeight = false;

            var shardImage = RareWindowSetup.Picture(cost.transform, "Shard", shard);
            shardImage.rectTransform.sizeDelta = new Vector2(ShardSize, ShardSize);
            shardImage.preserveAspect = true;
            var shardLayout = shardImage.gameObject.AddComponent<LayoutElement>();
            shardLayout.preferredWidth = shardLayout.minWidth = ShardSize;

            var price = RareWindowSetup.Text(cost.transform, "Price", PriceSize, TextColor, TextAlignmentOptions.Center, numbers);
            price.rectTransform.sizeDelta = new Vector2(30f, CostHeight);

            var card = go.GetComponent<QuickShopCard>();
            GrayboxSetup.SetReferences(card, ("button", button), ("background", background), ("icon", icon),
                ("shard", shardImage), ("level", level), ("price", price));
            return card;
        }

        // ── As poses "ligada" e o botão do DBG ───────────────────────────────────────────────────────────────────

        static void BuildLayout(Transform safeArea, GameObject quickShop, Transform shop, Transform rare, Transform debugMenu)
        {
            // O botão no menu Loja do DBG, antes do Fechar.
            var debugButton = DebugPanelSetup.CreateButton(debugMenu, DebugButtonName, "Loja rápida: não");
            var close = debugMenu.Find("Close");
            if (close != null) debugButton.transform.SetSiblingIndex(close.GetSiblingIndex());

            var go = new GameObject(LayoutName, typeof(RectTransform), typeof(QuickShopLayout));
            go.transform.SetParent(safeArea, false);
            go.transform.SetSiblingIndex(quickShop.transform.GetSiblingIndex());
            var layout = go.GetComponent<QuickShopLayout>();
            var so = new SerializedObject(layout);
            so.FindProperty("quickShop").objectReferenceValue = quickShop;
            so.FindProperty("rareWindow").objectReferenceValue = rare;
            so.FindProperty("rareAbove").objectReferenceValue = shop;
            so.FindProperty("debugButton").objectReferenceValue = debugButton;
            so.FindProperty("debugLabel").objectReferenceValue = debugButton.GetComponentInChildren<TMP_Text>();
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(layout);
        }

        /// <summary>Como cada peça fica com a loja rápida ligada, a partir da cena desligada (a da E7).</summary>
        static List<QuickShopLayout.Pose> OnPoses(Transform safeArea)
        {
            var poses = new List<QuickShopLayout.Pose>();

            // Descansar e DESCER: 128 cada, 6 entre eles; o que está dentro acompanha (centralizado).
            if (safeArea.Find("Rest/RestButton") is RectTransform rest) Widen(rest, right: false, poses);
            if (safeArea.Find("NextFloor/Bar") is RectTransform next) Widen(next, right: true, poses);
            if (safeArea.Find("Rest/Notice") is RectTransform notice)
                poses.Add(Moved(notice, new Vector2(-WideGap / 2f - WideButtonWidth, HudSkinSetup.BarTop + 4f)));

            // A barra do andar volta ao alto (como antes da E2).
            if (safeArea.Find("FloorBar") is RectTransform bar)
            {
                var pose = QuickShopLayout.Pose.Of(bar);
                pose.anchorMin = pose.anchorMax = pose.pivot = new Vector2(0f, 1f);
                pose.anchoredPosition = new Vector2(FloorBarLeft, -FloorBarTop);
                poses.Add(pose);
            }

            // A janela do raro: o alto dela na altura do tile da LOJA.
            if (safeArea.Find("RareWindow/Screen/Panel") is RectTransform panel)
            {
                var pose = QuickShopLayout.Pose.Of(panel);
                pose.anchorMin = pose.anchorMax = pose.pivot = new Vector2(0.5f, 1f);
                pose.anchoredPosition = new Vector2(0f, -RareTop);
                poses.Add(pose);
            }

            // O menu do AD também sobe até a altura da LOJA (Bruno, 2026-11-19): no meio da tela, o VOLTAR ficava colado
            // nos cards. No 9:20 do G75 sobram ~80 px entre o VOLTAR e os cards.
            var boost = FindBoost();
            if (boost != null && boost.transform.Find("Screen/Panel") is RectTransform boostPanel)
            {
                var pose = QuickShopLayout.Pose.Of(boostPanel);
                pose.anchorMin = pose.anchorMax = pose.pivot = new Vector2(0.5f, 1f);
                pose.anchoredPosition = new Vector2(0f, -RareTop);
                poses.Add(pose);
            }

            // O DBG sobe para o alto, logo abaixo do HUD, ao lado da coluna da direita (fora do painel aberto do DBG).
            if (safeArea.Find("DebugPanel/Toggle") is RectTransform toggle) poses.Add(TopLeft(toggle, DebugToggleLeft, DebugTop));
            if (safeArea.Find("DebugPanel/CalcWarning") is RectTransform warning) poses.Add(TopLeft(warning, DebugWarningLeft, DebugTop));
            return poses;
        }

        static BoostScreen FindBoost() => Object.FindAnyObjectByType<BoostScreen>(FindObjectsInactive.Include);

        static bool UpdatePoses(QuickShopLayout layout, Transform safeArea)
        {
            var poses = OnPoses(safeArea);
            var so = new SerializedObject(layout);
            // O menu do AD vai para depois da LOJA com a loja rápida ligada (o tile da LOJA cobria o X, Bruno, 2026-11-19).
            var boostProperty = so.FindProperty("boostScreen");
            var boost = FindBoost();
            bool boostChanged = boost != null && boostProperty.objectReferenceValue != boost.transform;
            if (boostChanged)
            {
                boostProperty.objectReferenceValue = boost.transform;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(layout);
                Debug.Log("[Setup] Loja rápida: o menu do AD passa para depois da LOJA quando ligada (o X fica por cima do tile).");
            }
            var array = so.FindProperty("onPoses");
            bool same = array.arraySize == poses.Count;
            for (int i = 0; same && i < poses.Count; i++)
            {
                var item = array.GetArrayElementAtIndex(i);
                var pose = poses[i];
                same = item.FindPropertyRelative("target").objectReferenceValue == pose.target
                       && item.FindPropertyRelative("anchorMin").vector2Value == pose.anchorMin
                       && item.FindPropertyRelative("anchorMax").vector2Value == pose.anchorMax
                       && item.FindPropertyRelative("pivot").vector2Value == pose.pivot
                       && item.FindPropertyRelative("anchoredPosition").vector2Value == pose.anchoredPosition
                       && item.FindPropertyRelative("sizeDelta").vector2Value == pose.sizeDelta;
            }
            if (same) return boostChanged;

            array.arraySize = poses.Count;
            for (int i = 0; i < poses.Count; i++)
            {
                var item = array.GetArrayElementAtIndex(i);
                var pose = poses[i];
                item.FindPropertyRelative("target").objectReferenceValue = pose.target;
                item.FindPropertyRelative("anchorMin").vector2Value = pose.anchorMin;
                item.FindPropertyRelative("anchorMax").vector2Value = pose.anchorMax;
                item.FindPropertyRelative("pivot").vector2Value = pose.pivot;
                item.FindPropertyRelative("anchoredPosition").vector2Value = pose.anchoredPosition;
                item.FindPropertyRelative("sizeDelta").vector2Value = pose.sizeDelta;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(layout);
            Debug.Log($"[Setup] Loja rápida: {poses.Count} poses \"ligada\" (Descansar e DESCER largos, a barra no alto, o raro na altura da LOJA, o DBG acima dos cards).");
            return true;
        }

        /// <summary>Preso pelo canto de cima à esquerda da tela, a <paramref name="left"/> e <paramref name="top"/> px.</summary>
        static QuickShopLayout.Pose TopLeft(RectTransform rect, float left, float top)
        {
            var pose = QuickShopLayout.Pose.Of(rect);
            pose.anchorMin = pose.anchorMax = pose.pivot = new Vector2(0f, 1f);
            pose.anchoredPosition = new Vector2(left, -top);
            return pose;
        }

        static QuickShopLayout.Pose Moved(RectTransform rect, Vector2 position)
        {
            var pose = QuickShopLayout.Pose.Of(rect);
            pose.anchoredPosition = position;
            return pose;
        }

        /// <summary>
        /// Um botão de baixo com 128 em vez de 94, encostado no meio da tela (6 entre os dois). O que está dentro dele com
        /// largura fixa de 94 (a madeira acesa do DESCER) cresce junto; o que tem tamanho próprio vai para o meio.
        /// </summary>
        static void Widen(RectTransform button, bool right, List<QuickShopLayout.Pose> poses)
        {
            var pose = QuickShopLayout.Pose.Of(button);
            pose.anchoredPosition = new Vector2(right ? WideGap / 2f : -WideGap / 2f, pose.anchoredPosition.y);
            pose.sizeDelta = new Vector2(WideButtonWidth, pose.sizeDelta.y);
            poses.Add(pose);
            WidenChildren(button, WideButtonWidth - NarrowButtonWidth, poses);
        }

        static void WidenChildren(RectTransform parent, float grow, List<QuickShopLayout.Pose> poses)
        {
            bool laidOut = parent.GetComponent<LayoutGroup>() != null;
            foreach (Transform child in parent)
            {
                if (!(child is RectTransform rect)) continue;
                bool ignored = child.GetComponent<LayoutElement>() is LayoutElement element && element.ignoreLayout;
                if (laidOut && !ignored) continue; // o layout do botão já centraliza
                // Esticado (ou esticado pelo código, como o Fill do DESCER, salvo com largura zero): cresce com o pai.
                bool stretched = !Mathf.Approximately(rect.anchorMin.x, rect.anchorMax.x)
                                 || (Mathf.Approximately(rect.anchorMin.x, 0f) && Mathf.Approximately(rect.sizeDelta.x, 0f));
                if (stretched)
                {
                    WidenChildren(rect, grow, poses);
                    continue;
                }
                var pose = QuickShopLayout.Pose.Of(rect);
                if (Mathf.Approximately(rect.sizeDelta.x, NarrowButtonWidth))
                {
                    pose.sizeDelta = new Vector2(rect.sizeDelta.x + grow, rect.sizeDelta.y);
                    pose.anchoredPosition += new Vector2(grow * (rect.pivot.x - rect.anchorMin.x), 0f);
                    poses.Add(pose);
                    WidenChildren(rect, grow, poses);
                }
                else if (!Mathf.Approximately(rect.anchorMin.x, 0.5f))
                {
                    pose.anchoredPosition += new Vector2(grow * (0.5f - rect.anchorMin.x), 0f);
                    poses.Add(pose);
                }
            }
        }
    }
}
