using System;
using System.Collections.Generic;

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
                             int fromFloor = 0, int toFloor = 0, long endBreaks = 0, double maxSeconds = 0, int rares = 0,
                             double extraSeconds = 0)
        {
            MaxSeconds = maxSeconds;
            ExtraSeconds = Math.Max(0, extraSeconds);
            Rares = Math.Max(0, rares);
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
        /// <summary>O teto que valeu nesta volta: o do andar e, com anúncios ativos ao fechar, as horas extras (D-254).</summary>
        public double MaxSeconds { get; }
        /// <summary>Quanto do <see cref="MaxSeconds"/> veio dos anúncios ativos ao fechar (D-244, D-254); 0 sem eles.</summary>
        public double ExtraSeconds { get; }
        /// <summary>Raros que ele quebrou no offline, um por intervalo do contador (D-284).</summary>
        public int Rares { get; }
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
        public OfflineReturn(double secondsAway, double secondsCounted, long crystals, double shards, int level, int fromLevel = 0,
                             double maxSeconds = 0, double extraSeconds = 0, int rares = 0)
        {
            MaxSeconds = Math.Max(0, Clean(maxSeconds));
            ExtraSeconds = Math.Max(0, Clean(extraSeconds));
            Rares = Math.Max(0, rares);
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
        /// <summary>O teto da última volta (M9e E3); 0 nos saves de antes (o modal faz a conta de agora).</summary>
        public double MaxSeconds { get; }
        /// <summary>As horas extras dos anúncios na última volta (D-244, D-254): a linha "Anúncios +3 h" do modal.</summary>
        public double ExtraSeconds { get; }
        /// <summary>Raros que ele quebrou fora, somando as voltas sem coletar (D-284).</summary>
        public int Rares { get; }
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
                                     Crystals + report.Crystals, Shards + report.Shards, level, from,
                                     report.MaxSeconds, report.ExtraSeconds, Rares + report.Rares);
        }

        static double Clean(double value) => double.IsNaN(value) || double.IsInfinity(value) ? 0 : value;
    }

    /// <summary>A carta de um raro quebrado no offline (D-284): quando, em segundos desde que o app fechou, e a regra dela.</summary>
    public readonly struct OfflineRareCard
    {
        public OfflineRareCard(double at, RareCardRule rule)
        {
            At = at;
            Rule = rule;
        }

        public double At { get; }
        public RareCardRule Rule { get; }
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
        /// <param name="ads">
        /// Os anúncios do Boost (M9e E2): cada um ativo no <paramref name="state"/> soma as horas dele (+1, +2, +4 h, D-254),
        /// com o teto deles (8 h, D-244), também depois do andar 20. <c>null</c> = sem anúncios.
        /// </param>
        public static double MaxSeconds(MiningRules rules, GameState state, Permanents permanents, AdOffers ads = null)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            int deepest = Math.Max(state?.Cave ?? 1, permanents?.State.DeepestFloor ?? 1);
            double max = rules.OfflineMaxSecondsFor(deepest);
            if (ads == null || !ads.IsEnabled || state == null) return max;
            double extra = 0;
            foreach (var offer in ads.All)
                if (state.Bonuses.AdSecondsLeft(offer.Kind) > 0) extra += offer.OfflineSeconds;
            return extra > 0 ? Math.Min(max + extra, Math.Max(max, ads.OfflineCapSeconds)) : max;
        }

        /// <summary>
        /// A conta do tempo fora, com o estado da run e as permanentes de agora. <paramref name="autoAdvance"/>: o avanço
        /// automático vale (o marco do andar 5 e o ajuste ligado, <see cref="MiningSimulation.IsAutoAdvancing"/>).
        /// </summary>
        /// <param name="ads">Os anúncios do Boost (M9e E2): as horas extras no teto (<see cref="MaxSeconds"/>).</param>
        /// <param name="rareCards">As cartas dos raros quebrados no offline (D-284), em ordem de tempo; <c>null</c> = nenhuma.</param>
        public static OfflineReport Compute(MiningRules rules, EconomyRules economy, GameState state, Permanents permanents,
                                            double secondsAway, bool autoAdvance = false, AdOffers ads = null,
                                            IReadOnlyList<OfflineRareCard> rareCards = null)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            if (economy == null) throw new ArgumentNullException(nameof(economy));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (double.IsNaN(secondsAway)) throw new ArgumentOutOfRangeException(nameof(secondsAway));

            var intern = InternStats.Compute(rules, economy, state, permanents);
            double max = MaxSeconds(rules, state, permanents, ads);
            double extra = Math.Max(0, max - MaxSeconds(rules, state, permanents));
            double counted = secondsAway <= 0 ? 0 : Math.Min(secondsAway, max);
            int floor = state.Cave;
            long breaks = state.CaveBreaks;
            if (!intern.IsHired)
                return new OfflineReport(secondsAway, counted, false, 0, 0, 0, 0, 0, floor, floor, breaks, max, 0, extra);

            // O relatório mostra o ritmo do andar de partida, sem cartas.
            double reward = rules.CrystalRewardIn(floor) * intern.OfflineRewardMultiplier;
            long hits = HitsPerCrystal(rules.CrystalHealthIn(floor), intern.Damage);
            double spacing = permanents?.CrystalSpacing(rules) ?? rules.MinCrystalSpacing; // com o Atalho (M9d)
            double perCrystal = SecondsPerCrystal(rules, hits, intern.HitInterval, spacing);

            // Trechos: cada pedaço de tempo com os mesmos bônus para ele — as cartas do raro (M9b E4, D-181, D-190), os
            // anúncios (M9e, D-246) e as cartas dos raros do offline (D-284) — e no mesmo andar (M9c E4). O tempo no meio
            // de um cristal passa para o trecho seguinte.
            var effects = InternEffects(state.Bonuses, rareCards, counted);
            int rares = 0;
            if (rareCards != null)
                for (int i = 0; i < rareCards.Count; i++)
                    if (rareCards[i].At <= counted + Epsilon) rares++;
            var cardMax = effects != null ? new double[Enum.GetValues(typeof(RareCard)).Length] : null;
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
                if (effects != null) until = Factors(effects, cardMax, from, until, out damage, out divisor, out multiplier);
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
                                     state.Cave, floor, breaks, max, rares, extra);
        }

        const double Epsilon = 1e-9;

        /// <summary>
        /// Um bônus para o estagiário no offline, com começo e fim em segundos desde que o app fechou: uma carta do raro
        /// (<see cref="Group"/> = a carta), um anúncio (<see cref="IsAd"/>) ou a carta de um raro do offline.
        /// </summary>
        readonly struct InternEffect
        {
            public InternEffect(double start, double end, BonusKind kind, double value, bool isAd, int group)
            {
                Start = start;
                End = end;
                Kind = kind;
                Value = value;
                IsAd = isAd;
                Group = group;
            }

            public double Start { get; }
            public double End { get; }
            public BonusKind Kind { get; }
            public double Value { get; }
            public bool IsAd { get; }
            public int Group { get; }
        }

        /// <summary>Os bônus que mudam o estagiário no offline; <c>null</c> sem nenhum.</summary>
        static List<InternEffect> InternEffects(ActiveBonuses bonuses, IReadOnlyList<OfflineRareCard> rareCards, double counted)
        {
            if (counted <= 0) return null;
            List<InternEffect> effects = null;
            for (int i = 0; i < bonuses.Count; i++)
            {
                var bonus = bonuses.All[i];
                if (!IsInternCard(bonus) || bonus.SecondsLeft <= 0) continue;
                (effects ??= new List<InternEffect>()).Add(new InternEffect(0, bonus.SecondsLeft, bonus.Kind, bonus.Value,
                                                                            ActiveBonuses.IsAdOffer(bonus), bonus.Tier));
            }
            if (rareCards != null)
                for (int i = 0; i < rareCards.Count; i++)
                {
                    var card = rareCards[i];
                    var kind = card.Rule.Kind;
                    if (card.At >= counted || !IsInternKind(kind)) continue; // as de crítico não fazem nada para ele (D-167)
                    (effects ??= new List<InternEffect>()).Add(new InternEffect(card.At, card.At + card.Rule.Seconds, kind,
                                                                                card.Rule.Value, false, (int)card.Rule.Card));
                }
            return effects;
        }

        /// <summary>
        /// Os multiplicadores do estagiário a partir de <paramref name="from"/>: a mesma carta ativa duas vezes (a do jogo e
        /// a de um raro do offline) vale uma (renova, D-243); entre anúncios, o maior (D-251); anúncio × cartas (D-287).
        /// Devolve até quando eles valem (o próximo começo ou fim).
        /// </summary>
        static double Factors(List<InternEffect> effects, double[] cardMax, double from, double until,
                              out double damage, out double divisor, out double multiplier)
        {
            Array.Clear(cardMax, 0, cardMax.Length);
            double adDamage = 1, adDivisor = 1, adShards = 1;
            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect.Start > from + Epsilon)
                {
                    until = Math.Min(until, effect.Start);
                    continue;
                }
                if (effect.End <= from + Epsilon) continue;
                until = Math.Min(until, effect.End);
                if (effect.IsAd)
                {
                    if (effect.Kind == BonusKind.DamageMultiplier) adDamage = Math.Max(adDamage, effect.Value);
                    else if (effect.Kind == BonusKind.IntervalDivisor) adDivisor = Math.Max(adDivisor, effect.Value);
                    else adShards = Math.Max(adShards, effect.Value);
                }
                else if (effect.Group >= 0 && effect.Group < cardMax.Length)
                    cardMax[effect.Group] = Math.Max(cardMax[effect.Group], effect.Value);
            }
            damage = adDamage;
            divisor = adDivisor;
            multiplier = adShards;
            for (int card = 0; card < cardMax.Length; card++)
            {
                if (cardMax[card] <= 0) continue;
                var kind = RareCardRule.KindOf((RareCard)card);
                if (kind == BonusKind.DamageMultiplier) damage *= cardMax[card];
                else if (kind == BonusKind.IntervalDivisor) divisor *= cardMax[card];
                else if (kind == BonusKind.ShardsMultiplier) multiplier *= cardMax[card];
            }
            return until;
        }

        static bool IsInternKind(BonusKind kind) =>
            kind == BonusKind.DamageMultiplier || kind == BonusKind.IntervalDivisor || kind == BonusKind.ShardsMultiplier;

        /// <summary>Os cristais que o andar pede (D-239): com a Memória, se houver permanentes.</summary>
        static long Requirement(MiningRules rules, Permanents permanents, int floor) =>
            permanents?.FloorRequirement(rules, floor) ?? Math.Max(0, rules.FloorBreaksIn(floor));

        /// <summary>
        /// Um bônus que muda o estagiário: carta do raro ou anúncio do v8 (D-246), em dano ×, intervalo ÷ ou lascas × (o
        /// crítico não, D-167).
        /// </summary>
        static bool IsInternCard(ActiveBonus bonus) =>
            ActiveBonuses.AppliesToIntern(bonus) && bonus.Value > 0 && IsInternKind(bonus.Kind);

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
