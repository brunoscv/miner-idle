using MinersIdle.Core;
using MinersIdle.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// O botão de passar de caverna (M8, D-110), na coluna da direita: só aparece com o Letrado da caverna atual comprado
    /// e, ao tocar, pede a passagem à simulação (a regra é dela). Fica num objeto sempre ativo; o que some é o botão.
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
            simulation.UpgradePurchased += OnUpgradePurchased;
            simulation.CaveChanged += OnCaveChanged;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.UpgradePurchased -= OnUpgradePurchased;
            simulation.CaveChanged -= OnCaveChanged;
        }

        protected override void Rebuild(MiningSimulation simulation) => Refresh();

        void OnUpgradePurchased(UpgradeId id, int level) => Refresh();
        void OnCaveChanged(int cave) => Refresh();

        void Pass()
        {
            Simulation?.PassCave();
            Refresh();
        }

        void Refresh()
        {
            bool show = Simulation != null && Simulation.CanPassCave;
            if (visual.activeSelf != show) visual.SetActive(show);
        }
    }
}
