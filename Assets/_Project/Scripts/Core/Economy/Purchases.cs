namespace MinersIdle.Core
{
    public enum PurchaseResult
    {
        Purchased,
        NotEnoughShards,
        /// <summary>Limite de compras atingido (ou linha desligada).</summary>
        MaxLevel,
        /// <summary>Letrado (M8, D-93): faltam cristais quebrados na caverna atual.</summary>
        Locked,
        /// <summary>Letrado (M8): o desta caverna já foi comprado; falta passar de caverna.</summary>
        PassFirst,
    }

    /// <summary>
    /// Regra de compra (REQ-ECO-08): só conclui com lascas suficientes e limite não atingido; uma chamada faz no máximo
    /// uma compra. Não sabe de eventos nem de atributos: quem chama (a simulação, na E2) recalcula e avisa.
    /// </summary>
    public static class Purchases
    {
        /// <summary>
        /// Folga para o custo não falhar por arredondamento de <c>double</c> (Arquitetura, D-T4): absoluta para valores
        /// pequenos, relativa para os grandes (a Velocidade do GDD chega a ~10²³, onde o erro passa de milhões).
        /// </summary>
        public const double CostTolerance = 1e-9;
        public const double RelativeCostTolerance = 1e-12;

        /// <summary>Custo da próxima compra da linha, ou <c>null</c> se ela já está no limite.</summary>
        public static double? NextCost(GameState state, EconomyRules economy, UpgradeId id)
        {
            var line = economy.Get(id);
            int level = state.LevelOf(id);
            return line.IsMaxed(level) ? (double?)null : line.CostAt(level);
        }

        /// <summary>Se o saldo paga o custo, com a folga de arredondamento.</summary>
        public static bool CanAfford(double shards, double cost)
        {
            double tolerance = System.Math.Max(CostTolerance, cost * RelativeCostTolerance);
            return shards >= cost - tolerance;
        }

        /// <summary>
        /// A trava do Letrado (M8, GDD v5, D-93): o nível <i>n</i> só se compra na caverna <i>n</i> (um por caverna) e depois
        /// de <see cref="MiningRules.LetradoBreaks"/> cristais quebrados nela. <c>null</c> = liberado (as outras linhas
        /// nunca travam).
        /// </summary>
        public static PurchaseResult? Blocked(GameState state, MiningRules rules, UpgradeId id)
        {
            if (id != UpgradeId.Letrado) return null;
            if (state.LevelOf(id) >= state.Cave) return PurchaseResult.PassFirst;
            if (state.CaveBreaks < rules.LetradoBreaks) return PurchaseResult.Locked;
            return null;
        }

        /// <param name="rules">Para a trava do Letrado (<see cref="Blocked"/>). Sem ela, nada trava.</param>
        public static PurchaseResult TryPurchase(GameState state, EconomyRules economy, UpgradeId id,
                                                 MiningRules rules = null)
        {
            double? cost = NextCost(state, economy, id);
            if (cost == null) return PurchaseResult.MaxLevel;
            var blocked = rules != null ? Blocked(state, rules, id) : null;
            if (blocked != null) return blocked.Value;
            if (!CanAfford(state.Shards, cost.Value)) return PurchaseResult.NotEnoughShards;

            state.SpendShards(cost.Value);
            state.RaiseLevel(id);
            return PurchaseResult.Purchased;
        }
    }
}
