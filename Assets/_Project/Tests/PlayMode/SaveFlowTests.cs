using System;
using System.Collections;
using System.IO;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Persistence;
using MinersIdle.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MinersIdle.Tests
{
    /// <summary>
    /// Marco 4, E3: comprar, salvar, "fechar" (recarregar a cena) e achar o mesmo progresso. Usa uma pasta temporária,
    /// nunca o save de quem joga.
    /// </summary>
    public class SaveFlowTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
        string _dir;

        [SetUp]
        public void SetUp() => _dir = Path.Combine(Path.GetTempPath(), "MinersIdleSaveFlow_" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        IEnumerator OpenGameWithSave(Action<MiningRunner> then)
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var runner = UnityEngine.Object.FindAnyObjectByType<MiningRunner>();
            runner.UseSave(new SaveService(new FileSaveStore(_dir))); // o que o GameBootstrap faz
            then(runner);
        }

        [UnityTest]
        public IEnumerator Progress_SurvivesClosingAndReopening()
        {
            MiningRunner runner = null;
            yield return OpenGameWithSave(r => runner = r);
            Assert.That(runner.HasSave, Is.True);

            var simulation = runner.Simulation;
            simulation.AddShards(1000);
            Assert.That(simulation.Purchase(UpgradeId.Pickaxe), Is.EqualTo(PurchaseResult.Purchased)); // salva sozinho
            Assert.That(simulation.Purchase(UpgradeId.Boots), Is.EqualTo(PurchaseResult.Purchased));
            double shards = simulation.State.Shards;
            Assert.That(File.Exists(Path.Combine(_dir, FileSaveStore.FileName)), Is.True, "a compra gravou o save");

            // "Fecha e abre": cena nova, save carregado de novo.
            yield return OpenGameWithSave(r => runner = r);

            var reopened = runner.Simulation;
            Assert.That(reopened, Is.Not.SameAs(simulation));
            Assert.That(reopened.State.Shards, Is.EqualTo(shards));
            Assert.That(reopened.State.LevelOf(UpgradeId.Pickaxe), Is.EqualTo(1));
            Assert.That(reopened.State.LevelOf(UpgradeId.Boots), Is.EqualTo(1));
            Assert.That(reopened.Stats.Damage, Is.GreaterThan(reopened.Rules.Damage), "atributos recalculados do save");

            var hud = UnityEngine.Object.FindAnyObjectByType<HudView>();
            Assert.That(hud.ShardsText, Is.EqualTo(NumberFormatter.FormatShards(shards)));
            Assert.That(hud.DamageText, Is.EqualTo(NumberFormatter.Format(reopened.Stats.Damage)));
        }

        [UnityTest]
        public IEnumerator ResetProgress_DeletesTheSave_AndStartsFromZero()
        {
            MiningRunner runner = null;
            yield return OpenGameWithSave(r => runner = r);
            runner.Simulation.AddShards(100);
            runner.SaveNow();

            runner.ResetProgress();
            Assert.That(runner.Simulation.State.Shards, Is.EqualTo(0));

            yield return OpenGameWithSave(r => runner = r);
            Assert.That(runner.Simulation.State.Shards, Is.EqualTo(0), "não havia mais save para carregar");
        }
    }
}
