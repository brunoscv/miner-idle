using MinersIdle.Game;
using MinersIdle.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="ProjectSetup"/> para a E1 do Marco 8b (D-159), com a cena Game aberta: o menu DBG → Dia
    /// (<see cref="DayDebugPanel"/>), com os botões e o resumo do dia e das permanentes. O menu em si (fundo, grade,
    /// Fechar) vem do <see cref="DebugPanelSetup"/>. Só cria o que falta; os textos ficam na cena.
    /// </summary>
    static class DayDebugSetup
    {
        /// <summary>Os botões do menu, na ordem da grade de 3 colunas (o Fechar vai por último).</summary>
        static readonly (string name, string field, string label)[] Buttons =
        {
            ("AddRed10", "addRed10", "+10 vermelhas"),
            ("AddRed100", "addRed100", "+100 vermelhas"),
            ("RestNow", "restNow", "Descansar agora"),
            ("GrantPermanents", "grantLevels", "+1 nv permanentes"),
            ("ResetPermanents", "resetPermanents", "Zerar permanentes"),
        };

        const string SummaryFormat = "Dia {0} · banco {1} · leva agora {2} (andar {3})\n" +
                                     "Memória {4} · Sangue {5} · Coração {6}\nDedos {7} · Alma {8}" + NewPermanents;

        /// <summary>M9d E1: os níveis do Dedo Fogoso e do Atalho no fim do resumo.</summary>
        const string NewPermanents = " · Fogoso {9} · Atalho {10}";

        const string ShortcutName = "Shortcut";

        static readonly Color TextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);

        public static bool EnsureInScene()
        {
            var debugPanel = Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include);
            var menu = debugPanel != null ? debugPanel.transform.Find(DebugPanelSetup.DayMenuName) : null;
            if (menu == null) return false;
            if (menu.GetComponent<DayDebugPanel>() is DayDebugPanel existing)
                return EnsureFloorWord(existing) | EnsureShortcut(menu, existing);

            var grid = menu.Find("Content/Buttons");
            var references = new System.Collections.Generic.List<(string, Object)>
            {
                ("runner", Object.FindAnyObjectByType<MiningRunner>()),
                ("menu", menu.GetComponent<DebugMenu>()),
            };
            foreach (var (name, field, label) in Buttons)
            {
                var button = grid.Find(name);
                references.Add((field, button != null ? button.GetComponent<Button>() : DebugPanelSetup.CreateButton(grid, name, label)));
            }
            grid.Find("Close")?.SetAsLastSibling();

            // O resumo no alto do menu, acima da grade.
            var content = menu.Find("Content");
            var summary = GrayboxSetup.CreateHudText(content, "Summary", "", 7f, TextColor, new Vector2(0f, 1f), new Vector2(1f, 1f));
            var rect = (RectTransform)summary.transform;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0f, -DebugPanelSetup.DayTextHeight);
            rect.offsetMax = Vector2.zero;
            summary.alignment = TextAlignmentOptions.TopLeft;
            summary.margin = new Vector4(4f, 3f, 4f, 0f);
            references.Add(("summary", summary));

            var panel = menu.gameObject.AddComponent<DayDebugPanel>();
            GrayboxSetup.SetReferences(panel, references.ToArray());
            var so = new UnityEditor.SerializedObject(panel);
            so.FindProperty("summaryFormat").stringValue = SummaryFormat;
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[Setup] Painel de debug: menu \"Dia\" com lascas vermelhas, Descansar agora e as permanentes (Marco 8b, D-159).");
            EnsureShortcut(menu, panel);
            return true;
        }

        /// <summary>
        /// M9d E1 (D-277): o botão "Atalho" na grade (antes do Fechar), +1 nível por toque e de volta ao 0 no limite, e os
        /// níveis do Dedo Fogoso e do Atalho no resumo.
        /// </summary>
        static bool EnsureShortcut(Transform menu, DayDebugPanel panel)
        {
            var grid = menu.Find("Content/Buttons");
            if (grid == null) return false;
            bool changed = false;

            var button = grid.Find(ShortcutName);
            if (button == null)
            {
                button = DebugPanelSetup.CreateButton(grid, ShortcutName, "Atalho 0/5").transform;
                var close = grid.Find("Close");
                if (close != null) button.SetSiblingIndex(close.GetSiblingIndex());
                changed = true;
            }

            var so = new UnityEditor.SerializedObject(panel);
            var reference = so.FindProperty("shortcut");
            var target = button.GetComponent<Button>();
            if (reference.objectReferenceValue != target)
            {
                reference.objectReferenceValue = target;
                changed = true;
            }
            var label = so.FindProperty("shortcutFormat");
            if (string.IsNullOrEmpty(label.stringValue))
            {
                label.stringValue = "Atalho {0}/{1}";
                changed = true;
            }
            var format = so.FindProperty("summaryFormat");
            if (!format.stringValue.Contains("{9}"))
            {
                format.stringValue += NewPermanents;
                changed = true;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            if (changed) Debug.Log("[Setup] DBG → Dia: botão \"Atalho\" e o Dedo Fogoso e o Atalho no resumo (M9d E1, D-277).");
            return changed;
        }

        const string RestBackgroundName = "RestBackground";

        /// <summary>
        /// M9d E2 (D-279): o botão "Fundo" na grade do DBG → Dia (antes do Fechar), preta ⇄ desfocada, ligado à tela do
        /// descanso. Chamado pelo <see cref="RestSetup"/> depois de criar a tela.
        /// </summary>
        internal static bool EnsureRestBackground(RestShopScreen restShop)
        {
            var debugPanel = Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include);
            var menu = debugPanel != null ? debugPanel.transform.Find(DebugPanelSetup.DayMenuName) : null;
            var panel = menu != null ? menu.GetComponent<DayDebugPanel>() : null;
            var grid = menu != null ? menu.Find("Content/Buttons") : null;
            if (panel == null || grid == null || restShop == null) return false;
            bool changed = false;

            var button = grid.Find(RestBackgroundName);
            if (button == null)
            {
                button = DebugPanelSetup.CreateButton(grid, RestBackgroundName, "Fundo: preta").transform;
                var close = grid.Find("Close");
                if (close != null) button.SetSiblingIndex(close.GetSiblingIndex());
                changed = true;
            }

            var so = new UnityEditor.SerializedObject(panel);
            changed |= Set(so.FindProperty("restBackground"), button.GetComponent<Button>());
            changed |= Set(so.FindProperty("restShop"), restShop);
            changed |= SetText(so.FindProperty("backgroundBlackLabel"), "Fundo: preta");
            changed |= SetText(so.FindProperty("backgroundBlurredLabel"), "Fundo: desfocada");
            so.ApplyModifiedPropertiesWithoutUndo();
            if (changed) Debug.Log("[Setup] DBG → Dia: botão \"Fundo\" da tela do descanso, preta ⇄ desfocada (M9d E2, D-279).");
            return changed;
        }

        static bool Set(UnityEditor.SerializedProperty property, Object value)
        {
            if (property.objectReferenceValue == value) return false;
            property.objectReferenceValue = value;
            return true;
        }

        static bool SetText(UnityEditor.SerializedProperty property, string value)
        {
            if (!string.IsNullOrEmpty(property.stringValue)) return false;
            property.stringValue = value;
            return true;
        }

        /// <summary>P9 (M9c E2): o resumo do DBG → Dia diz "andar" no lugar de "nível".</summary>
        static bool EnsureFloorWord(DayDebugPanel panel)
        {
            var so = new UnityEditor.SerializedObject(panel);
            var format = so.FindProperty("summaryFormat");
            if (!format.stringValue.Contains("(nível {3})")) return false;
            format.stringValue = format.stringValue.Replace("(nível {3})", "(andar {3})");
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Setup] DBG → Dia: \"andar\" no lugar de \"nível\" (M9c E2, P9).");
            return true;
        }
    }
}
