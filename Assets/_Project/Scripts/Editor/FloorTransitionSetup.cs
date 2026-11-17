using MinersIdle.Game;
using MinersIdle.Presentation;
using MinersIdle.UI;
using UnityEditor;
using UnityEngine;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="ProjectSetup"/>, com a cena Game aberta (M9c E0, D-267): a transição de andar
    /// (<see cref="FloorTransition"/>, no objeto da faixa da caverna nova, com o tremor e o som da B) e o botão
    /// "Transição: B" no menu DBG → Caverna
    /// (<see cref="FloorTransitionDebug"/>). Só cria o que falta.
    /// </summary>
    static class FloorTransitionSetup
    {
        const string ButtonName = "FloorTransition";

        public static bool EnsureInScene()
        {
            bool changed = false;
            var banner = Object.FindAnyObjectByType<CaveBanner>(FindObjectsInactive.Include);
            var stage = Object.FindAnyObjectByType<DescentStage>(FindObjectsInactive.Include);
            var runner = Object.FindAnyObjectByType<MiningRunner>(FindObjectsInactive.Include);
            if (banner == null || stage == null || runner == null) return false;

            var transition = banner.GetComponent<FloorTransition>();
            if (transition == null)
            {
                transition = banner.gameObject.AddComponent<FloorTransition>();
                GrayboxSetup.SetReferences(transition, ("runner", runner), ("stage", stage), ("banner", banner));
                Debug.Log("[Setup] Transição de andar (B, a câmera desce; D-268) criada na cena Game (M9c E0).");
                changed = true;
            }

            // A B (M9c E0): o tremor e o som do cartão, também na transição que já estava na cena.
            var serialized = new SerializedObject(transition);
            var shakeField = serialized.FindProperty("screenShake");
            var soundField = serialized.FindProperty("sound");
            if (shakeField.objectReferenceValue == null || soundField.objectReferenceValue == null)
            {
                var shake = Object.FindAnyObjectByType<ScreenShake>(FindObjectsInactive.Include);
                var sound = Object.FindAnyObjectByType<SoundBoard>(FindObjectsInactive.Include);
                if (shake != null || sound != null)
                {
                    if (shakeField.objectReferenceValue == null) shakeField.objectReferenceValue = shake;
                    if (soundField.objectReferenceValue == null) soundField.objectReferenceValue = sound;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log("[Setup] Transição de andar: tremor e som da B ligados (M9c E0).");
                    changed = true;
                }
            }

            var debugPanel = Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include);
            var menu = debugPanel != null ? debugPanel.transform.Find("CaveMenu") : null;
            var grid = menu != null ? menu.Find("Content/Buttons") : null;
            if (grid == null) return changed;
            if (menu.GetComponent<FloorTransitionDebug>() != null) return changed | EnsureShortButton(menu, grid);

            var existing = grid.Find(ButtonName);
            var button = existing != null ? existing.GetComponent<UnityEngine.UI.Button>()
                                          : DebugPanelSetup.CreateButton(grid, ButtonName, "Transição: B");
            grid.Find("Close")?.SetAsLastSibling();

            var debug = menu.gameObject.AddComponent<FloorTransitionDebug>();
            GrayboxSetup.SetReferences(debug, ("toggle", button), ("transition", transition));
            Debug.Log("[Setup] Painel de debug: \"Transição\" no menu Caverna (M9c E0).");
            EnsureShortButton(menu, grid);
            return true;
        }

        const string ShortButtonName = "ShortTransition";

        /// <summary>M9c E2: DBG → Andar → "Transição curta: não", ao lado da escolha da transição.</summary>
        static bool EnsureShortButton(Transform menu, Transform grid)
        {
            var debug = menu.GetComponent<FloorTransitionDebug>();
            var so = new SerializedObject(debug);
            var field = so.FindProperty("shortToggle");
            if (field.objectReferenceValue != null) return false;
            var existing = grid.Find(ShortButtonName);
            var button = existing != null ? existing.GetComponent<UnityEngine.UI.Button>()
                                          : DebugPanelSetup.CreateButton(grid, ShortButtonName, "Transição curta: não");
            grid.Find("Close")?.SetAsLastSibling();
            field.objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Setup] Painel de debug: \"Transição curta\" no menu Andar (M9c E2).");
            return true;
        }
    }
}
