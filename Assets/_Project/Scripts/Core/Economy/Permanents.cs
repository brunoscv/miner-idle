using System;

namespace MinersIdle.Core
{
    /// <summary>O que o resumo do dia mostra (D-154): o dia que terminou e o que a run deixou.</summary>
    public readonly struct RestSummary
    {
        public RestSummary(int day, int level, double playSeconds, double shardsEarned, double redShardsGained, double redShardsBank)
        {
            Day = day;
            Level = level;
            PlaySeconds = playSeconds;
            ShardsEarned = shardsEarned;
            RedShardsGained = redShardsGained;
            RedShardsBank = redShardsBank;
        }

        /// <summary>O dia que acabou de terminar.</summary>
        public int Day { get; }
        /// <summary>O nível alcançado na run.</summary>
        public int Level { get; }
        public double PlaySeconds { get; }
        /// <summary>Lascas comuns ganhas nas quebras da run.</summary>
        public double ShardsEarned { get; }
        /// <summary>Lascas vermelhas que o descanso levou para o banco.</summary>
        public double RedShardsGained { get; }
        /// <summary>O banco depois do descanso.</summary>
        public double RedShardsBank { get; }
    }

    /// <summary>
    /// As melhorias permanentes em jogo (Marco 8b): as regras (<see cref="PermanentRules"/>) e o estado
    /// (<see cref="PermanentState"/>) juntos, com os efeitos da D-158 já calculados para os atributos
    /// (<see cref="MinerStats"/>), a trava do Mapa (<see cref="Purchases"/>) e a run nova. Uma linha desligada no balanço
    /// (o GDD v5) não tem efeito, mesmo com nível guardado.
    /// </summary>
    public sealed class Permanents
    {
        public Permanents(PermanentRules rules, PermanentState state)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            State = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>Sem permanentes nem descanso, com um estado só seu (o jogo até o GDD v5 e os testes antigos).</summary>
        public static Permanents None() => new Permanents(PermanentRules.None, new PermanentState());

        public PermanentRules Rules { get; }
        public PermanentState State { get; }

        /// <summary>Nível que conta para o efeito: o guardado, até o limite da linha deste balanço.</summary>
        public int EffectiveLevel(PermanentId id) => Math.Min(State.LevelOf(id), Rules.Get(id).MaxLevel);

        double Effect(PermanentId id) => Rules.Get(id).Effect * EffectiveLevel(id);

        /// <summary>Sangue da Montanha: multiplica o dano final (D-158).</summary>
        public double DamageMultiplier => 1 + Effect(PermanentId.MountainBlood);

        /// <summary>
        /// Coração Ardente: a fração de lascas a mais em cada quebra (0,5 por nível; 20 × 50% = o teto de +1000%). Soma
        /// com a Mineração, cada uma no seu teto (D-208): Mineração 1 + Coração 1 = 1 + 1 + 0,5 = ×2,5.
        /// </summary>
        public double RewardBonus => Effect(PermanentId.BurningHeart);

        /// <summary>Dedos Nervosos: chance de crítico a mais no golpe (D-158; o toque saiu no M9g, D-311).</summary>
        public double CritBonus => Effect(PermanentId.NervousFingers);

        /// <summary>Dedo Fogoso: o que soma no dano do crítico (M9d; GDD v8.2: +0,5 por nível).</summary>
        public double CritDamageBonus => Effect(PermanentId.FieryFinger);

        /// <summary>Unidades do mundo por metro: 1 m = 2 unidades (D-58, D-252).</summary>
        public const double UnitsPerMeter = 2;

        /// <summary>
        /// A distância entre um cristal e o seguinte na fila, em unidades (M9d, D-252, D-277): a do balanço (GDD v8.2: 3 m)
        /// menos o Atalho (0,5 m por nível). Nunca abaixo de um décimo da do balanço (o v8 para em 0,5 m, bem acima).
        /// </summary>
        public double CrystalSpacing(MiningRules rules) =>
            Math.Max(rules.MinCrystalSpacing * 0.1, rules.MinCrystalSpacing - Effect(PermanentId.Shortcut) * UnitsPerMeter);

        /// <summary>
        /// Os cristais que o andar <paramref name="floor"/> pede para o Próximo Andar (GDD v8, D-239; v8.4: <c>andar × 5</c>)
        /// menos a Memória (5 por nível) e, com o recorde no marco do 60 (v8.4, M9j E2), menos 100; no mínimo 0 (aí a barra
        /// já nasce cheia).
        /// </summary>
        public long FloorRequirement(MiningRules rules, int floor)
        {
            var marks = Rules.Milestones;
            double discount = FloorMilestones.Reached(marks.DiscountFloor, State.DeepestFloor) ? marks.Discount : 0;
            return Math.Max(0, (long)Math.Round(rules.FloorBreaksIn(floor) - Effect(PermanentId.Memory) - discount));
        }

        /// <summary>Alma dos Anões: os níveis de Picareta com que a run começa.</summary>
        public int StartingPickaxe => (int)Math.Round(Effect(PermanentId.DwarfSoul));

        /// <summary>Treinar Estagiário: multiplica o dano do estagiário (M7; GDD v7: +10% por nível).</summary>
        public double InternDamageMultiplier => 1 + Effect(PermanentId.InternTraining);

        /// <summary>
        /// A run nova já começa com o estagiário contratado: pelo marco do andar 15 do GDD v8.4 (M9j E2, D-367) ou, nos
        /// balanços de antes, pela Promoção (M7, D-169).
        /// </summary>
        public bool StartsWithIntern =>
            FloorMilestones.Reached(Rules.Milestones.InternStartFloor, State.DeepestFloor)
            || EffectiveLevel(PermanentId.InternPromotion) > 0;

        /// <summary>Tempo Curto (GDD v8.4, M9j E3): os segundos a menos no intervalo do golpe do anão.</summary>
        public double ShortTimeSeconds => Effect(PermanentId.ShortTime);

        /// <summary>Bombardeador (GDD v8.4, M9j E3): os segundos a menos na carga da bomba.</summary>
        public double BombChargeReduction => Effect(PermanentId.Bomber);

        /// <summary>A bomba em uso (M9g, D-240): 0 = a Pequena, de graça; cada compra da linha Bomba passa para a próxima.</summary>
        public int BombLevel => EffectiveLevel(PermanentId.Bomb);

        /// <summary>Raro Dourado: as cartas do raro no tier 2 (GDD v8, D-286).</summary>
        public bool HasGoldenRare => EffectiveLevel(PermanentId.GoldenRare) > 0;

        /// <summary>
        /// O que a linha espera para abrir: o nível do Treinar que a Promoção pede (D-169; GDD v8.2: 5), ou o andar mais
        /// fundo que o Raro Dourado pede (D-286: 10); 0 numa linha que não espera nada.
        /// </summary>
        public int RequiredLevel(PermanentId id)
        {
            if (id == PermanentId.GoldenRare) return Rules.GoldenRareFloor;
            if (id != PermanentId.InternPromotion) return 0;
            int max = Rules.Get(PermanentId.InternTraining).MaxLevel;
            // GDD v8.2: a Promoção pede o Treinar no 5 (M9d); no v7, no limite (D-169).
            return Rules.PromotionTrainingLevel > 0 ? Math.Min(Rules.PromotionTrainingLevel, max) : max;
        }

        /// <summary>
        /// Se a linha ainda espera: a Promoção, o Treinar no nível pedido (GDD v7 §4.2: "requer estagiário treinado nível
        /// 10"); o Raro Dourado, o andar do raro (D-286).
        /// </summary>
        public bool IsLocked(PermanentId id)
        {
            if (id == PermanentId.GoldenRare) return State.DeepestFloor < Rules.GoldenRareFloor;
            if (id != PermanentId.InternPromotion) return false;
            var training = Rules.Get(PermanentId.InternTraining);
            return !training.IsEnabled || EffectiveLevel(PermanentId.InternTraining) < RequiredLevel(id);
        }

        /// <summary>O que a próxima compra custa em lascas vermelhas, ou <c>null</c> no limite (ou com a linha desligada).</summary>
        public double? NextCost(PermanentId id)
        {
            var line = Rules.Get(id);
            int level = State.LevelOf(id);
            return line.IsMaxed(level) ? (double?)null : line.CostAt(level);
        }

        /// <summary>Compra o próximo nível com lascas vermelhas: só com o banco suficiente e o limite não atingido.</summary>
        internal PurchaseResult TryPurchase(PermanentId id)
        {
            double? cost = NextCost(id);
            if (cost == null) return PurchaseResult.MaxLevel;
            if (IsLocked(id)) return PurchaseResult.Locked;
            if (!Purchases.CanAfford(State.RedShards, cost.Value)) return PurchaseResult.NotEnoughShards;

            State.SpendRedShards(cost.Value);
            State.RaiseLevel(id);
            return PurchaseResult.Purchased;
        }

        /// <summary>
        /// Atalho do debug: um nível de graça em cada linha que ainda sobe. A Promoção só sobe com o Treinar já no limite
        /// (a ordem do enum põe o Treinar antes: no passo em que ele chega ao limite, ela já vem junto).
        /// </summary>
        internal bool GrantLevels()
        {
            bool any = false;
            foreach (PermanentId id in Enum.GetValues(typeof(PermanentId)))
                any |= GrantLevel(id);
            return any;
        }

        /// <summary>Atalho do debug: um nível de graça na linha, se ela ainda sobe e não espera outra (<see cref="IsLocked"/>).</summary>
        internal bool GrantLevel(PermanentId id)
        {
            if (Rules.Get(id).IsMaxed(State.LevelOf(id)) || IsLocked(id)) return false;
            State.RaiseLevel(id);
            return true;
        }

        /// <summary>
        /// As lascas vermelhas que uma run no nível <paramref name="level"/> leva se descansar agora (D-138): a tabela e,
        /// com o recorde no marco do 45 (GDD v8.4, M9j E2, D-370), o dobro.
        /// </summary>
        public double Pending(int level) => Rules.IsRestEnabled ? Rules.RedShards.UpTo(level) * RedShardsMultiplier(level) : 0;

        /// <summary>
        /// O que multiplica a tabela das vermelhas (D-370): o Olho Treinado (+100% por nível) vezes o ×2 do marco do 45
        /// (contando o andar da run). Olho no 10 com o marco: ×22.
        /// </summary>
        double RedShardsMultiplier(int level) =>
            (1 + Effect(PermanentId.TrainedEye))
            * (FloorMilestones.Reached(Rules.Milestones.RedShardsDoubleFloor, Math.Max(State.DeepestFloor, level)) ? 2 : 1);

        /// <summary>Encerra o dia: as pendentes da run vão para o banco e o dia avança.</summary>
        internal RestSummary Rest(GameState run)
        {
            double gained = Pending(run.Cave);
            int day = State.Day;
            State.AddRedShards(gained);
            State.NextDay();
            return new RestSummary(day, run.Cave, run.PlaySeconds, run.ShardsEarned, gained, State.RedShards);
        }

        /// <summary>
        /// A run nova (D-137): nível 1, sem lascas, as melhorias base no zero, a não ser a Picareta, que começa com a Alma
        /// dos Anões (no máximo o limite da linha), como se comprada: a próxima compra custa o que custaria depois delas.
        /// Com a Promoção (M7, D-169), o estagiário já vem contratado, com a árvore dele no zero.
        /// </summary>
        public GameState NewRun(EconomyRules economy)
        {
            if (economy == null) throw new ArgumentNullException(nameof(economy));
            int pickaxe = StartingPickaxe;
            int intern = StartsWithIntern && economy.HasIntern ? 1 : 0;
            if (pickaxe <= 0 && intern == 0) return new GameState();
            return GameState.Restore(0, 0, 0, id => id == UpgradeId.Pickaxe ? pickaxe : id == UpgradeId.Intern ? intern : 0, economy);
        }
    }
}
