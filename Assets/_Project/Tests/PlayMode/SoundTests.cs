using System.Collections;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MinersIdle.Tests
{
    /// <summary>
    /// Marco 5, E4: cada evento tem o seu bipe, o mesmo som não se empilha no mesmo quadro e o DBG desliga o som. Conta
    /// pelo <see cref="SoundBoard.PlayedCount"/>, que não depende de haver placa de som (o batch roda sem).
    /// </summary>
    public class SoundTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        MiningRunner _runner;
        SoundBoard _sound;

        IEnumerator LoadGame()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null; // a partida já escolheu um alvo

            _runner = Object.FindAnyObjectByType<MiningRunner>();
            _sound = Object.FindAnyObjectByType<SoundBoard>();
            Assert.That(_sound, Is.Not.Null, "a cena Game precisa do som (rode o setup do projeto)");
            Assert.That(_runner.Simulation.Miner.Target, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator EverySound_IsAShortClipThatStartsAndEndsSilent()
        {
            yield return LoadGame();

            for (int i = 0; i < SfxSynth.Count; i++)
            {
                var clip = _sound.ClipOf((Sfx)i);
                Assert.That(clip, Is.Not.Null, ((Sfx)i).ToString());
                Assert.That(clip.length, Is.InRange(0.02f, 0.5f), ((Sfx)i) + ": bipe curto");

                var samples = new float[clip.samples];
                clip.GetData(samples, 0);
                Assert.That(Mathf.Abs(samples[0]), Is.LessThan(0.05f), ((Sfx)i) + " começa sem estalo");
                Assert.That(Mathf.Abs(samples[samples.Length - 1]), Is.LessThan(0.05f), ((Sfx)i) + " termina sem estalo");
            }
        }

        [UnityTest]
        public IEnumerator TapCriticalBreakAndPurchase_EachPlayTheirSound()
        {
            yield return LoadGame();
            var simulation = _runner.Simulation;
            _runner.ForceCritical = true;

            int taps = _sound.PlayedCount(Sfx.Tap);
            int criticals = _sound.PlayedCount(Sfx.Critical);
            simulation.Tap();
            Assert.That(_sound.PlayedCount(Sfx.Tap), Is.EqualTo(taps + 1), "toque");
            Assert.That(_sound.PlayedCount(Sfx.Critical), Is.EqualTo(criticals + 1), "crítico");

            int breaks = _sound.PlayedCount(Sfx.Break);
            simulation.BreakTarget();
            Assert.That(_sound.PlayedCount(Sfx.Break), Is.EqualTo(breaks + 1), "quebra");

            simulation.AddShards(1e9);
            bool bought = false;
            foreach (UpgradeId id in System.Enum.GetValues(typeof(UpgradeId))) // o save de outro teste pode ter uma linha no limite
                if (simulation.Purchase(id) == PurchaseResult.Purchased) { bought = true; break; }
            Assert.That(bought, Is.True);
            Assert.That(_sound.PlayedCount(Sfx.Purchase), Is.EqualTo(1), "compra");

            // O golpe do anão: espera o próximo (no tempo ×20 vem logo).
            int hits = _sound.PlayedCount(Sfx.Hit);
            _runner.TimeScale = 20f;
            float waited = 0f;
            while (_sound.PlayedCount(Sfx.Hit) == hits && waited < 3f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.That(_sound.PlayedCount(Sfx.Hit), Is.GreaterThan(hits), "golpe do anão");
        }

        [UnityTest]
        public IEnumerator TenTapsInOneFrame_PlayTheTapOnce_AndTheMilestoneOnce()
        {
            yield return LoadGame();
            yield return new WaitForSecondsRealtime(0.1f); // longe de qualquer som dos quadros anteriores
            int taps = _sound.PlayedCount(Sfx.Tap);

            for (int i = 0; i < 10; i++) _runner.Simulation.Tap();

            Assert.That(_sound.PlayedCount(Sfx.Tap), Is.EqualTo(taps + 1), "o mesmo som não se empilha no mesmo quadro");
            Assert.That(_sound.PlayedCount(Sfx.Milestone), Is.EqualTo(1), "x10");
        }

        [UnityTest]
        public IEnumerator SoundOffInTheDebugPanel_SilencesEverything()
        {
            yield return LoadGame();
            var panel = Object.FindAnyObjectByType<MinersIdle.UI.DebugPanel>();
            var button = panel.transform.Find("Content/Buttons/Sound");
            Assert.That(button, Is.Not.Null, "botão Som on/off no DBG");

            button.GetComponent<Button>().onClick.Invoke();
            Assert.That(_sound.SoundEnabled, Is.False);

            int taps = _sound.PlayedCount(Sfx.Tap);
            int breaks = _sound.PlayedCount(Sfx.Break);
            _runner.Simulation.Tap();
            _runner.Simulation.BreakTarget();
            Assert.That(_sound.PlayedCount(Sfx.Tap), Is.EqualTo(taps));
            Assert.That(_sound.PlayedCount(Sfx.Break), Is.EqualTo(breaks));

            button.GetComponent<Button>().onClick.Invoke();
            Assert.That(_sound.SoundEnabled, Is.True, "liga de novo");
        }
    }
}
