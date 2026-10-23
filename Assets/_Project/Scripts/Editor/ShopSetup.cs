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
    /// Parte do <see cref="GrayboxSetup"/> para a loja (Marco 4, E2): o catálogo com os textos em pt-BR, o prefab de uma
    /// linha e a loja na cena Game (botão LOJA com o selo, fundo que escurece, painel com as linhas e VOLTAR). Os textos
    /// gravados aqui são dados da cena e do asset, não do código do jogo. Só cria o que falta.
    /// </summary>
    static class ShopSetup
    {
        const string CatalogPath = "Assets/_Project/Data/ShopCatalog.asset";
        const string RowPrefabPath = "Assets/_Project/Prefabs/UI/UpgradeRow.prefab";
        const string ShopName = "Shop";

        // Layout na referência 270×480 (D-14, provisório).
        const float RowHeight = 44f;
        const float HudBottom = 42f;   // o painel começa abaixo do HUD
        const float HeaderHeight = 26f;
        const float FooterHeight = 36f;

        static readonly Color DimColor = new Color(0f, 0f, 0f, 0.6f);
        static readonly Color PanelColor = new Color32(0x1B, 0x13, 0x0D, 0xF5);
        static readonly Color RowColor = new Color32(0x2B, 0x1D, 0x14, 0xF2);
        static readonly Color ButtonColor = new Color32(0x3A, 0x2A, 0x1E, 0xFF);
        static readonly Color BuyColor = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        static readonly Color AlertColor = new Color32(0xD8, 0x42, 0x35, 0xFF);
        static readonly Color LabelColor = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        static readonly Color TextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        static readonly Color DarkText = new Color32(0x15, 0x19, 0x1C, 0xFF);

        /// <summary>Catálogo e prefab (fora da cena).</summary>
        public static void EnsureAssets(Sprite square)
        {
            EnsureCatalog();
            EnsureRowPrefab(square);
        }

        static void EnsureCatalog()
        {
            if (File.Exists(CatalogPath)) return;

            ProjectSetup.EnsureFolder(Path.GetDirectoryName(CatalogPath)?.Replace('\\', '/'));
            var catalog = ScriptableObject.CreateInstance<ShopCatalog>();
            var so = new SerializedObject(catalog);
            so.FindProperty("shopTitle").stringValue = "LOJA";
            so.FindProperty("balanceFormat").stringValue = "LASCAS {0}";
            so.FindProperty("levelFormat").stringValue = "nv {0}/{1}";
            so.FindProperty("maxedLabel").stringValue = "máx.";
            so.FindProperty("boughtLabel").stringValue = "comprada";
            so.FindProperty("lockedLabel").stringValue = "Em breve";
            so.FindProperty("bonusPercentFormat").stringValue = "+{0}%";

            var entries = so.FindProperty("entries");
            entries.arraySize = 0;
            AddEntry(entries, false, UpgradeId.Pickaxe, "Picareta", "Dano {0} » {1}", ShopCatalog.EffectStyle.Number, new Color32(0xB0, 0xB7, 0xBD, 0xFF));
            AddEntry(entries, false, UpgradeId.Speed, "Velocidade", "Golpe a cada {0} » {1}", ShopCatalog.EffectStyle.Seconds, new Color32(0x4F, 0xC3, 0xF7, 0xFF));
            AddEntry(entries, false, UpgradeId.Mining, "Mineração", "Lascas {0} » {1}", ShopCatalog.EffectStyle.BonusPercent, new Color32(0xC2, 0x3C, 0xFF, 0xFF));
            AddEntry(entries, false, UpgradeId.DoublePickaxe, "Picareta Dupla", "Dano {0} » {1}", ShopCatalog.EffectStyle.Number, new Color32(0xF2, 0xA9, 0x00, 0xFF));
            AddEntry(entries, false, UpgradeId.Boots, "Botas", "Caminhada {0} » {1}", ShopCatalog.EffectStyle.Number, new Color32(0x8D, 0x6E, 0x63, 0xFF));
            AddEntry(entries, true, UpgradeId.Pickaxe, "Estagiário", "Um ajudante que minera junto", ShopCatalog.EffectStyle.Number, new Color32(0x9E, 0x9E, 0x9E, 0xFF));
            AddEntry(entries, true, UpgradeId.Pickaxe, "Letrado", "Libera a próxima caverna", ShopCatalog.EffectStyle.Number, new Color32(0x9E, 0x9E, 0x9E, 0xFF));
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(catalog, CatalogPath);
            Debug.Log($"[Setup] Catálogo da loja criado em {CatalogPath}.");
        }

        static void AddEntry(SerializedProperty entries, bool locked, UpgradeId upgrade, string title, string effect,
                             ShopCatalog.EffectStyle style, Color icon)
        {
            int i = entries.arraySize;
            entries.InsertArrayElementAtIndex(i);
            var entry = entries.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("locked").boolValue = locked;
            entry.FindPropertyRelative("upgrade").enumValueIndex = (int)upgrade;
            entry.FindPropertyRelative("title").stringValue = title;
            entry.FindPropertyRelative("effectFormat").stringValue = effect;
            entry.FindPropertyRelative("style").enumValueIndex = (int)style;
            entry.FindPropertyRelative("iconColor").colorValue = icon;
        }

        static void EnsureRowPrefab(Sprite square)
        {
            if (File.Exists(RowPrefabPath)) return;

            ProjectSetup.EnsureFolder(Path.GetDirectoryName(RowPrefabPath)?.Replace('\\', '/'));
            var root = new GameObject("UpgradeRow", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(LayoutElement));
            root.GetComponent<Image>().color = RowColor;
            root.GetComponent<LayoutElement>().preferredHeight = RowHeight;

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(root.transform, false);
            var iconRect = (RectTransform)icon.transform;
            iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(6f, 0f);
            iconRect.sizeDelta = new Vector2(26f, 26f);
            var iconImage = icon.GetComponent<Image>();
            iconImage.sprite = square;
            iconImage.raycastTarget = false;

            // Textos entre o ícone (até 38 px) e o botão (os últimos 76 px).
            var title = Text(root.transform, "Title", 9f, LabelColor, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(1f, 1f), 38f, 130f);
            var level = Text(root.transform, "Level", 7f, TextColor, TextAlignmentOptions.Right, new Vector2(0f, 0.5f), new Vector2(1f, 1f), 38f, 78f);
            var effect = Text(root.transform, "Effect", 7f, TextColor, TextAlignmentOptions.Left, new Vector2(0f, 0f), new Vector2(1f, 0.5f), 38f, 78f);

            var buy = new GameObject("Buy", typeof(RectTransform), typeof(Image), typeof(Button));
            buy.transform.SetParent(root.transform, false);
            var buyRect = (RectTransform)buy.transform;
            buyRect.anchorMin = buyRect.anchorMax = new Vector2(1f, 0.5f);
            buyRect.pivot = new Vector2(1f, 0.5f);
            buyRect.anchoredPosition = new Vector2(-6f, 0f);
            buyRect.sizeDelta = new Vector2(66f, 30f);
            buy.GetComponent<Image>().color = BuyColor;
            var buyLabel = GrayboxSetup.CreateHudText(buy.transform, "Label", "", 9f, DarkText, Vector2.zero, Vector2.one);

            var view = root.AddComponent<UpgradeRowView>();
            GrayboxSetup.SetReferences(view,
                ("icon", iconImage), ("title", title), ("level", level), ("effect", effect),
                ("buyButton", buy.GetComponent<Button>()), ("buyLabel", buyLabel), ("group", root.GetComponent<CanvasGroup>()));

            PrefabUtility.SaveAsPrefabAsset(root, RowPrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[Setup] Prefab da linha da loja criado em {RowPrefabPath}.");
        }

        /// <summary>Texto ancorado horizontalmente entre <paramref name="left"/> px e <paramref name="right"/> px das bordas.</summary>
        static TMP_Text Text(Transform parent, string name, float size, Color color, TextAlignmentOptions alignment,
                             Vector2 anchorMin, Vector2 anchorMax, float left, float right)
        {
            var text = GrayboxSetup.CreateHudText(parent, name, "", size, color, anchorMin, anchorMax);
            text.alignment = alignment;
            var rect = text.rectTransform;
            rect.offsetMin = new Vector2(left, 0f);
            rect.offsetMax = new Vector2(-right, 0f);
            return text;
        }

        /// <summary>A loja na cena Game já aberta (dentro do Safe Area, acima de tudo).</summary>
        public static bool EnsureInScene()
        {
            var safeArea = GameObject.Find("SafeArea");
            if (safeArea == null || safeArea.transform.Find(ShopName) != null) return false;

            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(CatalogPath);
            var rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath).GetComponent<UpgradeRowView>();
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(GrayboxSetup.CirclePath);

            var root = new GameObject(ShopName, typeof(RectTransform));
            root.transform.SetParent(safeArea.transform, false);
            Stretch((RectTransform)root.transform, 0f, 0f, 0f, 0f);

            // Botão LOJA à direita, abaixo do HUD (como no mockup), com o selo "!".
            var open = new GameObject("ShopButton", typeof(RectTransform), typeof(Image), typeof(Button));
            open.transform.SetParent(root.transform, false);
            var openRect = (RectTransform)open.transform;
            openRect.anchorMin = openRect.anchorMax = openRect.pivot = new Vector2(1f, 1f);
            openRect.anchoredPosition = new Vector2(-4f, -HudBottom);
            openRect.sizeDelta = new Vector2(52f, 30f);
            open.GetComponent<Image>().color = ButtonColor;
            GrayboxSetup.CreateHudText(open.transform, "Label", catalog.ShopTitle, 10f, LabelColor, Vector2.zero, Vector2.one);

            var badge = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(open.transform, false);
            var badgeRect = (RectTransform)badge.transform;
            badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(1f, 1f);
            badgeRect.anchoredPosition = new Vector2(-2f, -2f);
            badgeRect.sizeDelta = new Vector2(12f, 12f);
            var badgeImage = badge.GetComponent<Image>();
            badgeImage.sprite = circle;
            badgeImage.color = AlertColor;
            badgeImage.raycastTarget = false;
            GrayboxSetup.CreateHudText(badge.transform, "Mark", "!", 8f, TextColor, Vector2.zero, Vector2.one);
            badge.SetActive(false);

            // Tela da loja: fundo que escurece e bloqueia toques no mapa + painel.
            var screen = new GameObject("Screen", typeof(RectTransform), typeof(Image));
            screen.transform.SetParent(root.transform, false);
            Stretch((RectTransform)screen.transform, 0f, 0f, 0f, 0f);
            screen.GetComponent<Image>().color = DimColor; // raycastTarget ligado: bloqueia o mapa, não pausa nada

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(screen.transform, false);
            Stretch((RectTransform)panel.transform, 8f, 8f, HudBottom, 8f);
            panel.GetComponent<Image>().color = PanelColor;

            var title = GrayboxSetup.CreateHudText(panel.transform, "Title", "", 12f, LabelColor, new Vector2(0f, 1f), new Vector2(0.5f, 1f));
            title.alignment = TextAlignmentOptions.Left;
            TopBand(title.rectTransform, 8f, 0f);
            var balance = GrayboxSetup.CreateHudText(panel.transform, "Balance", "", 9f, TextColor, new Vector2(0.5f, 1f), new Vector2(1f, 1f));
            balance.alignment = TextAlignmentOptions.Right;
            TopBand(balance.rectTransform, 0f, 8f);

            var rows = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup));
            rows.transform.SetParent(panel.transform, false);
            Stretch((RectTransform)rows.transform, 4f, 4f, HeaderHeight, FooterHeight);
            var layout = rows.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 3f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            var back = new GameObject("Back", typeof(RectTransform), typeof(Image), typeof(Button));
            back.transform.SetParent(panel.transform, false);
            var backRect = (RectTransform)back.transform;
            backRect.anchorMin = backRect.anchorMax = backRect.pivot = new Vector2(0.5f, 0f);
            backRect.anchoredPosition = new Vector2(0f, 6f);
            backRect.sizeDelta = new Vector2(110f, 24f);
            back.GetComponent<Image>().color = ButtonColor;
            GrayboxSetup.CreateHudText(back.transform, "Label", "VOLTAR", 9f, LabelColor, Vector2.zero, Vector2.one);

            screen.SetActive(false);

            var view = root.AddComponent<ShopView>();
            GrayboxSetup.SetReferences(view,
                ("runner", Object.FindAnyObjectByType<MiningRunner>()), ("catalog", catalog), ("rowPrefab", rowPrefab),
                ("rowsParent", rows.transform), ("screen", screen), ("title", title), ("balance", balance),
                ("backButton", back.GetComponent<Button>()), ("openButton", open.GetComponent<Button>()), ("badge", badge));

            Debug.Log("[Setup] Loja criada na cena Game.");
            return true;
        }

        static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Faixa do cabeçalho: altura fixa no topo do painel.</summary>
        static void TopBand(RectTransform rect, float left, float right)
        {
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -HeaderHeight);
            rect.offsetMax = new Vector2(-right, 0f);
        }
    }
}
