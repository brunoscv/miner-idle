using System;
using MinersIdle.Core;
using MinersIdle.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// Uma linha da aba das permanentes (Marco 8b, E4, D-155), no prefab da linha da loja: ícone, nome, nível/limite,
    /// efeito "de » para" e o botão Comprar com o custo em lascas vermelhas (o cristal vermelho ao lado do número). No
    /// limite, a linha fica apagada, com "máx." e a moldura cinza; quem a põe no fim da lista é a <see cref="ShopView"/>.
    /// Só mostra o que a <see cref="PermanentPreview"/> diz.
    /// </summary>
    public sealed class PermanentRowView : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] Image frame;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text level;
        [SerializeField] TMP_Text effect;
        [SerializeField] Button buyButton;
        [SerializeField] TMP_Text buyLabel;
        [Tooltip("O cristal vermelho do preço: some no limite.")]
        [SerializeField] Image priceIcon;
        [SerializeField] CanvasGroup group;
        [Tooltip("A moldura da linha no limite.")]
        [SerializeField] Sprite maxedFrame;
        [SerializeField] Color buyableLabelColor = Color.white;
        [SerializeField] Color unavailableLabelColor = Color.gray;
        [SerializeField] Color buyableIconColor = new Color(1f, 0.32f, 0.3f);
        [SerializeField] Color unavailableIconColor = new Color(0.55f, 0.35f, 0.33f);
        [Range(0f, 1f)]
        [SerializeField] float maxedAlpha = 0.45f;

        ShopCatalog _catalog;
        ShopCatalog.PermanentEntry _entry;
        Action<PermanentId> _buy;

        public PermanentId Permanent => _entry.Permanent;
        public bool IsMaxed { get; private set; }
        public bool CanBuy => buyButton.interactable;
        public string TitleText => title.text;
        public string LevelText => level.text;
        public string EffectText => effect.text;
        public string ButtonText => buyLabel.text;

        /// <summary>Monta a linha uma vez (textos fixos e o clique); depois só <see cref="Refresh"/>.</summary>
        public void Bind(ShopCatalog catalog, ShopCatalog.PermanentEntry entry, Action<PermanentId> buy)
        {
            _catalog = catalog;
            _entry = entry;
            _buy = buy;

            title.text = entry.Title;
            if (entry.Icon != null)
            {
                icon.sprite = entry.Icon;
                icon.color = Color.white;
                icon.preserveAspect = true;
            }
            frame.sprite = entry.Frame;
            frame.enabled = entry.Frame != null;
            buyButton.onClick.AddListener(OnBuyClicked);
        }

        void OnDestroy() => buyButton.onClick.RemoveListener(OnBuyClicked);

        void OnBuyClicked() => _buy?.Invoke(_entry.Permanent);

        public void Refresh(PermanentPreview preview)
        {
            if (gameObject.activeSelf != preview.IsEnabled) gameObject.SetActive(preview.IsEnabled);
            if (!preview.IsEnabled) return;

            level.text = string.Format(_catalog.LevelFormat, preview.Level, preview.MaxLevel);
            bool buyable = preview.State == UpgradeButtonState.Purchasable;
            buyButton.interactable = buyable;
            buyLabel.color = buyable ? buyableLabelColor : unavailableLabelColor;
            priceIcon.color = buyable ? buyableIconColor : unavailableIconColor;

            IsMaxed = preview.IsMaxed;
            group.alpha = IsMaxed ? maxedAlpha : 1f;
            var wantedFrame = IsMaxed && maxedFrame != null ? maxedFrame : _entry.Frame;
            if (frame.sprite != wantedFrame) frame.sprite = wantedFrame;
            if (priceIcon.gameObject.activeSelf == IsMaxed) priceIcon.gameObject.SetActive(!IsMaxed);

            if (IsMaxed)
            {
                effect.text = string.Format(_entry.MaxedFormat, Format(preview.CurrentValue));
                buyLabel.text = _catalog.MaxedLabel;
                return;
            }

            effect.text = string.Format(_entry.EffectFormat, Format(preview.CurrentValue), Format(preview.NextValue));
            buyLabel.text = NumberFormatter.FormatShards(preview.NextCost.Value);
        }

        string Format(double value) => _catalog.FormatValue(_entry.Style, value);
    }
}
