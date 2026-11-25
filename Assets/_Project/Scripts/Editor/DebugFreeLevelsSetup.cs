using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="ProjectSetup"/>, com a cena Game aberta (M9j E3): os botões "Comprar …" do painel de debug
    /// passam a dar <see cref="DebugPanel.FreeLevels"/> níveis de graça, e o rótulo diz isso ("Velocidade +10"). Só troca o
    /// rótulo que ainda começa com "Comprar": um rótulo mudado à mão fica.
    /// </summary>
    static class DebugFreeLevelsSetup
    {
        static readonly (string field, string title)[] Buttons =
        {
            ("buyPickaxe", "Picareta"), ("buySpeed", "Velocidade"), ("buyMining", "Mineração"), ("buyCritical", "Crítico"),
        };

        public static bool EnsureInScene()
        {
            var panel = Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include);
            if (panel == null) return false;

            var so = new SerializedObject(panel);
            bool changed = false;
            foreach (var (field, title) in Buttons)
            {
                var button = so.FindProperty(field)?.objectReferenceValue as Button;
                var label = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
                if (label == null || !label.text.StartsWith("Comprar")) continue;
                label.text = $"{title} +{DebugPanel.FreeLevels}";
                EditorUtility.SetDirty(label);
                changed = true;
            }
            if (changed) Debug.Log($"[Setup] Painel de debug: \"Comprar …\" vira \"… +{DebugPanel.FreeLevels}\", de graça (M9j E3).");
            return changed;
        }
    }
}
