using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// O fim da descida (M8, E3, D-112): quando a última parede cai, a tela "Coração da montanha" com o tempo de jogo e as
    /// lascas coletadas. Aparece uma vez só, pelo evento (reabrir o jogo depois não mostra de novo); CONTINUAR fecha e o
    /// jogo segue na última caverna. Como a loja, escurece e bloqueia o mapa, mas não pausa nada. Textos em dados.
    /// </summary>
    public sealed class HeartScreen : MiningViewBase
    {
        [SerializeField] FeedbackTexts texts;
        [Tooltip("O fundo que escurece e o painel: ligado só com a tela aberta.")]
        [SerializeField] GameObject screen;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text body;
        [SerializeField] Button continueButton;
        [SerializeField] TMP_Text continueLabel;
        [Tooltip("Segundos reais entre a quebra da parede e a tela (os estilhaços aparecem antes).")]
        [SerializeField, Min(0f)] float showDelay = 1.2f;

        float _wait = -1f;

        public bool IsOpen => screen.activeSelf;

        /// <summary>A tela foi pedida e aparece em <see cref="showDelay"/> s (para testes).</summary>
        public bool IsPending => _wait >= 0f;

        public string BodyText => body.text;

        void Awake()
        {
            title.text = texts.HeartTitle;
            continueLabel.text = texts.HeartContinue;
            screen.SetActive(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            continueButton.onClick.AddListener(Close);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            continueButton.onClick.RemoveListener(Close);
        }

        protected override void Subscribe(MiningSimulation simulation) => simulation.HeartReached += OnHeartReached;

        protected override void Unsubscribe(MiningSimulation simulation) => simulation.HeartReached -= OnHeartReached;

        protected override void Rebuild(MiningSimulation simulation)
        {
            _wait = -1f;
            screen.SetActive(false);
        }

        void OnHeartReached() => _wait = showDelay;

        void Update()
        {
            if (_wait < 0f) return;
            _wait -= Time.unscaledDeltaTime;
            if (_wait < 0f) Open();
        }

        /// <summary>Abre já, com os números de agora (o atraso termina aqui; também para testes).</summary>
        public void Open()
        {
            _wait = -1f;
            var state = Simulation?.State;
            if (state == null) return;

            body.text = string.Format(texts.HeartBodyFormat, NumberFormatter.FormatDuration(state.PlaySeconds),
                                      NumberFormatter.FormatShards(state.ShardsEarned));
            screen.SetActive(true);
        }

        public void Close() => screen.SetActive(false);
    }
}
