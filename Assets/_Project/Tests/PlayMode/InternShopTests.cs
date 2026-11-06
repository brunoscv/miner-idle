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
    /// E3 do M7 (D-170, D-174): o estagiário na LOJA da cena Game (sem save), no GDD v7. A linha Estagiário (500) se
    /// compra e, contratado, desce para o fim com "contratado"; a 3ª aba, ESTAGIÁRIO, aparece com ele e some sem ele;
    /// Picareta, Velocidade e Mineração dele com a prévia; Treinar e Promoção na aba das permanentes, a Promoção com o
    /// motivo até o Treinar no limite; as 7 linhas das permanentes cabem (40 px).
    /// </summary>
    public class InternShopTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        MiningRunner _runner;
        ShopView _shop;

        MiningSimulation Simulation => _runner.Simulation;

        IEnumerator LoadGame()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            _shop = Object.FindAnyObjectByType<ShopView>();
            Assert.That(_shop.InternRows.Count, Is.EqualTo(3), "as 3 melhorias dele (rode o setup do projeto)");
            if (!Simulation.Economy.HasIntern) Assert.Ignore("O balanço ativo não tem estagiário (rode o setup: GDD v7).");
            _runner.TimeScale = 0f; // sem mineração: as lascas são só as do teste
            _runner.UseRules(_runner.Rules);
            yield return null;
        }

        UpgradeRowView RunRow(UpgradeId id) => _shop.Rows.First(r => r.Upgrade == id);
        UpgradeRowView InternRow(UpgradeId id) => _shop.InternRows.First(r => r.Upgrade == id);
        PermanentRowView PermanentRow(PermanentId id) => _shop.PermanentRows.First(r => r.Permanent == id);

        static void Click(Component row) => row.transform.Find("Buy").GetComponent<Button>().onClick.Invoke();

        [UnityTest]
        public IEnumerator InternLine_Costs500_AndHiringSendsItToTheEnd_WithTheThirdTab()
        {
            yield return LoadGame();
            _shop.Open();
            var intern = RunRow(UpgradeId.Intern);
            Assert.That(intern.IsLocked, Is.False, "não é mais \"Em breve\"");
            Assert.That(intern.ButtonText, Is.EqualTo("500"));
            Assert.That(intern.LevelText, Is.Empty, "compra única: sem o nv 0/1");
            Assert.That(intern.CanBuy, Is.False, "sem lascas");
            Assert.That(_shop.IsInternTabShown, Is.False, "a aba dele só com ele contratado");

            Simulation.AddShards(600);
            Assert.That(intern.CanBuy, Is.True);
            Assert.That(_shop.IsBadgeShown, Is.True, "o \"!\" da LOJA acende com o Estagiário");

            Click(intern);
            Assert.That(Simulation.IsInternHired, Is.True, "um toque contrata");
            Assert.That(Simulation.State.Shards, Is.EqualTo(100).Within(1e-9), "600 − 500");
            Assert.That(intern.IsDone, Is.True);
            Assert.That(intern.ButtonText, Is.EqualTo("contratado"));
            Assert.That(intern.CanBuy, Is.False);
            Assert.That(intern.transform.GetSiblingIndex(), Is.EqualTo(intern.transform.parent.childCount - 1), "no fim da lista");
            Assert.That(_shop.IsInternTabShown, Is.True, "a 3ª aba aparece");
            Assert.That(_shop.IsInternTabBadgeShown, Is.True, "com 100 lascas, as dele (50) dão para comprar");
        }

        [UnityTest]
        public IEnumerator InternTab_ShowsHisThreeUpgrades_AndBuyingRaisesHisDamage()
        {
            yield return LoadGame();
            Simulation.HireIntern();
            Simulation.AddShards(120);
            _shop.Open();
            _shop.transform.Find("Screen/Panel/Tabs/InternTab").GetComponent<Button>().onClick.Invoke();
            Assert.That(_shop.ActiveTab, Is.EqualTo(ShopView.Tab.Intern));
            Assert.That(_shop.IsInternListShown, Is.True);
            Assert.That(_shop.IsRunListShown, Is.False);
            Assert.That(_shop.IsInternTabBadgeShown, Is.False, "a aba aberta não precisa do \"!\"");

            var pickaxe = InternRow(UpgradeId.InternPickaxe);
            Assert.That(pickaxe.LevelText, Is.EqualTo("nv 0/20"));
            Assert.That(pickaxe.EffectText, Is.EqualTo("Dano 1 » 5"));
            Assert.That(pickaxe.ButtonText, Is.EqualTo("50"));
            Assert.That(InternRow(UpgradeId.InternSpeed).EffectText, Is.EqualTo("Golpe a cada 2,00s » 1,90s"));
            Assert.That(InternRow(UpgradeId.InternMining).EffectText, Is.EqualTo("Offline +0% » +100%"));

            Click(pickaxe);
            Assert.That(Simulation.InternStats.Damage, Is.EqualTo(5), "na hora");
            Assert.That(pickaxe.LevelText, Is.EqualTo("nv 1/20"));
            Assert.That(Simulation.State.Shards, Is.EqualTo(70).Within(1e-9), "120 − 50");
        }

        [UnityTest]
        public IEnumerator FiringTheIntern_HidesHisTab_AndTheShopGoesBackToTheRun()
        {
            yield return LoadGame();
            Simulation.HireIntern();
            _shop.Open(ShopView.Tab.Intern);
            Assert.That(_shop.ActiveTab, Is.EqualTo(ShopView.Tab.Intern));

            Simulation.FireIntern();
            Assert.That(_shop.IsInternTabShown, Is.False);
            Assert.That(_shop.ActiveTab, Is.EqualTo(ShopView.Tab.Run));
            Assert.That(_shop.IsRunListShown, Is.True);
            var intern = RunRow(UpgradeId.Intern);
            Assert.That(intern.IsDone, Is.False, "a linha volta a ser compra");
            Assert.That(intern.ButtonText, Is.EqualTo("500"));
        }

        [UnityTest]
        public IEnumerator TrainAndPromotion_InThePermanentsTab_PromotionWaitsForTraining()
        {
            yield return LoadGame();
            if (!Simulation.Permanents.Rules.IsRestEnabled) Assert.Ignore("Sem permanentes no balanço ativo.");
            Simulation.AddRedShards(200);
            _shop.OpenPermanents();

            var train = PermanentRow(PermanentId.InternTraining);
            var promotion = PermanentRow(PermanentId.InternPromotion);
            Assert.That(train.TitleText, Is.EqualTo("Treinar"));
            Assert.That(train.EffectText, Is.EqualTo("Dano dele +0% » +10%"));
            Assert.That(train.LevelText, Is.EqualTo("nv 0/10"));
            Assert.That(train.CanBuy, Is.True);
            Assert.That(promotion.CanBuy, Is.False, "espera o Treinar no limite");
            Assert.That(promotion.EffectText, Is.EqualTo("Pede o Treinar no 10"));
            Assert.That(promotion.ButtonText, Is.EqualTo("50"));

            // As 7 cabem acima do VOLTAR (D-174): cada linha pede 40 px. (A altura que ela recebe depende da tela do
            // teste em batch, que é pequena; o que a E3 muda é o que a linha pede.)
            Assert.That(promotion.GetComponent<LayoutElement>().preferredHeight, Is.EqualTo(40f), "linhas de 40 px");
            Assert.That(_shop.PermanentRows.Count * (40f + 3f), Is.LessThanOrEqualTo(6 * (46f + 3f) + 8f),
                        "as 7 de 40 ocupam o lugar das 6 de 46 (mais a folga até o VOLTAR)");

            for (int i = 0; i < 10; i++) Simulation.GrantPermanentLevel(PermanentId.InternTraining);
            Assert.That(train.IsMaxed, Is.True);
            Assert.That(train.transform.GetSiblingIndex(), Is.EqualTo(train.transform.parent.childCount - 1), "no fim");
            Assert.That(promotion.EffectText, Is.EqualTo("Começa a run contratado"));
            Assert.That(promotion.CanBuy, Is.True, "com o Treinar no 10, acende");

            Click(promotion);
            Assert.That(Simulation.Permanents.State.LevelOf(PermanentId.InternPromotion), Is.EqualTo(1));
            Assert.That(promotion.IsMaxed, Is.True);
        }
    }
}
