using System.Collections;
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
    /// M1b (E2): HUD em madeira (nome em cima; ícone e valor embaixo) e a coluna da direita com LOJA, AD e AJUSTES do
    /// mesmo tamanho (D-67). Sem o kit de madeira (fora do git, D-65) a cena fica no graybox e os testes são ignorados.
    /// </summary>
    public class HudSkinTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        /// <summary>48 dp no Moto G75 ≈ 30 px da referência 270 (o mínimo de toque do Android).</summary>
        const float MinTouch = 30f;

        Transform _safeArea;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _safeArea = Object.FindAnyObjectByType<SafeAreaController>().transform;
            if (_safeArea.Find("AdButton") == null) Assert.Ignore("Sem o kit de madeira: HUD no graybox (D-65).");
        }

        [Test]
        public void HudPanels_ShowNameOnTop_AndIconBesideValue()
        {
            foreach (var name in new[] { "Lascas", "Dano", "Caverna", "Velocidade" })
            {
                var panel = _safeArea.Find("Hud/" + name);
                Assert.That(panel.GetComponent<Image>().sprite, Is.Not.Null, name);
                Assert.That(panel.Find("Row/Icon").GetComponent<Image>().sprite, Is.Not.Null, name);
                Assert.That(panel.Find("Row/Value").GetComponent<TMP_Text>(), Is.Not.Null, name);

                var label = (RectTransform)panel.Find("Label");
                var row = (RectTransform)panel.Find("Row");
                Assert.That(label.anchoredPosition.y, Is.GreaterThan(row.anchoredPosition.y), $"{name}: nome acima");
            }
        }

        [Test]
        public void SideButtons_ShareOneSize_BigEnoughToTouch_InAColumn()
        {
            var shop = (RectTransform)_safeArea.Find("Shop/ShopButton");
            var ad = (RectTransform)_safeArea.Find("AdButton");
            var settings = (RectTransform)_safeArea.Find("SettingsButton");

            Assert.That(shop.sizeDelta.y, Is.GreaterThanOrEqualTo(MinTouch));
            Assert.That(ad.sizeDelta, Is.EqualTo(shop.sizeDelta));
            Assert.That(settings.sizeDelta, Is.EqualTo(shop.sizeDelta));
            Assert.That(shop.position.y, Is.GreaterThan(ad.position.y), "LOJA em cima");
            Assert.That(ad.position.y, Is.GreaterThan(settings.position.y), "AJUSTES embaixo do AD");
        }

        [UnityTest]
        public IEnumerator ComingSoonButtons_ShowNotice_ThenHideIt()
        {
            foreach (var name in new[] { "AdButton", "SettingsButton" })
            {
                var button = _safeArea.Find(name).GetComponent<ComingSoonButton>();
                Assert.That(button.IsNoticeShown, Is.False, name);

                button.GetComponent<Button>().onClick.Invoke();
                Assert.That(button.IsNoticeShown, Is.True, $"{name}: \"Em breve\" ao tocar");

                yield return new WaitForSecondsRealtime(1.5f);
                Assert.That(button.IsNoticeShown, Is.False, $"{name}: o aviso some sozinho");
            }
        }

        [Test]
        public void ShopOpen_CoversTheComingSoonButtons()
        {
            var shopRoot = _safeArea.Find("Shop");
            Assert.That(_safeArea.Find("AdButton").GetSiblingIndex(), Is.LessThan(shopRoot.GetSiblingIndex()));
            Assert.That(_safeArea.Find("SettingsButton").GetSiblingIndex(), Is.LessThan(shopRoot.GetSiblingIndex()));
        }
    }
}
