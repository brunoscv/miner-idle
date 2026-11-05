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

        /// <summary>
        /// D-148: com o anão recuado (80%, 9 m), os esteios continuam perto da câmera, a partir dos mesmos 5,6 m da F1, e
        /// também na frente do anão, para o começo da caverna não ficar sem arco.
        /// </summary>
        [UnityTest]
        public IEnumerator Supports_WithTheDwarfSetBack_StayNearTheCamera()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var stage = Object.FindAnyObjectByType<DescentStage>();
            var runner = Object.FindAnyObjectByType<MiningRunner>();
            var framing = stage.Framing;
            float nearest = stage.NearestSupport;
            framing.distance = 9f;
            stage.Framing = framing;
            Assert.That(stage.NearestSupport, Is.EqualTo(nearest), "o limite dos esteios não anda com o anão");
            runner.TimeScale = 20f;

            int seen = 0, inFrontOfTheDwarf = 0;
            for (int frame = 0; frame < 120; frame++)
            {
                yield return null;
                foreach (Transform child in stage.transform)
                {
                    if (child.name != "Support" || !child.TryGetComponent<MeshRenderer>(out var renderer) || !renderer.enabled) continue;
                    seen++;
                    Assert.That(child.localPosition.z, Is.GreaterThanOrEqualTo(nearest - 1e-4f), "nenhum esteio colado na câmera");
                    if (child.localPosition.z < framing.distance) inFrontOfTheDwarf++;
                }
            }
            if (seen == 0) Assert.Ignore("Sem o esteio em sprite (pack fora do git, D-65).");
            Assert.That(inFrontOfTheDwarf, Is.GreaterThan(0), "esteios entre a câmera e o anão recuado");
        }

        [UnityTest]
        public IEnumerator EdgeRocks_LineBothSidesOfThePath_NeverTheMiddle()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;

            var stage = Object.FindAnyObjectByType<DescentStage>();
            // Bioma sem pedras (o 1, com a faixa de tiles, D-145): passa para o próximo que tem.
            for (int i = 0; i < 10 && stage.Biome != null && !stage.Biome.EdgeRocks; i++) stage.NextBiome();
            yield return null;
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

        /// <summary>
        /// Esteio baixo (D-117): o jogo abre no de 3,0 m; o botão do DBG passa pelas alturas; nas baixas, o esteio fica
        /// abaixo da câmera (a viga pode aparecer) e cada um tem o seu lampião solto, num tamanho fixo, no poste da
        /// esquerda, nunca na frente do anão.
        /// </summary>
        [UnityTest]
        public IEnumerator ShortSupports_StayBelowTheCamera_WithASmallLooseLamp()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var stage = Object.FindAnyObjectByType<DescentStage>();
            if (stage.SupportLooks < 2) Assert.Ignore("Sem as opções do esteio (rode o setup).");
            Assert.That(stage.SupportHeight, Is.EqualTo(3f).Within(0.05f), "o jogo abre no esteio de 3,0 m (D-117)");
            var runner = Object.FindAnyObjectByType<MiningRunner>();
            runner.TimeScale = 20f;

            for (int look = 1; look < stage.SupportLooks; look++)
            {
                stage.SupportLook = look;
                Assert.That(stage.SupportHeight, Is.LessThan(stage.Framing.height), $"opção {look}: o esteio abaixo da câmera");

                int lamps = 0;
                for (int frame = 0; frame < 60; frame++)
                {
                    yield return null;
                    int supports = 0, lampsNow = 0;
                    foreach (Transform child in stage.transform)
                    {
                        if (!child.TryGetComponent<MeshRenderer>(out var renderer) || !renderer.enabled) continue;
                        if (child.name == "Support")
                        {
                            supports++;
                            Assert.That(child.localScale.y, Is.EqualTo(stage.SupportHeight).Within(0.001f), $"opção {look}: altura");
                        }
                        else if (child.name == "Lantern")
                        {
                            lamps++;
                            lampsNow++;
                            Assert.That(child.localScale.y, Is.LessThan(0.6f), $"opção {look}: lampião pequeno");
                            Assert.That(child.localPosition.x, Is.LessThan(0f), $"opção {look}: no poste da esquerda");
                            Assert.That(child.localPosition.y + child.localScale.y, Is.LessThan(stage.SupportHeight),
                                        $"opção {look}: abaixo da viga");
                            Assert.That(child.localPosition.z, Is.GreaterThan(stage.Framing.distance), $"opção {look}: atrás do anão");
                        }
                    }
                    Assert.That(lampsNow, Is.EqualTo(supports), $"opção {look}: um lampião em cada esteio (D-117)");
                }
                Assert.That(lamps, Is.GreaterThan(0), $"opção {look}: algum lampião aparece na caverna 1");
            }

            stage.SupportLook = stage.SupportLooks; // passa do fim: fica na última
            Assert.That(stage.SupportLook, Is.EqualTo(stage.SupportLooks - 1));
        }
    }
}
