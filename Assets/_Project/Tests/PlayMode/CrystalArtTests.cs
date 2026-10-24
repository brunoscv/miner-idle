using System.Collections;
using MinersIdle.Presentation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M1b (E5, D-77): a fila reveza os cristais desenhados pelo Bruno (glass, sapphire, yellow), e os estilhaços de cada
    /// um saem na cor dele.
    /// </summary>
    public class CrystalArtTests
    {
        static readonly string[] Names = { "crystal_glass", "crystal_sapphire", "crystal_yellow" };

        [UnityTest]
        public IEnumerator QueueCrystals_RotateBrunosCrystals()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Project/Scenes/Game.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var stage = Object.FindAnyObjectByType<DescentStage>();
            for (int id = 0; id < 9; id++)
            {
                var texture = stage.CrystalTexture(id);
                Assert.That(texture.name, Is.EqualTo(Names[id % Names.Length]), $"cristal {id}");
                Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point), $"cristal {id}");
            }
        }

        [UnityTest]
        public IEnumerator EachCrystal_BreaksIntoShardsOfItsOwnColor()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Project/Scenes/Game.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var stage = Object.FindAnyObjectByType<DescentStage>();
            for (int c = 1; c < Names.Length; c++) // o vidro quase não tem matiz
            {
                // Lê o PNG do disco (no modo batch sem placa de vídeo não dá para ler a textura da GPU).
                var copy = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                copy.LoadImage(System.IO.File.ReadAllBytes($"Assets/_Project/Art/Crystals/{Names[c]}.png"));

                // Matiz médio dos pixels coloridos do cristal (a base de terra, embaixo, fica de fora).
                float sx = 0f, sy = 0f;
                var pixels = copy.GetPixels32();
                for (int i = copy.width * 20; i < pixels.Length; i++)
                {
                    var p = pixels[i];
                    if (p.a < 200) continue;
                    Color.RGBToHSV(p, out float h, out float s, out float v);
                    if (s < 0.3f || v < 0.45f) continue;
                    sx += Mathf.Cos(h * 2f * Mathf.PI);
                    sy += Mathf.Sin(h * 2f * Mathf.PI);
                }
                float hue = Mathf.Repeat(Mathf.Atan2(sy, sx) / (2f * Mathf.PI), 1f);
                Color.RGBToHSV(stage.CrystalShardColor(c), out float shardHue, out _, out _);
                float diff = Mathf.Abs(Mathf.DeltaAngle(hue * 360f, shardHue * 360f));
                Assert.That(diff, Is.LessThan(20f), $"{Names[c]}: cristal {hue * 360f:0}°, estilhaço {shardHue * 360f:0}°");
                Object.Destroy(copy);
            }
        }
    }
}
