using System;
using System.Collections.Generic;
using System.IO;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Persistence;
using NUnit.Framework;
using UnityEditor;

namespace MinersIdle.Tests
{
    /// <summary>
    /// E1 do M7: o estagiário no núcleo (GDD v7 §4.1, §4.2, §5; D-164…D-172). Contratar por 500, a árvore dele só depois,
    /// os golpes no mesmo cristal do anão (sem alvo duplicado, uma quebra paga uma vez), Treinar e Promoção, o descanso, o
    /// save e o <c>Balance_GDD_v7</c>. Os números estão escritos à mão, como no GDD.
    /// </summary>
    public class InternTests
    {
        const string GddV7Path = "Assets/_Project/Data/Balance_GDD_v7.asset";
        const string GddV5Path = "Assets/_Project/Data/Balance_GDD_v5.asset";

        /// <summary>Uma fila parada (nada nasce), anão de dano 1 a cada 2,0 s (o intervalo base do v7).</summary>
        static MiningRules Rules(double health = 1000, ArrivalStrike arrival = ArrivalStrike.Wait, int caves = 1) =>
            new MiningRules(
                initialCrystals: 0, firstCrystalDistance: 3, maxCrystals: 12, spawnInterval: 1e9, minCrystalSpacing: 2,
                spawnDistance: 12, crystalHealth: health, crystalReward: 3, damage: 1, hitInterval: 2, walkSpeed: 4,
                miningRange: 1, caveCount: caves, letradoBreaks: 0, arrivalStrike: arrival);

        /// <summary>A economia do v7 que importa aqui: Mineração e Velocidade do anão e o estagiário inteiro.</summary>
        static EconomyRules V7(bool intern = true) => new EconomyRules(
            pickaxe: new UpgradeDefinition(2, 2, 49, 1, CostFormula.Linear),
            speed: new UpgradeDefinition(50, 2, 19, 0.1),
            mining: new UpgradeDefinition(20, 5, 10, 1),
            doublePickaxe: new UpgradeDefinition(5000, 1, 0, 2),
            boots: new UpgradeDefinition(0, 1, 0, 0),
            minHitInterval: 0.1,
            letrado: new UpgradeDefinition(0, 1, 100, 1),
            critical: new UpgradeDefinition(50, 2, 10, 0.05),
            intern: intern ? new UpgradeDefinition(500, 1, 1, 1) : null,
            internPickaxe: intern ? new UpgradeDefinition(50, 4, 19, 5) : null,
            internSpeed: intern ? new UpgradeDefinition(50, 5, 19, 0.1) : null,
            internMining: intern ? new UpgradeDefinition(50, 1.2, 10, 1) : null,
            internPickaxeGrowth: 2);

        static UpgradeDefinition Line(int max, double effect) => new UpgradeDefinition(10, 2, max, effect, CostFormula.OffsetExponential);

        /// <summary>GDD v7 §4.2, com as duas do estagiário.</summary>
        static PermanentRules V7Permanents() => new PermanentRules(
            10, new RedShardTable(10, 10, 5, 50, 10, 1),
            Line(10, 10), Line(20, 0.1), Line(20, 0.5), Line(10, 0.05), Line(10, 1),
            Line(10, 0.1), new UpgradeDefinition(50, 1, 1, 1));

        static MiningSimulation Sim(MiningRules rules = null, GameState state = null, PermanentState permanent = null,
                                    EconomyRules economy = null) =>
            new MiningSimulation(rules ?? Rules(), new SeededRandom(7), state, economy ?? V7(),
                                 new StrikeRules(0, 2, 0.33, 1, tapCritChance: 0.05),
                                 new Permanents(V7Permanents(), permanent ?? new PermanentState()));

        static GameState With(params (UpgradeId id, int level)[] levels)
        {
            var map = new Dictionary<UpgradeId, int>();
            foreach (var (id, level) in levels) map[id] = level;
            return GameState.Restore(0, 0, 0, id => map.TryGetValue(id, out int l) ? l : 0, V7());
        }

        static InternStats Stats(GameState state, Permanents permanents = null) =>
            InternStats.Compute(Rules(), V7(), state, permanents);

        // ── Custo e efeito (§5.4, D-164, D-165) ─────────────────────────────────────────────────────────────

        [Test]
        public void Hiring_CostsFiveHundred_Once()
        {
            var sim = Sim();
            sim.AddShards(1200);
            Assert.That(sim.IsInternHired, Is.False);
            Assert.That(sim.Preview(UpgradeId.Intern).NextCost, Is.EqualTo(500));

            Assert.That(sim.Purchase(UpgradeId.Intern), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(sim.State.Shards, Is.EqualTo(700));
            Assert.That(sim.IsInternHired, Is.True);
            Assert.That(sim.Purchase(UpgradeId.Intern), Is.EqualTo(PurchaseResult.MaxLevel), "compra única");
            Assert.That(sim.Preview(UpgradeId.Intern).State, Is.EqualTo(UpgradeButtonState.Maxed));
        }

        [Test]
        public void HisTree_IsLocked_UntilHiring()
        {
            var sim = Sim();
            sim.AddShards(1e6);
            foreach (var id in new[] { UpgradeId.InternPickaxe, UpgradeId.InternSpeed, UpgradeId.InternMining })
            {
                Assert.That(sim.Purchase(id), Is.EqualTo(PurchaseResult.Locked), id.ToString());
                Assert.That(sim.Preview(id).State, Is.EqualTo(UpgradeButtonState.Locked), id.ToString());
            }

            sim.Purchase(UpgradeId.Intern);
            double before = sim.State.Shards;
            Assert.That(sim.Purchase(UpgradeId.InternPickaxe), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(sim.State.Shards, Is.EqualTo(before - 50), "a 1ª Picareta dele custa 50");
            Assert.That(sim.InternStats.Damage, Is.EqualTo(10), "e leva o dano de 5 a 10 na hora (nível 2, D-205)");
        }

        [Test]
        public void Pickaxe_CostsFiftyTimesFour_AndHitsFiveTimesTwo()
        {
            var economy = V7().Get(UpgradeId.InternPickaxe);
            Assert.That(economy.CostAt(0), Is.EqualTo(50));
            Assert.That(economy.CostAt(1), Is.EqualTo(200));
            Assert.That(economy.CostAt(2), Is.EqualTo(800));
            Assert.That(economy.MaxLevel, Is.EqualTo(19), "19 compras = nível 20 (D-207)");

            Assert.That(Stats(With((UpgradeId.Intern, 1))).Damage, Is.EqualTo(5), "D-205: nível 1 é o valor-base, 5 sem compra");
            Assert.That(Stats(With((UpgradeId.Intern, 1))).HitInterval, Is.EqualTo(2), "e o intervalo base");
            Assert.That(Stats(With((UpgradeId.Intern, 1), (UpgradeId.InternPickaxe, 1))).Damage, Is.EqualTo(10), "nível 2");
            Assert.That(Stats(With((UpgradeId.Intern, 1), (UpgradeId.InternPickaxe, 2))).Damage, Is.EqualTo(20), "nível 3");
            Assert.That(Stats(With((UpgradeId.Intern, 1), (UpgradeId.InternPickaxe, 3))).Damage, Is.EqualTo(40), "nível 4");
            Assert.That(Stats(With((UpgradeId.Intern, 1), (UpgradeId.InternPickaxe, 19))).Damage, Is.EqualTo(5 * Math.Pow(2, 19)),
                        "19 compras: nível 20, o limite (D-207)");
        }

        [Test]
        public void Damage_DoesNotGrowWithTheCave()
        {
            var sim = Sim(Rules(caves: 100));
            sim.HireIntern();
            sim.SkipToCave(30);
            Assert.That(sim.InternStats.Damage, Is.EqualTo(5), "§5.3: o dano dele vem só das melhorias dele");
        }

        [Test]
        public void Speed_CostsFiftyTimesFive_AndStopsAtPointOne_After19()
        {
            var line = V7().Get(UpgradeId.InternSpeed);
            Assert.That(line.CostAt(0), Is.EqualTo(50));
            Assert.That(line.CostAt(1), Is.EqualTo(250));
            Assert.That(line.CostAt(2), Is.EqualTo(1250));
            Assert.That(line.MaxLevel, Is.EqualTo(19), "D-165: 2,0 − 0,1 × 19 = 0,1");

            Assert.That(Stats(With((UpgradeId.Intern, 1), (UpgradeId.InternSpeed, 1))).HitInterval, Is.EqualTo(1.9).Within(1e-9));
            Assert.That(Stats(With((UpgradeId.Intern, 1), (UpgradeId.InternSpeed, 19))).HitInterval, Is.EqualTo(0.1).Within(1e-9));
        }

        [Test]
        public void Mining_CostsFiftyTimesOnePointTwo_AndCountsOnlineToo()
        {
            var line = V7().Get(UpgradeId.InternMining);
            Assert.That(line.CostAt(0), Is.EqualTo(50));
            Assert.That(line.CostAt(1), Is.EqualTo(60).Within(1e-9));
            Assert.That(line.CostAt(2), Is.EqualTo(72).Within(1e-9));
            Assert.That(line.MaxLevel, Is.EqualTo(10), "+1000%");
            Assert.That(Stats(With((UpgradeId.Intern, 1), (UpgradeId.InternMining, 10))).OfflineRewardMultiplier, Is.EqualTo(11));

            // D-209: no jogo aberto, a Mineração dele entra somada na do cristal (revê a D-166).
            var sim = Sim(Rules(health: 4), With((UpgradeId.Intern, 1), (UpgradeId.InternMining, 10),
                                                    (UpgradeId.InternPickaxe, 1)));
            sim.SpawnCrystalAt(1);
            sim.Tick(2.001); // os dois batem no 2,0 s: o anão tira 1, o estagiário 10 e quebra
            Assert.That(sim.State.Breaks, Is.EqualTo(1));
            Assert.That(sim.State.Shards, Is.EqualTo(3 * 11).Within(1e-9), "3 lascas × (1 + a Mineração dele, +1000%)");
        }

        /// <summary>
        /// D-209: com o jogo aberto, a Mineração do estagiário soma com a do anão e com o Coração Ardente, em todo cristal,
        /// não importa quem dá o último golpe. Cristal de 1 lasca, Mineração do anão 1 e dele 1: 1 + 1 + 1 = 3.
        /// </summary>
        [TestCase(1, 0, 1, 3.0)]
        [TestCase(1, 1, 1, 3.5)]
        [TestCase(10, 20, 10, 31.0)]
        [TestCase(0, 0, 3, 4.0)]
        public void InternMining_AddsToTheShards_Online(int dwarfMining, int heart, int internMining, double multiplier)
        {
            var permanent = PermanentState.Restore(0, 1, id => id == PermanentId.BurningHeart ? heart : 0, V7Permanents());
            var sim = Sim(Rules(health: 1e9), With((UpgradeId.Intern, 1), (UpgradeId.Mining, dwarfMining),
                                                   (UpgradeId.InternMining, internMining)), permanent);
            Assert.That(sim.Stats.RewardMultiplier, Is.EqualTo(multiplier).Within(1e-9));
        }

        [Test]
        public void InternMining_BreakByTheDwarf_AlsoPaysIt()
        {
            // O anão (dano 1) quebra sozinho um cristal de vida 1 antes do 1º golpe dele: a Mineração dele vale igual.
            var sim = Sim(Rules(health: 1), With((UpgradeId.Intern, 1), (UpgradeId.Mining, 1), (UpgradeId.InternMining, 1)));
            int internHits = 0;
            sim.InternHitLanded += _ => internHits++;
            sim.SpawnCrystalAt(1);
            sim.Tick(2.001);
            Assert.That(sim.State.Breaks, Is.GreaterThanOrEqualTo(1));
            Assert.That(sim.State.Shards, Is.EqualTo(3 * 3 * sim.State.Breaks).Within(1e-9), "3 lascas × (1 + 1 + 1)");
        }

        [Test]
        public void InternMining_WithoutTheIntern_DoesNotCount()
        {
            var sim = Sim(state: With((UpgradeId.Mining, 1)));
            Assert.That(sim.IsInternHired, Is.False);
            Assert.That(sim.Stats.RewardMultiplier, Is.EqualTo(2), "só a Mineração do anão");
        }

        [Test]
        public void Preview_PromisesWhatThePurchaseDelivers()
        {
            var sim = Sim();
            sim.AddShards(1e9);
            sim.Purchase(UpgradeId.Intern);
            foreach (var (id, read) in new (UpgradeId, Func<InternStats, double>)[]
                     {
                         (UpgradeId.InternPickaxe, s => s.Damage),
                         (UpgradeId.InternSpeed, s => s.HitInterval),
                         (UpgradeId.InternMining, s => s.OfflineRewardMultiplier),
                     })
            {
                var before = sim.Preview(id);
                Assert.That(before.CurrentValue, Is.EqualTo(read(sim.InternStats)).Within(1e-9), id.ToString());
                sim.Purchase(id);
                Assert.That(read(sim.InternStats), Is.EqualTo(before.NextValue).Within(1e-9), id + ": o \"para\" da prévia");
            }
        }

        // ── No cristal do anão (§5.2, D-168) ───────────────────────────────────────────────────────────────────

        [Test]
        public void Hits_TheDwarfsTarget_OnHisOwnClock()
        {
            var sim = Sim();
            var crystal = sim.SpawnCrystalAt(1); // já no alcance
            sim.Tick(0.5);
            sim.HireIntern(); // nasce no cristal do anão; no modo Wait, o 1º golpe dele 2 s depois
            var hits = new List<(double at, Crystal crystal, bool critical)>();
            sim.InternHitLanded += hit => hits.Add((sim.Elapsed, hit.Crystal, hit.IsCritical));

            sim.Tick(6.5);

            Assert.That(hits.Count, Is.EqualTo(3), "2,5 · 4,5 · 6,5");
            Assert.That(hits[0].at, Is.EqualTo(2.5).Within(1e-9));
            foreach (var hit in hits)
            {
                Assert.That(hit.crystal, Is.SameAs(crystal), "o mesmo cristal do anão");
                Assert.That(hit.critical, Is.False, "D-167: sem crítico");
            }
            Assert.That(sim.InternHits, Is.EqualTo(3));
            Assert.That(crystal.Health, Is.EqualTo(1000 - 3 - 3 * 5).Within(1e-9), "3 golpes do anão (2, 4, 6) e 3 dele, de 5 (D-205)");
        }

        [Test]
        public void Hired_WithTheStrikeOnArrival_HitsRightAway()
        {
            var sim = Sim(Rules(arrival: ArrivalStrike.Immediate));
            sim.SpawnCrystalAt(1);
            sim.Tick(0.5);
            int hits = 0;
            sim.InternHitLanded += _ => hits++;
            sim.HireIntern();
            sim.Tick(0);
            Assert.That(hits, Is.EqualTo(1), "D-168: o mesmo modo do anão (golpe na chegada)");
        }

        [Test]
        public void SameInstant_TheDwarfBreaks_AndTheInternDoesNotHitTheFallenCrystal()
        {
            var sim = Sim(Rules(health: 1), With((UpgradeId.Intern, 1)));
            var first = sim.SpawnCrystalAt(1);
            var second = sim.SpawnCrystalAt(9);
            var internTargets = new List<Crystal>();
            sim.InternHitLanded += hit => internTargets.Add(hit.Crystal);
            int broken = 0;
            sim.CrystalBroken += _ => broken++;

            sim.Tick(2.0001); // o anão e o estagiário vencem no mesmo instante; o anão primeiro

            Assert.That(broken, Is.EqualTo(1));
            Assert.That(internTargets, Has.No.Member(first), "não bate no que caiu");
            Assert.That(sim.State.Breaks, Is.EqualTo(1));
            Assert.That(sim.State.Shards, Is.EqualTo(3), "paga uma vez (D-95)");
            Assert.That(sim.Miner.Target, Is.SameAs(second), "os dois seguem juntos para o próximo");
        }

        [Test]
        public void InternBreak_PaysOnce_CountsForTheMap_AndBothRun()
        {
            var sim = Sim(Rules(health: 4), With((UpgradeId.Intern, 1), (UpgradeId.InternPickaxe, 1)));
            sim.SpawnCrystalAt(1);
            sim.SpawnCrystalAt(9);
            Crystal brokenBy = null;
            sim.InternHitLanded += hit => { if (hit.Crystal.Health <= 0) brokenBy = hit.Crystal; };

            sim.Tick(2.0001); // anão 1 (sobra 3), estagiário 5: quebra

            Assert.That(brokenBy, Is.Not.Null, "o golpe dele deu a quebra");
            Assert.That(sim.State.Breaks, Is.EqualTo(1));
            Assert.That(sim.State.CaveBreaks, Is.EqualTo(1), "conta na trava do Mapa (D-95)");
            Assert.That(sim.State.Shards, Is.EqualTo(3));
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Running), "e os dois correm juntos");
        }

        [Test]
        public void HitsTheWall_Too()
        {
            var sim = Sim(Rules(caves: 3), With((UpgradeId.Intern, 1)));
            sim.FillCaveBreaks();
            Assert.That(sim.Purchase(UpgradeId.Letrado), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(sim.PassCave(), Is.True);
            bool hitWall = false;
            sim.InternHitLanded += hit => hitWall |= hit.Crystal.IsWall;

            sim.Tick(10);

            Assert.That(hitWall, Is.True);
        }

        /// <summary>
        /// A matriz do Roadmap, no lugar de casos soltos: uma partida longa com a fila nascendo, toques que quebram cristais
        /// no meio da picaretada (D-126) e compras dos dois. O estagiário só bate com o anão batendo, sempre no alvo dele,
        /// nunca num cristal já quebrado, e cada cristal quebra (e paga) uma vez.
        /// </summary>
        [Test]
        public void LongSession_WithTaps_NeverDoublesATarget_NorHitsAFallenCrystal()
        {
            var rules = new MiningRules(
                initialCrystals: 6, firstCrystalDistance: 3, maxCrystals: 10, spawnInterval: 0.8, minCrystalSpacing: 2,
                spawnDistance: 12, crystalHealth: 6, crystalReward: 1, damage: 1, hitInterval: 2, walkSpeed: 4,
                miningRange: 1, minCrystals: 3, arrivalStrike: ArrivalStrike.Charge, swingRecovery: 0.72, tapReach: 3.9);
            var sim = Sim(rules);
            sim.HireIntern();
            var broken = new HashSet<Crystal>();
            int breaks = 0;
            sim.CrystalBroken += c =>
            {
                Assert.That(broken.Add(c), Is.True, "cada cristal quebra uma vez");
                breaks++;
            };
            sim.InternHitLanded += hit =>
            {
                Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Mining), "só com o anão batendo");
                Assert.That(hit.Crystal, Is.SameAs(sim.Miner.Target), "no alvo do anão");
                Assert.That(broken.Contains(hit.Crystal), Is.False, "nunca num cristal que caiu");
            };

            var random = new System.Random(3);
            for (int step = 0; step < 6000; step++)
            {
                sim.Tick(0.05);
                if (random.NextDouble() < 0.3) sim.Tap();
                if (step % 400 == 0)
                {
                    sim.AddShards(5000);
                    sim.Purchase(UpgradeId.InternPickaxe);
                    sim.Purchase(UpgradeId.InternSpeed);
                    sim.Purchase(UpgradeId.Speed);
                }
            }

            Assert.That(sim.InternHits, Is.GreaterThan(50));
            Assert.That(breaks, Is.EqualTo(sim.State.Breaks));
        }

        // ── Do anão, nada (D-167); Treinar e Promoção (§4.2, D-169) ───────────────────────────────────────────

        [Test]
        public void ForcedCritical_AndMountainBlood_DoNotTouchHim_ButTrainingDoes()
        {
            var permanent = PermanentState.Restore(0, 1, id => id == PermanentId.MountainBlood ? 5 : id == PermanentId.InternTraining ? 3 : 0,
                                                   V7Permanents());
            var sim = Sim(state: With((UpgradeId.Intern, 1), (UpgradeId.InternPickaxe, 1)), permanent: permanent);
            sim.ForceCritical = true;
            sim.SpawnCrystalAt(1);
            var damages = new List<double>();
            sim.InternHitLanded += hit => damages.Add(hit.Damage);

            sim.Tick(2.0001);

            Assert.That(damages, Is.EqualTo(new[] { 10 * 1.3 }).Within(1e-9), "nível 2 (10) × Treinar 3; sem crítico e sem o Sangue");
        }

        [Test]
        public void Promotion_NeedsTrainingTen_AndCostsFifty()
        {
            var permanent = new PermanentState();
            var sim = Sim(permanent: permanent);
            sim.AddRedShards(2000);
            Assert.That(sim.PurchasePermanent(PermanentId.InternPromotion), Is.EqualTo(PurchaseResult.Locked));
            Assert.That(sim.PreviewPermanent(PermanentId.InternPromotion).State, Is.EqualTo(UpgradeButtonState.Locked));

            double spent = 0;
            for (int i = 0; i < 10; i++)
            {
                double cost = sim.PreviewPermanent(PermanentId.InternTraining).NextCost.Value;
                Assert.That(sim.PurchasePermanent(PermanentId.InternTraining), Is.EqualTo(PurchaseResult.Purchased));
                spent += cost;
            }
            Assert.That(spent, Is.EqualTo(1123), "10 + 2^(n−1), 10 compras");
            Assert.That(sim.PreviewPermanent(PermanentId.InternPromotion).State, Is.EqualTo(UpgradeButtonState.Purchasable));
            Assert.That(sim.PurchasePermanent(PermanentId.InternPromotion), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(permanent.RedShards, Is.EqualTo(2000 - 1123 - 50));
            Assert.That(sim.PurchasePermanent(PermanentId.InternPromotion), Is.EqualTo(PurchaseResult.MaxLevel), "compra única");
        }

        [Test]
        public void Rest_FiresTheIntern_UnlessPromoted_AndHisTreeStartsAtZero()
        {
            var economy = V7();
            var permanent = new PermanentState();
            var sim = Sim(state: With((UpgradeId.Intern, 1), (UpgradeId.InternPickaxe, 4)), permanent: permanent);
            sim.Rest(force: true);
            var next = new MiningSimulation(Rules(), new SeededRandom(1), sim.Permanents.NewRun(economy), economy,
                                            permanents: sim.Permanents);
            Assert.That(next.IsInternHired, Is.False, "D-137: descansar zera o estagiário");

            next.PromoteIntern();
            var promoted = new MiningSimulation(Rules(), new SeededRandom(1), next.Permanents.NewRun(economy), economy,
                                                permanents: next.Permanents);
            Assert.That(promoted.IsInternHired, Is.True, "com a Promoção, a run já começa com ele");
            Assert.That(promoted.State.LevelOf(UpgradeId.InternPickaxe), Is.EqualTo(0), "D-169: a árvore dele no zero");
            Assert.That(promoted.InternStats.Damage, Is.EqualTo(5 * 2).Within(1e-9), "nível 1 (5) e o Treinar 10 continua valendo (×2)");
        }

        [Test]
        public void NoInternInTheBalance_NoPromotionEffect()
        {
            var permanent = PermanentState.Restore(0, 1, id => id == PermanentId.InternTraining ? 10 : id == PermanentId.InternPromotion ? 1 : 0,
                                                   V7Permanents());
            var economy = V7(intern: false);
            var run = new Permanents(V7Permanents(), permanent).NewRun(economy);
            Assert.That(run.LevelOf(UpgradeId.Intern), Is.EqualTo(0), "D-172: sem estagiário no balanço, a Promoção não o traz");
            Assert.That(Sim(state: run, economy: economy).IsInternHired, Is.False);
        }

        // ── Atalhos do debug (DBG → Estagiário) ────────────────────────────────────────────────────────────────

        [Test]
        public void DebugShortcuts_HireFireGrantAndPromote()
        {
            var sim = Sim();
            int changes = 0;
            sim.InternChanged += _ => changes++;

            Assert.That(sim.HireIntern(), Is.True);
            Assert.That(sim.HireIntern(), Is.False, "já contratado");
            Assert.That(sim.GrantInternLevels(), Is.True);
            Assert.That(sim.State.LevelOf(UpgradeId.InternPickaxe), Is.EqualTo(1));
            Assert.That(sim.State.LevelOf(UpgradeId.InternSpeed), Is.EqualTo(1));
            Assert.That(sim.State.LevelOf(UpgradeId.InternMining), Is.EqualTo(1));
            Assert.That(sim.InternStats.Damage, Is.EqualTo(10), "Picareta dele no nível 2");
            Assert.That(sim.InternStats.HitInterval, Is.EqualTo(1.9).Within(1e-9));

            Assert.That(sim.GrantPermanentLevel(PermanentId.InternTraining), Is.True);
            Assert.That(sim.InternStats.Damage, Is.EqualTo(11).Within(1e-9), "Treinar +1 vale na hora");
            Assert.That(sim.GrantPermanentLevel(PermanentId.InternPromotion), Is.False, "a Promoção espera o Treinar 10");
            Assert.That(sim.PromoteIntern(), Is.True);
            Assert.That(sim.Permanents.State.LevelOf(PermanentId.InternTraining), Is.EqualTo(10));
            Assert.That(sim.Permanents.State.LevelOf(PermanentId.InternPromotion), Is.EqualTo(1));

            Assert.That(sim.FireIntern(), Is.True);
            Assert.That(sim.IsInternHired, Is.False);
            Assert.That(sim.State.LevelOf(UpgradeId.InternPickaxe), Is.EqualTo(0), "a árvore dele sai junto");
            Assert.That(changes, Is.GreaterThanOrEqualTo(5));
        }

        [Test]
        public void GrantPermanentLevels_BringsThePromotion_TheStepTrainingReachesTen()
        {
            var sim = Sim();
            for (int i = 0; i < 9; i++) sim.GrantPermanentLevels();
            Assert.That(sim.Permanents.State.LevelOf(PermanentId.InternPromotion), Is.EqualTo(0));
            sim.GrantPermanentLevels();
            Assert.That(sim.Permanents.State.LevelOf(PermanentId.InternTraining), Is.EqualTo(10));
            Assert.That(sim.Permanents.State.LevelOf(PermanentId.InternPromotion), Is.EqualTo(1));
        }

        // ── Save ───────────────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void Save_KeepsTheIntern_AndAVersion4SaveOpensWithoutHim()
        {
            string dir = Path.Combine(Path.GetTempPath(), "MinersIdleInternTests_" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new FileSaveStore(dir);
                var save = new SaveService(store);

                var permanent = PermanentState.Restore(30, 3, id => id == PermanentId.InternTraining ? 4 : 0, V7Permanents());
                save.Save(With((UpgradeId.Intern, 1), (UpgradeId.InternPickaxe, 3), (UpgradeId.InternSpeed, 2)), permanent);
                var loaded = save.Load(V7(), V7Permanents(), out var loadedPermanent, out var status);
                Assert.That(status, Is.EqualTo(SaveLoadStatus.Loaded));
                Assert.That(loaded.LevelOf(UpgradeId.Intern), Is.EqualTo(1));
                Assert.That(loaded.LevelOf(UpgradeId.InternPickaxe), Is.EqualTo(3));
                Assert.That(loaded.LevelOf(UpgradeId.InternSpeed), Is.EqualTo(2));
                Assert.That(loadedPermanent.LevelOf(PermanentId.InternTraining), Is.EqualTo(4));

                // Um save de antes do M7 (versão 4, sem as linhas do estagiário): abre com ele não contratado.
                File.WriteAllText(store.FilePath,
                    "{ \"version\": 4, \"shards\": 600.0, \"levels\": [ { \"upgrade\": \"Pickaxe\", \"level\": 2 } ], " +
                    "\"permanent\": { \"redShards\": 12.0, \"day\": 2, \"levels\": [ { \"upgrade\": \"Memory\", \"level\": 1 } ] } }");
                var old = save.Load(V7(), V7Permanents(), out var oldPermanent, out status);
                Assert.That(status, Is.EqualTo(SaveLoadStatus.Loaded));
                Assert.That(old.Shards, Is.EqualTo(600));
                Assert.That(old.LevelOf(UpgradeId.Pickaxe), Is.EqualTo(2));
                Assert.That(old.LevelOf(UpgradeId.Intern), Is.EqualTo(0));
                Assert.That(oldPermanent.LevelOf(PermanentId.Memory), Is.EqualTo(1));
                Assert.That(oldPermanent.LevelOf(PermanentId.InternTraining), Is.EqualTo(0));
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
            }
        }

        [Test]
        public void Restore_DropsHisTree_WithoutTheHire()
        {
            var state = With((UpgradeId.InternPickaxe, 3));
            Assert.That(state.LevelOf(UpgradeId.InternPickaxe), Is.EqualTo(0), "a árvore só existe com ele contratado");
        }

        // ── O Balance_GDD_v7 ───────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// D-205, pelo <c>Balance_GDD_v7</c> que o jogo usa: dano = 5 × 2^(nível − 1) com nível = 1 + compras, e o custo da
        /// próxima compra = 50 × 4^(nível − 1). Os números estão escritos à mão, e o golpe dele no cristal é medido.
        /// </summary>
        [TestCase(0, 5.0, 50.0)]
        [TestCase(1, 10.0, 200.0)]
        [TestCase(4, 80.0, 12800.0)]
        [TestCase(9, 2560.0, 13107200.0)]
        [TestCase(18, 1310720.0, 3435973836800.0)]
        [TestCase(19, 2621440.0, -1.0)]
        public void BalanceGddV7Asset_InternPickaxe_ReachesTheGddValues(int purchases, double damage, double nextCost)
        {
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV7Path);
            Assert.That(asset, Is.Not.Null, "o setup cria o Balance_GDD_v7");
            var economy = asset.ToEconomy();
            var rules = asset.ToRules();
            var state = GameState.Restore(0, 0, 0,
                id => id == UpgradeId.Intern ? 1 : id == UpgradeId.InternPickaxe ? purchases : 0,
                economy, rules.CaveCount, cave: 100); // vida de bilhões: ninguém quebra o cristal
            var sim = new MiningSimulation(rules, new SeededRandom(7), state, economy, asset.ToStrike());

            Assert.That(sim.InternStats.Damage, Is.EqualTo(damage), "nível " + (purchases + 1));
            double? cost = sim.Preview(UpgradeId.InternPickaxe).NextCost;
            if (nextCost < 0) Assert.That(cost, Is.Null, "19 compras = nível 20: no limite (D-207)");
            else Assert.That(cost, Is.EqualTo(nextCost), "a próxima compra");

            var hits = new List<double>();
            sim.InternHitLanded += hit => hits.Add(hit.Damage);
            for (int i = 0; i < 300 && hits.Count == 0; i++) sim.Tick(0.1);
            Assert.That(hits, Is.Not.Empty, "ele bateu");
            Assert.That(hits[0], Is.EqualTo(damage), "o golpe dele no cristal");
        }

        [Test]
        public void BalanceGddV7Asset_HasTheInternOfTheGdd_AndV5DoesNot()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV7Path);
            Assert.That(asset, Is.Not.Null, "o setup cria o Balance_GDD_v7");
            var economy = asset.ToEconomy();
            var rules = asset.ToRules();

            Assert.That(economy.HasIntern, Is.True);
            Assert.That(economy.Get(UpgradeId.Intern).CostAt(0), Is.EqualTo(500));
            Assert.That(economy.Get(UpgradeId.Intern).MaxLevel, Is.EqualTo(1));
            Assert.That(economy.Get(UpgradeId.InternPickaxe).CostAt(2), Is.EqualTo(800));
            Assert.That(economy.Get(UpgradeId.InternPickaxe).MaxLevel, Is.EqualTo(19), "nível 20 (D-207)");
            Assert.That(economy.Get(UpgradeId.InternSpeed).CostAt(2), Is.EqualTo(1250));
            Assert.That(economy.Get(UpgradeId.InternSpeed).MaxLevel, Is.EqualTo(19));
            Assert.That(economy.Get(UpgradeId.InternMining).CostAt(1), Is.EqualTo(60).Within(1e-9));
            Assert.That(economy.Get(UpgradeId.InternMining).MaxLevel, Is.EqualTo(10));

            var hired = GameState.Restore(0, 0, 0, id => id == UpgradeId.Intern ? 1 : id == UpgradeId.InternPickaxe ? 3 : 0, economy);
            var stats = InternStats.Compute(rules, economy, hired);
            Assert.That(stats.Damage, Is.EqualTo(40), "3 compras = nível 4: 5 × 2^3 (D-205)");
            Assert.That(stats.HitInterval, Is.EqualTo(2), "parte de 2,0 s");

            var permanents = asset.ToPermanents();
            Assert.That(permanents.Get(PermanentId.InternTraining).MaxLevel, Is.EqualTo(10));
            Assert.That(permanents.Get(PermanentId.InternTraining).CostAt(0), Is.EqualTo(11));
            Assert.That(permanents.Get(PermanentId.InternTraining).Effect, Is.EqualTo(0.1).Within(1e-9));
            Assert.That(permanents.Get(PermanentId.InternPromotion).CostAt(0), Is.EqualTo(50));
            Assert.That(permanents.Get(PermanentId.InternPromotion).MaxLevel, Is.EqualTo(1));

            var v5 = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV5Path);
            Assert.That(v5.ToEconomy().HasIntern, Is.False, "D-172: o estagiário só existe no v7");
        }
    }
}
