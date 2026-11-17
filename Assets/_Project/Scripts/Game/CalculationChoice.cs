using System;
using MinersIdle.Core;
using MinersIdle.Persistence;

namespace MinersIdle.Game
{
    /// <summary>
    /// O cálculo em uso, escolhido no DBG (M8 E3b-3, D-143): a taxa e o incremento da Picareta trocados no DBG → Cálculos
    /// (<c>null</c> = o número do balanço), o acúmulo dos marcos do combo (M9b E3, D-183/D-217: só o maior marco no lugar
    /// da soma) e o ritmo do cristal raro (M9b E4, D-185/D-226: o lento de 2%). O padrão (<c>default</c>) é tudo como o GDD
    /// diz. O seletor de balanço e a Picareta pela caverna (D-201) saíram no v8 (M9c E1, D-256): o jogo tem um balanço só.
    /// </summary>
    public readonly struct CalculationChoice : IEquatable<CalculationChoice>
    {
        public CalculationChoice(double? pickaxeCaveGrowth = null, double? pickaxeStep = null,
                                 ComboStacking comboStacking = ComboStacking.Cumulative, RareRhythm rareRhythm = RareRhythm.Gdd)
        {
            PickaxeCaveGrowth = pickaxeCaveGrowth;
            PickaxeStep = pickaxeStep;
            ComboStacking = comboStacking;
            RareRhythm = rareRhythm;
        }

        /// <summary>A taxa da Picareta (<c>taxa^n</c>), ou <c>null</c> para a do balanço.</summary>
        public double? PickaxeCaveGrowth { get; }

        /// <summary>O incremento da Picareta (Dano Base a mais por compra), ou <c>null</c> para o do balanço.</summary>
        public double? PickaxeStep { get; }

        /// <summary>
        /// Como os marcos do combo se acumulam (D-183): a soma (GDD, o padrão) ou só o do maior marco, o botão do
        /// DBG → Cálculos (D-217).
        /// </summary>
        public ComboStacking ComboStacking { get; }

        /// <summary>O ritmo do cristal raro (D-185): o do documento (15%) ou o lento do DBG (2%, D-226).</summary>
        public RareRhythm RareRhythm { get; }

        /// <summary>Tudo como o GDD diz, sem número trocado.</summary>
        public bool IsDefault => !PickaxeCaveGrowth.HasValue && !PickaxeStep.HasValue &&
                                 ComboStacking == ComboStacking.Cumulative && RareRhythm == RareRhythm.Gdd;

        public CalculationChoice WithPickaxeCaveGrowth(double? growth) =>
            new CalculationChoice(growth, PickaxeStep, ComboStacking, RareRhythm);
        public CalculationChoice WithPickaxeStep(double? step) =>
            new CalculationChoice(PickaxeCaveGrowth, step, ComboStacking, RareRhythm);
        public CalculationChoice WithComboStacking(ComboStacking stacking) =>
            new CalculationChoice(PickaxeCaveGrowth, PickaxeStep, stacking, RareRhythm);
        public CalculationChoice WithRareRhythm(RareRhythm rhythm) =>
            new CalculationChoice(PickaxeCaveGrowth, PickaxeStep, ComboStacking, rhythm);

        public CalculationData ToData() => new CalculationData
        {
            hasPickaxeCaveGrowth = PickaxeCaveGrowth.HasValue,
            pickaxeCaveGrowth = PickaxeCaveGrowth ?? 0,
            hasPickaxeStep = PickaxeStep.HasValue,
            pickaxeStep = PickaxeStep ?? 0,
            comboHighestOnly = ComboStacking == ComboStacking.HighestOnly,
            rareSlow = RareRhythm == RareRhythm.Slow,
        };

        public static CalculationChoice FromData(CalculationData data) => data == null
            ? default
            : new CalculationChoice(data.hasPickaxeCaveGrowth ? data.pickaxeCaveGrowth : (double?)null,
                                    data.hasPickaxeStep ? data.pickaxeStep : (double?)null,
                                    data.comboHighestOnly ? ComboStacking.HighestOnly : ComboStacking.Cumulative,
                                    data.rareSlow ? RareRhythm.Slow : RareRhythm.Gdd);

        public bool Equals(CalculationChoice other) =>
            Nullable.Equals(PickaxeCaveGrowth, other.PickaxeCaveGrowth) && Nullable.Equals(PickaxeStep, other.PickaxeStep) &&
            ComboStacking == other.ComboStacking && RareRhythm == other.RareRhythm;

        public override bool Equals(object obj) => obj is CalculationChoice other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(PickaxeCaveGrowth, PickaxeStep, ComboStacking, RareRhythm);
    }
}
