using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M1b (E1): os sprites de UI em pixel art são importados nítidos (filtro Point, sem compressão, sem mipmap) e com
    /// 1 px do sprite = 1 px da referência (PPU 100). Os packs de terceiros ficam fora do git (D-65): sem eles, o teste do
    /// pack é ignorado, como na CI.
    /// </summary>
    public class UiSpriteImportTests
    {
        const string Wood = "Assets/_Vendor/AssetStore/PixelUIKit/themes/wood/png";
        const string Fantasy = "Assets/_Vendor/AssetStore/FantasyUIKitLite/x1";
        const string Icons = "Assets/_Vendor/AssetStore/AnvilRpgIcons";
        const string ProjectIcons = "Assets/_Project/Art/UI";

        [TestCase(Wood)]
        [TestCase(Fantasy)]
        [TestCase(Icons)]
        [TestCase(ProjectIcons)]
        public void UiSprites_ArePixelArtSprites(string folder)
        {
            if (!Directory.Exists(folder)) Assert.Ignore($"{folder} não existe (pack fora do git, D-65).");

            var files = Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories);
            Assert.That(files, Is.Not.Empty);
            foreach (var file in files)
            {
                var path = file.Replace('\\', '/');
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point), path);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed), path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(100f), path);
            }
        }

        [Test]
        public void WoodPanel_HasNineSliceBorderFromTheAtlas()
        {
            var path = Wood + "/panels/panel.png";
            if (!File.Exists(path)) Assert.Ignore("Kit de madeira fora do git (D-65).");

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Assert.That(sprite.border, Is.EqualTo(new Vector4(8, 8, 8, 8)));
            var window = AssetDatabase.LoadAssetAtPath<Sprite>(Wood + "/panels/window.png");
            Assert.That(window.border, Is.EqualTo(new Vector4(8, 8, 8, 16)), "a janela tem barra de título em cima");
        }
    }
}
