using System;

namespace MinersIdle.Core
{
    /// <summary>
    /// As cinco linhas de melhoria e o piso do intervalo de golpe, somente leitura. No M3 vêm do asset de valores
    /// (<c>Balance_Prototipo</c> ou <c>Balance_GDD_v1</c>, D-35).
    /// </summary>
    public sealed class EconomyRules
    {
        readonly UpgradeDefinition[] _lines;

        /// <param name="pickaxe"><see cref="UpgradeDefinition.Effect"/> = quanto o dano multiplica por nível (GDD: 2).</param>
        /// <param name="speed"><see cref="UpgradeDefinition.Effect"/> = segundos a menos no intervalo por nível (GDD: 0,1).</param>
        /// <param name="mining"><see cref="UpgradeDefinition.Effect"/> = fração a mais de lascas por nível (GDD: 0,01 = 1%).</param>
        /// <param name="doublePickaxe"><see cref="UpgradeDefinition.Effect"/> = multiplicador do dano depois da compra (GDD: 2).</param>
        /// <param name="boots"><see cref="UpgradeDefinition.Effect"/> = fração a mais de caminhada por nível (protótipo: 0,1).</param>
        /// <param name="minHitInterval">Intervalo mínimo entre golpes, em segundos (GDD: 0,5).</param>
        public EconomyRules(
            UpgradeDefinition pickaxe, UpgradeDefinition speed, UpgradeDefinition mining,
            UpgradeDefinition doublePickaxe, UpgradeDefinition boots, double minHitInterval)
        {
            if (minHitInterval <= 0 || double.IsNaN(minHitInterval)) throw new ArgumentOutOfRangeException(nameof(minHitInterval));

            _lines = new[]
            {
                pickaxe ?? throw new ArgumentNullException(nameof(pickaxe)),
                speed ?? throw new ArgumentNullException(nameof(speed)),
                mining ?? throw new ArgumentNullException(nameof(mining)),
                doublePickaxe ?? throw new ArgumentNullException(nameof(doublePickaxe)),
                boots ?? throw new ArgumentNullException(nameof(boots)),
            };
            MinHitInterval = minHitInterval;
        }

        public double MinHitInterval { get; }

        public UpgradeDefinition Get(UpgradeId id) => _lines[(int)id];

        /// <summary>Uma economia sem nenhuma melhoria (todas as linhas desligadas): o jogo do M2.</summary>
        public static EconomyRules None(double minHitInterval = 0.5)
        {
            var off = new UpgradeDefinition(0, 1, 0, 0);
            return new EconomyRules(off, off, off, off, off, minHitInterval);
        }
    }
}
