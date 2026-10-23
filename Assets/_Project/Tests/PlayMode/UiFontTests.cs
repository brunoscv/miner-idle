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
    /// M1b (E1, D-64): os textos do jogo usam a Pixelify Sans, que tem os acentos do português. O DBG e o BuildInfo
    /// ficam na fonte de antes (ferramenta de desenvolvimento).
    /// </summary>
    public class UiFontTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
        const string GameFontName = "PixelifySans Bitmap";
        const string Portuguese = "ÁÂÃÇÉÊÍÓÔÕÚáâãçéêíóôõú»";

        [UnityTest]
        public IEnumerator GameTexts_UseThePixelFont_WithPortugueseAccents()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            int checkedTexts = 0;
            foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (IsDevTool(text.transform)) continue;
                Assert.That(text.font.name, Is.EqualTo(GameFontName), PathOf(text.transform));
                checkedTexts++;
            }

            Assert.That(checkedTexts, Is.GreaterThan(10), "HUD, loja e efeitos");
            var font = Object.FindAnyObjectByType<MinersIdle.UI.HudView>().GetComponentInChildren<TMP_Text>().font;
            Assert.That(font.HasCharacters(Portuguese, out var missing), Is.True, $"faltam: {missing}");
        }

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
