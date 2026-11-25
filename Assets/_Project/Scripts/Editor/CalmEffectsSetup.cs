using MinersIdle.Presentation;
using MinersIdle.UI;
using UnityEditor;
using UnityEngine;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="ProjectSetup"/>, com a cena Game aberta: o botão "Efeitos: limpos" no menu DBG → Efeitos
    /// (<see cref="DebugCalmEffects"/>, M9i #24, D-346). Só cria o que falta. Os efeitos limpos em si não precisam de setup:
    /// são o padrão do <see cref="MiningFeedbackView"/>.
    /// </summary>
    static class CalmEffectsSetup
    {
        const string ButtonName = "CalmEffects";

        public static bool EnsureInScene()
        {
            var debugPanel = Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include);
            var menu = debugPanel != null ? debugPanel.transform.Find("EffectsMenu") : null;
            var grid = menu != null ? menu.Find("Content/Buttons") : null;
            var feedback = Object.FindAnyObjectByType<MiningFeedbackView>(FindObjectsInactive.Include);
            if (grid == null || feedback == null || menu.GetComponent<DebugCalmEffects>() != null) return false;

            var existing = grid.Find(ButtonName);
            var button = existing != null ? existing.GetComponent<UnityEngine.UI.Button>()
                                          : DebugPanelSetup.CreateButton(grid, ButtonName, "Efeitos: limpos");
            grid.Find("Close")?.SetAsLastSibling();

            var toggle = menu.gameObject.AddComponent<DebugCalmEffects>();
            GrayboxSetup.SetReferences(toggle, ("toggle", button), ("feedback", feedback));
            Debug.Log("[Setup] Painel de debug: \"Efeitos: limpos/antigos\" no menu Efeitos (M9i #24, D-346).");
            return true;
        }
    }
}
