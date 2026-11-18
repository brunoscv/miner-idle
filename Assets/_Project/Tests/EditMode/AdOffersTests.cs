using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Persistence;
using NUnit.Framework;
using UnityEditor;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9e E2: os anúncios do Boost (GDD v8.2 §3.5; D-244, D-246, D-250, D-251, D-254, D-284, D-287, D-290). Curto, médio
    /// e grande dão ×2/×3/×4 em dano, lascas e dano do crítico e +10/15/20% de crítico por 20/40/80 min; o mesmo de novo
    /// renova; tipos diferentes juntos, vale o maior, e o menor volta quando o maior acaba; anúncio × carta do raro. O 4º
    /// deixa as cartas no tier 2 por 1 h. No offline: as horas extras dos ativos ao fechar (teto 8 h), o bônus para o
    /// estagiário pelo tempo que faltava e um raro a cada 60 s com carta sorteada.
    /// </summary>
    public class AdOffersTests
    {
        const double Hour = 3600;
        const string GddV8Path = "Assets/_Project/Data/Balance_GDD_v8.asset";

        static readonly AdOffers Ads = new AdOffers(new[]
        {
            new AdOffer(AdKind.Short, 2, 0.10, 20 * 60, 1 * Hour),
            new AdOffer(AdKind.Medium, 3, 0.15, 40 * 60, 2 * Hour),
            new AdOffer(AdKind.Big, 4, 0.20, 80 * 60, 4 * Hour),
        }, Hour, 8 * Hour);

        static readonly RareCardRule[] Tier1 =
        {
            new RareCardRule(RareCard.Damage, 2, 30), new RareCardRule(RareCard.Shards, 2, 30),
            new RareCardRule(RareCard.Critical, 0.10, 30), new RareCardRule(RareCard.CritDamage, 2, 30),
        };

        static readonly RareCardRule[] Tier2 =
        {
            new RareCardRule(RareCard.Damage, 4, 30), new RareCardRule(RareCard.Shards, 4, 30),
            new RareCardRule(RareCard.Critical, 0.20, 30), new RareCardRule(RareCard.CritDamage, 4, 30),
        };

        /// <summary>Vida 1 em todo andar (um golpe por cristal: o dano não muda o tempo); offline 4 h, 6 h depois do 20.</summary>
        static MiningRules Rules() =>
            new MiningRules(
                initialCrystals: 1, firstCrystalDistance: 3, maxCrystals: 12, spawnInterval: 1e9, minCrystalSpacing: 2,
                spawnDistance: 12, crystalHealth: 1, crystalReward: 0.2, damage: 1, hitInterval: 2, walkSpeed: 4,
                miningRange: 1, caveCount: 100, caveHealthGrowth: 1, floorBreaks: 10,
                arrivalStrike: ArrivalStrike.Immediate, pickaxeFormula: PickaxeFormula.LevelScaled, levelsPerBiome: 10,
                offlineMaxSeconds: 4 * Hour, rewardFormula: RewardFormula.FloorScaled, offlineDeepMaxSeconds: 6 * Hour,
                offlineDeepFloor: 20, autoAdvanceFloor: 5, internFloor: 5, rareFloor: 10);

        static EconomyRules Economy() => new EconomyRules(
            pickaxe: new UpgradeDefinition(2, 2, 49, 1, CostFormula.Linear),
            speed: new UpgradeDefinition(50, 2, 19, 0.1),
            mining: new UpgradeDefinition(20, 5, 10, 1),
            doublePickaxe: new UpgradeDefinition(5000, 1, 0, 2),
            boots: new UpgradeDefinition(0, 1, 0, 0),
            minHitInterval: 0.1,
            intern: new UpgradeDefinition(500, 1, 1, 1),
            internPickaxe: new UpgradeDefinition(50, 4, 19, 5),
            internSpeed: new UpgradeDefinition(50, 5, 19, 0.1),
            internMining: new UpgradeDefinition(50, 1.2, 10, 1),
            internPickaxeGrowth: 2);

        static StrikeRules Strike(IEnumerable<RareCardRule> cards = null) =>
            new StrikeRules(0, 1.5, 0.33, 1, rare: new RareRules(0, 0, 30, cards ?? Tier1, 60, cards == null ? Tier2 : null),
                            ads: Ads);

        static readonly UpgradeDefinition Off = new UpgradeDefinition(0, 1, 0, 0);

        static PermanentRules PermanentRules() => new PermanentRules(
            10, new RedShardTable(10, 10, 5, 50, 10, 1), 0, 10,
            Off, Off, Off, Off, Off, Off, Off, Off, Off, new UpgradeDefinition(500, 1, 1, 1));

        /// <summary>No andar <paramref name="floor"/>, com o estagiário, o recorde <paramref name="deepest"/>, sem o automático.</summary>
        static MiningSimulation Sim(int floor = 10, int deepest = 10, StrikeRules strike = null, int golden = 0,
                                    IEnumerable<ActiveBonus> bonuses = null)
        {
            var levels = new Dictionary<UpgradeId, int> { [UpgradeId.Intern] = 1 };
            var state = GameState.Restore(0, 0, 0, id => levels.TryGetValue(id, out int l) ? l : 0, Economy(), 100, floor,
                                          bonuses: bonuses);
            var permanent = PermanentState.Restore(0, 1, id => id == PermanentId.GoldenRare ? golden : 0, PermanentRules(), deepest);
            var sim = new MiningSimulation(Rules(), new SeededRandom(3), state, Economy(), strike ?? Strike(),
                                           new Permanents(PermanentRules(), permanent), new SeededRandom(5));
            sim.AutoAdvance = false;
            return sim;
        }

        static double Value(MiningSimulation sim, AdKind kind, BonusKind bonus) =>
            sim.Bonuses.All.Single(b => b.Source == BonusSource.Ad && b.Tier == (int)kind && b.Kind == bonus).Value;

        // ── Assistir, renovar, o maior vale (D-244, D-251) ──────────────────────────────────────────────────────

        [Test]
        public void TheShortAd_DoublesDamageShardsAndCritDamage_PlusTenPercent_For20Minutes()
        {
            var sim = Sim();
            double damage = sim.Stats.Damage, reward = sim.Stats.RewardMultiplier;
            var watched = new List<AdKind>();
            sim.AdWatched += watched.Add;

            Assert.That(sim.WatchAd(AdKind.Short), Is.True);
            Assert.That(watched, Is.EqualTo(new[] { AdKind.Short }));
            Assert.That(Value(sim, AdKind.Short, BonusKind.DamageMultiplier), Is.EqualTo(2));
            Assert.That(Value(sim, AdKind.Short, BonusKind.ShardsMultiplier), Is.EqualTo(2));
            Assert.That(Value(sim, AdKind.Short, BonusKind.CritDamageMultiplier), Is.EqualTo(2));
            Assert.That(Value(sim, AdKind.Short, BonusKind.CritChance), Is.EqualTo(0.10));
            Assert.That(sim.AdSecondsLeft(AdKind.Short), Is.EqualTo(1200));
            Assert.That(sim.Stats.Damage, Is.EqualTo(2 * damage).Within(1e-9));
            Assert.That(sim.Stats.RewardMultiplier, Is.EqualTo(2 * reward).Within(1e-9));

            sim.Tick(1200);
            Assert.That(sim.AdSecondsLeft(AdKind.Short), Is.EqualTo(0));
            Assert.That(sim.Bonuses.Count, Is.EqualTo(0), "acabou");
            Assert.That(sim.Stats.Damage, Is.EqualTo(damage).Within(1e-9));
        }

        [Test]
        public void TheSameAdAgain_RenewsTheTime_WithoutAddingTheEffect()
        {
            var sim = Sim();
            sim.WatchAd(AdKind.Medium);
            sim.Tick(1000);
            Assert.That(sim.AdSecondsLeft(AdKind.Medium), Is.EqualTo(1400).Within(1e-6));
            Assert.That(sim.WatchAd(AdKind.Medium), Is.True, "com ele ativo, pode de novo (D-244)");
            Assert.That(sim.AdSecondsLeft(AdKind.Medium), Is.EqualTo(2400), "renova");
            Assert.That(sim.Bonuses.DamageMultiplier, Is.EqualTo(3), "sem somar");
            Assert.That(sim.Bonuses.Count, Is.EqualTo(4), "um bônus por atributo, sem repetir");
        }

        [Test]
        public void DifferentAds_TheBiggestCounts_AndTheSmallerComesBackWhenItEnds()
        {
            var sim = Sim();
            sim.WatchAd(AdKind.Big);
            sim.Tick(80 * 60 - 100); // faltam 100 s do grande
            sim.WatchAd(AdKind.Short);
            Assert.That(sim.Bonuses.DamageMultiplier, Is.EqualTo(4), "vale o maior (D-251), não ×8");
            Assert.That(sim.Bonuses.ShardsMultiplier, Is.EqualTo(4));
            Assert.That(sim.Bonuses.CritDamageMultiplier, Is.EqualTo(4));
            Assert.That(sim.Bonuses.CritChance, Is.EqualTo(0.20).Within(1e-12), "o maior, não a soma");

            sim.Tick(100);
            Assert.That(sim.AdSecondsLeft(AdKind.Big), Is.EqualTo(0));
            Assert.That(sim.Bonuses.DamageMultiplier, Is.EqualTo(2), "o curto, que contava por baixo, volta");
            Assert.That(sim.AdSecondsLeft(AdKind.Short), Is.EqualTo(1100).Within(1e-6));
        }

        [Test]
        public void RareCardAndAd_Multiply_AndTheCritChanceAdds()
        {
            var sim = Sim();
            sim.WatchAd(AdKind.Big);
            sim.WatchAd(AdKind.Short);
            sim.GrantRareCard(RareCard.Damage);
            sim.GrantRareCard(RareCard.Critical);
            sim.GrantRareCard(RareCard.CritDamage);
            Assert.That(sim.Bonuses.DamageMultiplier, Is.EqualTo(8), "×2 da carta × o ×4 do grande (D-287)");
            Assert.That(sim.Bonuses.CritDamageMultiplier, Is.EqualTo(8));
            Assert.That(sim.Bonuses.CritChance, Is.EqualTo(0.30).Within(1e-12), "+10% da carta + o maior dos anúncios, +20%");
        }

        [Test]
        public void TheAd_AlsoCountsForTheIntern_ButNotTheCritical()
        {
            var sim = Sim();
            double intern = sim.InternStats.Damage;
            sim.WatchAd(AdKind.Big);
            Assert.That(sim.Bonuses.InternDamageMultiplier, Is.EqualTo(4), "D-246");
            Assert.That(sim.InternStats.Damage, Is.EqualTo(4 * intern).Within(1e-9));
        }

        [Test]
        public void ShortAdsDebug_DividesTheDurationBy60()
        {
            var sim = Sim();
            sim.AdDurationDivisor = 60;
            sim.WatchAd(AdKind.Big);
            Assert.That(sim.AdSecondsLeft(AdKind.Big), Is.EqualTo(80));
            sim.WatchAd(AdKind.RareTier2);
            Assert.That(sim.AdSecondsLeft(AdKind.RareTier2), Is.EqualTo(60));
            sim.AdDurationDivisor = 1;
            sim.WatchAd(AdKind.Short);
            Assert.That(sim.AdSecondsLeft(AdKind.Short), Is.EqualTo(1200));
            Assert.Throws<ArgumentOutOfRangeException>(() => sim.AdDurationDivisor = 0.5);
        }

        [Test]
        public void WithoutAdsInTheBalance_NothingToWatch()
        {
            var sim = Sim(strike: new StrikeRules(0, 1.5, 0.33, 1));
            foreach (AdKind kind in Enum.GetValues(typeof(AdKind)))
            {
                Assert.That(sim.CanWatch(kind), Is.False);
                Assert.That(sim.WatchAd(kind), Is.False);
            }
        }

        // ── O tier 2 por 1 h (D-250) ────────────────────────────────────────────────────────────────────────────

        [Test]
        public void Tier2Ad_FromFloor10_GivesTier2ForAnHour()
        {
            Assert.That(Sim(floor: 5, deepest: 9).CanWatch(AdKind.RareTier2), Is.False, "antes do andar 10, não aparece");

            var sim = Sim();
            Assert.That(sim.CanWatch(AdKind.RareTier2), Is.True);
            Assert.That(sim.WatchAd(AdKind.RareTier2), Is.True);
            Assert.That(sim.RareTier, Is.EqualTo(2));
            Assert.That(sim.AdSecondsLeft(AdKind.RareTier2), Is.EqualTo(Hour));
            sim.GrantRareCard(RareCard.Shards);
            Assert.That(sim.Bonuses.ShardsMultiplier, Is.EqualTo(4), "a carta sai no tier 2");
            Assert.That(sim.Bonuses.DamageMultiplier, Is.EqualTo(1), "o anúncio do tier 2 não dá bônus próprio");

            sim.Tick(Hour);
            Assert.That(sim.RareTier, Is.EqualTo(1), "acabou a hora");
        }

        [Test]
        public void Tier2Ad_DisappearsWithTheGoldenRare()
        {
            var sim = Sim(golden: 1);
            Assert.That(sim.RareTier, Is.EqualTo(2));
            Assert.That(sim.CanWatch(AdKind.RareTier2), Is.False, "comprado o tier 2, o 4º anúncio some (D-250)");
            Assert.That(sim.CanWatch(AdKind.Short), Is.True);
        }

        // ── O offline (D-244, D-246, D-254, D-284) ──────────────────────────────────────────────────────────────

        [Test]
        public void ExtraHours_FromTheAdsActiveWhenClosing_UpTo8Hours()
        {
            var sim = Sim();
            Assert.That(sim.OfflineMaxSeconds, Is.EqualTo(4 * Hour));
            sim.WatchAd(AdKind.Short);
            Assert.That(sim.OfflineMaxSeconds, Is.EqualTo(5 * Hour), "+1 h");
            sim.WatchAd(AdKind.Medium);
            Assert.That(sim.OfflineMaxSeconds, Is.EqualTo(7 * Hour), "somam (D-244)");
            sim.WatchAd(AdKind.Big);
            Assert.That(sim.OfflineMaxSeconds, Is.EqualTo(8 * Hour), "4 + 7 = 11 h, teto de 8 h");
            sim.WatchAd(AdKind.RareTier2);
            Assert.That(sim.OfflineMaxSeconds, Is.EqualTo(8 * Hour), "o do tier 2 não dá horas");

            var deep = Sim(floor: 20, deepest: 20);
            Assert.That(deep.OfflineMaxSeconds, Is.EqualTo(6 * Hour));
            deep.WatchAd(AdKind.Short);
            Assert.That(deep.OfflineMaxSeconds, Is.EqualTo(7 * Hour));
            deep.WatchAd(AdKind.Big);
            Assert.That(deep.OfflineMaxSeconds, Is.EqualTo(8 * Hour), "o teto de 8 h vale também depois do 20");

            var report = deep.ApplyOffline(10 * Hour);
            Assert.That(report.SecondsCounted, Is.EqualTo(8 * Hour), "conta os ativos na hora que fechou (D-254)");
            Assert.That(report.MaxSeconds, Is.EqualTo(8 * Hour));
            Assert.That(deep.AdSecondsLeft(AdKind.Big), Is.EqualTo(0), "o tempo dele correu fora");
            Assert.That(deep.OfflineMaxSeconds, Is.EqualTo(6 * Hour));
        }

        [Test]
        public void Offline_TheAdCountsForTheIntern_ForTheTimeThatWasLeft()
        {
            // Sem raros (andar 5, recorde 9): só o anúncio muda a conta.
            double Shards(params AdKind[] ads)
            {
                var sim = Sim(floor: 5, deepest: 9);
                foreach (var ad in ads) sim.WatchAd(ad);
                sim.Tick(600); // o anúncio já correu 10 min antes de fechar
                return sim.ApplyOffline(Hour).Shards;
            }

            double plain = Shards();
            Assert.That(plain, Is.GreaterThan(0));
            Assert.That(Shards(AdKind.Big), Is.EqualTo(4 * plain).Within(1).Percent, "o grande cobre a hora toda: ×4");
            // O curto: faltavam 10 min, ×2 neles; a outra parte da hora, ×1 → (2·10 + 50) / 60.
            Assert.That(Shards(AdKind.Short), Is.EqualTo(plain * 70 / 60).Within(1).Percent);
            Assert.That(Shards(AdKind.Short, AdKind.Big), Is.EqualTo(4 * plain).Within(1).Percent, "entre anúncios, o maior");
        }

        [Test]
        public void Offline_OneRareEvery60Seconds_WithARandomCardForTheIntern()
        {
            var plainSim = Sim(floor: 9, deepest: 9);
            double plain = plainSim.ApplyOffline(600).Shards;
            Assert.That(plainSim.ApplyOffline(600).Rares, Is.EqualTo(0), "antes do andar 10, nenhum raro");

            // Só cartas que mudam o estagiário: lascas ×2 e dano ×2 (dano não muda o tempo aqui: um golpe por cristal).
            var shardsOnly = Sim(floor: 9, strike: Strike(new[] { Tier1[0], Tier1[1] }));
            var report = shardsOnly.ApplyOffline(600);
            Assert.That(report.Rares, Is.EqualTo(10), "10 min, um a cada 60 s (D-284)");
            Assert.That(report.Shards, Is.GreaterThan(plain), "alguma carta de lascas saiu");
            Assert.That(report.Shards, Is.LessThanOrEqualTo(plain * 1.5 + 1e-9), "cada carta vale 30 s de cada 60");
            Assert.That(shardsOnly.State.RareTimer, Is.EqualTo(0), "o contador da run não anda fora (D-283)");
            Assert.That(shardsOnly.Bonuses.Count, Is.EqualTo(0), "as cartas do offline são só dele, lá fora");

            // Só cartas de crítico: nada para ele (D-167).
            var critOnly = Sim(floor: 9, strike: Strike(new[] { Tier1[2], Tier1[3] }));
            var crit = critOnly.ApplyOffline(600);
            Assert.That(crit.Rares, Is.EqualTo(10));
            Assert.That(crit.Shards, Is.EqualTo(plain).Within(1e-9));
        }

        [Test]
        public void Offline_RaresNeedTheIntern()
        {
            var state = GameState.Restore(0, 0, 0, _ => 0, Economy(), 100, 10);
            var permanent = PermanentState.Restore(0, 1, _ => 0, PermanentRules(), 10);
            var sim = new MiningSimulation(Rules(), new SeededRandom(3), state, Economy(), Strike(),
                                           new Permanents(PermanentRules(), permanent), new SeededRandom(5));
            var report = sim.ApplyOffline(600);
            Assert.That(report.Hired, Is.False);
            Assert.That(report.Rares, Is.EqualTo(0));
        }

        // ── O save e o balanço ──────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void Save_KeepsTheActiveAds_AndTheirTime()
        {
            string dir = Path.Combine(Path.GetTempPath(), "MinersIdleAdsSave_" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new FileSaveStore(dir);
                var sim = Sim();
                sim.WatchAd(AdKind.Big);
                sim.WatchAd(AdKind.Short);
                sim.WatchAd(AdKind.RareTier2);
                sim.Tick(300);
                new SaveService(store).Save(sim.State);

                var loaded = new SaveService(store).Load(Economy(), out var status, 100);
                Assert.That(status, Is.EqualTo(SaveLoadStatus.Loaded));
                var back = Sim(bonuses: loaded.Bonuses.All);
                foreach (var kind in new[] { AdKind.Big, AdKind.Short, AdKind.RareTier2 })
                    Assert.That(back.AdSecondsLeft(kind), Is.EqualTo(sim.AdSecondsLeft(kind)).Within(1e-9), kind.ToString());
                Assert.That(back.Bonuses.DamageMultiplier, Is.EqualTo(4));
                Assert.That(back.RareTier, Is.EqualTo(2));
                Assert.That(back.OfflineMaxSeconds, Is.EqualTo(8 * Hour));
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
            }
        }

        [Test]
        public void TheV8Balance_HasTheThreeAds_TheTier2Ad_AndNoSpeedAd()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV8Path);
            Assert.That(asset, Is.Not.Null, "rode o setup do projeto");
            var strike = asset.ToStrike();
            Assert.That(strike.Ad.IsEnabled, Is.False, "o AD de velocidade saiu (D-290)");
            var ads = strike.Ads;
            Assert.That(ads.All.Select(a => a.Kind), Is.EqualTo(new[] { AdKind.Short, AdKind.Medium, AdKind.Big }));
            Assert.That(ads.All.Select(a => a.Multiplier), Is.EqualTo(new[] { 2.0, 3, 4 }));
            Assert.That(ads.All.Select(a => a.CritChance), Is.EqualTo(new[] { 0.10, 0.15, 0.20 }));
            Assert.That(ads.All.Select(a => a.Seconds), Is.EqualTo(new[] { 1200.0, 2400, 4800 }));
            Assert.That(ads.All.Select(a => a.OfflineSeconds), Is.EqualTo(new[] { Hour, 2 * Hour, 4 * Hour }));
            Assert.That(ads.Tier2Seconds, Is.EqualTo(Hour));
            Assert.That(ads.OfflineCapSeconds, Is.EqualTo(8 * Hour));
        }
    }
}
