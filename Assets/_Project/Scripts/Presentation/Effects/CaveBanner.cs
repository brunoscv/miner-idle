using MinersIdle.Core;
using MinersIdle.Game;
using TMPro;
using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// A faixa da caverna nova (M8, E2, D-111): "Caverna 2 · Galeria rochosa" no alto da tela, quando a passagem termina.
    /// Aparece no escuro da troca de bioma (<see cref="DescentStage.CaveFadeSeconds"/>), fica um pouco e some. Só ilustra:
    /// a caverna já mudou na simulação. O texto vem dos dados; o nome, do bioma (D-102).
    /// </summary>
    public sealed class CaveBanner : MiningViewBase
    {
        const float FadeInSeconds = 0.2f;
        const float HoldSeconds = 1.6f;
        const float FadeOutSeconds = 0.5f;

        [SerializeField] FeedbackTexts texts;
        [Tooltip("A descida: diz o nome do bioma de cada caverna.")]
        [SerializeField] DescentStage stage;
        [SerializeField] Camera worldCamera;
        [SerializeField] TextMeshPro label;
        [Tooltip("M9i E2 (D-332): a placa de madeira atrás do texto (a placa, as cordas, os pregos). Some junto com o texto.")]
        [SerializeField] SpriteRenderer[] plate = new SpriteRenderer[0];
        [Tooltip("Altura da faixa na tela (0 = base, 1 = topo).")]
        [SerializeField, Range(0f, 1f)] float screenHeight = 0.66f;

        int _cave;
        float _age = float.PositiveInfinity;
        // A transição de andar (M9c E0) segura a faixa: ela decide a transparência e a altura até soltar.
        bool _held;
        float _heldHeight;

        /// <summary>Se a faixa está na tela agora (para testes).</summary>
        public bool IsShown => label.gameObject.activeSelf;

        /// <summary>O texto da faixa (para testes).</summary>
        public string Text => label.text;

        /// <summary>A transparência do texto agora (para testes).</summary>
        public float Alpha => label.alpha;

        /// <summary>Quantas peças a placa tem (M9i E2: a placa, duas cordas e dois pregos).</summary>
        public int PlatePartCount => plate.Length;

        /// <summary>A transparência da placa agora (para testes): −1 sem placa.</summary>
        public float PlateAlpha => plate.Length > 0 && plate[0] != null ? plate[0].color.a : -1f;

        void Awake() => label.gameObject.SetActive(false);

        protected override void Subscribe(MiningSimulation simulation) => simulation.CaveChanged += OnCaveChanged;

        protected override void Unsubscribe(MiningSimulation simulation) => simulation.CaveChanged -= OnCaveChanged;

        protected override void Rebuild(MiningSimulation simulation)
        {
            _cave = simulation.State.Cave;
            _age = float.PositiveInfinity;
            _held = false;
            label.gameObject.SetActive(false);
        }

        void OnCaveChanged(int cave)
        {
            if (cave == _cave) return; // os 100 cristais do debug avisam sem mudar de caverna
            bool newBiome = Simulation.Rules.BiomeOf(cave) != Simulation.Rules.BiomeOf(_cave);
            _cave = cave;

            string name = stage != null ? stage.BiomeName(cave) : null;
            label.SetText(string.Format(texts.CaveBannerFormat, cave, name ?? ""));
            // Bioma novo: entra no escuro, quando ele troca. Mesmo bioma (GDD v7, D-129): não escurece, entra na hora.
            _age = newBiome ? -DescentStage.CaveFadeSeconds * 0.5f : 0f;
            label.gameObject.SetActive(true);
            SetAlpha(0f);
        }

        /// <summary>
        /// A transição de andar (M9c E0) mostra a faixa da caverna nova como o cartão dela: transparência e altura na tela
        /// (0 = base, 1 = topo) vêm de quem segura, a cada quadro, até <see cref="Release"/>.
        /// </summary>
        public void Hold(float alpha, float height)
        {
            if (!label.gameObject.activeSelf) label.gameObject.SetActive(true);
            _held = true;
            _heldHeight = height;
            SetAlpha(Mathf.Clamp01(alpha));
            Place(height);
        }

        /// <summary>Solta a faixa segura pela transição: ela some sozinha a partir da transparência em que ficou.</summary>
        public void Release()
        {
            if (!_held) return;
            _held = false;
            if (label.alpha <= 0.001f)
            {
                label.gameObject.SetActive(false);
                return;
            }
            // Segue do ponto do sumiço que corresponde à transparência de agora.
            _age = FadeInSeconds + HoldSeconds + (1f - label.alpha) * FadeOutSeconds;
        }

        /// <summary>A transparência do texto e da placa juntos.</summary>
        void SetAlpha(float alpha)
        {
            label.alpha = alpha;
            for (int i = 0; i < plate.Length; i++)
            {
                if (plate[i] == null) continue;
                var color = plate[i].color;
                color.a = alpha;
                plate[i].color = color;
            }
        }

        void Place(float height)
        {
            // Presa à tela: acompanha a câmera (e o tremor).
            float depth = -worldCamera.transform.position.z;
            var at = worldCamera.ViewportToWorldPoint(new Vector3(0.5f, height, depth));
            label.transform.position = new Vector3(at.x, at.y, 0f);
        }

        void LateUpdate()
        {
            if (!label.gameObject.activeSelf) return;
            if (_held)
            {
                Place(_heldHeight);
                return;
            }

            _age += Time.unscaledDeltaTime;
            if (_age >= FadeInSeconds + HoldSeconds + FadeOutSeconds)
            {
                label.gameObject.SetActive(false);
                return;
            }

            float alpha = _age < 0f ? 0f
                : _age < FadeInSeconds ? _age / FadeInSeconds
                : _age < FadeInSeconds + HoldSeconds ? 1f
                : 1f - (_age - FadeInSeconds - HoldSeconds) / FadeOutSeconds;
            SetAlpha(alpha);
            Place(screenHeight);
        }
    }
}
