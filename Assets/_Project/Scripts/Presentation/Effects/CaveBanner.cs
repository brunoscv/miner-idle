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
        [Tooltip("Altura da faixa na tela (0 = base, 1 = topo).")]
        [SerializeField, Range(0f, 1f)] float screenHeight = 0.66f;

        int _cave;
        float _age = float.PositiveInfinity;

        /// <summary>Se a faixa está na tela agora (para testes).</summary>
        public bool IsShown => label.gameObject.activeSelf;

        /// <summary>O texto da faixa (para testes).</summary>
        public string Text => label.text;

        void Awake() => label.gameObject.SetActive(false);

        protected override void Subscribe(MiningSimulation simulation) => simulation.CaveChanged += OnCaveChanged;

        protected override void Unsubscribe(MiningSimulation simulation) => simulation.CaveChanged -= OnCaveChanged;

        protected override void Rebuild(MiningSimulation simulation)
        {
            _cave = simulation.State.Cave;
            _age = float.PositiveInfinity;
            label.gameObject.SetActive(false);
        }

        void OnCaveChanged(int cave)
        {
            if (cave == _cave) return; // os 100 cristais do debug avisam sem mudar de caverna
            _cave = cave;

            string name = stage != null ? stage.BiomeName(cave) : null;
            label.SetText(string.Format(texts.CaveBannerFormat, cave, name ?? ""));
            _age = -DescentStage.CaveFadeSeconds * 0.5f; // entra no escuro, quando o bioma troca
            label.gameObject.SetActive(true);
            label.alpha = 0f;
        }

        void LateUpdate()
        {
            if (!label.gameObject.activeSelf) return;

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
            label.alpha = alpha;

            // Presa à tela: acompanha a câmera (e o tremor).
            float depth = -worldCamera.transform.position.z;
            var at = worldCamera.ViewportToWorldPoint(new Vector3(0.5f, screenHeight, depth));
            label.transform.position = new Vector3(at.x, at.y, 0f);
        }
    }
}
