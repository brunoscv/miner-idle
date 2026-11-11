using System.Collections.Generic;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// A coluna de buffs (M9b, E2; D-141, mockup aprovado em 2026-11-10): à esquerda, abaixo do HUD, um ícone por bônus
    /// ativo, na ordem em que entraram (o mais recente embaixo), com o anel âmbar que esvazia e os segundos ao lado. Nos
    /// últimos <see cref="EndingSeconds"/> o número fica âmbar e o ícone pisca. Tocar num ícone mostra por
    /// <see cref="DetailSeconds"/> o nome, o efeito e de onde veio, sem pausar; o toque na coluna não vira toque no mapa
    /// (o ícone recebe o raycast). Só escuta a simulação (<see cref="MiningSimulation.BonusesChanged"/>) e lê o tempo que
    /// falta a cada quadro, sem alocar.
    /// </summary>
    public sealed class BuffColumn : MiningViewBase
    {
        public const float EndingSeconds = 5f;
        public const float DetailSeconds = 2f;
        /// <summary>Ícones criados de saída (o pior caso do mockup: 4 do combo, 2 do raro e 1 do AD).</summary>
        const int InitialSlots = 8;
        const float BlinkPeriod = 0.5f;

        [SerializeField] BuffCatalog catalog;
        [Tooltip("O ícone modelo, desligado; a coluna cria as cópias dentro de Slots.")]
        [SerializeField] BuffSlot slotTemplate;
        [SerializeField] RectTransform slots;

        [Header("Detalhe (tocar no ícone)")]
        [SerializeField] RectTransform detail;
        [SerializeField] TMP_Text detailTitle;
        [SerializeField] TMP_Text detailDescription;
        [SerializeField] TMP_Text detailFooter;

        [Header("Cores")]
        [SerializeField] Color secondsColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        [SerializeField] Color endingColor = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        [Tooltip("A transparência do ícone na metade apagada do piscar.")]
        [SerializeField, Range(0f, 1f)] float blinkAlpha = 0.45f;

        readonly List<BuffSlot> _pool = new List<BuffSlot>();
        readonly List<UnityAction> _clicks = new List<UnityAction>();
        int _shown;
        int _detailIndex = -1;
        float _detailLeft;
        int _detailSeconds = -1;
        bool _listening;

        /// <summary>Quantos ícones estão na coluna agora (para testes).</summary>
        public int ShownCount => _shown;
        public BuffSlot SlotAt(int index) => _pool[index];
        public bool IsDetailOpen => detail != null && detail.gameObject.activeSelf;
        public int DetailIndex => IsDetailOpen ? _detailIndex : -1;
        public string DetailTitleText => detailTitle.text;
        public string DetailFooterText => detailFooter.text;

        void Awake()
        {
            if (slotTemplate != null) slotTemplate.gameObject.SetActive(false);
            if (detail != null) detail.gameObject.SetActive(false);
            EnsurePool(InitialSlots);
        }

        protected override void OnEnable()
        {
            for (int i = 0; i < _pool.Count; i++) _pool[i].Button.onClick.AddListener(_clicks[i]);
            _listening = true;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            for (int i = 0; i < _pool.Count; i++) _pool[i].Button.onClick.RemoveListener(_clicks[i]);
            _listening = false;
        }

        protected override void Subscribe(MiningSimulation simulation) => simulation.BonusesChanged += Refresh;
        protected override void Unsubscribe(MiningSimulation simulation) => simulation.BonusesChanged -= Refresh;
        protected override void Rebuild(MiningSimulation simulation) => Refresh();

        void EnsurePool(int count)
        {
            if (slotTemplate == null || slots == null) return;
            while (_pool.Count < count)
            {
                var slot = Instantiate(slotTemplate, slots, false);
                slot.name = "Slot" + _pool.Count;
                slot.gameObject.SetActive(false);
                int index = _pool.Count;
                UnityAction click = () => ToggleDetail(index);
                _pool.Add(slot);
                _clicks.Add(click);
                if (_listening) slot.Button.onClick.AddListener(click);
            }
        }

        /// <summary>Os ícones dos bônus de agora, na ordem da lista (entrou, acabou, o save trouxe, o DBG limpou).</summary>
        void Refresh()
        {
            var bonuses = Simulation?.Bonuses;
            int count = bonuses?.Count ?? 0;
            EnsurePool(count);
            for (int i = 0; i < _pool.Count; i++)
            {
                bool show = i < count;
                if (show) _pool[i].Bind(catalog != null ? catalog.Find(bonuses.All[i].Source, bonuses.All[i].Tier)?.Icon : null);
                if (_pool[i].gameObject.activeSelf != show) _pool[i].gameObject.SetActive(show);
            }
            _shown = System.Math.Min(count, _pool.Count);
            if (_detailIndex >= _shown) HideDetail();
            else if (IsDetailOpen) FillDetail(); // a lista andou: o detalhe segue o mesmo lugar
            Draw();
        }

        void Update()
        {
            if (Simulation == null) return;
            Draw();
            if (!IsDetailOpen) return;
            _detailLeft -= Time.unscaledDeltaTime;
            if (_detailLeft <= 0f) HideDetail();
            else UpdateFooter();
        }

        void Draw()
        {
            var bonuses = Simulation?.Bonuses;
            if (bonuses == null) return;
            bool dim = Mathf.Repeat(Time.unscaledTime, BlinkPeriod) < BlinkPeriod * 0.5f;
            int count = Mathf.Min(_shown, bonuses.Count);
            string format = catalog != null ? catalog.SecondsFormat : "{0}";
            for (int i = 0; i < count; i++)
            {
                var bonus = bonuses.All[i];
                bool ending = bonus.SecondsLeft <= EndingSeconds;
                _pool[i].Draw(bonus.SecondsLeft, bonus.Duration, format, ending, secondsColor, endingColor,
                              ending && dim ? blinkAlpha : 1f);
            }
        }

        /// <summary>Tocar no ícone <paramref name="index"/>: abre o detalhe (ou fecha, se já era ele).</summary>
        public void ToggleDetail(int index)
        {
            if (index < 0 || index >= _shown || detail == null) return;
            if (IsDetailOpen && _detailIndex == index)
            {
                HideDetail();
                return;
            }
            _detailIndex = index;
            _detailLeft = DetailSeconds;
            detail.gameObject.SetActive(true);
            FillDetail();
        }

        void FillDetail()
        {
            var bonus = Simulation.Bonuses.All[_detailIndex];
            var entry = catalog != null ? catalog.Find(bonus.Source, bonus.Tier) : null;
            detailTitle.text = entry?.Title ?? "";
            detailDescription.text = entry?.Description ?? "";
            _detailSeconds = -1;
            UpdateFooter();

            // Ao lado do ícone, na altura dele.
            LayoutRebuilder.ForceRebuildLayoutImmediate(slots);
            var position = detail.position;
            position.y = _pool[_detailIndex].Rect.position.y;
            detail.position = position;
        }

        void UpdateFooter()
        {
            if (_detailIndex < 0 || _detailIndex >= Simulation.Bonuses.Count) return;
            var bonus = Simulation.Bonuses.All[_detailIndex];
            int seconds = (int)System.Math.Ceiling(System.Math.Max(0, bonus.SecondsLeft) - 1e-9);
            if (seconds == _detailSeconds) return;
            _detailSeconds = seconds;
            if (bonus.Source == BonusSource.Rare)
                detailFooter.SetText(catalog != null ? catalog.RareFooterFormat : "{0}", (float)seconds);
            else if (bonus.Source == BonusSource.Ad)
                detailFooter.SetText(catalog != null ? catalog.AdFooterFormat : "{0}", (float)seconds);
            else
                detailFooter.SetText(catalog != null ? catalog.ComboFooterFormat : "{0} {1}", (float)bonus.Tier, (float)seconds);
        }

        void HideDetail()
        {
            _detailIndex = -1;
            if (detail != null && detail.gameObject.activeSelf) detail.gameObject.SetActive(false);
        }
    }
}
