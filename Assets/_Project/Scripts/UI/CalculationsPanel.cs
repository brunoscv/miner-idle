using System.Text;
using MinersIdle.Core;
using MinersIdle.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// DBG → Cálculos (M8 E3b-3, D-143): a taxa e o incremento da Picareta (passos de 0,01 e de 1; tocar no valor volta
    /// ao do GDD), Reiniciar run e Padrão GDD. O seletor de balanço e o botão "Picareta" (pela caverna, D-201) saíram no v8
    /// (M9c E1, D-256). Embaixo do DBG, um aviso fixo diz o cálculo em uso: "Cálculo: GDD v8" quando tudo está no padrão; em
    /// âmbar, com a lista do que saiu do GDD. O botão "Acúmulo" do combo (D-217) saiu com o
    /// combo no M9g (D-311). M9b E4 (D-226): o botão "Raro" troca o ritmo do cristal raro (15%
    /// ⇄ 2%, D-185), também no aviso. Fica no menu Cálculos, filho do painel de debug: no release some junto (lá
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

        [Tooltip("O nome do balanço no aviso (o único, desde o v8: \"GDD v8\").")]
        [SerializeField] string balanceLabel;
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

        [Header("Ritmo do cristal raro (M9b E4, D-226)")]
        [Tooltip("O botão \"Raro\": o ritmo do documento ⇄ o lento.")]
        [SerializeField] Button rareRhythm;
        [Tooltip("Rótulo do botão. {0} = a chance do ritmo em uso (15%, 2%).")]
        [SerializeField] string rareRhythmFormat;
        [Tooltip("Somado ao aviso com o ritmo lento. {0} = a chance dele.")]
        [SerializeField] string rareSlowChangedFormat;

        [Header("Distância entre cristais (M9d E1, D-277)")]
        [Tooltip("O botão \"Distância\": a do GDD (3 m) ⇄ 1 m.")]
        [SerializeField] Button crystalSpacing;
        [Tooltip("Rótulo do botão. {0} = a distância em uso, em metros.")]
        [SerializeField] string spacingFormat;
        [Tooltip("Somado ao aviso com a distância fora do GDD. {0} = metros.")]
        [SerializeField] string spacingChangedFormat;

        /// <summary>A outra distância do botão, em metros (D-277: 3 m ⇄ 1 m).</summary>
        public const double AltSpacingMeters = 1;

        [SerializeField] Color defaultColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        [Tooltip("O aviso quando algo saiu do GDD (D-143: âmbar).")]
        [SerializeField] Color changedColor = new Color32(0xFF, 0xB3, 0x2E, 0xFF);

        /// <summary>Rótulo apagado dos botões da Picareta num balanço sem taxa nem incremento (o v5).</summary>
        const float DisabledLabelAlpha = 0.35f;

        readonly StringBuilder _warning = new StringBuilder();
        TMP_Text _growthLabel, _stepLabel, _rareLabel, _spacingLabel;
        TMP_Text[] _tuningLabels;
        float _refresh;

        void Awake()
        {
            if (growthValue != null) _growthLabel = growthValue.GetComponentInChildren<TMP_Text>();
            if (stepValue != null) _stepLabel = stepValue.GetComponentInChildren<TMP_Text>();
            if (rareRhythm != null) _rareLabel = rareRhythm.GetComponentInChildren<TMP_Text>();
            if (crystalSpacing != null) _spacingLabel = crystalSpacing.GetComponentInChildren<TMP_Text>();
            _tuningLabels = new[]
            {
                growthMinus.GetComponentInChildren<TMP_Text>(), _growthLabel, growthPlus.GetComponentInChildren<TMP_Text>(),
                stepMinus.GetComponentInChildren<TMP_Text>(), _stepLabel, stepPlus.GetComponentInChildren<TMP_Text>(),
            };
        }

        void OnEnable()
        {
            restartRun.onClick.AddListener(RestartRun);
            resetDefaults.onClick.AddListener(ResetDefaults);
            growthMinus.onClick.AddListener(LowerGrowth);
            growthValue.onClick.AddListener(DefaultGrowth);
            growthPlus.onClick.AddListener(RaiseGrowth);
            stepMinus.onClick.AddListener(LowerStep);
            stepValue.onClick.AddListener(DefaultStep);
            stepPlus.onClick.AddListener(RaiseStep);
            if (rareRhythm != null) rareRhythm.onClick.AddListener(ToggleRareRhythm);
            if (crystalSpacing != null) crystalSpacing.onClick.AddListener(ToggleCrystalSpacing);
            runner.SimulationStarted += OnSimulationStarted;
            Refresh();
        }

        void OnDisable()
        {
            restartRun.onClick.RemoveListener(RestartRun);
            resetDefaults.onClick.RemoveListener(ResetDefaults);
            growthMinus.onClick.RemoveListener(LowerGrowth);
            growthValue.onClick.RemoveListener(DefaultGrowth);
            growthPlus.onClick.RemoveListener(RaiseGrowth);
            stepMinus.onClick.RemoveListener(LowerStep);
            stepValue.onClick.RemoveListener(DefaultStep);
            stepPlus.onClick.RemoveListener(RaiseStep);
            if (rareRhythm != null) rareRhythm.onClick.RemoveListener(ToggleRareRhythm);
            if (crystalSpacing != null) crystalSpacing.onClick.RemoveListener(ToggleCrystalSpacing);
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

        /// <summary>A Picareta deste balanço tem taxa e incremento (a do GDD v7 e v8).</summary>
        bool Tunable => Asset != null && Asset.PickaxeTunable;

        public double Growth => runner.Calculation.PickaxeCaveGrowth ?? (Asset != null ? Asset.PickaxeCaveGrowth : 0);
        public double Step => runner.Calculation.PickaxeStep ?? (Asset != null ? Asset.PickaxeStep : 0);

        public void RestartRun() => runner.RestartRun();

        /// <summary>Tudo de volta ao GDD, mantendo o progresso.</summary>
        public void ResetDefaults() => runner.UseCalculation(default);

        public void LowerGrowth() => SetGrowth(Growth - GrowthStep);
        public void RaiseGrowth() => SetGrowth(Growth + GrowthStep);
        public void DefaultGrowth() => SetGrowth(Asset.PickaxeCaveGrowth);
        public void LowerStep() => SetStep(Step - StepStep);
        public void RaiseStep() => SetStep(Step + StepStep);
        public void DefaultStep() => SetStep(Asset.PickaxeStep);

        /// <summary>O botão "Raro" (D-226): o ritmo do documento (15%) ⇄ o lento (2%), mantendo o progresso.</summary>
        public void ToggleRareRhythm()
        {
            var next = runner.Calculation.RareRhythm == RareRhythm.Gdd ? RareRhythm.Slow : RareRhythm.Gdd;
            runner.UseCalculation(runner.Calculation.WithRareRhythm(next));
        }

        /// <summary>A distância entre cristais do balanço em uso (sem o Atalho), em metros.</summary>
        public double SpacingMeters =>
            (runner.Calculation.CrystalSpacing ?? (Asset != null ? Asset.MinCrystalSpacing : 0)) / Permanents.UnitsPerMeter;

        /// <summary>
        /// O botão "Distância" (M9d E1, D-277): a do GDD (3 m) ⇄ 1 m, mantendo o progresso. Vale para os cristais que
        /// nascem depois; o Atalho continua tirando a parte dele.
        /// </summary>
        public void ToggleCrystalSpacing()
        {
            double? next = runner.Calculation.CrystalSpacing.HasValue ? null : AltSpacingMeters * Permanents.UnitsPerMeter;
            runner.UseCalculation(runner.Calculation.WithCrystalSpacing(next));
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
            bool tunable = Tunable;
            growthMinus.interactable = growthValue.interactable = growthPlus.interactable = tunable;
            stepMinus.interactable = stepValue.interactable = stepPlus.interactable = tunable;
            foreach (var label in _tuningLabels)
                if (label != null) label.alpha = tunable ? 1f : DisabledLabelAlpha;
            if (_growthLabel != null) _growthLabel.text = string.Format(growthFormat, NumberFormatter.Format(Growth));
            if (_stepLabel != null) _stepLabel.text = string.Format(stepFormat, NumberFormatter.Format(Step));

            bool rareSlow = choice.RareRhythm == RareRhythm.Slow;
            double rareChance = Asset == null ? 0 : rareSlow ? Asset.RareSlowChance : Asset.RareChance;
            if (_rareLabel != null && !string.IsNullOrEmpty(rareRhythmFormat))
                _rareLabel.text = string.Format(rareRhythmFormat, NumberFormatter.FormatPercent(rareChance));
            // GDD v8 (D-283): o raro nasce pelo contador, sem chance; o ritmo lento não vale, e o botão some.
            bool hasRhythm = Asset == null || Asset.RareInterval <= 0;
            if (rareRhythm != null && rareRhythm.gameObject.activeSelf != hasRhythm) rareRhythm.gameObject.SetActive(hasRhythm);

            bool spacingChanged = choice.CrystalSpacing.HasValue;
            if (_spacingLabel != null && !string.IsNullOrEmpty(spacingFormat))
                _spacingLabel.text = string.Format(spacingFormat, NumberFormatter.Format(SpacingMeters));

            RefreshPickaxe();

            if (warning == null) return;
            _warning.Clear();
            _warning.AppendFormat(warningFormat, balanceLabel);
            if (tunable && choice.PickaxeCaveGrowth.HasValue) _warning.AppendFormat(growthChangedFormat, NumberFormatter.Format(Growth));
            if (tunable && choice.PickaxeStep.HasValue) _warning.AppendFormat(stepChangedFormat, NumberFormatter.Format(Step));
            if (rareSlow && !string.IsNullOrEmpty(rareSlowChangedFormat))
                _warning.AppendFormat(rareSlowChangedFormat, NumberFormatter.FormatPercent(rareChance));
            if (spacingChanged && !string.IsNullOrEmpty(spacingChangedFormat))
                _warning.AppendFormat(spacingChangedFormat, NumberFormatter.Format(SpacingMeters));
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
