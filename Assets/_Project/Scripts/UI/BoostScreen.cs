using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// O menu do Boost (M9e E3, P6; mockup da E0, <c>tools/ci/out/m9e-e0-boost.png</c>): uma linha por anúncio — curto,
    /// médio e grande, e o "Raro tier 2" a partir do andar 10 até o Raro Dourado (D-250). Cada linha diz o bônus, a duração
    /// e as horas extras; o botão ASSISTIR abre o anúncio (<see cref="IRewardedAd"/>, hoje o simulado, D-218) e, com ele
    /// ativo, vira RENOVAR com o tempo que falta embaixo (D-244). A recompensa é o
    /// <see cref="MiningSimulation.WatchAd"/>. Como a loja, escurece e bloqueia o mapa sem pausar a mineração; o anúncio
    /// abre por cima e o menu continua aberto embaixo, para ver o RENOVAR. Textos no <see cref="BuffCatalog"/>.
    /// </summary>
    public sealed class BoostScreen : MiningViewBase
    {
        [SerializeField] BuffCatalog catalog;
        [Tooltip("Quem mostra o anúncio: um componente que implementa IRewardedAd (hoje o SimulatedAdScreen).")]
        [SerializeField] MonoBehaviour rewardedAd;
        [Tooltip("O fundo que escurece e o painel: ligado só com o menu aberto.")]
        [SerializeField] GameObject screen;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text note;
        [SerializeField] BoostRowView[] rows = new BoostRowView[0];
        [SerializeField] Button backButton;
        [SerializeField] TMP_Text backLabel;

        IRewardedAd _ad;

        public bool IsOpen => screen.activeSelf;
        public BoostRowView Row(AdKind kind)
        {
            foreach (var row in rows)
                if (row != null && row.Kind == kind) return row;
            return null;
        }

        void Awake()
        {
            _ad = rewardedAd as IRewardedAd;
            if (_ad == null) Debug.LogError("[BoostScreen] O anúncio não implementa IRewardedAd.", this);
            title.text = catalog.BoostTitle;
            note.text = catalog.BoostNote;
            backLabel.text = catalog.BoostBack;
            foreach (var row in rows)
                if (row != null) row.Bind(catalog, Watch);
            screen.SetActive(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            backButton.onClick.AddListener(Close);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            backButton.onClick.RemoveListener(Close);
        }

        /// <summary>Uma partida nova (o DBG trocou o balanço, o save voltou): o menu fecha.</summary>
        protected override void Rebuild(MiningSimulation simulation) => Close();

        public void Open()
        {
            if (Simulation == null || !Simulation.Strike.Ads.IsEnabled) return;
            screen.SetActive(true);
            Refresh();
        }

        public void Close() => screen.SetActive(false);

        /// <summary>ASSISTIR / RENOVAR de uma linha: o anúncio abre; a recompensa é o anúncio <paramref name="kind"/>.</summary>
        public void Watch(AdKind kind)
        {
            var simulation = Simulation;
            if (simulation == null || _ad == null || _ad.IsShowing || !simulation.CanWatch(kind)) return;
            _ad.Show(() => simulation.WatchAd(kind), RewardText(simulation, kind));
            Refresh();
        }

        string RewardText(MiningSimulation simulation, AdKind kind)
        {
            var entry = catalog.Find(BonusSource.Ad, (int)kind);
            var ads = simulation.Strike.Ads;
            double seconds = kind == AdKind.RareTier2 ? ads.Tier2Seconds : ads.Get(kind).Seconds;
            string gain = entry != null
                ? string.Format(entry.Gain ?? "", kind == AdKind.RareTier2 ? "" : NumberFormatter.FormatEffect(BonusKind.DamageMultiplier, ads.Get(kind).Multiplier))
                : "";
            return string.Format(catalog.AdOfferRewardFormat ?? "{0} {1}", gain,
                                 NumberFormatter.FormatSpan(seconds / simulation.AdDurationDivisor));
        }

        void Update()
        {
            if (IsOpen) Refresh();
        }

        void Refresh()
        {
            var simulation = Simulation;
            if (simulation == null) return;
            bool showing = _ad != null && _ad.IsShowing;
            foreach (var row in rows)
                if (row != null) row.Refresh(simulation, showing);
        }
    }
}
