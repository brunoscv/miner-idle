using MinersIdle.Core;
using MinersIdle.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// O resumo do dia (Marco 8b, E3, D-154), depois de descansar pelo modal: o dia que terminou, o nível, o tempo, as
    /// lascas vermelhas ganhas e o banco. A run nova já começou por trás (fila e semente novas); COMEÇAR só fecha. O botão
    /// das melhorias permanentes chega com a aba delas na loja (E4). O atalho Descansar agora do DBG não passa por aqui.
    /// Os números vêm do <see cref="RestSummary"/> do descanso, não da simulação, que já é a da run nova. Textos em dados.
    /// </summary>
    public sealed class DaySummaryScreen : MonoBehaviour
    {
        [SerializeField] FeedbackTexts texts;
        [Tooltip("O fundo que escurece e o painel: ligado só com o resumo aberto.")]
        [SerializeField] GameObject screen;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text levelLabel;
        [SerializeField] TMP_Text levelValue;
        [SerializeField] TMP_Text timeLabel;
        [SerializeField] TMP_Text timeValue;
        [SerializeField] TMP_Text gainedLabel;
        [SerializeField] TMP_Text gainedValue;
        [SerializeField] TMP_Text bankLabel;
        [SerializeField] TMP_Text bankValue;
        [SerializeField] Button startButton;
        [SerializeField] TMP_Text startLabel;

        public bool IsOpen => screen.activeSelf;
        public string TitleText => title.text;
        public string LevelText => levelValue.text;
        public string GainedText => gainedValue.text;
        public string BankText => bankValue.text;

        void Awake()
        {
            levelLabel.text = texts.SummaryLevelLabel;
            timeLabel.text = texts.SummaryTimeLabel;
            gainedLabel.text = texts.SummaryGainedLabel;
            bankLabel.text = texts.SummaryBankLabel;
            startLabel.text = texts.SummaryStart;
            screen.SetActive(false);
        }

        void OnEnable() => startButton.onClick.AddListener(Close);

        void OnDisable() => startButton.onClick.RemoveListener(Close);

        public void Open(RestSummary summary)
        {
            title.text = string.Format(texts.SummaryTitleFormat, summary.Day);
            levelValue.text = summary.Level.ToString();
            timeValue.text = NumberFormatter.FormatDuration(summary.PlaySeconds);
            gainedValue.text = string.Format(texts.SummaryGainedFormat, NumberFormatter.FormatShards(summary.RedShardsGained));
            bankValue.text = NumberFormatter.FormatShards(summary.RedShardsBank);
            screen.SetActive(true);
        }

        public void Close() => screen.SetActive(false);
    }
}
