using System;

namespace MinersIdle.Core
{
    /// <summary>
    /// O que o estagiário minerou com o jogo fechado ou minimizado (M9, E1). Só a conta: nada toca, nada vibra, nada anda
    /// na tela (D-191). Sai de <see cref="OfflineMining.Compute"/>.
    /// </summary>
    public readonly struct OfflineReport
    {
        public OfflineReport(double secondsAway, double secondsCounted, bool hired, long crystals, long hitsPerCrystal,
                             double secondsPerCrystal, double shardsPerCrystal, double shards)
        {
            SecondsAway = secondsAway;
            SecondsCounted = secondsCounted;
            Hired = hired;
            Crystals = crystals;
            HitsPerCrystal = hitsPerCrystal;
            SecondsPerCrystal = secondsPerCrystal;
            ShardsPerCrystal = shardsPerCrystal;
            Shards = shards;
        }

        /// <summary>O tempo fora pelo relógio do aparelho (negativo: o relógio voltou, D-196).</summary>
        public double SecondsAway { get; }
        /// <summary>O tempo que contou: o fora, até o teto (6 h, D-189); 0 com o relógio voltado.</summary>
        public double SecondsCounted { get; }
        /// <summary>Havia estagiário contratado (sem ele, nada acontece offline, D-139).</summary>
        public bool Hired { get; }
        /// <summary>Cristais que ele quebrou (não contam na trava do Mapa nem passam de nível, D-189).</summary>
        public long Crystals { get; }
        /// <summary>Golpes dele por cristal: ⌈vida ÷ dano⌉.</summary>
        public long HitsPerCrystal { get; }
        /// <summary>Segundos por cristal: a corrida até ele e os golpes (D-193).</summary>
        public double SecondsPerCrystal { get; }
        /// <summary>Lascas de cada cristal, já com a Mineração dele.</summary>
        public double ShardsPerCrystal { get; }
        /// <summary>As lascas que ele trouxe.</summary>
        public double Shards { get; }

        /// <summary>O relógio do aparelho voltou (hora de agora antes da do save): offline 0, sem erro (D-196).</summary>
        public bool ClockWentBack => SecondsAway < 0;
        /// <summary>Ficou fora mais que o teto: o resto se perdeu.</summary>
        public bool Capped => SecondsAway > SecondsCounted && !ClockWentBack;
    }

    /// <summary>
    /// A volta que espera o COLETAR (M9, E2, D-197): o que o estagiário trouxe e ainda não entrou no saldo. Fica no estado
    /// da run e no save, para fechar e abrir o app no meio do modal não pagar duas vezes; voltas seguidas sem coletar somam.
    /// </summary>
    public readonly struct OfflineReturn
    {
        public OfflineReturn(double secondsAway, double secondsCounted, long crystals, double shards, int level)
        {
            SecondsAway = Math.Max(0, Clean(secondsAway));
            SecondsCounted = Math.Max(0, Clean(secondsCounted));
            Crystals = Math.Max(0, crystals);
            Shards = Math.Max(0, Clean(shards));
            Level = Math.Max(0, level);
        }

        /// <summary>O tempo fora pelo relógio.</summary>
        public double SecondsAway { get; }
        /// <summary>O tempo que contou (até o teto de cada volta).</summary>
        public double SecondsCounted { get; }
        public long Crystals { get; }
        /// <summary>As lascas que entram no COLETAR.</summary>
        public double Shards { get; }
        /// <summary>O nível em que ele minerou (o da última volta).</summary>
        public int Level { get; }

        /// <summary>Há lascas esperando o COLETAR.</summary>
        public bool HasShards => Shards > 0;
        /// <summary>Ficou fora mais que o teto: o modal mostra o que contou ("máx. 6 h").</summary>
        public bool Capped => SecondsAway > SecondsCounted + 1e-6;

        /// <summary>Esta volta mais <paramref name="report"/> (mais uma volta sem coletar), no <paramref name="level"/>.</summary>
        public OfflineReturn Add(OfflineReport report, int level) =>
            new OfflineReturn(SecondsAway + Math.Max(0, report.SecondsAway), SecondsCounted + report.SecondsCounted,
                              Crystals + report.Crystals, Shards + report.Shards, level);

        static double Clean(double value) => double.IsNaN(value) || double.IsInfinity(value) ? 0 : value;
    }

    /// <summary>Um trecho do offline com os mesmos atributos do começo ao fim (<see cref="OfflineMining.Stretch"/>).</summary>
    public readonly struct OfflineStretch
    {
        public OfflineStretch(long crystals, double shards, double leftoverSeconds)
        {
            Crystals = crystals;
            Shards = shards;
            LeftoverSeconds = leftoverSeconds;
        }

        public long Crystals { get; }
        public double Shards { get; }
        /// <summary>O tempo que sobrou no meio de um cristal: passa para o trecho seguinte (ou se perde no último).</summary>
        public double LeftoverSeconds { get; }
    }

    /// <summary>
    /// A conta do offline (M9, D-193): o ritmo do jogo aberto sem o anão. Só o estagiário minera, com tudo o que é dele
    /// (Picareta dele × Treinar, Velocidade dele, Mineração dele; sem crítico, D-167), nos cristais do nível em que o
    /// jogador parou; nada do anão (o Coração Ardente não vale, D-194). Por cristal: a corrida até ele (o espaço mínimo
    /// entre cristais ÷ a corrida das regras: 2 ÷ 4 = 0,5 s) e os golpes, com o primeiro na chegada no modo A2
    /// (<see cref="ArrivalStrike"/>, o mesmo do jogo aberto). Uma conta fechada: 6 h custam o mesmo que 1 min.
    ///
    /// Em trechos (<see cref="Stretch"/>): no marco dos Bônus temporários, a carta do raro que ainda estiver ativa vale
    /// para ele até acabar (D-181, D-190); cada pedaço de tempo com outros atributos vira um trecho, e o tempo no meio de
    /// um cristal passa de um para o outro.
    /// </summary>
    public static class OfflineMining
    {
        /// <summary>A conta do tempo fora, com o estado da run e as permanentes de agora.</summary>
        public static OfflineReport Compute(MiningRules rules, EconomyRules economy, GameState state, Permanents permanents,
                                            double secondsAway)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (economy == null) throw new ArgumentNullException(nameof(economy));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (double.IsNaN(secondsAway)) throw new ArgumentOutOfRangeException(nameof(secondsAway));

            var intern = InternStats.Compute(rules, economy, state, permanents);
            double counted = secondsAway <= 0 ? 0 : Math.Min(secondsAway, rules.OfflineMaxSeconds);
            if (!intern.IsHired)
                return new OfflineReport(secondsAway, counted, false, 0, 0, 0, 0, 0);

            double health = rules.CrystalHealthIn(state.Cave);
            double reward = rules.CrystalRewardIn(state.Cave) * intern.OfflineRewardMultiplier;
            long hits = HitsPerCrystal(health, intern.Damage);
            double perCrystal = SecondsPerCrystal(rules, hits, intern.HitInterval);
            var stretch = Stretch(perCrystal, reward, counted);
            return new OfflineReport(secondsAway, counted, true, stretch.Crystals, hits, perCrystal, reward, stretch.Shards);
        }

        /// <summary>Golpes para quebrar um cristal: ⌈vida ÷ dano⌉ (a folga evita um golpe a mais por arredondamento).</summary>
        public static long HitsPerCrystal(double health, double damage)
        {
            if (damage <= 0) throw new ArgumentOutOfRangeException(nameof(damage));
            return Math.Max(1, (long)Math.Ceiling(health / damage - 1e-9));
        }

        /// <summary>
        /// Segundos por cristal: a corrida e os golpes. A2 (o do GDD v7): o 1º golpe na chegada, os outros a cada
        /// intervalo. Espera (o jogo até a D-124): um intervalo antes de cada golpe. A1: o relógio enche correndo, até um
        /// intervalo.
        /// </summary>
        public static double SecondsPerCrystal(MiningRules rules, long hits, double interval)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            double run = rules.MinCrystalSpacing / rules.WalkSpeed;
            switch (rules.ArrivalStrike)
            {
                case ArrivalStrike.Immediate:
                    return run + (hits - 1) * interval;
                case ArrivalStrike.Charge:
                    return run + (interval - Math.Min(run, interval)) + (hits - 1) * interval;
                default:
                    return run + hits * interval;
            }
        }

        /// <summary>
        /// Um trecho: quantos cristais cabem em <paramref name="seconds"/> (mais o que sobrou do trecho anterior), a
        /// <paramref name="secondsPerCrystal"/> cada, pagando <paramref name="shardsPerCrystal"/>.
        /// </summary>
        public static OfflineStretch Stretch(double secondsPerCrystal, double shardsPerCrystal, double seconds,
                                             double carriedSeconds = 0)
        {
            if (secondsPerCrystal <= 0) throw new ArgumentOutOfRangeException(nameof(secondsPerCrystal));
            double time = Math.Max(0, seconds) + Math.Max(0, carriedSeconds);
            long crystals = (long)Math.Floor(time / secondsPerCrystal + 1e-9);
            double leftover = Math.Max(0, time - crystals * secondsPerCrystal);
            return new OfflineStretch(crystals, crystals * shardsPerCrystal, leftover);
        }
    }
}
