namespace MinersIdle.Core
{
    public enum PurchaseResult
    {
        Purchased,
        NotEnoughShards,
        /// <summary>Limite de compras atingido (ou linha desligada).</summary>
        MaxLevel,
        /// <summary>Uma linha do estagiário antes de contratá-lo (M7) e a Promoção antes do Treinar no limite.</summary>
        Locked,
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

        /// <summary>
        /// O custo com as regras do jogo: além do limite de compras, a Velocidade (do anão ou do estagiário) para quando o
        /// intervalo do golpe já está no piso (D-371: 0,1 s), para ninguém comprar à toa; a loja mostra MÁX.
        /// </summary>
        public static double? NextCost(GameState state, EconomyRules economy, UpgradeId id, MiningRules rules, Permanents permanents)
        {
            double? cost = NextCost(state, economy, id);
            return cost != null && rules != null && AtHitFloor(state, economy, id, rules, permanents) ? null : cost;
        }

        static bool AtHitFloor(GameState state, EconomyRules economy, UpgradeId id, MiningRules rules, Permanents permanents)
        {
            const double tolerance = 1e-9;
            switch (id)
            {
                case UpgradeId.Speed:
                    return MinerStats.Compute(rules, economy, state, permanents).HitInterval <= economy.MinHitInterval + tolerance;
                case UpgradeId.InternSpeed:
                    return InternStats.Compute(rules, economy, state, permanents).HitInterval <= economy.MinHitInterval + tolerance;
                default:
                    return false;
            }
        }

        /// <summary>Se o saldo paga o custo, com a folga de arredondamento.</summary>
        public static bool CanAfford(double shards, double cost)
        {
            double tolerance = System.Math.Max(CostTolerance, cost * RelativeCostTolerance);
            return shards >= cost - tolerance;
        }

        /// <summary>
        /// As da árvore do estagiário (M7, §5.4) só depois de contratá-lo; contratar, só depois do andar do marco (GDD v8: 5,
        /// P7). <c>null</c> = liberado (as outras linhas nunca travam; a trava do Letrado saiu com o Mapa no v8, D-256).
        /// </summary>
        public static PurchaseResult? Blocked(GameState state, MiningRules rules, UpgradeId id, Permanents permanents = null)
        {
            if (EconomyRules.IsInternTree(id)) return state.LevelOf(UpgradeId.Intern) > 0 ? (PurchaseResult?)null : PurchaseResult.Locked;
            if (id == UpgradeId.Intern && rules != null && !rules.IsInternUnlocked(DeepestFloor(state, permanents)))
                return PurchaseResult.Locked;
            return null;
        }

        /// <summary>O andar mais fundo já alcançado (o recorde das permanentes, D-253), ou o de agora, o que for maior.</summary>
        public static int DeepestFloor(GameState state, Permanents permanents) =>
            System.Math.Max(state.Cave, permanents?.State.DeepestFloor ?? 1);

        /// <param name="rules">Para as travas (<see cref="Blocked"/>).</param>
        public static PurchaseResult TryPurchase(GameState state, EconomyRules economy, UpgradeId id,
                                                 MiningRules rules = null, Permanents permanents = null)
        {
            double? cost = rules != null ? NextCost(state, economy, id, rules, permanents) : NextCost(state, economy, id);
            if (cost == null) return PurchaseResult.MaxLevel;
            var blocked = rules != null || EconomyRules.IsInternTree(id) ? Blocked(state, rules, id, permanents) : null;
            if (blocked != null) return blocked.Value;
            if (!CanAfford(state.Shards, cost.Value)) return PurchaseResult.NotEnoughShards;

            state.SpendShards(cost.Value);
            state.RaiseLevel(id);
            return PurchaseResult.Purchased;
        }
    }
}
