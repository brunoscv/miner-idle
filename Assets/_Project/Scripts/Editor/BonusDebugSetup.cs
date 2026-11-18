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
    /// (<see cref="BonusDebugPanel"/>), com o Limpar e a lista dos bônus ativos no
    /// alto. O menu em si (fundo, grade, Fechar) vem do <see cref="DebugPanelSetup"/>. Só cria o que falta; os textos ficam
    /// na cena.
    /// </summary>
    static class BonusDebugSetup
    {
        /// <summary>Os botões dos marcos do combo, que saíram no M9g (D-311): o setup os tira da cena.</summary>
        static readonly string[] OldComboButtons = { "Combo10", "Combo30", "Combo50", "Combo100" };

        /// <summary>A primeira linha do alto, sem o combo (M9g); antes, "Combo ×{0} · {1} · dano +{2}".</summary>
        const string HeaderFormat = "Dano +{0}";
        const string NoneLabel = "Nenhum bônus ativo. Use os atalhos.";

        const string ClearName = "ClearBonuses";
        const string ClearLabel = "Limpar bônus";

        static readonly Color TextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);

        public static bool EnsureInScene()
        {
            var debugPanel = Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include);
            var menu = debugPanel != null ? debugPanel.transform.Find(DebugPanelSetup.BonusMenuName) : null;
            if (menu == null) return false;
            var current = menu.GetComponent<BonusDebugPanel>();
            if (current != null)
                return RemoveCombo(menu, current) | EnsureRare(menu, current) | EnsureAd(menu, current)
                       | EnsureRareV8(menu, current) | EnsureAdsV8(menu, current);

            var grid = menu.Find("Content/Buttons");
            var clearButton = grid.Find(ClearName);
            var clear = clearButton != null ? clearButton.GetComponent<Button>() : DebugPanelSetup.CreateButton(grid, ClearName, ClearLabel);
            grid.Find("Close")?.SetAsLastSibling();

            // O dano a mais e os bônus ativos no alto do menu, acima da grade.
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
            so.FindProperty("headerFormat").stringValue = HeaderFormat;
            so.FindProperty("noneLabel").stringValue = NoneLabel;
            so.FindProperty("separator").stringValue = " · ";
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[Setup] Painel de debug: menu \"Bônus\" com Limpar bônus (M9b, E1).");
            EnsureRare(menu, panel);
            EnsureAd(menu, panel);
            EnsureRareV8(menu, panel);
            EnsureAdsV8(menu, panel);
            return true;
        }

        /// <summary>
        /// M9g (D-311): saem os botões dos marcos do combo, e o alto deixa de mostrar o combo e o acúmulo. Devolve se mudou.
        /// </summary>
        static bool RemoveCombo(Transform menu, BonusDebugPanel panel)
        {
            bool changed = false;
            var grid = menu.Find("Content/Buttons");
            foreach (var name in OldComboButtons)
            {
                var button = grid != null ? grid.Find(name) : null;
                if (button == null) continue;
                Object.DestroyImmediate(button.gameObject);
                changed = true;
            }
            var so = new SerializedObject(panel);
            var header = so.FindProperty("headerFormat");
            if (header.stringValue != HeaderFormat)
            {
                header.stringValue = HeaderFormat;
                so.FindProperty("noneLabel").stringValue = NoneLabel;
                so.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }
            if (changed) Debug.Log("[Setup] DBG → Bônus: saem os atalhos do combo (M9g, D-311).");
            return changed;
        }

        /// <summary>
        /// M9e E2: um botão por anúncio do Boost (direto, sem o modal) e Anúncios curtos, antes do Fechar (o menu cresce
        /// para 8 fileiras); no alto, cada anúncio ativo e o teto do offline de agora.
        /// </summary>
        static bool EnsureAdsV8(Transform menu, BonusDebugPanel panel)
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
            var buttons = new[]
            {
                Ensure("AdShort", "Anúncio curto"), Ensure("AdMedium", "Anúncio médio"),
                Ensure("AdBig", "Anúncio grande"), Ensure("AdRareTier2", "Anúncio tier 2"),
            };
            var shortAds = Ensure("ShortAds", "Anúncios curtos: não");
            grid.Find("Close")?.SetAsLastSibling();

            var content = (RectTransform)menu.Find("Content");
            if (content != null && !Mathf.Approximately(content.sizeDelta.y, DebugPanelSetup.BonusMenuHeight))
            {
                content.sizeDelta = new Vector2(content.sizeDelta.x, DebugPanelSetup.BonusMenuHeight);
                changed = true;
            }

            var so = new SerializedObject(panel);
            if (so.FindProperty("shortAds").objectReferenceValue != shortAds)
            {
                var list = so.FindProperty("ads");
                list.arraySize = buttons.Length;
                for (int i = 0; i < buttons.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
                so.FindProperty("shortAds").objectReferenceValue = shortAds;
                so.FindProperty("debugAdDivisor").doubleValue = 60;
                so.FindProperty("shortAdsFormat").stringValue = "Anúncios curtos: {0}";
                var labels = so.FindProperty("adLabels");
                string[] names = { "Curto", "Médio", "Grande", "Tier 2" };
                labels.arraySize = names.Length;
                for (int i = 0; i < names.Length; i++) labels.GetArrayElementAtIndex(i).stringValue = names[i];
                so.FindProperty("adOfferFormat").stringValue = "{0} {1}, {2}";
                so.FindProperty("offlineFormat").stringValue = "Offline se fechar agora: até {0}";
                so.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }
            if (changed) Debug.Log("[Setup] Painel de debug: no menu Bônus, os anúncios curto, médio, grande e tier 2 e Anúncios curtos (M9e E2).");
            return changed;
        }

        /// <summary>
        /// M9e E1 (D-285): a carta Dano de Crítico, Raro agora, Contador 5 s e Tier 2, antes do Fechar (o menu cresce para 6
        /// fileiras); os nomes das cartas sem número (o efeito vem do bônus: ×2 no tier 1, ×4 no 2) e o contador no alto.
        /// </summary>
        static bool EnsureRareV8(Transform menu, BonusDebugPanel panel)
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
            var critDamage = Ensure("CardCritDamage", "Carta Dano Crít.");
            var now = Ensure("RareNow", "Raro agora");
            var counter = Ensure("RareCounter", "Contador: 60 s");
            var tier2 = Ensure("RareTier2", "Tier 2: não");
            grid.Find("Close")?.SetAsLastSibling();

            var content = (RectTransform)menu.Find("Content");
            if (content != null && !Mathf.Approximately(content.sizeDelta.y, DebugPanelSetup.BonusMenuHeight))
            {
                content.sizeDelta = new Vector2(content.sizeDelta.x, DebugPanelSetup.BonusMenuHeight);
                changed = true;
            }

            var so = new SerializedObject(panel);
            if (so.FindProperty("rareNow").objectReferenceValue != now)
            {
                so.FindProperty("rareNow").objectReferenceValue = now;
                so.FindProperty("rareCounter").objectReferenceValue = counter;
                so.FindProperty("rareTier2").objectReferenceValue = tier2;
                var cards = so.FindProperty("cards");
                cards.arraySize = 5;
                cards.GetArrayElementAtIndex(4).objectReferenceValue = critDamage;
                var labels = so.FindProperty("cardLabels");
                labels.arraySize = CardLabelsV8.Length;
                for (int i = 0; i < CardLabelsV8.Length; i++) labels.GetArrayElementAtIndex(i).stringValue = CardLabelsV8[i];
                so.FindProperty("rareCardFormat").stringValue = "{0} {2}, {1}";
                so.FindProperty("debugIntervalSeconds").doubleValue = 5;
                so.FindProperty("counterButtonFormat").stringValue = "Contador: {0}";
                so.FindProperty("tier2ButtonFormat").stringValue = "Tier 2: {0}";
                so.FindProperty("yesLabel").stringValue = "sim";
                so.FindProperty("noLabel").stringValue = "não";
                so.FindProperty("counterFormat").stringValue = "Raro em {0} (a cada {1}) · tier {2}";
                so.FindProperty("counterLockedFormat").stringValue = "Raro: só a partir do andar {0} · tier {1}";
                so.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }
            if (changed) Debug.Log("[Setup] Painel de debug: no menu Bônus, Raro agora, Contador 5 s, Tier 2 e a carta Dano de Crítico (M9e E1, D-285).");
            return changed;
        }

        /// <summary>Os nomes das cartas no alto do menu, na ordem do enum RareCard; o efeito vem do bônus ativo.</summary>
        static readonly string[] CardLabelsV8 = { "Dano", "Rápido", "Lascas", "Crítico", "Dano de crítico" };

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
