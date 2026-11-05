using System;
using System.IO;
using MinersIdle.Core;
using MinersIdle.Persistence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MinersIdle.Tests
{
    /// <summary>
    /// Marco 4, E3: o save guarda e devolve o progresso, e um arquivo ruim nunca trava o jogo (D-43, D-44). Marco 8, E3: a
    /// versão 2 guarda a descida (caverna, quebras nela, tempo de jogo, lascas ganhas, coração da montanha) e a versão 1
    /// abre migrada. Marco 8b, E2: a versão 4 guarda também a parte permanente (banco, dia e níveis das permanentes); até
    /// a 3, ela abre zerada.
    /// </summary>
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

        const int Caves = 10;

        /// <summary>O balanço com a descida: só o Letrado à venda (GDD v5).</summary>
        static EconomyRules WithLetrado()
        {
            var off = new UpgradeDefinition(0, 1, 0, 0);
            return new EconomyRules(off, off, off, off, off, 0.1, letrado: new UpgradeDefinition(100, 10, 10, 2));
        }

        static MiningRules CaveRules() =>
            new MiningRules(
                initialCrystals: 4, firstCrystalDistance: 3, maxCrystals: 12, spawnInterval: 1e9, minCrystalSpacing: 2, spawnDistance: 12,
                crystalHealth: 10, crystalReward: 1, damage: 100, hitInterval: 1, walkSpeed: 4, miningRange: 1,
                minCrystals: 4, caveCount: Caves, caveHealthGrowth: 8, caveRewardGrowth: 3, letradoBreaks: 100);

        void WriteSave(string json)
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_store.FilePath, json);
        }

        [Test]
        public void CurrentVersion_KeepsTheDescent()
        {
            var state = new GameState();
            var sim = new MiningSimulation(CaveRules(), new SeededRandom(1), state, WithLetrado());
            sim.Tick(20); // quebras, lascas e tempo de jogo
            sim.SkipToCave(4);
            sim.Tick(5);
            _save.Save(state);

            var loaded = _save.Load(WithLetrado(), out var status, Caves);

            Assert.That(status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(loaded.Cave, Is.EqualTo(4));
            Assert.That(loaded.CaveBreaks, Is.EqualTo(state.CaveBreaks).And.GreaterThan(0));
            Assert.That(loaded.LevelOf(UpgradeId.Letrado), Is.EqualTo(3));
            Assert.That(loaded.PlaySeconds, Is.EqualTo(25).Within(1e-9));
            Assert.That(loaded.ShardsEarned, Is.EqualTo(state.ShardsEarned).And.GreaterThan(0));
            Assert.That(loaded.ReachedHeart, Is.False);
            Assert.That(File.ReadAllText(_store.FilePath), Does.Match(@"""version"":\s*4"), "save v4 (Marco 8b, E2)");
        }

        [Test]
        public void Version1_OpensInCave1_WithTheCountFromZero()
        {
            // O save do Moto G75 em 2026-10-31 (achado da E2): o Letrado 1 gravado, a caverna não.
            WriteSave("{ \"version\": 1, \"shards\": 32.0, \"hits\": 78, \"breaks\": 114, \"levels\": [" +
                      "{ \"upgrade\": \"Pickaxe\", \"level\": 0 }, { \"upgrade\": \"Letrado\", \"level\": 1 } ] }");

            var state = _save.Load(WithLetrado(), out var status, Caves);

            Assert.That(status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(state.Shards, Is.EqualTo(32));
            Assert.That(state.Breaks, Is.EqualTo(114));
            Assert.That(state.Cave, Is.EqualTo(1));
            Assert.That(state.CaveBreaks, Is.EqualTo(0));
            Assert.That(state.LevelOf(UpgradeId.Letrado), Is.EqualTo(1), "comprado, a parede por quebrar");
            Assert.That(state.ShardsEarned, Is.EqualTo(32), "o que se sabe das lascas ganhas");
            var sim = new MiningSimulation(CaveRules(), new SeededRandom(1), state, WithLetrado());
            Assert.That(sim.CanPassCave, Is.True, "o botão de passar volta");

            _save.Save(state);
            Assert.That(File.ReadAllText(_store.FilePath), Does.Match(@"""version"":\s*4"), "regravado na versão nova");
        }

        /// <summary>
        /// E3b-2 do M8 (D-129): as 10 cavernas do save v2 viram os 10 primeiros dos 100 níveis do GDD v7. O nível e o Mapa
        /// (o Letrado) ficam como estavam; o coração da caverna 10 não vale mais (ele está no nível 100).
        /// </summary>
        [Test]
        public void Version2_OpensAtTheSameLevel_OfTheHundred()
        {
            var off = new UpgradeDefinition(0, 1, 0, 0);
            var map = new EconomyRules(off, off, off, off, off, 0.1, letrado: new UpgradeDefinition(0, 1, 100, 1));
            WriteSave("{ \"version\": 2, \"shards\": 50.0, \"cave\": 7, \"caveBreaks\": 40, \"playSeconds\": 600, " +
                      "\"shardsEarned\": 900, \"levels\": [ { \"upgrade\": \"Letrado\", \"level\": 7 } ] }");

            var state = _save.Load(map, out var status, 100);

            Assert.That(status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(state.Cave, Is.EqualTo(7));
            Assert.That(state.CaveBreaks, Is.EqualTo(40));
            Assert.That(state.LevelOf(UpgradeId.Letrado), Is.EqualTo(7), "o Mapa do nível 7 comprado: falta passar");
            Assert.That(state.PlaySeconds, Is.EqualTo(600));
            Assert.That(state.ShardsEarned, Is.EqualTo(900));

            WriteSave("{ \"version\": 2, \"cave\": 10, \"reachedHeart\": true, " +
                      "\"levels\": [ { \"upgrade\": \"Letrado\", \"level\": 10 } ] }");
            var end = _save.Load(map, out _, 100);
            Assert.That(end.Cave, Is.EqualTo(10));
            Assert.That(end.ReachedHeart, Is.False, "o coração passou para o nível 100");

            _save.Save(end);
            Assert.That(File.ReadAllText(_store.FilePath), Does.Match(@"""version"":\s*4"));
        }

        [Test]
        public void TheDescentOutOfPlace_IsCorrected()
        {
            WriteSave("{ \"version\": 2, \"cave\": 15, \"caveBreaks\": -4, \"playSeconds\": -1, \"shardsEarned\": -9, " +
                      "\"levels\": [ { \"upgrade\": \"Letrado\", \"level\": 3 } ] }");
            var deep = _save.Load(WithLetrado(), out _, Caves);
            Assert.That(deep.Cave, Is.EqualTo(10), "caverna 15 vira a última");
            Assert.That(deep.CaveBreaks, Is.EqualTo(0));
            Assert.That(deep.PlaySeconds, Is.EqualTo(0));
            Assert.That(deep.ShardsEarned, Is.EqualTo(0));
            Assert.That(deep.LevelOf(UpgradeId.Letrado), Is.EqualTo(9), "na caverna 10, ao menos o Letrado que levou até ela");

            WriteSave("{ \"version\": 2, \"cave\": 0, \"reachedHeart\": true, " +
                      "\"levels\": [ { \"upgrade\": \"Letrado\", \"level\": 7 } ] }");
            var shallow = _save.Load(WithLetrado(), out _, Caves);
            Assert.That(shallow.Cave, Is.EqualTo(1), "caverna 0 vira 1");
            Assert.That(shallow.LevelOf(UpgradeId.Letrado), Is.EqualTo(1), "não mais Letrados do que a caverna abre");
            Assert.That(shallow.ReachedHeart, Is.False, "o coração só na última caverna");

            var noDescent = _save.Load(Prototype(), out _); // o protótipo tem uma caverna só
            Assert.That(noDescent.Cave, Is.EqualTo(1));
        }

        [Test]
        public void AStandingWall_IsNotSaved_AndTheHeartIsKept()
        {
            var state = new GameState();
            var sim = new MiningSimulation(CaveRules(), new SeededRandom(1), state, WithLetrado());
            sim.AddShards(1e6);
            sim.FillCaveBreaks();
            sim.Purchase(UpgradeId.Letrado);
            Assume.That(sim.PassCave(), Is.True);
            _save.Save(state);

            var reopened = new MiningSimulation(CaveRules(), new SeededRandom(1), _save.Load(WithLetrado(), out _, Caves), WithLetrado());
            Assert.That(reopened.Wall, Is.Null, "a parede de pé não volta");
            Assert.That(reopened.State.Cave, Is.EqualTo(1));
            Assert.That(reopened.CanPassCave, Is.True, "o botão de passar volta");

            WriteSave("{ \"version\": 2, \"cave\": 10, \"reachedHeart\": true, " +
                      "\"levels\": [ { \"upgrade\": \"Letrado\", \"level\": 10 } ] }");
            int hearts = 0;
            var end = new MiningSimulation(CaveRules(), new SeededRandom(1), _save.Load(WithLetrado(), out _, Caves), WithLetrado());
            end.HeartReached += () => hearts++;
            end.Tick(10);
            Assert.That(end.State.ReachedHeart, Is.True);
            Assert.That(end.CanPassCave, Is.False, "depois do coração, não há passagem");
            Assert.That(hearts, Is.EqualTo(0), "o fim não aparece de novo");
        }

        // ── A parte permanente (Marco 8b, E2) ──────────────────────────────────────────────────────────────────

        static UpgradeDefinition PermanentLine(int max, double effect) =>
            new UpgradeDefinition(10, 2, max, effect, CostFormula.OffsetExponential);

        static PermanentRules V7Permanents() => new PermanentRules(
            10, new RedShardTable(10, 10, 5, 50, 10, 1),
            PermanentLine(10, 10), PermanentLine(20, 0.1), PermanentLine(20, 0.5), PermanentLine(10, 0.05), PermanentLine(10, 1));

        static PermanentState PlayedPermanent()
        {
            var permanent = new PermanentState();
            var sim = new MiningSimulation(Rules(), new SeededRandom(1), economy: Prototype(),
                                           permanents: new Permanents(V7Permanents(), permanent));
            sim.AddRedShards(40);
            sim.PurchasePermanent(PermanentId.MountainBlood); // 11
            sim.PurchasePermanent(PermanentId.MountainBlood); // 12
            sim.PurchasePermanent(PermanentId.DwarfSoul);     // 11
            sim.Rest(force: true);                            // dia 2
            return permanent;
        }

        [Test]
        public void SaveThenLoad_KeepsThePermanentPart_NextToTheRun()
        {
            var state = PlayedState();
            _save.Save(state, PlayedPermanent());

            var loaded = _save.Load(Prototype(), V7Permanents(), out var permanent, out var status);

            Assert.That(status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(loaded.Shards, Is.EqualTo(state.Shards), "a run, como antes");
            Assert.That(permanent.RedShards, Is.EqualTo(6));
            Assert.That(permanent.Day, Is.EqualTo(2));
            Assert.That(permanent.LevelOf(PermanentId.MountainBlood), Is.EqualTo(2));
            Assert.That(permanent.LevelOf(PermanentId.DwarfSoul), Is.EqualTo(1));
            Assert.That(permanent.LevelOf(PermanentId.Memory), Is.EqualTo(0));
            Assert.That(File.ReadAllText(_store.FilePath), Does.Contain("\"permanent\""));
        }

        /// <summary>Um save de antes do Marco 8b: a run abre como estava, as permanentes zeradas, no dia 1.</summary>
        [Test]
        public void Version3_OpensTheRun_WithThePermanentsZeroed()
        {
            WriteSave("{ \"version\": 3, \"shards\": 77.0, \"levels\": [ { \"upgrade\": \"Pickaxe\", \"level\": 2 } ] }");

            var state = _save.Load(Prototype(), V7Permanents(), out var permanent, out var status);

            Assert.That(status, Is.EqualTo(SaveLoadStatus.Loaded));
            Assert.That(state.Shards, Is.EqualTo(77));
            Assert.That(state.LevelOf(UpgradeId.Pickaxe), Is.EqualTo(2));
            Assert.That(permanent.RedShards, Is.EqualTo(0));
            Assert.That(permanent.Day, Is.EqualTo(1));
            foreach (PermanentId id in Enum.GetValues(typeof(PermanentId)))
                Assert.That(permanent.LevelOf(id), Is.EqualTo(0), id.ToString());
        }

        /// <summary>Valores errados na parte permanente zeram ou vão para o limite só nela; a run abre inteira.</summary>
        [Test]
        public void PermanentValuesOutOfPlace_AreCorrected_WithoutTouchingTheRun()
        {
            WriteSave("{ \"version\": 4, \"shards\": 77.0, \"cave\": 5, " +
                      "\"permanent\": { \"redShards\": -3, \"day\": -2, \"levels\": [ " +
                      "{ \"upgrade\": \"Memory\", \"level\": 99 }, { \"upgrade\": \"DwarfSoul\", \"level\": -4 }, " +
                      "{ \"upgrade\": \"Unknown\", \"level\": 3 } ] } }");

            var state = _save.Load(Prototype(), V7Permanents(), out var permanent, out _, 10);

            Assert.That(state.Shards, Is.EqualTo(77));
            Assert.That(state.Cave, Is.EqualTo(5));
            Assert.That(permanent.RedShards, Is.EqualTo(0));
            Assert.That(permanent.Day, Is.EqualTo(1));
            Assert.That(permanent.LevelOf(PermanentId.Memory), Is.EqualTo(10), "no limite da linha");
            Assert.That(permanent.LevelOf(PermanentId.DwarfSoul), Is.EqualTo(0));
        }

        /// <summary>Num balanço sem permanentes (o GDD v5 do DBG → Cálculos), os níveis guardados não se perdem.</summary>
        [Test]
        public void ABalanceWithoutPermanents_KeepsTheSavedLevels()
        {
            _save.Save(PlayedState(), PlayedPermanent());

            _save.Load(Prototype(), PermanentRules.None, out var permanent, out _);
            Assert.That(permanent.LevelOf(PermanentId.MountainBlood), Is.EqualTo(2), "sem efeito ali, mas guardado");
            Assert.That(new Permanents(PermanentRules.None, permanent).DamageMultiplier, Is.EqualTo(1));

            _save.Save(PlayedState(), permanent);
            _save.Load(Prototype(), V7Permanents(), out var back, out _);
            Assert.That(back.LevelOf(PermanentId.MountainBlood), Is.EqualTo(2), "de volta no balanço que tem a linha");
        }

        [Test]
        public void Delete_RemovesTheSave()
        {
            _save.Save(PlayedState());
            _save.Delete();

            _save.Load(Prototype(), V7Permanents(), out var permanent, out var status);
            Assert.That(status, Is.EqualTo(SaveLoadStatus.NewGame));
            Assert.That(permanent.RedShards, Is.EqualTo(0), "as duas partes saem juntas");
            Assert.That(permanent.Day, Is.EqualTo(1));
        }
    }
}
