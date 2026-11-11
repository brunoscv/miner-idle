using System;
using MinersIdle.Core;
using MinersIdle.Persistence;

namespace MinersIdle.Game
{
    /// <summary>
    /// O cálculo em uso, escolhido no DBG (M8 E3b-3, D-143): o balanço (índice de <see cref="MiningRunner.Balances"/>;
    /// 0 = o do GDD, hoje o v7), a taxa e o incremento da Picareta trocados no DBG → Cálculos (<c>null</c> = o número do
    /// balanço), a Picareta pela caverna do DBG → Caverna (D-201: a leitura da D-133 no lugar da pela compra) e o acúmulo dos
    /// marcos do combo (M9b E3, D-183/D-217: só o maior marco no lugar da soma) e o ritmo do cristal raro (M9b E4, D-185/
    /// D-226: o lento de 2%). O padrão (<c>default</c>) é tudo como o GDD diz.
    /// </summary>
    public readonly struct CalculationChoice : IEquatable<CalculationChoice>
    {
        public CalculationChoice(int balance, double? pickaxeCaveGrowth = null, double? pickaxeStep = null,
                                 bool pickaxeByCave = false, ComboStacking comboStacking = ComboStacking.Cumulative,
                                 RareRhythm rareRhythm = RareRhythm.Gdd)
        {
            RareRhythm = rareRhythm;
            Balance = balance;
            PickaxeCaveGrowth = pickaxeCaveGrowth;
            PickaxeStep = pickaxeStep;
            PickaxeByCave = pickaxeByCave;
            ComboStacking = comboStacking;
        }

        public int Balance { get; }

        /// <summary>A taxa da Picareta (<c>taxa^n</c>), ou <c>null</c> para a do balanço (GDD v7: 1,2).</summary>
        public double? PickaxeCaveGrowth { get; }

        /// <summary>O incremento da Picareta (Dano Base a mais por compra), ou <c>null</c> para o do balanço (GDD v7: 0, D-202).</summary>
        public double? PickaxeStep { get; }

        /// <summary>A Picareta pela caverna (D-133) no lugar da pela compra (GDD v7, D-201): o botão do DBG → Caverna.</summary>
        public bool PickaxeByCave { get; }

        /// <summary>
        /// Como os marcos do combo se acumulam (D-183): a soma (GDD, o padrão) ou só o do maior marco, o botão do
        /// DBG → Cálculos (D-217).
        /// </summary>
        public ComboStacking ComboStacking { get; }

        /// <summary>O ritmo do cristal raro (D-185): o do documento (15%) ou o lento do DBG (2%, D-226).</summary>
        public RareRhythm RareRhythm { get; }

        /// <summary>Tudo como o GDD diz: o primeiro balanço, sem número trocado.</summary>
        public bool IsDefault => Balance == 0 && !PickaxeCaveGrowth.HasValue && !PickaxeStep.HasValue && !PickaxeByCave &&
                                 ComboStacking == ComboStacking.Cumulative && RareRhythm == RareRhythm.Gdd;

        public CalculationChoice WithBalance(int balance) =>
            new CalculationChoice(balance, PickaxeCaveGrowth, PickaxeStep, PickaxeByCave, ComboStacking, RareRhythm);
        public CalculationChoice WithPickaxeCaveGrowth(double? growth) =>
            new CalculationChoice(Balance, growth, PickaxeStep, PickaxeByCave, ComboStacking, RareRhythm);
        public CalculationChoice WithPickaxeStep(double? step) =>
            new CalculationChoice(Balance, PickaxeCaveGrowth, step, PickaxeByCave, ComboStacking, RareRhythm);
        public CalculationChoice WithPickaxeByCave(bool byCave) =>
            new CalculationChoice(Balance, PickaxeCaveGrowth, PickaxeStep, byCave, ComboStacking, RareRhythm);
        public CalculationChoice WithComboStacking(ComboStacking stacking) =>
            new CalculationChoice(Balance, PickaxeCaveGrowth, PickaxeStep, PickaxeByCave, stacking, RareRhythm);
        public CalculationChoice WithRareRhythm(RareRhythm rhythm) =>
            new CalculationChoice(Balance, PickaxeCaveGrowth, PickaxeStep, PickaxeByCave, ComboStacking, rhythm);

        public CalculationData ToData() => new CalculationData
        {
            balance = Balance,
            hasPickaxeCaveGrowth = PickaxeCaveGrowth.HasValue,
            pickaxeCaveGrowth = PickaxeCaveGrowth ?? 0,
            hasPickaxeStep = PickaxeStep.HasValue,
            pickaxeStep = PickaxeStep ?? 0,
            pickaxeByCave = PickaxeByCave,
            comboHighestOnly = ComboStacking == ComboStacking.HighestOnly,
            rareSlow = RareRhythm == RareRhythm.Slow,
        };

        public static CalculationChoice FromData(CalculationData data) => data == null
            ? default
            : new CalculationChoice(data.balance,
                                    data.hasPickaxeCaveGrowth ? data.pickaxeCaveGrowth : (double?)null,
                                    data.hasPickaxeStep ? data.pickaxeStep : (double?)null,
                                    data.pickaxeByCave,
                                    data.comboHighestOnly ? ComboStacking.HighestOnly : ComboStacking.Cumulative,
                                    data.rareSlow ? RareRhythm.Slow : RareRhythm.Gdd);

        public bool Equals(CalculationChoice other) =>
            Balance == other.Balance && Nullable.Equals(PickaxeCaveGrowth, other.PickaxeCaveGrowth) &&
            Nullable.Equals(PickaxeStep, other.PickaxeStep) && PickaxeByCave == other.PickaxeByCave &&
            ComboStacking == other.ComboStacking && RareRhythm == other.RareRhythm;

        public override bool Equals(object obj) => obj is CalculationChoice other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Balance, PickaxeCaveGrowth, PickaxeStep, PickaxeByCave, ComboStacking, RareRhythm);
    }
}
