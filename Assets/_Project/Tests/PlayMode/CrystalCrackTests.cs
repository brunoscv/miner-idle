using System.Collections;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using MinersIdle.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9f, o cristal rachando (D-299…D-302): a folha do v8 nos biomas, o estágio pela vida, a barra de vida fora (com o
    /// DBG para comparar) e os botões do DBG → Andar.
    /// </summary>
    public class CrystalCrackTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        [TestCase(10.0, 10.0, 0)]
        [TestCase(9.9, 10.0, 1)]
        [TestCase(7.0, 10.0, 1)]
        [TestCase(6.5, 10.0, 2)]
        [TestCase(3.4, 10.0, 2)]
        [TestCase(3.3, 10.0, 3)]
        [TestCase(0.1, 10.0, 3)]
        [TestCase(0.0, 10.0, 3)]
        public void Stage_IntactOnlyAtFullHealth_ThenByThirds(double health, double max, int expected)
        {
            Assert.That(CrackStage.Of(health, max), Is.EqualTo(expected));
        }

        [TestCase(9.9, 0.1, 10.0, true, 1)]   // o primeiro golpe tira o inteiro
        [TestCase(9.0, 0.5, 10.0, false, 1)]  // dentro do mesmo terço
        [TestCase(6.0, 1.5, 10.0, true, 2)]   // passou de 2/3
        [TestCase(1.0, 8.9, 10.0, true, 3)]   // pulou dois estágios num golpe: um efeito só
        [TestCase(0.0, 3.0, 10.0, false, 3)]  // quebrou: a quebra tem o efeito dela
        public void Crossed_OnlyWhenTheHitReachesANewStage(double after, double damage, double max, bool crossed, int stage)
        {
            Assert.That(CrackStage.Crossed(after, damage, max, out int now), Is.EqualTo(crossed));
            Assert.That(now, Is.EqualTo(stage));
        }

        [UnityTest]
        public IEnumerator NewCrack_FlashesAndDropsChips_OncePerStage()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            var runner = Object.FindAnyObjectByType<MiningRunner>();
            runner.TimeScale = 0f;
            yield return null;

            var stage = Object.FindAnyObjectByType<DescentStage>();
            var feedback = Object.FindAnyObjectByType<MiningFeedbackView>();
            var sparks = Object.FindAnyObjectByType<SparkBurst>();
            var sim = runner.Simulation;

            // Andar 20: o cristal aguenta muitos golpes da Picareta do começo, então os primeiros só passam ao racha 1.
            sim.GoToFloor(20);
            for (int i = 0; i < 400 && sim.IsInFloorTransition; i++) { sim.Tick(0.05); yield return null; }
            yield return null;

            int hits = 0, crossings = 0, lastStage = -1;
            Crystal first = null;
            System.Action<MiningHit> onHit = hit =>
            {
                if (first == null) first = hit.Crystal;
                if (hit.Crystal != first) return;
                hits++;
                int now = CrackStage.Of(hit.Crystal.Health, hit.Crystal.MaxHealth);
                if (hit.Crystal.Health > 0 && now > (lastStage < 0 ? 0 : lastStage)) crossings++;
                lastStage = now;
            };
            sim.HitLanded += onHit;
            int before = feedback.CracksShown;
            bool flashed = false;
            for (int i = 0; i < 3000 && hits < 3; i++)
            {
                sim.Tick(0.02);
                if (first != null && stage.IsCrystalFlashing(first)) flashed = true;
                if (i % 10 == 0) yield return null;
            }
            sim.HitLanded -= onHit;

            Assert.That(hits, Is.GreaterThanOrEqualTo(3), "o anão bateu três vezes no mesmo cristal");
            Assert.That(first.Health, Is.GreaterThan(0), "sem quebrar");
            Assert.That(crossings, Is.GreaterThanOrEqualTo(1), "o primeiro golpe tira o inteiro (D-300)");
            Assert.That(feedback.CracksShown - before, Is.EqualTo(crossings), "um efeito por estágio novo, não por golpe (D-303)");
            Assert.That(flashed, Is.True, "o cristal brilhou ao rachar");
            Assert.That(sparks.ActiveCount, Is.GreaterThan(0), "as lascas no ar");
        }

        [Test]
        public void Stage_InvalidMax_IsIntact()
        {
            Assert.That(CrackStage.Of(1, 0), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Target_CracksWithItsHealth_AndTheHealthBarIsGone()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            var runner = Object.FindAnyObjectByType<MiningRunner>();
            runner.TimeScale = 0f;
            yield return null;
            yield return null;

            var stage = Object.FindAnyObjectByType<DescentStage>();
            Assert.That(stage.Biome, Is.Not.Null);
            Assert.That(stage.Biome.HasCrystalSheet, Is.True, "o bioma com a folha do v8 (rode o setup do projeto)");
            int row = stage.Biome.CrystalRow;
            Assert.That(row, Is.EqualTo(0), "andares 1–10: azul (D-299)");

            var target = runner.Simulation.Miner.Target;
            Assert.That(target, Is.Not.Null);
            Assert.That(target.Health, Is.EqualTo(target.MaxHealth));
            Assert.That(stage.TargetCrackStage, Is.EqualTo(0), "inteiro com a vida cheia");
            Assert.That(stage.TargetCrystalFrame, Is.EqualTo(row * CrackStage.Count));
            Assert.That(stage.IsHealthBarShown, Is.False, "sem barra de vida no v8 (D-301)");

            target.ApplyDamage(target.MaxHealth * 0.1);
            yield return null;
            Assert.That(stage.TargetCrackStage, Is.EqualTo(1), "o primeiro golpe já racha");
            Assert.That(stage.TargetCrystalFrame, Is.EqualTo(row * CrackStage.Count + 1));

            target.ApplyDamage(target.MaxHealth * 0.4);
            yield return null;
            Assert.That(stage.TargetCrackStage, Is.EqualTo(2));

            target.ApplyDamage(target.MaxHealth * 0.3);
            yield return null;
            Assert.That(stage.TargetCrackStage, Is.EqualTo(3), "o último terço");
        }

        [UnityTest]
        public IEnumerator DebugButtons_BringTheHealthBarBack_AndShowTheStages()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            var runner = Object.FindAnyObjectByType<MiningRunner>();
            runner.TimeScale = 0f;
            yield return null;

            var stage = Object.FindAnyObjectByType<DescentStage>();
            var panel = Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include);
            var bar = panel.transform.Find("CaveMenu/Content/Buttons/CrystalHealthBar");
            var stages = panel.transform.Find("CaveMenu/Content/Buttons/CrystalStages");
            Assert.That(bar, Is.Not.Null, "DBG → Andar → Barra de vida (rode o setup do projeto)");
            Assert.That(stages, Is.Not.Null, "DBG → Andar → Estágios (rode o setup do projeto)");

            bar.GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(stage.ShowHealthBar, Is.True);
            Assert.That(stage.IsHealthBarShown, Is.True, "a barra volta para comparar");
            Assert.That(bar.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("Barra de vida: sim"));

            bar.GetComponent<Button>().onClick.Invoke();
            stages.GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(stage.IsHealthBarShown, Is.False);
            Assert.That(stage.ShowCrackStages, Is.True);
            Assert.That(stages.GetComponentInChildren<TMP_Text>().text, Is.EqualTo("Estágios: sim"));
            stages.GetComponent<Button>().onClick.Invoke();
            Assert.That(stage.ShowCrackStages, Is.False);
        }

        [UnityTest]
        public IEnumerator EachBiome_HasItsRowOfTheSheet()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            var runner = Object.FindAnyObjectByType<MiningRunner>();
            runner.TimeScale = 0f;
            yield return null;

            var stage = Object.FindAnyObjectByType<DescentStage>();
            int[] expected = { 0, 2, 3, 1, 4, 1, 0, 2, 4, 3 }; // D-299
            for (int b = 0; b < expected.Length; b++)
            {
                runner.Simulation.GoToFloor(b * 10 + 1);
                for (int i = 0; i < 400 && runner.Simulation.IsInFloorTransition; i++)
                {
                    runner.Simulation.Tick(0.05);
                    yield return null;
                }
                yield return null;
                Assert.That(stage.Biome.HasCrystalSheet, Is.True, $"bioma {b + 1} com a folha");
                Assert.That(stage.Biome.CrystalRow, Is.EqualTo(expected[b]), $"bioma {b + 1}");
            }
        }
    }
}
