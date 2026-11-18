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
    /// do asset (o núcleo não muda), o custo segue o dano da Picareta (D-202; no v8, 7 × o dano, P5), e a escolha vai e
    /// volta do arquivo. Desde o v8 (M9c E1, D-256) o balanço é um só, o <c>Balance_GDD_v8</c>, e a Picareta pela caverna
    /// saiu: um arquivo antigo com essas escolhas abre só com o resto.
    /// </summary>
    public class CalculationTests
    {
        const string GddV8Path = "Assets/_Project/Data/Balance_GDD_v8.asset";
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
            var v8 = Load(GddV8Path);
            Assert.That(v8.ToRules().PickaxeCaveGrowth, Is.EqualTo(1.2), "taxa do GDD v8");
            Assert.That(v8.PickaxeStep, Is.EqualTo(0), "incremento: o Dano Base não sobe (D-202)");
            var pickaxe = v8.ToEconomy().Get(UpgradeId.Pickaxe);
            Assert.That(pickaxe.CostAt(0), Is.EqualTo(14).Within(1e-9), "7 × (1 + 1,2^0)");
            Assert.That(pickaxe.CostAt(1), Is.EqualTo(15.4).Within(1e-9), "7 × (1 + 1,2^1)");
        }

        [Test]
        public void Growth_ChangesThePickaxeOnly()
        {
            var v8 = Load(GddV8Path);
            var rules = v8.ToRules(pickaxeCaveGrowth: 1.25);
            Assert.That(rules.PickaxeCaveGrowth, Is.EqualTo(1.25));
            Assert.That(rules.CaveCount, Is.EqualTo(v8.ToRules().CaveCount));
            Assert.That(rules.CrystalHealth, Is.EqualTo(v8.ToRules().CrystalHealth), "a vida do cristal fica");
        }

        [Test]
        public void Growth_AlsoChangesThePickaxeCost()
        {
            var v8 = Load(GddV8Path);
            var pickaxe = v8.ToEconomy(pickaxeCaveGrowth: 1.25).Get(UpgradeId.Pickaxe);
            Assert.That(pickaxe.CostAt(2), Is.EqualTo(7 * (1 + 1.25 * 1.25)).Within(1e-9), "7 × o dano com a taxa nova (D-202)");
        }

        [Test]
        public void Step_ChangesTheBaseDamagePerBuy_AndTheCostFollowsTheDamage()
        {
            var v8 = Load(GddV8Path);
            var pickaxe = v8.ToEconomy(pickaxeStep: 3).Get(UpgradeId.Pickaxe);
            Assert.That(pickaxe.Effect, Is.EqualTo(3));
            Assert.That(pickaxe.CostAt(0), Is.EqualTo(14).Within(1e-9), "Dano Base 1 + 1,2^0 = 2 → custo 14");
            Assert.That(pickaxe.CostAt(1), Is.EqualTo(7 * (1 + 3 + 1.2)).Within(1e-9), "Dano Base 4 + 1,2^1 → 7 × 5,2");
            Assert.That(pickaxe.MaxLevel, Is.EqualTo(v8.ToEconomy().Get(UpgradeId.Pickaxe).MaxLevel));
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
            Assert.That(default(CalculationChoice).WithPickaxeCaveGrowth(1.21).IsDefault, Is.False);
            Assert.That(default(CalculationChoice).WithPickaxeStep(2).IsDefault, Is.False);
            Assert.That(default(CalculationChoice).WithPickaxeStep(2).WithPickaxeStep(null).IsDefault, Is.True);
            Assert.That(default(CalculationChoice).WithPickaxeCaveGrowth(1.3).WithPickaxeCaveGrowth(null).IsDefault, Is.True);
        }

        /// <summary>M9b E4 (D-226): o ritmo do raro do DBG → Cálculos vai para o calculations.json; arquivo antigo = o GDD.</summary>
        [Test]
        public void Store_KeepsTheRareRhythm_AndAnOldFileUsesTheGdd()
        {
            Assert.That(default(CalculationChoice).RareRhythm, Is.EqualTo(RareRhythm.Gdd), "15% do GDD (D-184)");
            Assert.That(default(CalculationChoice).WithRareRhythm(RareRhythm.Slow).IsDefault, Is.False);

            var store = new CalculationStore(new FileSaveStore(_dir, CalculationStore.FileName));
            var choice = default(CalculationChoice).WithPickaxeStep(2).WithRareRhythm(RareRhythm.Slow);
            store.Save(choice.ToData());
            var back = CalculationChoice.FromData(store.Load());
            Assert.That(back, Is.EqualTo(choice));
            Assert.That(back.WithPickaxeStep(5).RareRhythm, Is.EqualTo(RareRhythm.Slow), "trocar outra coisa não volta o ritmo");

            Directory.CreateDirectory(_dir);
            File.WriteAllText(Path.Combine(_dir, CalculationStore.FileName), "{ \"balance\": 0, \"comboHighestOnly\": true }"); // o acúmulo do combo saiu (D-311): o campo velho é ignorado
            Assert.That(CalculationChoice.FromData(store.Load()).RareRhythm, Is.EqualTo(RareRhythm.Gdd));
        }

        [Test]
        public void ThePickaxe_IsAlwaysByPurchase_AndTheGrowthStillTunes()
        {
            var v8 = Load(GddV8Path);
            Assert.That(v8.ToRules().PickaxeFormula, Is.EqualTo(PickaxeFormula.LevelScaled), "pela compra (D-201; a pela caverna saiu, D-256)");
            var tuned = v8.ToRules(pickaxeCaveGrowth: 1.3);
            Assert.That(tuned.PickaxeFormula, Is.EqualTo(PickaxeFormula.LevelScaled));
            Assert.That(tuned.PickaxeCaveGrowth, Is.EqualTo(1.3), "a taxa do DBG vale");
        }

        [Test]
        public void Store_AnOldFileWithTheRetiredChoices_KeepsTheRest()
        {
            var store = new CalculationStore(new FileSaveStore(_dir, CalculationStore.FileName));
            Directory.CreateDirectory(_dir);
            File.WriteAllText(Path.Combine(_dir, CalculationStore.FileName),
                              "{ \"balance\": 1, \"pickaxeByCave\": true, \"hasPickaxeStep\": true, \"pickaxeStep\": 3 }");
            var old = CalculationChoice.FromData(store.Load());
            Assert.That(old.PickaxeStep, Is.EqualTo(3), "o que ainda existe volta");
            Assert.That(old, Is.EqualTo(default(CalculationChoice).WithPickaxeStep(3)),
                        "o seletor de balanço e a Picareta pela caverna ficam de fora (D-256)");
        }

        [Test]
        public void Store_KeepsTheChoice_InItsOwnFile()
        {
            var store = new CalculationStore(new FileSaveStore(_dir, CalculationStore.FileName));
            Assert.That(store.Load(), Is.Null, "sem arquivo, nenhuma escolha");

            var choice = new CalculationChoice(1.22, null);
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
