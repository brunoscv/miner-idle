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
    /// Só mostra o que a <see cref="PermanentPreview"/> diz. M7 (E3): a linha que espera outra (a Promoção) mostra o motivo.
    /// M9g (E3, D-314): a linha com um nome por nível (a Bomba) mostra o nome e o valor ("Pequena 10% » 20%"), e o ícone
    /// segue o nível, recortado da folha (só quando o nível muda).
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
        Sprite[] _levelIcons;
        int _shownIconLevel = -1;

        public PermanentId Permanent => _entry.Permanent;
        public bool IsMaxed { get; private set; }
        public bool CanBuy => buyButton.interactable;
        public string TitleText => title.text;
        public string LevelText => level.text;
        public string EffectText => effect.text;
        public string ButtonText => buyLabel.text;
        /// <summary>Só consulta (D-276, fora do descanso): o efeito de agora, sem preço nem botão.</summary>
        public bool IsConsult { get; private set; }
        public bool IsBuyShown => buyButton.gameObject.activeSelf;

        public void SetConsult(bool consult)
        {
            IsConsult = consult;
            if (buyButton.gameObject.activeSelf == consult) buyButton.gameObject.SetActive(!consult);
        }

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

        void OnDestroy()
        {
            buyButton.onClick.RemoveListener(OnBuyClicked);
            if (_levelIcons == null) return;
            foreach (var sprite in _levelIcons)
                if (sprite != null) Destroy(sprite);
        }

        void OnBuyClicked() => _buy?.Invoke(_entry.Permanent);

        public void Refresh(PermanentPreview preview)
        {
            if (gameObject.activeSelf != preview.IsEnabled) gameObject.SetActive(preview.IsEnabled);
            if (!preview.IsEnabled) return;

            level.text = string.Format(_catalog.LevelFormat, preview.Level, preview.MaxLevel);
            ShowLevelIcon(preview.Level);
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
                effect.text = Effect(_entry.MaxedFormat, preview);
                buyLabel.text = _catalog.MaxedLabel;
                return;
            }

            // Espera outra linha (a Promoção antes do Treinar no limite, M7): o motivo no lugar do efeito, o preço apagado.
            if (preview.State == UpgradeButtonState.Locked && !string.IsNullOrEmpty(_entry.LockedFormat))
            {
                effect.text = string.Format(_entry.LockedFormat, preview.RequiredLevel);
                buyLabel.text = NumberFormatter.FormatShards(preview.NextCost.Value);
                return;
            }

            if (IsConsult)
            {
                effect.text = Effect(_entry.MaxedFormat, preview);
                buyLabel.text = NumberFormatter.FormatShards(preview.NextCost.Value);
                return;
            }

            effect.text = Effect(_entry.EffectFormat, preview);
            buyLabel.text = NumberFormatter.FormatShards(preview.NextCost.Value);
        }

        string Format(double value) => _catalog.FormatValue(_entry.Style, value);

        /// <summary>
        /// O texto do efeito: {0} = o valor de agora e {1} = o de depois; numa linha com nomes por nível (a Bomba), {0} = o
        /// nome de agora, {1} = o valor de agora, {2} = o de depois e {3} = o nome de depois.
        /// </summary>
        string Effect(string format, PermanentPreview preview)
        {
            if (!_entry.HasLevelNames) return string.Format(format, Format(preview.CurrentValue), Format(preview.NextValue));
            return string.Format(format, LevelName(preview.Level), Format(preview.CurrentValue), Format(preview.NextValue),
                                 LevelName(preview.Level + 1));
        }

        string LevelName(int index)
        {
            var names = _entry.LevelNames;
            return names[Mathf.Clamp(index, 0, names.Count - 1)];
        }

        /// <summary>O ícone do nível, recortado da folha uma vez por nível (a Bomba: a bomba comprada).</summary>
        void ShowLevelIcon(int index)
        {
            var sheet = _entry.LevelSheet;
            int frames = _entry.LevelFrames;
            if (sheet == null || frames <= 0 || index == _shownIconLevel) return;
            _shownIconLevel = index;
            index = Mathf.Clamp(index, 0, frames - 1);
            _levelIcons ??= new Sprite[frames];
            if (_levelIcons[index] == null)
            {
                float width = (float)sheet.width / frames;
                _levelIcons[index] = Sprite.Create(sheet, new Rect(index * width, 0f, width, sheet.height), new Vector2(0.5f, 0.5f));
                _levelIcons[index].name = sheet.name + "_" + index;
            }
            icon.sprite = _levelIcons[index];
            icon.color = Color.white;
            icon.preserveAspect = true;
        }

        /// <summary>O ícone que a linha mostra agora (para os testes).</summary>
        public Sprite IconSprite => icon.sprite;
    }
}
