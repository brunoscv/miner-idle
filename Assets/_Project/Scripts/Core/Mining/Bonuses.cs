using System;
using System.Collections.Generic;

namespace MinersIdle.Core
{
    /// <summary>O que um bônus com tempo faz (M9b). O AD traz o dele na E5.</summary>
    public enum BonusKind
    {
        /// <summary>+% no dano do golpe e do toque (D-178); os de vários bônus somam (D-179).</summary>
        DamagePercent,
        /// <summary>Todo golpe e todo toque saem críticos (D-178).</summary>
        GuaranteedCritical,
        /// <summary>O dano multiplica pelo valor (a carta "Dano ×2" do raro, D-219), depois dos +% do combo.</summary>
        DamageMultiplier,
        /// <summary>O intervalo do golpe divide pelo valor (a carta "Rápido", D-219), depois do piso de 0,1 s (D-180).</summary>
        IntervalDivisor,
        /// <summary>O que cada cristal quebrado paga multiplica pelo valor (a carta "Lascas ×2", D-219).</summary>
        ShardsMultiplier,
        /// <summary>Pontos a mais na chance de crítico do golpe e do toque (a carta "Crítico +25%", D-219): 0,25 = +25.</summary>
        CritChance,
        /// <summary>
        /// O multiplicador do crítico multiplica pelo valor (a carta "Dano de Crítico ×2" do v8, D-243): ×1,5 vira ×3. Só o
        /// anão (o estagiário não tem crítico, D-167). No fim da lista: o save guarda pelo nome.
        /// </summary>
        CritDamageMultiplier,
        /// <summary>
        /// As cartas do raro no tier 2 enquanto durar (o 4º anúncio, "tier 2 por 1 h", GDD v8, D-250). Sem valor de efeito.
        /// </summary>
        RareTier2,
    }

    /// <summary>De onde o bônus veio.</summary>
    public enum BonusSource
    {
        /// <summary>Um marco do combo (×10, ×30, ×50, ×100), ao parar de tocar (D-96).</summary>
        Combo,
        /// <summary>Uma carta do cristal raro (D-190); o <see cref="ActiveBonus.Tier"/> é a <see cref="RareCard"/>.</summary>
        Rare,
        /// <summary>
        /// O AD. No v7 (M9b E5, D-187), o boost de velocidade: intervalo ÷ 2,5 por 30 s, só no anão (D-177), com o
        /// <see cref="ActiveBonus.Tier"/> 0. No v8 (M9e E2), os anúncios do Boost: o <see cref="ActiveBonus.Tier"/> é o
        /// <see cref="AdKind"/>, e cada anúncio entra como um bônus por atributo.
        /// </summary>
        Ad,
    }

    /// <summary>
    /// Os anúncios do menu do Boost (GDD v8.2 §3.5, M9e E2). O número vai no <see cref="ActiveBonus.Tier"/> dos bônus dele:
    /// não reordenar. O 0 é o boost de velocidade do v7.
    /// </summary>
    public enum AdKind
    {
        /// <summary>Curto: ×2 em dano, lascas e dano do crítico, +10% de crítico, 20 min, +1 h de offline.</summary>
        Short = 1,
        /// <summary>Médio: ×3, +15%, 40 min, +2 h.</summary>
        Medium = 2,
        /// <summary>Grande: ×4, +20%, 1 h 20, +4 h.</summary>
        Big = 3,
        /// <summary>As cartas do raro no tier 2 por 1 h (D-250); some com o Raro Dourado.</summary>
        RareTier2 = 4,
    }

    /// <summary>
    /// Um anúncio do Boost como está no balanço (GDD v8.2 §3.5): o multiplicador de dano, lascas e dano do crítico, os
    /// pontos a mais na chance de crítico, quanto dura e quantos segundos soma ao teto do offline (D-244, D-254).
    /// </summary>
    public readonly struct AdOffer
    {
        public AdOffer(AdKind kind, double multiplier, double critChance, double seconds, double offlineSeconds)
        {
            if (kind < AdKind.Short || kind > AdKind.Big) throw new ArgumentOutOfRangeException(nameof(kind));
            if (double.IsNaN(multiplier) || multiplier < 1) throw new ArgumentOutOfRangeException(nameof(multiplier));
            if (double.IsNaN(critChance) || critChance < 0) throw new ArgumentOutOfRangeException(nameof(critChance));
            if (double.IsNaN(seconds) || seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (double.IsNaN(offlineSeconds) || offlineSeconds < 0) throw new ArgumentOutOfRangeException(nameof(offlineSeconds));
            Kind = kind;
            Multiplier = multiplier;
            CritChance = critChance;
            Seconds = seconds;
            OfflineSeconds = offlineSeconds;
        }

        public AdKind Kind { get; }
        /// <summary>Dano, lascas e dano do crítico ×isto (2, 3, 4).</summary>
        public double Multiplier { get; }
        /// <summary>Pontos a mais na chance de crítico (0,10 = +10%).</summary>
        public double CritChance { get; }
        /// <summary>Quanto dura, em segundos de jogo (corre também com o app fechado, D-181).</summary>
        public double Seconds { get; }
        /// <summary>O que soma ao teto do offline, se ativo ao fechar o app (D-254): 1 h, 2 h, 4 h.</summary>
        public double OfflineSeconds { get; }
    }

    /// <summary>
    /// Os anúncios do Boost do balanço (GDD v8.2 §3.5; D-244, D-250, D-251, D-254): curto, médio e grande, o do tier 2 do
    /// raro e o teto do offline com as horas extras. Sem anúncios (<see cref="None"/>): o jogo até o v7.
    /// </summary>
    public sealed class AdOffers
    {
        readonly AdOffer[] _byKind = new AdOffer[4];
        readonly bool[] _has = new bool[4];

        /// <param name="tier2Seconds">Quanto dura o "tier 2 por 1 h" (D-250: 3600); 0 = sem ele.</param>
        /// <param name="offlineCapSeconds">O teto do offline com as horas extras (D-244: 8 h).</param>
        public AdOffers(IEnumerable<AdOffer> offers, double tier2Seconds, double offlineCapSeconds)
        {
            if (double.IsNaN(tier2Seconds) || tier2Seconds < 0) throw new ArgumentOutOfRangeException(nameof(tier2Seconds));
            if (double.IsNaN(offlineCapSeconds) || offlineCapSeconds < 0) throw new ArgumentOutOfRangeException(nameof(offlineCapSeconds));
            var list = new List<AdOffer>();
            if (offers != null)
                foreach (var offer in offers)
                {
                    int i = (int)offer.Kind;
                    if (_has[i]) throw new ArgumentException($"O anúncio {offer.Kind} aparece duas vezes.", nameof(offers));
                    _has[i] = true;
                    _byKind[i] = offer;
                    list.Add(offer);
                }
            list.Sort((a, b) => a.Kind.CompareTo(b.Kind));
            All = list;
            Tier2Seconds = tier2Seconds;
            OfflineCapSeconds = offlineCapSeconds;
        }

        public IReadOnlyList<AdOffer> All { get; }
        public double Tier2Seconds { get; }
        public double OfflineCapSeconds { get; }

        /// <summary>O balanço tem o menu do Boost (algum anúncio).</summary>
        public bool IsEnabled => All.Count > 0;

        /// <summary>O balanço tem o "tier 2 por 1 h" (D-250).</summary>
        public bool HasTier2 => Tier2Seconds > 0;

        public bool Has(AdKind kind) => kind >= AdKind.Short && kind <= AdKind.Big && _has[(int)kind];

        public AdOffer Get(AdKind kind) =>
            Has(kind) ? _byKind[(int)kind] : throw new ArgumentException($"Sem o anúncio {kind}.", nameof(kind));

        public static AdOffers None() => new AdOffers(null, 0, 0);
    }

    /// <summary>
    /// O boost do AD (M9b E5; GDD v7 §7.2, D-187, D-188): o intervalo do golpe do anão dividido por <see cref="SpeedDivisor"/>
    /// por <see cref="Seconds"/>, depois do piso (D-180). Sem acúmulo: outro só depois que este acaba. Sem divisor ou sem
    /// tempo, não há AD (o jogo até o v5).
    /// </summary>
    public sealed class AdRules
    {
        public AdRules(double speedDivisor, double seconds)
        {
            if (double.IsNaN(speedDivisor) || speedDivisor < 0) throw new ArgumentOutOfRangeException(nameof(speedDivisor));
            if (double.IsNaN(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            SpeedDivisor = speedDivisor;
            Seconds = seconds;
        }

        /// <summary>Quanto o intervalo do golpe divide (GDD v7: 2,5).</summary>
        public double SpeedDivisor { get; }
        /// <summary>Quanto dura, em segundos de jogo (GDD v7: 30).</summary>
        public double Seconds { get; }
        /// <summary>O balanço tem o boost (o botão AD funciona).</summary>
        public bool IsEnabled => SpeedDivisor > 1 && Seconds > 0;

        public static AdRules None() => new AdRules(0, 0);
    }

    /// <summary>
    /// As cartas do cristal raro (D-190; GDD v8, D-243). O número vai no <see cref="ActiveBonus.Tier"/>: não reordenar. Cada
    /// balanço usa as suas: o v7 tem Dano, Rápido, Lascas e Crítico; o v8, Dano, Lascas, Crítico e Dano de Crítico.
    /// </summary>
    public enum RareCard
    {
        /// <summary>Dano ×2 (anão e estagiário).</summary>
        Damage = 0,
        /// <summary>Golpe 2× mais rápido: o intervalo ÷ 2 (anão e estagiário).</summary>
        Speed = 1,
        /// <summary>Lascas ×2 em cada cristal quebrado (e no offline do estagiário).</summary>
        Shards = 2,
        /// <summary>Crítico +25 pontos no v7, +10 no v8 (só o anão: o estagiário não tem crítico, D-167).</summary>
        Critical = 3,
        /// <summary>Dano de Crítico ×2 (GDD v8, D-243): o multiplicador do crítico dobra. Só o anão (D-167).</summary>
        CritDamage = 4,
    }

    /// <summary>
    /// Uma carta do raro como está no balanço: o efeito e a duração (GDD v7: ×2 / ÷2 / ×2 / +0,25, 60 s, D-190; GDD v8: ×2 /
    /// ×2 / +0,10 / ×2, 30 s, e no tier 2 ×4 / ×4 / +0,20 / ×4, D-243).
    /// </summary>
    public readonly struct RareCardRule
    {
        public RareCardRule(RareCard card, double value, double seconds)
        {
            if (!Enum.IsDefined(typeof(RareCard), card)) throw new ArgumentOutOfRangeException(nameof(card));
            if (double.IsNaN(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (double.IsNaN(seconds) || seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            Card = card;
            Value = value;
            Seconds = seconds;
        }

        public RareCard Card { get; }
        public double Value { get; }
        public double Seconds { get; }
        public BonusKind Kind => KindOf(Card);

        /// <summary>O tipo de bônus de cada carta.</summary>
        public static BonusKind KindOf(RareCard card)
        {
            switch (card)
            {
                case RareCard.Damage: return BonusKind.DamageMultiplier;
                case RareCard.Speed: return BonusKind.IntervalDivisor;
                case RareCard.Shards: return BonusKind.ShardsMultiplier;
                case RareCard.CritDamage: return BonusKind.CritDamageMultiplier;
                default: return BonusKind.CritChance;
            }
        }
    }

    /// <summary>
    /// As regras do cristal raro (M9b E4; D-97, D-184…D-186, D-190): quando nasce, o tempo para escolher e as cartas. Sem
    /// cartas (ou com menos de 2), não há raro. Quando nasce: com <see cref="Interval"/> (GDD v8, D-283), um contador de
    /// tantos segundos de run; sem ele (o v7), a chance de cada cristal que nasce ser raro (o padrão e o ritmo lento do DBG).
    /// O tier 2 (GDD v8, D-243, D-286) troca o valor das cartas que o têm.
    /// </summary>
    public sealed class RareRules
    {
        static readonly int CardCount = Enum.GetValues(typeof(RareCard)).Length;

        readonly RareCardRule[] _byCard = new RareCardRule[CardCount];
        readonly bool[] _has = new bool[CardCount];
        readonly RareCardRule[] _tier2 = new RareCardRule[CardCount];
        readonly bool[] _hasTier2 = new bool[CardCount];

        /// <param name="interval">
        /// Segundos de run entre um raro e o próximo (GDD v8: 60, D-283). 0 = pela chance (o v7, D-184).
        /// </param>
        /// <param name="tier2Cards">As cartas no tier 2 (GDD v8: ×4, ×4, +0,20, ×4, D-243); vazio = sem tier 2.</param>
        public RareRules(double chance, double slowChance, double chooseSeconds, IEnumerable<RareCardRule> cards,
                         double interval = 0, IEnumerable<RareCardRule> tier2Cards = null)
        {
            if (double.IsNaN(chance) || chance < 0 || chance > 1) throw new ArgumentOutOfRangeException(nameof(chance));
            if (double.IsNaN(slowChance) || slowChance < 0 || slowChance > 1) throw new ArgumentOutOfRangeException(nameof(slowChance));
            if (double.IsNaN(chooseSeconds) || chooseSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(chooseSeconds));
            if (double.IsNaN(interval) || double.IsInfinity(interval) || interval < 0) throw new ArgumentOutOfRangeException(nameof(interval));
            Chance = chance;
            SlowChance = slowChance;
            ChooseSeconds = chooseSeconds;
            Interval = interval;
            var list = new List<RareCardRule>();
            if (cards != null)
                foreach (var card in cards)
                {
                    if (_has[(int)card.Card]) throw new ArgumentException($"A carta {card.Card} aparece duas vezes.", nameof(cards));
                    _has[(int)card.Card] = true;
                    _byCard[(int)card.Card] = card;
                    list.Add(card);
                }
            list.Sort((a, b) => a.Card.CompareTo(b.Card));
            Cards = list;
            if (tier2Cards != null)
                foreach (var card in tier2Cards)
                {
                    if (_hasTier2[(int)card.Card]) throw new ArgumentException($"A carta {card.Card} do tier 2 aparece duas vezes.", nameof(tier2Cards));
                    if (!_has[(int)card.Card]) throw new ArgumentException($"A carta {card.Card} do tier 2 não está no tier 1.", nameof(tier2Cards));
                    _hasTier2[(int)card.Card] = true;
                    _tier2[(int)card.Card] = card;
                    HasTier2 = true;
                }
        }

        /// <summary>Segundos de run entre um raro e o próximo (GDD v8: 60, D-283); 0 = pela chance (o v7).</summary>
        public double Interval { get; }

        /// <summary>O raro nasce pelo contador (GDD v8, D-283), não pela chance de cada cristal.</summary>
        public bool UsesCounter => Interval > 0;

        /// <summary>O balanço tem o tier 2 do raro (GDD v8, D-243).</summary>
        public bool HasTier2 { get; }

        /// <summary>Chance de cada cristal que nasce ser raro (D-184: 15%).</summary>
        public double Chance { get; }
        /// <summary>O ritmo lento do DBG (D-185: 2%, sem raro novo com janela aberta ou carta ativa).</summary>
        public double SlowChance { get; }
        /// <summary>Segundos para escolher (D-186: 30), só com o app aberto (D-222).</summary>
        public double ChooseSeconds { get; }
        /// <summary>As cartas que a janela sorteia, na ordem do enum.</summary>
        public IReadOnlyList<RareCardRule> Cards { get; }

        /// <summary>Há raro: pelo menos 2 cartas (a janela mostra 2 diferentes, D-190).</summary>
        public bool IsEnabled => Cards.Count >= 2;

        public bool Has(RareCard card) => (int)card >= 0 && (int)card < CardCount && _has[(int)card];
        public RareCardRule Get(RareCard card) =>
            Has(card) ? _byCard[(int)card] : throw new ArgumentException($"Sem a carta {card}.", nameof(card));

        /// <summary>
        /// A carta no <paramref name="tier"/> (1 ou 2): no tier 2, o valor dele, se a carta tem; senão, o do tier 1.
        /// </summary>
        public RareCardRule Get(RareCard card, int tier)
        {
            var rule = Get(card);
            return tier >= 2 && _hasTier2[(int)card] ? _tier2[(int)card] : rule;
        }

        /// <summary>Sem raro (os balanços até o v5 e os testes que não passam regras).</summary>
        public static RareRules None() => new RareRules(0, 0, 30, null);
    }

    /// <summary>
    /// Quantos raros nascem (D-185): o padrão do documento (15% por cristal) ou o lento do DBG (2%, e nenhum raro novo
    /// enquanto houver janela aberta ou carta ativa).
    /// </summary>
    public enum RareRhythm
    {
        Gdd,
        Slow,
    }

    /// <summary>
    /// Como os marcos do combo se acumulam (D-183, D-211). <see cref="Cumulative"/> é o GDD ("bônus temporário
    /// acumulativo"); <see cref="HighestOnly"/> é a alternativa do DBG → Cálculos.
    /// </summary>
    public enum ComboStacking
    {
        /// <summary>
        /// Parar no ×57 dá os bônus do ×10, do ×30 e do ×50 juntos. Um bônus igual a outro ainda ativo vale junto, com o
        /// próprio tempo (dois +10% = +20%); o crítico garantido soma o tempo que sobra.
        /// </summary>
        Cumulative,
        /// <summary>Parar no ×57 dá só o do ×50. O mesmo marco de novo renova o tempo, sem somar efeito.</summary>
        HighestOnly,
    }

    /// <summary>Um marco do combo, como está no balanço (GDD v7 §3.4): a partir de quantos toques, o quê e por quanto tempo.</summary>
    public readonly struct ComboBonusRule
    {
        public ComboBonusRule(int combo, BonusKind kind, double value, double seconds)
        {
            if (combo < 1) throw new ArgumentOutOfRangeException(nameof(combo));
            if (double.IsNaN(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (double.IsNaN(seconds) || seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            Combo = combo;
            Kind = kind;
            Value = value;
            Seconds = seconds;
        }

        /// <summary>O combo que é preciso alcançar (10, 30, 50, 100).</summary>
        public int Combo { get; }
        public BonusKind Kind { get; }
        /// <summary>O efeito: 0,10 = +10% de dano. Sem uso no crítico garantido.</summary>
        public double Value { get; }
        /// <summary>A duração, em segundos de jogo.</summary>
        public double Seconds { get; }
    }

    /// <summary>
    /// Um bônus valendo agora: o que faz, de onde veio (<see cref="Tier"/> = o marco do combo) e quanto falta. O tempo é
    /// de jogo (o ×20 do DBG acelera) e corre também com o app fechado (D-181).
    /// </summary>
    public readonly struct ActiveBonus
    {
        public ActiveBonus(BonusKind kind, double value, double duration, double secondsLeft, BonusSource source, int tier)
        {
            Kind = kind;
            Value = value;
            Duration = duration;
            SecondsLeft = secondsLeft;
            Source = source;
            Tier = tier;
        }

        public BonusKind Kind { get; }
        public double Value { get; }
        /// <summary>A duração cheia (para o anel da coluna de buffs esvaziar). Cresce quando o crítico soma tempo.</summary>
        public double Duration { get; }
        public double SecondsLeft { get; }
        public BonusSource Source { get; }
        /// <summary>O marco que deu o bônus (10, 30, 50, 100 no combo).</summary>
        public int Tier { get; }

        internal ActiveBonus WithSecondsLeft(double seconds) => new ActiveBonus(Kind, Value, Math.Max(Duration, seconds), seconds, Source, Tier);
        internal ActiveBonus Advanced(double step) => new ActiveBonus(Kind, Value, Duration, SecondsLeft - step, Source, Tier);
    }

    /// <summary>
    /// Os bônus com tempo da run (M9b, E1): ficam no <see cref="GameState"/> (vão para o save, D-181, e acabam com a run no
    /// descanso, D-213). O fim de cada um é um evento do <see cref="MiningSimulation.Tick"/>, então o tempo bate com o do
    /// jogo sem olhar relógio. Nada aqui aloca por quadro.
    /// </summary>
    public sealed class ActiveBonuses
    {
        readonly List<ActiveBonus> _bonuses = new List<ActiveBonus>();

        public IReadOnlyList<ActiveBonus> All => _bonuses;
        public int Count => _bonuses.Count;

        /// <summary>A soma dos +% de dano ativos (D-179): 0,25 = +25%.</summary>
        public double DamagePercent
        {
            get
            {
                double sum = 0;
                for (int i = 0; i < _bonuses.Count; i++)
                    if (_bonuses[i].Kind == BonusKind.DamagePercent) sum += _bonuses[i].Value;
                return sum;
            }
        }

        /// <summary>Se algum crítico garantido está ativo.</summary>
        public bool GuaranteedCritical
        {
            get
            {
                for (int i = 0; i < _bonuses.Count; i++)
                    if (_bonuses[i].Kind == BonusKind.GuaranteedCritical) return true;
                return false;
            }
        }

        /// <summary>O produto dos multiplicadores de dano ativos (a carta "Dano ×2"); 1 sem nenhum.</summary>
        public double DamageMultiplier => Product(BonusKind.DamageMultiplier, internOnly: false);

        /// <summary>O produto dos divisores do intervalo ativos (a carta "Rápido"); 1 sem nenhum.</summary>
        public double IntervalDivisor => Product(BonusKind.IntervalDivisor, internOnly: false);

        /// <summary>O produto dos multiplicadores de lascas ativos (a carta "Lascas ×2"); 1 sem nenhum.</summary>
        public double ShardsMultiplier => Product(BonusKind.ShardsMultiplier, internOnly: false);

        /// <summary>O produto dos multiplicadores do dano do crítico (a carta "Dano de Crítico ×2" do v8); 1 sem nenhum.</summary>
        public double CritDamageMultiplier => Product(BonusKind.CritDamageMultiplier, internOnly: false);

        /// <summary>
        /// A soma dos pontos a mais na chance de crítico (a carta "Crítico +25%"): 0,25 = +25. Dos anúncios, só o maior
        /// (D-251), somado ao resto (D-287).
        /// </summary>
        public double CritChance
        {
            get
            {
                double sum = 0, ad = 0;
                for (int i = 0; i < _bonuses.Count; i++)
                {
                    if (_bonuses[i].Kind != BonusKind.CritChance) continue;
                    if (IsAdOffer(_bonuses[i])) ad = Math.Max(ad, _bonuses[i].Value);
                    else sum += _bonuses[i].Value;
                }
                return sum + ad;
            }
        }

        /// <summary>O dano × do estagiário: as cartas do raro (D-190) e os anúncios do v8 (D-246); o combo não (D-177).</summary>
        public double InternDamageMultiplier => Product(BonusKind.DamageMultiplier, internOnly: true);

        /// <summary>O intervalo ÷ do estagiário: só as cartas do raro (o boost do AD do v7 não vale para ele, D-177).</summary>
        public double InternIntervalDivisor => Product(BonusKind.IntervalDivisor, internOnly: true);

        /// <summary>Se alguma carta do raro está ativa (o ritmo lento não faz raro novo, D-185).</summary>
        public bool HasRareCard
        {
            get
            {
                for (int i = 0; i < _bonuses.Count; i++)
                    if (_bonuses[i].Source == BonusSource.Rare) return true;
                return false;
            }
        }

        /// <summary>O boost de velocidade do AD do v7 ativo, ou <c>null</c> (D-188: no máximo um).</summary>
        public ActiveBonus? AdBoost
        {
            get
            {
                for (int i = 0; i < _bonuses.Count; i++)
                    if (_bonuses[i].Source == BonusSource.Ad && !IsAdOffer(_bonuses[i])) return _bonuses[i];
                return null;
            }
        }

        /// <summary>O anúncio do tier 2 do raro está ativo (D-250).</summary>
        public bool HasRareTier2Ad
        {
            get
            {
                for (int i = 0; i < _bonuses.Count; i++)
                    if (_bonuses[i].Kind == BonusKind.RareTier2) return true;
                return false;
            }
        }

        /// <summary>Segundos que faltam do anúncio <paramref name="kind"/> (o maior dos bônus dele); 0 sem ele.</summary>
        public double AdSecondsLeft(AdKind kind)
        {
            double left = 0;
            for (int i = 0; i < _bonuses.Count; i++)
                if (_bonuses[i].Source == BonusSource.Ad && _bonuses[i].Tier == (int)kind) left = Math.Max(left, _bonuses[i].SecondsLeft);
            return left;
        }

        /// <summary>Um bônus de um anúncio do Boost do v8 (não o boost de velocidade do v7, que tem o tier 0).</summary>
        public static bool IsAdOffer(ActiveBonus bonus) => bonus.Source == BonusSource.Ad && bonus.Tier > 0;

        /// <summary>
        /// Se o bônus vale também para o estagiário: as cartas do raro (D-190) e os anúncios do v8 (D-246). O combo e o
        /// boost de velocidade do v7, não (D-177). O crítico não muda nada para ele (D-167).
        /// </summary>
        public static bool AppliesToIntern(ActiveBonus bonus) => bonus.Source == BonusSource.Rare || IsAdOffer(bonus);

        /// <summary>
        /// O produto dos bônus do tipo: os anúncios do v8 entre si valem o maior (D-251), e esse maior multiplica o resto
        /// (as cartas do raro, D-287).
        /// </summary>
        double Product(BonusKind kind, bool internOnly)
        {
            double product = 1, ad = 1;
            for (int i = 0; i < _bonuses.Count; i++)
            {
                var bonus = _bonuses[i];
                if (bonus.Kind != kind || bonus.Value <= 0 || (internOnly && !AppliesToIntern(bonus))) continue;
                if (IsAdOffer(bonus)) ad = Math.Max(ad, bonus.Value);
                else product *= bonus.Value;
            }
            return product * ad;
        }

        /// <summary>Segundos até o próximo bônus acabar; infinito sem nenhum.</summary>
        public double NextExpiry
        {
            get
            {
                double next = double.PositiveInfinity;
                for (int i = 0; i < _bonuses.Count; i++) next = Math.Min(next, Math.Max(0, _bonuses[i].SecondsLeft));
                return next;
            }
        }

        /// <summary>
        /// Entra um bônus, pela regra do acúmulo (D-211): no padrão, um bônus de dano vale junto com os iguais, e o crítico
        /// garantido soma o tempo ao que já está ativo; na alternativa, o mesmo marco renova o tempo.
        /// </summary>
        internal void Add(ActiveBonus bonus, ComboStacking stacking)
        {
            if (bonus.SecondsLeft <= 0) return;
            for (int i = 0; i < _bonuses.Count; i++)
            {
                var active = _bonuses[i];
                if (stacking == ComboStacking.HighestOnly && active.Source == bonus.Source && active.Tier == bonus.Tier)
                {
                    _bonuses[i] = active.WithSecondsLeft(bonus.SecondsLeft);
                    return;
                }
                if (stacking == ComboStacking.Cumulative && bonus.Kind == BonusKind.GuaranteedCritical
                    && active.Kind == BonusKind.GuaranteedCritical)
                {
                    _bonuses[i] = active.WithSecondsLeft(active.SecondsLeft + bonus.SecondsLeft);
                    return;
                }
            }
            _bonuses.Add(bonus);
        }

        /// <summary>
        /// Entra uma carta do raro (D-190) ou um bônus de anúncio (D-244): igual a outro ainda ativo (mesma origem, mesmo
        /// tier e mesmo tipo), só renova o tempo, sem somar efeito; diferente, vale junto.
        /// </summary>
        internal void AddOrRenew(ActiveBonus bonus)
        {
            if (bonus.SecondsLeft <= 0) return;
            for (int i = 0; i < _bonuses.Count; i++)
            {
                var active = _bonuses[i];
                if (active.Source != bonus.Source || active.Tier != bonus.Tier || active.Kind != bonus.Kind) continue;
                _bonuses[i] = new ActiveBonus(bonus.Kind, bonus.Value, bonus.Duration, bonus.SecondsLeft, bonus.Source, bonus.Tier);
                return;
            }
            _bonuses.Add(bonus);
        }

        /// <summary>O tempo passa para todos (jogo aberto ou fechado).</summary>
        internal void Advance(double step)
        {
            if (step <= 0) return;
            for (int i = 0; i < _bonuses.Count; i++) _bonuses[i] = _bonuses[i].Advanced(step);
        }

        /// <summary>Tira os que acabaram (com a folga <paramref name="epsilon"/>). Devolve se tirou algum.</summary>
        internal bool RemoveExpired(double epsilon)
        {
            int before = _bonuses.Count;
            _bonuses.RemoveAll(b => b.SecondsLeft <= epsilon);
            return _bonuses.Count != before;
        }

        internal bool Clear()
        {
            if (_bonuses.Count == 0) return false;
            _bonuses.Clear();
            return true;
        }

        /// <summary>Os bônus de um save, sem os vencidos nem os estragados.</summary>
        internal void Restore(IEnumerable<ActiveBonus> bonuses)
        {
            _bonuses.Clear();
            if (bonuses == null) return;
            foreach (var bonus in bonuses)
            {
                if (double.IsNaN(bonus.SecondsLeft) || double.IsInfinity(bonus.SecondsLeft) || bonus.SecondsLeft <= 0) continue;
                if (double.IsNaN(bonus.Value) || double.IsInfinity(bonus.Value) || bonus.Value < 0) continue;
                if (!Enum.IsDefined(typeof(BonusKind), bonus.Kind) || !Enum.IsDefined(typeof(BonusSource), bonus.Source)) continue;
                double duration = double.IsNaN(bonus.Duration) || bonus.Duration < bonus.SecondsLeft ? bonus.SecondsLeft : bonus.Duration;
                _bonuses.Add(new ActiveBonus(bonus.Kind, bonus.Value, duration, bonus.SecondsLeft, bonus.Source, bonus.Tier));
            }
        }
    }
}
