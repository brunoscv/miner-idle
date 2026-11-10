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
            _runner.TimeScale = 0f;         // no v7 dois cristais já pagam a Picareta: sem mineração, sem lascas
            _runner.UseRules(_runner.Rules); // partida nova, sem as lascas dos primeiros quadros
            var simulation = _runner.Simulation;
            yield return null;

            Assert.That(_shop.IsOpen, Is.False, "começa fechada");
            Assert.That(_shop.IsBadgeShown, Is.False, "sem lascas, sem selo");

            Click("ShopButton");
            Assert.That(_shop.IsOpen, Is.True);
            Assert.That(_shop.Rows.Count, Is.EqualTo(8), "6 melhorias (Crítico desde o v7) + Estagiário + Letrado");
            // O Letrado é de verdade desde o M8 e o Estagiário desde a E3 do M7 (D-174): nenhuma linha "Em breve".
            Assert.That(_shop.Rows.Count(r => r.IsLocked), Is.EqualTo(0), "nenhuma linha bloqueada");
            Assert.That(Row(UpgradeId.Intern).CanBuy, Is.False, "o Estagiário (500) sem lascas: apagado");

            var pickaxe = Row(UpgradeId.Pickaxe);
            Assert.That(pickaxe.CanBuy, Is.False, "sem lascas: apagado");
            Assert.That(pickaxe.LevelText, Is.EqualTo("nv 1/50"), "nível 1 = sem compra (D-207)");
            Assert.That(pickaxe.ButtonText, Is.EqualTo(NumberFormatter.FormatCost(simulation.Preview(UpgradeId.Pickaxe).NextCost.Value)));
            Assert.That(pickaxe.ButtonText, Is.EqualTo("4,2"), "2,1 × golpe 2, com a fração (D-202, D-203)");

            simulation.AddShards(1000);
            Assert.That(_shop.IsBadgeShown, Is.True, "com lascas, selo \"!\"");
            Assert.That(pickaxe.CanBuy, Is.True);

            double shards = simulation.State.Shards;
            double cost = simulation.Preview(UpgradeId.Pickaxe).NextCost.Value;
            pickaxe.GetComponentInChildren<Button>().onClick.Invoke(); // um toque
            Assert.That(simulation.State.LevelOf(UpgradeId.Pickaxe), Is.EqualTo(1), "um toque = uma compra");
            Assert.That(simulation.State.Shards, Is.EqualTo(shards - cost).Within(1e-9));
            Assert.That(pickaxe.LevelText, Is.EqualTo("nv 2/50"), "1 compra: nível 2 de 50 (D-207)");
            Assert.That(pickaxe.ButtonText, Is.EqualTo("4,62"), "2,1 × (1 + 1,2^1)");

            Click("Screen/Panel/Back");
            Assert.That(_shop.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator SinglePurchase_ShowsBought_AfterBuying()
        {
            yield return LoadGame();
            _runner.UseRules(TestBalance.GddV5); // a Picareta Dupla (compra única) saiu no v7 (D-136)
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
