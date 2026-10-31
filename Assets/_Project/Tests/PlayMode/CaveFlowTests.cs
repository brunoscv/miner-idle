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
    /// Marco 8: a passagem de caverna na cena Game. O Letrado mostra o progresso dos 100 cristais (D-109), o botão DESCER
    /// aparece com ele comprado (D-110). E2: DESCER põe a parede no corredor (D-107); quebrada, a descida escurece, o bioma
    /// troca no escuro, a faixa "Caverna 2 · nome" aparece (D-111) e a CAVERNA do HUD muda.
    /// </summary>
    public class CaveFlowTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        [UnityTest]
        public IEnumerator BuyTheLetrado_BreakTheWall_HudBiomeAndBannerFollow()
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
            var banner = Object.FindAnyObjectByType<CaveBanner>();
            var panel = Object.FindAnyObjectByType<DebugPanel>();
            Assert.That(pass, Is.Not.Null, "a cena precisa do botão DESCER (rode o setup do projeto)");
            Assert.That(banner, Is.Not.Null, "a cena precisa da faixa da caverna (rode o setup do projeto)");

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
            shop.Close();

            // DESCER: a parede no fim da fila; a caverna ainda é a 1.
            var biome1 = stage.Biome;
            pass.GetComponentInChildren<Button>(includeInactive: true).onClick.Invoke();
            Assert.That(simulation.Wall, Is.Not.Null, "a parede na fila");
            Assert.That(pass.IsShown, Is.False, "some ao passar");
            Assert.That(simulation.State.Cave, Is.EqualTo(1));

            // Os cristais antes dela pelo debug; o anão corre até ela (×20) e ela aparece no corredor.
            for (int i = 0; i < 20 && simulation.Miner.Target != simulation.Wall; i++)
            {
                simulation.Tick(0);
                if (simulation.Miner.Target != simulation.Wall) simulation.BreakTarget();
            }
            Assert.That(simulation.Miner.Target, Is.SameAs(simulation.Wall), "a parede é o alvo");
            runner.TimeScale = 20f;
            for (float t = 0; t < 3f && simulation.Miner.State != MinerState.Mining; t += Time.unscaledDeltaTime) yield return null;
            runner.TimeScale = 0f;
            yield return null;
            Assume.That(simulation.Wall, Is.Not.Null, "a parede ainda de pé");
            Assert.That(stage.IsWallShown, Is.True, "a parede no corredor");

            // A quebra: a caverna muda na hora; a descida escurece e o bioma troca no escuro.
            simulation.BreakTarget();
            Assert.That(simulation.Wall, Is.Null);
            Assert.That(simulation.State.Cave, Is.EqualTo(2));
            Assert.That(hud.CaveText, Is.EqualTo("2"));
            Assert.That(stage.IsChangingCave, Is.True, "a troca escurecida começou");
            yield return null;
            Assert.That(stage.IsWallShown, Is.False);

            float darkest = 0f;
            for (float t = 0; t < DescentStage.CaveFadeSeconds + 0.3f; t += Time.unscaledDeltaTime)
            {
                darkest = Mathf.Max(darkest, stage.Darkness);
                yield return null;
            }
            Assert.That(darkest, Is.GreaterThan(0.5f), "escureceu");
            Assert.That(stage.IsChangingCave, Is.False);
            Assert.That(stage.Darkness, Is.EqualTo(0f), "clareou de novo");
            if (biome1 != null)
            {
                Assert.That(stage.Biome, Is.Not.SameAs(biome1), "o bioma da caverna 2");
                Assert.That(stage.FloorTexture.name, Does.EndWith("02"));
                Assert.That(banner.IsShown, Is.True, "a faixa da caverna nova");
                Assert.That(banner.Text, Does.StartWith("Caverna 2").And.Contain(stage.Biome.DisplayName));
            }

            shop.Open();
            Assert.That(letrado.EffectText, Does.Contain("0/100"), "a trava recomeça");
            shop.Close();

            // DBG: Próxima caverna (sem comprar e sem parede).
            panel.transform.Find("Content/Buttons/NextCave").GetComponent<Button>().onClick.Invoke();
            Assert.That(simulation.State.Cave, Is.EqualTo(3));
            Assert.That(hud.CaveText, Is.EqualTo("3"));
        }

        /// <summary>E3 (D-112): DBG → Caverna 10, o Letrado 10, a última parede e a tela do coração da montanha, uma vez só.</summary>
        [UnityTest]
        public IEnumerator LastCave_TheLastWall_ShowsTheHeartOnce()
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
            var heart = Object.FindAnyObjectByType<HeartScreen>();
            var panel = Object.FindAnyObjectByType<DebugPanel>();
            Assert.That(heart, Is.Not.Null, "a cena precisa da tela do coração (rode o setup do projeto)");
            Assert.That(heart.IsOpen, Is.False);

            // DBG: a última caverna, com a trava cumprida e as lascas do último Letrado.
            panel.transform.Find("Content/Buttons/LastCave").GetComponent<Button>().onClick.Invoke();
            int last = simulation.Rules.CaveCount;
            Assert.That(simulation.State.Cave, Is.EqualTo(last));
            Assert.That(hud.CaveText, Is.EqualTo(last.ToString()));

            shop.Open();
            var letrado = shop.Rows.First(r => !r.IsLocked && r.Upgrade == UpgradeId.Letrado);
            Assert.That(letrado.CanBuy, Is.True, "o Letrado 10 à venda");
            letrado.GetComponentInChildren<Button>().onClick.Invoke();
            shop.Close();
            Assert.That(pass.IsShown, Is.True, "DESCER para a última parede");

            pass.GetComponentInChildren<Button>(includeInactive: true).onClick.Invoke();
            Assert.That(simulation.Wall, Is.Not.Null);
            for (int i = 0; i < 20 && simulation.Miner.Target != simulation.Wall; i++)
            {
                simulation.Tick(0);
                if (simulation.Miner.Target != simulation.Wall) simulation.BreakTarget();
            }
            Assert.That(simulation.Miner.Target, Is.SameAs(simulation.Wall));

            simulation.BreakTarget();
            Assert.That(simulation.State.ReachedHeart, Is.True);
            Assert.That(simulation.State.Cave, Is.EqualTo(last), "o jogo segue na última caverna");
            Assert.That(heart.IsPending, Is.True, "a tela vem depois dos estilhaços");
            Assert.That(heart.IsOpen, Is.False);

            for (float t = 0; t < 3f && !heart.IsOpen; t += Time.unscaledDeltaTime) yield return null;
            Assert.That(heart.IsOpen, Is.True, "o coração da montanha");
            Assert.That(heart.BodyText, Does.Contain(NumberFormatter.FormatShards(simulation.State.ShardsEarned)));
            Assert.That(heart.BodyText, Does.Contain(" s").Or.Contain(" min"), "o tempo de jogo");

            heart.GetComponentInChildren<Button>().onClick.Invoke();
            Assert.That(heart.IsOpen, Is.False, "CONTINUAR fecha");
            Assert.That(pass.IsShown, Is.False, "não há mais passagem");

            runner.TimeScale = 20f;
            yield return new WaitForSecondsRealtime(0.5f);
            runner.TimeScale = 1f;
            Assert.That(heart.IsOpen || heart.IsPending, Is.False, "o fim aparece uma vez só");
        }
    }
}
