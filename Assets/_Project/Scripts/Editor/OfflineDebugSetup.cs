using MinersIdle.Game;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="ProjectSetup"/> para a E1 do M9, com a cena Game aberta: o menu DBG → Offline
    /// (<see cref="OfflineDebugPanel"/>), com os botões de simular o jogo fechado, o de voltar o relógio e a conta do último
    /// offline. O menu em si (fundo, grade, Fechar) vem do <see cref="DebugPanelSetup"/>. Só cria o que falta; os textos
    /// ficam na cena.
    /// </summary>
    static class OfflineDebugSetup
    {
        /// <summary>Os botões de simular, na ordem de <see cref="OfflineDebugPanel.SimulatedSeconds"/>.</summary>
        static readonly (string name, string label)[] SimulateButtons =
        {
            ("Away1Min", "Fora 1 min"),
            ("Away10Min", "Fora 10 min"),
            ("Away1H", "Fora 1 h"),
            ("Away6H", "Fora 6 h"),
            ("Away8H", "Fora 8 h"),
        };

        const string ClockBackName = "ClockBack";
        const string ClockBackLabel = "Relógio −1 h";

        const string SummaryFormat = "Último offline: {0} fora, contou {1}{7}\n" +
                                     "{2} cristais · {3} golpe(s) e {4} por cristal\n" +
                                     "{5} por cristal · trouxe {6} lascas";

        static readonly Color TextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);

        public static bool EnsureInScene()
        {
            var debugPanel = Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include);
            var menu = debugPanel != null ? debugPanel.transform.Find(DebugPanelSetup.OfflineMenuName) : null;
            if (menu == null || menu.GetComponent<OfflineDebugPanel>() != null) return false;

            var grid = menu.Find("Content/Buttons");
            var simulate = new Button[SimulateButtons.Length];
            for (int i = 0; i < SimulateButtons.Length; i++)
            {
                var (name, label) = SimulateButtons[i];
                var existing = grid.Find(name);
                simulate[i] = existing != null ? existing.GetComponent<Button>() : DebugPanelSetup.CreateButton(grid, name, label);
            }
            var clockBackButton = grid.Find(ClockBackName);
            var clockBack = clockBackButton != null
                ? clockBackButton.GetComponent<Button>()
                : DebugPanelSetup.CreateButton(grid, ClockBackName, ClockBackLabel);
            grid.Find("Close")?.SetAsLastSibling();

            // A conta do último offline no alto do menu, acima da grade.
            var content = menu.Find("Content");
            var summary = GrayboxSetup.CreateHudText(content, "Summary", "", 7f, TextColor, new Vector2(0f, 1f), new Vector2(1f, 1f));
            var rect = (RectTransform)summary.transform;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0f, -DebugPanelSetup.DayTextHeight);
            rect.offsetMax = Vector2.zero;
            summary.alignment = TextAlignmentOptions.TopLeft;
            summary.margin = new Vector4(4f, 3f, 4f, 0f);

            var panel = menu.gameObject.AddComponent<OfflineDebugPanel>();
            GrayboxSetup.SetReferences(panel,
                ("runner", Object.FindAnyObjectByType<MiningRunner>()),
                ("clockBack", clockBack),
                ("summary", summary));

            var so = new SerializedObject(panel);
            var buttons = so.FindProperty("simulate");
            buttons.arraySize = simulate.Length;
            for (int i = 0; i < simulate.Length; i++) buttons.GetArrayElementAtIndex(i).objectReferenceValue = simulate[i];
            so.FindProperty("noneLabel").stringValue = "Nenhum offline ainda. Contrate o estagiário e simule um tempo fora.";
            so.FindProperty("summaryFormat").stringValue = SummaryFormat;
            so.FindProperty("notHiredFormat").stringValue = "Último offline: {0} fora, sem estagiário: nada (D-139).";
            so.FindProperty("cappedLabel").stringValue = " (teto)";
            so.FindProperty("clockBackLabel").stringValue = " (relógio voltou: nada)";
            so.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log("[Setup] Painel de debug: menu \"Offline\" com simular 1 min…8 h fora e voltar o relógio (M9, E1).");
            return true;
        }
    }
}
