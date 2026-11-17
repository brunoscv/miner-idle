using MinersIdle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// DBG → Caverna → "Transição: B" (M9c E0, D-267; a B é a escolhida, D-268): troca a transição de andar entre as três (C, B, B com mosaico, A)
    /// e "nenhuma", para o Bruno escolher vendo. Vale para a próxima descida e não fica salvo. Para ver: DBG → Caverna →
    /// Próxima caverna. Está dentro do painel de debug: no release some junto com ele (fica a transição da cena).
    /// </summary>
    public sealed class FloorTransitionDebug : MonoBehaviour
    {
        static readonly FloorTransitionStyle[] Order =
        {
            FloorTransitionStyle.CaveFlight, FloorTransitionStyle.CameraDrop, FloorTransitionStyle.CameraDropMosaic,
            FloorTransitionStyle.BlurCard, FloorTransitionStyle.None,
        };

        [SerializeField] Button toggle;
        [SerializeField] FloorTransition transition;
        [Tooltip("{0} = a transição em uso.")]
        [SerializeField] string labelFormat = "Transição: {0}";
        [SerializeField] string caveFlightName = "C";
        [SerializeField] string cameraDropName = "B";
        [SerializeField] string cameraDropMosaicName = "B mosaico";
        [SerializeField] string blurCardName = "A";
        [SerializeField] string noneName = "nenhuma";
        [Header("Transição curta (M9c E2)")]
        [SerializeField] Button shortToggle;
        [Tooltip("{0} = sim ou não.")]
        [SerializeField] string shortFormat = "Transição curta: {0}";
        [SerializeField] string yesName = "sim";
        [SerializeField] string noName = "não";

        TMP_Text _label, _shortLabel;

        void Awake()
        {
            if (toggle != null) _label = toggle.GetComponentInChildren<TMP_Text>();
            if (shortToggle != null) _shortLabel = shortToggle.GetComponentInChildren<TMP_Text>();
        }

        void OnEnable()
        {
            if (toggle != null) toggle.onClick.AddListener(Next);
            if (shortToggle != null) shortToggle.onClick.AddListener(ToggleShort);
            Refresh();
        }

        void OnDisable()
        {
            if (toggle != null) toggle.onClick.RemoveListener(Next);
            if (shortToggle != null) shortToggle.onClick.RemoveListener(ToggleShort);
        }

        /// <summary>Liga e desliga a transição curta (0,3 s).</summary>
        public void ToggleShort()
        {
            if (transition == null) return;
            transition.IsShort = !transition.IsShort;
            Refresh();
        }

        /// <summary>A próxima transição da lista (C → B → B mosaico → A → nenhuma → C…).</summary>
        public void Next()
        {
            if (transition == null) return;
            int index = System.Array.IndexOf(Order, transition.Style);
            transition.Style = Order[(index + 1) % Order.Length];
            Refresh();
        }

        void Refresh()
        {
            if (transition == null) return;
            if (_label != null) _label.text = string.Format(labelFormat, NameOf(transition.Style));
            if (_shortLabel != null) _shortLabel.text = string.Format(shortFormat, transition.IsShort ? yesName : noName);
        }

        string NameOf(FloorTransitionStyle style)
        {
            switch (style)
            {
                case FloorTransitionStyle.CaveFlight: return caveFlightName;
                case FloorTransitionStyle.CameraDrop: return cameraDropName;
                case FloorTransitionStyle.CameraDropMosaic: return cameraDropMosaicName;
                case FloorTransitionStyle.BlurCard: return blurCardName;
                default: return noneName;
            }
        }
    }
}
