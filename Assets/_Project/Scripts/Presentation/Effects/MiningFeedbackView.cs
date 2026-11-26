using System.Collections.Generic;
using MinersIdle.Core;
using MinersIdle.Game;
using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// Feedback de mineração (M5, E3; REQ-UI-05: cor, forma e movimento). Só escuta a simulação e o dedo e escolhe a
    /// reação; nenhuma regra mora aqui. Golpe: o cristal treme e solta faíscas. Dedo na tela: só a faísca no dedo (o toque
    /// e o combo saíram no M9g, D-311). Crítico: "CRÍTICO!" amarelo perto do anão e tremor de tela. Quebra: tremor leve e
    /// a lasca voando até o HUD.
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
    /// M9b (E4, E5): a carta do raro e o boost do AD sobem do anão em âmbar ("DANO ×2").
    ///
    /// M9g (D-316): a bomba estoura em cada cristal que pegou: faíscas laranja e amarelas, o cristal treme forte, o dano
    /// sobe em laranja e a tela treme uma vez por bomba (não uma por cristal).
    ///
    /// M9i #24 (D-346), os efeitos limpos: com a Velocidade e o Crítico no máximo (andares 40–50), cada golpe a cada 0,1 s
    /// soltava 9 + 18 faíscas, um "CRÍTICO!" e um tremor, e a tela virava um borrão dourado (a foto do Apellon). Agora:
    /// <list type="bullet">
    /// <item><b>faíscas no ritmo do golpe:</b> o tempo médio entre os golpes do anão (tempo real) abaixo de
    /// <see cref="calmRhythmSeconds"/> diminui as faíscas do golpe e do crítico na mesma proporção, até
    /// <see cref="calmMinFactor"/>; o crítico fica também menor. Com um golpe a cada 2 s (o começo), nada muda;</item>
    /// <item><b>crítico discreto:</b> no máximo um "CRÍTICO!" e um tremor de tela de golpe a cada <see cref="calmTextGap"/>;</item>
    /// <item><b>tetos:</b> no máximo <see cref="calmSparkLimit"/> faíscas na tela e um número do estagiário a cada
    /// <see cref="calmInternNumberGap"/>.</item>
    /// </list>
    /// A quebra, a rachadura e a bomba seguem como eram (só o teto das faíscas vale para elas). DBG → Efeitos → "Efeitos:
    /// limpos/antigos" (<see cref="Calm"/>) volta ao jeito antigo para comparar.
    /// </summary>
    public sealed class MiningFeedbackView : MiningViewBase
    {
        static readonly Color HitSparkColor = new Color32(0xFF, 0xB3, 0x47, 0xFF);
        static readonly Color TapSparkColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        static readonly Color CriticalColor = new Color32(0xFF, 0xE0, 0x3A, 0xFF);
        static readonly Color MilestoneColor = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        static readonly Color InternColor = new Color32(0xB8, 0xBC, 0xC2, 0xFF);
        static readonly Color BombColor = new Color32(0xFF, 0x7A, 0x1F, 0xFF);

        [SerializeField] FeedbackTexts texts;
        [SerializeField] TapInput tapInput;
        [Tooltip("A descida (M6): diz onde o cristal e o anão estão na tela e faz o cristal tremer.")]
        [SerializeField] DescentStage stage;
        [SerializeField] Camera worldCamera;
        [SerializeField] ScreenShake screenShake;
        [SerializeField] SparkBurst sparks;
        [SerializeField] FloatingTexts floatingTexts;
        [SerializeField] ShardFlight shardFlight;
        [Tooltip("Os textos do ganho da carta do raro e do AD (M9b).")]
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
        [Tooltip("O estouro da bomba (M9g, D-316): uma vez por lançamento.")]
        [SerializeField] float bombShake = 0.6f;
        [SerializeField] float bombShakeSeconds = 0.3f;

        [Header("Efeitos limpos (M9i #24, D-346)")]
        [Tooltip("Abaixo deste tempo entre golpes (segundos reais), as faíscas do golpe e do crítico diminuem na proporção.")]
        [SerializeField] float calmRhythmSeconds = 0.5f;
        [Tooltip("O mínimo da proporção: com um golpe a cada 0,1 s, 9 faíscas viram 2 e 18 viram 4.")]
        [SerializeField] float calmMinFactor = 0.2f;
        [Tooltip("No máximo um \"CRÍTICO!\" e um tremor de golpe a cada tantos segundos.")]
        [SerializeField] float calmTextGap = 0.5f;
        [Tooltip("No máximo um número do estagiário a cada tantos segundos.")]
        [SerializeField] float calmInternNumberGap = 0.3f;
        [Tooltip("Faíscas na tela ao mesmo tempo, no máximo.")]
        [SerializeField] int calmSparkLimit = 40;
        [Tooltip("Lascas pelo dano (M9k, D-374): no máximo uma lasca voando do golpe a cada tantos segundos (~4 por segundo).")]
        [SerializeField] float calmShardGap = 0.25f;
        [Tooltip("Lascas pelo dano (M9k, D-374): quantas lascas a bomba solta, uma por cristal atingido, no máximo.")]
        [SerializeField] int calmBombShards = 3;

        float _intensity = 1f;
        bool _calm = true;
        float _hitGap = 2f;            // média dos segundos reais entre os golpes do anão
        float _lastHitAt = -1f;
        float _lastCriticalTextAt = -10f;
        float _lastHitShakeAt = -10f;
        float _lastInternNumberAt = -10f;
        float _lastShardAt = -10f;
        int _bombShards;               // lascas da bomba desta explosão (D-374)

        /// <summary>
        /// Os efeitos limpos (D-346), ligados de fábrica. Desligado (DBG → Efeitos → "Efeitos: antigos"), volta ao jeito de
        /// antes do M9i #24: todas as faíscas, todo "CRÍTICO!" e todo tremor, sem teto.
        /// </summary>
        public bool Calm
        {
            get => _calm;
            set
            {
                _calm = value;
                if (sparks != null) sparks.Limit = value ? calmSparkLimit : 0;
            }
        }

        /// <summary>A proporção das faíscas do golpe agora (1 = todas; para os testes e o DBG).</summary>
        public float RhythmFactor => _calm ? Mathf.Clamp(_hitGap / Mathf.Max(0.01f, calmRhythmSeconds), calmMinFactor, 1f) : 1f;

        /// <summary>Quantos "CRÍTICO!" já subiram (para os testes).</summary>
        public int CriticalTextsShown { get; private set; }

        void Awake() => Calm = _calm;

        /// <summary>Multiplica quantidades e tremores (o ajuste "Intensidade dos efeitos" do DBG, M6 E4).</summary>
        public float Intensity
        {
            get => _intensity;
            set => _intensity = Mathf.Clamp(value, 0.25f, 3f);
        }

        /// <summary>Quantos números de golpe do estagiário já subiram (para testes).</summary>
        public int InternNumbersShown { get; private set; }

        /// <summary>Quantas lascas voaram dos golpes e da bomba (M9k, D-374; para os testes).</summary>
        public int ShardsFlown { get; private set; }

        /// <summary>O balanço paga a cada golpe (GDD v8.4, D-348): a lasca voa do golpe, não da quebra.</summary>
        bool PaysPerHit => Simulation != null && Simulation.Rules.PaysPerHit;

        /// <summary>Uma lasca voa do cristal até o LASCAS do HUD.</summary>
        void LaunchShard(Vector3 at)
        {
            shardFlight.Launch(at, sizeScale * 0.7f);
            ShardsFlown++;
        }

        /// <summary>A cor do número dos golpes do estagiário (cinza, D-171).</summary>
        public static Color InternNumberColor => InternColor;

        /// <summary>O último texto do ganho (a carta do raro ou o AD; para testes). <c>null</c> = nenhum.</summary>
        public string LastGainText { get; private set; }

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
            simulation.RareCardApplied += OnRareCardApplied;
            simulation.AdBoostGranted += OnAdBoostGranted;
            simulation.BombHitLanded += OnBombHit;
            simulation.BombLanded += OnBombLanded;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.HitLanded -= OnHit;
            simulation.InternHitLanded -= OnInternHit;
            simulation.CrystalBroken -= OnBroken;
            simulation.RareCardApplied -= OnRareCardApplied;
            simulation.AdBoostGranted -= OnAdBoostGranted;
            simulation.BombHitLanded -= OnBombHit;
            simulation.BombLanded -= OnBombLanded;
        }

        protected override void Rebuild(MiningSimulation simulation) { }

        /// <summary>Quantas peças, com a intensidade do DBG (pelo menos uma).</summary>
        int Count(int baseCount) => Mathf.Max(1, Mathf.RoundToInt(baseCount * _intensity));

        /// <summary>Como <see cref="Count"/>, já na proporção do ritmo do golpe (D-346).</summary>
        int Count(int baseCount, float factor) => Mathf.Max(1, Mathf.RoundToInt(baseCount * _intensity * factor));

        /// <summary>Mede o ritmo dos golpes do anão em tempo real (a tela é o que importa, também com o DBG ×20).</summary>
        void MeasureRhythm(float now)
        {
            if (_lastHitAt >= 0f) _hitGap = Mathf.Lerp(_hitGap, Mathf.Min(now - _lastHitAt, 2f), 0.35f);
            _lastHitAt = now;
        }

        void OnHit(MiningHit hit)
        {
            var at = stage.CrystalOnScreen(hit.Crystal);
            float size = SparkBurst.DefaultSize * sizeScale;
            float now = Time.unscaledTime;
            MeasureRhythm(now);
            float factor = RhythmFactor;

            stage.Shake(hit.Crystal, 0.1f * _intensity); // metros no corredor
            ShowCrack(hit, at);
            sparks.Emit(at, Count(9, factor), HitSparkColor, 4f * sizeScale, size);

            // Lascas pelo dano (M9k, D-374): uma lasca por golpe, no limpo no máximo uma a cada calmShardGap.
            if (PaysPerHit && (!_calm || now - _lastShardAt >= calmShardGap))
            {
                LaunchShard(at);
                _lastShardAt = now;
            }

            // O tremor do golpe (e o do crítico, mais forte, no lugar dele): no limpo, no máximo um a cada calmTextGap.
            bool shakeFree = !_calm || now - _lastHitShakeAt >= calmTextGap;
            if (shakeFree)
            {
                if (hit.IsCritical) screenShake.Shake(criticalShake * _intensity, criticalShakeSeconds);
                else screenShake.Shake(hitShake * _intensity, hitShakeSeconds);
                _lastHitShakeAt = now;
            }

            if (!hit.IsCritical) return;
            // Golpe rápido, crítico menor: de 1,4× (o de sempre) até 1× o tamanho da faísca do golpe.
            float criticalSize = _calm ? Mathf.Lerp(1f, 1.4f, Mathf.InverseLerp(calmMinFactor, 1f, factor)) : 1.4f;
            sparks.Emit(at, Count(18, factor), CriticalColor, 6f * sizeScale, size * criticalSize, 0.5f);
            if (_calm && now - _lastCriticalTextAt < calmTextGap) return;
            var near = stage.MinerOnScreen + (Vector3)(Random.insideUnitCircle * 1.2f * sizeScale) + Vector3.up * (0.5f * sizeScale);
            floatingTexts.Show(texts.CriticalLabel, near, CriticalColor, 1.05f * sizeScale * 0.65f, 0.85f, 1.2f * sizeScale); // um pouco menor (pedido do Bruno)
            _lastCriticalTextAt = now;
            CriticalTextsShown++;
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
            ShowCrack(hit, at);

            float now = Time.unscaledTime;
            if (_calm && now - _lastInternNumberAt < calmInternNumberGap) return; // os números não se empilham (D-346)
            _lastInternNumberAt = now;
            var near = at + (Vector3)(Random.insideUnitCircle * 0.4f * sizeScale) + Vector3.up * (0.4f * sizeScale);
            floatingTexts.Show(NumberFormatter.Format(hit.Damage), near, InternColor, 0.45f * sizeScale, 0.7f, 0.8f * sizeScale);
            InternNumbersShown++;
        }

        /// <summary>Quantos estouros de bomba já saíram, um por cristal atingido (para os testes).</summary>
        public int BombBlastsShown { get; private set; }

        /// <summary>O estouro da bomba num cristal (M9g, D-316): fogo e o dano em laranja, sobre o cristal.</summary>
        void OnBombHit(MiningHit hit)
        {
            var at = stage.CrystalOnScreen(hit.Crystal);
            float size = SparkBurst.DefaultSize * sizeScale;
            stage.Shake(hit.Crystal, 0.2f * _intensity);
            stage.FlashCrystal(hit.Crystal); // o clarão em cada cristal atingido
            ShowCrack(hit, at);
            sparks.Emit(at, Count(16), BombColor, 6f * sizeScale, size * 1.6f, 0.5f);
            sparks.Emit(at, Count(10), CriticalColor, 4f * sizeScale, size * 1.1f, 0.35f);

            var near = at + Vector3.up * (0.6f * sizeScale);
            floatingTexts.Show(NumberFormatter.Format(hit.Damage), near, BombColor, 0.7f * sizeScale, 0.9f, 1f * sizeScale);
            BombBlastsShown++;

            // Lascas pelo dano (M9k, D-374): uma por cristal atingido, no limpo até calmBombShards por explosão.
            if (PaysPerHit && (!_calm || _bombShards < calmBombShards))
            {
                LaunchShard(at);
                _bombShards++;
            }
        }

        void OnBombLanded(IReadOnlyList<Crystal> hit)
        {
            _bombShards = 0; // a explosão acabou (o BombLanded sai depois de todos os BombHitLanded)
            if (hit.Count > 0) screenShake.Shake(bombShake * _intensity, bombShakeSeconds);
        }

        /// <summary>Quantas rachaduras novas mostraram o brilho e as lascas (para os testes).</summary>
        public int CracksShown { get; private set; }

        /// <summary>
        /// A rachadura nova (M9f E2, D-303): o golpe passou o cristal para o estágio seguinte do desenho (D-300). O cristal
        /// brilha um instante e solta 3 a 4 lascas na cor dele, menores e mais curtas que os estilhaços da quebra. O golpe
        /// que quebra não passa por aqui: a quebra tem o efeito dela.
        /// </summary>
        void ShowCrack(MiningHit hit, Vector3 at)
        {
            var crystal = hit.Crystal;
            if (!stage.DrawsCracks(crystal) || !CrackStage.Crossed(crystal.Health, hit.Damage, crystal.MaxHealth, out _)) return;
            stage.FlashCrystal(crystal);
            float size = SparkBurst.DefaultSize * sizeScale;
            sparks.Emit(at, Count(4), stage.ShardColorOf(crystal), 3.5f * sizeScale, size * 1.6f, 0.45f);
            CracksShown++;
        }

        void OnTapped(Vector2 screenPosition)
        {
            var world = worldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -worldCamera.transform.position.z));
            sparks.Emit(new Vector3(world.x, world.y, 0f), Count(3), TapSparkColor, 2f * sizeScale, SparkBurst.DefaultSize * sizeScale * 0.6f);
        }

        void OnBroken(Crystal crystal)
        {
            var at = stage.CrystalOnScreen(crystal);
            float size = SparkBurst.DefaultSize * sizeScale;
            Color shards = stage.ShardColorOf(crystal);

            screenShake.Shake(breakShake * _intensity, breakShakeSeconds);
            sparks.Emit(at, Count(10), HitSparkColor, 5f * sizeScale, size);
            sparks.Emit(at, Count(18), shards, 6f * sizeScale, size * 2f, 0.6f); // estilhaços na cor do cristal
            if (!PaysPerHit) shardFlight.Launch(at, sizeScale * 0.7f); // no v8.4 a lasca já voou nos golpes (D-374)
        }

        /// <summary>A carta escolhida sobe do anão com o efeito no tier de agora ({0}: "DANO ×2", ×4 no tier 2; M9e E1).</summary>
        void OnRareCardApplied(RareCard card)
        {
            var gain = buffs != null ? buffs.Find(BonusSource.Rare, (int)card)?.Gain : null;
            var simulation = Simulation;
            if (gain != null && simulation != null && simulation.Strike.Rare.Has(card))
            {
                var rule = simulation.Strike.Rare.Get(card, simulation.RareTier);
                gain = string.Format(gain, NumberFormatter.FormatEffect(rule.Kind, rule.Value));
            }
            ShowGain(gain);
        }

        /// <summary>O boost do AD entrou (D-231): "VELOCIDADE ×2,5" sobe do anão em âmbar.</summary>
        void OnAdBoostGranted() => ShowGain(buffs != null ? buffs.Find(BonusSource.Ad, 0)?.Gain : null);

        void ShowGain(string gain)
        {
            if (string.IsNullOrEmpty(gain)) return;
            floatingTexts.Show(gain, stage.MinerOnScreen + Vector3.up * (0.8f * sizeScale), MilestoneColor,
                               0.9f * sizeScale * 0.65f, 1.2f, 1.2f * sizeScale);
            LastGainText = gain;
        }
    }
}
