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
    /// <summary>
    /// Marco 2, E5: cada botão do painel de debug está ligado e faz o que diz. Desde 2026-11-05 (D-147) os atalhos ficam nos
    /// menus Loja, Efeitos e Caverna, que abrem em cima como a câmera, um de cada vez.
    /// </summary>
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
            Click("ShopMenu/Content/Buttons/AddShards");
            Assert.That(simulation.State.Shards, Is.EqualTo(shards + DebugPanel.ShardsToAdd));
            Assert.That(hud.ShardsText, Is.EqualTo(NumberFormatter.FormatShards(simulation.State.Shards)));

            int count = simulation.Crystals.Count;
            Click("ShopMenu/Content/Buttons/SpawnCrystal");
            Assert.That(simulation.Crystals.Count, Is.EqualTo(count + 1));
            stage.Draw();
            Assert.That(stage.DrawnCrystals, Is.EqualTo(simulation.Crystals.Count), "o corredor desenha a fila inteira");

            Assert.That(simulation.Miner.Target, Is.Not.Null, "o anão já escolheu um alvo");
            long breaks = simulation.State.Breaks;
            Click("CaveMenu/Content/Buttons/BreakTarget");
            Assert.That(simulation.State.Breaks, Is.EqualTo(breaks + 1));
            stage.Draw();
            Assert.That(stage.DrawnCrystals, Is.EqualTo(simulation.Crystals.Count));

            // M6 (E3): "Câmera" no lugar do "Mostrar ordem" do mapa (D-61).
            Assert.That(cameraPanel.IsOpen, Is.False);
            Click("Content/Buttons/Camera");
            Assert.That(cameraPanel.IsOpen, Is.True, "abre os controles da câmera");

            ulong seed = runner.Seed;
            double progress = simulation.State.Shards;
            Click("CaveMenu/Content/Buttons/RestartSameSeed");
            Assert.That(runner.Seed, Is.EqualTo(seed));
            Assert.That(runner.Simulation, Is.Not.SameAs(simulation), "fila nova");
            Assert.That(runner.Simulation.State.Shards, Is.EqualTo(progress), "Reiniciar mantém o progresso (M4)");
            stage.Draw();
            Assert.That(stage.DrawnCrystals, Is.EqualTo(runner.Simulation.Crystals.Count), "o corredor segue a partida nova");
            Assert.That(cameraPanel.IsOpen, Is.True, "os controles da câmera continuam abertos");
            Assert.That(hud.ShardsText, Is.EqualTo(NumberFormatter.FormatShards(progress)));

            Click("CaveMenu/Content/Buttons/DeleteSave");
            Assert.That(runner.Simulation.State.Shards, Is.EqualTo(0), "Apagar save zera o progresso");
            Assert.That(hud.ShardsText, Is.EqualTo(NumberFormatter.FormatShards(0)));

            Click("CaveMenu/Content/Buttons/RestartNewSeed");
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

            // Sem lascas suficientes: nada muda (a Dupla, que era o teste daqui, saiu no GDD v7, D-136).
            double damage = simulation.Stats.Damage;
            double pickaxeCostNow = economy.Get(UpgradeId.Pickaxe).CostAt(0);
            if (simulation.State.Shards < pickaxeCostNow)
            {
                Click("ShopMenu/Content/Buttons/BuyPickaxe");
                Assert.That(simulation.State.LevelOf(UpgradeId.Pickaxe), Is.EqualTo(0), "sem lascas, não compra");
            }

            for (int i = 0; i < 10; i++) Click("ShopMenu/Content/Buttons/AddShards");
            double shards = simulation.State.Shards;
            double pickaxeCost = economy.Get(UpgradeId.Pickaxe).CostAt(0);

            Click("ShopMenu/Content/Buttons/BuyPickaxe");
            Assert.That(simulation.State.LevelOf(UpgradeId.Pickaxe), Is.EqualTo(1));
            Assert.That(simulation.State.Shards, Is.EqualTo(shards - pickaxeCost).Within(1e-9));
            Assert.That(simulation.Stats.Damage, Is.GreaterThan(damage));
            Assert.That(hud.DamageText, Is.EqualTo(NumberFormatter.Format(simulation.Stats.Damage)));
            Assert.That(hud.ShardsText, Is.EqualTo(NumberFormatter.FormatShards(simulation.State.Shards)));

            double interval = simulation.Stats.HitInterval;
            Click("ShopMenu/Content/Buttons/BuySpeed");
            Assert.That(simulation.Stats.HitInterval, Is.LessThan(interval));
            Assert.That(hud.SpeedText, Is.EqualTo(NumberFormatter.FormatSeconds(simulation.Stats.HitInterval)));

            // O Crítico (GDD v7, D-136) entrou no lugar das Botas: +5% de chance no golpe e no toque.
            double crit = simulation.Stats.CritBonus;
            Click("ShopMenu/Content/Buttons/BuyCritical");
            if (economy.Get(UpgradeId.Critical).IsEnabled)
            {
                Assert.That(simulation.State.LevelOf(UpgradeId.Critical), Is.EqualTo(1));
                Assert.That(simulation.Stats.CritBonus, Is.GreaterThan(crit));
            }
            else
                Assert.That(simulation.State.LevelOf(UpgradeId.Critical), Is.EqualTo(0), "linha desligada: nada comprado");

            Click("ShopMenu/Content/Buttons/BuyMining");
            Assert.That(simulation.State.LevelOf(UpgradeId.Mining), Is.EqualTo(1));

            yield return null; // o painel atualiza as estatísticas no Update
            Assert.That(_panel.transform.Find("ShopMenu/Content/Upgrades").GetComponent<TMPro.TMP_Text>().text, Does.Contain("nv 1"));
        }

        /// <summary>
        /// D-147: o DBG só tem o tempo, a câmera e os menus; cada menu abre em cima, fica aberto com o DBG fechado, fecha os
        /// outros (e a câmera) ao abrir, e tem o seu Fechar. Os atalhos de cada menu estão no lugar certo.
        /// </summary>
        [UnityTest]
        public IEnumerator Menus_OpenOneAtATime_AndHoldTheirShortcuts()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            _panel = Object.FindAnyObjectByType<DebugPanel>();
            var cameraPanel = Object.FindAnyObjectByType<DescentCameraPanel>();
            var shop = _panel.transform.Find("ShopMenu")?.GetComponent<DebugMenu>();
            var effects = _panel.transform.Find("EffectsMenu")?.GetComponent<DebugMenu>();
            var cave = _panel.transform.Find("CaveMenu")?.GetComponent<DebugMenu>();
            Assert.That(shop, Is.Not.Null, "menu Loja (rode o setup do projeto)");
            Assert.That(effects, Is.Not.Null, "menu Efeitos");
            Assert.That(cave, Is.Not.Null, "menu Caverna");

            var main = _panel.transform.Find("Content/Buttons");
            Assert.That(main.Cast<Transform>().Select(t => t.name),
                        Is.EqualTo(new[] { "Speed1", "Speed5", "Speed20", "Camera", "Shop", "Effects", "Cave", "Calc", "Day" }),
                        "o DBG fica só com o tempo, a câmera e os menus (Cálculos desde a E3b-3, Dia desde o Marco 8b)");
            Assert.That(_panel.transform.Find("ShopMenu/Content/Buttons/BuyDoublePickaxe"), Is.Null, "a Dupla saiu (D-136)");
            foreach (var name in new[] { "AutoTap", "ForceCritical", "Shake", "Sound" })
                Assert.That(_panel.transform.Find("EffectsMenu/Content/Buttons/" + name), Is.Not.Null, name);
            foreach (var name in new[] { "BreakTarget", "NextCave", "SaveNow", "RestartSameSeed", "RestartNewSeed", "DeleteSave", "ResetStats" })
                Assert.That(_panel.transform.Find("CaveMenu/Content/Buttons/" + name), Is.Not.Null, name);

            Assert.That(shop.IsOpen || effects.IsOpen || cave.IsOpen, Is.False, "os menus começam fechados");
            Click("Toggle");
            Click("Content/Buttons/Shop");
            Assert.That(shop.IsOpen, Is.True, "Loja abre");
            Click("Toggle");
            Assert.That(_panel.IsOpen, Is.False);
            Assert.That(shop.IsOpen, Is.True, "a Loja fica aberta com o DBG fechado");

            Click("Toggle");
            Click("Content/Buttons/Effects");
            Assert.That(effects.IsOpen, Is.True);
            Assert.That(shop.IsOpen, Is.False, "abrir um menu fecha o outro");

            Click("Content/Buttons/Camera");
            Assert.That(cameraPanel.IsOpen, Is.True);
            Assert.That(effects.IsOpen, Is.False, "a câmera fecha os menus");

            Click("Content/Buttons/Cave");
            Assert.That(cave.IsOpen, Is.True);
            Assert.That(cameraPanel.IsOpen, Is.False, "um menu fecha a câmera");
            Click("Content/Buttons/Cave");
            Assert.That(cave.IsOpen, Is.False, "o mesmo botão fecha");

            Click("Content/Buttons/Shop");
            Click("ShopMenu/Content/Buttons/Close");
            Assert.That(shop.IsOpen, Is.False, "Fechar fecha o menu");
        }
    }
}
