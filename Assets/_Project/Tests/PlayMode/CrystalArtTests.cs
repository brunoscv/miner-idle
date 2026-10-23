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
    /// M1b (E5, D-70): a fila usa o cristal do Bruno em 5 cores, na mesma ordem dos estilhaços da quebra
    /// (<see cref="DescentArt.CrystalColors"/>): o cristal e os estilhaços dele têm a mesma cor.
    /// </summary>
    public class CrystalArtTests
    {
        static readonly string[] Names = { "white", "gold", "blue", "purple", "red" };

        [UnityTest]
        public IEnumerator QueueCrystals_UseTheFiveColors_InTheShardOrder()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Project/Scenes/Game.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var stage = Object.FindAnyObjectByType<DescentStage>();
            Assert.That(DescentArt.CrystalColorCount, Is.EqualTo(Names.Length));
            for (int id = 0; id < 10; id++)
            {
                var texture = stage.CrystalTexture(id);
                Assert.That(texture.name, Does.Contain(Names[id % Names.Length]), $"cristal {id}");
                Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point), $"cristal {id}");
            }
        }

        [UnityTest]
        public IEnumerator EachCrystalColor_MatchesItsShardColor()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Project/Scenes/Game.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var stage = Object.FindAnyObjectByType<DescentStage>();
            for (int c = 1; c < Names.Length; c++) // o branco quase não tem matiz
            {
                // Lê o PNG do disco (no modo batch sem placa de vídeo não dá para ler a textura da GPU).
                Assert.That(stage.CrystalTexture(c).name, Does.Contain(Names[c]));
                var copy = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                copy.LoadImage(System.IO.File.ReadAllBytes($"Assets/_Project/Art/Crystals/crystal_{Names[c]}.png"));

                // Matiz médio dos pixels coloridos x matiz do estilhaço.
                float sx = 0f, sy = 0f;
                foreach (var p in copy.GetPixels32())
                {
                    if (p.a < 200) continue;
                    Color.RGBToHSV(p, out float h, out float s, out float v);
                    if (s < 0.3f || v < 0.3f) continue;
                    sx += Mathf.Cos(h * 2f * Mathf.PI);
                    sy += Mathf.Sin(h * 2f * Mathf.PI);
                }
                float hue = Mathf.Repeat(Mathf.Atan2(sy, sx) / (2f * Mathf.PI), 1f);
                Color.RGBToHSV(DescentArt.CrystalColors[c, 0], out float shardHue, out _, out _);
                float diff = Mathf.Abs(Mathf.DeltaAngle(hue * 360f, shardHue * 360f));
                Assert.That(diff, Is.LessThan(15f), $"{Names[c]}: cristal {hue * 360f:0}°, estilhaço {shardHue * 360f:0}°");
                Object.Destroy(copy);
            }
        }
    }
}
