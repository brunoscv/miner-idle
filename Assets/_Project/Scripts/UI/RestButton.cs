using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// O botão Descansar da barra de baixo (Marco 8b, E3, D-152), no lado esquerdo (o PASSAR fica no direito). Só aparece
    /// quando dá para descansar (a partir do nível mínimo, GDD v7: 10) e mostra embaixo do nome as lascas vermelhas que o
    /// jogador leva agora (D-156). Ao tocar, abre o modal (<see cref="RestScreen"/>); quem descansa é ele. Fica num objeto
    /// sempre ativo; o que some é o botão. Atualiza por evento, nunca por quadro.
    /// </summary>
    public sealed class RestButton : MiningViewBase
    {
        [SerializeField] FeedbackTexts texts;
        [SerializeField] RestScreen restScreen;
        [SerializeField] Button button;
        [Tooltip("O botão na tela (ligado só quando dá para descansar).")]
        [SerializeField] GameObject visual;
        [SerializeField] TMP_Text label;
        [Tooltip("As lascas vermelhas que o jogador leva, embaixo do nome.")]
        [SerializeField] TMP_Text count;

        double _shownCount = -1;

        /// <summary>Se o botão está na tela agora (para testes).</summary>
        public bool IsShown => visual.activeSelf;

        public string CountText => count.text;

        void Awake() => label.text = texts.RestLabel;

        protected override void OnEnable()
        {
            base.OnEnable();
            button.onClick.AddListener(OpenModal);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            button.onClick.RemoveListener(OpenModal);
        }

        protected override void Subscribe(MiningSimulation simulation)
        {
            simulation.CaveChanged += OnCaveChanged;
            simulation.PermanentsChanged += Refresh;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.CaveChanged -= OnCaveChanged;
            simulation.PermanentsChanged -= Refresh;
        }

        protected override void Rebuild(MiningSimulation simulation)
        {
            _shownCount = -1;
            Refresh();
        }

        void OnCaveChanged(int cave) => Refresh();

        void OpenModal() => restScreen.Open();

        void Refresh()
        {
            bool show = Simulation != null && Simulation.CanRest;
            if (visual.activeSelf != show) visual.SetActive(show);
            if (!show) return;

            double pending = Simulation.PendingRedShards;
            if (pending == _shownCount) return;
            _shownCount = pending;
            count.text = NumberFormatter.FormatShards(pending);
        }
    }
}
