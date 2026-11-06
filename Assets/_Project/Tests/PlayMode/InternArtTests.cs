using System.Collections;
using System.IO;
using MinersIdle.Presentation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M1b (E7, D-76): as variações de cor do anão ficam guardadas no jogo; o estagiário é o anão grisalho, ao lado e um
    /// pouco atrás do anão. Desde o M7 (E1) ele aparece quando é contratado na simulação, e o botão "Estagiário" do DBG →
    /// Câmera (que só ligava o desenho) saiu.
    /// </summary>
    public class InternArtTests
    {
        const string Variants = "Assets/_Project/Art/Character/Variants/";

        [Test]
        public void DwarfColorVariants_AreStoredForTheFuturePicker()
        {
            foreach (var sheet in new[] { "dwarf-idle", "dwarf-walking", "dwarf-mining" })
            foreach (var color in new[] { "castanho", "loiro", "grisalho" })
                Assert.That(File.Exists($"{Variants}{sheet}-{color}.png"), Is.True, $"{sheet}-{color}");
        }

        [UnityTest]
        public IEnumerator Intern_ShowsOnlyWhenHired_BesideAndBehindTheDwarf()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Project/Scenes/Game.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;

            var stage = Object.FindAnyObjectByType<DescentStage>();
            if (stage.DwarfFrame < 0) Assert.Ignore("Sem as folhas do anão na cena (rode o setup).");
            var simulation = Object.FindAnyObjectByType<MinersIdle.Game.MiningRunner>().Simulation;
            if (!simulation.Economy.HasIntern) Assert.Ignore("O balanço ativo não tem estagiário (rode o setup: GDD v7).");
            simulation.FireIntern(); // o save de outro teste pode ter um contratado
            yield return null;
            yield return null;
            Assert.That(stage.InternFrame, Is.EqualTo(-1), "sem contratar, escondido");

            simulation.HireIntern();
            yield return null;
            yield return null;
            Assert.That(stage.InternFrame, Is.GreaterThanOrEqualTo(0), "contratado, aparece");

            var intern = stage.transform.Find("Intern");
            var dwarf = stage.transform.Find("Dwarf");
            Assert.That(intern.GetComponent<MeshRenderer>().enabled, Is.True);
            Assert.That(intern.localPosition.x, Is.GreaterThan(dwarf.localPosition.x), "ao lado");
            Assert.That(intern.localPosition.z, Is.GreaterThan(dwarf.localPosition.z), "um pouco atrás");

            simulation.FireIntern();
            yield return null;
            yield return null;
            Assert.That(intern.GetComponent<MeshRenderer>().enabled, Is.False, "demitido, some de novo");
        }

        /// <summary>
        /// Até o M7 o botão "Estagiário" do DBG → Câmera ligava o desenho (e a regra era cabê-lo no painel). Agora quem decide
        /// é a contratação: o botão saiu, e os atalhos estão no DBG → Estagiário.
        /// </summary>
        [UnityTest]
        public IEnumerator CameraPanel_HasNoInternButton_TheDbgInternMenuDoes()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Project/Scenes/Game.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var panel = Object.FindAnyObjectByType<MinersIdle.UI.DescentCameraPanel>(FindObjectsInactive.Include);
            Assert.That(panel.transform.Find("Content/Buttons/Intern"), Is.Null, "o botão da câmera saiu");
            var debug = Object.FindAnyObjectByType<MinersIdle.UI.DebugPanel>(FindObjectsInactive.Include);
            Assert.That(debug.transform.Find("InternMenu/Content/Buttons/Hire"), Is.Not.Null, "Contratar no DBG → Estagiário");
        }
    }
}
