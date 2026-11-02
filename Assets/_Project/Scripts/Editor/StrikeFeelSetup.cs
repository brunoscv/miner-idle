using UnityEditor;
using UnityEngine;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Golpe fluido (D-124, D-125): o botão "Golpe" do DBG → Câmera, que passa pelas opções para comparar no aparelho
    /// (como era, só os efeitos, A1 e A2). Os rótulos ficam na cena; o painel lê pelo índice.
    /// </summary>
    static class StrikeFeelSetup
    {
        const string StrikeButton = "Strike";

        /// <summary>Na ordem do <c>DescentCameraPanel.StrikeLooks</c>; cabem nos 48 px do botão.</summary>
        static readonly string[] StrikeLabels = { "Golpe hoje", "Só efeitos", "Golpe A1", "Golpe A2" };

        /// <summary>O jogo abre em A2 com os efeitos (D-124, escolha do Bruno em 2026-11-02; D-125).</summary>
        const int DefaultLook = 3;

        public static bool EnsureInScene()
        {
            var panel = Object.FindAnyObjectByType<MinersIdle.UI.DescentCameraPanel>(FindObjectsInactive.Include);
            var buttons = panel != null ? panel.transform.Find("Content/Buttons") : null;
            if (buttons == null) return false;

            bool changed = false;
            var button = buttons.Find(StrikeButton);
            if (button == null)
            {
                button = DebugPanelSetup.CreateButton(buttons, StrikeButton, StrikeLabels[DefaultLook]).transform;
                GrayboxSetup.SetReferences(panel, ("strike", button.GetComponent<UnityEngine.UI.Button>()));
                Debug.Log("[Setup] Botão \"Golpe\" no DBG → Câmera (golpe fluido, D-124/D-125).");
                changed = true;
            }

            var so = new SerializedObject(panel);
            var labels = so.FindProperty("strikeLabels");
            bool same = labels.arraySize == StrikeLabels.Length;
            for (int i = 0; same && i < StrikeLabels.Length; i++) same = labels.GetArrayElementAtIndex(i).stringValue == StrikeLabels[i];
            if (!same)
            {
                labels.arraySize = StrikeLabels.Length;
                for (int i = 0; i < StrikeLabels.Length; i++) labels.GetArrayElementAtIndex(i).stringValue = StrikeLabels[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }

            // O botão mostra o padrão até o painel abrir (aí mostra o atual).
            var text = button.GetComponentInChildren<TMPro.TMP_Text>();
            if (text.text == StrikeLabels[DefaultLook]) return changed;
            text.text = StrikeLabels[DefaultLook];
            EditorUtility.SetDirty(text);
            return true;
        }
    }
}
