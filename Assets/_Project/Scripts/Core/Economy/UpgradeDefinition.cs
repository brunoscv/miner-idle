using System;

namespace MinersIdle.Core
{
    /// <summary>As linhas de melhoria do M3 (D-36). A ordem é a da loja.</summary>
    public enum UpgradeId
    {
        /// <summary>Dano por golpe.</summary>
        Pickaxe,
        /// <summary>Intervalo entre golpes.</summary>
        Speed,
        /// <summary>Lascas por cristal.</summary>
        Mining,
        /// <summary>Picareta Dupla: compra única que multiplica o dano.</summary>
        DoublePickaxe,
        /// <summary>Velocidade de caminhada (linha nova, nasce do mapa).</summary>
        Boots,
    }

    /// <summary>
    /// Uma linha de melhoria, somente leitura (D-35: formato do GDD, números no asset). O nível é o número de compras
    /// feitas e começa em 0 (D-38): a próxima compra custa <c>custo inicial × crescimento^nível</c>.
    /// O significado de <see cref="Effect"/> depende da linha (ver <see cref="MinerStats"/>).
    /// </summary>
    public sealed class UpgradeDefinition
    {
        public UpgradeDefinition(double baseCost, double costGrowth, int maxLevel, double effect)
        {
            if (baseCost < 0 || double.IsNaN(baseCost)) throw new ArgumentOutOfRangeException(nameof(baseCost));
            if (costGrowth <= 0 || double.IsNaN(costGrowth)) throw new ArgumentOutOfRangeException(nameof(costGrowth));
            if (maxLevel < 0) throw new ArgumentOutOfRangeException(nameof(maxLevel));
            if (double.IsNaN(effect)) throw new ArgumentOutOfRangeException(nameof(effect));

            BaseCost = baseCost;
            CostGrowth = costGrowth;
            MaxLevel = maxLevel;
            Effect = effect;
        }

        /// <summary>Custo da 1ª compra.</summary>
        public double BaseCost { get; }
        /// <summary>Quanto o custo multiplica a cada compra.</summary>
        public double CostGrowth { get; }
        /// <summary>Número máximo de compras. 0 = linha desligada.</summary>
        public int MaxLevel { get; }
        public double Effect { get; }

        public bool IsEnabled => MaxLevel > 0;

        public bool IsMaxed(int level) => level >= MaxLevel;

        /// <summary>Custo da compra que leva do nível <paramref name="level"/> ao seguinte.</summary>
        public double CostAt(int level)
        {
            if (level < 0) throw new ArgumentOutOfRangeException(nameof(level));
            return BaseCost * Math.Pow(CostGrowth, level);
        }
    }
}
