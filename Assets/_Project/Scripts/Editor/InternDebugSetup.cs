using MinersIdle.Game;
using MinersIdle.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="ProjectSetup"/> para a E1 do M7, com a cena Game aberta: o menu DBG → Estagiário
    /// (<see cref="InternDebugPanel"/>), com os botões e o resumo do estagiário. O menu em si (fundo, grade, Fechar) vem do
    /// <see cref="DebugPanelSetup"/>. Só cria o que falta; os textos ficam na cena.
    /// </summary>
    static class InternDebugSetup
    {
        /// <summary>Os botões do menu, na ordem da grade de 3 colunas (o Fechar vai por último).</summary>
        static readonly (string name, string field, string label)[] Buttons =
        {
            ("Hire", "hire", "Contratar"),
            ("Fire", "fire", "Demitir"),
            ("GrantInternLevels", "grantLevels", "+1 nv melhorias"),
            ("Train", "train", "Treinar +1"),
            ("Promote", "promote", "Promoção"),
        };

        const string SummaryFormat = "Estagiário: {0} · dano {1} · intervalo {2} · golpes {3}\n" +
                                     "Picareta {4} · Velocidade {5} · Mineração {6} (offline ×{9})\n" +
                                     "Treinar {7} · Promoção {8}";

        static readonly Color TextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);

        public static bool EnsureInScene()
        {
            var debugPanel = Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include);
            var menu = debugPanel != null ? debugPanel.transform.Find(DebugPanelSetup.InternMenuName) : null;
            if (menu == null || menu.GetComponent<InternDebugPanel>() != null) return false;

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

            var panel = menu.gameObject.AddComponent<InternDebugPanel>();
            GrayboxSetup.SetReferences(panel, references.ToArray());
            var so = new UnityEditor.SerializedObject(panel);
            so.FindProperty("summaryFormat").stringValue = SummaryFormat;
            so.FindProperty("hiredLabel").stringValue = "contratado";
            so.FindProperty("notHiredLabel").stringValue = "não contratado";
            so.FindProperty("unavailableLabel").stringValue = "Sem estagiário neste balanço (só no GDD v7).";
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[Setup] Painel de debug: menu \"Estagiário\" com contratar, níveis, Treinar e Promoção (M7, E1).");
            return true;
        }
    }
}
