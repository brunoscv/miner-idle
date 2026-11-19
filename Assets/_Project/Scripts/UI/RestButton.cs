using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// O botão Descansar da barra de baixo (Marco 8b, E3, D-152), no lado esquerdo (o Próximo Andar fica no direito).
    /// Mostra embaixo do nome as lascas vermelhas que o jogador leva agora (D-156); ao tocar, abre o modal
    /// (<see cref="RestScreen"/>); quem descansa é ele.
    ///
    /// M9i E1 (#15): antes do andar do descanso o botão já aparece, <b>apagado</b>, com um cadeado no lugar do cristal e
    /// "ANDAR 10" no lugar das lascas; o toque mostra um aviso curto. Some só quando o balanço não tem descanso ou a run
    /// já descansou. Fica num objeto sempre ativo; o que some é o botão. Atualiza por evento, nunca por quadro (o
    /// <c>Update</c> só conta o tempo do aviso).
    /// </summary>
    public sealed class RestButton : MiningViewBase
    {
        public enum ButtonState { Hidden, Locked, Open }

        [SerializeField] FeedbackTexts texts;
        [SerializeField] RestScreen restScreen;
        [SerializeField] Button button;
        [Tooltip("O botão na tela (ligado quando dá para descansar ou quando está trancado).")]
        [SerializeField] GameObject visual;
        [SerializeField] TMP_Text label;
        [Tooltip("As lascas vermelhas que o jogador leva, embaixo do nome (trancado: o andar do descanso).")]
        [SerializeField] TMP_Text count;

        [Header("Trancado (M9i E1)")]
        [SerializeField] Image background;
        [SerializeField] Image icon;
        [SerializeField] Sprite lockSprite;
        [SerializeField] Color lockedTint = new Color(0.55f, 0.55f, 0.55f, 1f);
        [SerializeField] Color lockedTextColor = new Color32(0xBE, 0xB4, 0xAA, 0xFF);
        [Tooltip("O aviso ao tocar no botão trancado; começa escondido.")]
        [SerializeField] GameObject notice;
        [SerializeField] TMP_Text noticeText;
        [SerializeField, Min(0.1f)] float noticeSeconds = 1.6f;

        double _shownCount = -1;
        ButtonState _state = (ButtonState)(-1);
        Sprite _openIcon;
        Color _openIconColor, _openLabelColor, _openCountColor;
        float _noticeLeft;

        public ButtonState State => _state;

        /// <summary>Se o botão está na tela agora (para testes): aberto ou trancado.</summary>
        public bool IsShown => visual.activeSelf;

        /// <summary>Se está na tela apagado, esperando o andar do descanso (M9i E1).</summary>
        public bool IsLocked => _state == ButtonState.Locked;

        /// <summary>Se o aviso do botão trancado está na tela (para testes).</summary>
        public bool IsNoticeShown => notice != null && notice.activeSelf;

        public string CountText => count.text;

        void Awake()
        {
            label.text = texts.RestLabel;
            _openLabelColor = label.color;
            _openCountColor = count.color;
            if (icon != null)
            {
                _openIcon = icon.sprite;
                _openIconColor = icon.color;
            }
            if (notice != null) notice.SetActive(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            button.onClick.AddListener(OnClick);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            button.onClick.RemoveListener(OnClick);
            HideNotice();
        }

        protected override void Subscribe(MiningSimulation simulation)
        {
            simulation.CaveChanged += OnCaveChanged;
            simulation.PermanentsChanged += Refresh;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.CaveChanged -= OnCaveChanged;
            simulation.PermanentsChanged -= Refresh;
        }

        protected override void Rebuild(MiningSimulation simulation)
        {
            _shownCount = -1;
            _state = (ButtonState)(-1);
            Refresh();
        }

        void OnCaveChanged(int cave) => Refresh();

        void OnClick()
        {
            if (_state == ButtonState.Locked) ShowNotice();
            else if (_state == ButtonState.Open) restScreen.Open();
        }

        void ShowNotice()
        {
            if (notice == null) return;
            _noticeLeft = noticeSeconds;
            notice.SetActive(true);
        }

        void HideNotice()
        {
            _noticeLeft = 0f;
            if (notice != null) notice.SetActive(false);
        }

        void Update()
        {
            if (_noticeLeft <= 0f) return;
            _noticeLeft -= Time.unscaledDeltaTime;
            if (_noticeLeft <= 0f) HideNotice();
        }

        void Refresh()
        {
            var simulation = Simulation;
            var state = simulation == null ? ButtonState.Hidden
                      : simulation.CanRest ? ButtonState.Open
                      : simulation.IsRestLocked ? ButtonState.Locked
                      : ButtonState.Hidden;
            if (state != _state)
            {
                _state = state;
                _shownCount = -1;
                bool show = state != ButtonState.Hidden;
                if (visual.activeSelf != show) visual.SetActive(show);
                if (state != ButtonState.Locked) HideNotice();
                if (show) Paint(state == ButtonState.Locked, simulation.RestLevel);
            }
            if (state != ButtonState.Open) return;

            double pending = simulation.PendingRedShards;
            if (pending == _shownCount) return;
            _shownCount = pending;
            count.text = NumberFormatter.FormatShards(pending);
        }

        /// <summary>Apagado (cinza, cadeado, o andar que falta) ou aberto (madeira, o cristal vermelho, as lascas).</summary>
        void Paint(bool locked, int restLevel)
        {
            if (background != null) background.color = locked ? lockedTint : Color.white;
            if (icon != null)
            {
                icon.sprite = locked && lockSprite != null ? lockSprite : _openIcon;
                icon.color = locked ? Color.white : _openIconColor;
            }
            label.color = locked ? lockedTextColor : _openLabelColor;
            count.color = locked ? lockedTextColor : _openCountColor;
            if (locked)
            {
                count.text = string.Format(texts.RestLockedFormat, restLevel);
                if (noticeText != null) noticeText.text = string.Format(texts.RestLockedNotice, restLevel);
            }
        }
    }
}
