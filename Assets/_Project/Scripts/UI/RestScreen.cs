using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// O modal de Descansar (Marco 8b, E3, D-153): a janela da loja, menor e no centro. Mostra o nível alcançado, o tempo
    /// do dia, as lascas da run, as lascas vermelhas que o jogador leva e quanto o próximo nível somaria, para ele decidir
    /// se para ou continua. CONTINUAR fecha sem mudar nada; DESCANSAR pede o descanso ao <see cref="MiningRunner"/> (a regra
    /// é da simulação) e abre o resumo do dia. Como a loja, escurece e bloqueia o mapa, mas não pausa a mineração
    /// (REQ-UI-04): os números acompanham a run enquanto ele está aberto. Textos em dados.
    /// </summary>
    public sealed class RestScreen : MiningViewBase
    {
        [SerializeField] FeedbackTexts texts;
        [SerializeField] DaySummaryScreen summary;
        [Tooltip("O fundo que escurece e o painel: ligado só com o modal aberto.")]
        [SerializeField] GameObject screen;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text day;
        [SerializeField] TMP_Text levelLabel;
        [SerializeField] TMP_Text levelValue;
        [SerializeField] TMP_Text timeLabel;
        [SerializeField] TMP_Text timeValue;
        [SerializeField] TMP_Text shardsLabel;
        [SerializeField] TMP_Text shardsValue;
        [SerializeField] TMP_Text takeLabel;
        [SerializeField] TMP_Text takeValue;
        [SerializeField] TMP_Text next;
        [SerializeField] TMP_Text note;
        [SerializeField] Button continueButton;
        [SerializeField] TMP_Text continueLabel;
        [SerializeField] Button confirmButton;
        [SerializeField] TMP_Text confirmLabel;

        int _shownSecond = -1;

        public bool IsOpen => screen.activeSelf;
        public string LevelText => levelValue.text;
        public string TimeText => timeValue.text;
        public string ShardsText => shardsValue.text;
        public string TakeText => takeValue.text;
        public string NextText => next.text;
        public string DayText => day.text;

        void Awake()
        {
            title.text = texts.RestTitle;
            levelLabel.text = texts.RestLevelLabel;
            timeLabel.text = texts.RestTimeLabel;
            shardsLabel.text = texts.RestShardsLabel;
            takeLabel.text = texts.RestTakeLabel;
            note.text = texts.RestNote;
            continueLabel.text = texts.RestContinue;
            confirmLabel.text = texts.RestLabel;
            screen.SetActive(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            continueButton.onClick.AddListener(Close);
            confirmButton.onClick.AddListener(Confirm);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            continueButton.onClick.RemoveListener(Close);
            confirmButton.onClick.RemoveListener(Confirm);
        }

        protected override void Subscribe(MiningSimulation simulation)
        {
            simulation.CaveChanged += OnCaveChanged;
            simulation.ShardsChanged += OnShardsChanged;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.CaveChanged -= OnCaveChanged;
            simulation.ShardsChanged -= OnShardsChanged;
        }

        /// <summary>Uma partida nova (save apagado, outro balanço, o descanso): o modal da run de antes fecha.</summary>
        protected override void Rebuild(MiningSimulation simulation) => Close();

        void OnCaveChanged(int cave) => Refresh();
        void OnShardsChanged(double shards) => Refresh();

        /// <summary>O tempo do dia corre com o modal aberto: o texto muda uma vez por segundo, não a cada quadro.</summary>
        void Update()
        {
            if (!IsOpen || Simulation == null) return;
            if ((int)Simulation.State.PlaySeconds != _shownSecond) Refresh();
        }

        /// <summary>Abre com os números de agora. Não abre se não dá para descansar.</summary>
        public void Open()
        {
            if (Simulation == null || !Simulation.CanRest) return;
            screen.SetActive(true);
            Refresh();
        }

        public void Close() => screen.SetActive(false);

        /// <summary>DESCANSAR: o descanso de verdade (a run nova começa por trás) e o resumo do dia por cima.</summary>
        public void Confirm()
        {
            var result = runner.Rest();
            Close();
            if (result.HasValue) summary.Open(result.Value);
        }

        void Refresh()
        {
            var simulation = Simulation;
            if (!IsOpen || simulation == null) return;
            if (!simulation.CanRest)
            {
                Close();
                return;
            }

            var state = simulation.State;
            _shownSecond = (int)state.PlaySeconds;
            day.text = string.Format(texts.DayFormat, simulation.Permanents.State.Day);
            levelValue.text = state.Cave.ToString();
            timeValue.text = NumberFormatter.FormatDuration(state.PlaySeconds);
            shardsValue.text = NumberFormatter.FormatShards(state.ShardsEarned);
            takeValue.text = NumberFormatter.FormatShards(simulation.PendingRedShards);
            double nextShards = simulation.NextLevelRedShards;
            next.text = state.Cave < simulation.Rules.CaveCount
                ? string.Format(texts.RestNextFormat, NumberFormatter.FormatShards(nextShards))
                : texts.RestNoNext;
        }
    }
}
