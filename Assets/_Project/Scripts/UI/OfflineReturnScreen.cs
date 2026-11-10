using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// O modal "Bem-vindo de volta" (M9, E2, D-197; mockup aprovado pelo Bruno em 2026-11-10): na volta com 60 s ou mais
    /// fora (D-195) e o estagiário tendo trazido lascas, mostra o tempo fora (e o que contou, quando passou do teto de
    /// 6 h), os cristais que ele quebrou e as lascas. As lascas só entram no COLETAR, pelo <see cref="MiningRunner"/>; até
    /// lá ficam pendentes no estado da run e no save, então fechar e abrir o app no meio do modal não paga duas vezes.
    ///
    /// Abre sozinho quando há volta pendente: ao começar a partida (o save trouxe uma) e quando chega uma nova
    /// (<see cref="MiningSimulation.OfflineReturnChanged"/>, inclusive pelo DBG → Offline). Como a loja, escurece e bloqueia
    /// o mapa sem pausar a mineração. Textos em dados.
    /// </summary>
    public sealed class OfflineReturnScreen : MiningViewBase
    {
        [SerializeField] FeedbackTexts texts;
        [Tooltip("O fundo que escurece e o painel: ligado só com uma volta para coletar.")]
        [SerializeField] GameObject screen;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text awayLabel;
        [SerializeField] TMP_Text awayValue;
        [Tooltip("A linha \"Contou\": só aparece quando o tempo fora passou do teto.")]
        [SerializeField] GameObject countedRow;
        [SerializeField] TMP_Text countedLabel;
        [SerializeField] TMP_Text countedValue;
        [SerializeField] TMP_Text crystalsLabel;
        [SerializeField] TMP_Text crystalsValue;
        [SerializeField] TMP_Text broughtLabel;
        [SerializeField] TMP_Text shardsValue;
        [SerializeField] TMP_Text note;
        [SerializeField] Button collectButton;
        [SerializeField] TMP_Text collectLabel;

        public bool IsOpen => screen.activeSelf;
        public bool ShowsCounted => countedRow.activeSelf;
        public string AwayText => awayValue.text;
        public string CountedText => countedValue.text;
        public string CrystalsText => crystalsValue.text;
        public string ShardsText => shardsValue.text;
        public string NoteText => note.text;

        void Awake()
        {
            title.text = texts.OfflineTitle;
            awayLabel.text = texts.OfflineAwayLabel;
            countedLabel.text = texts.OfflineCountedLabel;
            crystalsLabel.text = texts.OfflineCrystalsLabel;
            broughtLabel.text = texts.OfflineBroughtLabel;
            collectLabel.text = texts.OfflineCollect;
            screen.SetActive(false); // o OnEnable (logo depois) abre, se a partida já tiver uma volta pendente
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            collectButton.onClick.AddListener(Collect);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            collectButton.onClick.RemoveListener(Collect);
        }

        protected override void Subscribe(MiningSimulation simulation) =>
            simulation.OfflineReturnChanged += OnOfflineReturnChanged;

        protected override void Unsubscribe(MiningSimulation simulation) =>
            simulation.OfflineReturnChanged -= OnOfflineReturnChanged;

        /// <summary>Uma partida nova: abre se ela trouxe uma volta para coletar (o save), fecha se não.</summary>
        protected override void Rebuild(MiningSimulation simulation) => Show(simulation.PendingOffline);

        void OnOfflineReturnChanged(OfflineReturn pending) => Show(pending);

        /// <summary>COLETAR: as lascas entram (o runner grava na hora) e o modal fecha.</summary>
        public void Collect()
        {
            runner.CollectOffline();
            screen.SetActive(false);
        }

        void Show(OfflineReturn pending)
        {
            if (!pending.HasShards)
            {
                screen.SetActive(false);
                return;
            }

            awayValue.text = NumberFormatter.FormatDuration(pending.SecondsAway);
            countedRow.SetActive(pending.Capped);
            countedValue.text = string.Format(texts.OfflineCountedFormat,
                                              NumberFormatter.FormatDuration(pending.SecondsCounted, trimWholeHours: true));
            crystalsValue.text = NumberFormatter.Format(pending.Crystals);
            shardsValue.text = string.Format(texts.OfflineShardsFormat, NumberFormatter.FormatShards(pending.Shards));
            note.text = pending.Capped
                ? string.Format(texts.OfflineCappedNoteFormat,
                                NumberFormatter.FormatDuration(Simulation.Rules.OfflineMaxSeconds, trimWholeHours: true))
                : string.Format(texts.OfflineNoteFormat, pending.Level);
            screen.SetActive(true);
            // A linha "Contou" entra ou sai: o painel se ajusta ao conteúdo já neste quadro.
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)screen.transform);
        }
    }
}
