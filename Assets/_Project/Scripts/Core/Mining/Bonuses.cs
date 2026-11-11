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
    }

    /// <summary>De onde o bônus veio.</summary>
    public enum BonusSource
    {
        /// <summary>Um marco do combo (×10, ×30, ×50, ×100), ao parar de tocar (D-96).</summary>
        Combo,
        /// <summary>Uma carta do cristal raro (D-190); o <see cref="ActiveBonus.Tier"/> é a <see cref="RareCard"/>.</summary>
        Rare,
    }

    /// <summary>As 4 cartas do cristal raro (D-190). O número vai no <see cref="ActiveBonus.Tier"/>: não reordenar.</summary>
    public enum RareCard
    {
        /// <summary>Dano ×2 (anão e estagiário).</summary>
        Damage = 0,
        /// <summary>Golpe 2× mais rápido: o intervalo ÷ 2 (anão e estagiário).</summary>
        Speed = 1,
        /// <summary>Lascas ×2 em cada cristal quebrado (e no offline do estagiário).</summary>
        Shards = 2,
        /// <summary>Crítico +25 pontos (só o anão: o estagiário não tem crítico, D-167).</summary>
        Critical = 3,
    }

    /// <summary>Uma carta do raro como está no balanço: o efeito e a duração (GDD v7: ×2 / ÷2 / ×2 / +0,25, 60 s, D-190).</summary>
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
                default: return BonusKind.CritChance;
            }
        }
    }

    /// <summary>
    /// As regras do cristal raro (M9b E4; D-97, D-184…D-186, D-190): a chance de cada cristal que nasce ser raro (o
    /// padrão e o ritmo lento do DBG), o tempo para escolher e as cartas. Sem cartas (ou com menos de 2), não há raro.
    /// </summary>
    public sealed class RareRules
    {
        readonly RareCardRule[] _byCard = new RareCardRule[4];
        readonly bool[] _has = new bool[4];

        public RareRules(double chance, double slowChance, double chooseSeconds, IEnumerable<RareCardRule> cards)
        {
            if (double.IsNaN(chance) || chance < 0 || chance > 1) throw new ArgumentOutOfRangeException(nameof(chance));
            if (double.IsNaN(slowChance) || slowChance < 0 || slowChance > 1) throw new ArgumentOutOfRangeException(nameof(slowChance));
            if (double.IsNaN(chooseSeconds) || chooseSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(chooseSeconds));
            Chance = chance;
            SlowChance = slowChance;
            ChooseSeconds = chooseSeconds;
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
        }

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

        public bool Has(RareCard card) => _has[(int)card];
        public RareCardRule Get(RareCard card) =>
            _has[(int)card] ? _byCard[(int)card] : throw new ArgumentException($"Sem a carta {card}.", nameof(card));

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

        /// <summary>A soma dos pontos a mais na chance de crítico (a carta "Crítico +25%"): 0,25 = +25.</summary>
        public double CritChance
        {
            get
            {
                double sum = 0;
                for (int i = 0; i < _bonuses.Count; i++)
                    if (_bonuses[i].Kind == BonusKind.CritChance) sum += _bonuses[i].Value;
                return sum;
            }
        }

        /// <summary>O dano × do estagiário: só as cartas do raro valem para ele (D-177, D-190).</summary>
        public double InternDamageMultiplier => Product(BonusKind.DamageMultiplier, internOnly: true);

        /// <summary>O intervalo ÷ do estagiário: só as cartas do raro (o boost do AD não vale para ele, D-177).</summary>
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

        /// <summary>Se o bônus vale também para o estagiário: só as cartas do raro (D-177, D-190).</summary>
        public static bool AppliesToIntern(ActiveBonus bonus) => bonus.Source == BonusSource.Rare;

        double Product(BonusKind kind, bool internOnly)
        {
            double product = 1;
            for (int i = 0; i < _bonuses.Count; i++)
                if (_bonuses[i].Kind == kind && _bonuses[i].Value > 0 && (!internOnly || AppliesToIntern(_bonuses[i])))
                    product *= _bonuses[i].Value;
            return product;
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
        /// Entra uma carta do raro (D-190): igual a outra ainda ativa (mesma origem e mesma carta), só renova o tempo, sem
        /// somar efeito; diferente, vale junto.
        /// </summary>
        internal void AddOrRenew(ActiveBonus bonus)
        {
            if (bonus.SecondsLeft <= 0) return;
            for (int i = 0; i < _bonuses.Count; i++)
            {
                var active = _bonuses[i];
                if (active.Source != bonus.Source || active.Tier != bonus.Tier) continue;
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
