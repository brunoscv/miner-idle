using System.Collections;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9i E8 (D-341): a loja rápida do DBG, a proposta para o Apellon. Ligada, quatro cards (Picareta, Velocidade,
    /// Mineração, Crítico) compram como a LOJA: o mesmo custo, o mesmo nível, as mesmas lascas. Sem as lascas, o card
    /// fica apagado e não compra. Desligada, a tela é a da E7.
    /// </summary>
    public class QuickShopTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
        const string PrefsKey = "dbg.quickShop";

        MiningRunner _runner;
        QuickShopLayout _layout;
        QuickShopView _view;
        Transform _safeArea;
        int _savedPref;

        MiningSimulation Simulation => _runner.Simulation;

        [UnitySetUp]
        public IEnumerator LoadGame()
        {
            _savedPref = PlayerPrefs.GetInt(PrefsKey, 0); // a escolha do Bruno no Editor volta no fim
            PlayerPrefs.SetInt(PrefsKey, 0);
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            _layout = Object.FindAnyObjectByType<QuickShopLayout>();
            _view = Object.FindAnyObjectByType<QuickShopView>(FindObjectsInactive.Include);
            _safeArea = Object.FindAnyObjectByType<SafeAreaController>().transform;
            Assert.That(_layout, Is.Not.Null, "a cena precisa da loja rápida (rode o setup do projeto)");
            Assert.That(_view, Is.Not.Null);
        }

        [TearDown]
        public void RestorePref()
        {
            PlayerPrefs.SetInt(PrefsKey, _savedPref);
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator Off_TheScreenIsTheE7One()
        {
            Assert.That(_layout.IsOn, Is.False);
            Assert.That(_view.gameObject.activeInHierarchy, Is.False);
            var rest = (RectTransform)_safeArea.Find("Rest/RestButton");
            Assert.That(rest.sizeDelta.x, Is.EqualTo(94f), "o Descansar da E7");
            yield return null;
        }

        [UnityTest]
        public IEnumerator On_FourCardsAboveWiderButtons()
        {
            _layout.SetOn(true);
            yield return null;
            Assert.That(_view.gameObject.activeInHierarchy, Is.True);
            Assert.That(_view.Cards.Count, Is.EqualTo(4));
            var expected = new[] { UpgradeId.Pickaxe, UpgradeId.Speed, UpgradeId.Mining, UpgradeId.Critical };
            for (int i = 0; i < expected.Length; i++) Assert.That(_view.Cards[i].Upgrade, Is.EqualTo(expected[i]));

            var rest = (RectTransform)_safeArea.Find("Rest/RestButton");
            var next = (RectTransform)_safeArea.Find("NextFloor/Bar");
            Assert.That(rest.sizeDelta.x, Is.EqualTo(128f), "duas colunas de card");
            Assert.That(next.sizeDelta.x, Is.EqualTo(128f));

            // Desligar devolve tudo.
            _layout.SetOn(false);
            yield return null;
            Assert.That(rest.sizeDelta.x, Is.EqualTo(94f));
            Assert.That(_view.gameObject.activeInHierarchy, Is.False);
        }

        [UnityTest]
        public IEnumerator TheCard_BuysLikeTheShop()
        {
            _layout.SetOn(true);
            yield return null;
            var card = _view.Cards[0]; // a Picareta
            var before = Simulation.Preview(UpgradeId.Pickaxe);
            Assert.That(before.NextCost.HasValue, Is.True);
            double cost = before.NextCost.Value;

            // Sem as lascas (o jogo novo começa sem): apagado, e o toque não compra.
            if (Simulation.State.Shards < cost)
            {
                Assert.That(card.CanBuy, Is.False, "sem lascas, apagado");
                card.Button.onClick.Invoke();
                Assert.That(Simulation.Preview(UpgradeId.Pickaxe).Level, Is.EqualTo(before.Level), "apagado não compra");
            }

            // Com as lascas: acende, e o toque compra um nível pelo mesmo custo da LOJA.
            Simulation.AddShards(cost);
            yield return null;
            double shards = Simulation.State.Shards;
            Assert.That(card.CanBuy, Is.True);
            card.Button.onClick.Invoke();
            yield return null;
            var after = Simulation.Preview(UpgradeId.Pickaxe);
            Assert.That(after.Level, Is.EqualTo(before.Level + 1), "um nível");
            Assert.That(Simulation.State.Shards, Is.EqualTo(shards - cost).Within(1e-9), "o custo da LOJA");
            Assert.That(card.LevelText, Does.Contain((after.Level + 1).ToString()), "a Picareta conta do 1 (D-207)");
        }

        [UnityTest]
        public IEnumerator On_TheDebugToggleStaysOutOfTheOpenPanel()
        {
            // Bruno, 2026-11-19: acima dos cards, o botão DBG ficava por baixo do painel aberto e não fechava mais.
            _layout.SetOn(true);
            var toggle = (RectTransform)_safeArea.Find("DebugPanel/Toggle");
            var content = (RectTransform)_safeArea.Find("DebugPanel/Content");
            toggle.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That(content.gameObject.activeSelf, Is.True, "o DBG abriu");
            Assert.That(Bounds(toggle).Overlaps(Bounds(content)), Is.False, "o botão fica fora do painel aberto");
            toggle.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.That(content.gameObject.activeSelf, Is.False, "e fecha");
        }

        static Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [UnityTest]
        public IEnumerator WithTheRareWindowOpen_TheCardsStayOnTop()
        {
            _layout.SetOn(true);
            yield return null;
            Simulation.OpenRareWindow();
            yield return null;
            Assert.That(_view.IsAboveRareWindow, Is.True, "tocáveis acima do escurecido do raro (P10)");
            var rare = _safeArea.Find("RareWindow");
            var shop = _safeArea.Find("Shop");
            Assert.That(rare.GetSiblingIndex(), Is.GreaterThan(shop.GetSiblingIndex()), "o raro por cima da coluna");
        }
    }
}
