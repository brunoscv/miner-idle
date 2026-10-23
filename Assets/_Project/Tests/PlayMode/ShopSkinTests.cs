using System.Collections;
using System.Linq;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.UI;
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
    /// M1b (E3): a loja em madeira. Cada linha tem ícone e moldura do catálogo; o botão comprar fica rubi quando dá e
    /// cinza quando não dá. Sem os packs (fora do git, D-65) a loja fica no graybox e os testes são ignorados.
    /// </summary>
    public class ShopSkinTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        ShopView _shop;
        MiningRunner _runner;

        [UnitySetUp]
        public IEnumerator OpenShop()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            _shop = Object.FindAnyObjectByType<ShopView>();
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            if (_shop.Rows[0].transform.Find("Frame") == null) Assert.Ignore("Sem os packs: loja no graybox (D-65).");
            _shop.Open();
        }

        [Test]
        public void EveryRow_HasItsIcon_InsideAColoredFrame()
        {
            foreach (var row in _shop.Rows)
            {
                var icon = row.transform.Find("Icon").GetComponent<Image>();
                var frame = row.transform.Find("Frame").GetComponent<Image>();
                Assert.That(icon.sprite, Is.Not.Null, row.name);
                Assert.That(frame.enabled && frame.sprite != null, Is.True, row.name);
            }

            var frames = _shop.Rows.Where(r => !r.IsLocked)
                              .Select(r => r.transform.Find("Frame").GetComponent<Image>().sprite).Distinct();
            Assert.That(frames.Count(), Is.EqualTo(5), "uma cor de raridade por melhoria");
        }

        [UnityTest]
        public IEnumerator BuyButton_IsRuby_WhenAffordable_AndGrey_WhenNot()
        {
            var pickaxe = _shop.Rows.First(r => !r.IsLocked && r.Upgrade == UpgradeId.Pickaxe);
            var buy = pickaxe.transform.Find("Buy");
            var image = buy.GetComponent<Image>();
            var button = buy.GetComponent<Button>();
            var label = buy.GetComponentInChildren<TMP_Text>();

            Assert.That(pickaxe.CanBuy, Is.False, "sem lascas");
            Assert.That(image.overrideSprite, Is.EqualTo(button.spriteState.disabledSprite), "cinza");
            var greyLabel = label.color;

            _runner.Simulation.AddShards(1000);
            yield return null;

            Assert.That(pickaxe.CanBuy, Is.True);
            Assert.That(image.overrideSprite, Is.EqualTo(image.sprite), "rubi (o sprite normal)");
            Assert.That(image.sprite.name, Does.Contain("ruby"));
            Assert.That(label.color, Is.Not.EqualTo(greyLabel), "o preço muda de cor junto");
        }

        [Test]
        public void ShopWindow_UsesTheWoodWindow_AndBackIsBigEnoughToTouch()
        {
            var panel = _shop.transform.Find("Screen/Panel");
            Assert.That(panel.GetComponent<Image>().sprite.name, Is.EqualTo("window"));
            Assert.That(((RectTransform)panel.Find("Back")).sizeDelta.y, Is.GreaterThanOrEqualTo(30f));
        }
    }
}
