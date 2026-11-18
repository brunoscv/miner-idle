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
                             double secondsPerCrystal, double shardsPerCrystal, double shards,
                             int fromFloor = 0, int toFloor = 0, long endBreaks = 0)
        {
            FromFloor = fromFloor;
            ToFloor = Math.Max(fromFloor, toFloor);
            EndBreaks = Math.Max(0, endBreaks);
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
        /// <summary>Cristais que ele quebrou: contam na barra do andar e, com o automático, passam de andar (D-246, M9c E4).</summary>
        public long Crystals { get; }
        /// <summary>Golpes dele por cristal: ⌈vida ÷ dano⌉.</summary>
        public long HitsPerCrystal { get; }
        /// <summary>Segundos por cristal: a corrida até ele e os golpes (D-193).</summary>
        public double SecondsPerCrystal { get; }
        /// <summary>Lascas de cada cristal, já com a Mineração dele.</summary>
        public double ShardsPerCrystal { get; }
        /// <summary>As lascas que ele trouxe.</summary>
        public double Shards { get; }
        /// <summary>O andar em que o jogador deixou o jogo (M9c E4).</summary>
        public int FromFloor { get; }
        /// <summary>O andar em que ele está na volta: o mesmo, ou mais fundo com o avanço automático (D-246).</summary>
        public int ToFloor { get; }
        /// <summary>Os cristais na barra do <see cref="ToFloor"/> na volta (sem o automático, a barra pode estar cheia, P4).</summary>
        public long EndBreaks { get; }
        /// <summary>Quantos andares ele desceu sozinho.</summary>
        public int FloorsDescended => ToFloor - FromFloor;

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
        public OfflineReturn(double secondsAway, double secondsCounted, long crystals, double shards, int level, int fromLevel = 0)
        {
            SecondsAway = Math.Max(0, Clean(secondsAway));
            SecondsCounted = Math.Max(0, Clean(secondsCounted));
            Crystals = Math.Max(0, crystals);
            Shards = Math.Max(0, Clean(shards));
            Level = Math.Max(0, level);
            FromLevel = fromLevel > 0 ? Math.Min(fromLevel, Level) : Level; // save de antes da E4: sem descida
        }

        /// <summary>O tempo fora pelo relógio.</summary>
        public double SecondsAway { get; }
        /// <summary>O tempo que contou (até o teto de cada volta).</summary>
        public double SecondsCounted { get; }
        public long Crystals { get; }
        /// <summary>As lascas que entram no COLETAR.</summary>
        public double Shards { get; }
        /// <summary>O andar em que ele está na volta (o da última volta).</summary>
        public int Level { get; }
        /// <summary>O andar em que o jogador deixou o jogo (a primeira volta sem COLETAR); igual ao <see cref="Level"/> sem descida.</summary>
        public int FromLevel { get; }
        /// <summary>Ele desceu andares sozinho (o modal mostra "12 » 15", M9c E4).</summary>
        public bool Descended => Level > FromLevel;

        /// <summary>Há lascas esperando o COLETAR.</summary>
        public bool HasShards => Shards > 0;
        /// <summary>Ficou fora mais que o teto: o modal mostra o que contou ("máx. 6 h").</summary>
        public bool Capped => SecondsAway > SecondsCounted + 1e-6;

        /// <summary>
        /// Esta volta mais <paramref name="report"/> (mais uma volta sem coletar), no <paramref name="level"/>. O andar de
        /// partida é o da primeira volta.
        /// </summary>
        public OfflineReturn Add(OfflineReport report, int level)
        {
            int from = HasShards ? FromLevel : report.FromFloor > 0 ? report.FromFloor : level;
            return new OfflineReturn(SecondsAway + Math.Max(0, report.SecondsAway), SecondsCounted + report.SecondsCounted,
                                     Crystals + report.Crystals, Shards + report.Shards, level, from);
        }

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
    ///
    /// Andares (M9c E4, D-246): os cristais dele contam na barra do andar. Com o avanço automático, ao encher a barra ele
    /// passa ao andar seguinte na hora (sem transição: offline é só a conta, D-191) e continua lá, com a vida e as lascas
    /// de lá; cada andar é mais um trecho. Sem o automático, a barra enche e fica cheia (P4). O andar 100 espera o toque
    /// (D-270).
    /// </summary>
    public static class OfflineMining
    {
        /// <summary>
        /// O teto do tempo contado (GDD v8, D-253): 4 h; 6 h para quem já chegou ao andar 20 (o mais fundo de todas as runs,
        /// <see cref="PermanentState.DeepestFloor"/>, ou o andar de agora, o que for maior).
        /// </summary>
        public static double MaxSeconds(MiningRules rules, GameState state, Permanents permanents)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            int deepest = Math.Max(state?.Cave ?? 1, permanents?.State.DeepestFloor ?? 1);
            return rules.OfflineMaxSecondsFor(deepest);
        }

        /// <summary>
        /// A conta do tempo fora, com o estado da run e as permanentes de agora. <paramref name="autoAdvance"/>: o avanço
        /// automático vale (o marco do andar 5 e o ajuste ligado, <see cref="MiningSimulation.IsAutoAdvancing"/>).
        /// </summary>
        public static OfflineReport Compute(MiningRules rules, EconomyRules economy, GameState state, Permanents permanents,
                                            double secondsAway, bool autoAdvance = false)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (economy == null) throw new ArgumentNullException(nameof(economy));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (double.IsNaN(secondsAway)) throw new ArgumentOutOfRangeException(nameof(secondsAway));

            var intern = InternStats.Compute(rules, economy, state, permanents);
            double counted = secondsAway <= 0 ? 0 : Math.Min(secondsAway, MaxSeconds(rules, state, permanents));
            int floor = state.Cave;
            long breaks = state.CaveBreaks;
            if (!intern.IsHired)
                return new OfflineReport(secondsAway, counted, false, 0, 0, 0, 0, 0, floor, floor, breaks);

            // O relatório mostra o ritmo do andar de partida, sem cartas.
            double reward = rules.CrystalRewardIn(floor) * intern.OfflineRewardMultiplier;
            long hits = HitsPerCrystal(rules.CrystalHealthIn(floor), intern.Damage);
            double spacing = permanents?.CrystalSpacing(rules) ?? rules.MinCrystalSpacing; // com o Atalho (M9d)
            double perCrystal = SecondsPerCrystal(rules, hits, intern.HitInterval, spacing);

            // Trechos: cada pedaço de tempo com as mesmas cartas do raro (M9b E4, D-181, D-190) e no mesmo andar (M9c E4).
            // O tempo no meio de um cristal passa para o trecho seguinte.
            bool cards = HasInternCard(state.Bonuses, counted);
            var bonuses = state.Bonuses.All;
            long crystals = 0;
            double shards = 0, carried = 0, from = 0;
            while (true)
            {
                // Barra cheia com o automático: o andar seguinte, sem gastar tempo (o andar 100 espera o toque, D-270).
                if (autoAdvance && floor < rules.CaveCount && breaks >= Requirement(rules, permanents, floor))
                {
                    floor++;
                    breaks = 0;
                    continue;
                }
                if (from >= counted - Epsilon) break;

                double until = counted, damage = 1, divisor = 1, multiplier = 1;
                if (cards)
                {
                    for (int i = 0; i < bonuses.Count; i++)
                    {
                        var bonus = bonuses[i];
                        if (!IsInternCard(bonus) || bonus.SecondsLeft <= from) continue;
                        until = Math.Min(until, bonus.SecondsLeft);
                        if (bonus.Kind == BonusKind.DamageMultiplier) damage *= bonus.Value;
                        else if (bonus.Kind == BonusKind.IntervalDivisor) divisor *= bonus.Value;
                        else multiplier *= bonus.Value;
                    }
                }
                double floorReward = rules.CrystalRewardIn(floor) * intern.OfflineRewardMultiplier * multiplier;
                long floorHits = HitsPerCrystal(rules.CrystalHealthIn(floor), intern.Damage * damage);
                double floorPerCrystal = SecondsPerCrystal(rules, floorHits, intern.HitInterval / divisor, spacing);
                var stretch = Stretch(floorPerCrystal, floorReward, until - from, carried);

                long missing = autoAdvance && floor < rules.CaveCount
                    ? Requirement(rules, permanents, floor) - breaks
                    : long.MaxValue;
                if (stretch.Crystals >= missing)
                {
                    // A barra enche neste trecho: conta só até o cristal que a enche; o resto do tempo é do andar seguinte.
                    crystals += missing;
                    shards += missing * floorReward;
                    breaks += missing;
                    from = Math.Min(until, from + Math.Max(0, missing * floorPerCrystal - carried));
                    carried = 0;
                    continue;
                }
                crystals += stretch.Crystals;
                shards += stretch.Shards;
                breaks += stretch.Crystals;
                carried = stretch.LeftoverSeconds;
                from = until;
            }
            return new OfflineReport(secondsAway, counted, true, crystals, hits, perCrystal, reward, shards,
                                     state.Cave, floor, breaks);
        }

        const double Epsilon = 1e-9;

        /// <summary>Os cristais que o andar pede (D-239): com a Memória, se houver permanentes.</summary>
        static long Requirement(MiningRules rules, Permanents permanents, int floor) =>
            permanents?.FloorRequirement(rules, floor) ?? Math.Max(0, rules.FloorBreaksIn(floor));

        /// <summary>Uma carta do raro que muda o estagiário: dano ×, intervalo ÷ ou lascas × (o crítico não, D-167).</summary>
        static bool IsInternCard(ActiveBonus bonus) =>
            ActiveBonuses.AppliesToIntern(bonus) && bonus.Value > 0 &&
            (bonus.Kind == BonusKind.DamageMultiplier || bonus.Kind == BonusKind.IntervalDivisor || bonus.Kind == BonusKind.ShardsMultiplier);

        static bool HasInternCard(ActiveBonuses bonuses, double counted)
        {
            if (counted <= 0) return false;
            for (int i = 0; i < bonuses.Count; i++)
                if (IsInternCard(bonuses.All[i]) && bonuses.All[i].SecondsLeft > 0) return true;
            return false;
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
        /// <param name="spacing">A distância entre cristais, com o Atalho (M9d); <c>null</c> = a do balanço.</param>
        public static double SecondsPerCrystal(MiningRules rules, long hits, double interval, double? spacing = null)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            double run = (spacing ?? rules.MinCrystalSpacing) / rules.WalkSpeed;
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
