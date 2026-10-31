using System;
using System.Collections.Generic;

namespace MinersIdle.Core
{
    /// <summary>O que o <see cref="EconomyReport"/> mediu numa partida simulada.</summary>
    public sealed class EconomyReportResult
    {
        internal EconomyReportResult(
            double? firstPurchaseAt, int purchases, double earned, double spent,
            IReadOnlyList<double> checkpoints, IReadOnlyList<double> shardsPerMinute, IReadOnlyList<int> levels)
        {
            FirstPurchaseAt = firstPurchaseAt;
            Purchases = purchases;
            Earned = earned;
            Spent = spent;
            Checkpoints = checkpoints;
            ShardsPerMinute = shardsPerMinute;
            Levels = levels;
        }

        /// <summary>Segundos de jogo até a 1ª compra; <c>null</c> se não houve compra.</summary>
        public double? FirstPurchaseAt { get; }
        public int Purchases { get; }
        /// <summary>Lascas ganhas no total (o saldo final mais o que foi gasto).</summary>
        public double Earned { get; }
        public double Spent { get; }
        /// <summary>Instantes medidos, em segundos de jogo.</summary>
        public IReadOnlyList<double> Checkpoints { get; }
        /// <summary>Ritmo em cada instante, em lascas/min: média dos 5 min anteriores a ele (ou desde o início).</summary>
        public IReadOnlyList<double> ShardsPerMinute { get; }
        /// <summary>Nível final de cada linha, na ordem de <see cref="UpgradeId"/>.</summary>
        public IReadOnlyList<int> Levels { get; }
    }

    /// <summary>
    /// Relatório de economia (Marco 3, E4): joga uma partida sem cena, com uma política simples de jogador — a cada segundo,
    /// junta para a melhoria <b>mais barata</b> disponível e compra assim que der — e mede quando sai a 1ª compra e o
    /// ritmo de lascas ao longo do tempo. Mede, não decide: a meta do protótipo (1ª compra em menos de 3 min, D-35) é
    /// conferida por quem lê. Determinístico pela semente.
    /// </summary>
    public static class EconomyReport
    {
        /// <summary>De quanto em quanto tempo de jogo o "jogador" olha a loja.</summary>
        public const double DecisionSeconds = 1;

        /// <summary>
        /// Janela do ritmo: lascas ganhas nos últimos 5 min. Uma janela de 1 min dava zero na economia do GDD, onde sai
        /// um cristal a cada ~100 s.
        /// </summary>
        public const int RateWindowSeconds = 300;

        static readonly UpgradeId[] Lines = (UpgradeId[])Enum.GetValues(typeof(UpgradeId));

        /// <param name="strike">Crítico do golpe automático (o relatório não toca). Sem ela, sem crítico.</param>
        public static EconomyReportResult Run(
            MiningRules rules, EconomyRules economy, ulong seed, double seconds, IReadOnlyList<double> checkpoints,
            StrikeRules strike = null)
        {
            if (seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (checkpoints == null) throw new ArgumentNullException(nameof(checkpoints));

            var simulation = new MiningSimulation(rules, new SeededRandom(seed), economy: economy, strike: strike);
            int steps = (int)Math.Ceiling(seconds / DecisionSeconds);
            var earnedAt = new double[steps + 1]; // lascas ganhas até o fim de cada segundo
            double spent = 0;
            double? firstPurchaseAt = null;
            int purchases = 0;

            for (int step = 1; step <= steps; step++)
            {
                simulation.Tick(DecisionSeconds);

                while (TryCheapest(simulation, out var id, out double cost))
                {
                    if (simulation.Purchase(id) != PurchaseResult.Purchased) break; // junta para a mais barata
                    spent += cost;
                    purchases++;
                    firstPurchaseAt ??= simulation.Elapsed;
                }

                earnedAt[step] = simulation.State.Shards + spent;
            }

            var rates = new double[checkpoints.Count];
            for (int i = 0; i < checkpoints.Count; i++)
            {
                int at = Math.Min(steps, Math.Max(0, (int)Math.Round(checkpoints[i] / DecisionSeconds)));
                int from = Math.Max(0, at - RateWindowSeconds);
                rates[i] = at > from ? (earnedAt[at] - earnedAt[from]) / ((at - from) * DecisionSeconds / 60) : 0;
            }

            var levels = new int[Lines.Length];
            for (int i = 0; i < Lines.Length; i++) levels[i] = simulation.State.LevelOf(Lines[i]);

            return new EconomyReportResult(
                firstPurchaseAt, purchases, earnedAt[steps], spent, checkpoints, rates, levels);
        }

        /// <summary>A linha mais barata ainda à venda (empate: a primeira na ordem da loja).</summary>
        static bool TryCheapest(MiningSimulation simulation, out UpgradeId cheapest, out double cost)
        {
            cheapest = default;
            cost = double.PositiveInfinity;
            foreach (var id in Lines)
            {
                if (id == UpgradeId.Letrado) continue; // a descida entra no relatório na E4 do M8
                double? next = Purchases.NextCost(simulation.State, simulation.Economy, id);
                if (next.HasValue && next.Value < cost)
                {
                    cheapest = id;
                    cost = next.Value;
                }
            }
            return !double.IsPositiveInfinity(cost);
        }
    }
}
