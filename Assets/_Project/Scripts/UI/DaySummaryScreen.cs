using MinersIdle.Core;
using MinersIdle.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// O resumo do dia (Marco 8b, E3, D-154), depois de descansar: o dia que terminou, o nível, o tempo, as lascas
    /// vermelhas ganhas e o banco. Desde o M9d (D-265) a run nova fica <b>parada</b> por trás até o COMEÇAR (o INICIAR
    /// provisório da E1), que a monta de novo com as permanentes compradas. PERMANENTES fecha e abre a loja na aba das
    /// permanentes (só ali se compram); ao fechar a loja ainda descansando, o resumo volta. Abre pelo evento
    /// <see cref="MiningRunner.Rested"/> (o modal, o DBG e o jogo reaberto descansando, save v9). Os números do dia vêm do
    /// <see cref="RestSummary"/>; o banco, das permanentes (muda com as compras). Textos em dados.
    /// </summary>
    public sealed class DaySummaryScreen : MonoBehaviour
    {
        [SerializeField] FeedbackTexts texts;
        [SerializeField] MiningRunner runner;
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
        [SerializeField] ShopView shop;
        [SerializeField] Button permanentsButton;
        [SerializeField] TMP_Text permanentsLabel;

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
            permanentsLabel.text = texts.SummaryPermanents;
            screen.SetActive(false);
        }

        void OnEnable()
        {
            startButton.onClick.AddListener(StartRun);
            permanentsButton.onClick.AddListener(OpenPermanents);
            if (shop != null) shop.Closed += OnShopClosed;
            if (runner == null) return;
            runner.Rested += Open;
            runner.SimulationStarted += OnSimulationStarted;
            if (runner.PendingRest.HasValue && !IsOpen) Open(runner.PendingRest.Value);
        }

        void OnDisable()
        {
            startButton.onClick.RemoveListener(StartRun);
            permanentsButton.onClick.RemoveListener(OpenPermanents);
            if (shop != null) shop.Closed -= OnShopClosed;
            if (runner == null) return;
            runner.Rested -= Open;
            runner.SimulationStarted -= OnSimulationStarted;
        }

        /// <summary>Uma partida que não está descansando (save apagado, a run do INICIAR): o resumo some.</summary>
        void OnSimulationStarted(MiningSimulation simulation)
        {
            if (!simulation.IsResting) Close();
        }

        /// <summary>A loja aberta do PERMANENTES fechou: se ainda está descansando, o resumo volta, com o banco de agora.</summary>
        void OnShopClosed()
        {
            if (runner != null && runner.PendingRest.HasValue) Open(runner.PendingRest.Value);
        }

        /// <summary>COMEÇAR (o INICIAR do M9d): a run nova começa agora.</summary>
        public void StartRun()
        {
            Close();
            if (runner != null) runner.StartRun();
        }

        public void Open(RestSummary summary)
        {
            title.text = string.Format(texts.SummaryTitleFormat, summary.Day);
            levelValue.text = summary.Level.ToString();
            timeValue.text = NumberFormatter.FormatDuration(summary.PlaySeconds);
            gainedValue.text = string.Format(texts.SummaryGainedFormat, NumberFormatter.FormatShards(summary.RedShardsGained));
            var permanents = runner != null ? runner.Simulation?.Permanents : null;
            bankValue.text = NumberFormatter.FormatShards(permanents?.State.RedShards ?? summary.RedShardsBank);
            screen.SetActive(true);
        }

        public void Close() => screen.SetActive(false);

        void OpenPermanents()
        {
            Close();
            shop.OpenPermanents();
        }
    }
}
