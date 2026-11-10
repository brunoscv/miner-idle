using System.Text;
using MinersIdle.Core;
using MinersIdle.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// DBG → Cálculos (M8 E3b-3, D-143): o seletor do balanço (GDD v7, o padrão, ou GDD v5, o jogo de antes), a taxa e o
    /// incremento da Picareta pela caverna (D-133; passos de 0,01 e de 1; tocar no valor volta ao do GDD), Reiniciar run e
    /// Padrão GDD; e o botão "Picareta" que mora no menu Caverna (D-201: pela compra, a do GDD v7, ou pela caverna, a
    /// leitura da D-133). Embaixo do DBG, um aviso fixo diz o cálculo em uso: "Cálculo: GDD v7" quando tudo está no padrão; em
    /// âmbar, com a lista do que saiu do GDD. Fica no menu Cálculos, filho do painel de debug: no release some junto (lá
    /// vale sempre o GDD). Só manda comandos ao <see cref="MiningRunner"/>; quem guarda a escolha no aparelho é ele.
    /// Os textos (rótulos e modelos) ficam na cena.
    /// </summary>
    public sealed class CalculationsPanel : MonoBehaviour
    {
        /// <summary>Passo da taxa e do incremento (D-133): os números que o Bruno deixou configuráveis.</summary>
        public const double GrowthStep = 0.01;
        public const double StepStep = 1;

        /// <summary>Limites do DBG: a taxa abaixo de 1 faria o dano cair a cada nível; o incremento não fica negativo.</summary>
        public const double MinGrowth = 1, MaxGrowth = 2, MinStep = 0, MaxStep = 20;

        const float RefreshSeconds = 0.5f;

        [SerializeField] MiningRunner runner;
        [SerializeField] DebugMenu menu;

        [SerializeField] Button balance;
        [Tooltip("Rótulo do seletor para cada balanço, na ordem do MiningRunner.Balances (o primeiro é o do GDD).")]
        [SerializeField] string[] balanceLabels;
        [SerializeField] Button restartRun;
        [SerializeField] Button resetDefaults;

        [SerializeField] Button growthMinus;
        [Tooltip("Mostra a taxa; tocar volta à do GDD.")]
        [SerializeField] Button growthValue;
        [SerializeField] Button growthPlus;
        [Tooltip("{0} = taxa da Picareta.")]
        [SerializeField] string growthFormat;

        [SerializeField] Button stepMinus;
        [Tooltip("Mostra o incremento; tocar volta ao do GDD.")]
        [SerializeField] Button stepValue;
        [SerializeField] Button stepPlus;
        [Tooltip("{0} = incremento da Picareta.")]
        [SerializeField] string stepFormat;

        [Tooltip("A linha da Picareta no alto do menu.")]
        [SerializeField] TMP_Text pickaxe;
        [Tooltip("{0} dano agora · {1} próxima compra · {2} a linha inteira · {3} nível da Picareta")]
        [SerializeField, TextArea(2, 4)] string pickaxeFormat;
        [Tooltip("No lugar do custo, quando a Picareta está no limite.")]
        [SerializeField] string maxedLabel;

        [Tooltip("O aviso fixo na tela, fora do menu.")]
        [SerializeField] TMP_Text warning;
        [Tooltip("{0} = rótulo do balanço.")]
        [SerializeField] string warningFormat;
        [Tooltip("Somado ao aviso quando a taxa saiu do GDD. {0} = taxa.")]
        [SerializeField] string growthChangedFormat;
        [Tooltip("Somado ao aviso quando o incremento saiu do GDD. {0} = incremento.")]
        [SerializeField] string stepChangedFormat;

        [Header("Picareta pela compra ou pela caverna (D-201)")]
        [Tooltip("O botão \"Picareta\", no menu Caverna do DBG: troca entre a pela compra (GDD v7) e a pela caverna (D-133).")]
        [SerializeField] Button pickaxeByCave;
        [Tooltip("Rótulo do botão com a Picareta pela compra (a do GDD).")]
        [SerializeField] string pickaxeByLevelLabel;
        [Tooltip("Rótulo do botão com a Picareta pela caverna.")]
        [SerializeField] string pickaxeByCaveLabel;
        [Tooltip("Somado ao aviso com a Picareta pela caverna.")]
        [SerializeField] string pickaxeByCaveChangedFormat;
        [SerializeField] Color defaultColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        [Tooltip("O aviso quando algo saiu do GDD (D-143: âmbar).")]
        [SerializeField] Color changedColor = new Color32(0xFF, 0xB3, 0x2E, 0xFF);

        /// <summary>Rótulo apagado dos botões da Picareta num balanço sem taxa nem incremento (o v5).</summary>
        const float DisabledLabelAlpha = 0.35f;

        readonly StringBuilder _warning = new StringBuilder();
        TMP_Text _balanceLabel, _growthLabel, _stepLabel, _byCaveLabel;
        TMP_Text[] _tuningLabels;
        float _refresh;

        void Awake()
        {
            if (balance != null) _balanceLabel = balance.GetComponentInChildren<TMP_Text>();
            if (growthValue != null) _growthLabel = growthValue.GetComponentInChildren<TMP_Text>();
            if (stepValue != null) _stepLabel = stepValue.GetComponentInChildren<TMP_Text>();
            if (pickaxeByCave != null) _byCaveLabel = pickaxeByCave.GetComponentInChildren<TMP_Text>();
            _tuningLabels = new[]
            {
                growthMinus.GetComponentInChildren<TMP_Text>(), _growthLabel, growthPlus.GetComponentInChildren<TMP_Text>(),
                stepMinus.GetComponentInChildren<TMP_Text>(), _stepLabel, stepPlus.GetComponentInChildren<TMP_Text>(),
            };
        }

        void OnEnable()
        {
            balance.onClick.AddListener(NextBalance);
            restartRun.onClick.AddListener(RestartRun);
            resetDefaults.onClick.AddListener(ResetDefaults);
            growthMinus.onClick.AddListener(LowerGrowth);
            growthValue.onClick.AddListener(DefaultGrowth);
            growthPlus.onClick.AddListener(RaiseGrowth);
            stepMinus.onClick.AddListener(LowerStep);
            stepValue.onClick.AddListener(DefaultStep);
            stepPlus.onClick.AddListener(RaiseStep);
            if (pickaxeByCave != null) pickaxeByCave.onClick.AddListener(TogglePickaxeByCave);
            runner.SimulationStarted += OnSimulationStarted;
            Refresh();
        }

        void OnDisable()
        {
            balance.onClick.RemoveListener(NextBalance);
            restartRun.onClick.RemoveListener(RestartRun);
            resetDefaults.onClick.RemoveListener(ResetDefaults);
            growthMinus.onClick.RemoveListener(LowerGrowth);
            growthValue.onClick.RemoveListener(DefaultGrowth);
            growthPlus.onClick.RemoveListener(RaiseGrowth);
            stepMinus.onClick.RemoveListener(LowerStep);
            stepValue.onClick.RemoveListener(DefaultStep);
            stepPlus.onClick.RemoveListener(RaiseStep);
            if (pickaxeByCave != null) pickaxeByCave.onClick.RemoveListener(TogglePickaxeByCave);
            runner.SimulationStarted -= OnSimulationStarted;
        }

        void Update()
        {
            // O dano e o custo mudam com a caverna e as compras; só vale atualizar com o menu aberto.
            if (menu == null || !menu.IsOpen) return;
            _refresh -= Time.unscaledDeltaTime;
            if (_refresh > 0f) return;
            _refresh = RefreshSeconds;
            Refresh();
        }

        void OnSimulationStarted(MiningSimulation simulation) => Refresh();

        MiningRulesAsset Asset => runner.Rules;

        /// <summary>A Picareta deste balanço tem taxa e incremento (a do GDD v7, pela compra ou pela caverna).</summary>
        bool Tunable => Asset != null && Asset.PickaxeTunable;

        /// <summary>A Picareta deste balanço troca entre a pela compra e a pela caverna (só a do GDD v7, D-201).</summary>
        bool Switchable => Asset != null && Asset.PickaxeSwitchable;

        public double Growth => runner.Calculation.PickaxeCaveGrowth ?? (Asset != null ? Asset.PickaxeCaveGrowth : 0);
        public double Step => runner.Calculation.PickaxeStep ?? (Asset != null ? Asset.PickaxeStep : 0);

        public void NextBalance()
        {
            int count = runner.Balances.Count;
            if (count == 0) return;
            runner.UseCalculation(runner.Calculation.WithBalance((runner.Calculation.Balance + 1) % count));
        }

        public void RestartRun() => runner.RestartRun();

        /// <summary>Tudo de volta ao GDD; outro balanço recomeça a run, só a Picareta mantém o progresso.</summary>
        public void ResetDefaults() => runner.UseCalculation(default);

        public void LowerGrowth() => SetGrowth(Growth - GrowthStep);
        public void RaiseGrowth() => SetGrowth(Growth + GrowthStep);
        public void DefaultGrowth() => SetGrowth(Asset.PickaxeCaveGrowth);
        public void LowerStep() => SetStep(Step - StepStep);
        public void RaiseStep() => SetStep(Step + StepStep);
        public void DefaultStep() => SetStep(Asset.PickaxeStep);

        /// <summary>O botão "Picareta" do DBG → Caverna (D-201): pela compra ⇄ pela caverna, mantendo o progresso.</summary>
        public void TogglePickaxeByCave()
        {
            if (!Switchable) return;
            runner.UseCalculation(runner.Calculation.WithPickaxeByCave(!runner.Calculation.PickaxeByCave));
        }

        void SetGrowth(double growth)
        {
            if (!Tunable) return;
            growth = System.Math.Round(System.Math.Clamp(growth, MinGrowth, MaxGrowth), 2);
            // O mesmo número do GDD não conta como troca: o aviso volta ao normal.
            double? value = System.Math.Abs(growth - Asset.PickaxeCaveGrowth) < 1e-9 ? null : growth;
            runner.UseCalculation(runner.Calculation.WithPickaxeCaveGrowth(value));
        }

        void SetStep(double step)
        {
            if (!Tunable) return;
            step = System.Math.Round(System.Math.Clamp(step, MinStep, MaxStep));
            double? value = System.Math.Abs(step - Asset.PickaxeStep) < 1e-9 ? null : step;
            runner.UseCalculation(runner.Calculation.WithPickaxeStep(value));
        }

        void Refresh()
        {
            _refresh = RefreshSeconds;
            var choice = runner.Calculation;
            string balanceName = balanceLabels != null && choice.Balance < balanceLabels.Length ? balanceLabels[choice.Balance] : "";
            if (_balanceLabel != null) _balanceLabel.text = balanceName;

            bool tunable = Tunable;
            growthMinus.interactable = growthValue.interactable = growthPlus.interactable = tunable;
            stepMinus.interactable = stepValue.interactable = stepPlus.interactable = tunable;
            foreach (var label in _tuningLabels)
                if (label != null) label.alpha = tunable ? 1f : DisabledLabelAlpha;
            if (_growthLabel != null) _growthLabel.text = string.Format(growthFormat, NumberFormatter.Format(Growth));
            if (_stepLabel != null) _stepLabel.text = string.Format(stepFormat, NumberFormatter.Format(Step));

            bool switchable = Switchable;
            bool byCave = switchable && choice.PickaxeByCave;
            if (pickaxeByCave != null) pickaxeByCave.interactable = switchable;
            if (_byCaveLabel != null)
            {
                _byCaveLabel.text = byCave ? pickaxeByCaveLabel : pickaxeByLevelLabel;
                _byCaveLabel.alpha = switchable ? 1f : DisabledLabelAlpha;
            }

            RefreshPickaxe();

            if (warning == null) return;
            _warning.Clear();
            _warning.AppendFormat(warningFormat, balanceName);
            if (tunable && choice.PickaxeCaveGrowth.HasValue) _warning.AppendFormat(growthChangedFormat, NumberFormatter.Format(Growth));
            if (tunable && choice.PickaxeStep.HasValue) _warning.AppendFormat(stepChangedFormat, NumberFormatter.Format(Step));
            if (byCave) _warning.Append(pickaxeByCaveChangedFormat);
            warning.text = _warning.ToString();
            warning.color = choice.IsDefault ? defaultColor : changedColor;
        }

        void RefreshPickaxe()
        {
            var simulation = runner.Simulation;
            if (pickaxe == null || simulation == null) return;

            var line = simulation.Economy.Get(UpgradeId.Pickaxe);
            double total = 0;
            for (int level = 0; level < line.MaxLevel; level++) total += line.CostAt(level);
            double? next = simulation.Preview(UpgradeId.Pickaxe).NextCost;
            pickaxe.text = string.Format(pickaxeFormat,
                NumberFormatter.Format(simulation.Stats.Damage),
                next.HasValue ? NumberFormatter.FormatCost(next.Value) : maxedLabel,
                NumberFormatter.FormatCost(total),
                // D-207: na Picareta do GDD v7 o nível começa em 1 (sem compra); no v5 (Multiply), conta as compras.
                simulation.State.LevelOf(UpgradeId.Pickaxe) + (simulation.Rules.PickaxeFormula == PickaxeFormula.Multiply ? 0 : 1));
        }
    }
}
