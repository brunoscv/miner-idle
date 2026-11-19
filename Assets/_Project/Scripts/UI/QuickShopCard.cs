using System;
using MinersIdle.Core;
using MinersIdle.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// Um card da loja rápida (M9i E8, D-341): o tile do Apellon com o ícone da melhoria, "Nv n" e o preço. O card inteiro
    /// é o botão de comprar. Como na LOJA, sem as lascas da compra (ou no limite) ele fica apagado e não compra; com elas,
    /// acende. Só mostra o que a <see cref="UpgradePreview"/> diz; quem compra é a <see cref="QuickShopView"/>.
    /// </summary>
    public sealed class QuickShopCard : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image background;
        [SerializeField] Image icon;
        [SerializeField] Image shard;
        [SerializeField] TMP_Text level;
        [SerializeField] TMP_Text price;
        [Tooltip("O card apagado: a mesma tinta do Descansar trancado (M9i E6).")]
        [SerializeField] Color dimTint = new Color(0.545f, 0.545f, 0.545f, 1f);
        [SerializeField] Color textColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        [SerializeField] Color dimTextColor = new Color32(0xBE, 0xB4, 0xAA, 0xFF);

        ShopCatalog _catalog;
        ShopCatalog.Entry _entry;
        Action<UpgradeId> _buy;
        Color _iconColor = Color.white;
        int _shownLevel = -1;
        double _shownCost = -1;
        UpgradeButtonState _shownState = (UpgradeButtonState)(-1);

        public UpgradeId Upgrade => _entry != null ? _entry.Upgrade : default;
        public bool CanBuy => button.interactable;
        public string LevelText => level.text;
        public string PriceText => price.text;
        public Button Button => button;

        public void Bind(ShopCatalog catalog, ShopCatalog.Entry entry, Action<UpgradeId> buy)
        {
            _catalog = catalog;
            _entry = entry;
            _buy = buy;
            _iconColor = entry.IconColor;
            if (entry.Icon != null)
            {
                icon.sprite = entry.Icon;
                icon.preserveAspect = true;
            }
            button.onClick.AddListener(OnClick);
        }

        void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClick);
        }

        void OnClick()
        {
            if (_entry != null && button.interactable) _buy?.Invoke(_entry.Upgrade);
        }

        public void Refresh(UpgradePreview preview)
        {
            if (gameObject.activeSelf != preview.IsEnabled) gameObject.SetActive(preview.IsEnabled);
            if (!preview.IsEnabled) return;

            double cost = preview.NextCost ?? -1;
            if (preview.Level == _shownLevel && preview.State == _shownState && cost.Equals(_shownCost)) return;
            _shownLevel = preview.Level;
            _shownState = preview.State;
            _shownCost = cost;

            int first = _entry.LevelFromOne ? 1 : 0; // D-207: a Picareta conta do nível 1, como na LOJA
            level.text = string.Format(_catalog.QuickLevelFormat, preview.Level + first);

            bool maxed = preview.State == UpgradeButtonState.Maxed;
            price.text = maxed || !preview.NextCost.HasValue ? _catalog.MaxedLabel : NumberFormatter.FormatCost(cost);
            shard.enabled = !maxed && preview.NextCost.HasValue;

            bool buyable = preview.State == UpgradeButtonState.Purchasable;
            button.interactable = buyable;
            var tint = buyable ? Color.white : dimTint;
            background.color = tint;
            icon.color = _iconColor * tint;
            shard.color = tint;
            level.color = price.color = buyable ? textColor : dimTextColor;
        }
    }
}
