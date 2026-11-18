using MinersIdle.Game;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="ProjectSetup"/> para a E3b-3 do M8 (D-143), com a cena Game aberta: o menu DBG → Cálculos
    /// (<see cref="CalculationsPanel"/>): os botões, a linha da Picareta e o aviso fixo ao lado do botão DBG. No v8 (M9c E1,
    /// D-256) saem o seletor de balanço e o botão "Picareta" (pela caverna) do menu Andar. O menu em si (fundo, grade, Fechar) vem do
    /// <see cref="DebugPanelSetup"/>. Só cria o que falta; os textos ficam na cena.
    /// </summary>
    static class CalculationsSetup
    {
        /// <summary>O nome do balanço no aviso: o único desde o v8.</summary>
        const string BalanceLabel = "GDD v8";

        /// <summary>Os botões do menu, na ordem da grade de 3 colunas (o Fechar vai por último).</summary>
        static readonly (string name, string field, string label)[] Buttons =
        {
            ("RestartRun", "restartRun", "Reiniciar run"),
            ("ResetDefaults", "resetDefaults", "Padrão GDD"),
            ("GrowthMinus", "growthMinus", "Taxa -"),
            ("GrowthValue", "growthValue", "Taxa 1,2"),
            ("GrowthPlus", "growthPlus", "Taxa +"),
            ("StepMinus", "stepMinus", "Incremento -"),
            ("StepValue", "stepValue", "Incremento 0"),
            ("StepPlus", "stepPlus", "Incremento +"),
        };

        const string PickaxeFormat = "Picareta nv {3} · dano agora {0}\nPróxima compra {1} · a linha inteira {2}";
        const string WarningName = "CalcWarning";

        public static bool EnsureInScene()
        {
            bool changed = false;

            var debugPanel = Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include);
            var menu = debugPanel != null ? debugPanel.transform.Find(DebugPanelSetup.CalcMenuName) : null;
            if (menu == null) return changed;
            var existing = menu.GetComponent<CalculationsPanel>();
            if (existing != null)
            {
                changed |= EnsureWarningBack(debugPanel, existing);
                changed |= RemoveComboStacking(menu);
                changed |= EnsureRareRhythm(menu, existing);
                changed |= EnsureCrystalSpacing(menu, existing);
                return RemoveRetiredButtons(debugPanel, menu, existing) || changed;
            }

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

            // A linha da Picareta no alto do menu, acima da grade.
            var content = menu.Find("Content");
            var pickaxe = GrayboxSetup.CreateHudText(content, "Pickaxe", "", 7f, TextColor, new Vector2(0f, 1f), new Vector2(1f, 1f));
            var pickaxeRect = (RectTransform)pickaxe.transform;
            pickaxeRect.pivot = new Vector2(0.5f, 1f);
            pickaxeRect.offsetMin = new Vector2(0f, -DebugPanelSetup.CalcTextHeight);
            pickaxeRect.offsetMax = Vector2.zero;
            pickaxe.alignment = TextAlignmentOptions.TopLeft;
            pickaxe.margin = new Vector4(4f, 3f, 4f, 0f);
            references.Add(("pickaxe", pickaxe));

            references.Add(("warning", CreateWarning(debugPanel.transform)));

            var panel = menu.gameObject.AddComponent<CalculationsPanel>();
            GrayboxSetup.SetReferences(panel, references.ToArray());

            var so = new SerializedObject(panel);
            so.FindProperty("balanceLabel").stringValue = BalanceLabel;
            so.FindProperty("growthFormat").stringValue = "Taxa {0}";
            so.FindProperty("stepFormat").stringValue = "Incremento {0}";
            so.FindProperty("pickaxeFormat").stringValue = PickaxeFormat;
            so.FindProperty("maxedLabel").stringValue = "no limite";
            so.FindProperty("warningFormat").stringValue = "Cálculo: {0}";
            so.FindProperty("growthChangedFormat").stringValue = " · Picareta taxa {0}";
            so.FindProperty("stepChangedFormat").stringValue = " · Picareta incremento {0}";
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[Setup] Painel de debug: menu \"Cálculos\" com a taxa e o incremento da Picareta, Reiniciar run e o " +
                      "aviso do cálculo na tela (M8 E3b-3, D-143).");
            EnsureRareRhythm(menu, panel);
            EnsureCrystalSpacing(menu, panel);
            return true;
        }

        const string CrystalSpacingName = "CrystalSpacing";

        /// <summary>
        /// M9d E1 (D-277): o botão "Distância" na grade do menu Cálculos (antes do Fechar): a distância entre cristais do
        /// GDD (3 m) ⇄ 1 m. O rótulo mostra a distância em uso.
        /// </summary>
        static bool EnsureCrystalSpacing(Transform menu, CalculationsPanel panel)
        {
            var grid = menu.Find("Content/Buttons");
            if (grid == null) return false;
            bool changed = false;

            var button = grid.Find(CrystalSpacingName);
            if (button == null)
            {
                button = DebugPanelSetup.CreateButton(grid, CrystalSpacingName, "Distância: 3 m").transform;
                var close = grid.Find("Close");
                if (close != null) button.SetSiblingIndex(close.GetSiblingIndex());
                changed = true;
            }

            var so = new SerializedObject(panel);
            changed |= SetIfDifferent(so.FindProperty("crystalSpacing"), button.GetComponent<Button>());
            changed |= SetIfEmpty(so.FindProperty("spacingFormat"), "Distância: {0} m");
            changed |= SetIfEmpty(so.FindProperty("spacingChangedFormat"), " · distância {0} m");
            so.ApplyModifiedPropertiesWithoutUndo();

            if (changed) Debug.Log("[Setup] Painel de debug: botão \"Distância\" (3 m ⇄ 1 m) no menu Cálculos (M9d E1, D-277).");
            return changed;
        }

        const string RareRhythmName = "RareRhythm";

        /// <summary>
        /// M9b E4 (D-226): o botão "Raro" na grade do menu Cálculos (antes do Fechar): o ritmo do cristal raro do documento
        /// (15%) ⇄ o lento (2%, D-185). O rótulo mostra a chance em uso, lida do balanço.
        /// </summary>
        static bool EnsureRareRhythm(Transform menu, CalculationsPanel panel)
        {
            var grid = menu.Find("Content/Buttons");
            if (grid == null) return false;
            bool changed = false;

            var button = grid.Find(RareRhythmName);
            if (button == null)
            {
                button = DebugPanelSetup.CreateButton(grid, RareRhythmName, "Raro: 15%").transform;
                var close = grid.Find("Close");
                if (close != null) button.SetSiblingIndex(close.GetSiblingIndex());
                changed = true;
            }

            var so = new SerializedObject(panel);
            changed |= SetIfDifferent(so.FindProperty("rareRhythm"), button.GetComponent<Button>());
            changed |= SetIfEmpty(so.FindProperty("rareRhythmFormat"), "Raro: {0}");
            changed |= SetIfEmpty(so.FindProperty("rareSlowChangedFormat"), " · raro {0}");
            so.ApplyModifiedPropertiesWithoutUndo();

            if (changed) Debug.Log("[Setup] Painel de debug: botão \"Raro\" (ritmo 15% ⇄ 2%) no menu Cálculos (M9b E4, D-226).");
            return changed;
        }

        const string ComboStackingName = "ComboStacking";

        /// <summary>
        /// M9g (D-311): o botão "Acúmulo" (a soma dos marcos do combo ⇄ só o maior, D-217) sai do menu Cálculos com o
        /// combo. Devolve se mudou.
        /// </summary>
        static bool RemoveComboStacking(Transform menu)
        {
            var button = menu.Find("Content/Buttons/" + ComboStackingName);
            if (button == null) return false;
            Object.DestroyImmediate(button.gameObject);
            Debug.Log("[Setup] Painel de debug: sai o botão \"Acúmulo\" do menu Cálculos (M9g, D-311).");
            return true;
        }

        /// <summary>
        /// M9c E1 (D-256): o seletor de balanço do menu Cálculos e o botão "Picareta" (pela caverna) do menu Andar saem da
        /// cena; o aviso passa a dizer "GDD v8". Devolve se mudou.
        /// </summary>
        static bool RemoveRetiredButtons(DebugPanel debugPanel, Transform menu, CalculationsPanel panel)
        {
            bool changed = false;
            foreach (var retired in new[] { menu.Find("Content/Buttons/Balance"),
                                            debugPanel.transform.Find("CaveMenu/Content/Buttons/PickaxeByCave") })
            {
                if (retired == null) continue;
                Object.DestroyImmediate(retired.gameObject);
                changed = true;
            }
            var so = new SerializedObject(panel);
            var label = so.FindProperty("balanceLabel");
            if (label.stringValue != BalanceLabel)
            {
                label.stringValue = BalanceLabel;
                so.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }
            if (changed) Debug.Log("[Setup] DBG: sem o seletor de balanço e sem a Picareta pela caverna; o aviso diz GDD v8 (M9c E1).");
            return changed;
        }

        static bool SetIfDifferent(SerializedProperty property, Object value)
        {
            if (property.objectReferenceValue == value) return false;
            property.objectReferenceValue = value;
            return true;
        }

        static bool SetIfEmpty(SerializedProperty property, string value)
        {
            if (!string.IsNullOrEmpty(property.stringValue)) return false;
            property.stringValue = value;
            return true;
        }

        static readonly Color TextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        static readonly Color WarningBack = new Color(0f, 0f, 0f, 0.65f);

        /// <summary>
        /// O aviso fixo: ao lado do botão DBG, no pé da tela, numa faixa escura do tamanho do texto (lê-se sobre qualquer
        /// chão). Filho do painel de debug: some com ele no release. Não recebe toque.
        /// </summary>
        static TMP_Text CreateWarning(Transform panel)
        {
            var back = new GameObject(WarningName, typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup),
                                      typeof(ContentSizeFitter));
            back.transform.SetParent(panel, false);
            var rect = (RectTransform)back.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = DebugPanelSetup.WarningPosition;
            rect.sizeDelta = new Vector2(60f, 16f);
            var image = back.GetComponent<Image>();
            image.color = WarningBack;
            image.raycastTarget = false;
            var layout = back.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 0, 0);
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            back.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            var text = GrayboxSetup.CreateHudText(back.transform, "Text", "", 7f, TextColor, Vector2.zero, Vector2.one);
            text.alignment = TextAlignmentOptions.Left;
            return text;
        }

        /// <summary>O aviso da primeira versão (texto solto, sem a faixa escura) vira o novo, no mesmo lugar.</summary>
        static bool EnsureWarningBack(DebugPanel debugPanel, CalculationsPanel panel)
        {
            var old = debugPanel.transform.Find(WarningName);
            if (old == null || old.GetComponent<Image>() != null) return false;
            Object.DestroyImmediate(old.gameObject);
            GrayboxSetup.SetReferences(panel, ("warning", CreateWarning(debugPanel.transform)));
            Debug.Log("[Setup] Aviso do cálculo com faixa escura atrás (E3b-3).");
            return true;
        }
    }
}
