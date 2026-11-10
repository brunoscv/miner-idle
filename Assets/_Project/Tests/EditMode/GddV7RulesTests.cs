using System;
using System.Linq;
using MinersIdle.Core;
using MinersIdle.Game;
using NUnit.Framework;
using UnityEditor;

namespace MinersIdle.Tests
{
    /// <summary>
    /// E3b-1 do M8: as regras do GDD v7 no núcleo (D-133…D-136) e o <c>Balance_GDD_v7</c> que o setup cria (D-130).
    /// Os números das regras estão escritos à mão; o teste do asset confere que ele traz esses mesmos números.
    /// </summary>
    public class GddV7RulesTests
    {
        const string GddV7Path = "Assets/_Project/Data/Balance_GDD_v7.asset";

        static MiningRules Rules(PickaxeFormula formula = PickaxeFormula.CaveScaled, double health = 1e12, int caves = 10) =>
            new MiningRules(
                initialCrystals: 0, firstCrystalDistance: 3, maxCrystals: 12, spawnInterval: 1e9, minCrystalSpacing: 2,
                spawnDistance: 12, crystalHealth: health, crystalReward: 1.2, damage: 1, hitInterval: 1, walkSpeed: 4,
                miningRange: 1, caveCount: caves, caveHealthGrowth: 1.25, caveRewardGrowth: 1.25,
                pickaxeFormula: formula, pickaxeCaveGrowth: 1.2);

        /// <summary>
        /// As linhas do anão no GDD v7, com o Letrado já de custo zero e sem ×2 (D-128). A Picareta aqui é a da D-133 (+1 no
        /// Dano Base, custo 2 × Dano Base), que mede a fórmula com incremento; a do jogo desde a D-202 está em
        /// <see cref="ApellonPickaxe"/>.
        /// </summary>
        static EconomyRules V7() => new EconomyRules(
            pickaxe: new UpgradeDefinition(2, 2, 49, 1, CostFormula.Linear),
            speed: new UpgradeDefinition(50, 2, 19, 0.1),
            mining: new UpgradeDefinition(20, 5, 10, 1),
            doublePickaxe: new UpgradeDefinition(5000, 1, 0, 2),
            boots: new UpgradeDefinition(0, 1, 0, 0),
            minHitInterval: 0.1,
            letrado: new UpgradeDefinition(0, 1, 10, 1),
            critical: new UpgradeDefinition(50, 2, 10, 0.05));

        /// <summary>GDD v7 §3.3 (D-136): golpe automático sem crítico de base, toque com 5%; sem teto de toques.</summary>
        static StrikeRules V7Strike() => new StrikeRules(0, 2, 0.33, 1, tapCritChance: 0.05);

        static MiningSimulation Sim(MiningRules rules = null, ulong critSeed = 7) =>
            new MiningSimulation(rules ?? Rules(), new SeededRandom(critSeed), economy: V7(), strike: V7Strike());

        // ── Fórmulas de custo ────────────────────────────────────────────────────────────────────────────────────

        /// <summary>A fórmula Linear (a da D-133, antes da D-202): 2 × Dano Base (1, 2, 3…): 2, 4, 6… até 98; somam 2.450.</summary>
        [Test]
        public void LinearCost_IsBasePlusStepTimesLevel()
        {
            var pickaxe = V7().Get(UpgradeId.Pickaxe);

            Assert.That(pickaxe.CostAt(0), Is.EqualTo(2));
            Assert.That(pickaxe.CostAt(1), Is.EqualTo(4));
            Assert.That(pickaxe.CostAt(48), Is.EqualTo(98));
            Assert.That(Enumerable.Range(0, pickaxe.MaxLevel).Sum(level => pickaxe.CostAt(level)), Is.EqualTo(2450));
        }

        // ── Picareta pela tabela do Apellon (D-202) ──────────────────────────────────────────────────────────────

        /// <summary>Dano Base 1 fixo (incremento 0), taxa 1,2, custo 2,1 × o dano da Picareta no nível atual.</summary>
        static UpgradeDefinition ApellonPickaxe() => new UpgradeDefinition(2.1, 1.2, 49, 0, CostFormula.DamageScaled, costOffset: 1);

        static EconomyRules V7Apellon() => new EconomyRules(
            pickaxe: ApellonPickaxe(),
            speed: new UpgradeDefinition(50, 2, 19, 0.1),
            mining: new UpgradeDefinition(20, 5, 10, 1),
            doublePickaxe: new UpgradeDefinition(5000, 1, 0, 2),
            boots: new UpgradeDefinition(0, 1, 0, 0),
            minHitInterval: 0.1,
            letrado: new UpgradeDefinition(0, 1, 10, 1),
            critical: new UpgradeDefinition(50, 2, 10, 0.05));

        /// <summary>Os pontos da tabela do Apellon: nível da Picareta → dano e custo da próxima compra (D-202).</summary>
        [TestCase(1, 2.00, 4.20), TestCase(10, 6.16, 12.94), TestCase(20, 32.95, 69.19), TestCase(30, 198.81, 417.51)]
        public void ApellonPickaxe_MatchesHisTable(int level, double damage, double cost)
        {
            var state = GameState.Restore(0, 0, 0, id => id == UpgradeId.Pickaxe ? level - 1 : 0, V7Apellon());
            var sim = new MiningSimulation(Rules(PickaxeFormula.LevelScaled), new SeededRandom(7), state, V7Apellon(), V7Strike());

            Assert.That(sim.Stats.Damage, Is.EqualTo(damage).Within(0.005), "Dano Base 1 + 1,2^(nível − 1)");
            Assert.That(sim.Preview(UpgradeId.Pickaxe).NextCost.Value, Is.EqualTo(cost).Within(0.005), "2,1 × o dano");
        }

        /// <summary>Os mesmos pontos da tabela, agora pelo <c>Balance_GDD_v7</c> que o jogo usa (D-202).</summary>
        [TestCase(1, 2.00, 4.20), TestCase(10, 6.16, 12.94), TestCase(20, 32.95, 69.19), TestCase(30, 198.81, 417.51)]
        public void BalanceGddV7Asset_MatchesApellonsTable(int level, double damage, double cost)
        {
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV7Path);
            Assert.That(asset, Is.Not.Null, "o setup cria o Balance_GDD_v7");
            var economy = asset.ToEconomy();
            var state = GameState.Restore(0, 0, 0, id => id == UpgradeId.Pickaxe ? level - 1 : 0, economy);
            var sim = new MiningSimulation(asset.ToRules(), new SeededRandom(7), state, economy, asset.ToStrike());

            Assert.That(sim.Stats.Damage, Is.EqualTo(damage).Within(0.005), "dano do nível " + level);
            Assert.That(sim.Preview(UpgradeId.Pickaxe).NextCost.Value, Is.EqualTo(cost).Within(0.005), "custo no nível " + level);
            Assert.That(NumberFormatter.FormatCost(sim.Preview(UpgradeId.Pickaxe).NextCost.Value),
                        Is.EqualTo(cost.ToString("#,0.##", new System.Globalization.CultureInfo("pt-BR"))), "o que a loja mostra");
        }

        [Test]
        public void ApellonPickaxe_ChargesTheDecimals_AndTheBaseDamageStaysAtOne()
        {
            var sim = new MiningSimulation(Rules(PickaxeFormula.LevelScaled), new SeededRandom(7), economy: V7Apellon(),
                                           strike: V7Strike());
            sim.AddShards(4.2 + 4.62);

            Assert.That(sim.Purchase(UpgradeId.Pickaxe), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(sim.Stats.Damage, Is.EqualTo(1 + 1.2).Within(1e-9), "nível 2: Dano Base 1 + 1,2^1");
            Assert.That(sim.Purchase(UpgradeId.Pickaxe), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(sim.Stats.Damage, Is.EqualTo(1 + 1.44).Within(1e-9), "nível 3: Dano Base 1 + 1,2^2");
            Assert.That(sim.State.Shards, Is.EqualTo(0).Within(1e-9), "cobrou 4,20 e 4,62, com as casas decimais");
            Assert.That(sim.Preview(UpgradeId.Pickaxe).NextCost.Value, Is.EqualTo(2.1 * 2.44).Within(1e-9));
        }

        [Test]
        public void ApellonPickaxe_FortyNineBuys_CostAbout79721()
        {
            var pickaxe = ApellonPickaxe();
            Assert.That(pickaxe.CostAt(41), Is.EqualTo(2.1 * (1 + Math.Pow(1.2, 41))).Within(1e-6), "42 compras feitas: ≈ 3.705,92");
            Assert.That(pickaxe.CostAt(48), Is.EqualTo(13273.57).Within(0.005), "a 49ª compra");
            Assert.That(Enumerable.Range(0, pickaxe.MaxLevel).Sum(level => pickaxe.CostAt(level)), Is.EqualTo(79721.23).Within(0.005));
        }

        [Test]
        public void ExponentialCost_IsStillTheDefault()
        {
            var speed = V7().Get(UpgradeId.Speed);

            Assert.That(speed.CostFormula, Is.EqualTo(CostFormula.Exponential));
            Assert.That(speed.CostAt(0), Is.EqualTo(50));
            Assert.That(speed.CostAt(18), Is.EqualTo(50 * Math.Pow(2, 18)));
        }

        [Test]
        public void LinearCost_AcceptsAZeroStep_ExponentialDoesNot()
        {
            Assert.That(new UpgradeDefinition(10, 0, 5, 1, CostFormula.Linear).CostAt(4), Is.EqualTo(10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UpgradeDefinition(10, 0, 5, 1));
        }

        // ── Picareta pela caverna (D-133) ────────────────────────────────────────────────────────────────────────

        [Test]
        public void CaveScaledPickaxe_InCaveOne_HitsForTwo()
        {
            Assert.That(Sim().Stats.Damage, Is.EqualTo(2), "Dano Base 1 + 1,2^0");
        }

        [Test]
        public void CaveScaledPickaxe_EachPurchaseAddsOneToTheBase()
        {
            var sim = Sim();
            sim.AddShards(2 + 4);

            Assert.That(sim.Purchase(UpgradeId.Pickaxe), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(sim.Stats.Damage, Is.EqualTo(3));
            Assert.That(sim.Purchase(UpgradeId.Pickaxe), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(sim.Stats.Damage, Is.EqualTo(4));
            Assert.That(sim.State.Shards, Is.EqualTo(0).Within(1e-9), "custou 2 e depois 4");
        }

        [Test]
        public void CaveScaledPickaxe_GrowsWithTheCave_WithoutTheLetradoDoubling()
        {
            var sim = Sim();
            sim.AddShards(2);
            sim.Purchase(UpgradeId.Pickaxe); // Dano Base 2

            Assert.That(sim.SkipToCave(10), Is.True);

            Assert.That(sim.Stats.Damage, Is.EqualTo(2 + Math.Pow(1.2, 9)).Within(1e-9), "Dano Base 2 + 1,2^9, sem ×2");
        }

        // ── Picareta pela compra (GDD v7, D-201) ─────────────────────────────────────────────────────────────────

        [Test]
        public void LevelScaledPickaxe_WithoutPurchase_HitsForTwo()
        {
            Assert.That(Sim(Rules(PickaxeFormula.LevelScaled)).Stats.Damage, Is.EqualTo(2), "Dano Base 1 + 1,2^(1 − 1)");
        }

        [Test]
        public void LevelScaledPickaxe_EachPurchaseAddsOneToTheBase_AndOneLevelToTheRate()
        {
            var sim = Sim(Rules(PickaxeFormula.LevelScaled));
            sim.AddShards(2 + 4);

            Assert.That(sim.Purchase(UpgradeId.Pickaxe), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(sim.Stats.Damage, Is.EqualTo(2 + 1.2).Within(1e-9), "nível 2: Dano Base 2 + 1,2^1");
            Assert.That(sim.Purchase(UpgradeId.Pickaxe), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(sim.Stats.Damage, Is.EqualTo(3 + 1.44).Within(1e-9), "nível 3: Dano Base 3 + 1,2^2");
            Assert.That(sim.State.Shards, Is.EqualTo(0).Within(1e-9), "o custo segue 2 × Dano Base: 2 e depois 4");
        }

        [Test]
        public void LevelScaledPickaxe_DoesNotGrowWithTheCave()
        {
            var sim = Sim(Rules(PickaxeFormula.LevelScaled));
            sim.AddShards(2);
            sim.Purchase(UpgradeId.Pickaxe);
            double inCaveOne = sim.Stats.Damage;

            Assert.That(sim.SkipToCave(10), Is.True);

            Assert.That(sim.Stats.Damage, Is.EqualTo(inCaveOne), "a caverna não entra no dano (D-201)");
        }

        [Test]
        public void LevelScaledPickaxe_ThePreviewShowsTheNextLevel()
        {
            var sim = Sim(Rules(PickaxeFormula.LevelScaled));
            var preview = sim.Preview(UpgradeId.Pickaxe);

            Assert.That(preview.CurrentValue, Is.EqualTo(2));
            Assert.That(preview.NextValue, Is.EqualTo(2 + 1.2).Within(1e-9), "a loja mostra Dano 2 » 3,2");
        }

        [Test]
        public void MultiplyPickaxe_IsTheFormulaUpToGddV5()
        {
            var sim = new MiningSimulation(Rules(PickaxeFormula.Multiply), new SeededRandom(1), economy: new EconomyRules(
                pickaxe: new UpgradeDefinition(50, 4, 19, 2), speed: new UpgradeDefinition(0, 1, 0, 0),
                mining: new UpgradeDefinition(0, 1, 0, 0), doublePickaxe: new UpgradeDefinition(0, 1, 0, 0),
                boots: new UpgradeDefinition(0, 1, 0, 0), minHitInterval: 0.1));
            sim.AddShards(50);
            sim.Purchase(UpgradeId.Pickaxe);

            Assert.That(sim.Stats.Damage, Is.EqualTo(2), "1 × 2^1");
        }

        // ── Crítico (D-136) ──────────────────────────────────────────────────────────────────────────────────────

        static (int hits, int critical) CountAutomatic(MiningSimulation sim, int hits)
        {
            sim.SpawnCrystalAt(1);
            int total = 0, critical = 0;
            sim.HitLanded += hit =>
            {
                if (hit.IsTap) return;
                total++;
                if (hit.IsCritical) critical++;
            };
            sim.Tick(hits);
            return (total, critical);
        }

        static int CountTapCriticals(MiningSimulation sim, int taps)
        {
            sim.SpawnCrystalAt(1);
            sim.Tick(0);
            int critical = 0;
            sim.HitLanded += hit => { if (hit.IsTap && hit.IsCritical) critical++; };
            for (int i = 0; i < taps; i++) sim.Tap();
            return critical;
        }

        [Test]
        public void AutomaticHit_HasNoCriticalBase()
        {
            var (hits, critical) = CountAutomatic(Sim(), 20_000);

            Assert.That(hits, Is.GreaterThanOrEqualTo(19_999));
            Assert.That(critical, Is.EqualTo(0));
        }

        /// <summary>20.000 toques a 5%: média 1.000, desvio ~31; a faixa é de ~4 desvios.</summary>
        [Test]
        public void Tap_HasFivePercentCriticalBase()
        {
            Assert.That(CountTapCriticals(Sim(), 20_000), Is.InRange(875, 1_125));
        }

        [Test]
        public void CriticalLine_AddsFivePercent_ToTheHitAndToTheTap()
        {
            var auto = Sim();
            auto.AddShards(50);
            Assert.That(auto.Purchase(UpgradeId.Critical), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(auto.Stats.CritBonus, Is.EqualTo(0.05).Within(1e-12));
            var (hits, autoCritical) = CountAutomatic(auto, 100_000);
            Assert.That(autoCritical / (double)hits, Is.InRange(0.047, 0.053), "golpe: 0% + 5%");

            var tap = Sim(critSeed: 11);
            tap.AddShards(50);
            tap.Purchase(UpgradeId.Critical);
            Assert.That(CountTapCriticals(tap, 20_000), Is.InRange(1_830, 2_170), "toque: 5% + 5% (média 2.000, desvio ~42)");
        }

        [Test]
        public void CriticalLine_CostsFiftyTimesTwoToTheLevel_UpToTen()
        {
            var sim = Sim();
            sim.AddShards(50 * (Math.Pow(2, 10) - 1));

            for (int level = 0; level < 10; level++)
                Assert.That(sim.Purchase(UpgradeId.Critical), Is.EqualTo(PurchaseResult.Purchased), $"compra {level + 1}");

            Assert.That(sim.Purchase(UpgradeId.Critical), Is.EqualTo(PurchaseResult.MaxLevel));
            Assert.That(sim.Stats.CritBonus, Is.EqualTo(0.5).Within(1e-12));
            Assert.That(sim.State.Shards, Is.EqualTo(0).Within(1e-6));
        }

        [Test]
        public void CriticalPreview_ShowsTheChanceTheLineAdds()
        {
            var preview = Sim().Preview(UpgradeId.Critical);

            Assert.That(preview.IsEnabled, Is.True);
            Assert.That(preview.CurrentValue, Is.EqualTo(0));
            Assert.That(preview.NextValue, Is.EqualTo(0.05).Within(1e-12));
            Assert.That(preview.NextCost, Is.EqualTo(50));
        }

        [Test]
        public void CriticalLine_IsOff_WhenTheEconomyDoesNotHaveIt()
        {
            var economy = new EconomyRules(new UpgradeDefinition(1, 1, 1, 2), new UpgradeDefinition(1, 1, 1, 0.1),
                new UpgradeDefinition(1, 1, 1, 1), new UpgradeDefinition(1, 1, 1, 2), new UpgradeDefinition(1, 1, 1, 0.1), 0.1);

            Assert.That(economy.Get(UpgradeId.Critical).IsEnabled, Is.False, "o jogo até o GDD v5 não tem a linha");
        }

        // ── Picareta Dupla fora (D-136) ──────────────────────────────────────────────────────────────────────────

        [Test]
        public void DoublePickaxe_IsOff()
        {
            var sim = Sim();
            sim.AddShards(1e6);

            Assert.That(sim.Preview(UpgradeId.DoublePickaxe).IsEnabled, Is.False, "some da loja");
            Assert.That(sim.Purchase(UpgradeId.DoublePickaxe), Is.EqualTo(PurchaseResult.MaxLevel));
        }

        // ── O asset (D-130) ──────────────────────────────────────────────────────────────────────────────────────

        // ---- E3b-2: Mapa e 100 níveis (D-128, D-129) ----

        static MiningRules HundredLevels() =>
            new MiningRules(
                initialCrystals: 4, firstCrystalDistance: 3, maxCrystals: 12, spawnInterval: 1e9, minCrystalSpacing: 2,
                spawnDistance: 12, crystalHealth: 1, crystalReward: 1.2, damage: 1, hitInterval: 1, walkSpeed: 4,
                miningRange: 1, minCrystals: 4, caveCount: 100, caveHealthGrowth: 1.25, caveRewardGrowth: 1.25,
                pickaxeFormula: PickaxeFormula.CaveScaled, pickaxeCaveGrowth: 1.2, levelsPerBiome: 10);

        static EconomyRules Map() => new EconomyRules(
            pickaxe: new UpgradeDefinition(2, 2, 49, 1, CostFormula.Linear),
            speed: new UpgradeDefinition(50, 2, 19, 0.1),
            mining: new UpgradeDefinition(20, 5, 10, 1),
            doublePickaxe: new UpgradeDefinition(5000, 1, 0, 2),
            boots: new UpgradeDefinition(0, 1, 0, 0),
            minHitInterval: 0.1,
            letrado: new UpgradeDefinition(0, 1, 100, 1),
            critical: new UpgradeDefinition(50, 2, 10, 0.05));

        [TestCase(1, 0), TestCase(10, 0), TestCase(11, 1), TestCase(20, 1), TestCase(21, 2), TestCase(91, 9), TestCase(100, 9)]
        public void Biome_ChangesEveryTenLevels(int level, int biome)
        {
            Assert.That(HundredLevels().BiomeOf(level), Is.EqualTo(biome));
        }

        [Test]
        public void Biome_OnePerCave_UpToGddV5()
        {
            var rules = Rules();
            Assert.That(rules.LevelsPerBiome, Is.EqualTo(1));
            Assert.That(Enumerable.Range(1, 10).Select(rules.BiomeOf), Is.EqualTo(Enumerable.Range(0, 10)));
        }

        [Test]
        public void LevelsPerBiome_BelowOne_IsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new MiningRules(0, 3, 12, 1, 2, 12, 1, 1, 1, 1, 4, 1, levelsPerBiome: 0));
        }

        [Test]
        public void Map_IsFree_AfterTheHundredCrystals_AndDoesNotDoubleTheDamage()
        {
            var sim = new MiningSimulation(HundredLevels(), new SeededRandom(1), economy: Map(), strike: V7Strike());
            Assert.That(sim.Purchase(UpgradeId.Letrado), Is.EqualTo(PurchaseResult.Locked), "a trava dos 100 cristais");

            sim.FillCaveBreaks();
            Assert.That(sim.State.Shards, Is.EqualTo(0));
            Assert.That(sim.Purchase(UpgradeId.Letrado), Is.EqualTo(PurchaseResult.Purchased), "de graça (D-128)");
            Assert.That(sim.PassCave(), Is.True);
            double before = sim.Stats.Damage;

            for (int i = 0; i < 40 && sim.Wall != null; i++)
            {
                sim.Tick(0);
                sim.BreakTarget();
            }
            Assert.That(sim.State.Cave, Is.EqualTo(2));
            Assert.That(sim.Stats.Damage, Is.EqualTo(1 + Math.Pow(1.2, 1)).Within(1e-9), "só a taxa da caverna, sem ×2");
            Assert.That(sim.Stats.Damage, Is.GreaterThan(before));
        }

        [Test]
        public void LevelHundred_TheLastMapLeadsToTheHeart()
        {
            var sim = new MiningSimulation(HundredLevels(), new SeededRandom(1), economy: Map(), strike: V7Strike());
            int hearts = 0;
            sim.HeartReached += () => hearts++;

            Assert.That(sim.SkipToCave(150), Is.True, "vai até o último nível");
            Assert.That(sim.State.Cave, Is.EqualTo(100));
            Assert.That(sim.State.LevelOf(UpgradeId.Letrado), Is.EqualTo(99));
            sim.FillCaveBreaks();
            Assert.That(sim.Purchase(UpgradeId.Letrado), Is.EqualTo(PurchaseResult.Purchased), "o Mapa 100");
            Assert.That(sim.PassCave(), Is.True, "a última parede");
            Assert.That(sim.Wall.MaxHealth, Is.EqualTo(2 * Math.Pow(1.25, 100)).Within(1e-6).Percent, "D-98: 2 × a vida de um nível além");

            for (int i = 0; i < 40 && sim.Wall != null; i++)
            {
                sim.Tick(0);
                sim.BreakTarget();
            }
            Assert.That(hearts, Is.EqualTo(1));
            Assert.That(sim.State.ReachedHeart, Is.True);
            Assert.That(sim.State.Cave, Is.EqualTo(100));
            Assert.That(sim.CanPassCave, Is.False);
        }

        [Test]
        public void BalanceGddV7Asset_HasTheNumbersOfTheGdd()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV7Path);
            Assert.That(asset, Is.Not.Null, "o setup cria o Balance_GDD_v7");

            var rules = asset.ToRules();
            var economy = asset.ToEconomy();
            var strike = asset.ToStrike();

            Assert.That(rules.CrystalHealthIn(1), Is.EqualTo(1));
            Assert.That(rules.CrystalHealthIn(10), Is.EqualTo(Math.Pow(1.25, 9)).Within(1e-9), "vida 1 × 1,25^(c−1)");
            Assert.That(rules.CrystalRewardIn(10), Is.EqualTo(1.2 * Math.Pow(1.25, 9)).Within(1e-9), "lascas = vida × 1,2 (D-134)");
            Assert.That(rules.Damage, Is.EqualTo(1));
            Assert.That(rules.HitInterval, Is.EqualTo(2));
            Assert.That(rules.PickaxeFormula, Is.EqualTo(PickaxeFormula.LevelScaled), "Picareta pela compra (D-201)");
            Assert.That(rules.PickaxeCaveGrowth, Is.EqualTo(1.2));
            Assert.That(asset.ToRules(pickaxeByCave: true).PickaxeFormula, Is.EqualTo(PickaxeFormula.CaveScaled),
                        "a leitura da D-133, no DBG → Caverna");

            var pickaxe = economy.Get(UpgradeId.Pickaxe);
            Assert.That((pickaxe.CostFormula, pickaxe.BaseCost, pickaxe.CostGrowth, pickaxe.MaxLevel, pickaxe.Effect, pickaxe.CostOffset),
                        Is.EqualTo((CostFormula.DamageScaled, 2.1, 1.2, 49, 0.0, 1.0)), "custo 2,1 × o dano, Dano Base fixo (D-202)");
            Assert.That(pickaxe.CostAt(0), Is.EqualTo(4.2).Within(1e-9), "golpe 2 → próxima compra 4,20");
            var speed = economy.Get(UpgradeId.Speed);
            Assert.That((speed.BaseCost, speed.CostGrowth, speed.MaxLevel, speed.Effect), Is.EqualTo((50.0, 2.0, 19, 0.1)));
            Assert.That(economy.MinHitInterval, Is.EqualTo(0.1));
            var mining = economy.Get(UpgradeId.Mining);
            Assert.That((mining.BaseCost, mining.CostGrowth, mining.MaxLevel, mining.Effect), Is.EqualTo((20.0, 5.0, 10, 1.0)));
            var critical = economy.Get(UpgradeId.Critical);
            Assert.That((critical.BaseCost, critical.CostGrowth, critical.MaxLevel, critical.Effect), Is.EqualTo((50.0, 2.0, 10, 0.05)));
            Assert.That(economy.Get(UpgradeId.DoublePickaxe).IsEnabled, Is.False);
            Assert.That(economy.Get(UpgradeId.Boots).IsEnabled, Is.False);
            var letrado = economy.Get(UpgradeId.Letrado);
            Assert.That((letrado.CostAt(0), letrado.CostAt(99), letrado.Effect), Is.EqualTo((0.0, 0.0, 1.0)), "Mapa: custo zero e sem ×2 (D-128)");
            Assert.That(letrado.MaxLevel, Is.EqualTo(100), "um Mapa por nível; o 100º leva à parede do coração");
            Assert.That(rules.CaveCount, Is.EqualTo(100), "100 níveis (D-129)");
            Assert.That(rules.LevelsPerBiome, Is.EqualTo(10), "um bioma a cada 10 níveis (D-129)");
            Assert.That(rules.LetradoBreaks, Is.EqualTo(100), "a trava dos 100 cristais continua");

            Assert.That(strike.CritChance, Is.EqualTo(0));
            Assert.That(strike.TapCritChance, Is.EqualTo(0.05));
            Assert.That(strike.TapFraction, Is.EqualTo(0.33));
        }
    }
}
