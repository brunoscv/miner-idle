using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// O botão AD da coluna da direita (M9b E5; D-188, D-230): livre (madeira, ícone e "AD") abre o anúncio
    /// (<see cref="IRewardedAd"/>); apagado com o anúncio na tela; apagado com os segundos que faltam do boost no lugar do
    /// "AD", até ele acabar. A recompensa do anúncio é o boost (<see cref="MiningSimulation.GrantAdBoost"/>). Sem AD no
    /// balanço, fica apagado.
    /// </summary>
    public sealed class AdButtonView : MiningViewBase
    {
        public enum ButtonState { Off, Free, Watching, Boosted }

        [SerializeField] BuffCatalog catalog;
        [SerializeField] Button button;
        [SerializeField] Image background;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text label;
        [Tooltip("Quem mostra o anúncio: um componente que implementa IRewardedAd (hoje o SimulatedAdScreen).")]
        [SerializeField] MonoBehaviour rewardedAd;

        [Header("Aparência")]
        [SerializeField] Sprite normalSprite;
        [SerializeField] Sprite disabledSprite;
        [SerializeField] Color labelColor = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        [SerializeField] Color disabledLabelColor = new Color32(0xBE, 0xB4, 0xAA, 0xFF);
        [SerializeField, Range(0f, 1f)] float disabledIconAlpha = 0.35f;

        IRewardedAd _ad;
        ButtonState _state = (ButtonState)(-1);
        int _seconds = -1;

        public ButtonState State => _state;
        public string LabelText => label.text;

        void Awake()
        {
            _ad = rewardedAd as IRewardedAd;
            if (_ad == null) Debug.LogError("[AdButtonView] O anúncio não implementa IRewardedAd.", this);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            button.onClick.AddListener(Watch);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            button.onClick.RemoveListener(Watch);
        }

        protected override void Rebuild(MiningSimulation simulation) => Refresh();

        void Update() => Refresh();

        /// <summary>Tocou no AD livre: o anúncio abre; a recompensa é o boost.</summary>
        public void Watch()
        {
            if (Simulation == null || _ad == null || _ad.IsShowing || !Simulation.CanWatchAd) return;
            _ad.Show(OnRewarded);
            Refresh();
        }

        void OnRewarded() => Simulation?.GrantAdBoost();

        void Refresh()
        {
            var simulation = Simulation;
            var state = simulation == null || _ad == null ? ButtonState.Off
                      : _ad.IsShowing ? ButtonState.Watching
                      : simulation.HasAdBoost ? ButtonState.Boosted
                      : simulation.CanWatchAd ? ButtonState.Free
                      : ButtonState.Off;
            if (state != _state)
            {
                _state = state;
                bool free = state == ButtonState.Free;
                background.sprite = free || disabledSprite == null ? normalSprite : disabledSprite;
                button.interactable = free;
                label.color = free ? labelColor : disabledLabelColor;
                var color = icon.color;
                color.a = free ? 1f : disabledIconAlpha;
                icon.color = color;
                _seconds = -1;
                if (state != ButtonState.Boosted) label.text = catalog.AdButton;
            }
            if (state != ButtonState.Boosted) return;

            int seconds = Mathf.CeilToInt((float)simulation.AdBoostSecondsLeft);
            if (seconds == _seconds) return;
            _seconds = seconds;
            label.SetText(catalog.SecondsFormat, (float)seconds);
        }
    }
}
