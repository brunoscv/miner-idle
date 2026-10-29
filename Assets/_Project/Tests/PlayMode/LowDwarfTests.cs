using System.Collections;
using MinersIdle.Presentation;
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
    /// Anão com menos pixels (folhas <c>low-dwarf-*</c>, quadros de 64 px): só pelo DBG → Câmera → "Anão 64px", para
    /// comparar com o anão atual. O jogo começa sempre com o anão atual.
    /// </summary>
    public class LowDwarfTests
    {
        [UnityTest]
        public IEnumerator LowDwarf_SwapsOnlyWhenTurnedOn_AndBack()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Project/Scenes/Game.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;

            var stage = Object.FindAnyObjectByType<DescentStage>();
            if (stage.DwarfSheet == null) Assert.Ignore("Sem as folhas do anão na cena (rode o setup).");
            Assert.That(stage.UseLowDwarf, Is.False, "começa com o anão atual");
            Assert.That(stage.DwarfSheet.name, Does.Not.StartWith("low-"));

            var panel = Object.FindAnyObjectByType<MinersIdle.UI.DescentCameraPanel>(FindObjectsInactive.Include);
            var button = panel.transform.Find("Content/Buttons/LowDwarf");
            if (button == null) Assert.Ignore("Sem o botão do anão com menos pixels (rode o setup).");
            panel.Toggle();
            button.GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return null;
            Assert.That(stage.DwarfSheet.name, Does.StartWith("low-"), "o botão troca o anão");

            button.GetComponent<Button>().onClick.Invoke();
            yield return null;
            yield return null;
            Assert.That(stage.DwarfSheet.name, Does.Not.StartWith("low-"), "e destroca");
        }

        /// <summary>Os botões do DBG → Câmera (duas linhas desde a E9) cabem dentro do painel, com o nome numa linha só.</summary>
        [UnityTest]
        public IEnumerator CameraPanelButtons_FitInsideThePanel()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Project/Scenes/Game.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var panel = Object.FindAnyObjectByType<MinersIdle.UI.DescentCameraPanel>(FindObjectsInactive.Include);
            var content = (RectTransform)panel.transform.Find("Content");
            var buttons = (RectTransform)content.Find("Buttons");
            if (buttons.Find("LowDwarf") == null) Assert.Ignore("Sem o botão do anão com menos pixels (rode o setup).");
            content.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttons);

            var corners = new Vector3[4];
            content.GetWorldCorners(corners);
            var area = new Rect(corners[0], corners[2] - corners[0]);
            foreach (RectTransform button in buttons)
            {
                button.GetWorldCorners(corners);
                Assert.That(area.Contains(corners[0]) && area.Contains(corners[2]), Is.True, $"{button.name} cabe no painel");
                var label = button.GetComponentInChildren<TMP_Text>();
                Assert.That(label.preferredWidth, Is.LessThanOrEqualTo(button.rect.width), $"o nome de {button.name} cabe");
            }
        }
    }
}
