using System;

namespace MinersIdle.Core
{
    /// <summary>Como o botão Comprar de uma linha da loja deve aparecer (S03).</summary>
    public enum UpgradeButtonState
    {
        Purchasable,
        /// <summary>Apagado, mostrando o custo que falta.</summary>
        NotEnoughShards,
        /// <summary>No limite ("máx."), ou compra única já feita ("comprada"), ou linha desligada.</summary>
        Maxed,
        /// <summary>Letrado (M8, D-109): apagado, com o progresso dos cristais da caverna no lugar do efeito.</summary>
        Locked,
        /// <summary>Letrado (M8): o desta caverna já foi comprado; falta passar de caverna.</summary>
        PassFirst,
    }

    /// <summary>
    /// O que a loja mostra de uma melhoria (Marco 4, E1): nível, limite, custo da próxima compra, estado do botão e o efeito
    /// "de → para". Calculada pelas mesmas regras da compra (<see cref="Purchases"/>) e dos atributos
    /// (<see cref="MinerStats"/>), então o que a loja promete é o que a compra entrega.
    /// </summary>
    public readonly struct UpgradePreview
    {
        UpgradePreview(UpgradeId id, int level, int maxLevel, double? nextCost, UpgradeButtonState state,
                       double currentValue, double nextValue, long progress, long progressGoal)
        {
            Progress = progress;
            ProgressGoal = progressGoal;
            Id = id;
            Level = level;
            MaxLevel = maxLevel;
            NextCost = nextCost;
            State = state;
            CurrentValue = currentValue;
            NextValue = nextValue;
        }

        public UpgradeId Id { get; }
        public int Level { get; }
        public int MaxLevel { get; }
        /// <summary>Custo da próxima compra; <c>null</c> no limite.</summary>
        public double? NextCost { get; }
        public UpgradeButtonState State { get; }

        /// <summary>
        /// O atributo que a linha melhora, agora: dano (Picareta e Dupla), intervalo em segundos (Velocidade), multiplicador
        /// de lascas (Mineração, 1,08 = +8%) ou caminhada (Botas).
        /// </summary>
        public double CurrentValue { get; }
        /// <summary>O mesmo atributo depois da próxima compra (igual a <see cref="CurrentValue"/> no limite).</summary>
        public double NextValue { get; }

        /// <summary>Letrado: cristais quebrados na caverna atual, no máximo <see cref="ProgressGoal"/> (D-109).</summary>
        public long Progress { get; }
        /// <summary>Letrado: quantos cristais a trava pede; 0 nas outras linhas.</summary>
        public long ProgressGoal { get; }

        /// <summary>Linha que só se compra uma vez (Picareta Dupla): no limite, a loja mostra "comprada".</summary>
        public bool IsSinglePurchase => MaxLevel == 1;
        /// <summary>Linha desligada no asset (limite 0), como as Botas no GDD v1.</summary>
        public bool IsEnabled => MaxLevel > 0;

        public static UpgradePreview For(MiningRules rules, EconomyRules economy, GameState state, UpgradeId id)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            var line = economy?.Get(id) ?? throw new ArgumentNullException(nameof(economy));
            int level = state.LevelOf(id);
            double? cost = Purchases.NextCost(state, economy, id);

            var blocked = cost == null ? null : Purchases.Blocked(state, rules, id);
            var buttonState = cost == null ? UpgradeButtonState.Maxed
                : blocked == PurchaseResult.PassFirst ? UpgradeButtonState.PassFirst
                : blocked == PurchaseResult.Locked ? UpgradeButtonState.Locked
                : Purchases.CanAfford(state.Shards, cost.Value) ? UpgradeButtonState.Purchasable
                : UpgradeButtonState.NotEnoughShards;

            if (id == UpgradeId.Letrado)
            {
                // "Caverna 1 » 2": a caverna em que está e a que o Letrado abre.
                long goal = rules.LetradoBreaks;
                return new UpgradePreview(id, level, line.MaxLevel, cost, buttonState, state.Cave, state.Cave + 1,
                                          Math.Min(state.CaveBreaks, goal), goal);
            }

            double current = ValueOf(id, MinerStats.Compute(rules, economy, state));
            double next = cost == null ? current : ValueOf(id, MinerStats.Compute(rules, economy, state, raised: id));

            return new UpgradePreview(id, level, line.MaxLevel, cost, buttonState, current, next, 0, 0);
        }

        static double ValueOf(UpgradeId id, MinerStats stats)
        {
            switch (id)
            {
                case UpgradeId.Pickaxe:
                case UpgradeId.DoublePickaxe:
                    return stats.Damage;
                case UpgradeId.Speed:
                    return stats.HitInterval;
                case UpgradeId.Mining:
                    return stats.RewardMultiplier;
                case UpgradeId.Boots:
                    return stats.WalkSpeed;
                default:
                    throw new ArgumentOutOfRangeException(nameof(id), id, null);
            }
        }
    }
}
