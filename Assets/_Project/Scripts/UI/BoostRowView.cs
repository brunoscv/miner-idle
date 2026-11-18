using System;
using MinersIdle.Core;
using MinersIdle.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// Uma linha do menu do Boost (M9e E3, P6): o ícone, o nome e a duração; o bônus; o crítico e as horas extras (ou, no
    /// do tier 2, "Some com o Raro Dourado"); o botão ASSISTIR/RENOVAR e o "falta 31m" embaixo dele. Os números vêm do
    /// balanço (<see cref="AdOffers"/>); os textos, do <see cref="BuffCatalog"/>. Só muda o texto quando o número muda.
    /// </summary>
    public sealed class BoostRowView : MonoBehaviour
    {
        [SerializeField] AdKind kind = AdKind.Short;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text duration;
        [SerializeField] TMP_Text effect;
        [SerializeField] TMP_Text detail;
        [SerializeField] Button button;
        [SerializeField] TMP_Text buttonLabel;
        [Tooltip("\"falta 31m\" debaixo do botão: só com o anúncio ativo.")]
        [SerializeField] TMP_Text left;

        [Header("Cores")]
        [SerializeField] Color labelColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        [SerializeField] Color disabledLabelColor = new Color32(0xBE, 0xB4, 0xAA, 0xFF);

        BuffCatalog _catalog;
        Action<AdKind> _watch;
        int _state = -1;
        long _leftKey = long.MinValue;
        double _shownDivisor = -1;

        public AdKind Kind => kind;
        public bool IsShown => gameObject.activeSelf;
        public bool CanPress => button.interactable;
        public string ButtonText => buttonLabel.text;
        public string LeftText => left.gameObject.activeSelf ? left.text : "";
        public string EffectText => effect.text;
        public string DetailText => detail.text;
        public string DurationText => duration.text;

        public void Bind(BuffCatalog catalog, Action<AdKind> watch)
        {
            _catalog = catalog;
            _watch = watch;
            var entry = catalog.Find(BonusSource.Ad, (int)kind);
            title.text = entry != null ? entry.Title : kind.ToString();
            button.onClick.AddListener(OnClick);
        }

        void OnDestroy() => button.onClick.RemoveListener(OnClick);

        void OnClick() => _watch?.Invoke(kind);

        public void Refresh(MiningSimulation simulation, bool adShowing)
        {
            var ads = simulation.Strike.Ads;
            bool tier2 = kind == AdKind.RareTier2;
            // O do tier 2 só a partir do andar 10 e até o Raro Dourado (D-250); os outros, se o balanço os tem.
            bool shown = tier2 ? simulation.CanWatch(kind) || simulation.AdSecondsLeft(kind) > 0 && !simulation.Permanents.HasGoldenRare
                               : ads.Has(kind);
            if (gameObject.activeSelf != shown) gameObject.SetActive(shown);
            if (!shown) return;

            if (_shownDivisor != simulation.AdDurationDivisor)
            {
                _shownDivisor = simulation.AdDurationDivisor;
                FillTexts(simulation, ads, tier2);
            }

            double secondsLeft = simulation.AdSecondsLeft(kind);
            bool active = secondsLeft > 0;
            bool canPress = !adShowing && simulation.CanWatch(kind);
            int state = (active ? 1 : 0) | (canPress ? 2 : 0);
            if (state != _state)
            {
                _state = state;
                buttonLabel.text = active ? _catalog.BoostRenew : _catalog.BoostWatch;
                button.interactable = canPress;
                buttonLabel.color = canPress ? labelColor : disabledLabelColor;
                if (left.gameObject.activeSelf != active) left.gameObject.SetActive(active);
                _leftKey = long.MinValue;
            }
            if (!active) return;
            long key = NumberFormatter.TimerKey(secondsLeft);
            if (key == _leftKey) return;
            _leftKey = key;
            left.text = string.Format(_catalog.BoostLeftFormat ?? "{0}", NumberFormatter.FormatTimer(secondsLeft));
        }

        void FillTexts(MiningSimulation simulation, AdOffers ads, bool tier2)
        {
            var entry = _catalog.Find(BonusSource.Ad, (int)kind);
            double seconds = (tier2 ? ads.Tier2Seconds : ads.Get(kind).Seconds) / simulation.AdDurationDivisor;
            duration.text = NumberFormatter.FormatSpan(seconds);
            if (tier2)
            {
                effect.text = entry != null ? entry.Description : "";
                detail.text = _catalog.BoostTier2Note;
                return;
            }
            var offer = ads.Get(kind);
            string multiplier = NumberFormatter.FormatEffect(BonusKind.DamageMultiplier, offer.Multiplier);
            effect.text = entry != null ? string.Format(entry.Description ?? "", multiplier) : multiplier;
            detail.text = string.Format(_catalog.BoostDetailFormat ?? "{0} {1}",
                                        NumberFormatter.FormatEffect(BonusKind.CritChance, offer.CritChance),
                                        NumberFormatter.FormatSpan(offer.OfflineSeconds));
        }
    }
}
