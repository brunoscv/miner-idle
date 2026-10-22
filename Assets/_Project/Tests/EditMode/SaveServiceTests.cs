using System;
using System.IO;
using MinersIdle.Core;
using MinersIdle.Persistence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MinersIdle.Tests
{
    /// <summary>Marco 4, E3: o save guarda e devolve o progresso, e um arquivo ruim nunca trava o jogo (D-43, D-44).</summary>
    public class SaveServiceTests
    {
        string _dir;
        FileSaveStore _store;
        SaveService _save;

        static EconomyRules Prototype() => new EconomyRules(
            pickaxe: new UpgradeDefinition(10, 4, 19, 2),
            speed: new UpgradeDefinition(15, 3, 10, 0.05),
            mining: new UpgradeDefinition(10, 1.2, 100, 0.01),
            doublePickaxe: new UpgradeDefinition(500, 1, 1, 2),
            boots: new UpgradeDefinition(10, 2, 10, 0.1),
            minHitInterval: 0.5);

        static MiningRules Rules() =>
            new MiningRules(
                initialCrystals: 0, firstCrystalDistance: 3, maxCrystals: 12, spawnInterval: 1e9, minCrystalSpacing: 2, spawnDistance: 12,
                crystalHealth: 1, crystalReward: 1, damage: 1, hitInterval: 1, walkSpeed: 4, miningRange: 1);

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "MinersIdleSaveTests_" + Guid.NewGuid().ToString("N"));
            _store = new FileSaveStore(_dir);
            _save = new SaveService(_store);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        /// <summary>Um progresso de verdade: quebras, golpes e compras pela simulação.</summary>
        static GameState PlayedState()
        {
            var state = new GameState();
            var sim = new MiningSimulation(Rules(), new SeededRandom(1), state, Prototype());
            sim.SpawnCrystalAt(1);
            sim.Tick(1); // 1 golpe, 1 quebra
            sim.AddShards(1234.56);
            sim.Purchase(UpgradeId.Pickaxe);
            sim.Purchase(UpgradeId.Pickaxe);
            sim.Purchase(UpgradeId.Mining);
            sim.Purchase(UpgradeId.DoublePickaxe);
            return state;
        }

        [Test]
        public void NoFile_IsANewGame()
        {
            var state = _save.Load(Prototype(), out var status);

            Assert.That(status, Is.EqualTo(SaveLoadStatus.NewGame));
            Assert.That(state.Shards, Is.EqualTo(0));
        }

        [Test]
        public void SaveThenLoad_GivesBackTheSameProgress()
        {
            var original = PlayedState();
            _save.Save(original);

            var loaded = _save.Load(Prototype(), out var status);

            Assert.That(status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(loaded.Shards, Is.EqualTo(original.Shards));
            Assert.That(loaded.Hits, Is.EqualTo(original.Hits).And.GreaterThan(0));
            Assert.That(loaded.Breaks, Is.EqualTo(original.Breaks).And.GreaterThan(0));
            foreach (UpgradeId id in Enum.GetValues(typeof(UpgradeId)))
                Assert.That(loaded.LevelOf(id), Is.EqualTo(original.LevelOf(id)), id.ToString());
            Assert.That(loaded.LevelOf(UpgradeId.Pickaxe), Is.EqualTo(2));
        }

        [Test]
        public void Saving_ReplacesTheFileWhole_AndLeavesNoTemporary()
        {
            _save.Save(PlayedState());
            _save.Save(new GameState());

            Assert.That(_save.Load(Prototype(), out _).Shards, Is.EqualTo(0), "vale o último save");
            Assert.That(Directory.GetFiles(_dir), Is.EquivalentTo(new[] { _store.FilePath }), "sem .tmp sobrando");
        }

        [Test]
        public void LeftoverTemporary_FromAnInterruptedWrite_IsIgnored()
        {
            _save.Save(PlayedState());
            File.WriteAllText(_store.FilePath + ".tmp", "{ \"version\": 1, \"shards\": 99"); // escrita cortada no meio

            var loaded = _save.Load(Prototype(), out var status);

            Assert.That(status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(loaded.LevelOf(UpgradeId.Pickaxe), Is.EqualTo(2), "vale o save inteiro anterior");
            Assert.That(File.Exists(_store.FilePath + ".tmp"), Is.False);
        }

        [Test]
        public void CorruptedFile_StartsANewGame_AndKeepsTheBadFileAside()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_store.FilePath, "isto não é json {{{");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"\[Save\]"));

            var state = _save.Load(Prototype(), out var status);

            Assert.That(status, Is.EqualTo(SaveLoadStatus.Recovered));
            Assert.That(state.Shards, Is.EqualTo(0));
            Assert.That(File.Exists(_store.FilePath), Is.False);
            Assert.That(File.ReadAllText(_store.BadPath), Is.EqualTo("isto não é json {{{"), "guardado para análise");
        }

        [Test]
        public void UnknownVersion_StartsANewGame_AndKeepsTheFileAside()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_store.FilePath, "{ \"version\": 99, \"shards\": 500 }");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"versão desconhecida"));

            var state = _save.Load(Prototype(), out var status);

            Assert.That(status, Is.EqualTo(SaveLoadStatus.Recovered));
            Assert.That(state.Shards, Is.EqualTo(0));
            Assert.That(File.Exists(_store.BadPath), Is.True);
        }

        [Test]
        public void ValuesOutOfPlace_AreCorrected_AndUnknownLinesIgnored()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_store.FilePath,
                "{ \"version\": 1, \"shards\": -50, \"hits\": -3, \"breaks\": 7, \"levels\": [" +
                "{ \"upgrade\": \"Pickaxe\", \"level\": 999 }, { \"upgrade\": \"Speed\", \"level\": -2 }, " +
                "{ \"upgrade\": \"Telescopio\", \"level\": 4 }, { \"upgrade\": \"Boots\", \"level\": 3 } ] }");

            var state = _save.Load(Prototype(), out var status);

            Assert.That(status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(state.Shards, Is.EqualTo(0), "lascas negativas viram 0");
            Assert.That(state.Hits, Is.EqualTo(0));
            Assert.That(state.Breaks, Is.EqualTo(7));
            Assert.That(state.LevelOf(UpgradeId.Pickaxe), Is.EqualTo(19), "nível acima do limite cai para o limite");
            Assert.That(state.LevelOf(UpgradeId.Speed), Is.EqualTo(0));
            Assert.That(state.LevelOf(UpgradeId.Boots), Is.EqualTo(3));
            Assert.That(state.LevelOf(UpgradeId.Mining), Is.EqualTo(0), "linha ausente no save começa em 0");
        }

        [Test]
        public void Delete_RemovesTheSave()
        {
            _save.Save(PlayedState());
            _save.Delete();

            _save.Load(Prototype(), out var status);
            Assert.That(status, Is.EqualTo(SaveLoadStatus.NewGame));
        }
    }
}
