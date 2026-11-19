using System;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// M9i E5 (#17): "Sair do jogo?", aberto pelo segundo Voltar do Android (<see cref="BackButtonHandler"/>). Diz o que o
    /// estagiário minera com o jogo fechado (o teto de offline de agora) ou que, sem ele, nada é minerado. SAIR grava o
    /// save e fecha o app; FICAR (ou outro Voltar) fecha o modal.
    /// </summary>
    public sealed class ExitGameScreen : MiningViewBase, IBackHandler
    {
        [SerializeField] FeedbackTexts texts;
        [SerializeField] GameObject screen;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text body;
        [SerializeField] TMP_Text saved;
        [SerializeField] Button stayButton;
        [SerializeField] TMP_Text stayLabel;
        [SerializeField] Button leaveButton;
        [SerializeField] TMP_Text leaveLabel;

        /// <summary>O que o SAIR faz depois de gravar (fecha o app; os testes trocam).</summary>
        public Action Quit = Application.Quit;

        public bool IsOpen => screen.activeSelf;
        public string BodyText => body.text;

        void Awake()
        {
            title.text = texts.ExitTitle;
            saved.text = texts.ExitSaved;
            stayLabel.text = texts.ExitStay;
            leaveLabel.text = texts.ExitLeave;
            screen.SetActive(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            stayButton.onClick.AddListener(Close);
            leaveButton.onClick.AddListener(Leave);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            stayButton.onClick.RemoveListener(Close);
            leaveButton.onClick.RemoveListener(Leave);
        }

        protected override void Rebuild(MiningSimulation simulation) => screen.SetActive(false);

        /// <summary>Abre com o que o estagiário rende fora agora.</summary>
        public void Open()
        {
            var simulation = Simulation;
            body.text = simulation != null && simulation.IsInternHired
                ? string.Format(texts.ExitInternFormat,
                                NumberFormatter.FormatDuration(simulation.OfflineMaxSeconds, trimWholeHours: true))
                : texts.ExitNoIntern;
            screen.SetActive(true);
        }

        /// <summary>FICAR.</summary>
        public void Close() => screen.SetActive(false);

        /// <summary>SAIR: grava e fecha o app.</summary>
        public void Leave()
        {
            runner.SaveNow();
            screen.SetActive(false);
            Quit?.Invoke();
        }

        /// <summary>M9i E5: com o modal aberto, o Voltar é o FICAR.</summary>
        public bool HandleBack()
        {
            if (!IsOpen) return false;
            Close();
            return true;
        }
    }
}
