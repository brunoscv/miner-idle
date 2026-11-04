using System;

namespace MinersIdle.Core
{
    /// <summary>As linhas de melhoria do M3 (D-36), mais o Letrado (M8). A ordem é a de criação; a da loja vem do catálogo.</summary>
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
        /// <summary>
        /// Letrado (M8, GDD v5): o nível <i>n</i> leva à caverna <i>n+1</i> e dobra o dano nela. Só se compra com os cristais
        /// da caverna atual quebrados e um por caverna (<see cref="Purchases.Blocked"/>).
        /// </summary>
        Letrado,
        /// <summary>
        /// Chance de Crítico (GDD v7 §4.1, D-136): cada nível soma <see cref="UpgradeDefinition.Effect"/> na chance de
        /// crítico do golpe automático e do toque.
        /// </summary>
        Critical,
    }

    /// <summary>Como o custo cresce a cada compra (E3b do M8, D-143). O nível é o número de compras feitas (D-38).</summary>
    public enum CostFormula
    {
        /// <summary><c>inicial × crescimento^nível</c>: a de todas as linhas até o GDD v5 e da maioria no v7.</summary>
        Exponential,
        /// <summary>
        /// <c>inicial + passo × nível</c> (o passo fica em <see cref="UpgradeDefinition.CostGrowth"/>): a Picareta do anão
        /// no GDD v7, "2 × Dano Base" (D-133), com inicial 2 e passo 2.
        /// </summary>
        Linear,
    }

    /// <summary>
    /// Uma linha de melhoria, somente leitura (D-35: formato do GDD, números no asset). O nível é o número de compras
    /// feitas e começa em 0 (D-38): a próxima compra custa <c>custo inicial × crescimento^nível</c>, ou
    /// <c>inicial + passo × nível</c> na fórmula <see cref="CostFormula.Linear"/>.
    /// O significado de <see cref="Effect"/> depende da linha (ver <see cref="MinerStats"/>).
    /// </summary>
    public sealed class UpgradeDefinition
    {
        public UpgradeDefinition(double baseCost, double costGrowth, int maxLevel, double effect,
                                 CostFormula costFormula = CostFormula.Exponential)
        {
            if (baseCost < 0 || double.IsNaN(baseCost)) throw new ArgumentOutOfRangeException(nameof(baseCost));
            if (double.IsNaN(costGrowth) || costGrowth < 0 || (costGrowth == 0 && costFormula == CostFormula.Exponential))
                throw new ArgumentOutOfRangeException(nameof(costGrowth));
            if (!Enum.IsDefined(typeof(CostFormula), costFormula)) throw new ArgumentOutOfRangeException(nameof(costFormula));
            if (maxLevel < 0) throw new ArgumentOutOfRangeException(nameof(maxLevel));
            if (double.IsNaN(effect)) throw new ArgumentOutOfRangeException(nameof(effect));

            BaseCost = baseCost;
            CostGrowth = costGrowth;
            MaxLevel = maxLevel;
            Effect = effect;
            CostFormula = costFormula;
        }

        /// <summary>Custo da 1ª compra.</summary>
        public double BaseCost { get; }
        /// <summary>Quanto o custo multiplica a cada compra (ou, na fórmula linear, quanto soma).</summary>
        public double CostGrowth { get; }
        public CostFormula CostFormula { get; }
        /// <summary>Número máximo de compras. 0 = linha desligada.</summary>
        public int MaxLevel { get; }
        public double Effect { get; }

        public bool IsEnabled => MaxLevel > 0;

        public bool IsMaxed(int level) => level >= MaxLevel;

        /// <summary>Custo da compra que leva do nível <paramref name="level"/> ao seguinte.</summary>
        public double CostAt(int level)
        {
            if (level < 0) throw new ArgumentOutOfRangeException(nameof(level));
            return CostFormula == CostFormula.Linear
                ? BaseCost + CostGrowth * level
                : BaseCost * Math.Pow(CostGrowth, level);
        }
    }
}
