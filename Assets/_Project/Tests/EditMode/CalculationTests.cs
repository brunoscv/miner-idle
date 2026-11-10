using System;
using System.IO;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Persistence;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M8 E3b-3 (D-143): a taxa e o incremento da Picareta trocados no DBG → Cálculos entram nas regras montadas a partir
    /// do asset (o núcleo não muda), o custo continua "2 × Dano Base" (D-133), e a escolha vai e volta do arquivo.
    /// </summary>
    public class CalculationTests
    {
        const string GddV7Path = "Assets/_Project/Data/Balance_GDD_v7.asset";
        const string GddV5Path = "Assets/_Project/Data/Balance_GDD_v5.asset";

        string _dir;

        [SetUp]
        public void SetUp() => _dir = Path.Combine(Path.GetTempPath(), "MinersIdleCalcTests_" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        static MiningRulesAsset Load(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(path);
            Assert.That(asset, Is.Not.Null, path + " (rode o setup do projeto)");
            return asset;
        }

        [Test]
        public void WithoutChoice_TheRulesAreTheAssetOnes()
        {
            var v7 = Load(GddV7Path);
            Assert.That(v7.ToRules().PickaxeCaveGrowth, Is.EqualTo(1.2), "taxa do GDD v7 (D-133)");
            Assert.That(v7.PickaxeStep, Is.EqualTo(0), "incremento do GDD v7: o Dano Base não sobe (D-202)");
            var pickaxe = v7.ToEconomy().Get(UpgradeId.Pickaxe);
            Assert.That(pickaxe.CostAt(0), Is.EqualTo(4.2).Within(1e-9), "2,1 × (1 + 1,2^0)");
            Assert.That(pickaxe.CostAt(1), Is.EqualTo(4.62).Within(1e-9), "2,1 × (1 + 1,2^1)");
        }

        [Test]
        public void Growth_ChangesThePickaxeOnly()
        {
            var v7 = Load(GddV7Path);
            var rules = v7.ToRules(pickaxeCaveGrowth: 1.25);
            Assert.That(rules.PickaxeCaveGrowth, Is.EqualTo(1.25));
            Assert.That(rules.CaveCount, Is.EqualTo(v7.ToRules().CaveCount));
            Assert.That(rules.CrystalHealth, Is.EqualTo(v7.ToRules().CrystalHealth), "a vida do cristal fica");
        }

        [Test]
        public void Growth_AlsoChangesThePickaxeCost()
        {
            var v7 = Load(GddV7Path);
            var pickaxe = v7.ToEconomy(pickaxeCaveGrowth: 1.25).Get(UpgradeId.Pickaxe);
            Assert.That(pickaxe.CostAt(2), Is.EqualTo(2.1 * (1 + 1.25 * 1.25)).Within(1e-9), "2,1 × o dano com a taxa nova (D-202)");
        }

        [Test]
        public void Step_ChangesTheBaseDamagePerBuy_AndTheCostFollowsTheDamage()
        {
            var v7 = Load(GddV7Path);
            var pickaxe = v7.ToEconomy(pickaxeStep: 3).Get(UpgradeId.Pickaxe);
            Assert.That(pickaxe.Effect, Is.EqualTo(3));
            Assert.That(pickaxe.CostAt(0), Is.EqualTo(4.2).Within(1e-9), "Dano Base 1 + 1,2^0 = 2 → custo 4,20");
            Assert.That(pickaxe.CostAt(1), Is.EqualTo(2.1 * (1 + 3 + 1.2)).Within(1e-9), "Dano Base 4 + 1,2^1 → 2,1 × 5,2");
            Assert.That(pickaxe.MaxLevel, Is.EqualTo(v7.ToEconomy().Get(UpgradeId.Pickaxe).MaxLevel));
        }

        [Test]
        public void TheV5Pickaxe_IgnoresGrowthAndStep()
        {
            var v5 = Load(GddV5Path);
            Assert.That(v5.PickaxeFormula, Is.EqualTo(PickaxeFormula.Multiply));
            var plain = v5.ToEconomy().Get(UpgradeId.Pickaxe);
            var tuned = v5.ToEconomy(pickaxeStep: 9).Get(UpgradeId.Pickaxe);
            Assert.That(tuned.Effect, Is.EqualTo(plain.Effect), "no v5 a Picareta multiplica: o incremento não vale");
            Assert.That(v5.ToRules(pickaxeCaveGrowth: 1.5).PickaxeCaveGrowth, Is.EqualTo(v5.ToRules().PickaxeCaveGrowth));
        }

        [Test]
        public void Choice_IsDefaultOnlyWhenEverythingIsTheGdd()
        {
            Assert.That(default(CalculationChoice).IsDefault, Is.True);
            Assert.That(new CalculationChoice(1).IsDefault, Is.False);
            Assert.That(default(CalculationChoice).WithPickaxeCaveGrowth(1.21).IsDefault, Is.False);
            Assert.That(default(CalculationChoice).WithPickaxeStep(2).IsDefault, Is.False);
            Assert.That(default(CalculationChoice).WithPickaxeStep(2).WithPickaxeStep(null).IsDefault, Is.True);
            Assert.That(default(CalculationChoice).WithPickaxeByCave(true).IsDefault, Is.False, "Picareta pela caverna (D-201)");
            Assert.That(default(CalculationChoice).WithPickaxeByCave(true).WithPickaxeByCave(false).IsDefault, Is.True);
            Assert.That(default(CalculationChoice).WithComboStacking(ComboStacking.HighestOnly).IsDefault, Is.False,
                        "só o maior marco (D-217)");
            Assert.That(default(CalculationChoice).ComboStacking, Is.EqualTo(ComboStacking.Cumulative), "o GDD soma (D-183)");
        }

        [Test]
        public void Store_KeepsTheComboStacking_AndAnOldFileSums()
        {
            var store = new CalculationStore(new FileSaveStore(_dir, CalculationStore.FileName));
            var choice = default(CalculationChoice).WithPickaxeStep(2).WithComboStacking(ComboStacking.HighestOnly);
            store.Save(choice.ToData());
            var back = CalculationChoice.FromData(store.Load());
            Assert.That(back, Is.EqualTo(choice));
            Assert.That(back.WithBalance(1).ComboStacking, Is.EqualTo(ComboStacking.HighestOnly), "trocar outra coisa não volta o acúmulo");

            Directory.CreateDirectory(_dir);
            File.WriteAllText(Path.Combine(_dir, CalculationStore.FileName), "{ \"balance\": 0, \"pickaxeByCave\": true }");
            Assert.That(CalculationChoice.FromData(store.Load()).ComboStacking, Is.EqualTo(ComboStacking.Cumulative),
                        "arquivo de antes do M9b: a soma do GDD");
        }

        [Test]
        public void ByCave_SwitchesTheV7PickaxeToTheCave_AndTheV5IgnoresIt()
        {
            var v7 = Load(GddV7Path);
            Assert.That(v7.ToRules().PickaxeFormula, Is.EqualTo(PickaxeFormula.LevelScaled), "o GDD: pela compra (D-201)");
            var byCave = v7.ToRules(pickaxeCaveGrowth: 1.3, pickaxeByCave: true);
            Assert.That(byCave.PickaxeFormula, Is.EqualTo(PickaxeFormula.CaveScaled), "a leitura da D-133");
            Assert.That(byCave.PickaxeCaveGrowth, Is.EqualTo(1.3), "a taxa do DBG vale nas duas");

            var v5 = Load(GddV5Path);
            Assert.That(v5.ToRules(pickaxeByCave: true).PickaxeFormula, Is.EqualTo(PickaxeFormula.Multiply));
        }

        [Test]
        public void Store_KeepsThePickaxeByCave_AndAnOldFileIsByLevel()
        {
            var store = new CalculationStore(new FileSaveStore(_dir, CalculationStore.FileName));
            var choice = default(CalculationChoice).WithPickaxeByCave(true);
            store.Save(choice.ToData());
            Assert.That(CalculationChoice.FromData(store.Load()), Is.EqualTo(choice));

            Directory.CreateDirectory(_dir);
            File.WriteAllText(Path.Combine(_dir, CalculationStore.FileName), "{ \"balance\": 0, \"hasPickaxeStep\": true, \"pickaxeStep\": 3 }");
            var old = CalculationChoice.FromData(store.Load());
            Assert.That(old.PickaxeByCave, Is.False, "arquivo de antes da D-201: a do GDD");
            Assert.That(old.PickaxeStep, Is.EqualTo(3));
        }

        [Test]
        public void Store_KeepsTheChoice_InItsOwnFile()
        {
            var store = new CalculationStore(new FileSaveStore(_dir, CalculationStore.FileName));
            Assert.That(store.Load(), Is.Null, "sem arquivo, nenhuma escolha");

            var choice = new CalculationChoice(1, 1.22, null);
            store.Save(choice.ToData());
            Assert.That(File.Exists(Path.Combine(_dir, CalculationStore.FileName)), Is.True);
            Assert.That(File.Exists(Path.Combine(_dir, FileSaveStore.FileName)), Is.False, "não mexe no save");
            Assert.That(CalculationChoice.FromData(store.Load()), Is.EqualTo(choice));

            store.Save(default(CalculationChoice).WithPickaxeStep(4).ToData());
            var back = CalculationChoice.FromData(store.Load());
            Assert.That(back.PickaxeStep, Is.EqualTo(4));
            Assert.That(back.PickaxeCaveGrowth, Is.Null, "o \"nenhum\" volta como nenhum");
        }

        [Test]
        public void Store_UnreadableFile_IsNoChoice()
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(Path.Combine(_dir, CalculationStore.FileName), "{ isto não é json");
            var store = new CalculationStore(new FileSaveStore(_dir, CalculationStore.FileName));
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("ilegível"));
            Assert.That(store.Load(), Is.Null);
        }
    }
}
