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
            Assert.That(_shop.PermanentRows.Count, Is.EqualTo(11),
                        "as 5 do anão, as 2 do estagiário (M7), o Dedo Fogoso e o Atalho (M9d), o Raro Dourado (M9e) e a Bomba (M9g; rode o setup do projeto)");
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
            Simulation.IsResting = true; // M9d (§4.2): as permanentes só se compram descansando
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
            Simulation.IsResting = true; // M9d (§4.2): as permanentes só se compram descansando
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
            // GDD v8 (D-239): a Memória vai ao nível 100, custo 10 + 2^(n−1); o total é 1.000 + 2^100 − 1 (os últimos níveis
            // são impagáveis no jogo; aqui o DBG dá as lascas).
            double total = 1000 + System.Math.Pow(2, 100) - 1;
            Simulation.IsResting = true; // M9d (§4.2): as permanentes só se compram descansando
            Simulation.AddRedShards(total * 2);
            _shop.OpenPermanents();
            var memory = Row(PermanentId.Memory);
            Assert.That(memory.transform.GetSiblingIndex(), Is.EqualTo(0), "na ordem da tabela, a 1ª");

            for (int i = 0; i < 100; i++)
                Assert.That(Simulation.PurchasePermanent(PermanentId.Memory), Is.EqualTo(PurchaseResult.Purchased), $"nível {i + 1}");

            Assert.That(Simulation.Permanents.State.RedShards, Is.EqualTo(total).Within(total * 1e-12), "o total da Memória");
            Assert.That(memory.IsMaxed, Is.True);
            Assert.That(memory.CanBuy, Is.False);
            Assert.That(memory.ButtonText, Is.EqualTo("máx."));
            Assert.That(memory.LevelText, Is.EqualTo("nv 100/100"));
            Assert.That(memory.EffectText, Is.EqualTo("Andar −500 cristais"), "−5 por nível (D-239)");
            Assert.That(memory.transform.GetSiblingIndex(), Is.EqualTo(memory.transform.parent.childCount - 1), "no fim");
            Assert.That(Row(PermanentId.MountainBlood).transform.GetSiblingIndex(), Is.EqualTo(0), "as outras sobem");
            Assert.That(Row(PermanentId.DwarfSoul).transform.GetSiblingIndex(), Is.EqualTo(4), "depois do Dedo Fogoso (M9d)");
        }

        [UnityTest]
        public IEnumerator ShopBadge_LightsUpWithAPermanentToBuy_OnlyWhileResting()
        {
            yield return LoadGame();
            Assert.That(_shop.IsBadgeShown, Is.False, "no começo, nada para comprar");

            Simulation.AddRedShards(11);
            Assert.That(_shop.IsBadgeShown, Is.False, "D-276: no meio da run as permanentes são só consulta, sem \"!\"");

            Simulation.IsResting = true;
            Simulation.AddRedShards(1);
            Assert.That(_shop.IsBadgeShown, Is.True, "descansando, o \"!\" da LOJA acende com uma permanente");
            _shop.Open();
            Assert.That(_shop.IsPermanentsTabBadgeShown, Is.True, "e o da aba das permanentes");
            Assert.That(_shop.IsRunTabBadgeShown, Is.False, "a da run está aberta");

            _shop.SelectTab(ShopView.Tab.Permanents);
            Assert.That(_shop.IsPermanentsTabBadgeShown, Is.False, "a aba aberta não precisa do \"!\"");
        }

        /// <summary>M9d E2 (D-265): na tela do descanso, as permanentes se compram, o banco desce e INICIAR começa a run.</summary>
        [UnityTest]
        public IEnumerator RestScreen_BuysPermanents_AndStartBeginsTheRun()
        {
            yield return LoadGame();
            var rest = Object.FindAnyObjectByType<RestShopScreen>();
            Simulation.SkipToCave(12);
            var modal = Object.FindAnyObjectByType<RestScreen>();
            modal.Open();
            modal.transform.Find("Screen/Panel/Confirm").GetComponent<Button>().onClick.Invoke();
            rest.SkipZzz();
            Assert.That(rest.IsPanelShown, Is.True);
            Assert.That(rest.BankText, Is.EqualTo("7"), "o banco depois do descanso");
            Assert.That(rest.Rows.Count, Is.EqualTo(11), "as 11 permanentes, com o Raro Dourado (M9e) e a Bomba (M9g)");

            PermanentRowView RestRow(PermanentId id) => rest.Rows.First(r => r.Permanent == id);
            var memory = RestRow(PermanentId.Memory);
            Assert.That(memory.CanBuy, Is.False, "7 não paga 11");
            Assert.That(memory.IsBuyShown, Is.True, "com preço e botão");
            Simulation.AddRedShards(4);
            Assert.That(memory.CanBuy, Is.True, "11 paga 11");
            BuyButton(memory).onClick.Invoke();
            Assert.That(rest.BankText, Is.EqualTo("0"), "7 + 4 − 11");
            Assert.That(memory.LevelText, Is.EqualTo("nv 1/100"));

            rest.transform.Find("Screen/Panel/Start").GetComponent<Button>().onClick.Invoke();
            Assert.That(rest.IsOpen, Is.False);
            Assert.That(_runner.IsResting, Is.False);
            Assert.That(Simulation.Permanents.State.LevelOf(PermanentId.Memory), Is.EqualTo(1), "a compra vale na run nova");
        }

        /// <summary>
        /// M9d (§4.2, D-265, D-276): fora do descanso, as permanentes aparecem na LOJA só para consulta: o nível e o efeito
        /// de agora, sem preço nem botão, e "Compre ao Descansar" no alto.
        /// </summary>
        [UnityTest]
        public IEnumerator OutsideTheRest_ThePermanentsShow_ButDontBuy()
        {
            yield return LoadGame();
            Simulation.AddRedShards(100);
            _shop.OpenPermanents();
            var blood = Row(PermanentId.MountainBlood);
            Assert.That(blood.CanBuy, Is.False, "só no descanso");
            Assert.That(blood.IsConsult, Is.True);
            Assert.That(blood.IsBuyShown, Is.False, "sem preço nem botão (D-276)");
            Assert.That(blood.EffectText, Is.EqualTo("Dano +0%"), "o efeito de agora");
            Assert.That(_shop.IsConsultNoteShown, Is.True, "\"Compre ao Descansar\" no alto");
            BuyButton(blood).onClick.Invoke();
            Assert.That(Simulation.Permanents.State.RedShards, Is.EqualTo(100), "nada comprado");
            _shop.SelectTab(ShopView.Tab.Run);
            Assert.That(_shop.IsConsultNoteShown, Is.False, "o aviso é só da aba das permanentes");
            Assert.That(Row(PermanentId.FieryFinger), Is.Not.Null, "o Dedo Fogoso na lista");
            Assert.That(Row(PermanentId.Shortcut), Is.Not.Null, "o Atalho na lista");
        }
    }
}
