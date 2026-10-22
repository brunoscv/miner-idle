using System.Collections;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using MinersIdle.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MinersIdle.Tests
{
    /// <summary>Marco 2, E3 (no corredor desde o M6, E3): a cena Game roda o loop sozinha e o desenho acompanha a simulação.</summary>
    public class MiningSceneTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        [UnityTest]
        public IEnumerator GameScene_RunsTheLoopByItself_AndViewsFollowTheSimulation()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var runner = Object.FindAnyObjectByType<MiningRunner>();
            var stage = Object.FindAnyObjectByType<DescentStage>();
            Assert.That(runner, Is.Not.Null, "a cena Game precisa do objeto Mining (rode o setup do projeto)");
            Assert.That(stage, Is.Not.Null, "a cena Game precisa da descida (rode o setup do projeto)");
            Assert.That(runner.Simulation, Is.Not.Null);

            var simulation = runner.Simulation;
            double start = simulation.Miner.Distance;
            var minerOnScreen = stage.MinerOnScreen;
            runner.TimeScale = 20f;

            // ~3 s reais × 20 = ~1 min de jogo: dá para quebrar alguns cristais (~12 s cada).
            yield return new WaitForSecondsRealtime(3f);

            // Congela a simulação e deixa passar um quadro para o desenho alcançar o último estado.
            runner.enabled = false;
            yield return null;

            Assert.That(simulation.Elapsed, Is.GreaterThan(20), "o tempo de jogo andou");
            Assert.That(simulation.State.Breaks, Is.GreaterThan(0), "quebrou cristais sem nenhum toque");
            Assert.That(simulation.State.Shards, Is.EqualTo(simulation.State.Breaks * simulation.Rules.CrystalReward));
            Assert.That(simulation.Miner.Distance, Is.GreaterThan(start), "o anão correu pela fila");
            Assert.That(stage.DrawnCrystals, Is.EqualTo(simulation.Crystals.Count), "um desenho por cristal vivo");

            // M6: o anão corre parado na tela; quem anda é a fila, desenhada em relação a ele (0,5 m por unidade, D-58).
            Assert.That(Vector3.Distance(stage.MinerOnScreen, minerOnScreen), Is.LessThan(1e-3f), "o anão não sai do lugar na tela");
            foreach (var crystal in simulation.Crystals)
            {
                float ahead = (float)((crystal.Distance - simulation.Miner.Distance) * stage.MetersPerUnit);
                Assert.That(stage.CrystalLocalPosition(crystal).z, Is.EqualTo(stage.Framing.distance - ahead).Within(1e-4f),
                            "cristal " + crystal.Id);
            }

            var hud = Object.FindAnyObjectByType<HudView>();
            Assert.That(hud, Is.Not.Null, "a cena Game precisa do HUD (rode o setup do projeto)");
            Assert.That(hud.ShardsText, Is.EqualTo(NumberFormatter.Format(simulation.State.Shards)), "LASCAS no HUD = lascas do jogo");
        }
    }
}
