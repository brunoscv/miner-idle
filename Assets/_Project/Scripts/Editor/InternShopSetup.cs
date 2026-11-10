using System.IO;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="GrayboxSetup"/> para a E3 do M7 (D-170, D-174), com a cena Game aberta e depois do
    /// <see cref="PermanentShopSetup"/> (a barra de abas já existe): no catálogo, a linha Estagiário de verdade (no lugar do
    /// "Em breve", com "contratado"), a aba ESTAGIÁRIO com as 3 melhorias dele e, nas permanentes, Treinar e Promoção; na
    /// loja, a 3ª aba e a lista dele; as linhas das permanentes passam a 40 px (7 linhas cabem) e a linha da loja ganha a
    /// moldura cinza da compra única feita. Medidas do mockup aprovado pelo Bruno em 2026-11-05. Só cria o que falta; não
    /// sobrescreve textos, ícones ou molduras mudados à mão.
    /// </summary>
    static class InternShopSetup
    {
        const string CatalogPath = "Assets/_Project/Data/ShopCatalog.asset";
        const string RowPrefabPath = "Assets/_Project/Prefabs/UI/UpgradeRow.prefab";
        const string PermanentRowPrefabPath = "Assets/_Project/Prefabs/UI/PermanentRow.prefab";

        /// <summary>A altura da linha das permanentes desde o M7 (D-174); antes, 46 como a da loja.</summary>
        const float PermanentRowHeight = 40f;
        const float OldPermanentRowHeight = 46f;

        public static bool EnsureInScene()
        {
            var safeArea = GameObject.Find("SafeArea")?.transform;
            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(CatalogPath);
            if (safeArea == null || catalog == null) return false;

            EnsureCatalog(catalog);
            EnsurePermanentRowHeight();
            EnsureDoneFrame();
            return EnsureTab(safeArea, catalog);
        }

        // ── Catálogo ─────────────────────────────────────────────────────────────────────────────────────────────

        static void EnsureCatalog(ShopCatalog catalog)
        {
            var so = new SerializedObject(catalog);
            bool changed = false;

            // A linha "Em breve" do Estagiário (D-42) vira a compra de verdade (500 no GDD v7).
            var entries = so.FindProperty("entries");
            SerializedProperty pickaxe = null, speed = null, mining = null;
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                bool locked = entry.FindPropertyRelative("locked").boolValue;
                var id = (UpgradeId)entry.FindPropertyRelative("upgrade").enumValueIndex;
                if (locked && entry.FindPropertyRelative("title").stringValue == "Estagiário")
                {
                    entry.FindPropertyRelative("locked").boolValue = false;
                    entry.FindPropertyRelative("upgrade").enumValueIndex = (int)UpgradeId.Intern;
                    entry.FindPropertyRelative("boughtLabel").stringValue = "contratado";
                    // A moldura cinza era a do "Em breve"; à venda, a roxa (épico), como no mockup.
                    var epic = PermanentShopSetup.Frame("slot_rarity_epic");
                    if (epic != null) entry.FindPropertyRelative("frame").objectReferenceValue = epic;
                    changed = true;
                }
                if (locked) continue;
                if (id == UpgradeId.Pickaxe) pickaxe = entry;
                else if (id == UpgradeId.Speed) speed = entry;
                else if (id == UpgradeId.Mining) mining = entry;
            }

            // D-207: as duas Picaretas contam o nível a partir de 1 (nível 1 = sem compra), como no GDD do Apellon.
            changed |= MarkLevelFromOne(so.FindProperty("entries"), UpgradeId.Pickaxe);
            changed |= MarkLevelFromOne(so.FindProperty("internEntries"), UpgradeId.InternPickaxe);

            if (string.IsNullOrEmpty(so.FindProperty("internTabLabel").stringValue))
            {
                so.FindProperty("internTabLabel").stringValue = "ESTAGIÁRIO";
                changed = true;
            }

            // As melhorias dele, com os ícones e molduras das do anão (do mockup).
            var intern = so.FindProperty("internEntries");
            if (intern.arraySize == 0)
            {
                AddUpgrade(intern, UpgradeId.InternPickaxe, "Picareta", "Dano {0} » {1}", ShopCatalog.EffectStyle.Number, pickaxe);
                AddUpgrade(intern, UpgradeId.InternSpeed, "Velocidade", "Golpe a cada {0} » {1}", ShopCatalog.EffectStyle.Seconds, speed);
                AddUpgrade(intern, UpgradeId.InternMining, "Mineração", "Lascas {0} » {1}", ShopCatalog.EffectStyle.BonusPercent, mining); // D-209: online e offline
                changed = true;
            }

            // Treinar e Promoção depois das 5 do anão (D-170). Os ícones entram pelo PermanentShopSetup (ArtFor).
            var permanents = so.FindProperty("permanentEntries");
            if (permanents.arraySize > 0)
            {
                changed |= AddPermanent(permanents, PermanentId.InternTraining, "Treinar", "Dano dele {0} » {1}",
                                        "Dano dele {0}", "", ShopCatalog.EffectStyle.ChancePercent);
                changed |= AddPermanent(permanents, PermanentId.InternPromotion, "Promoção", "Começa a run contratado",
                                        "Começa a run contratado", "Pede o Treinar no {0}", ShopCatalog.EffectStyle.Number);
                for (int i = 0; i < permanents.arraySize; i++)
                {
                    var entry = permanents.GetArrayElementAtIndex(i);
                    var (icon, frame) = PermanentShopSetup.ArtFor((PermanentId)entry.FindPropertyRelative("permanent").enumValueIndex);
                    changed |= SetIfEmpty(entry.FindPropertyRelative("icon"), icon);
                    changed |= SetIfEmpty(entry.FindPropertyRelative("frame"), frame);
                }
            }

            if (!changed) return;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Estagiário na loja: a linha de compra, a aba ESTAGIÁRIO e Treinar/Promoção (M7, E3, D-174).");
        }

        static bool MarkLevelFromOne(SerializedProperty entries, UpgradeId id)
        {
            bool changed = false;
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("locked").boolValue) continue;
                if ((UpgradeId)entry.FindPropertyRelative("upgrade").enumValueIndex != id) continue;
                var flag = entry.FindPropertyRelative("levelFromOne");
                if (flag.boolValue) continue;
                flag.boolValue = true;
                changed = true;
            }
            return changed;
        }

        static void AddUpgrade(SerializedProperty entries, UpgradeId id, string title, string effect,
                               ShopCatalog.EffectStyle style, SerializedProperty artFrom)
        {
            int i = entries.arraySize;
            entries.InsertArrayElementAtIndex(i);
            var entry = entries.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("locked").boolValue = false;
            entry.FindPropertyRelative("upgrade").enumValueIndex = (int)id;
            entry.FindPropertyRelative("title").stringValue = title;
            entry.FindPropertyRelative("effectFormat").stringValue = effect;
            entry.FindPropertyRelative("style").enumValueIndex = (int)style;
            entry.FindPropertyRelative("boughtLabel").stringValue = "";
            entry.FindPropertyRelative("iconColor").colorValue =
                artFrom != null ? artFrom.FindPropertyRelative("iconColor").colorValue : Color.white;
            entry.FindPropertyRelative("icon").objectReferenceValue = artFrom?.FindPropertyRelative("icon").objectReferenceValue;
            entry.FindPropertyRelative("frame").objectReferenceValue = artFrom?.FindPropertyRelative("frame").objectReferenceValue;
        }

        static bool AddPermanent(SerializedProperty entries, PermanentId id, string title, string effect, string maxed,
                                 string locked, ShopCatalog.EffectStyle style)
        {
            for (int i = 0; i < entries.arraySize; i++)
                if ((PermanentId)entries.GetArrayElementAtIndex(i).FindPropertyRelative("permanent").enumValueIndex == id)
                    return false;

            int index = entries.arraySize;
            entries.InsertArrayElementAtIndex(index);
            var entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("permanent").enumValueIndex = (int)id;
            entry.FindPropertyRelative("title").stringValue = title;
            entry.FindPropertyRelative("effectFormat").stringValue = effect;
            entry.FindPropertyRelative("maxedFormat").stringValue = maxed;
            entry.FindPropertyRelative("lockedFormat").stringValue = locked;
            entry.FindPropertyRelative("style").enumValueIndex = (int)style;
            entry.FindPropertyRelative("icon").objectReferenceValue = null;
            entry.FindPropertyRelative("frame").objectReferenceValue = null;
            return true;
        }

        static bool SetIfEmpty(SerializedProperty property, Object value)
        {
            if (value == null || property.objectReferenceValue != null) return false;
            property.objectReferenceValue = value;
            return true;
        }

        // ── Prefabs ─────────────────────────────────────────────────────────────────────────────────────────────

        /// <summary>7 linhas na aba das permanentes: de 46 para 40 px (D-174). Só troca o 46 de antes.</summary>
        static void EnsurePermanentRowHeight()
        {
            if (!File.Exists(PermanentRowPrefabPath)) return;
            var root = PrefabUtility.LoadPrefabContents(PermanentRowPrefabPath);
            try
            {
                var element = root.GetComponent<LayoutElement>();
                if (element == null || !Mathf.Approximately(element.preferredHeight, OldPermanentRowHeight)) return;
                element.preferredHeight = PermanentRowHeight;
                if (Mathf.Approximately(element.minHeight, OldPermanentRowHeight)) element.minHeight = PermanentRowHeight;
                PrefabUtility.SaveAsPrefabAsset(root, PermanentRowPrefabPath);
                Debug.Log($"[Setup] Linha das permanentes com {PermanentRowHeight} px (M7, E3, D-174).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>A moldura cinza da compra única feita (o Estagiário contratado), na linha da loja.</summary>
        static void EnsureDoneFrame()
        {
            var frame = PermanentShopSetup.Frame("slot_disabled");
            if (!File.Exists(RowPrefabPath) || frame == null) return;
            var root = PrefabUtility.LoadPrefabContents(RowPrefabPath);
            try
            {
                var so = new SerializedObject(root.GetComponent<UpgradeRowView>());
                var property = so.FindProperty("doneFrame");
                if (property.objectReferenceValue != null) return;
                property.objectReferenceValue = frame;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, RowPrefabPath);
                Debug.Log("[Setup] Linha da loja com a moldura da compra única feita (M7, E3).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ── A 3ª aba (cena) ─────────────────────────────────────────────────────────────────────────────────────

        static bool EnsureTab(Transform safeArea, ShopCatalog catalog)
        {
            var shop = safeArea.Find("Shop");
            var panel = shop != null ? shop.Find("Screen/Panel") : null;
            var tabs = panel != null ? panel.Find("Tabs") : null;
            var permanentRows = panel != null ? panel.Find("PermanentRows") : null;
            if (tabs == null || permanentRows == null || tabs.Find("InternTab") != null) return false;

            var font = tabs.Find("RunTab/Label").GetComponent<TMPro.TMP_Text>().font;
            var (button, label, badge) = PermanentShopSetup.Tab(tabs, "InternTab", catalog.InternTabLabel, font,
                                                                 shop.Find("ShopButton/Badge"));

            // A lista dele: uma cópia vazia da das permanentes (o mesmo lugar e o mesmo layout).
            var rows = Object.Instantiate(permanentRows.gameObject, panel, false);
            rows.name = "InternRows";
            for (int i = rows.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(rows.transform.GetChild(i).gameObject);
            rows.transform.SetSiblingIndex(permanentRows.GetSiblingIndex() + 1);
            rows.SetActive(false);

            GrayboxSetup.SetReferences(shop.GetComponent<ShopView>(),
                ("internTabButton", button), ("internTabLabel", label), ("internTabBadge", badge),
                ("internRowsParent", rows.transform));
            button.gameObject.SetActive(false); // aparece com ele contratado
            Debug.Log("[Setup] Aba ESTAGIÁRIO na loja (M7, E3, D-170).");
            return true;
        }
    }
}
