using System;

namespace MinersIdle.Core
{
    /// <summary>
    /// Atributos do anão calculados a partir dos valores base (<see cref="MiningRules"/>) e dos níveis das melhorias.
    /// Sempre recalculados do zero, nunca somados aos poucos: assim nenhum bônus acumula duas vezes.
    /// </summary>
    public readonly struct MinerStats
    {
        public MinerStats(double damage, double hitInterval, double walkSpeed, double rewardMultiplier)
        {
            Damage = damage;
            HitInterval = hitInterval;
            WalkSpeed = walkSpeed;
            RewardMultiplier = rewardMultiplier;
        }

        /// <summary>Dano por golpe (DANO do HUD).</summary>
        public double Damage { get; }
        /// <summary>Segundos entre golpes (VELOCIDADE do HUD).</summary>
        public double HitInterval { get; }
        /// <summary>Unidades de mundo por segundo.</summary>
        public double WalkSpeed { get; }
        /// <summary>Multiplica as lascas de cada cristal (Mineração).</summary>
        public double RewardMultiplier { get; }

        /// <summary>
        /// Fórmulas (formato do GDD, REQ-ECO): dano = base × Picareta.efeito^n × (Dupla.efeito se comprada);
        /// intervalo = max(mínimo, base − Velocidade.efeito × n); caminhada = base × (1 + Botas.efeito × n);
        /// lascas = × (1 + Mineração.efeito × n).
        /// </summary>
        public static MinerStats Compute(MiningRules rules, EconomyRules economy, GameState state)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (economy == null) throw new ArgumentNullException(nameof(economy));
            if (state == null) throw new ArgumentNullException(nameof(state));

            int pickaxe = state.LevelOf(UpgradeId.Pickaxe);
            int doublePickaxe = state.LevelOf(UpgradeId.DoublePickaxe);

            double damage = rules.Damage * Math.Pow(economy.Get(UpgradeId.Pickaxe).Effect, pickaxe);
            if (doublePickaxe > 0) damage *= economy.Get(UpgradeId.DoublePickaxe).Effect;

            double interval = Math.Max(
                economy.MinHitInterval,
                rules.HitInterval - economy.Get(UpgradeId.Speed).Effect * state.LevelOf(UpgradeId.Speed));

            double walk = rules.WalkSpeed * (1 + economy.Get(UpgradeId.Boots).Effect * state.LevelOf(UpgradeId.Boots));
            double reward = 1 + economy.Get(UpgradeId.Mining).Effect * state.LevelOf(UpgradeId.Mining);

            return new MinerStats(damage, interval, walk, reward);
        }
    }
}
