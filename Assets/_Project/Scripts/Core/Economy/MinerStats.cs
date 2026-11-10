using System;

namespace MinersIdle.Core
{
    /// <summary>
    /// Atributos do anão calculados a partir dos valores base (<see cref="MiningRules"/>) e dos níveis das melhorias.
    /// Sempre recalculados do zero, nunca somados aos poucos: assim nenhum bônus acumula duas vezes.
    /// </summary>
    public readonly struct MinerStats
    {
        public MinerStats(double damage, double hitInterval, double walkSpeed, double rewardMultiplier,
                          double critBonus = 0)
        {
            CritBonus = critBonus;
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
        /// Chance de crítico que a linha Chance de Crítico soma, no golpe automático e no toque (D-136; 0,05 = +5%). A
        /// chance base de cada um está nas <see cref="StrikeRules"/>.
        /// </summary>
        public double CritBonus { get; }

        /// <summary>
        /// Fórmulas (formato do GDD, REQ-ECO): dano = base × Picareta.efeito^n (até o GDD v5), base + Picareta.efeito × n +
        /// taxa^n (GDD v7, D-201: n = compras = nível da Picareta − 1) ou base + Picareta.efeito × n + taxa^(caverna − 1)
        /// (a leitura da D-133, alternativa no DBG; <see cref="MiningRules.PickaxeFormula"/>), × (Dupla.efeito se comprada) ×
        /// (Letrado.efeito da caverna 2 em diante, com o Letrado dela comprado: GDD v5, M8); crítico = Crítico.efeito × n;
        /// intervalo = max(mínimo, base − Velocidade.efeito × n); caminhada = base × (1 + Botas.efeito × n);
        /// lascas = × (1 + Mineração.efeito × n). Desde o Marco 8b, as permanentes (D-158): o dano final × Sangue da
        /// Montanha, as lascas × Coração Ardente e o crítico + Dedos Nervosos (<paramref name="permanents"/>; sem elas, nada).
        /// </summary>
        public static MinerStats Compute(MiningRules rules, EconomyRules economy, GameState state, Permanents permanents = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            return Compute(rules, economy, state, raised: null, permanents);
        }

        /// <summary>
        /// Os atributos com os níveis de <paramref name="state"/>, mas com a linha <paramref name="raised"/> um nível acima:
        /// é o "depois da compra" da prévia da loja (<see cref="UpgradePreview"/>).
        /// </summary>
        internal static MinerStats Compute(MiningRules rules, EconomyRules economy, GameState state, UpgradeId? raised,
                                           Permanents permanents = null)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (economy == null) throw new ArgumentNullException(nameof(economy));

            int Level(UpgradeId id) => state.LevelOf(id) + (raised == id ? 1 : 0);

            double pickaxe = economy.Get(UpgradeId.Pickaxe).Effect;
            int pickaxeLevel = Level(UpgradeId.Pickaxe);
            double damage;
            switch (rules.PickaxeFormula)
            {
                case PickaxeFormula.LevelScaled: // nível da Picareta − 1 = compras (D-201)
                    damage = rules.Damage + pickaxe * pickaxeLevel + Math.Pow(rules.PickaxeCaveGrowth, pickaxeLevel);
                    break;
                case PickaxeFormula.CaveScaled:
                    damage = rules.Damage + pickaxe * pickaxeLevel + Math.Pow(rules.PickaxeCaveGrowth, state.Cave - 1);
                    break;
                default:
                    damage = rules.Damage * Math.Pow(pickaxe, pickaxeLevel);
                    break;
            }
            if (Level(UpgradeId.DoublePickaxe) > 0) damage *= economy.Get(UpgradeId.DoublePickaxe).Effect;
            // O Letrado n dobra o dano na caverna n+1, a que ele abriu (só se chega nela com ele comprado).
            if (state.Cave > 1 && Level(UpgradeId.Letrado) >= state.Cave - 1) damage *= economy.Get(UpgradeId.Letrado).Effect;

            if (permanents != null) damage *= permanents.DamageMultiplier;

            double interval = Math.Max(
                economy.MinHitInterval,
                rules.HitInterval - economy.Get(UpgradeId.Speed).Effect * Level(UpgradeId.Speed));

            double walk = rules.WalkSpeed * (1 + economy.Get(UpgradeId.Boots).Effect * Level(UpgradeId.Boots));
            double reward = 1 + economy.Get(UpgradeId.Mining).Effect * Level(UpgradeId.Mining);
            if (permanents != null) reward *= permanents.RewardMultiplier;

            double crit = economy.Get(UpgradeId.Critical).Effect * Level(UpgradeId.Critical);
            if (permanents != null) crit += permanents.CritBonus;

            return new MinerStats(damage, interval, walk, reward, crit);
        }
    }
}
