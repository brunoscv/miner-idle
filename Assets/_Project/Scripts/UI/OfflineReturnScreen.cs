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
    public sealed class OfflineReturnScreen : MiningViewBase, IBackHandler
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
        [Tooltip("M9c E4: a linha dos andares descidos (\"12 » 15\"), só com descida.")]
        [SerializeField] GameObject floorsRow;
        [SerializeField] TMP_Text floorsLabel;
        [SerializeField] TMP_Text floorsValue;
        [Tooltip("M9e E3: as horas extras dos anúncios (\"+3 h\"), só com elas.")]
        [SerializeField] GameObject adsRow;
        [SerializeField] TMP_Text adsLabel;
        [SerializeField] TMP_Text adsValue;
        [Tooltip("M9e E3: os raros que ele quebrou fora, só com eles (D-284).")]
        [SerializeField] GameObject raresRow;
        [SerializeField] TMP_Text raresLabel;
        [SerializeField] TMP_Text raresValue;
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
        public bool ShowsFloors => floorsRow != null && floorsRow.activeSelf;
        public string FloorsText => floorsValue != null ? floorsValue.text : "";
        public bool ShowsAds => adsRow != null && adsRow.activeSelf;
        public string AdsText => adsValue != null ? adsValue.text : "";
        public bool ShowsRares => raresRow != null && raresRow.activeSelf;
        public string RaresText => raresValue != null ? raresValue.text : "";

        void Awake()
        {
            title.text = texts.OfflineTitle;
            awayLabel.text = texts.OfflineAwayLabel;
            countedLabel.text = texts.OfflineCountedLabel;
            crystalsLabel.text = texts.OfflineCrystalsLabel;
            if (floorsLabel != null) floorsLabel.text = texts.OfflineFloorsLabel;
            if (adsLabel != null) adsLabel.text = texts.OfflineAdsLabel;
            if (raresLabel != null) raresLabel.text = texts.OfflineRaresLabel;
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

        /// <summary>M9i E5: o Voltar do Android (fica: as lascas esperam o COLETAR).</summary>
        public bool HandleBack()
        {
            return IsOpen;
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
                                NumberFormatter.FormatDuration(
                                    // O teto que valeu (com as horas dos anúncios, M9e E3); os saves de antes, a conta de agora.
                                    pending.MaxSeconds > 0 ? pending.MaxSeconds
                                        : OfflineMining.MaxSeconds(Simulation.Rules, Simulation.State, Simulation.Permanents),
                                    trimWholeHours: true))
                : pending.Descended ? string.Format(texts.OfflineDescentNoteFormat, pending.FromLevel, pending.Level)
                : string.Format(texts.OfflineNoteFormat, pending.Level);
            if (floorsRow != null)
            {
                floorsRow.SetActive(pending.Descended);
                floorsValue.text = string.Format(texts.OfflineFloorsFormat, pending.FromLevel, pending.Level);
            }
            if (adsRow != null)
            {
                adsRow.SetActive(pending.ExtraSeconds > 0);
                adsValue.text = string.Format(texts.OfflineAdsFormat ?? "{0}",
                                              NumberFormatter.FormatSpan(pending.ExtraSeconds));
            }
            if (raresRow != null)
            {
                raresRow.SetActive(pending.Rares > 0);
                raresValue.text = NumberFormatter.Format(pending.Rares);
            }
            screen.SetActive(true);
            // A linha "Contou" entra ou sai: o painel se ajusta ao conteúdo já neste quadro.
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)screen.transform);
        }
    }
}
