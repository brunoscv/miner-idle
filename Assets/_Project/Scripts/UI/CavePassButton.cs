using MinersIdle.Core;
using MinersIdle.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// O botão de passar de andar (M8, D-110), na coluna da direita. No v8 (M9c E1), provisório até o Próximo Andar da E2:
    /// aparece com a barra do andar cheia (<see cref="MiningSimulation.CanAdvance"/>) e, ao tocar, pede o Próximo Andar à
    /// simulação (a regra é dela). Fica num objeto sempre ativo; o que some é o botão.
    /// </summary>
    public sealed class CavePassButton : MiningViewBase
    {
        [SerializeField] Button button;
        [Tooltip("O botão na tela (ligado só quando dá para passar).")]
        [SerializeField] GameObject visual;

        /// <summary>Se o botão está na tela agora (para testes).</summary>
        public bool IsShown => visual.activeSelf;

        protected override void OnEnable()
        {
            base.OnEnable();
            button.onClick.AddListener(Pass);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            button.onClick.RemoveListener(Pass);
        }

        protected override void Subscribe(MiningSimulation simulation)
        {
            simulation.CrystalBroken += OnCrystalBroken;
            simulation.CaveChanged += OnCaveChanged;
            simulation.FloorTransitionEnded += Refresh;
            simulation.HeartReached += Refresh;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.CrystalBroken -= OnCrystalBroken;
            simulation.CaveChanged -= OnCaveChanged;
            simulation.FloorTransitionEnded -= Refresh;
            simulation.HeartReached -= Refresh;
        }

        protected override void Rebuild(MiningSimulation simulation) => Refresh();

        void OnCrystalBroken(Crystal crystal) => Refresh();
        void OnCaveChanged(int cave) => Refresh();

        void Pass()
        {
            Simulation?.AdvanceFloor();
            Refresh();
        }

        void Refresh()
        {
            bool show = Simulation != null && Simulation.CanAdvance;
            if (visual.activeSelf != show) visual.SetActive(show);
        }
    }
}
