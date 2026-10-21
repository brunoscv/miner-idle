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
    /// <summary>Marco 2, E3: a cena Game roda o loop sozinha e o desenho acompanha a simulação.</summary>
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
            var field = Object.FindAnyObjectByType<CrystalFieldView>();
            var minerView = Object.FindAnyObjectByType<MinerView>();
            Assert.That(runner, Is.Not.Null, "a cena Game precisa do objeto Mining (rode o setup do projeto)");
            Assert.That(field, Is.Not.Null);
            Assert.That(minerView, Is.Not.Null);
            Assert.That(runner.Simulation, Is.Not.Null);

            var simulation = runner.Simulation;
            var start = simulation.Miner.Position;
            runner.TimeScale = 20f;

            // ~3 s reais × 20 = ~1 min de jogo: dá para quebrar alguns cristais (~12 s cada).
            yield return new WaitForSecondsRealtime(3f);

            // Congela a simulação e deixa passar um quadro para o desenho alcançar o último estado.
            runner.enabled = false;
            yield return null;

            Assert.That(simulation.Elapsed, Is.GreaterThan(20), "o tempo de jogo andou");
            Assert.That(simulation.State.Breaks, Is.GreaterThan(0), "quebrou cristais sem nenhum toque");
            Assert.That(simulation.State.Shards, Is.EqualTo(simulation.State.Breaks * simulation.Rules.CrystalReward));
            Assert.That(Vec2.Distance(simulation.Miner.Position, start), Is.GreaterThan(0), "o anão saiu do lugar");
            Assert.That(field.ActiveCount, Is.EqualTo(simulation.Crystals.Count), "um desenho por cristal vivo");

            var drawn = minerView.transform.position;
            Assert.That(drawn.x, Is.EqualTo((float)simulation.Miner.Position.X).Within(1e-3f));
            Assert.That(drawn.y, Is.EqualTo((float)simulation.Miner.Position.Y).Within(1e-3f));

            var hud = Object.FindAnyObjectByType<HudView>();
            Assert.That(hud, Is.Not.Null, "a cena Game precisa do HUD (rode o setup do projeto)");
            Assert.That(hud.ShardsText, Is.EqualTo(NumberFormatter.Format(simulation.State.Shards)), "LASCAS no HUD = lascas do jogo");
        }
    }
}
