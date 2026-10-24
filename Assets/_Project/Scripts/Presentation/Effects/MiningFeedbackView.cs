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
    ///
    /// M6 (E4): na vista de frente o anão e o cristal ficam muitas vezes maiores na tela que no mapa visto de cima; as
    /// faíscas, os textos e a lasca crescem por <see cref="sizeScale"/>, e o impacto ganha força: tremor leve a cada golpe,
    /// estilhaços na cor do cristal e um clarão branco na tela na quebra (no crítico, só o tremor; pedido do Bruno). <see cref="Intensity"/> (DBG) multiplica
    /// quantidades, tremores e clarões, para calibrar no aparelho.
    /// </summary>
    public sealed class MiningFeedbackView : MiningViewBase
    {
        static readonly Color HitSparkColor = new Color32(0xFF, 0xB3, 0x47, 0xFF);
        static readonly Color TapSparkColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        static readonly Color CriticalColor = new Color32(0xFF, 0xE0, 0x3A, 0xFF);
        static readonly Color MilestoneColor = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        static readonly Color BreakFlashColor = Color.white;

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
        [Tooltip("Quadrado branco do tamanho da tela, por cima de tudo: o clarão da quebra.")]
        [SerializeField] SpriteRenderer flash;

        [Header("Força na vista de frente (M6, E4)")]
        [Tooltip("Faíscas, textos e lasca em relação à vista de cima do mapa (o cristal e o anão ficaram ~4× maiores).")]
        [SerializeField] float sizeScale = 3.5f;
        [SerializeField] float hitShake = 0.12f;
        [SerializeField] float hitShakeSeconds = 0.08f;
        [SerializeField, Range(0f, 1f)] float breakFlash = 0.22f;
        [SerializeField] float flashSeconds = 0.12f;

        [Header("Tremor de tela (unidades de mundo; a tela tem ~17 de largura)")]
        [SerializeField] float criticalShake = 1.0f;
        [SerializeField] float criticalShakeSeconds = 0.3f;
        [SerializeField] float breakShake = 0.8f;
        [SerializeField] float breakShakeSeconds = 0.25f;
        [SerializeField] float milestoneShake = 0.6f;
        [SerializeField] float milestoneShakeSeconds = 0.2f;
        [Tooltip("Com muitos toques por segundo, os tremores caem até esta fração (1 = nunca diminuem).")]
        [SerializeField, Range(0f, 1f)] float minCalm = 0.6f;

        // Ritmo de toques: os últimos instantes de toque, para os tremores diminuírem quando o dedo vai muito rápido.
        readonly float[] _recentTaps = new float[16];
        int _tapCursor;
        float _comboPulse;
        float _flash;
        float _flashAlpha;
        Color _flashColor;

        float _intensity = 1f;

        /// <summary>Multiplica quantidades, tremores e clarões (o ajuste "Intensidade dos efeitos" do DBG, M6 E4).</summary>
        public float Intensity
        {
            get => _intensity;
            set => _intensity = Mathf.Clamp(value, 0.25f, 3f);
        }

        public bool IsFlashing => _flash > 0f;

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
            UpdateFlash();
            if (!comboText.gameObject.activeSelf) return;

            comboText.transform.position = stage.MinerOnScreen + Vector3.up * (0.8f * sizeScale);
            if (_comboPulse <= 0f) return;
            _comboPulse = Mathf.Max(0f, _comboPulse - Time.unscaledDeltaTime / ComboPulseSeconds);
            comboText.transform.localScale = Vector3.one * (ComboScale * (1f + 0.35f * _comboPulse));
        }

        /// <summary>O "xN" cresce menos que os outros efeitos: fica sobre o anão o tempo todo do combo.</summary>
        float ComboScale => sizeScale * 0.6f;

        /// <summary>O clarão cobre a tela da câmera e some rápido.</summary>
        void UpdateFlash()
        {
            if (flash == null || _flash <= 0f) return;

            _flash = Mathf.Max(0f, _flash - Time.unscaledDeltaTime / flashSeconds);
            var color = _flashColor;
            color.a = _flashAlpha * _flash;
            flash.color = color;
            flash.enabled = _flash > 0f;

            var cameraPosition = worldCamera.transform.position;
            float height = worldCamera.orthographicSize * 2.2f; // com folga para o tremor
            flash.transform.position = new Vector3(cameraPosition.x, cameraPosition.y, 0f);
            flash.transform.localScale = new Vector3(height * worldCamera.aspect, height, 1f);
        }

        void Flash(Color color, float alpha)
        {
            if (flash == null || alpha <= 0f) return;
            _flashColor = color;
            _flashAlpha = Mathf.Clamp01(Mathf.Max(alpha * _intensity, _flashAlpha * _flash));
            _flash = 1f;
        }

        /// <summary>Quantas peças, com a intensidade do DBG (pelo menos uma).</summary>
        int Count(int baseCount) => Mathf.Max(1, Mathf.RoundToInt(baseCount * _intensity));

        void OnHit(MiningHit hit)
        {
            var at = stage.CrystalOnScreen(hit.Crystal);
            float calm = Calm();
            float size = SparkBurst.DefaultSize * sizeScale;

            stage.Shake(hit.Crystal, (hit.IsTap ? 0.05f : 0.1f) * calm * _intensity); // metros no corredor
            if (hit.IsTap)
            {
                sparks.Emit(at, Count(4), TapSparkColor, 3f * sizeScale, size * 0.8f);
            }
            else
            {
                sparks.Emit(at, Count(9), HitSparkColor, 4f * sizeScale, size);
                screenShake.Shake(hitShake * calm * _intensity, hitShakeSeconds);
            }

            if (!hit.IsCritical) return;
            sparks.Emit(at, Count(18), CriticalColor, 6f * sizeScale, size * 1.4f, 0.5f);
            var near = stage.MinerOnScreen + (Vector3)(Random.insideUnitCircle * 1.2f * sizeScale) + Vector3.up * (0.5f * sizeScale);
            floatingTexts.Show(texts.CriticalLabel, near, CriticalColor, 1.05f * sizeScale * 0.65f, 0.85f, 1.2f * sizeScale); // um pouco menor (pedido do Bruno)
            screenShake.Shake(criticalShake * calm * _intensity, criticalShakeSeconds); // sem clarão: só a quebra pisca (pedido do Bruno)
        }

        void OnTapped(Vector2 screenPosition)
        {
            _recentTaps[_tapCursor] = Time.unscaledTime;
            _tapCursor = (_tapCursor + 1) % _recentTaps.Length;

            var world = worldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -worldCamera.transform.position.z));
            sparks.Emit(new Vector3(world.x, world.y, 0f), Count(3), TapSparkColor, 2f * sizeScale, SparkBurst.DefaultSize * sizeScale * 0.6f);
        }

        void OnBroken(Crystal crystal)
        {
            var at = stage.CrystalOnScreen(crystal);
            float calm = Calm();
            float size = SparkBurst.DefaultSize * sizeScale;
            Color shards = stage.CrystalShardColor(crystal.Id);

            screenShake.Shake(breakShake * calm * _intensity, breakShakeSeconds);
            sparks.Emit(at, Count(10), HitSparkColor, 5f * sizeScale, size);
            sparks.Emit(at, Count(18), shards, 6f * sizeScale, size * 2f, 0.6f); // estilhaços na cor do cristal
            Flash(BreakFlashColor, breakFlash * calm);
            shardFlight.Launch(at, sizeScale * 0.7f);
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
            floatingTexts.ShowNumber(texts.MilestoneFormat, combo, stage.MinerOnScreen + Vector3.up * (1.6f * sizeScale),
                                     MilestoneColor, 1.6f * sizeScale * 0.65f, 0.9f, 1.5f * sizeScale);
            screenShake.Shake(milestoneShake * _intensity, milestoneShakeSeconds);
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
