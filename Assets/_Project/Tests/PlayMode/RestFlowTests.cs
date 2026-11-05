using System.Collections;
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
    /// Marco 8b, E3 (D-152…D-154, D-157): a barra de baixo com Descansar e PASSAR, o modal de Descansar e o resumo do dia,
    /// na cena Game (sem save), no GDD v7 (descanso a partir do nível 10).
    /// </summary>
    public class RestFlowTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        MiningRunner _runner;
        RestButton _rest;
        CavePassButton _pass;
        RestScreen _modal;
        DaySummaryScreen _summary;

        MiningSimulation Simulation => _runner.Simulation;

        IEnumerator LoadGame()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null; // um quadro de layout para a UI ter posição

            _runner = Object.FindAnyObjectByType<MiningRunner>();
            _rest = Object.FindAnyObjectByType<RestButton>();
            _pass = Object.FindAnyObjectByType<CavePassButton>();
            _modal = Object.FindAnyObjectByType<RestScreen>();
            _summary = Object.FindAnyObjectByType<DaySummaryScreen>();
            Assert.That(_rest, Is.Not.Null, "a cena precisa do botão Descansar (rode o setup do projeto)");
            Assert.That(_modal, Is.Not.Null, "a cena precisa do modal de Descansar (rode o setup do projeto)");
            Assert.That(_summary, Is.Not.Null, "a cena precisa do resumo do dia (rode o setup do projeto)");
            if (!Simulation.Permanents.Rules.IsRestEnabled)
                Assert.Ignore("O balanço ativo não tem descanso (rode o setup do projeto: GDD v7).");
        }

        void BuyTheMap()
        {
            Simulation.FillCaveBreaks();
            Simulation.AddShards(1e12);
            Assert.That(Simulation.Purchase(UpgradeId.Letrado), Is.EqualTo(PurchaseResult.Purchased));
        }

        static Button ButtonAt(Component root, string path) => root.transform.Find(path).GetComponent<Button>();

        static Vector2 ScreenCenterOf(RectTransform rect) =>
            RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));

        [UnityTest]
        public IEnumerator RestButton_ShowsFromTheRestLevel_WithTheRedShardsItTakes()
        {
            yield return LoadGame();
            int restLevel = Simulation.Permanents.Rules.RestLevel;
            Assert.That(restLevel, Is.EqualTo(10), "GDD v7");

            Simulation.SkipToCave(9);
            Assert.That(_rest.IsShown, Is.False, "no nível 9 ainda não");

            Simulation.SkipToCave(10);
            Assert.That(_rest.IsShown, Is.True, "no 10 aparece");
            Assert.That(_rest.CountText, Is.EqualTo("5"));

            Simulation.SkipToCave(11);
            Assert.That(_rest.CountText, Is.EqualTo("6"));
        }

        [UnityTest]
        public IEnumerator PassButton_OnlyWithTheMap_AndEachButtonOnItsSide()
        {
            yield return LoadGame();
            Simulation.SkipToCave(12);
            Assert.That(_pass.IsShown, Is.False, "sem o Mapa, sem PASSAR");
            Assert.That(_rest.IsShown, Is.True);

            BuyTheMap();
            Assert.That(_pass.IsShown, Is.True, "com o Mapa, o PASSAR");
            yield return null;

            var restRect = (RectTransform)_rest.transform.Find("RestButton");
            var passRect = (RectTransform)_pass.transform.Find("PassCaveButton");
            Assert.That(ScreenCenterOf(restRect).x, Is.LessThan(Screen.width * 0.5f), "Descansar à esquerda");
            Assert.That(ScreenCenterOf(passRect).x, Is.GreaterThan(Screen.width * 0.5f), "PASSAR à direita");
            Assert.That(restRect.rect.size, Is.EqualTo(passRect.rect.size), "do mesmo tamanho");
        }

        [UnityTest]
        public IEnumerator Modal_ShowsTheDay_AndContinueChangesNothing()
        {
            yield return LoadGame();
            Simulation.SkipToCave(12);
            Simulation.AddShards(1240);

            ButtonAt(_rest, "RestButton").onClick.Invoke();
            Assert.That(_modal.IsOpen, Is.True, "o toque no Descansar abre o modal");
            Assert.That(_modal.DayText, Is.EqualTo("DIA 1"));
            Assert.That(_modal.LevelText, Is.EqualTo("12"));
            Assert.That(_modal.ShardsText, Is.EqualTo(NumberFormatter.FormatShards(Simulation.State.ShardsEarned)));
            Assert.That(_modal.TimeText, Is.EqualTo(NumberFormatter.FormatDuration(Simulation.State.PlaySeconds)));
            Assert.That(_modal.TakeText, Is.EqualTo("7"));
            Assert.That(_modal.NextText, Does.Contain("+1"), "o nível 13 soma 1");

            var before = Simulation;
            ButtonAt(_modal, "Screen/Panel/Continue").onClick.Invoke();
            Assert.That(_modal.IsOpen, Is.False);
            Assert.That(_runner.Simulation, Is.SameAs(before), "a mesma run");
            Assert.That(Simulation.State.Cave, Is.EqualTo(12));
            Assert.That(Simulation.Permanents.State.RedShards, Is.EqualTo(0));
            Assert.That(Simulation.Permanents.State.Day, Is.EqualTo(1));
            Assert.That(_summary.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator Confirm_StartsTheNewRun_AndShowsTheDaySummary()
        {
            yield return LoadGame();
            Simulation.SkipToCave(12);
            ButtonAt(_rest, "RestButton").onClick.Invoke();
            ButtonAt(_modal, "Screen/Panel/Confirm").onClick.Invoke();

            Assert.That(_modal.IsOpen, Is.False);
            Assert.That(Simulation.State.Cave, Is.EqualTo(1), "a run nova, do nível 1");
            Assert.That(Simulation.State.Shards, Is.EqualTo(0));
            Assert.That(Simulation.Permanents.State.RedShards, Is.EqualTo(7), "o banco somou");
            Assert.That(Simulation.Permanents.State.Day, Is.EqualTo(2));
            Assert.That(_rest.IsShown, Is.False, "no nível 1 não tem Descansar");

            Assert.That(_summary.IsOpen, Is.True, "o resumo do dia");
            Assert.That(_summary.TitleText, Is.EqualTo("DIA 1 ENCERRADO"));
            Assert.That(_summary.LevelText, Is.EqualTo("12"));
            Assert.That(_summary.GainedText, Is.EqualTo("+7"));
            Assert.That(_summary.BankText, Is.EqualTo("7"));

            ButtonAt(_summary, "Screen/Panel/Start").onClick.Invoke();
            Assert.That(_summary.IsOpen, Is.False, "COMEÇAR fecha");
        }

        [UnityTest]
        public IEnumerator DebugRestNow_SkipsTheModalAndTheSummary()
        {
            yield return LoadGame();
            Simulation.SkipToCave(12);
            Assert.That(_runner.Rest(force: true), Is.Not.Null);
            Assert.That(_modal.IsOpen, Is.False);
            Assert.That(_summary.IsOpen, Is.False, "o atalho do DBG não para no resumo");
        }

        [UnityTest]
        public IEnumerator TapOutsideTheBar_StillHitsTheCrystal()
        {
            yield return LoadGame();
            Simulation.SkipToCave(12);
            BuyTheMap();
            yield return null;
            var input = Object.FindAnyObjectByType<TapInput>();

            int combo = Simulation.Combo;
            Assert.That(input.TapAt(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)), Is.True, "o meio da tela");
            Assert.That(Simulation.Combo, Is.EqualTo(combo + 1));

            foreach (var rect in new[] { (RectTransform)_rest.transform.Find("RestButton"),
                                         (RectTransform)_pass.transform.Find("PassCaveButton") })
                Assert.That(input.TapAt(ScreenCenterOf(rect)), Is.False, rect.name + " é interface");
        }

        [UnityTest]
        public IEnumerator HeartScreen_HasRest_ThatOpensTheModal()
        {
            yield return LoadGame();
            var heart = Object.FindAnyObjectByType<HeartScreen>();
            Simulation.SkipToCave(Simulation.Rules.CaveCount);

            heart.Open();
            Assert.That(heart.IsRestShown, Is.True, "no último nível dá para descansar");
            ButtonAt(heart, "Screen/Panel/Rest").onClick.Invoke();
            Assert.That(heart.IsOpen, Is.False);
            Assert.That(_modal.IsOpen, Is.True);
            Assert.That(_modal.NextText, Is.Not.Empty);
            Assert.That(_modal.NextText, Does.Not.Contain("+"), "não há próximo nível");
        }
    }
}
