namespace MinersIdle.Core
{
    public enum PurchaseResult
    {
        Purchased,
        NotEnoughShards,
        /// <summary>Limite de compras atingido (ou linha desligada).</summary>
        MaxLevel,
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

        public static PurchaseResult TryPurchase(GameState state, EconomyRules economy, UpgradeId id)
        {
            double? cost = NextCost(state, economy, id);
            if (cost == null) return PurchaseResult.MaxLevel;
            double tolerance = System.Math.Max(CostTolerance, cost.Value * RelativeCostTolerance);
            if (state.Shards < cost.Value - tolerance) return PurchaseResult.NotEnoughShards;

            state.SpendShards(cost.Value);
            state.RaiseLevel(id);
            return PurchaseResult.Purchased;
        }
    }
}
