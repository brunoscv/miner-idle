using System.Collections;
using MinersIdle.Game;
using MinersIdle.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M1b (E4, D-74): o corredor da caverna 1 com a arte do Pixel Fantasy Caves. Texturas geradas grandes e em pixel art;
    /// esteios em sprite só do anão para o fundo. Sem o pack (fora do git, D-65) o corredor é o gerado por código e os
    /// testes são ignorados.
    /// </summary>
    public class CorridorArtTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
        const string Generated = "Assets/_Vendor/AssetStore/PixelFantasyCaves/Generated/";

        [Test]
        public void GeneratedTextures_AreBigPixelArt()
        {
            foreach (var name in new[] { "rock_dark", "cobble" })
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Generated + name + ".png");
                if (texture == null) Assert.Ignore("Pixel Fantasy Caves fora do git (D-65).");
                Assert.That(texture.width, Is.EqualTo(256), name);
                Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point), name);
                Assert.That(texture.wrapMode, Is.EqualTo(TextureWrapMode.Repeat), name);
            }
        }

        [UnityTest]
        public IEnumerator Supports_AreNeverBetweenTheCameraAndTheDwarf()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var stage = Object.FindAnyObjectByType<DescentStage>();
            var runner = Object.FindAnyObjectByType<MiningRunner>();
            runner.TimeScale = 20f;

            int seen = 0;
            for (int frame = 0; frame < 120; frame++)
            {
                yield return null;
                foreach (Transform child in stage.transform)
                {
                    if (!child.TryGetComponent<MeshRenderer>(out var renderer) || !renderer.enabled) continue;
                    if (child.name == "Support")
                    {
                        seen++;
                        Assert.That(child.localPosition.z, Is.GreaterThan(stage.Framing.distance), "esteio na frente do anão");
                    }
                    else if (child.name == "PostLeft" || child.name == "PostRight" || child.name == "Beam")
                        Assert.Fail("com o esteio em sprite, as caixas de madeira não aparecem");
                }
            }
            if (seen == 0) Assert.Ignore("Sem o esteio em sprite (pack fora do git, D-65).");
        }

        [UnityTest]
        public IEnumerator EdgeRocks_LineBothSidesOfThePath_NeverTheMiddle()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;

            var stage = Object.FindAnyObjectByType<DescentStage>();
            float half = stage.Framing.corridorWidth * 0.5f;
            int left = 0, right = 0;
            foreach (Transform child in stage.transform)
            {
                if (child.name != "EdgeRock" || !child.GetComponent<MeshRenderer>().enabled) continue;
                Assert.That(Mathf.Abs(child.localPosition.x), Is.GreaterThan(half - 0.5f), "na beira, não no caminho");
                if (child.localPosition.x < 0f) left++; else right++;
            }
            if (left + right == 0) Assert.Ignore("Sem a pedra da beira (fora do git).");
            Assert.That(left, Is.GreaterThan(10), "pedras do lado esquerdo");
            Assert.That(right, Is.GreaterThan(10), "pedras do lado direito");
        }
    }
}
