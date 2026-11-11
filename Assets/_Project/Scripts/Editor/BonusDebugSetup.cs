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
    /// Parte do <see cref="ProjectSetup"/> para a E1 do M9b, com a cena Game aberta: o menu DBG → Bônus
    /// (<see cref="BonusDebugPanel"/>), com os atalhos de combo ×10, ×30, ×50 e ×100, o Limpar e a lista dos bônus ativos no
    /// alto. O menu em si (fundo, grade, Fechar) vem do <see cref="DebugPanelSetup"/>. Só cria o que falta; os textos ficam
    /// na cena.
    /// </summary>
    static class BonusDebugSetup
    {
        /// <summary>Os botões de combo, na ordem de <see cref="BonusDebugPanel.Combos"/>.</summary>
        static readonly (string name, string label)[] GrantButtons =
        {
            ("Combo10", "Combo ×10"),
            ("Combo30", "Combo ×30"),
            ("Combo50", "Combo ×50"),
            ("Combo100", "Combo ×100"),
        };

        const string ClearName = "ClearBonuses";
        const string ClearLabel = "Limpar bônus";

        static readonly Color TextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);

        public static bool EnsureInScene()
        {
            var debugPanel = Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include);
            var menu = debugPanel != null ? debugPanel.transform.Find(DebugPanelSetup.BonusMenuName) : null;
            if (menu == null) return false;
            var current = menu.GetComponent<BonusDebugPanel>();
            if (current != null) return EnsureRare(menu, current) | EnsureAd(menu, current);

            var grid = menu.Find("Content/Buttons");
            var grant = new Button[GrantButtons.Length];
            for (int i = 0; i < GrantButtons.Length; i++)
            {
                var (name, label) = GrantButtons[i];
                var existing = grid.Find(name);
                grant[i] = existing != null ? existing.GetComponent<Button>() : DebugPanelSetup.CreateButton(grid, name, label);
                grant[i].transform.SetSiblingIndex(i);
            }
            var clearButton = grid.Find(ClearName);
            var clear = clearButton != null ? clearButton.GetComponent<Button>() : DebugPanelSetup.CreateButton(grid, ClearName, ClearLabel);
            grid.Find("Close")?.SetAsLastSibling();

            // O combo, a regra do acúmulo e os bônus ativos no alto do menu, acima da grade.
            var content = menu.Find("Content");
            var summary = GrayboxSetup.CreateHudText(content, "Summary", "", 7f, TextColor, new Vector2(0f, 1f), new Vector2(1f, 1f));
            var rect = (RectTransform)summary.transform;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0f, -DebugPanelSetup.DayTextHeight);
            rect.offsetMax = Vector2.zero;
            summary.alignment = TextAlignmentOptions.TopLeft;
            summary.margin = new Vector4(4f, 3f, 4f, 0f);

            var panel = menu.gameObject.AddComponent<BonusDebugPanel>();
            GrayboxSetup.SetReferences(panel,
                ("runner", Object.FindAnyObjectByType<MiningRunner>()),
                ("clear", clear),
                ("summary", summary));

            var so = new SerializedObject(panel);
            var buttons = so.FindProperty("grant");
            buttons.arraySize = grant.Length;
            for (int i = 0; i < grant.Length; i++) buttons.GetArrayElementAtIndex(i).objectReferenceValue = grant[i];
            so.FindProperty("headerFormat").stringValue = "Combo ×{0} · {1} · dano +{2}";
            so.FindProperty("cumulativeLabel").stringValue = "acumulativo";
            so.FindProperty("highestOnlyLabel").stringValue = "só o maior marco";
            so.FindProperty("noneLabel").stringValue = "Nenhum bônus ativo. Toque até ×10 e pare, ou use os atalhos.";
            so.FindProperty("damageFormat").stringValue = "×{2}: +{0} dano, {1}";
            so.FindProperty("criticalFormat").stringValue = "×{2}: crítico garantido, {1}";
            so.FindProperty("separator").stringValue = " · ";
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[Setup] Painel de debug: menu \"Bônus\" com combo ×10, ×30, ×50, ×100 e Limpar bônus (M9b, E1).");
            EnsureRare(menu, panel);
            EnsureAd(menu, panel);
            return true;
        }

        /// <summary>M9b E5 (D-234): AD agora e Pular espera, antes do Fechar; o menu cresce para 5 fileiras.</summary>
        static bool EnsureAd(Transform menu, BonusDebugPanel panel)
        {
            var grid = menu.Find("Content/Buttons");
            if (grid == null) return false;
            bool changed = false;
            Button Ensure(string name, string label)
            {
                var found = grid.Find(name);
                if (found != null) return found.GetComponent<Button>();
                changed = true;
                return DebugPanelSetup.CreateButton(grid, name, label);
            }
            var now = Ensure("AdNow", "AD agora");
            var skip = Ensure("AdSkip", "Pular espera");
            grid.Find("Close")?.SetAsLastSibling();

            var content = (RectTransform)menu.Find("Content");
            if (content != null && !Mathf.Approximately(content.sizeDelta.y, DebugPanelSetup.BonusMenuHeight))
            {
                content.sizeDelta = new Vector2(content.sizeDelta.x, DebugPanelSetup.BonusMenuHeight);
                changed = true;
            }

            var so = new SerializedObject(panel);
            var screen = Object.FindAnyObjectByType<SimulatedAdScreen>(FindObjectsInactive.Include);
            if (so.FindProperty("adNow").objectReferenceValue != now || so.FindProperty("adScreen").objectReferenceValue != screen)
            {
                so.FindProperty("adNow").objectReferenceValue = now;
                so.FindProperty("adSkip").objectReferenceValue = skip;
                so.FindProperty("adScreen").objectReferenceValue = screen;
                so.FindProperty("adFormat").stringValue = "AD ×{0}, {1}";
                so.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }
            if (changed) Debug.Log("[Setup] Painel de debug: no menu Bônus, AD agora e Pular espera (M9b E5, D-234).");
            return changed;
        }

        /// <summary>Os atalhos do raro, na ordem da grade depois do Limpar (o nome do objeto, o rótulo).</summary>
        static readonly (string name, string label)[] RareButtons =
        {
            ("NextRare", "Próximo raro"),
            ("RareWindow", "Janela agora"),
            ("CardDamage", "Carta Dano ×2"),
            ("CardSpeed", "Carta Rápido"),
            ("CardShards", "Carta Lascas ×2"),
            ("CardCritical", "Carta Crítico"),
        };

        static readonly string[] CardLabels = { "Dano ×2", "Rápido", "Lascas ×2", "Crítico +25%" };

        /// <summary>
        /// M9b E4 (D-226): Próximo raro, Janela agora e uma carta de cada tipo, antes do Fechar; o menu cresce para 4
        /// fileiras; os textos do raro no alto.
        /// </summary>
        static bool EnsureRare(Transform menu, BonusDebugPanel panel)
        {
            var grid = menu.Find("Content/Buttons");
            if (grid == null) return false;
            bool changed = false;
            var buttons = new Button[RareButtons.Length];
            for (int i = 0; i < RareButtons.Length; i++)
            {
                var (name, label) = RareButtons[i];
                var found = grid.Find(name);
                if (found == null)
                {
                    buttons[i] = DebugPanelSetup.CreateButton(grid, name, label);
                    changed = true;
                }
                else buttons[i] = found.GetComponent<Button>();
            }
            grid.Find("Close")?.SetAsLastSibling();

            var content = (RectTransform)menu.Find("Content");
            if (content != null && !Mathf.Approximately(content.sizeDelta.y, DebugPanelSetup.BonusMenuHeight))
            {
                content.sizeDelta = new Vector2(content.sizeDelta.x, DebugPanelSetup.BonusMenuHeight);
                changed = true;
            }

            var so = new SerializedObject(panel);
            var next = so.FindProperty("nextRare");
            if (next.objectReferenceValue != buttons[0])
            {
                next.objectReferenceValue = buttons[0];
                so.FindProperty("openWindow").objectReferenceValue = buttons[1];
                var cards = so.FindProperty("cards");
                cards.arraySize = 4;
                for (int i = 0; i < 4; i++) cards.GetArrayElementAtIndex(i).objectReferenceValue = buttons[2 + i];
                var labels = so.FindProperty("cardLabels");
                labels.arraySize = CardLabels.Length;
                for (int i = 0; i < CardLabels.Length; i++) labels.GetArrayElementAtIndex(i).stringValue = CardLabels[i];
                so.FindProperty("rareCardFormat").stringValue = "{0}, {1}";
                so.FindProperty("windowFormat").stringValue = "Janela: {0} ou {1}, {2} · fila {3}";
                so.FindProperty("windowPickedFormat").stringValue = "Janela: saiu {0} (FECHAR) · fila {1}";
                so.FindProperty("nextRareLabel").stringValue = "O próximo cristal que nascer é raro.";
                so.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }

            if (changed) Debug.Log("[Setup] Painel de debug: no menu Bônus, Próximo raro, Janela agora e as 4 cartas (M9b E4, D-226).");
            return changed;
        }
    }
}
