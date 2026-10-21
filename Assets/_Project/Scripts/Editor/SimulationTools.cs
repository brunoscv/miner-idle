using System;
using System.Diagnostics;
using System.Globalization;
using MinersIdle.Core;
using MinersIdle.Game;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Roda o loop de mineração sem cena, com os valores de <c>Data/Balance_Prototipo</c>, e escreve o resultado no Console
    /// (Marco 2, E2). Pelo menu ou em batch:
    ///   Unity.exe -batchmode -nographics -projectPath &lt;pasta&gt; -executeMethod MinersIdle.EditorTools.SimulationTools.SimulateTenMinutesBatch -logFile Logs/sim.log
    /// </summary>
    public static class SimulationTools
    {
        const double TenMinutes = 600;

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
