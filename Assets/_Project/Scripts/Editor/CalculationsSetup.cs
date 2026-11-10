using MinersIdle.Game;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="ProjectSetup"/> para a E3b-3 do M8 (D-143), com a cena Game aberta: os balanços do seletor no
    /// <see cref="MiningRunner"/> (GDD v7, o padrão, e GDD v5) e o menu DBG → Cálculos (<see cref="CalculationsPanel"/>):
    /// os botões, a linha da Picareta e o aviso fixo ao lado do botão DBG. O menu em si (fundo, grade, Fechar) vem do
    /// <see cref="DebugPanelSetup"/>. Só cria o que falta; os textos ficam na cena.
    /// </summary>
    static class CalculationsSetup
    {
        /// <summary>Na ordem dos <see cref="MiningRunner.Balances"/>: o primeiro é o do GDD.</summary>
        static readonly string[] BalancePaths = { ProjectSetup.GddV7BalancePath, ProjectSetup.GddV5BalancePath };
        static readonly string[] BalanceLabels = { "GDD v7", "GDD v5" };

        /// <summary>Os botões do menu, na ordem da grade de 3 colunas (o Fechar vai por último).</summary>
        static readonly (string name, string field, string label)[] Buttons =
        {
            ("Balance", "balance", "GDD v7"),
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
            bool changed = EnsureBalances();

            var debugPanel = Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include);
            var menu = debugPanel != null ? debugPanel.transform.Find(DebugPanelSetup.CalcMenuName) : null;
            if (menu == null) return changed;
            var existing = menu.GetComponent<CalculationsPanel>();
            if (existing != null)
            {
                changed |= EnsureWarningBack(debugPanel, existing);
                return EnsurePickaxeByCave(debugPanel, existing) || changed;
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
            var labels = so.FindProperty("balanceLabels");
            labels.arraySize = BalanceLabels.Length;
            for (int i = 0; i < BalanceLabels.Length; i++) labels.GetArrayElementAtIndex(i).stringValue = BalanceLabels[i];
            so.FindProperty("growthFormat").stringValue = "Taxa {0}";
            so.FindProperty("stepFormat").stringValue = "Incremento {0}";
            so.FindProperty("pickaxeFormat").stringValue = PickaxeFormat;
            so.FindProperty("maxedLabel").stringValue = "no limite";
            so.FindProperty("warningFormat").stringValue = "Cálculo: {0}";
            so.FindProperty("growthChangedFormat").stringValue = " · Picareta taxa {0}";
            so.FindProperty("stepChangedFormat").stringValue = " · Picareta incremento {0}";
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[Setup] Painel de debug: menu \"Cálculos\" com o seletor GDD v7/v5, a taxa e o incremento da Picareta, " +
                      "Reiniciar run e o aviso do cálculo na tela (M8 E3b-3, D-143).");
            EnsurePickaxeByCave(debugPanel, panel);
            return true;
        }

        const string PickaxeByCaveName = "PickaxeByCave";
        const string PickaxeByLevelLabel = "Picareta: compra";
        const string PickaxeByCaveLabel = "Picareta: caverna";

        /// <summary>
        /// D-201: o botão "Picareta" no menu Caverna do DBG (antes do Fechar), ligado ao <see cref="CalculationsPanel"/>,
        /// que já guarda a escolha e escreve o aviso. Troca a Picareta pela compra (GDD v7) pela da caverna (D-133).
        /// </summary>
        static bool EnsurePickaxeByCave(DebugPanel debugPanel, CalculationsPanel panel)
        {
            var grid = debugPanel.transform.Find("CaveMenu/Content/Buttons");
            if (grid == null) return false;
            bool changed = false;

            var button = grid.Find(PickaxeByCaveName);
            if (button == null)
            {
                button = DebugPanelSetup.CreateButton(grid, PickaxeByCaveName, PickaxeByLevelLabel).transform;
                var close = grid.Find("Close");
                if (close != null) button.SetSiblingIndex(close.GetSiblingIndex());
                changed = true;
            }

            var so = new SerializedObject(panel);
            changed |= SetIfDifferent(so.FindProperty("pickaxeByCave"), button.GetComponent<Button>());
            changed |= SetIfEmpty(so.FindProperty("pickaxeByLevelLabel"), PickaxeByLevelLabel);
            changed |= SetIfEmpty(so.FindProperty("pickaxeByCaveLabel"), PickaxeByCaveLabel);
            changed |= SetIfEmpty(so.FindProperty("pickaxeByCaveChangedFormat"), " · Picareta pela caverna");
            so.ApplyModifiedPropertiesWithoutUndo();

            if (changed) Debug.Log("[Setup] Painel de debug: botão \"Picareta\" (compra ⇄ caverna) no menu Caverna (D-201).");
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

        /// <summary>O seletor do DBG → Cálculos passa pelos balanços do GDD v7 (o padrão) e do v5. Devolve se mudou.</summary>
        static bool EnsureBalances()
        {
            var runner = Object.FindAnyObjectByType<MiningRunner>();
            if (runner == null) return false;

            var so = new SerializedObject(runner);
            var balances = so.FindProperty("balances");
            bool same = balances.arraySize == BalancePaths.Length;
            for (int i = 0; same && i < BalancePaths.Length; i++)
                same = AssetDatabase.GetAssetPath(balances.GetArrayElementAtIndex(i).objectReferenceValue) == BalancePaths[i];
            if (same) return false;

            balances.arraySize = BalancePaths.Length;
            for (int i = 0; i < BalancePaths.Length; i++)
                balances.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(BalancePaths[i]);
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Setup] Balanços do DBG → Cálculos no MiningRunner: GDD v7 (padrão) e GDD v5 (E3b-3).");
            return true;
        }
    }
}
