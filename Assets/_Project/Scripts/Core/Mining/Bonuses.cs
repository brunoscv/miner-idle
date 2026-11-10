using System;
using System.Collections.Generic;

namespace MinersIdle.Core
{
    /// <summary>O que um bônus com tempo faz (M9b). O cristal raro e o AD trazem os tipos deles nas E4 e E5.</summary>
    public enum BonusKind
    {
        /// <summary>+% no dano do golpe e do toque (D-178); os de vários bônus somam (D-179).</summary>
        DamagePercent,
        /// <summary>Todo golpe e todo toque saem críticos (D-178).</summary>
        GuaranteedCritical,
    }

    /// <summary>De onde o bônus veio.</summary>
    public enum BonusSource
    {
        /// <summary>Um marco do combo (×10, ×30, ×50, ×100), ao parar de tocar (D-96).</summary>
        Combo,
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
