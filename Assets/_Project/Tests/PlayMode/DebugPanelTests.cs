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
            var stage = Object.FindAnyObjectByType<DescentStage>();
            var cameraPanel = Object.FindAnyObjectByType<DescentCameraPanel>();
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
            Assert.That(hud.ShardsText, Is.EqualTo(NumberFormatter.FormatShards(simulation.State.Shards)));

            int count = simulation.Crystals.Count;
            Click("Content/Buttons/SpawnCrystal");
            Assert.That(simulation.Crystals.Count, Is.EqualTo(count + 1));
            stage.Draw();
            Assert.That(stage.DrawnCrystals, Is.EqualTo(simulation.Crystals.Count), "o corredor desenha a fila inteira");

            Assert.That(simulation.Miner.Target, Is.Not.Null, "o anão já escolheu um alvo");
            long breaks = simulation.State.Breaks;
            Click("Content/Buttons/BreakTarget");
            Assert.That(simulation.State.Breaks, Is.EqualTo(breaks + 1));
            stage.Draw();
            Assert.That(stage.DrawnCrystals, Is.EqualTo(simulation.Crystals.Count));

            // M6 (E3): "Câmera" no lugar do "Mostrar ordem" do mapa (D-61).
            Assert.That(cameraPanel.IsOpen, Is.False);
            Click("Content/Buttons/Camera");
            Assert.That(cameraPanel.IsOpen, Is.True, "abre os controles da câmera");

            ulong seed = runner.Seed;
            double progress = simulation.State.Shards;
            Click("Content/Buttons/RestartSameSeed");
            Assert.That(runner.Seed, Is.EqualTo(seed));
            Assert.That(runner.Simulation, Is.Not.SameAs(simulation), "fila nova");
            Assert.That(runner.Simulation.State.Shards, Is.EqualTo(progress), "Reiniciar mantém o progresso (M4)");
            stage.Draw();
            Assert.That(stage.DrawnCrystals, Is.EqualTo(runner.Simulation.Crystals.Count), "o corredor segue a partida nova");
            Assert.That(cameraPanel.IsOpen, Is.True, "os controles da câmera continuam abertos");
            Assert.That(hud.ShardsText, Is.EqualTo(NumberFormatter.FormatShards(progress)));

            Click("Content/Buttons/DeleteSave");
            Assert.That(runner.Simulation.State.Shards, Is.EqualTo(0), "Apagar save zera o progresso");
            Assert.That(hud.ShardsText, Is.EqualTo(NumberFormatter.FormatShards(0)));

            Click("Content/Buttons/RestartNewSeed");
            Assert.That(runner.Seed, Is.Not.EqualTo(seed));

            Click("Content/Buttons/Camera");
            Assert.That(cameraPanel.IsOpen, Is.False);
        }

        /// <summary>Marco 3, E3: os botões de compra usam a economia do asset e o HUD mostra os atributos novos.</summary>
        [UnityTest]
        public IEnumerator BuyButtons_ApplyTheUpgrade_AndTheHudFollows()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            _panel = Object.FindAnyObjectByType<DebugPanel>();
            var runner = Object.FindAnyObjectByType<MiningRunner>();
            var hud = Object.FindAnyObjectByType<HudView>();
            var simulation = runner.Simulation;
            var economy = simulation.Economy;
            Click("Toggle");

            Assert.That(hud.DamageText, Is.EqualTo(NumberFormatter.Format(simulation.Stats.Damage)));
            Assert.That(hud.SpeedText, Is.EqualTo(NumberFormatter.FormatSeconds(simulation.Stats.HitInterval)));

            // Sem lascas suficientes: nada muda.
            double damage = simulation.Stats.Damage;
            Click("Content/Buttons/BuyDoublePickaxe");
            Assert.That(simulation.State.LevelOf(UpgradeId.DoublePickaxe), Is.EqualTo(0));

            for (int i = 0; i < 10; i++) Click("Content/Buttons/AddShards");
            double shards = simulation.State.Shards;
            double pickaxeCost = economy.Get(UpgradeId.Pickaxe).CostAt(0);

            Click("Content/Buttons/BuyPickaxe");
            Assert.That(simulation.State.LevelOf(UpgradeId.Pickaxe), Is.EqualTo(1));
            Assert.That(simulation.State.Shards, Is.EqualTo(shards - pickaxeCost).Within(1e-9));
            Assert.That(simulation.Stats.Damage, Is.GreaterThan(damage));
            Assert.That(hud.DamageText, Is.EqualTo(NumberFormatter.Format(simulation.Stats.Damage)));
            Assert.That(hud.ShardsText, Is.EqualTo(NumberFormatter.FormatShards(simulation.State.Shards)));

            double interval = simulation.Stats.HitInterval;
            Click("Content/Buttons/BuySpeed");
            Assert.That(simulation.Stats.HitInterval, Is.LessThan(interval));
            Assert.That(hud.SpeedText, Is.EqualTo(NumberFormatter.FormatSeconds(simulation.Stats.HitInterval)));

            double walk = simulation.Stats.WalkSpeed;
            Click("Content/Buttons/BuyBoots");
            Assert.That(simulation.Stats.WalkSpeed, Is.GreaterThan(walk));

            Click("Content/Buttons/BuyMining");
            Assert.That(simulation.State.LevelOf(UpgradeId.Mining), Is.EqualTo(1));

            yield return null; // o painel atualiza as estatísticas no Update
            Assert.That(_panel.transform.Find("Content/Upgrades").GetComponent<TMPro.TMP_Text>().text, Does.Contain("nv 1"));
        }
    }
}
