using System.Collections.Generic;
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
    /// faíscas, os textos e a lasca crescem por <see cref="sizeScale"/>. <see cref="Intensity"/> (DBG) multiplica
    /// quantidades e tremores, para calibrar no aparelho. Sem clarão de tela: nem no crítico nem na quebra (D-81, D-83).
    ///
    /// M8 (E2): a parede da passagem apanha como um cristal; ao quebrar, estilhaços maiores de lado a lado do corredor e um
    /// tremor mais forte, sem a lasca voando (a parede não paga lascas, D-108).
    ///
    /// M7 (E2, D-171): o golpe do estagiário é mais leve que o do anão: poucas faíscas cinza, o cristal treme pouco, sem
    /// tremor de tela; o dano sai no cristal, num número menor e cinza.
    ///
    /// M9b (E2, D-215): ao zerar o combo com bônus, o ganho sobe de onde estava o "xN" ("+25% DANO", "CRÍTICO GARANTIDO");
    /// no marco com texto grande no catálogo (o ×100), "FRENESI!" maior, com tremor leve. Sem vibração.
    /// </summary>
    public sealed class MiningFeedbackView : MiningViewBase
    {
        static readonly Color HitSparkColor = new Color32(0xFF, 0xB3, 0x47, 0xFF);
        static readonly Color TapSparkColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        static readonly Color CriticalColor = new Color32(0xFF, 0xE0, 0x3A, 0xFF);
        static readonly Color MilestoneColor = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        static readonly Color InternColor = new Color32(0xB8, 0xBC, 0xC2, 0xFF);
        static readonly Color HeadlineColor = new Color32(0xE8, 0x4A, 0x2A, 0xFF);

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
        [Tooltip("Os textos do ganho e o texto grande de cada marco (M9b).")]
        [SerializeField] BuffCatalog buffs;

        [Header("Força na vista de frente (M6, E4)")]
        [Tooltip("Faíscas, textos e lasca em relação à vista de cima do mapa (o cristal e o anão ficaram ~4× maiores).")]
        [SerializeField] float sizeScale = 3.5f;

        [Header("Tremor de tela (unidades de mundo; a tela tem ~17 de largura)")]
        [Tooltip("A cada golpe do anão (D-83).")]
        [SerializeField] float hitShake = 0.25f;
        [SerializeField] float hitShakeSeconds = 0.12f;
        [SerializeField] float criticalShake = 0.4f;
        [SerializeField] float criticalShakeSeconds = 0.3f;
        [SerializeField] float breakShake = 0.5f;
        [SerializeField] float breakShakeSeconds = 0.25f;
        [Tooltip("Na quebra da parede da passagem (M8).")]
        [SerializeField] float wallBreakShake = 0.9f;
        [SerializeField] float wallBreakShakeSeconds = 0.45f;
        [SerializeField] float milestoneShake = 0.25f;
        [SerializeField] float milestoneShakeSeconds = 0.2f;
        [Tooltip("No texto grande do ganho (o \"FRENESI!\" do ×100, D-215).")]
        [SerializeField] float headlineShake = 0.35f;
        [SerializeField] float headlineShakeSeconds = 0.35f;
        [Tooltip("Com muitos toques por segundo, os tremores caem até esta fração (1 = nunca diminuem).")]
        [SerializeField, Range(0f, 1f)] float minCalm = 0.6f;

        // Ritmo de toques: os últimos instantes de toque, para os tremores diminuírem quando o dedo vai muito rápido.
        readonly float[] _recentTaps = new float[16];
        int _tapCursor;
        float _comboPulse;

        float _intensity = 1f;

        /// <summary>Multiplica quantidades e tremores (o ajuste "Intensidade dos efeitos" do DBG, M6 E4).</summary>
        public float Intensity
        {
            get => _intensity;
            set => _intensity = Mathf.Clamp(value, 0.25f, 3f);
        }

        /// <summary>Quantos números de golpe do estagiário já subiram (para testes).</summary>
        public int InternNumbersShown { get; private set; }

        /// <summary>A cor do número dos golpes do estagiário (cinza, D-171).</summary>
        public static Color InternNumberColor => InternColor;

        public bool IsComboShown => comboText.gameObject.activeSelf;
        public string ComboShown => comboText.text;

        /// <summary>O último texto do ganho e o último texto grande (M9b; para testes). <c>null</c> = nenhum.</summary>
        public string LastGainText { get; private set; }
        public string LastHeadline { get; private set; }

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
            simulation.InternHitLanded += OnInternHit;
            simulation.CrystalBroken += OnBroken;
            simulation.ComboChanged += OnComboChanged;
            simulation.ComboMilestone += OnComboMilestone;
            simulation.ComboBonusesGranted += OnBonusesGranted;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.HitLanded -= OnHit;
            simulation.InternHitLanded -= OnInternHit;
            simulation.CrystalBroken -= OnBroken;
            simulation.ComboChanged -= OnComboChanged;
            simulation.ComboMilestone -= OnComboMilestone;
            simulation.ComboBonusesGranted -= OnBonusesGranted;
        }

        protected override void Rebuild(MiningSimulation simulation) => OnComboChanged(simulation.Combo);

        void LateUpdate()
        {
            if (!comboText.gameObject.activeSelf) return;

            comboText.transform.position = stage.MinerOnScreen + Vector3.up * (0.8f * sizeScale);
            if (_comboPulse <= 0f) return;
            _comboPulse = Mathf.Max(0f, _comboPulse - Time.unscaledDeltaTime / ComboPulseSeconds);
            comboText.transform.localScale = Vector3.one * (ComboScale * (1f + 0.35f * _comboPulse));
        }

        /// <summary>O "xN" cresce menos que os outros efeitos: fica sobre o anão o tempo todo do combo.</summary>
        float ComboScale => sizeScale * 0.6f;

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
            screenShake.Shake(criticalShake * calm * _intensity, criticalShakeSeconds);
        }

        /// <summary>
        /// O golpe do estagiário (M7, E2, D-171): faíscas cinza e poucas, o cristal treme de leve e o dano sobe do cristal
        /// num número menor e cinza. Sem tremor de tela: o anão continua sendo o golpe forte.
        /// </summary>
        void OnInternHit(MiningHit hit)
        {
            var at = stage.CrystalOnScreen(hit.Crystal);
            float size = SparkBurst.DefaultSize * sizeScale;
            stage.Shake(hit.Crystal, 0.04f * _intensity);
            sparks.Emit(at, Count(4), InternColor, 3f * sizeScale, size * 0.7f);

            var near = at + (Vector3)(Random.insideUnitCircle * 0.4f * sizeScale) + Vector3.up * (0.4f * sizeScale);
            floatingTexts.Show(NumberFormatter.Format(hit.Damage), near, InternColor, 0.45f * sizeScale, 0.7f, 0.8f * sizeScale);
            InternNumbersShown++;
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
            if (crystal.IsWall)
            {
                OnWallBroken(crystal);
                return;
            }

            var at = stage.CrystalOnScreen(crystal);
            float calm = Calm();
            float size = SparkBurst.DefaultSize * sizeScale;
            Color shards = stage.CrystalShardColor(crystal.Id);

            screenShake.Shake(breakShake * calm * _intensity, breakShakeSeconds);
            sparks.Emit(at, Count(10), HitSparkColor, 5f * sizeScale, size);
            sparks.Emit(at, Count(18), shards, 6f * sizeScale, size * 2f, 0.6f); // estilhaços na cor do cristal
            shardFlight.Launch(at, sizeScale * 0.7f);
        }

        /// <summary>A parede caiu: estilhaços grandes em três pontos do corredor (esquerda, meio, direita) e tremor forte.</summary>
        void OnWallBroken(Crystal wall)
        {
            float size = SparkBurst.DefaultSize * sizeScale;
            var middle = stage.CrystalLocalPosition(wall) + Vector3.up * 0.5f;
            screenShake.Shake(wallBreakShake * _intensity, wallBreakShakeSeconds);
            for (int side = -1; side <= 1; side++)
            {
                var at = stage.ToScreenWorld(middle + Vector3.right * (side * 0.9f));
                sparks.Emit(at, Count(12), HitSparkColor, 6f * sizeScale, size * 1.2f);
                sparks.Emit(at, Count(24), stage.MixedCrystalShardColor(wall.Id + side + 1), 8f * sizeScale, size * 3f, 0.8f);
            }
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

        /// <summary>
        /// O ganho ao zerar o combo (D-215): a soma do dano que entrou e/ou o crítico garantido, subindo de onde estava o
        /// "xN"; com o texto grande de um marco (o "FRENESI!"), ele sobe acima, maior, com tremor leve (obedece aos AJUSTES).
        /// </summary>
        void OnBonusesGranted(int combo, IReadOnlyList<ComboBonusRule> granted)
        {
            if (buffs == null) return;
            double damage = 0;
            bool critical = false;
            string headline = null;
            for (int i = 0; i < granted.Count; i++)
            {
                var rule = granted[i];
                if (rule.Kind == BonusKind.DamagePercent) damage += rule.Value;
                else if (rule.Kind == BonusKind.GuaranteedCritical) critical = true;
                var entry = buffs.Find(BonusSource.Combo, rule.Combo);
                if (entry != null && !string.IsNullOrEmpty(entry.Headline)) headline = entry.Headline;
            }

            string gain = damage > 0 && critical ? string.Format(buffs.GainBothFormat, NumberFormatter.FormatPercent(damage))
                        : damage > 0 ? string.Format(buffs.GainDamageFormat, NumberFormatter.FormatPercent(damage))
                        : critical ? buffs.GainCritical
                        : null;
            var at = stage.MinerOnScreen + Vector3.up * (0.8f * sizeScale);
            if (gain != null)
                floatingTexts.Show(gain, at, MilestoneColor, 0.9f * sizeScale * 0.65f, 1.2f, 1.2f * sizeScale);
            if (headline != null)
            {
                floatingTexts.Show(headline, at + Vector3.up * (0.9f * sizeScale), HeadlineColor, 1.8f * sizeScale * 0.65f, 1.3f,
                                   1.4f * sizeScale);
                screenShake.Shake(headlineShake * _intensity, headlineShakeSeconds);
            }
            LastGainText = gain;
            LastHeadline = headline;
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
