using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// M9i E4 (D-333, D-337): o aviso de versão nova. Abre quando o save foi gravado por uma versão mais velha do app
    /// (<see cref="MiningRunner.IsSaveFromOlderApp"/>), por cima de tudo, inclusive do "Bem-vindo de volta".
    /// RECOMEÇAR apaga o save, como o Apagar save dos AJUSTES (os ajustes ficam, D-200); CONTINUAR segue com o save, que
    /// passa a levar a versão de agora e não avisa mais. DBG → Offline → "Aviso de versão" abre o aviso a qualquer hora.
    /// </summary>
    public sealed class VersionNoticeScreen : MiningViewBase
    {
        [SerializeField] FeedbackTexts texts;
        [SerializeField] GameObject screen;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text body;
        [SerializeField] TMP_Text numbers;
        [SerializeField] Button restartButton;
        [SerializeField] TMP_Text restartLabel;
        [SerializeField] Button continueButton;
        [SerializeField] TMP_Text continueLabel;
        [Tooltip("DBG → Offline → \"Aviso de versão\" (opcional).")]
        [SerializeField] Button debugOpen;

        public bool IsOpen => screen.activeSelf;
        public string NumbersText => numbers.text;

        void Awake()
        {
            title.text = texts.VersionTitle;
            body.text = texts.VersionBody;
            restartLabel.text = texts.VersionRestart;
            continueLabel.text = texts.VersionContinue;
            screen.SetActive(false); // o OnEnable (logo depois) abre, se o save carregado for de uma versão mais velha
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            restartButton.onClick.AddListener(Restart);
            continueButton.onClick.AddListener(Continue);
            if (debugOpen != null) debugOpen.onClick.AddListener(Open);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            restartButton.onClick.RemoveListener(Restart);
            continueButton.onClick.RemoveListener(Continue);
            if (debugOpen != null) debugOpen.onClick.RemoveListener(Open);
        }

        /// <summary>Uma partida nova (o save carregou, ou RECOMEÇAR): abre só com um save de versão mais velha.</summary>
        protected override void Rebuild(MiningSimulation simulation)
        {
            if (runner.IsSaveFromOlderApp) Open();
            else screen.SetActive(false);
        }

        /// <summary>Abre o aviso (o save carregado ou o atalho do DBG), com as duas versões embaixo.</summary>
        public void Open()
        {
            string saved = runner.SavedAppVersion;
            numbers.text = string.Format(texts.VersionNumbersFormat,
                                         string.IsNullOrEmpty(saved) ? texts.VersionUnknown : saved, runner.AppVersion);
            screen.SetActive(true);
        }

        /// <summary>RECOMEÇAR: o jogo do zero, como o Apagar save (a partida nova fecha o aviso).</summary>
        public void Restart()
        {
            screen.SetActive(false);
            runner.ResetProgress();
        }

        /// <summary>CONTINUAR: segue com o save, gravado já com a versão de agora.</summary>
        public void Continue()
        {
            screen.SetActive(false);
            runner.ContinueWithOlderSave();
        }
    }
}
