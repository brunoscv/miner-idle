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
    /// Chão de arte própria (M1b E9, D-90): o jogo começa com o tile 01 do Bruno, e o DBG → Câmera → Chão passa pelos
    /// outros tons (e o calçamento antigo, se o pack estiver no PC) e volta ao 01, com o nome do chão no botão.
    /// </summary>
    public class FloorTests
    {
        [UnityTest]
        public IEnumerator Floor_StartsWithOwnTile_AndDebugButtonCyclesBack()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Project/Scenes/Game.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;

            var stage = Object.FindAnyObjectByType<DescentStage>();
            if (stage.FloorTexture == null || stage.FloorTexture.name != "tileset_ground_01")
                Assert.Ignore("O chão da cena não é o tile do Bruno (rode o setup).");

            var panel = Object.FindAnyObjectByType<MinersIdle.UI.DescentCameraPanel>(FindObjectsInactive.Include);
            var button = panel.transform.Find("Content/Buttons/Floor");
            if (button == null) Assert.Ignore("Sem o botão do chão (rode o setup).");
            panel.Toggle();
            var label = button.GetComponentInChildren<TMP_Text>();

            string[] tones = { "tileset_ground_02", "tileset_ground_03", "tileset_ground_04" };
            foreach (var tone in tones)
            {
                button.GetComponent<Button>().onClick.Invoke();
                yield return null;
                Assert.That(stage.FloorTexture.name, Is.EqualTo(tone), "o botão passa para o próximo tom");
                Assert.That(label.text, Is.EqualTo(stage.FloorLabel), "o botão mostra o nome do chão");
            }

            // Depois do 04 vem o calçamento (se o pack estiver no PC) e então o 01 de novo.
            for (int i = 0; i < 2 && stage.FloorTexture.name != "tileset_ground_01"; i++)
            {
                button.GetComponent<Button>().onClick.Invoke();
                yield return null;
            }
            Assert.That(stage.FloorTexture.name, Is.EqualTo("tileset_ground_01"), "volta ao chão padrão");
        }
    }
}
