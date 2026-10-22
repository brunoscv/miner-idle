using MinersIdle.Core;
using MinersIdle.Game;
using TMPro;
using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// Feedback de mineração (M5, E3; REQ-UI-05: cor, forma e movimento). Só escuta a simulação e o toque e escolhe a
    /// reação; nenhuma regra mora aqui. Golpe: o cristal treme e solta faíscas. Toque: tremor menor, faísca no cristal e
    /// no dedo. Crítico: "CRÍTICO!" amarelo perto do anão e tremor de tela. Combo: "xN" pulsando sobre o anão, "pop" nos
    /// múltiplos de 10. Quebra: tremor leve e a lasca voando até o HUD. Com toques muito rápidos, os tremores diminuem.
    /// </summary>
    public sealed class MiningFeedbackView : MiningViewBase
    {
        static readonly Color HitSparkColor = new Color32(0xFF, 0xB3, 0x47, 0xFF);
        static readonly Color TapSparkColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        static readonly Color CriticalColor = new Color32(0xFF, 0xE0, 0x3A, 0xFF);
        static readonly Color MilestoneColor = new Color32(0xF2, 0xA9, 0x00, 0xFF);

        const float ComboPulseSeconds = 0.15f;

        [SerializeField] FeedbackTexts texts;
        [SerializeField] TapInput tapInput;
        [Tooltip("A descida (M6): diz onde o cristal e o anão estão na tela e faz o cristal tremer.")]
        [SerializeField] DescentStage stage;
        [SerializeField] Camera worldCamera;
        [SerializeField] ScreenShake screenShake;
        [SerializeField] SparkBurst sparks;
        [SerializeField] FloatingTexts floatingTexts;
        [SerializeField] ShardFlight shardFlight;
        [Tooltip("O \"xN\" que fica sobre o anão enquanto o combo dura.")]
        [SerializeField] TextMeshPro comboText;

        [Header("Tremor de tela (unidades de mundo; a tela tem ~17 de largura)")]
        [SerializeField] float criticalShake = 0.4f;
        [SerializeField] float criticalShakeSeconds = 0.3f;
        [SerializeField] float breakShake = 0.3f;
        [SerializeField] float breakShakeSeconds = 0.25f;
        [SerializeField] float milestoneShake = 0.25f;
        [SerializeField] float milestoneShakeSeconds = 0.2f;
        [Tooltip("Com muitos toques por segundo, os tremores caem até esta fração (1 = nunca diminuem).")]
        [SerializeField, Range(0f, 1f)] float minCalm = 0.6f;

        // Ritmo de toques: os últimos instantes de toque, para os tremores diminuírem quando o dedo vai muito rápido.
        readonly float[] _recentTaps = new float[16];
        int _tapCursor;
        float _comboPulse;

        public bool IsComboShown => comboText.gameObject.activeSelf;
        public string ComboShown => comboText.text;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (tapInput != null) tapInput.Tapped += OnTapped;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (tapInput != null) tapInput.Tapped -= OnTapped;
        }

        protected override void Subscribe(MiningSimulation simulation)
        {
            simulation.HitLanded += OnHit;
            simulation.CrystalBroken += OnBroken;
            simulation.ComboChanged += OnComboChanged;
            simulation.ComboMilestone += OnComboMilestone;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.HitLanded -= OnHit;
            simulation.CrystalBroken -= OnBroken;
            simulation.ComboChanged -= OnComboChanged;
            simulation.ComboMilestone -= OnComboMilestone;
        }

        protected override void Rebuild(MiningSimulation simulation) => OnComboChanged(simulation.Combo);

        void LateUpdate()
        {
            if (!comboText.gameObject.activeSelf) return;

            comboText.transform.position = stage.MinerOnScreen + new Vector3(0f, 0.8f, 0f);
            if (_comboPulse <= 0f) return;
            _comboPulse = Mathf.Max(0f, _comboPulse - Time.unscaledDeltaTime / ComboPulseSeconds);
            comboText.transform.localScale = Vector3.one * (1f + 0.35f * _comboPulse);
        }

        void OnHit(MiningHit hit)
        {
            var at = stage.CrystalOnScreen(hit.Crystal);
            float calm = Calm();

            stage.Shake(hit.Crystal, (hit.IsTap ? 0.03f : 0.06f) * calm); // metros no corredor
            sparks.Emit(at, hit.IsTap ? 3 : 5, hit.IsTap ? TapSparkColor : HitSparkColor, hit.IsTap ? 3f : 4f);

            if (!hit.IsCritical) return;
            sparks.Emit(at, 8, CriticalColor, 6f);
            var near = stage.MinerOnScreen + (Vector3)(Random.insideUnitCircle * 1.2f) + Vector3.up * 0.5f;
            floatingTexts.Show(texts.CriticalLabel, near, CriticalColor, 1.3f, 0.8f, 1.2f);
            screenShake.Shake(criticalShake * calm, criticalShakeSeconds);
        }

        void OnTapped(Vector2 screenPosition)
        {
            _recentTaps[_tapCursor] = Time.unscaledTime;
            _tapCursor = (_tapCursor + 1) % _recentTaps.Length;

            var world = worldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -worldCamera.transform.position.z));
            sparks.Emit(new Vector3(world.x, world.y, 0f), 2, TapSparkColor, 2f);
        }

        void OnBroken(Crystal crystal)
        {
            var at = stage.CrystalOnScreen(crystal);
            screenShake.Shake(breakShake * Calm(), breakShakeSeconds);
            sparks.Emit(at, 6, HitSparkColor, 5f);
            shardFlight.Launch(at);
        }

        void OnComboChanged(int combo)
        {
            bool show = combo > 1; // "x1" não diz nada; o combo aparece a partir do 2º toque
            comboText.gameObject.SetActive(show);
            if (!show) return;

            comboText.SetText(texts.ComboFormat, combo);
            _comboPulse = 1f;
        }

        void OnComboMilestone(int combo)
        {
            floatingTexts.ShowNumber(texts.MilestoneFormat, combo, stage.MinerOnScreen + Vector3.up * 1.6f,
                                     MilestoneColor, 1.6f, 0.9f, 1.5f);
            screenShake.Shake(milestoneShake, milestoneShakeSeconds);
        }

        /// <summary>1 com toques calmos; cai até <see cref="minCalm"/> quando há muitos toques no último segundo.</summary>
        float Calm()
        {
            float now = Time.unscaledTime;
            int recent = 0;
            for (int i = 0; i < _recentTaps.Length; i++)
                if (_recentTaps[i] > 0f && now - _recentTaps[i] < 1f) recent++;
            return Mathf.Lerp(1f, minCalm, Mathf.Clamp01((recent - 4) / 8f));
        }
    }
}
