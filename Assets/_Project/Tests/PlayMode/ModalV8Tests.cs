using System.Collections;
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
    /// M9i E3 (D-336), na cena Game de verdade: toda janela de madeira tem a barra do título de 20 px, o título em 11 px
    /// dentro dela, e o X de 24 px com área de toque de 32 px. LOJA, Boost e Descansar ganharam o X, que fecha a tela
    /// como o botão de baixo.
    /// </summary>
    public class ModalV8Tests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        MiningRunner _runner;
        Transform _safeArea;

        [UnitySetUp]
        public IEnumerator LoadGame()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            _safeArea = Object.FindAnyObjectByType<SafeAreaController>().transform;
        }

        [Test]
        public void EveryWoodenWindow_HasTheTallTitleBar_AndTheBiggerTitle()
        {
            int windows = 0;
            foreach (var image in _safeArea.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite == null) continue;
                Assert.That(image.sprite.name, Is.Not.EqualTo("window"), image.transform.parent.name + ": janela com a barra velha");
                if (image.sprite.name != "window_title") continue;
                windows++;
                Assert.That(image.sprite.border.w, Is.EqualTo(28f), "a faixa de 20 px fica inteira na borda de cima do 9-slice");
                var title = image.transform.Find("Title")?.GetComponent<TMP_Text>();
                if (title == null) continue;
                Assert.That(title.fontSize, Is.EqualTo(11f), image.transform.parent.name + ": o título em 11 px");
                var rect = title.rectTransform;
                float top = rect.anchoredPosition.y + (1f - rect.pivot.y) * rect.sizeDelta.y;
                Assert.That(top, Is.EqualTo(-6f).Within(0.01f), "o título começa na faixa");
                Assert.That(rect.sizeDelta.y, Is.EqualTo(20f).Within(0.01f), "e tem a altura dela");
            }
            Assert.That(windows, Is.GreaterThanOrEqualTo(8), "LOJA, AJUSTES (com a confirmação e os créditos), Boost, anúncio, offline, descanso e raro");
        }

        [UnityTest]
        public IEnumerator TheShopX_ClosesTheShop()
        {
            var shop = Object.FindAnyObjectByType<ShopView>(FindObjectsInactive.Include);
            shop.Open();
            yield return null;
            Click(shop.transform);
            Assert.That(shop.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator TheBoostX_ClosesTheBoost()
        {
            var boost = Object.FindAnyObjectByType<BoostScreen>(FindObjectsInactive.Include);
            boost.Open();
            yield return null;
            Assert.That(boost.IsOpen, Is.True);
            Click(boost.transform);
            Assert.That(boost.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator TheRestX_ClosesTheRest()
        {
            _runner.Simulation.SkipToCave(_runner.Simulation.RestLevel);
            var rest = Object.FindAnyObjectByType<RestScreen>(FindObjectsInactive.Include);
            rest.Open();
            yield return null;
            Assert.That(rest.IsOpen, Is.True, "o descanso abre no andar dele");
            Click(rest.transform);
            Assert.That(rest.IsOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator TheSettingsX_StillClosesTheSettings()
        {
            var settings = Object.FindAnyObjectByType<SettingsScreen>(FindObjectsInactive.Include);
            settings.Open();
            yield return null;
            Click(settings.transform);
            Assert.That(settings.IsOpen, Is.False);
        }

        /// <summary>Acha o X aberto da tela (32×32, o desenho de 24 no meio) e toca nele.</summary>
        static void Click(Transform screenRoot)
        {
            Button x = null;
            foreach (var button in screenRoot.GetComponentsInChildren<Button>())
            {
                var icon = button.transform.Find("Icon")?.GetComponent<Image>();
                if (icon == null || icon.sprite == null || icon.sprite.name != "icon_close") continue;
                x = button;
            }
            Assert.That(x, Is.Not.Null, screenRoot.name + ": o X");
            var rect = (RectTransform)x.transform;
            Assert.That(rect.sizeDelta, Is.EqualTo(new Vector2(32f, 32f)), "a área de toque");
            Assert.That(((RectTransform)x.transform.Find("Icon")).sizeDelta, Is.EqualTo(new Vector2(24f, 24f)), "o desenho");
            x.onClick.Invoke();
        }
    }
}
