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
    /// <summary>
    /// Marco 8b, E4 (D-155, D-154): a aba das permanentes na LOJA, na cena Game (sem save), no GDD v7. Comprar vale na
    /// hora, o botão segue o banco, a linha no limite desce para o fim, o "!" acende com as permanentes e o resumo do dia
    /// abre a aba.
    /// </summary>
    public class PermanentShopTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        MiningRunner _runner;
        ShopView _shop;

        MiningSimulation Simulation => _runner.Simulation;

        IEnumerator LoadGame()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            _runner = Object.FindAnyObjectByType<MiningRunner>();
            _shop = Object.FindAnyObjectByType<ShopView>();
            Assert.That(_shop, Is.Not.Null);
            Assert.That(_shop.PermanentRows.Count, Is.EqualTo(7), "as 5 do anão e as 2 do estagiário, M7 (rode o setup do projeto)");
            if (!Simulation.Permanents.Rules.IsRestEnabled)
                Assert.Ignore("O balanço ativo não tem permanentes (rode o setup do projeto: GDD v7).");
        }

        PermanentRowView Row(PermanentId id) => _shop.PermanentRows.First(r => r.Permanent == id);

        static Button BuyButton(PermanentRowView row) => row.transform.Find("Buy").GetComponent<Button>();

        [UnityTest]
        public IEnumerator Tabs_SwitchBetweenTheRunAndThePermanents()
        {
            yield return LoadGame();
            _shop.Open();
            Assert.That(_shop.ActiveTab, Is.EqualTo(ShopView.Tab.Run), "a LOJA abre nas melhorias da run");
            Assert.That(_shop.IsPermanentsTabShown, Is.True);
            Assert.That(_shop.IsRunListShown, Is.True);

            _shop.transform.Find("Screen/Panel/Tabs/PermanentsTab").GetComponent<Button>().onClick.Invoke();
            Assert.That(_shop.ActiveTab, Is.EqualTo(ShopView.Tab.Permanents));
            Assert.That(_shop.IsRunListShown, Is.False, "a lista da run some");
            Assert.That(Row(PermanentId.Memory).gameObject.activeInHierarchy, Is.True);
            Assert.That(Row(PermanentId.Memory).TitleText, Is.EqualTo("Memória"));
            Assert.That(_shop.RedBalanceText, Is.EqualTo("0"), "o banco no alto");

            _shop.transform.Find("Screen/Panel/Tabs/RunTab").GetComponent<Button>().onClick.Invoke();
            Assert.That(_shop.ActiveTab, Is.EqualTo(ShopView.Tab.Run));
            Assert.That(_shop.IsRunListShown, Is.True);
        }

        [UnityTest]
        public IEnumerator BuyingMountainBlood_TakesElevenFromTheBank_AndTheDamageRisesTenPercentRightAway()
        {
            yield return LoadGame();
            Simulation.AddRedShards(30);
            double damage = Simulation.Stats.Damage;
            _shop.OpenPermanents();

            var blood = Row(PermanentId.MountainBlood);
            Assert.That(blood.ButtonText, Is.EqualTo("11"));
            Assert.That(blood.LevelText, Is.EqualTo("nv 0/20"));
            Assert.That(blood.EffectText, Is.EqualTo("Dano +0% » +10%"));
            BuyButton(blood).onClick.Invoke();

            Assert.That(Simulation.Permanents.State.RedShards, Is.EqualTo(19), "30 − 11");
            Assert.That(_shop.RedBalanceText, Is.EqualTo("19"));
            Assert.That(Simulation.Stats.Damage, Is.EqualTo(damage * 1.1).Within(1e-9), "na hora (D-155)");
            Assert.That(blood.LevelText, Is.EqualTo("nv 1/20"));
            Assert.That(blood.ButtonText, Is.EqualTo("12"), "a 2ª compra: 10 + 2");
        }

        [UnityTest]
        public IEnumerator BuyButton_IsOffWithoutEnoughRedShards()
        {
            yield return LoadGame();
            Simulation.AddRedShards(10);
            _shop.OpenPermanents();
            Assert.That(Row(PermanentId.MountainBlood).CanBuy, Is.False, "10 não paga 11");

            BuyButton(Row(PermanentId.MountainBlood)).onClick.Invoke();
            Assert.That(Simulation.Permanents.State.RedShards, Is.EqualTo(10), "nada comprado");

            Simulation.AddRedShards(1);
            Assert.That(Row(PermanentId.MountainBlood).CanBuy, Is.True, "com 11, acende");
        }

        [UnityTest]
        public IEnumerator LineAtTheLimit_GoesToTheEnd_Disabled()
        {
            yield return LoadGame();
            Simulation.AddRedShards(2000);
            _shop.OpenPermanents();
            var memory = Row(PermanentId.Memory);
            Assert.That(memory.transform.GetSiblingIndex(), Is.EqualTo(0), "na ordem da tabela, a 1ª");

            for (int i = 0; i < 10; i++)
                Assert.That(Simulation.PurchasePermanent(PermanentId.Memory), Is.EqualTo(PurchaseResult.Purchased));

            Assert.That(Simulation.Permanents.State.RedShards, Is.EqualTo(2000 - 1123), "o total da Memória");
            Assert.That(memory.IsMaxed, Is.True);
            Assert.That(memory.CanBuy, Is.False);
            Assert.That(memory.ButtonText, Is.EqualTo("máx."));
            Assert.That(memory.LevelText, Is.EqualTo("nv 10/10"));
            Assert.That(memory.EffectText, Is.EqualTo("Mapa −100 cristais"));
            Assert.That(memory.transform.GetSiblingIndex(), Is.EqualTo(memory.transform.parent.childCount - 1), "no fim");
            Assert.That(Row(PermanentId.MountainBlood).transform.GetSiblingIndex(), Is.EqualTo(0), "as outras sobem");
            Assert.That(Row(PermanentId.DwarfSoul).transform.GetSiblingIndex(), Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator ShopBadge_LightsUpWithAPermanentToBuy_AndTheTabShowsIt()
        {
            yield return LoadGame();
            Assert.That(_shop.IsBadgeShown, Is.False, "no começo, nada para comprar");

            Simulation.AddRedShards(11);
            Assert.That(_shop.IsBadgeShown, Is.True, "o \"!\" da LOJA acende com uma permanente");
            _shop.Open();
            Assert.That(_shop.IsPermanentsTabBadgeShown, Is.True, "e o da aba das permanentes");
            Assert.That(_shop.IsRunTabBadgeShown, Is.False, "a da run está aberta");

            _shop.SelectTab(ShopView.Tab.Permanents);
            Assert.That(_shop.IsPermanentsTabBadgeShown, Is.False, "a aba aberta não precisa do \"!\"");
        }

        [UnityTest]
        public IEnumerator DaySummary_PermanentsOpensTheShopOnThePermanentsTab()
        {
            yield return LoadGame();
            var summary = Object.FindAnyObjectByType<DaySummaryScreen>();
            Simulation.SkipToCave(12);
            var modal = Object.FindAnyObjectByType<RestScreen>();
            modal.Open();
            modal.transform.Find("Screen/Panel/Confirm").GetComponent<Button>().onClick.Invoke();
            Assert.That(summary.IsOpen, Is.True);

            summary.transform.Find("Screen/Panel/Permanents").GetComponent<Button>().onClick.Invoke();
            Assert.That(summary.IsOpen, Is.False);
            Assert.That(_shop.IsOpen, Is.True);
            Assert.That(_shop.ActiveTab, Is.EqualTo(ShopView.Tab.Permanents));
            Assert.That(_shop.RedBalanceText, Is.EqualTo("7"), "o banco depois do descanso");
            Assert.That(Row(PermanentId.Memory).CanBuy, Is.False, "7 não paga 11");
        }
    }
}
