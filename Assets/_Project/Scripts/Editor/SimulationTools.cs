using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using MinersIdle.Core;
using MinersIdle.Game;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Roda o jogo sem cena e escreve o resultado no Console: "Simular 10 minutos" (Marco 2, E2) e "Relatório de economia"
    /// (Marco 3, E4, com <see cref="EconomyReport"/>). Pelo menu ou em batch:
    ///   Unity.exe -batchmode -nographics -projectPath &lt;pasta&gt; -executeMethod MinersIdle.EditorTools.SimulationTools.SimulateTenMinutesBatch -logFile Logs/sim.log
    ///   Unity.exe -batchmode -nographics -projectPath &lt;pasta&gt; -executeMethod MinersIdle.EditorTools.SimulationTools.EconomyReportBatch -logFile Logs/economia.log
    /// </summary>
    public static class SimulationTools
    {
        const double TenMinutes = 600;

        const double ReportSeconds = 30 * 60;
        static readonly double[] ReportCheckpoints = { 5 * 60, 10 * 60, 30 * 60 };
        static readonly ulong[] ReportSeeds = { 1, 2, 3 };

        /// <summary>Meta do protótipo para a 1ª compra (D-35).</summary>
        public const double FirstPurchaseTargetSeconds = 180;

        [MenuItem("Tools/Miners Idle/Debug/Relatório de economia")]
        public static void EconomyReportMenu() => Debug.Log(BuildEconomyReport());

        public static void EconomyReportBatch()
        {
            try
            {
                Debug.Log(BuildEconomyReport());
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// 30 min de jogo com a política "junta para a mais barata", para o protótipo, o GDD v1 e o GDD v5 (D-87), com três
        /// sementes. O v5 só tem a caverna 1 (as cavernas entram no M8).
        /// </summary>
        public static string BuildEconomyReport()
        {
            ProjectSetup.EnsurePrototypeBalance();
            var text = new StringBuilder();
            text.AppendLine("[Economia] 30 min de jogo, comprando sempre a melhoria mais barata assim que der.");
            foreach (var path in new[] { ProjectSetup.PrototypeBalancePath, ProjectSetup.GddBalancePath, ProjectSetup.GddV5BalancePath })
            {
                var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(path);
                if (asset == null)
                {
                    text.AppendLine($"  {path}: não existe (rode o setup do projeto).");
                    continue;
                }

                text.AppendLine($"  {asset.name}:");
                foreach (var seed in ReportSeeds)
                {
                    var result = EconomyReport.Run(asset.ToRules(), asset.ToEconomy(), seed, ReportSeconds, ReportCheckpoints, asset.ToStrike());
                    text.AppendLine("    " + Describe(result, seed));
                }
            }
            return text.ToString();
        }

        static string Describe(EconomyReportResult result, ulong seed)
        {
            string first = result.FirstPurchaseAt.HasValue
                ? $"1ª compra aos {FormatTime(result.FirstPurchaseAt.Value)}" +
                  (result.FirstPurchaseAt.Value <= FirstPurchaseTargetSeconds ? " (meta < 3 min ok)" : " (ACIMA da meta de 3 min)")
                : "nenhuma compra";
            string rates = string.Join(" · ", result.Checkpoints.Select((t, i) =>
                $"{FormatTime(t)}: {NumberFormatter.Format(result.ShardsPerMinute[i])}/min"));
            var lines = (UpgradeId[])Enum.GetValues(typeof(UpgradeId));
            string levels = string.Join(", ", lines.Select((id, i) => $"{id} {result.Levels[i]}"));
            return $"semente {seed}: {first}; ritmo {rates}; {result.Purchases} compras; " +
                   $"ganhou {NumberFormatter.Format(result.Earned)}, gastou {NumberFormatter.Format(result.Spent)}; níveis: {levels}";
        }

        static string FormatTime(double seconds) =>
            seconds < 60
                ? $"{seconds:0} s"
                : string.Format(CultureInfo.InvariantCulture, "{0}min{1:00}", (int)(seconds / 60), (int)(seconds % 60));

        [MenuItem("Tools/Miners Idle/Debug/Simular 10 minutos")]
        public static void SimulateTenMinutes() => Debug.Log(Simulate(TenMinutes, NewSeed()));

        public static void SimulateTenMinutesBatch()
        {
            try
            {
                Debug.Log(Simulate(TenMinutes, NewSeed()));
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public static string Simulate(double seconds, ulong seed)
        {
            ProjectSetup.EnsurePrototypeBalance();
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(ProjectSetup.PrototypeBalancePath);
            if (asset == null)
                throw new InvalidOperationException($"Não consegui carregar {ProjectSetup.PrototypeBalancePath}.");

            var simulation = new MiningSimulation(asset.ToRules(), new SeededRandom(seed));
            var clock = Stopwatch.StartNew();
            simulation.Tick(seconds);
            clock.Stop();

            var state = simulation.State;
            return string.Format(CultureInfo.InvariantCulture,
                "[Simulação] {0:0.#} min de jogo, semente {1}: lascas {2:0.##}, golpes {3}, quebras {4}, " +
                "{5:0.00} lascas/min, {6} cristais na tela. Calculado em {7} ms.",
                seconds / 60, seed, state.Shards, state.Hits, state.Breaks,
                state.Shards / (seconds / 60), simulation.Crystals.Count, clock.ElapsedMilliseconds);
        }

        static ulong NewSeed() => (ulong)DateTime.UtcNow.Ticks;
    }
}
