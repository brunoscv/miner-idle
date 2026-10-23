using System;
using MinersIdle.Core;
using MinersIdle.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// Uma linha da loja (S03), prefab reutilizável (REQ-UI-06): ícone, nome, nível/limite, efeito "de » para" e o botão
    /// Comprar com o custo. Só mostra o que a <see cref="UpgradePreview"/> diz; quem compra é a <see cref="ShopView"/>.
    /// M1b E3: ícone e moldura vêm do catálogo; o fundo do botão troca sozinho (Sprite Swap do Button: normal, pressionado,
    /// desabilitado) e aqui só a cor do texto acompanha.
    /// </summary>
    public sealed class UpgradeRowView : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text level;
        [SerializeField] TMP_Text effect;
        [SerializeField] Button buyButton;
        [SerializeField] TMP_Text buyLabel;
        [Tooltip("Fundo da linha: fica mais apagado quando a linha está bloqueada.")]
        [SerializeField] CanvasGroup group;
        [Tooltip("Moldura do ícone (cor de raridade, do catálogo). Opcional.")]
        [SerializeField] Image frame;
        [Tooltip("Cor do preço quando dá para comprar e quando não dá.")]
        [SerializeField] Color buyableLabelColor = new Color32(0x15, 0x19, 0x1C, 0xFF);
        [SerializeField] Color unavailableLabelColor = new Color32(0x15, 0x19, 0x1C, 0xFF);

        ShopCatalog _catalog;
        ShopCatalog.Entry _entry;
        Action<UpgradeId> _buy;

        public UpgradeId Upgrade => _entry.Upgrade;
        public bool IsLocked => _entry.Locked;
        public bool CanBuy => buyButton.interactable;
        public string LevelText => level.text;
        public string EffectText => effect.text;
        public string ButtonText => buyLabel.text;

        /// <summary>Monta a linha uma vez (textos fixos e o clique); depois só <see cref="Refresh"/>.</summary>
        public void Bind(ShopCatalog catalog, ShopCatalog.Entry entry, Action<UpgradeId> buy)
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
            else icon.color = entry.IconColor;
            if (frame != null)
            {
                frame.sprite = entry.Frame;
                frame.enabled = entry.Frame != null;
            }
            buyButton.onClick.AddListener(OnBuyClicked);

            if (!entry.Locked) return;

            level.text = "";
            effect.text = entry.EffectFormat;
            buyLabel.text = catalog.LockedLabel;
            SetBuyable(false);
            group.alpha = 0.45f;
        }

        void OnDestroy() => buyButton.onClick.RemoveListener(OnBuyClicked);

        void OnBuyClicked()
        {
            if (!_entry.Locked) _buy?.Invoke(_entry.Upgrade);
        }

        public void Refresh(UpgradePreview preview)
        {
            if (_entry.Locked) return;

            level.text = string.Format(_catalog.LevelFormat, preview.Level, preview.MaxLevel);
            SetBuyable(preview.State == UpgradeButtonState.Purchasable);

            if (preview.State == UpgradeButtonState.Maxed)
            {
                effect.text = string.Format(_entry.EffectFormat, Format(preview.CurrentValue), Format(preview.CurrentValue));
                buyLabel.text = preview.IsSinglePurchase && preview.Level > 0 ? _catalog.BoughtLabel : _catalog.MaxedLabel;
                return;
            }

            effect.text = string.Format(_entry.EffectFormat, Format(preview.CurrentValue), Format(preview.NextValue));
            buyLabel.text = NumberFormatter.FormatShards(preview.NextCost.Value);
        }

        void SetBuyable(bool buyable)
        {
            buyButton.interactable = buyable;
            buyLabel.color = buyable ? buyableLabelColor : unavailableLabelColor;
        }

        string Format(double value)
        {
            switch (_entry.Style)
            {
                case ShopCatalog.EffectStyle.Seconds:
                    return NumberFormatter.FormatSeconds(value);
                case ShopCatalog.EffectStyle.BonusPercent:
                    return string.Format(_catalog.BonusPercentFormat, NumberFormatter.Format((value - 1) * 100));
                default:
                    return NumberFormatter.Format(value);
            }
        }
    }
}
