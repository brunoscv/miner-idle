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
    /// pouco atrás do anão, e só aparece pelo DBG até o M7.
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
        public IEnumerator Intern_ShowsOnlyWhenTurnedOn_BesideAndBehindTheDwarf()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Project/Scenes/Game.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;

            var stage = Object.FindAnyObjectByType<DescentStage>();
            if (stage.DwarfFrame < 0) Assert.Ignore("Sem as folhas do anão na cena (rode o setup).");
            Assert.That(stage.InternFrame, Is.EqualTo(-1), "começa escondido");

            stage.ShowIntern = true;
            yield return null;
            yield return null;
            Assert.That(stage.InternFrame, Is.GreaterThanOrEqualTo(0), "aparece pelo DBG");

            var intern = stage.transform.Find("Intern");
            var dwarf = stage.transform.Find("Dwarf");
            Assert.That(intern.GetComponent<MeshRenderer>().enabled, Is.True);
            Assert.That(intern.localPosition.x, Is.GreaterThan(dwarf.localPosition.x), "ao lado");
            Assert.That(intern.localPosition.z, Is.GreaterThan(dwarf.localPosition.z), "um pouco atrás");

            stage.ShowIntern = false;
            yield return null;
            yield return null;
            Assert.That(intern.GetComponent<MeshRenderer>().enabled, Is.False, "some de novo");
        }

        /// <summary>O botão "Estagiário" do DBG → Câmera fica dentro do painel (com 3 colunas ia para fora dele).</summary>
        [UnityTest]
        public IEnumerator InternButton_SitsInsideTheCameraPanel()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Project/Scenes/Game.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var panel = Object.FindAnyObjectByType<MinersIdle.UI.DescentCameraPanel>(FindObjectsInactive.Include);
            var content = (RectTransform)panel.transform.Find("Content");
            var button = (RectTransform)content.Find("Buttons/Intern");
            if (button == null) Assert.Ignore("Sem o estagiário na cena (rode o setup).");
            content.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)button.parent);

            var corners = new Vector3[4];
            content.GetWorldCorners(corners);
            var area = new Rect(corners[0], corners[2] - corners[0]);
            button.GetWorldCorners(corners);
            Assert.That(area.Contains(corners[0]) && area.Contains(corners[2]), Is.True, "o botão cabe no painel");
        }
    }
}
