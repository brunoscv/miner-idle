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
    /// nunca o save de quem joga. Marco 8b, E2: o dia e as permanentes também voltam.
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
            Assert.That(simulation.Purchase(UpgradeId.Speed), Is.EqualTo(PurchaseResult.Purchased)); // linha do protótipo e do v5
            double shards = simulation.State.Shards;
            Assert.That(File.Exists(Path.Combine(_dir, FileSaveStore.FileName)), Is.True, "a compra gravou o save");

            // "Fecha e abre": cena nova, save carregado de novo.
            yield return OpenGameWithSave(r => runner = r);

            var reopened = runner.Simulation;
            Assert.That(reopened, Is.Not.SameAs(simulation));
            Assert.That(reopened.State.Shards, Is.EqualTo(shards));
            Assert.That(reopened.State.LevelOf(UpgradeId.Pickaxe), Is.EqualTo(1));
            Assert.That(reopened.State.LevelOf(UpgradeId.Speed), Is.EqualTo(1));
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

        [UnityTest]
        public IEnumerator RestAndPermanents_SurviveClosingAndReopening()
        {
            MiningRunner runner = null;
            yield return OpenGameWithSave(r => runner = r);
            runner.Simulation.SkipToCave(12);
            Assert.That(runner.Rest(), Is.Not.Null, "nível 12: dá para descansar");
            Assert.That(runner.Simulation.PurchasePermanent(PermanentId.MountainBlood), Is.EqualTo(PurchaseResult.NotEnoughShards), "7 não paga 11");
            runner.Simulation.AddRedShards(20);
            Assert.That(runner.Simulation.PurchasePermanent(PermanentId.MountainBlood), Is.EqualTo(PurchaseResult.Purchased)); // grava sozinho

            yield return OpenGameWithSave(r => runner = r);
            var permanent = runner.Simulation.Permanents.State;
            Assert.That(permanent.Day, Is.EqualTo(2));
            Assert.That(permanent.RedShards, Is.EqualTo(7 + 20 - 11));
            Assert.That(permanent.LevelOf(PermanentId.MountainBlood), Is.EqualTo(1));
            Assert.That(runner.Simulation.State.Cave, Is.EqualTo(1), "a run de amanhã");
            Assert.That(runner.Simulation.Permanents.DamageMultiplier, Is.EqualTo(1.1).Within(1e-9), "o efeito volta junto");

            runner.ResetProgress();
            yield return OpenGameWithSave(r => runner = r);
            Assert.That(runner.Simulation.Permanents.State.Day, Is.EqualTo(1), "Apagar save apaga as duas partes");
            Assert.That(runner.Simulation.Permanents.State.LevelOf(PermanentId.MountainBlood), Is.EqualTo(0));
        }
    }
}
