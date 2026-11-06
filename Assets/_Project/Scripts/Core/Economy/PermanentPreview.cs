namespace MinersIdle.Core
{
    /// <summary>
    /// O que a aba das permanentes mostra de uma linha (Marco 8b, E4, D-155): nível, limite, custo em lascas vermelhas,
    /// estado do botão e o efeito "de → para". Sai das mesmas regras da compra (<see cref="Permanents"/>), então o que a
    /// loja promete é o que a compra entrega.
    /// </summary>
    public readonly struct PermanentPreview
    {
        PermanentPreview(PermanentId id, int level, int maxLevel, double? nextCost, UpgradeButtonState state,
                         double currentValue, double nextValue)
        {
            Id = id;
            Level = level;
            MaxLevel = maxLevel;
            NextCost = nextCost;
            State = state;
            CurrentValue = currentValue;
            NextValue = nextValue;
        }

        public PermanentId Id { get; }
        public int Level { get; }
        public int MaxLevel { get; }
        /// <summary>Custo da próxima compra, em lascas vermelhas; <c>null</c> no limite.</summary>
        public double? NextCost { get; }
        /// <summary><see cref="UpgradeButtonState.Purchasable"/>, <see cref="UpgradeButtonState.NotEnoughShards"/>,
        /// <see cref="UpgradeButtonState.Maxed"/> ou <see cref="UpgradeButtonState.Locked"/> (a Promoção antes do Treinar no
        /// limite, M7).</summary>
        public UpgradeButtonState State { get; }

        /// <summary>
        /// O que a linha soma agora, efeito × nível (D-158): cristais a menos na trava do Mapa (Memória), a fração do
        /// bônus (Sangue 0,1 = +10%, Coração 0,5 = +50%, Dedos 0,05 = +5%, Treinar 0,1 = +10%), os níveis de Picareta da
        /// run nova (Alma) ou 1 com a Promoção comprada.
        /// </summary>
        public double CurrentValue { get; }
        /// <summary>O mesmo depois da próxima compra (igual a <see cref="CurrentValue"/> no limite).</summary>
        public double NextValue { get; }

        public bool IsMaxed => State == UpgradeButtonState.Maxed;
        /// <summary>Linha desligada no balanço ativo (limite 0, o GDD v5): some da aba.</summary>
        public bool IsEnabled => MaxLevel > 0;

        public static PermanentPreview For(Permanents permanents, PermanentId id)
        {
            var line = permanents.Rules.Get(id);
            int level = permanents.EffectiveLevel(id);
            double? cost = permanents.NextCost(id);
            var state = cost == null ? UpgradeButtonState.Maxed
                : permanents.IsLocked(id) ? UpgradeButtonState.Locked
                : Purchases.CanAfford(permanents.State.RedShards, cost.Value) ? UpgradeButtonState.Purchasable
                : UpgradeButtonState.NotEnoughShards;
            double current = line.Effect * level;
            double next = cost == null ? current : line.Effect * (level + 1);
            return new PermanentPreview(id, level, line.MaxLevel, cost, state, current, next);
        }
    }
}
