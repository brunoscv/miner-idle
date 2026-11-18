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
        public IEnumerator HitCriticalBreakAndPurchase_EachPlayTheirSound()
        {
            yield return LoadGame();
            var simulation = _runner.Simulation;
            _runner.TimeScale = 0f; // o teste anda o tempo
            yield return new WaitForSecondsRealtime(0.1f); // longe de qualquer som dos quadros anteriores
            _runner.ForceCritical = true;

            int hits = _sound.PlayedCount(Sfx.Hit);
            int criticals = _sound.PlayedCount(Sfx.Critical);
            int breaks = _sound.PlayedCount(Sfx.Break);
            simulation.Tick(simulation.SecondsToNextHit + 1e-6);
            _runner.ForceCritical = false;
            Assert.That(_sound.PlayedCount(Sfx.Hit), Is.EqualTo(hits + 1), "golpe do anão");
            Assert.That(_sound.PlayedCount(Sfx.Critical), Is.EqualTo(criticals + 1), "crítico");

            // No v8 o golpe já quebra o cristal do andar 1; o BreakTarget só quebra se ainda houver alvo.
            simulation.BreakTarget();
            Assert.That(_sound.PlayedCount(Sfx.Break), Is.GreaterThan(breaks), "quebra");

            simulation.AddShards(1e9);
            bool bought = false;
            foreach (UpgradeId id in System.Enum.GetValues(typeof(UpgradeId))) // o save de outro teste pode ter uma linha no limite
                if (simulation.Purchase(id) == PurchaseResult.Purchased) { bought = true; break; }
            Assert.That(bought, Is.True);
            Assert.That(_sound.PlayedCount(Sfx.Purchase), Is.EqualTo(1), "compra");
        }

        /// <summary>Vários golpes no mesmo quadro (um passo grande de jogo) tocam o som uma vez (era com os toques, D-311).</summary>
        [UnityTest]
        public IEnumerator SeveralHitsInOneFrame_PlayTheHitOnce()
        {
            yield return LoadGame();
            var simulation = _runner.Simulation;
            _runner.TimeScale = 0f; // o teste anda o tempo
            yield return new WaitForSecondsRealtime(0.1f); // longe de qualquer som dos quadros anteriores
            int hits = _sound.PlayedCount(Sfx.Hit);
            int landed = 0;
            void OnHit(MiningHit hit) => landed++;
            simulation.HitLanded += OnHit;

            for (int i = 0; i < 5; i++) simulation.Tick(simulation.SecondsToNextHit + 1e-6);

            simulation.HitLanded -= OnHit;
            Assert.That(landed, Is.GreaterThan(1), "o anão bateu mais de uma vez");
            Assert.That(_sound.PlayedCount(Sfx.Hit), Is.EqualTo(hits + 1), "o mesmo som não se empilha no mesmo quadro");
        }

        [UnityTest]
        public IEnumerator SoundOffInTheDebugPanel_SilencesEverything()
        {
            yield return LoadGame();
            var panel = Object.FindAnyObjectByType<MinersIdle.UI.DebugPanel>();
            var button = panel.transform.Find("EffectsMenu/Content/Buttons/Sound");
            Assert.That(button, Is.Not.Null, "botão Som on/off no DBG → Efeitos (D-147)");

            button.GetComponent<Button>().onClick.Invoke();
            Assert.That(_sound.SoundEnabled, Is.False);

            int hits = _sound.PlayedCount(Sfx.Hit);
            int breaks = _sound.PlayedCount(Sfx.Break);
            _runner.TimeScale = 0f;
            _runner.Simulation.Tick(_runner.Simulation.SecondsToNextHit + 1e-6);
            _runner.Simulation.BreakTarget();
            Assert.That(_sound.PlayedCount(Sfx.Hit), Is.EqualTo(hits));
            Assert.That(_sound.PlayedCount(Sfx.Break), Is.EqualTo(breaks));

            button.GetComponent<Button>().onClick.Invoke();
            Assert.That(_sound.SoundEnabled, Is.True, "liga de novo");
        }
    }
}
