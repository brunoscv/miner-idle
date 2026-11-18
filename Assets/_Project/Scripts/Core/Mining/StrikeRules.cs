using System;

namespace MinersIdle.Core
{
    /// <summary>
    /// Regras do golpe (Marco 5), somente leitura: o crítico (REQ-MIN-03) e, desde o M9b, o raro, o AD, os anúncios do v8 e
    /// a Bomba. Os valores vêm do asset. O toque, o combo e o teto de toques saíram no M9g (D-311).
    /// </summary>
    public sealed class StrikeRules
    {
        /// <param name="rare">O cristal raro (M9b E4); <c>null</c> = nenhum raro.</param>
        /// <param name="ad">O boost do AD (M9b E5); <c>null</c> = sem AD (o jogo até o raro).</param>
        /// <param name="ads">Os anúncios do Boost (M9e E2, GDD v8); <c>null</c> = nenhum (o jogo até o v7).</param>
        /// <param name="bombs">A Bomba (M9g, GDD v8); <c>null</c> = sem bomba (o jogo até o v7).</param>
        public StrikeRules(double critChance, double critMultiplier, RareRules rare = null, AdRules ad = null,
                           AdOffers ads = null, BombRules bombs = null)
        {
            Bombs = bombs ?? BombRules.None();
            Rare = rare ?? RareRules.None();
            Ad = ad ?? AdRules.None();
            Ads = ads ?? AdOffers.None();
            if (double.IsNaN(critChance) || critChance < 0 || critChance > 1) throw new ArgumentOutOfRangeException(nameof(critChance));
            if (double.IsNaN(critMultiplier) || critMultiplier < 1) throw new ArgumentOutOfRangeException(nameof(critMultiplier));

            CritChance = critChance;
            CritMultiplier = critMultiplier;
        }

        /// <summary>Chance base de crítico do golpe automático (0–1; GDD v7: 0%, D-136).</summary>
        public double CritChance { get; }
        /// <summary>Quanto o dano multiplica no crítico.</summary>
        public double CritMultiplier { get; }

        /// <summary>O cristal raro e as cartas dele (M9b E4); sem cartas, nenhum raro nasce.</summary>
        public RareRules Rare { get; }

        /// <summary>O boost do AD (M9b E5); <see cref="AdRules.None"/> sem ele.</summary>
        public AdRules Ad { get; }

        /// <summary>Os anúncios do Boost (M9e E2, GDD v8); <see cref="AdOffers.None"/> sem eles.</summary>
        public AdOffers Ads { get; }

        /// <summary>A Bomba (M9g, GDD v8); <see cref="BombRules.None"/> sem ela.</summary>
        public BombRules Bombs { get; }

        /// <summary>Sem crítico. É o padrão de quem não passa regras (testes do M2–M4).</summary>
        public static StrikeRules NoCritical() => new StrikeRules(0, 2);
    }

    /// <summary>
    /// Um golpe que acertou um cristal: quanto tirou e se foi crítico. De quem veio, diz o evento (o golpe do anão, o do
    /// estagiário ou a bomba).
    /// </summary>
    public readonly struct MiningHit
    {
        public MiningHit(Crystal crystal, double damage, bool isCritical)
        {
            Crystal = crystal;
            Damage = damage;
            IsCritical = isCritical;
        }

        public Crystal Crystal { get; }
        public double Damage { get; }
        public bool IsCritical { get; }
    }
}
