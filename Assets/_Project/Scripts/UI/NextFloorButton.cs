using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// O Próximo Andar (M9c E2, D-269), embaixo à direita: o botão é a barra. Enchendo, mostra "PRÓXIMO ANDAR" e "x/y" e
    /// a madeira acende da esquerda para a direita com os cristais quebrados no andar; cheio, vira "DESCER · ANDAR n" com a
    /// seta, aceso e com a borda pulsando, e o toque pede o <see cref="MiningSimulation.AdvanceFloor"/> (a regra é do
    /// núcleo: aqui só se escuta e se desenha). No último andar leva ao coração; depois dele, some. Antes era o PASSAR
    /// provisório (<c>CavePassButton</c>, M8 → M9c E1).
    /// </summary>
    public sealed class NextFloorButton : MiningViewBase
    {
        [SerializeField] FeedbackTexts texts;
        [SerializeField] Button button;
        [Tooltip("O botão na tela inteiro (some depois do coração da montanha).")]
        [SerializeField] GameObject visual;
        [Tooltip("A máscara da madeira acesa: a largura dela é a barra (0 a 1 do botão).")]
        [SerializeField] RectTransform fill;
        [Tooltip("A madeira acesa, do tamanho do botão, dentro da máscara.")]
        [SerializeField] RectTransform lit;
        [Header("Enchendo")]
        [SerializeField] GameObject filling;
        [SerializeField] TMP_Text title;
        [FormerlySerializedAs("label")]
        [SerializeField] TMP_Text count;
        [Header("Cheio")]
        [SerializeField] GameObject ready;
        [SerializeField] TMP_Text readyTitle;
        [SerializeField] TMP_Text target;
        [Tooltip("A borda âmbar que pulsa com a barra cheia.")]
        [SerializeField] Graphic glow;
        [SerializeField, Min(0.1f)] float glowPeriod = 1.1f;
        [SerializeField, Range(0f, 1f)] float glowMinAlpha = 0.35f;
        [Header("AUTO (M9c E3, P6)")]
        [Tooltip("O selo do avanço automático, no canto de cima da barra.")]
        [SerializeField] GameObject autoChip;
        [SerializeField] TMP_Text autoLabel;

        float _fraction = -1f;
        long _done = -1, _goal = -1;
        int _target = -1;

        /// <summary>Se o botão está na tela (some só depois do coração).</summary>
        public bool IsShown => visual.activeSelf;
        /// <summary>Se a barra está cheia e o toque desce (o lado "DESCER · ANDAR n").</summary>
        public bool IsReady => ready.activeSelf;
        /// <summary>Quanto da madeira está acesa, de 0 a 1.</summary>
        public float FillFraction => Mathf.Max(0f, _fraction);
        /// <summary>O "x/y" na tela (para testes).</summary>
        public string CountText => count.text;
        /// <summary>O "ANDAR n" na tela (para testes).</summary>
        public string TargetText => target.text;
        /// <summary>Se o selo "AUTO" está na tela (o jogo desce sozinho, P6).</summary>
        public bool IsAutoShown => autoChip != null && autoChip.activeSelf;

        void Awake()
        {
            title.text = texts.NextFloorTitle;
            readyTitle.text = texts.NextFloorReady;
            if (autoLabel != null) autoLabel.text = texts.NextFloorAuto;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            button.onClick.AddListener(Advance);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            button.onClick.RemoveListener(Advance);
        }

        protected override void Subscribe(MiningSimulation simulation)
        {
            simulation.CrystalBroken += OnCrystalBroken;
            simulation.CaveChanged += OnFloorChanged;
            simulation.FloorTransitionEnded += Refresh;
            simulation.HeartReached += Refresh;
            simulation.PermanentsChanged += Refresh;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.CrystalBroken -= OnCrystalBroken;
            simulation.CaveChanged -= OnFloorChanged;
            simulation.FloorTransitionEnded -= Refresh;
            simulation.HeartReached -= Refresh;
            simulation.PermanentsChanged -= Refresh;
        }

        protected override void Rebuild(MiningSimulation simulation)
        {
            _fraction = -1f;
            _done = _goal = -1;
            _target = -1;
            Refresh();
        }

        void OnCrystalBroken(Crystal crystal) => Refresh();
        void OnFloorChanged(int floor) => Refresh();

        void Advance()
        {
            Simulation?.AdvanceFloor();
            Refresh();
        }

        void OnRectTransformDimensionsChange() => FitLit();

        /// <summary>A madeira acesa tem sempre a largura do botão: a máscara é que corta.</summary>
        void FitLit()
        {
            if (lit == null || visual == null) return;
            var size = ((RectTransform)visual.transform).rect.size;
            if (lit.rect.size != size)
            {
                lit.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
                lit.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
            }
        }

        void Refresh()
        {
            var simulation = Simulation;
            bool show = simulation != null && !simulation.State.ReachedHeart;
            if (visual.activeSelf != show) visual.SetActive(show);
            if (!show) return;

            bool canAdvance = simulation.CanAdvance;
            button.interactable = canAdvance;
            if (ready.activeSelf != canAdvance) ready.SetActive(canAdvance);
            if (filling.activeSelf == canAdvance) filling.SetActive(!canAdvance);
            if (glow.enabled != canAdvance) glow.enabled = canAdvance;

            float fraction = canAdvance ? 1f : (float)simulation.FloorProgress;
            if (!Mathf.Approximately(fraction, _fraction))
            {
                _fraction = fraction;
                fill.anchorMax = new Vector2(fraction, 1f);
                FitLit();
            }

            if (canAdvance)
            {
                int next = simulation.State.Cave + 1;
                if (next == _target) return;
                _target = next;
                if (next > simulation.Rules.CaveCount) target.text = texts.NextFloorHeart;
                else target.SetText(texts.NextFloorTargetFormat, next); // sem alocar
                return;
            }

            long goal = simulation.RequiredBreaks;
            long done = System.Math.Min(simulation.State.CaveBreaks, goal);
            if (done == _done && goal == _goal) return;
            _done = done;
            _goal = goal;
            count.SetText(texts.NextFloorCountFormat, done, goal); // TMP sem alocar: "{0}/{1}" com 0 casas
        }

        void Update()
        {
            // O selo "AUTO": o marco do andar 5 e o ajuste ligado (pode mudar nos AJUSTES ou no DBG a qualquer hora).
            if (autoChip != null)
            {
                var simulation = Simulation;
                bool auto = simulation != null && visual.activeSelf && simulation.IsAutoAdvancing;
                if (autoChip.activeSelf != auto) autoChip.SetActive(auto);
            }

            if (!glow.enabled) return;
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (2f * Mathf.PI / glowPeriod));
            glow.canvasRenderer.SetAlpha(Mathf.Lerp(glowMinAlpha, 1f, wave));
        }
    }
}
