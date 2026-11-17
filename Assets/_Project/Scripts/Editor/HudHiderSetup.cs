using MinersIdle.UI;
using UnityEditor;
using UnityEngine;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="ProjectSetup"/>, com a cena Game aberta: o botão "Esconder HUD" no menu DBG → Efeitos
    /// (<see cref="DebugHudHider"/>). Só cria o que falta.
    /// </summary>
    static class HudHiderSetup
    {
        const string ButtonName = "HideHud";

        public static bool EnsureInScene()
        {
            var debugPanel = Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include);
            var menu = debugPanel != null ? debugPanel.transform.Find("EffectsMenu") : null;
            var grid = menu != null ? menu.Find("Content/Buttons") : null;
            if (grid == null || menu.GetComponent<DebugHudHider>() != null) return false;

            var existing = grid.Find(ButtonName);
            var button = existing != null ? existing.GetComponent<UnityEngine.UI.Button>()
                                          : DebugPanelSetup.CreateButton(grid, ButtonName, "Esconder HUD");
            grid.Find("Close")?.SetAsLastSibling();

            var hider = menu.gameObject.AddComponent<DebugHudHider>();
            GrayboxSetup.SetReferences(hider, ("toggle", button), ("debugRoot", debugPanel.transform));
            Debug.Log("[Setup] Painel de debug: \"Esconder HUD\" no menu Efeitos.");
            return true;
        }
    }
}
