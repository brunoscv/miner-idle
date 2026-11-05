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
    /// Câmera → Bioma passa pelas 10 (cada uma com o seu chão e a sua neblina, com o nome no botão) e volta à 1. Na D-145
    /// o bioma 1 tem o chão em faixa (ground_01a…e, de parede a parede); os outros, o chão gerado de antes.
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
            Assert.That(stage.FloorTexture.name, Is.EqualTo(ExpectedFloor(stage.Biome, 1)), "o jogo abre com o chão da caverna 1");

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
                Assert.That(stage.FloorTexture.name, Is.EqualTo(ExpectedFloor(stage.Biome, cave)), "o botão passa para a próxima caverna");
                Assert.That(stage.SceneCamera.backgroundColor, Is.Not.EqualTo(before), "a neblina muda com a caverna");
                Assert.That(label.text, Is.EqualTo(stage.Biome.DisplayName), "o botão mostra o nome da caverna");
            }

            button.GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(stage.Biome, Is.SameAs(first), "depois da caverna 10 volta à 1");
            Assert.That(stage.FloorTexture.name, Is.EqualTo(ExpectedFloor(stage.Biome, 1)));
        }

        /// <summary>A faixa de tiles quando o bioma tem os 5 (D-145); senão o chão gerado da caverna.</summary>
        static string ExpectedFloor(BiomeDefinition biome, int cave) =>
            biome.HasFloorTiles ? DescentArt.FloorStripName(biome.FloorTiles) : $"floor_cave_{cave:00}";

        [UnityTest]
        public IEnumerator Biome1_FloorIsTileStrip_FromWallToWall()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Project/Scenes/Game.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var stage = Object.FindAnyObjectByType<DescentStage>();
            if (stage.Biome == null || !stage.Biome.HasFloorTiles) Assert.Ignore("Sem a faixa ground_01 no bioma 1 (rode o setup).");

            var tiles = stage.Biome.FloorTiles;
            var floor = (Texture2D)stage.FloorTexture;
            int size = tiles[0].width;
            int columns = floor.width / size;
            Assert.That(floor.name, Is.EqualTo("floor_ground_01"));
            Assert.That(stage.Biome.EdgeRocks, Is.False, "sem pedras por cima da beira da faixa");
            for (int i = 0; i < 5; i++) yield return null; // a beira é posta quadro a quadro
            foreach (Transform child in stage.transform)
                if (child.name == "EdgeRock" || child.name == "Pebble")
                    Assert.That(child.GetComponent<MeshRenderer>().enabled, Is.False, "nenhuma pedra na beira");
            Assert.That(floor.width % size, Is.Zero, "a faixa tem um número inteiro de tiles");
            Assert.That(floor.wrapModeU, Is.EqualTo(TextureWrapMode.Clamp), "na largura não repete: as beiras ficam nas paredes");
            Assert.That(floor.wrapModeV, Is.EqualTo(TextureWrapMode.Repeat), "ao longo do corredor repete");

            // Beira esquerda, transição, miolo…, transição, beira direita: o canto de cada coluna é o do tile certo.
            for (int c = 0; c < columns; c++)
            {
                var tile = tiles[DescentArt.FloorStripTileAt(c, columns)];
                Assert.That((Color)floor.GetPixel(c * size, 0), Is.EqualTo((Color)tile.GetPixel(0, 0)), $"coluna {c}");
                Assert.That((Color)floor.GetPixel(c * size + size - 1, size - 1), Is.EqualTo((Color)tile.GetPixel(size - 1, size - 1)), $"coluna {c}");
            }
        }

        /// <summary>
        /// D-146: as 3 opções do DBG no bioma 1. Arco (∩): o alto, com a altura do montinho, fica no meio da beira e o chão
        /// volta ao nível junto da parede. Platô: o mesmo alto, mas reto por um trecho (vários pontos na altura toda). Chão
        /// plano: nada sobe. O meio do corredor é sempre plano, e o bioma 2 não tem montinho.
        /// </summary>
        [UnityTest]
        public IEnumerator Biome1_MoundOptions_FlatArchAndPlateau()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Project/Scenes/Game.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var stage = Object.FindAnyObjectByType<DescentStage>();
            if (stage.Biome == null || !stage.Biome.EdgeMound) Assert.Ignore("Sem o montinho no bioma 1 (rode o setup).");
            var corridor = stage.transform.Find("Corridor").GetComponent<MeshFilter>();
            float half = stage.Framing.corridorWidth * 0.5f;
            Assert.That(stage.MoundLook, Is.EqualTo(1), "o jogo abre no arco");

            stage.MoundLook = 1;
            yield return null;
            var arch = FloorHeights(corridor.sharedMesh, half);
            float height = stage.MoundHeight;
            Assert.That(height, Is.GreaterThan(0f));
            Assert.That(arch.peak, Is.EqualTo(height).Within(1e-4f), "o alto do ∩ tem a altura do montinho");
            Assert.That(arch.wall, Is.EqualTo(0f).Within(1e-4f), "no ∩ o chão desce de novo até a parede");
            Assert.That(arch.middle, Is.EqualTo(0f).Within(1e-4f), "o meio do corredor continua plano");

            stage.MoundLook = 2;
            yield return null;
            var plateau = FloorHeights(corridor.sharedMesh, half);
            Assert.That(plateau.peak, Is.EqualTo(height).Within(1e-4f), "o topo do platô tem a altura do montinho");
            Assert.That(plateau.wall, Is.EqualTo(0f).Within(1e-4f), "o platô também desce até a parede");
            Assert.That(plateau.middle, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(plateau.atTop, Is.GreaterThan(arch.atTop + 4), "o topo do platô é achatado: muitos pontos lá em cima");

            stage.MoundLook = 0;
            yield return null;
            Assert.That(FloorHeights(corridor.sharedMesh, half).peak, Is.EqualTo(0f).Within(1e-4f), "chão plano");

            stage.MoundLook = 1;
            stage.NextBiome();
            yield return null;
            if (!stage.Biome.EdgeMound)
                Assert.That(FloorHeights(corridor.sharedMesh, half).peak, Is.EqualTo(0f).Within(1e-4f), "bioma sem montinho, plano");
        }

        /// <summary>
        /// A maior altura do chão junto da parede, a do vértice mais perto do meio (o pé do montinho: dali para dentro o chão
        /// é uma faixa plana só), a maior de todas e quantos pontos do chão estão nela.
        /// </summary>
        static (float wall, float middle, float peak, int atTop) FloorHeights(Mesh mesh, float half)
        {
            var vertices = mesh.vertices;
            var floor = new System.Collections.Generic.HashSet<int>(mesh.GetTriangles(DescentMeshes.FloorSubmesh));
            float wall = 0f, middle = 0f, peak = 0f, nearest = float.MaxValue;
            foreach (int i in floor)
            {
                var v = vertices[i];
                peak = Mathf.Max(peak, v.y);
                if (Mathf.Abs(v.x) > half - 0.01f) wall = Mathf.Max(wall, v.y);
                if (Mathf.Abs(v.x) < nearest)
                {
                    nearest = Mathf.Abs(v.x);
                    middle = v.y;
                }
            }
            int atTop = 0;
            foreach (int i in floor)
                if (peak > 0f && vertices[i].y > peak - 1e-4f) atTop++;
            return (wall, middle, peak, atTop);
        }

        [TestCase(4, new[] { 0, 1, 3, 4 })]
        [TestCase(6, new[] { 0, 1, 2, 2, 3, 4 })]
        [TestCase(8, new[] { 0, 1, 2, 2, 2, 2, 3, 4 })]
        public void FloorStrip_EdgesTransitionsAndRepeatedMiddle(int columns, int[] expected)
        {
            var order = new int[columns];
            for (int c = 0; c < columns; c++) order[c] = DescentArt.FloorStripTileAt(c, columns);
            Assert.That(order, Is.EqualTo(expected));
        }
    }
}
