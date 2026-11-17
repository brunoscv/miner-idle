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
    /// troca no escuro, a faixa "Caverna 2 · nome" aparece (D-111) e a CAVERNA do HUD muda. E3b-2 (GDD v7, D-129): 100
    /// níveis, o bioma troca a cada 10 (só aí escurece) e a faixa diz "Nível 2 · nome" em todo nível.
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
            panel.transform.Find("ShopMenu/Content/Buttons/FillCaveBreaks").GetComponent<Button>().onClick.Invoke();
            simulation.AddShards(1000);
            Assert.That(letrado.CanBuy, Is.True, "com os 100 cristais, o preço acende");
            if (simulation.Preview(UpgradeId.Letrado).NextCost == 0)
                Assert.That(letrado.ButtonText, Is.EqualTo("COMPRAR"), "o Mapa de graça (GDD v7) não mostra 0 (D-175)");

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

            // A quebra: o nível muda na hora. No v7 o nível 2 fica no bioma do 1 (D-129): não escurece, a faixa vem igual.
            bool newBiome = simulation.Rules.BiomeOf(2) != simulation.Rules.BiomeOf(1);
            simulation.BreakTarget();
            Assert.That(simulation.Wall, Is.Null);
            Assert.That(simulation.State.Cave, Is.EqualTo(2));
            Assert.That(hud.CaveText, Is.EqualTo("2"));
            Assert.That(stage.IsChangingCave, Is.EqualTo(newBiome), "só escurece na troca de bioma");
            yield return null;
            Assert.That(stage.IsWallShown, Is.False);

            float darkest = 0f;
            for (float t = 0; t < DescentStage.CaveFadeSeconds * 0.5f; t += Time.unscaledDeltaTime)
            {
                darkest = Mathf.Max(darkest, stage.Darkness);
                if (biome1 != null && !newBiome) Assert.That(banner.IsShown, Is.True, "a faixa do nível novo");
                yield return null;
            }
            if (newBiome) Assert.That(darkest, Is.GreaterThan(0.3f), "escureceu");
            else Assert.That(darkest, Is.EqualTo(0f), "mesmo bioma: não escurece");
            for (float t = 0; t < DescentStage.CaveFadeSeconds * 0.5f + 0.3f; t += Time.unscaledDeltaTime) yield return null;
            Assert.That(stage.IsChangingCave, Is.False);
            Assert.That(stage.Darkness, Is.EqualTo(0f), "clareou de novo");
            if (biome1 != null)
            {
                if (newBiome) Assert.That(stage.Biome, Is.Not.SameAs(biome1), "o bioma da caverna 2");
                else Assert.That(stage.Biome, Is.SameAs(biome1), "o nível 2 no bioma do 1");
                Assert.That(banner.IsShown, Is.True, "a faixa do nível novo");
                Assert.That(banner.Text, Does.StartWith("Nível 2").And.Contain(stage.Biome.DisplayName));
            }

            shop.Open();
            Assert.That(letrado.EffectText, Does.Contain("0/100"), "a trava recomeça");
            shop.Close();

            // DBG: Próxima caverna (sem comprar e sem parede).
            panel.transform.Find("CaveMenu/Content/Buttons/NextCave").GetComponent<Button>().onClick.Invoke();
            Assert.That(simulation.State.Cave, Is.EqualTo(3));
            Assert.That(hud.CaveText, Is.EqualTo("3"));
        }

        /// <summary>E3b-2 (D-129): DBG → +10 níveis. Do nível 1 ao 11 o bioma troca, escurecido, e a faixa traz o nome novo.</summary>
        [UnityTest]
        public IEnumerator TenLevels_ChangeTheBiome_InTheDark()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var simulation = Object.FindAnyObjectByType<MiningRunner>().Simulation;
            var stage = Object.FindAnyObjectByType<DescentStage>();
            var banner = Object.FindAnyObjectByType<CaveBanner>();
            var hud = Object.FindAnyObjectByType<HudView>();
            var panel = Object.FindAnyObjectByType<DebugPanel>();
            if (simulation.Rules.CaveCount < 11 || simulation.Rules.LevelsPerBiome != 10)
                Assert.Ignore("O balanço ativo não tem os 100 níveis do GDD v7 (rode o setup do projeto).");
            if (stage.Biome == null) Assert.Ignore("Sem os biomas na cena (rode o setup).");
            var biome1 = stage.Biome;

            var button = panel.transform.Find("ShopMenu/Content/Buttons/TenLevels");
            Assert.That(button, Is.Not.Null, "o atalho +10 níveis no DBG (rode o setup do projeto)");
            button.GetComponent<Button>().onClick.Invoke();
            Assert.That(simulation.State.Cave, Is.EqualTo(11));
            Assert.That(hud.CaveText, Is.EqualTo("11"));
            Assert.That(simulation.State.LevelOf(UpgradeId.Letrado), Is.EqualTo(10), "os Mapas de antes, de graça");
            // M9c E0 (D-266): o andar novo abre a transição; quem escurece é ela, e o bioma troca no escuro, lá dentro.
            Assert.That(simulation.IsInFloorTransition, Is.True, "bioma novo: a transição de andar");
            Assert.That(stage.Biome, Is.SameAs(biome1), "o bioma só troca no escuro");

            for (float t = 0; t < 6f && simulation.IsInFloorTransition; t += Time.unscaledDeltaTime) yield return null;
            Assert.That(simulation.IsInFloorTransition, Is.False, "a transição acaba sozinha");
            yield return null;
            Assert.That(stage.Biome, Is.Not.SameAs(biome1), "o 2º bioma");
            Assert.That(stage.FloorTexture.name, Does.EndWith("02"));
            Assert.That(banner.Text, Does.StartWith("Nível 11").And.Contain(stage.Biome.DisplayName));
        }

        /// <summary>
        /// M9c E0 (D-267): DBG → Caverna → "Transição: B" até "nenhuma"; aí a troca de bioma volta a ser a de antes (a
        /// descida escurece sozinha, o jogo não para).
        /// </summary>
        [UnityTest]
        public IEnumerator TenLevels_WithoutTransition_FadeLikeBefore()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var simulation = Object.FindAnyObjectByType<MiningRunner>().Simulation;
            var stage = Object.FindAnyObjectByType<DescentStage>();
            var transition = Object.FindAnyObjectByType<FloorTransition>();
            var panel = Object.FindAnyObjectByType<DebugPanel>();
            if (simulation.Rules.CaveCount < 11 || simulation.Rules.LevelsPerBiome != 10)
                Assert.Ignore("O balanço ativo não tem os 100 níveis do GDD v7 (rode o setup do projeto).");
            if (stage.Biome == null) Assert.Ignore("Sem os biomas na cena (rode o setup).");
            Assert.That(transition, Is.Not.Null, "a cena precisa da transição de andar (rode o setup do projeto)");
            var toggle = panel.transform.Find("CaveMenu/Content/Buttons/FloorTransition");
            Assert.That(toggle, Is.Not.Null, "o botão da transição no DBG → Caverna (rode o setup do projeto)");
            var biome1 = stage.Biome;

            Assert.That(transition.Style, Is.EqualTo(FloorTransitionStyle.CameraDrop), "a B é a da cena (D-268)");
            var label = toggle.GetComponentInChildren<TMPro.TMP_Text>();
            foreach (var expected in new[] { "B mosaico", "A", "nenhuma" })
            {
                toggle.GetComponent<Button>().onClick.Invoke();
                Assert.That(label.text, Is.EqualTo("Transição: " + expected));
            }
            Assert.That(transition.Style, Is.EqualTo(FloorTransitionStyle.None));

            panel.transform.Find("ShopMenu/Content/Buttons/TenLevels").GetComponent<Button>().onClick.Invoke();
            Assert.That(simulation.State.Cave, Is.EqualTo(11));
            Assert.That(simulation.IsInFloorTransition, Is.False, "sem transição o jogo não para");
            Assert.That(stage.IsChangingCave, Is.True, "bioma novo: escurece, como antes");

            for (float t = 0; t < DescentStage.CaveFadeSeconds + 0.3f; t += Time.unscaledDeltaTime) yield return null;
            Assert.That(stage.Biome, Is.Not.SameAs(biome1), "o 2º bioma");
        }

        /// <summary>
        /// M9c E0 (P10, B): a câmera desce. Na saída ela sobe e a descida desfoca; no fim, a câmera está no lugar de jogo,
        /// sem escuro nem desfoque, e o bioma é o novo.
        /// </summary>
        [UnityTest]
        public IEnumerator CameraDrop_RisesBlursAndComesBackInFocus()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var simulation = Object.FindAnyObjectByType<MiningRunner>().Simulation;
            var stage = Object.FindAnyObjectByType<DescentStage>();
            var transition = Object.FindAnyObjectByType<FloorTransition>();
            var panel = Object.FindAnyObjectByType<DebugPanel>();
            if (simulation.Rules.CaveCount < 11 || simulation.Rules.LevelsPerBiome != 10)
                Assert.Ignore("O balanço ativo não tem os 100 níveis do GDD v7 (rode o setup do projeto).");
            if (stage.Biome == null) Assert.Ignore("Sem os biomas na cena (rode o setup).");
            Assert.That(transition, Is.Not.Null, "a cena precisa da transição de andar (rode o setup do projeto)");
            var biome1 = stage.Biome;
            var camera = stage.SceneCamera;
            var home = camera.transform.localPosition;

            transition.Style = FloorTransitionStyle.CameraDrop;
            panel.transform.Find("ShopMenu/Content/Buttons/TenLevels").GetComponent<Button>().onClick.Invoke();
            Assert.That(simulation.IsInFloorTransition, Is.True);
            Assert.That(simulation.FloorTransitionLength, Is.EqualTo(transition.DurationOf(FloorTransitionStyle.CameraDrop)).Within(1e-6));

            bool blurred = false, rose = false;
            for (float t = 0; t < 6f && simulation.IsInFloorTransition; t += Time.unscaledDeltaTime)
            {
                yield return null;
                blurred |= stage.IsBlurred;
                rose |= camera.transform.localPosition.y > home.y + 0.5f;
            }
            Assert.That(simulation.IsInFloorTransition, Is.False, "a transição acaba sozinha");
            Assert.That(blurred, Is.True, "a descida desfocou");
            Assert.That(rose, Is.True, "a câmera saiu do lugar, para cima");

            yield return null;
            Assert.That(stage.IsBlurred, Is.False, "de volta ao foco");
            Assert.That(stage.Darkness, Is.EqualTo(0f), "sem escuro");
            Assert.That(Vector3.Distance(camera.transform.localPosition, home), Is.LessThan(1e-3f), "a câmera de jogo");
            Assert.That(stage.Biome, Is.Not.SameAs(biome1), "o bioma novo");
        }

        /// <summary>
        /// M9c E0 (P10, A): desfoque e cartão. A câmera não sai do lugar; a descida desfoca e escurece sem chegar ao
        /// preto; no fim, foco e luz de volta e o bioma novo.
        /// </summary>
        [UnityTest]
        public IEnumerator BlurCard_BlursInPlace_AndComesBackInFocus()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var simulation = Object.FindAnyObjectByType<MiningRunner>().Simulation;
            var stage = Object.FindAnyObjectByType<DescentStage>();
            var transition = Object.FindAnyObjectByType<FloorTransition>();
            var panel = Object.FindAnyObjectByType<DebugPanel>();
            if (simulation.Rules.CaveCount < 11 || simulation.Rules.LevelsPerBiome != 10)
                Assert.Ignore("O balanço ativo não tem os 100 níveis do GDD v7 (rode o setup do projeto).");
            if (stage.Biome == null) Assert.Ignore("Sem os biomas na cena (rode o setup).");
            Assert.That(transition, Is.Not.Null, "a cena precisa da transição de andar (rode o setup do projeto)");
            var biome1 = stage.Biome;
            var camera = stage.SceneCamera;
            var home = camera.transform.localPosition;

            transition.Style = FloorTransitionStyle.BlurCard;
            panel.transform.Find("ShopMenu/Content/Buttons/TenLevels").GetComponent<Button>().onClick.Invoke();
            Assert.That(simulation.IsInFloorTransition, Is.True);
            Assert.That(simulation.FloorTransitionLength, Is.EqualTo(transition.DurationOf(FloorTransitionStyle.BlurCard)).Within(1e-6));

            bool blurred = false, moved = false;
            float darkest = 0f;
            for (float t = 0; t < 6f && simulation.IsInFloorTransition; t += Time.unscaledDeltaTime)
            {
                yield return null;
                blurred |= stage.IsBlurred;
                moved |= Vector3.Distance(camera.transform.localPosition, home) > 1e-3f;
                darkest = Mathf.Max(darkest, stage.Darkness);
            }
            Assert.That(simulation.IsInFloorTransition, Is.False, "a transição acaba sozinha");
            Assert.That(blurred, Is.True, "a descida desfocou");
            Assert.That(moved, Is.False, "a câmera fica no lugar");
            Assert.That(darkest, Is.GreaterThan(0.3f).And.LessThan(1f), "escurece, sem chegar ao preto");

            yield return null;
            Assert.That(stage.IsBlurred, Is.False, "de volta ao foco");
            Assert.That(stage.Darkness, Is.EqualTo(0f), "sem escuro");
            Assert.That(stage.Biome, Is.Not.SameAs(biome1), "o bioma novo");
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
            panel.transform.Find("ShopMenu/Content/Buttons/LastCave").GetComponent<Button>().onClick.Invoke();
            int last = simulation.Rules.CaveCount;
            Assert.That(simulation.State.Cave, Is.EqualTo(last));
            Assert.That(hud.CaveText, Is.EqualTo(last.ToString()));
            // M9c E0: o andar novo abre a transição (o anão parado); o teste não espera por ela, pula o tempo dela.
            if (simulation.IsInFloorTransition) simulation.Tick(simulation.FloorTransitionLeft);
            Assert.That(simulation.IsInFloorTransition, Is.False);

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
