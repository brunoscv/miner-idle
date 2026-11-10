using System;
using MinersIdle.Persistence;

namespace MinersIdle.Game
{
    /// <summary>
    /// O cálculo em uso, escolhido no DBG (M8 E3b-3, D-143): o balanço (índice de <see cref="MiningRunner.Balances"/>;
    /// 0 = o do GDD, hoje o v7), a taxa e o incremento da Picareta trocados no DBG → Cálculos (<c>null</c> = o número do
    /// balanço) e a Picareta pela caverna do DBG → Caverna (D-201: a leitura da D-133 no lugar da pela compra). O padrão
    /// (<c>default</c>) é tudo como o GDD diz.
    /// </summary>
    public readonly struct CalculationChoice : IEquatable<CalculationChoice>
    {
        public CalculationChoice(int balance, double? pickaxeCaveGrowth = null, double? pickaxeStep = null,
                                 bool pickaxeByCave = false)
        {
            Balance = balance;
            PickaxeCaveGrowth = pickaxeCaveGrowth;
            PickaxeStep = pickaxeStep;
            PickaxeByCave = pickaxeByCave;
        }

        public int Balance { get; }

        /// <summary>A taxa da Picareta (<c>taxa^n</c>), ou <c>null</c> para a do balanço (GDD v7: 1,2).</summary>
        public double? PickaxeCaveGrowth { get; }

        /// <summary>O incremento da Picareta (Dano Base a mais por compra), ou <c>null</c> para o do balanço (GDD v7: 1).</summary>
        public double? PickaxeStep { get; }

        /// <summary>A Picareta pela caverna (D-133) no lugar da pela compra (GDD v7, D-201): o botão do DBG → Caverna.</summary>
        public bool PickaxeByCave { get; }

        /// <summary>Tudo como o GDD diz: o primeiro balanço, sem número trocado.</summary>
        public bool IsDefault => Balance == 0 && !PickaxeCaveGrowth.HasValue && !PickaxeStep.HasValue && !PickaxeByCave;

        public CalculationChoice WithBalance(int balance) =>
            new CalculationChoice(balance, PickaxeCaveGrowth, PickaxeStep, PickaxeByCave);
        public CalculationChoice WithPickaxeCaveGrowth(double? growth) =>
            new CalculationChoice(Balance, growth, PickaxeStep, PickaxeByCave);
        public CalculationChoice WithPickaxeStep(double? step) =>
            new CalculationChoice(Balance, PickaxeCaveGrowth, step, PickaxeByCave);
        public CalculationChoice WithPickaxeByCave(bool byCave) =>
            new CalculationChoice(Balance, PickaxeCaveGrowth, PickaxeStep, byCave);

        public CalculationData ToData() => new CalculationData
        {
            balance = Balance,
            hasPickaxeCaveGrowth = PickaxeCaveGrowth.HasValue,
            pickaxeCaveGrowth = PickaxeCaveGrowth ?? 0,
            hasPickaxeStep = PickaxeStep.HasValue,
            pickaxeStep = PickaxeStep ?? 0,
            pickaxeByCave = PickaxeByCave,
        };

        public static CalculationChoice FromData(CalculationData data) => data == null
            ? default
            : new CalculationChoice(data.balance,
                                    data.hasPickaxeCaveGrowth ? data.pickaxeCaveGrowth : (double?)null,
                                    data.hasPickaxeStep ? data.pickaxeStep : (double?)null,
                                    data.pickaxeByCave);

        public bool Equals(CalculationChoice other) =>
            Balance == other.Balance && Nullable.Equals(PickaxeCaveGrowth, other.PickaxeCaveGrowth) &&
            Nullable.Equals(PickaxeStep, other.PickaxeStep) && PickaxeByCave == other.PickaxeByCave;

        public override bool Equals(object obj) => obj is CalculationChoice other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Balance, PickaxeCaveGrowth, PickaxeStep, PickaxeByCave);
    }
}
