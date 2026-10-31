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
    /// Marco 8, E1: a passagem de caverna na cena Game. O Letrado mostra o progresso dos 100 cristais (D-109), o botão
    /// DESCER aparece com ele comprado (D-110), e passar troca a CAVERNA do HUD e o bioma do corredor.
    /// </summary>
    public class CaveFlowTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        [UnityTest]
        public IEnumerator BuyTheLetrado_PassTheCave_HudAndBiomeFollow()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var runner = Object.FindAnyObjectByType<MiningRunner>();
            var simulation = runner.Simulation;
            if (!simulation.Economy.Get(UpgradeId.Letrado).IsEnabled)
                Assert.Ignore("O balanço ativo não tem o Letrado (rode o setup do projeto).");

            var hud = Object.FindAnyObjectByType<HudView>();
            var shop = Object.FindAnyObjectByType<ShopView>();
            var pass = Object.FindAnyObjectByType<CavePassButton>();
            var stage = Object.FindAnyObjectByType<DescentStage>();
            var panel = Object.FindAnyObjectByType<DebugPanel>();
            Assert.That(pass, Is.Not.Null, "a cena precisa do botão DESCER (rode o setup do projeto)");

            Assert.That(simulation.State.Cave, Is.EqualTo(1));
            Assert.That(hud.CaveText, Is.EqualTo("1"));
            Assert.That(pass.IsShown, Is.False, "sem Letrado, sem botão");

            shop.Open(); // as linhas só se atualizam com a loja aberta
            var letrado = shop.Rows.First(r => !r.IsLocked && r.Upgrade == UpgradeId.Letrado);
            Assert.That(letrado.CanBuy, Is.False);
            Assert.That(letrado.EffectText, Does.Contain($"{simulation.State.CaveBreaks}/100"), "o progresso dos 100 cristais");

            // DBG: os 100 cristais e lascas para pagar.
            panel.transform.Find("Content/Buttons/FillCaveBreaks").GetComponent<Button>().onClick.Invoke();
            simulation.AddShards(1000);
            Assert.That(letrado.CanBuy, Is.True, "com os 100 cristais, o preço acende");

            letrado.GetComponentInChildren<Button>().onClick.Invoke();
            Assert.That(simulation.State.LevelOf(UpgradeId.Letrado), Is.EqualTo(1));
            Assert.That(pass.IsShown, Is.True, "com o Letrado, aparece DESCER");
            Assert.That(letrado.CanBuy, Is.False, "o 2º só na caverna 2");

            var biome1 = stage.Biome;
            pass.GetComponentInChildren<Button>(includeInactive: true).onClick.Invoke();
            yield return null;

            Assert.That(simulation.State.Cave, Is.EqualTo(2));
            Assert.That(hud.CaveText, Is.EqualTo("2"));
            Assert.That(pass.IsShown, Is.False, "some ao passar");
            if (biome1 != null)
            {
                Assert.That(stage.Biome, Is.Not.SameAs(biome1), "o bioma da caverna 2");
                Assert.That(stage.FloorTexture.name, Does.EndWith("02"));
            }
            Assert.That(letrado.EffectText, Does.Contain("0/100"), "a trava recomeça");

            // DBG: Próxima caverna (sem comprar).
            panel.transform.Find("Content/Buttons/NextCave").GetComponent<Button>().onClick.Invoke();
            Assert.That(simulation.State.Cave, Is.EqualTo(3));
            Assert.That(hud.CaveText, Is.EqualTo("3"));
        }
    }
}
