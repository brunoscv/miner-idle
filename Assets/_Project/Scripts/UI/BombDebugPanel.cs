using MinersIdle.Core;
using MinersIdle.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// DBG → Bomba (M9g E1): Encher, Lançar, Próxima bomba (troca a bomba sem comprar; no Barril, volta à Pequena), Bomba
    /// automática (lança sempre que enche: atalho para pular a repetição) e o Estilo da faixa (o trilho no quadrado, A+C, ou
    /// o destravar, C; pedido do Bruno para comparar no jogo). Não fica salvo. Está dentro do painel de debug: no release
    /// some junto com ele.
    /// </summary>
    public sealed class BombDebugPanel : MonoBehaviour
    {
        [SerializeField] MiningRunner runner;
        [SerializeField] Button fillButton;
        [SerializeField] Button throwButton;
        [SerializeField] Button nextButton;
        [SerializeField] Button autoButton;
        [SerializeField] Button styleButton;
        [Tooltip("As duas faixas: a do trilho no quadrado (A+C) e a de destravar (C). Só uma fica ligada.")]
        [SerializeField] GameObject trackSlider;
        [SerializeField] GameObject unlockSlider;
        [Tooltip("{0} = o nome da bomba em uso, {1} = a % dela.")]
        [SerializeField] string nextFormat = "Bomba: {0} {1}";
        [Tooltip("Os nomes das 8 bombas, na ordem da compra (D-240).")]
        [SerializeField] string[] bombNames =
            { "Pequena", "Pequena Vermelha", "Normal", "Normal Vermelha", "Dinamite", "Pacote", "Caixa", "Barril" };
        [Tooltip("{0} = sim ou não.")]
        [SerializeField] string autoFormat = "Automática: {0}";
        [Tooltip("{0} = o nome do estilo.")]
        [SerializeField] string styleFormat = "Estilo: {0}";
        [SerializeField] string trackStyleName = "A+C";
        [SerializeField] string unlockStyleName = "C";
        [SerializeField] string yesName = "sim";
        [SerializeField] string noName = "não";

        TMP_Text _nextLabel, _autoLabel, _styleLabel;

        /// <summary>A faixa em uso é a de destravar (C); senão, a do trilho (A+C).</summary>
        public bool UsesUnlockStyle => unlockSlider != null && unlockSlider.activeSelf;

        void Awake()
        {
            if (nextButton != null) _nextLabel = nextButton.GetComponentInChildren<TMP_Text>();
            if (autoButton != null) _autoLabel = autoButton.GetComponentInChildren<TMP_Text>();
            if (styleButton != null) _styleLabel = styleButton.GetComponentInChildren<TMP_Text>();
        }

        void OnEnable()
        {
            if (fillButton != null) fillButton.onClick.AddListener(Fill);
            if (throwButton != null) throwButton.onClick.AddListener(Throw);
            if (nextButton != null) nextButton.onClick.AddListener(Next);
            if (autoButton != null) autoButton.onClick.AddListener(ToggleAuto);
            if (styleButton != null) styleButton.onClick.AddListener(ToggleStyle);
            Refresh();
        }

        void OnDisable()
        {
            if (fillButton != null) fillButton.onClick.RemoveListener(Fill);
            if (throwButton != null) throwButton.onClick.RemoveListener(Throw);
            if (nextButton != null) nextButton.onClick.RemoveListener(Next);
            if (autoButton != null) autoButton.onClick.RemoveListener(ToggleAuto);
            if (styleButton != null) styleButton.onClick.RemoveListener(ToggleStyle);
        }

        MiningSimulation Simulation => runner != null ? runner.Simulation : null;

        public void Fill() => Simulation?.FillBomb();

        /// <summary>Lança na hora, cheia ou não (enche antes); sem cristal no alcance, não sai.</summary>
        public void Throw()
        {
            var simulation = Simulation;
            if (simulation == null) return;
            simulation.FillBomb();
            simulation.ThrowBomb();
        }

        public void Next()
        {
            Simulation?.CyclePermanent(PermanentId.Bomb);
            Refresh();
        }

        public void ToggleAuto()
        {
            var simulation = Simulation;
            if (simulation == null) return;
            simulation.AutoBomb = !simulation.AutoBomb;
            Refresh();
        }

        public void ToggleStyle()
        {
            bool unlock = !UsesUnlockStyle;
            if (trackSlider != null) trackSlider.SetActive(!unlock);
            if (unlockSlider != null) unlockSlider.SetActive(unlock);
            Refresh();
        }

        void Refresh()
        {
            var simulation = Simulation;
            if (_styleLabel != null) _styleLabel.text = string.Format(styleFormat, UsesUnlockStyle ? unlockStyleName : trackStyleName);
            if (simulation == null) return;
            if (_autoLabel != null) _autoLabel.text = string.Format(autoFormat, simulation.AutoBomb ? yesName : noName);
            if (_nextLabel != null)
            {
                int index = simulation.BombIndex;
                string name = bombNames != null && index < bombNames.Length ? bombNames[index] : (index + 1).ToString();
                _nextLabel.text = string.Format(nextFormat, name, NumberFormatter.FormatPercent(simulation.BombFraction));
            }
        }
    }
}
