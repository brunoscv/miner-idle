using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M1b (E1, D-64): os textos do jogo usam a Pixelify Sans, que tem os acentos do português. Os valores (número do
    /// HUD, saldo e preço da loja) usam a Press Start 2P (D-104), mais legível nos algarismos. O DBG e o BuildInfo ficam
    /// na fonte de antes (ferramenta de desenvolvimento).
    /// </summary>
    public class UiFontTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
        const string GameFontName = "PixelifySans Bitmap";
        const string NumbersFontName = "PressStart2P Bitmap";
        const string Portuguese = "ÁÂÃÇÉÊÍÓÔÕÚáâãçéêíóôõú»";

        [UnityTest]
        public IEnumerator GameTexts_UseThePixelFont_WithPortugueseAccents()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            int checkedTexts = 0, numberTexts = 0;
            TMP_FontAsset gameFont = null, numbersFont = null;
            foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (IsDevTool(text.transform)) continue;
                bool isNumber = IsNumberText(text.transform);
                Assert.That(text.font.name, Is.EqualTo(isNumber ? NumbersFontName : GameFontName), PathOf(text.transform));
                if (isNumber)
                {
                    numbersFont = text.font;
                    numberTexts++;
                }
                else gameFont = text.font;
                checkedTexts++;
            }

            Assert.That(checkedTexts, Is.GreaterThan(10), "HUD, loja e efeitos");
            Assert.That(numberTexts, Is.GreaterThanOrEqualTo(4 + 1 + 4), "4 valores do HUD, o saldo e o preço de cada linha");
            Assert.That(gameFont.HasCharacters(Portuguese, out var missing), Is.True, $"faltam: {missing}");
            Assert.That(numbersFont.HasCharacters("0123456789.,s-" + Portuguese, out missing), Is.True, $"faltam: {missing}");
        }

        /// <summary>A mesma regra do setup (UiSkinSetup.IsNumberText): "Value" do HUD, "Balance" da loja e "Buy/Label".</summary>
        static bool IsNumberText(Transform node) =>
            node.name == "Value" || node.name == "Balance" ||
            (node.name == "Label" && node.parent != null && node.parent.name == "Buy");

        static bool IsDevTool(Transform node)
        {
            for (; node != null; node = node.parent)
                if (node.name == "DebugPanel" || node.name == "BuildInfo") return true;
            return false;
        }

        static string PathOf(Transform node) =>
            node.parent == null ? node.name : PathOf(node.parent) + "/" + node.name;
    }
}
