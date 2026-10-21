using System.Collections;
using MinersIdle.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MinersIdle.Tests
{
    /// <summary>Fumaça do Marco 1: a cena Game abre, roda alguns quadros e tem o Safe Area.</summary>
    public class GameSceneSmokeTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        [UnityTest]
        public IEnumerator GameScene_Opens_AndHasSafeArea()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;

            Assert.That(Object.FindAnyObjectByType<SafeAreaController>(), Is.Not.Null);
            Assert.That(Camera.main, Is.Not.Null);
        }
    }
}
