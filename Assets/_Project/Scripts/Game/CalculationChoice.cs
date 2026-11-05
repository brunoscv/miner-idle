using System;
using MinersIdle.Persistence;

namespace MinersIdle.Game
{
    /// <summary>
    /// O cálculo em uso, escolhido no DBG → Cálculos (M8 E3b-3, D-143): o balanço (índice de
    /// <see cref="MiningRunner.Balances"/>; 0 = o do GDD, hoje o v7) e, na Picareta pela caverna (D-133), a taxa e o
    /// incremento trocados. <c>null</c> = o número do balanço. O padrão (<c>default</c>) é tudo como o GDD diz.
    /// </summary>
    public readonly struct CalculationChoice : IEquatable<CalculationChoice>
    {
        public CalculationChoice(int balance, double? pickaxeCaveGrowth = null, double? pickaxeStep = null)
        {
            Balance = balance;
            PickaxeCaveGrowth = pickaxeCaveGrowth;
            PickaxeStep = pickaxeStep;
        }

        public int Balance { get; }

        /// <summary>A taxa da Picareta (<c>taxa^(caverna − 1)</c>), ou <c>null</c> para a do balanço (GDD v7: 1,2).</summary>
        public double? PickaxeCaveGrowth { get; }

        /// <summary>O incremento da Picareta (Dano Base a mais por compra), ou <c>null</c> para o do balanço (GDD v7: 1).</summary>
        public double? PickaxeStep { get; }

        /// <summary>Tudo como o GDD diz: o primeiro balanço, sem número trocado.</summary>
        public bool IsDefault => Balance == 0 && !PickaxeCaveGrowth.HasValue && !PickaxeStep.HasValue;

        public CalculationChoice WithBalance(int balance) => new CalculationChoice(balance, PickaxeCaveGrowth, PickaxeStep);
        public CalculationChoice WithPickaxeCaveGrowth(double? growth) => new CalculationChoice(Balance, growth, PickaxeStep);
        public CalculationChoice WithPickaxeStep(double? step) => new CalculationChoice(Balance, PickaxeCaveGrowth, step);

        public CalculationData ToData() => new CalculationData
        {
            balance = Balance,
            hasPickaxeCaveGrowth = PickaxeCaveGrowth.HasValue,
            pickaxeCaveGrowth = PickaxeCaveGrowth ?? 0,
            hasPickaxeStep = PickaxeStep.HasValue,
            pickaxeStep = PickaxeStep ?? 0,
        };

        public static CalculationChoice FromData(CalculationData data) => data == null
            ? default
            : new CalculationChoice(data.balance,
                                    data.hasPickaxeCaveGrowth ? data.pickaxeCaveGrowth : (double?)null,
                                    data.hasPickaxeStep ? data.pickaxeStep : (double?)null);

        public bool Equals(CalculationChoice other) =>
            Balance == other.Balance && Nullable.Equals(PickaxeCaveGrowth, other.PickaxeCaveGrowth) &&
            Nullable.Equals(PickaxeStep, other.PickaxeStep);

        public override bool Equals(object obj) => obj is CalculationChoice other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Balance, PickaxeCaveGrowth, PickaxeStep);
    }
}
