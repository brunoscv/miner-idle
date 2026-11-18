using System.Collections;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9d E2 (D-265, D-279, D-280): a tela do descanso na cena Game. O ZZZ passa sozinho em 1,5 s; com a tela aberta,
    /// nada se mexe atrás (o anão, os cristais, o tempo do dia); abre desfocada (D-280) e o DBG → Dia troca para a preta.
    /// No teste em batch (sem placa de vídeo) a foto do fundo desfocado não sai e ele fica preto: o teste mede isso.
    /// </summary>
    public class RestShopScreenTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        MiningRunner _runner;
        RestShopScreen _rest;
        DebugPanel _debug;

        MiningSimulation Simulation => _runner.Simulation;

        IEnumerator LoadGame()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            _rest = Object.FindAnyObjectByType<RestShopScreen>();
            _debug = Object.FindAnyObjectByType<DebugPanel>();
            Assert.That(_rest, Is.Not.Null, "a cena precisa da tela do descanso (rode o setup do projeto)");
        }

        void RestByTheModal()
        {
            Simulation.SkipToCave(12);
            var modal = Object.FindAnyObjectByType<RestScreen>();
            modal.Open();
            modal.transform.Find("Screen/Panel/Confirm").GetComponent<Button>().onClick.Invoke();
        }

        Button DebugButton(string name)
        {
            var target = _debug.transform.Find("DayMenu/Content/Buttons/" + name);
            Assert.That(target, Is.Not.Null, "botão não encontrado: " + name);
            return target.GetComponent<Button>();
        }

        [UnityTest]
        public IEnumerator TheZzz_PassesByItself_AndNothingMovesBehind()
        {
            yield return LoadGame();
            RestByTheModal();
            Assert.That(_rest.IsZzzShown, Is.True);
            double distance = Simulation.Miner.Distance;
            int crystals = Simulation.Crystals.Count;

            yield return new WaitForSecondsRealtime(1.8f);

            Assert.That(_rest.IsZzzShown, Is.False, "1,5 s e o ZZZ sai");
            Assert.That(_rest.IsPanelShown, Is.True, "a loja do descanso");
            Assert.That(Simulation.Miner.Distance, Is.EqualTo(distance), "o anão parado");
            Assert.That(Simulation.Crystals.Count, Is.EqualTo(crystals), "nenhum cristal nasce");
            Assert.That(Simulation.State.PlaySeconds, Is.EqualTo(0), "o tempo do dia não anda");
        }

        [UnityTest]
        public IEnumerator TheDebugBackground_SwitchesBlurredAndBlack_WithTheScreenOpen()
        {
            yield return LoadGame();
            Assert.That(_rest.BackgroundMode, Is.EqualTo(RestShopScreen.Background.Blurred), "a desfocada, escolhida (D-280)");
            var button = DebugButton("RestBackground");
            Assert.That(button.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("Fundo: desfocada"));

            _runner.Rest(force: true);
            Assert.That(_rest.IsOpen, Is.True);
            bool canCapture = SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
            Assert.That(_rest.IsBlurredShown, Is.EqualTo(canCapture), "o jogo desfocado (sem placa de vídeo, fica a preta)");

            button.onClick.Invoke();
            Assert.That(_rest.BackgroundMode, Is.EqualTo(RestShopScreen.Background.Black));
            Assert.That(button.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("Fundo: preta"));
            Assert.That(_rest.IsBlurredShown, Is.False);

            button.onClick.Invoke();
            Assert.That(_rest.BackgroundMode, Is.EqualTo(RestShopScreen.Background.Blurred));
            Assert.That(_rest.IsBlurredShown, Is.EqualTo(canCapture));

            _runner.StartRun();
            Assert.That(_rest.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator ThePromotion_ShowsWhatItNeeds_OnTheRestScreen()
        {
            yield return LoadGame();
            _runner.Rest(force: true);
            Simulation.AddRedShards(1000);
            PermanentRowView promotion = null;
            foreach (var row in _rest.Rows)
                if (row.Permanent == PermanentId.InternPromotion) promotion = row;
            Assert.That(promotion, Is.Not.Null);
            Assert.That(promotion.CanBuy, Is.False);
            Assert.That(promotion.EffectText, Is.EqualTo("Pede o Treinar no 5"));

            for (int i = 0; i < 5; i++) Simulation.PurchasePermanent(PermanentId.InternTraining);
            Assert.That(promotion.CanBuy, Is.True, "Treinar 5: acende");
        }
    }
}
