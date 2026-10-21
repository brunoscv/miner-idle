using System.Collections;
using System.Linq;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using MinersIdle.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MinersIdle.Tests
{
    /// <summary>Marco 2, E5: cada botão do painel de debug está ligado e faz o que diz.</summary>
    public class DebugPanelTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        DebugPanel _panel;

        void Click(string path)
        {
            var target = _panel.transform.Find(path);
            Assert.That(target, Is.Not.Null, "botão não encontrado: " + path);
            target.GetComponent<Button>().onClick.Invoke();
        }

        [UnityTest]
        public IEnumerator EveryShortcut_DoesWhatItSays()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null; // um Update da partida: o anão já escolheu o alvo

            _panel = Object.FindAnyObjectByType<DebugPanel>();
            var runner = Object.FindAnyObjectByType<MiningRunner>();
            var field = Object.FindAnyObjectByType<CrystalFieldView>();
            var line = Object.FindAnyObjectByType<TargetLineView>();
            var hud = Object.FindAnyObjectByType<HudView>();
            Assert.That(_panel, Is.Not.Null, "a cena Game precisa do painel de debug (rode o setup do projeto)");
            Assert.That(Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>(), Is.Not.Null, "sem EventSystem os botões não recebem toque");

            Assert.That(_panel.IsOpen, Is.False, "começa fechado");
            Click("Toggle");
            Assert.That(_panel.IsOpen, Is.True);

            Click("Content/Buttons/Speed20");
            Assert.That(runner.TimeScale, Is.EqualTo(20f));
            Click("Content/Buttons/Speed5");
            Assert.That(runner.TimeScale, Is.EqualTo(5f));
            Click("Content/Buttons/Speed1");
            Assert.That(runner.TimeScale, Is.EqualTo(1f));

            var simulation = runner.Simulation;
            double shards = simulation.State.Shards;
            Click("Content/Buttons/AddShards");
            Assert.That(simulation.State.Shards, Is.EqualTo(shards + DebugPanel.ShardsToAdd));
            Assert.That(hud.ShardsText, Is.EqualTo(NumberFormatter.Format(simulation.State.Shards)));

            int count = simulation.Crystals.Count;
            Click("Content/Buttons/SpawnCrystal");
            Assert.That(simulation.Crystals.Count, Is.EqualTo(count + 1));
            Assert.That(field.ActiveCount, Is.EqualTo(simulation.Crystals.Count));

            Assert.That(simulation.Miner.Target, Is.Not.Null, "o anão já escolheu um alvo");
            long breaks = simulation.State.Breaks;
            Click("Content/Buttons/BreakTarget");
            Assert.That(simulation.State.Breaks, Is.EqualTo(breaks + 1));
            Assert.That(field.ActiveCount, Is.EqualTo(simulation.Crystals.Count));

            Click("Content/Buttons/ShowOrder");
            Assert.That(field.ActiveViews.All(v => v.IsShowingBatch), Is.True, "número do lote em todos os cristais");
            yield return null;
            yield return null;
            Assert.That(line.IsDrawn, Is.True, "linha até o alvo");

            ulong seed = runner.Seed;
            Click("Content/Buttons/RestartSameSeed");
            Assert.That(runner.Seed, Is.EqualTo(seed));
            Assert.That(runner.Simulation, Is.Not.SameAs(simulation), "partida nova");
            Assert.That(runner.Simulation.State.Shards, Is.EqualTo(0));
            Assert.That(field.ActiveCount, Is.EqualTo(runner.Simulation.Crystals.Count));
            Assert.That(field.ActiveViews.All(v => v.IsShowingBatch), Is.True, "\"Mostrar ordem\" continua ligado");
            Assert.That(hud.ShardsText, Is.EqualTo(NumberFormatter.Format(0)));

            Click("Content/Buttons/RestartNewSeed");
            Assert.That(runner.Seed, Is.Not.EqualTo(seed));

            Click("Content/Buttons/ShowOrder");
            Assert.That(field.ActiveViews.Any(v => v.IsShowingBatch), Is.False);
            yield return null;
            Assert.That(line.IsDrawn, Is.False);
        }
    }
}
