using System;

namespace MinersIdle.Core
{
    /// <summary>
    /// As linhas de melhoria e o piso do intervalo de golpe, somente leitura. No M3 vêm do asset de valores
    /// (<c>Balance_Prototipo</c> ou <c>Balance_GDD_v1</c>, D-35); o Letrado entra no M8 e o estagiário no M7.
    /// </summary>
    public sealed class EconomyRules
    {
        static readonly UpgradeDefinition Off = new UpgradeDefinition(0, 1, 0, 0);

        readonly UpgradeDefinition[] _lines;

        /// <param name="pickaxe"><see cref="UpgradeDefinition.Effect"/> = quanto o dano multiplica por nível (GDD: 2).</param>
        /// <param name="speed"><see cref="UpgradeDefinition.Effect"/> = segundos a menos no intervalo por nível (GDD: 0,1).</param>
        /// <param name="mining"><see cref="UpgradeDefinition.Effect"/> = fração a mais de lascas por nível (GDD: 0,01 = 1%).</param>
        /// <param name="doublePickaxe"><see cref="UpgradeDefinition.Effect"/> = multiplicador do dano depois da compra (GDD: 2).</param>
        /// <param name="boots"><see cref="UpgradeDefinition.Effect"/> = fração a mais de caminhada por nível (protótipo: 0,1).</param>
        /// <param name="minHitInterval">Intervalo mínimo entre golpes, em segundos (GDD: 0,5). Vale também para o estagiário (D-165).</param>
        /// <param name="letrado">
        /// <see cref="UpgradeDefinition.Effect"/> = multiplicador do dano a partir da caverna 2 (GDD v5: 2). Sem ela, a linha
        /// fica desligada e o jogo tem uma caverna só.
        /// </param>
        /// <param name="critical">
        /// <see cref="UpgradeDefinition.Effect"/> = chance de crítico a mais por nível (GDD v7: 0,05). Sem ela, a linha fica
        /// desligada (o jogo até o GDD v5).
        /// </param>
        /// <param name="intern">Contratar o estagiário (M7; GDD v7: 500, compra única). Sem ela, não há estagiário (D-172).</param>
        /// <param name="internPickaxe">
        /// <see cref="UpgradeDefinition.Effect"/> = o dano do estagiário na 1ª compra (GDD v7: 5); cada compra seguinte
        /// multiplica por <paramref name="internPickaxeGrowth"/>.
        /// </param>
        /// <param name="internSpeed"><see cref="UpgradeDefinition.Effect"/> = segundos a menos no intervalo dele por nível (GDD v7: 0,1).</param>
        /// <param name="internMining"><see cref="UpgradeDefinition.Effect"/> = fração a mais das lascas offline por nível (GDD v7: 1).</param>
        /// <param name="internPickaxeGrowth">O "2" de <c>5 × 2^(n−1)</c> (GDD v7 §5.4).</param>
        public EconomyRules(
            UpgradeDefinition pickaxe, UpgradeDefinition speed, UpgradeDefinition mining,
            UpgradeDefinition doublePickaxe, UpgradeDefinition boots, double minHitInterval,
            UpgradeDefinition letrado = null, UpgradeDefinition critical = null,
            UpgradeDefinition intern = null, UpgradeDefinition internPickaxe = null, UpgradeDefinition internSpeed = null,
            UpgradeDefinition internMining = null, double internPickaxeGrowth = 2)
        {
            if (minHitInterval <= 0 || double.IsNaN(minHitInterval)) throw new ArgumentOutOfRangeException(nameof(minHitInterval));
            if (internPickaxeGrowth <= 0 || double.IsNaN(internPickaxeGrowth))
                throw new ArgumentOutOfRangeException(nameof(internPickaxeGrowth));

            // Na ordem do UpgradeId.
            _lines = new[]
            {
                pickaxe ?? throw new ArgumentNullException(nameof(pickaxe)),
                speed ?? throw new ArgumentNullException(nameof(speed)),
                mining ?? throw new ArgumentNullException(nameof(mining)),
                doublePickaxe ?? throw new ArgumentNullException(nameof(doublePickaxe)),
                boots ?? throw new ArgumentNullException(nameof(boots)),
                letrado ?? Off,
                critical ?? Off,
                intern ?? Off,
                internPickaxe ?? Off,
                internSpeed ?? Off,
                internMining ?? Off,
            };
            MinHitInterval = minHitInterval;
            InternPickaxeGrowth = internPickaxeGrowth;
        }

        public double MinHitInterval { get; }

        /// <summary>Quanto o dano da Picareta do estagiário multiplica a cada compra depois da 1ª (GDD v7: 2).</summary>
        public double InternPickaxeGrowth { get; }

        public UpgradeDefinition Get(UpgradeId id) => _lines[(int)id];

        /// <summary>Se este balanço tem o estagiário (só o GDD v7, D-172).</summary>
        public bool HasIntern => Get(UpgradeId.Intern).IsEnabled;

        /// <summary>As linhas da árvore do estagiário, que só se compram depois de contratar (§5.4).</summary>
        public static bool IsInternTree(UpgradeId id) =>
            id == UpgradeId.InternPickaxe || id == UpgradeId.InternSpeed || id == UpgradeId.InternMining;

        /// <summary>Uma economia sem nenhuma melhoria (todas as linhas desligadas): o jogo do M2.</summary>
        public static EconomyRules None(double minHitInterval = 0.5)
        {
            return new EconomyRules(Off, Off, Off, Off, Off, minHitInterval);
        }
    }
}
