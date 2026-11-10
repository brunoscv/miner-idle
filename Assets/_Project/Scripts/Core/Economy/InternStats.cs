using System;

namespace MinersIdle.Core
{
    /// <summary>
    /// Os atributos do estagiário (M7, GDD v7 §5), calculados das melhorias dele e do Treinar. Como os do anão
    /// (<see cref="MinerStats"/>), sempre recalculados do zero. Do anão, nada vale para ele (D-167): sem crítico, sem
    /// Sangue da Montanha, sem Dedos Nervosos.
    /// </summary>
    public readonly struct InternStats
    {
        public InternStats(bool isHired, double damage, double hitInterval, double offlineRewardMultiplier)
        {
            IsHired = isHired;
            Damage = damage;
            HitInterval = hitInterval;
            OfflineRewardMultiplier = offlineRewardMultiplier;
        }

        /// <summary>Contratado nesta run (a linha <see cref="UpgradeId.Intern"/> comprada, ou a Promoção, D-169).</summary>
        public bool IsHired { get; }
        /// <summary>Dano por golpe dele.</summary>
        public double Damage { get; }
        /// <summary>Segundos entre os golpes dele.</summary>
        public double HitInterval { get; }
        /// <summary>
        /// 1 + a Mineração dele: multiplica as lascas que ele minera no offline (D-189). No jogo aberto, a mesma Mineração
        /// entra somada na dos cristais do anão (<see cref="MinerStats.RewardMultiplier"/>, D-209).
        /// </summary>
        public double OfflineRewardMultiplier { get; }

        /// <summary>
        /// Fórmulas (GDD v7 §5.4, D-205, D-165): dano = Picareta.efeito × crescimento^(nível − 1) (5 × 2^(nível − 1)), com o
        /// nível da Picareta dele = 1 + compras, como a do anão (D-202): contratado e sem compras, 5; × Treinar. O
        /// intervalo parte do das regras (2,0 s) e perde Velocidade.efeito por compra, até o mínimo da economia (0,1 s).
        /// Lascas offline × (1 + Mineração.efeito × nível).
        /// </summary>
        public static InternStats Compute(MiningRules rules, EconomyRules economy, GameState state, Permanents permanents = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            return Compute(rules, economy, state, raised: null, permanents);
        }

        /// <summary>Os atributos com a linha <paramref name="raised"/> um nível acima: o "depois da compra" da prévia.</summary>
        internal static InternStats Compute(MiningRules rules, EconomyRules economy, GameState state, UpgradeId? raised,
                                            Permanents permanents = null)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (economy == null) throw new ArgumentNullException(nameof(economy));

            int Level(UpgradeId id) => state.LevelOf(id) + (raised == id ? 1 : 0);

            bool hired = economy.HasIntern && Level(UpgradeId.Intern) > 0;

            int pickaxe = Level(UpgradeId.InternPickaxe);
            // Nível 1 é o valor-base (§5.4): sem compra ele já bate o efeito da linha (D-205).
            double damage = economy.Get(UpgradeId.InternPickaxe).Effect * Math.Pow(economy.InternPickaxeGrowth, pickaxe);
            if (permanents != null) damage *= permanents.InternDamageMultiplier;

            double interval = Math.Max(
                economy.MinHitInterval,
                rules.HitInterval - economy.Get(UpgradeId.InternSpeed).Effect * Level(UpgradeId.InternSpeed));

            double offline = 1 + economy.Get(UpgradeId.InternMining).Effect * Level(UpgradeId.InternMining);

            return new InternStats(hired, damage, interval, offline);
        }
    }
}
