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

        /// <summary>Dedos Nervosos: chance de crítico a mais, no golpe e no toque (D-158).</summary>
        public double CritBonus => Effect(PermanentId.NervousFingers);

        /// <summary>Memória: os cristais que a trava do Mapa pede, no mínimo 0 (no máximo a Memória remove a trava).</summary>
        public long MapLock(MiningRules rules) => Math.Max(0, (long)Math.Round(rules.LetradoBreaks - Effect(PermanentId.Memory)));

        /// <summary>Alma dos Anões: os níveis de Picareta com que a run começa.</summary>
        public int StartingPickaxe => (int)Math.Round(Effect(PermanentId.DwarfSoul));

        /// <summary>Treinar Estagiário: multiplica o dano do estagiário (M7; GDD v7: +10% por nível).</summary>
        public double InternDamageMultiplier => 1 + Effect(PermanentId.InternTraining);

        /// <summary>Promoção do Estagiário: a run nova já começa com ele contratado (M7, D-169).</summary>
        public bool StartsWithIntern => EffectiveLevel(PermanentId.InternPromotion) > 0;

        /// <summary>
        /// Se a linha ainda espera outra: a Promoção só se compra com o Treinar no limite (GDD v7 §4.2: "requer estagiário
        /// treinado nível 10").
        /// </summary>
        /// <summary>
        /// O nível que a linha de que esta depende precisa ter para ela abrir (a Promoção pede o Treinar no limite, D-169);
        /// 0 numa linha que não espera outra.
        /// </summary>
        public int RequiredLevel(PermanentId id) =>
            id == PermanentId.InternPromotion ? Rules.Get(PermanentId.InternTraining).MaxLevel : 0;

        public bool IsLocked(PermanentId id)
        {
            if (id != PermanentId.InternPromotion) return false;
            var training = Rules.Get(PermanentId.InternTraining);
            return !training.IsEnabled || EffectiveLevel(PermanentId.InternTraining) < training.MaxLevel;
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

        /// <summary>As lascas vermelhas que uma run no nível <paramref name="level"/> leva se descansar agora (D-138).</summary>
        public double Pending(int level) => Rules.IsRestEnabled ? Rules.RedShards.UpTo(level) : 0;

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
