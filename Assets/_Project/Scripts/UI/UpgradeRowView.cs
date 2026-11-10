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
        [Header("Compra única feita (M7, E3, D-174)")]
        [Tooltip("A moldura da linha já comprada (o Estagiário contratado), que desce para o fim da lista. Vazio = a do catálogo.")]
        [SerializeField] Sprite doneFrame;
        [Range(0f, 1f)]
        [SerializeField] float doneAlpha = 0.45f;

        ShopCatalog _catalog;
        ShopCatalog.Entry _entry;
        Action<UpgradeId> _buy;

        public UpgradeId Upgrade => _entry.Upgrade;
        public bool IsLocked => _entry.Locked;
        public bool CanBuy => buyButton.interactable;
        /// <summary>Compra única já feita (o Estagiário contratado): a loja a põe no fim da lista (D-174).</summary>
        public bool IsDone { get; private set; }
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

            // Linha desligada no balanço ativo (limite 0, como as Botas no GDD v5, D-103): some da loja.
            if (gameObject.activeSelf != preview.IsEnabled) gameObject.SetActive(preview.IsEnabled);
            if (!preview.IsEnabled) return;

            // Compra única (o Estagiário, D-174): sem o "nv 0/1", que não diz nada; feita, apagada e com a moldura cinza.
            int first = _entry.LevelFromOne ? 1 : 0; // D-207: nível 1 = sem compra
            level.text = preview.IsSinglePurchase ? "" : string.Format(_catalog.LevelFormat, preview.Level + first, preview.MaxLevel + first);
            SetBuyable(preview.State == UpgradeButtonState.Purchasable);
            SetDone(preview.IsSinglePurchase && preview.Level > 0);

            // Letrado (M8, D-109): sem os cristais da caverna, o progresso no lugar do efeito e o preço apagado.
            if (preview.State == UpgradeButtonState.Locked)
            {
                effect.text = string.Format(_catalog.ProgressFormat, preview.Progress, preview.ProgressGoal);
                buyLabel.text = CostLabel(preview.NextCost.Value);
                return;
            }

            if (preview.State == UpgradeButtonState.PassFirst)
            {
                effect.text = string.Format(_entry.EffectFormat, Format(preview.CurrentValue), Format(preview.NextValue));
                buyLabel.text = _catalog.PassFirstLabel;
                return;
            }

            if (preview.State == UpgradeButtonState.Maxed)
            {
                effect.text = string.Format(_entry.EffectFormat, Format(preview.CurrentValue), Format(preview.CurrentValue));
                buyLabel.text = !IsDone ? _catalog.MaxedLabel
                    : string.IsNullOrEmpty(_entry.BoughtLabel) ? _catalog.BoughtLabel : _entry.BoughtLabel;
                return;
            }

            effect.text = string.Format(_entry.EffectFormat, Format(preview.CurrentValue), Format(preview.NextValue));
            buyLabel.text = CostLabel(preview.NextCost.Value);
        }

        /// <summary>O preço no botão; uma compra de graça (o Mapa do GDD v7, D-175) mostra o texto do catálogo no lugar do 0.</summary>
        string CostLabel(double cost) =>
            cost <= 0 && !string.IsNullOrEmpty(_catalog.FreeLabel) ? _catalog.FreeLabel : NumberFormatter.FormatCost(cost);

        void SetDone(bool done)
        {
            if (done == IsDone) return;
            IsDone = done;
            group.alpha = done ? doneAlpha : 1f;
            if (frame == null) return;
            var wanted = done && doneFrame != null ? doneFrame : _entry.Frame;
            frame.sprite = wanted;
            frame.enabled = wanted != null;
        }

        void SetBuyable(bool buyable)
        {
            buyButton.interactable = buyable;
            buyLabel.color = buyable ? buyableLabelColor : unavailableLabelColor;
        }

        string Format(double value) => _catalog.FormatValue(_entry.Style, value);
    }
}
