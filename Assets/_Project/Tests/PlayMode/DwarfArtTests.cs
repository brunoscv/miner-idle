using System.Collections;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M1b (E6, D-75): o anão são as folhas do PixelLab tocadas inteiras. O quadro de impacto aparece no instante do
    /// dano; a corrida nunca mostra a pose parada (quadro 0).
    /// </summary>
    public class DwarfArtTests
    {
        DescentStage _stage;
        MiningRunner _runner;

        IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Project/Scenes/Game.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _stage = Object.FindAnyObjectByType<DescentStage>();
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            yield return null;
            if (_stage.DwarfFrame < 0) Assert.Ignore("Sem as folhas do anão na cena (rode o setup).");
        }

        [UnityTest]
        public IEnumerator ImpactFrame_ShowsAtTheInstantOfTheHit()
        {
            yield return Load();
            var simulation = _runner.Simulation;
            int hits = 0;
            void OnHit(MiningHit hit) => hits++;
            simulation.HitLanded += OnHit;

            int checkedHits = 0;
            float timeout = Time.realtimeSinceStartup + 20f;
            while (checkedHits < 3 && Time.realtimeSinceStartup < timeout)
            {
                int before = hits;
                yield return null; // volta depois do Update deste quadro: o golpe pode ter acontecido agora
                if (hits == before || simulation.Miner.State != MinerState.Mining) continue;
                // O desenho (LateUpdate) do quadro do golpe roda depois desta volta; confere no próximo.
                yield return null;
                Assert.That(_stage.DwarfFrame, Is.EqualTo(_stage.DwarfImpactFrame), "impacto no instante do dano");
                checkedHits++;
            }
            simulation.HitLanded -= OnHit;
            Assert.That(checkedHits, Is.GreaterThan(0), "viu golpes");
        }

        [UnityTest]
        public IEnumerator Running_NeverShowsTheStandingPose()
        {
            yield return Load();
            var simulation = _runner.Simulation;
            _runner.TimeScale = 5f;
            int runningFrames = 0;
            float timeout = Time.realtimeSinceStartup + 20f;
            while (runningFrames < 30 && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
                if (simulation.Miner.State != MinerState.Running) continue;
                Assert.That(_stage.DwarfFrame, Is.InRange(1, 8), "quadros da corrida");
                runningFrames++;
            }
            Assert.That(runningFrames, Is.GreaterThan(0), "viu o anão correr");
        }
    }
}
