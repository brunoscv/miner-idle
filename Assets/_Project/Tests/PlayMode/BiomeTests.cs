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
    /// Biomas das 10 cavernas (M1b E8, D-102; antes, o chão de comparação da E9): o jogo abre na caverna 1, e o DBG →
    /// Câmera → Bioma passa pelas 10 (cada uma com o seu chão e a sua neblina, com o nome no botão) e volta à 1.
    /// </summary>
    public class BiomeTests
    {
        [UnityTest]
        public IEnumerator Biome_StartsAtCave1_AndDebugButtonCyclesBack()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Project/Scenes/Game.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;

            var stage = Object.FindAnyObjectByType<DescentStage>();
            if (stage.Biome == null) Assert.Ignore("Sem os biomas na cena (rode o setup).");
            Assert.That(stage.FloorTexture.name, Is.EqualTo("floor_cave_01"), "o jogo abre com o chão da caverna 1");

            var panel = Object.FindAnyObjectByType<MinersIdle.UI.DescentCameraPanel>(FindObjectsInactive.Include);
            var button = panel.transform.Find("Content/Buttons/Biome");
            if (button == null) Assert.Ignore("Sem o botão do bioma (rode o setup).");
            panel.Toggle();
            var label = button.GetComponentInChildren<TMP_Text>();

            var first = stage.Biome;
            for (int cave = 2; cave <= 10; cave++)
            {
                var before = stage.SceneCamera.backgroundColor;
                button.GetComponent<Button>().onClick.Invoke();
                yield return null; // o painel atualiza o texto no quadro seguinte ao clique
                Assert.That(stage.FloorTexture.name, Is.EqualTo($"floor_cave_{cave:00}"), "o botão passa para a próxima caverna");
                Assert.That(stage.SceneCamera.backgroundColor, Is.Not.EqualTo(before), "a neblina muda com a caverna");
                Assert.That(label.text, Is.EqualTo(stage.Biome.DisplayName), "o botão mostra o nome da caverna");
            }

            button.GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(stage.Biome, Is.SameAs(first), "depois da caverna 10 volta à 1");
            Assert.That(stage.FloorTexture.name, Is.EqualTo("floor_cave_01"));
        }
    }
}
