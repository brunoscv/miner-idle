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
        /// Multiplica as lascas que ele minera no offline (a Mineração dele, D-166). No jogo aberto o cristal paga sempre com
        /// a Mineração do anão (D-95): este número só vale no M9.
        /// </summary>
        public double OfflineRewardMultiplier { get; }

        /// <summary>
        /// Fórmulas (GDD v7 §5.4, D-164, D-165): sem a Picareta dele, o dano é o Dano Base das regras (o do anão sem o
        /// crescimento pela caverna); com <i>n</i> compras, Picareta.efeito × crescimento^(n−1) (5 × 2^(n−1)); × Treinar. O
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
            double damage = pickaxe <= 0
                ? rules.Damage
                : economy.Get(UpgradeId.InternPickaxe).Effect * Math.Pow(economy.InternPickaxeGrowth, pickaxe - 1);
            if (permanents != null) damage *= permanents.InternDamageMultiplier;

            double interval = Math.Max(
                economy.MinHitInterval,
                rules.HitInterval - economy.Get(UpgradeId.InternSpeed).Effect * Level(UpgradeId.InternSpeed));

            double offline = 1 + economy.Get(UpgradeId.InternMining).Effect * Level(UpgradeId.InternMining);

            return new InternStats(hired, damage, interval, offline);
        }
    }
}
