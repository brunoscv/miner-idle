using MinersIdle.Presentation;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Marco 9h (Bloco G): a câmera do GDD v8.2 (D-245) vira a da cena. A descida que ainda está na F1 (D-69) passa para a do
    /// v8; uma câmera mexida à mão no Inspector (nem F1 nem v8) fica. O DBG → Câmera ganha o botão "Câmera v8 / F1" para
    /// comparar, e o "Padrão" passa a dizer v8.
    /// </summary>
    internal static class CameraV8Setup
    {
        const string PresetButton = "Preset";

        /// <summary>Na ordem de <c>DescentCameraPanel.Presets</c> (v8, F1) e, no fim, o de uma câmera fora deles.</summary>
        static readonly string[] PresetLabels = { "Câmera v8", "Câmera F1", "Câmera ?" };

        /// <summary>Na cena Game (já aberta). Devolve se mudou a cena.</summary>
        public static bool EnsureInScene()
        {
            bool changed = EnsureStageFraming();
            changed |= DescentSetup.EnsureCameraCycleButton(PresetButton, "preset", "presetLabels", PresetLabels, 0,
                                                            "Botão \"Câmera v8 / F1\" no DBG → Câmera (M9h, D-245).");
            changed |= RenameResetButton();
            // O botão novo nasce depois da fonte do jogo (UiSkinSetup): leva a fonte pixel aqui mesmo.
            var panel = Object.FindAnyObjectByType<DescentCameraPanel>(FindObjectsInactive.Include);
            changed |= panel != null && UiSkinSetup.ApplyGameFont(panel.transform.Find("Content/Buttons/" + PresetButton)) > 0;
            return changed;
        }

        static bool EnsureStageFraming()
        {
            var stage = Object.FindAnyObjectByType<DescentStage>();
            if (stage == null) return false;

            var so = new SerializedObject(stage);
            var framing = so.FindProperty("framing");
            var current = new DescentFraming
            {
                height = framing.FindPropertyRelative("height").floatValue,
                tilt = framing.FindPropertyRelative("tilt").floatValue,
                distance = framing.FindPropertyRelative("distance").floatValue,
                fieldOfView = framing.FindPropertyRelative("fieldOfView").floatValue,
            };
            if (!current.SameView(DescentFraming.F1)) return false;

            var v8 = DescentFraming.GddV8;
            framing.FindPropertyRelative("height").floatValue = v8.height;
            framing.FindPropertyRelative("tilt").floatValue = v8.tilt;
            framing.FindPropertyRelative("distance").floatValue = v8.distance;
            framing.FindPropertyRelative("fieldOfView").floatValue = v8.fieldOfView;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Setup] Câmera da descida passa para a do GDD v8 (60°, 5,5 m, 3 m, 60°; M9h, D-245).");
            return true;
        }

        /// <summary>O botão do DBG dizia "Padrão F1"; agora o padrão é a câmera do v8.</summary>
        static bool RenameResetButton()
        {
            var panel = Object.FindAnyObjectByType<DescentCameraPanel>(FindObjectsInactive.Include);
            var label = panel != null ? panel.transform.Find("Content/Buttons/ResetFraming/Label") : null;
            if (label == null || !label.TryGetComponent<TMP_Text>(out var text) || text.text == "Padrão v8") return false;
            text.text = "Padrão v8";
            EditorUtility.SetDirty(text);
            return true;
        }
    }
}
