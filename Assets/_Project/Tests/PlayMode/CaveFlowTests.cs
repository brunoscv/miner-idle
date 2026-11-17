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
    /// A passagem de andar na cena Game. Marco 8 tinha o Letrado/Mapa e a parede (D-107/D-109); o GDD v8 (M9c E1, D-256)
    /// trocou os dois pela barra do andar: cheia, o botão aparece (provisório até o Próximo Andar da E2), e tocar nele
    /// abre a transição (D-266), muda o andar e refaz a fila. E3b-2 (GDD v7, D-129): 100 andares, o bioma troca a cada 10
    /// e a faixa diz "Nível N · nome".
    /// </summary>
    public class CaveFlowTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        [UnityTest]
        public IEnumerator FillTheBar_AdvanceTheFloor_HudQueueAndBannerFollow()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var runner = Object.FindAnyObjectByType<MiningRunner>();
            var simulation = runner.Simulation;
            var hud = Object.FindAnyObjectByType<HudView>();
            var pass = Object.FindAnyObjectByType<CavePassButton>();
            var stage = Object.FindAnyObjectByType<DescentStage>();
            var banner = Object.FindAnyObjectByType<CaveBanner>();
            var panel = Object.FindAnyObjectByType<DebugPanel>();
            Assert.That(pass, Is.Not.Null, "a cena precisa do botão da passagem (rode o setup do projeto)");
            Assert.That(banner, Is.Not.Null, "a cena precisa da faixa do andar (rode o setup do projeto)");
            Assert.That(simulation.Economy.Get(UpgradeId.Letrado).IsEnabled, Is.False, "o Mapa saiu (D-256)");

            Assert.That(simulation.State.Cave, Is.EqualTo(1));
            Assert.That(hud.CaveText, Is.EqualTo("1"));
            Assert.That(simulation.CanAdvance, Is.False);
            Assert.That(pass.IsShown, Is.False, "barra vazia, sem botão");

            // DBG → Andar → Encher a barra.
            var fill = panel.transform.Find("CaveMenu/Content/Buttons/FillCaveBreaks");
            Assert.That(fill, Is.Not.Null, "o atalho Encher a barra no DBG → Andar (rode o setup do projeto)");
            fill.GetComponent<Button>().onClick.Invoke();
            Assert.That(simulation.FloorProgress, Is.EqualTo(1));
            Assert.That(pass.IsShown, Is.True, "barra cheia, o botão aparece");

            // A passagem: o andar muda na hora; com a transição, o palco segura a fila velha até o escuro.
            var oldQueue = simulation.Crystals.ToArray();
            var biome1 = stage.Biome;
            pass.GetComponentInChildren<Button>(includeInactive: true).onClick.Invoke();
            Assert.That(simulation.State.Cave, Is.EqualTo(2));
            Assert.That(hud.CaveText, Is.EqualTo("2"));
            Assert.That(simulation.State.CaveBreaks, Is.EqualTo(0), "a barra recomeça");
            Assert.That(simulation.Crystals.Intersect(oldQueue), Is.Empty, "a fila é a do andar novo");
            Assert.That(pass.IsShown, Is.False, "some ao passar");
            if (simulation.IsInFloorTransition)
                Assert.That(stage.IsHoldingOldQueue, Is.True, "os cristais novos só aparecem no escuro");

            for (float t = 0; t < 6f && simulation.IsInFloorTransition; t += Time.unscaledDeltaTime) yield return null;
            Assert.That(simulation.IsInFloorTransition, Is.False, "a transição acaba sozinha");
            yield return null;
            Assert.That(stage.IsHoldingOldQueue, Is.False, "a fila nova na tela");
            Assert.That(pass.IsShown, Is.False, "a barra do andar 2 está vazia");
            if (biome1 != null)
            {
                Assert.That(stage.Biome, Is.SameAs(biome1), "o andar 2 no bioma do 1");
                Assert.That(banner.Text, Does.StartWith("Nível 2").And.Contain(stage.Biome.DisplayName));
            }

            // DBG → Andar → Próximo andar (sem a barra).
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

            var button = panel.transform.Find("CaveMenu/Content/Buttons/TenLevels");
            Assert.That(button, Is.Not.Null, "o atalho +10 andares no DBG → Andar (rode o setup do projeto)");
            button.GetComponent<Button>().onClick.Invoke();
            Assert.That(simulation.State.Cave, Is.EqualTo(11));
            Assert.That(hud.CaveText, Is.EqualTo("11"));
            Assert.That(simulation.State.CaveBreaks, Is.EqualTo(0), "a barra do andar 11 começa vazia");
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

            panel.transform.Find("CaveMenu/Content/Buttons/TenLevels").GetComponent<Button>().onClick.Invoke();
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
            panel.transform.Find("CaveMenu/Content/Buttons/TenLevels").GetComponent<Button>().onClick.Invoke();
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
            panel.transform.Find("CaveMenu/Content/Buttons/TenLevels").GetComponent<Button>().onClick.Invoke();
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

        /// <summary>
        /// E3 (D-112) no v8 (D-258): DBG → Andar 100 (fim) enche a barra; o Próximo Andar do último andar leva à tela do
        /// coração da montanha, uma vez só.
        /// </summary>
        [UnityTest]
        public IEnumerator LastFloor_TheFullBar_ShowsTheHeartOnce()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var runner = Object.FindAnyObjectByType<MiningRunner>();
            var simulation = runner.Simulation;
            var hud = Object.FindAnyObjectByType<HudView>();
            var pass = Object.FindAnyObjectByType<CavePassButton>();
            var heart = Object.FindAnyObjectByType<HeartScreen>();
            var panel = Object.FindAnyObjectByType<DebugPanel>();
            Assert.That(heart, Is.Not.Null, "a cena precisa da tela do coração (rode o setup do projeto)");
            Assert.That(heart.IsOpen, Is.False);

            // DBG: o último andar, com a barra cheia.
            panel.transform.Find("CaveMenu/Content/Buttons/LastCave").GetComponent<Button>().onClick.Invoke();
            int last = simulation.Rules.CaveCount;
            Assert.That(simulation.State.Cave, Is.EqualTo(last));
            Assert.That(hud.CaveText, Is.EqualTo(last.ToString()));
            Assert.That(simulation.FloorProgress, Is.EqualTo(1), "o atalho enche a barra");
            // M9c E0: o andar novo abre a transição (o anão parado); o teste não espera por ela, pula o tempo dela.
            if (simulation.IsInFloorTransition) simulation.Tick(simulation.FloorTransitionLeft);
            Assert.That(simulation.IsInFloorTransition, Is.False);
            Assert.That(pass.IsShown, Is.True, "no último andar o botão leva ao coração");

            pass.GetComponentInChildren<Button>(includeInactive: true).onClick.Invoke();
            Assert.That(simulation.State.ReachedHeart, Is.True);
            Assert.That(simulation.State.Cave, Is.EqualTo(last), "o jogo segue no último andar");
            Assert.That(heart.IsPending, Is.True, "a tela vem logo depois");
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
