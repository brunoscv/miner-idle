using System.Collections;
using System.Linq;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MinersIdle.Tests
{
    /// <summary>Marco 4, E2: a loja abre, compra, mostra o estado certo de cada linha e não pausa a mineração.</summary>
    public class ShopTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        ShopView _shop;
        MiningRunner _runner;

        IEnumerator LoadGame()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            _shop = Object.FindAnyObjectByType<ShopView>();
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            Assert.That(_shop, Is.Not.Null, "a cena Game precisa da loja (rode o setup do projeto)");
        }

        void Click(string path) => _shop.transform.Find(path).GetComponent<Button>().onClick.Invoke();

        UpgradeRowView Row(UpgradeId id) => _shop.Rows.First(r => !r.IsLocked && r.Upgrade == id);

        [UnityTest]
        public IEnumerator OpenBuyAndClose()
        {
            yield return LoadGame();
            var simulation = _runner.Simulation;

            Assert.That(_shop.IsOpen, Is.False, "começa fechada");
            Assert.That(_shop.IsBadgeShown, Is.False, "sem lascas, sem selo");

            Click("ShopButton");
            Assert.That(_shop.IsOpen, Is.True);
            Assert.That(_shop.Rows.Count, Is.EqualTo(7), "5 melhorias + Estagiário + Letrado");
            Assert.That(_shop.Rows.Count(r => r.IsLocked), Is.EqualTo(2));
            Assert.That(_shop.Rows.Where(r => r.IsLocked).All(r => !r.CanBuy), Is.True, "linhas bloqueadas não compram (D-42)");

            var pickaxe = Row(UpgradeId.Pickaxe);
            Assert.That(pickaxe.CanBuy, Is.False, "sem lascas: apagado");
            Assert.That(pickaxe.ButtonText, Is.EqualTo(NumberFormatter.Format(simulation.Preview(UpgradeId.Pickaxe).NextCost.Value)));

            simulation.AddShards(1000);
            Assert.That(_shop.IsBadgeShown, Is.True, "com lascas, selo \"!\"");
            Assert.That(pickaxe.CanBuy, Is.True);

            double shards = simulation.State.Shards;
            double cost = simulation.Preview(UpgradeId.Pickaxe).NextCost.Value;
            pickaxe.GetComponentInChildren<Button>().onClick.Invoke(); // um toque
            Assert.That(simulation.State.LevelOf(UpgradeId.Pickaxe), Is.EqualTo(1), "um toque = uma compra");
            Assert.That(simulation.State.Shards, Is.EqualTo(shards - cost).Within(1e-9));
            Assert.That(pickaxe.LevelText, Does.Contain("1/"));

            Click("Screen/Panel/Back");
            Assert.That(_shop.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator SinglePurchase_ShowsBought_AfterBuying()
        {
            yield return LoadGame();
            var simulation = _runner.Simulation;
            Click("ShopButton");

            var dupla = Row(UpgradeId.DoublePickaxe);
            simulation.AddShards(simulation.Preview(UpgradeId.DoublePickaxe).NextCost.Value);
            dupla.GetComponentInChildren<Button>().onClick.Invoke();

            Assert.That(simulation.State.LevelOf(UpgradeId.DoublePickaxe), Is.EqualTo(1));
            Assert.That(dupla.CanBuy, Is.False);
            Assert.That(dupla.ButtonText, Is.Not.EqualTo(NumberFormatter.Format(0)), "mostra \"comprada\", não um custo");
        }

        /// <summary>P0 (REQ-UI-04): com a loja aberta o anão continua minerando.</summary>
        [UnityTest]
        public IEnumerator MiningKeepsRunning_WhileTheShopIsOpen()
        {
            yield return LoadGame();
            var simulation = _runner.Simulation;
            Click("ShopButton");
            _runner.TimeScale = 20f;

            double elapsed = simulation.Elapsed;
            long breaks = simulation.State.Breaks;
            yield return new WaitForSecondsRealtime(2f); // ~40 s de jogo

            Assert.That(_shop.IsOpen, Is.True);
            Assert.That(simulation.Elapsed, Is.GreaterThan(elapsed + 10), "o tempo de jogo andou com a loja aberta");
            Assert.That(simulation.State.Breaks, Is.GreaterThan(breaks), "quebrou cristais com a loja aberta");
        }
    }
}
