using System;
using System.Collections.Generic;
using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9, E1: a conta do offline (D-189, D-193, D-194, D-196). Só o estagiário, com o que é dele, nos cristais do nível
    /// em que o jogador parou, até 6 h; a conta fechada bate com a simulação golpe a golpe. Os números estão escritos à
    /// mão, como no plano do M9 (nível 15: vida 22,7, lascas 27,3 por cristal).
    /// </summary>
    public class OfflineMiningTests
    {
        const double Hour = 3600;

        /// <summary>O ritmo do GDD v7: corrida 2 un ÷ 4 un/s = 0,5 s, golpe a cada 2,0 s, o 1º na chegada (A2).</summary>
        static MiningRules Rules(ArrivalStrike arrival = ArrivalStrike.Immediate, double maxHours = 6) =>
            new MiningRules(
                initialCrystals: 0, firstCrystalDistance: 3, maxCrystals: 12, spawnInterval: 1e9, minCrystalSpacing: 2,
                spawnDistance: 12, crystalHealth: 1, crystalReward: 1.2, damage: 1, hitInterval: 2, walkSpeed: 4,
                miningRange: 1, caveCount: 100, caveHealthGrowth: 1.25, caveRewardGrowth: 1.25, letradoBreaks: 100,
                arrivalStrike: arrival, pickaxeFormula: PickaxeFormula.LevelScaled, levelsPerBiome: 10,
                offlineMaxSeconds: maxHours * Hour);

        static EconomyRules V7() => new EconomyRules(
            pickaxe: new UpgradeDefinition(2, 2, 49, 1, CostFormula.Linear),
            speed: new UpgradeDefinition(50, 2, 19, 0.1),
            mining: new UpgradeDefinition(20, 5, 10, 1),
            doublePickaxe: new UpgradeDefinition(5000, 1, 0, 2),
            boots: new UpgradeDefinition(0, 1, 0, 0),
            minHitInterval: 0.1,
            letrado: new UpgradeDefinition(0, 1, 100, 1),
            critical: new UpgradeDefinition(50, 2, 10, 0.05),
            intern: new UpgradeDefinition(500, 1, 1, 1),
            internPickaxe: new UpgradeDefinition(50, 4, 19, 5),
            internSpeed: new UpgradeDefinition(50, 5, 19, 0.1),
            internMining: new UpgradeDefinition(50, 1.2, 10, 1),
            internPickaxeGrowth: 2);

        static UpgradeDefinition Line(int max, double effect) => new UpgradeDefinition(10, 2, max, effect, CostFormula.OffsetExponential);

        static PermanentRules V7Permanents() => new PermanentRules(
            10, new RedShardTable(10, 10, 5, 50, 10, 1),
            Line(10, 10), Line(20, 0.1), Line(20, 0.5), Line(10, 0.05), Line(10, 1),
            Line(10, 0.1), new UpgradeDefinition(50, 1, 1, 1));

        static GameState At(int cave, params (UpgradeId id, int level)[] levels)
        {
            var map = new Dictionary<UpgradeId, int>();
            foreach (var (id, level) in levels) map[id] = level;
            return GameState.Restore(0, 0, 0, id => map.TryGetValue(id, out int l) ? l : 0, V7(), 100, cave);
        }

        static Permanents NoPermanents() => new Permanents(V7Permanents(), new PermanentState());

        static Permanents With(PermanentId id, int level) =>
            new Permanents(V7Permanents(), PermanentState.Restore(0, 1, p => p == id ? level : 0, V7Permanents()));

        static OfflineReport Compute(GameState state, double seconds, MiningRules rules = null, Permanents permanents = null) =>
            OfflineMining.Compute(rules ?? Rules(), V7(), state, permanents ?? NoPermanents(), seconds);

        // ── Quem minera (D-139, D-189) ───────────────────────────────────────────────────────────────────────

        [Test]
        public void WithoutTheIntern_NothingHappens()
        {
            var report = Compute(At(15), Hour);

            Assert.That(report.Hired, Is.False);
            Assert.That(report.Crystals, Is.EqualTo(0));
            Assert.That(report.Shards, Is.EqualTo(0));
            Assert.That(report.SecondsCounted, Is.EqualTo(Hour), "o tempo conta, mas ninguém minera");
        }

        /// <summary>O exemplo do plano, com o estagiário da D-205: recém-contratado no nível 15, dano 5 a cada 2,0 s.</summary>
        [Test]
        public void FreshIntern_AtLevel15_IsThePlanExample()
        {
            var report = Compute(At(15, (UpgradeId.Intern, 1)), Hour);

            double health = Math.Pow(1.25, 14); // 22,7
            Assert.That(report.Hired, Is.True);
            Assert.That(report.HitsPerCrystal, Is.EqualTo(5), "⌈22,7 ÷ 5⌉");
            Assert.That(report.SecondsPerCrystal, Is.EqualTo(0.5 + 4 * 2.0).Within(1e-9), "corrida + 4 intervalos (o 1º na chegada)");
            Assert.That(report.Crystals, Is.EqualTo(423), "⌊3600 ÷ 8,5⌋");
            Assert.That(report.ShardsPerCrystal, Is.EqualTo(1.2 * health).Within(1e-9), "lascas = vida × 1,2, sem Mineração");
            Assert.That(report.Shards, Is.EqualTo(423 * 1.2 * health).Within(1e-6));
        }

        [Test]
        public void StrongIntern_OneHitPerCrystal_TheRunSetsThePace()
        {
            // Picareta 5 (5 × 2^4 = 80), Velocidade 10 (1,0 s), Mineração 3 (×4): o exemplo forte do plano.
            var state = At(15, (UpgradeId.Intern, 1), (UpgradeId.InternPickaxe, 5), (UpgradeId.InternSpeed, 10),
                           (UpgradeId.InternMining, 3));
            var report = Compute(state, 6 * Hour);

            Assert.That(report.HitsPerCrystal, Is.EqualTo(1));
            Assert.That(report.SecondsPerCrystal, Is.EqualTo(0.5), "só a corrida");
            Assert.That(report.Crystals, Is.EqualTo(6 * 3600 * 2));
            Assert.That(report.ShardsPerCrystal, Is.EqualTo(4 * 1.2 * Math.Pow(1.25, 14)).Within(1e-9), "Mineração dele ×4 (D-166)");
            Assert.That(report.Shards, Is.EqualTo(43200 * 4 * 1.2 * Math.Pow(1.25, 14)).Within(1e-3), "~4,7 mi em 6 h");
        }

        [Test]
        public void Training_Counts_ButTheDwarfsPermanentsDoNot()
        {
            var state = At(15, (UpgradeId.Intern, 1));
            var plain = Compute(state, Hour);

            // Treinar 10: dano dele ×2 (0,1 por nível): 5 → 10, ⌈22,7 ÷ 10⌉ = 3 golpes.
            var trained = Compute(state, Hour, permanents: With(PermanentId.InternTraining, 10));
            Assert.That(trained.HitsPerCrystal, Is.EqualTo(3));
            Assert.That(trained.Crystals, Is.GreaterThan(plain.Crystals));

            // Coração Ardente (lascas do anão) e Sangue da Montanha (dano do anão) não valem (D-194).
            var heart = Compute(state, Hour, permanents: With(PermanentId.BurningHeart, 20));
            Assert.That(heart.Shards, Is.EqualTo(plain.Shards), "Coração Ardente fora");
            var blood = Compute(state, Hour, permanents: With(PermanentId.MountainBlood, 20));
            Assert.That(blood.HitsPerCrystal, Is.EqualTo(plain.HitsPerCrystal), "Sangue da Montanha fora");
        }

        [Test]
        public void TheDwarfsUpgrades_DoNotCount()
        {
            var plain = Compute(At(15, (UpgradeId.Intern, 1)), Hour);
            var dwarf = Compute(At(15, (UpgradeId.Intern, 1), (UpgradeId.Pickaxe, 40), (UpgradeId.Speed, 15),
                                   (UpgradeId.Mining, 10)), Hour);

            Assert.That(dwarf.Crystals, Is.EqualTo(plain.Crystals));
            Assert.That(dwarf.Shards, Is.EqualTo(plain.Shards), "nem a Mineração do anão");
        }

        // ── Tempo: teto e relógio (D-189, D-196) ─────────────────────────────────────────────────────────────

        [Test]
        public void EightHoursAway_CountOnlySix()
        {
            var state = At(15, (UpgradeId.Intern, 1));
            var eight = Compute(state, 8 * Hour);
            var six = Compute(state, 6 * Hour);

            Assert.That(eight.SecondsAway, Is.EqualTo(8 * Hour));
            Assert.That(eight.SecondsCounted, Is.EqualTo(6 * Hour));
            Assert.That(eight.Capped, Is.True);
            Assert.That(six.Capped, Is.False);
            Assert.That(eight.Shards, Is.EqualTo(six.Shards), "o que passou de 6 h se perdeu");
        }

        [Test]
        public void TheCapComesFromTheRules()
        {
            var report = Compute(At(15, (UpgradeId.Intern, 1)), 8 * Hour, Rules(maxHours: 2));
            Assert.That(report.SecondsCounted, Is.EqualTo(2 * Hour));
        }

        [Test]
        public void ClockWentBack_GivesNothing_WithoutError()
        {
            var report = Compute(At(15, (UpgradeId.Intern, 1), (UpgradeId.InternPickaxe, 10)), -Hour);

            Assert.That(report.ClockWentBack, Is.True);
            Assert.That(report.Capped, Is.False);
            Assert.That(report.SecondsCounted, Is.EqualTo(0));
            Assert.That(report.Crystals, Is.EqualTo(0));
            Assert.That(report.Shards, Is.EqualTo(0));
        }

        [Test]
        public void HighLevel_WeakIntern_MayBreakNothing()
        {
            // Nível 100: vida 1,25^99 ≈ 3,9 bi; dano 5 a cada 2 s (≈ 785 mi golpes) não quebra nenhum em 6 h.
            var report = Compute(At(100, (UpgradeId.Intern, 1)), 6 * Hour);

            Assert.That(report.HitsPerCrystal, Is.GreaterThan(700_000_000L));
            Assert.That(report.Crystals, Is.EqualTo(0));
            Assert.That(report.Shards, Is.EqualTo(0));
            Assert.That(double.IsInfinity(report.SecondsPerCrystal), Is.False);
        }

        // ── A conta fechada e a simulação golpe a golpe ──────────────────────────────────────────────────────

        /// <summary>
        /// A referência: o estagiário cristal a cristal, golpe a golpe, como no jogo aberto sem o anão: corre 0,5 s, bate
        /// (o 1º na chegada no A2, depois de um intervalo no modo Espera, o que falta do intervalo no A1) e conta o cristal
        /// quando o último golpe sai dentro do tempo.
        /// </summary>
        static long StepByStep(double health, double damage, double interval, double run, ArrivalStrike arrival, double seconds)
        {
            double time = 0;
            long crystals = 0;
            while (true)
            {
                time += run;
                double timer = arrival == ArrivalStrike.Immediate ? interval
                    : arrival == ArrivalStrike.Charge ? Math.Min(run, interval) : 0;
                double left = health;
                while (left > 1e-9)
                {
                    time += interval - timer;
                    timer = 0;
                    if (time > seconds + 1e-9) return crystals;
                    left -= damage;
                }
                crystals++;
            }
        }

        [TestCase(1, 0, 0, ArrivalStrike.Immediate)]
        [TestCase(15, 0, 0, ArrivalStrike.Immediate)]
        [TestCase(15, 3, 4, ArrivalStrike.Immediate)]
        [TestCase(30, 6, 12, ArrivalStrike.Immediate)]
        [TestCase(15, 2, 5, ArrivalStrike.Wait)]
        [TestCase(15, 2, 5, ArrivalStrike.Charge)]
        [TestCase(10, 20, 19, ArrivalStrike.Immediate)]
        public void ClosedForm_MatchesStepByStep(int cave, int pickaxe, int speed, ArrivalStrike arrival)
        {
            var rules = Rules(arrival);
            var state = At(cave, (UpgradeId.Intern, 1), (UpgradeId.InternPickaxe, pickaxe), (UpgradeId.InternSpeed, speed));
            var intern = InternStats.Compute(rules, V7(), state);

            foreach (double seconds in new[] { 37.0, 600, Hour, 6 * Hour })
            {
                var report = Compute(state, seconds, rules);
                long expected = StepByStep(rules.CrystalHealthIn(cave), intern.Damage, intern.HitInterval, 0.5, arrival, seconds);
                Assert.That(report.Crystals, Is.EqualTo(expected), $"{seconds} s");
            }
        }

        // ── Trechos (para o marco dos Bônus) ─────────────────────────────────────────────────────────────────

        [Test]
        public void Stretches_CarryTheLeftover_AndAddUpToTheWhole()
        {
            var whole = OfflineMining.Stretch(44.5, 10, 3600);
            var first = OfflineMining.Stretch(44.5, 10, 1000);
            var second = OfflineMining.Stretch(44.5, 10, 2600, first.LeftoverSeconds);

            Assert.That(first.Crystals + second.Crystals, Is.EqualTo(whole.Crystals));
            Assert.That(first.Shards + second.Shards, Is.EqualTo(whole.Shards).Within(1e-9));
            Assert.That(second.LeftoverSeconds, Is.EqualTo(whole.LeftoverSeconds).Within(1e-9));
        }

        // ── Na simulação (D-189, D-197) ──────────────────────────────────────────────────────────────────────

        [Test]
        public void ApplyOffline_PaysTheShards_IntoTheDay_WithoutMovingTheMapLock()
        {
            var state = At(15, (UpgradeId.Intern, 1));
            var sim = new MiningSimulation(Rules(), new SeededRandom(1), state, V7(), permanents: NoPermanents());
            double paid = 0;
            sim.ShardsChanged += _ => paid++;

            var report = sim.ApplyOffline(Hour);

            Assert.That(report.Shards, Is.GreaterThan(0));
            Assert.That(sim.State.Shards, Is.EqualTo(report.Shards).Within(1e-6));
            Assert.That(sim.State.ShardsEarned, Is.EqualTo(report.Shards).Within(1e-6), "conta nas lascas ganhas do dia");
            Assert.That(sim.State.CaveBreaks, Is.EqualTo(0), "não conta na trava do Mapa");
            Assert.That(sim.State.Breaks, Is.EqualTo(0));
            Assert.That(sim.State.Cave, Is.EqualTo(15), "não passa de nível");
            Assert.That(paid, Is.EqualTo(1), "o HUD fica sabendo");
        }

        [Test]
        public void ApplyOffline_WithoutTheIntern_ChangesNothing()
        {
            var sim = new MiningSimulation(Rules(), new SeededRandom(1), At(15), V7(), permanents: NoPermanents());
            bool changed = false;
            sim.ShardsChanged += _ => changed = true;

            sim.ApplyOffline(6 * Hour);

            Assert.That(sim.State.Shards, Is.EqualTo(0));
            Assert.That(changed, Is.False);
        }

        // ── A volta esperando o COLETAR (M9, E2, D-195, D-197) ───────────────────────────────────────────────

        [Test]
        public void FromTheNotice_TheShardsWaitForCollect_AndPayOnce()
        {
            var sim = new MiningSimulation(Rules(), new SeededRandom(1), At(15, (UpgradeId.Intern, 1)), V7(),
                                           permanents: NoPermanents());
            int returns = 0, shardEvents = 0;
            sim.OfflineReturnChanged += _ => returns++;
            sim.ShardsChanged += _ => shardEvents++;

            var report = sim.ApplyOffline(2 * Hour + 14 * 60, noticeSeconds: 60);

            // Nível 15, recém-contratado (dano 5, 2,0 s): 5 golpes, 8,5 s por cristal (o exemplo do plano do M9).
            Assert.That(report.Crystals, Is.EqualTo(945));
            Assert.That(sim.State.Shards, Is.EqualTo(0), "ainda não entrou: espera o COLETAR");
            Assert.That(sim.State.ShardsEarned, Is.EqualTo(0));
            Assert.That(sim.PendingOffline.HasShards, Is.True);
            Assert.That(sim.PendingOffline.Shards, Is.EqualTo(report.Shards).Within(1e-9));
            Assert.That(sim.PendingOffline.Crystals, Is.EqualTo(945));
            Assert.That(sim.PendingOffline.Level, Is.EqualTo(15));
            Assert.That(sim.PendingOffline.Capped, Is.False);
            Assert.That(returns, Is.EqualTo(1));
            Assert.That(shardEvents, Is.EqualTo(0));

            Assert.That(sim.CollectOffline(), Is.EqualTo(report.Shards).Within(1e-9));
            Assert.That(sim.State.Shards, Is.EqualTo(report.Shards).Within(1e-9));
            Assert.That(sim.State.ShardsEarned, Is.EqualTo(report.Shards).Within(1e-9), "conta nas lascas do dia (D-197)");
            Assert.That(sim.PendingOffline.HasShards, Is.False);
            Assert.That(returns, Is.EqualTo(2), "o modal fica sabendo que acabou");
            Assert.That(shardEvents, Is.EqualTo(1), "o HUD sobe no COLETAR");

            Assert.That(sim.CollectOffline(), Is.EqualTo(0), "o segundo COLETAR não paga");
            Assert.That(sim.State.Shards, Is.EqualTo(report.Shards).Within(1e-9));
        }

        [Test]
        public void BelowTheNotice_TheShardsGoInDirectly()
        {
            var sim = new MiningSimulation(Rules(), new SeededRandom(1), At(15, (UpgradeId.Intern, 1)), V7(),
                                           permanents: NoPermanents());
            bool returned = false;
            sim.OfflineReturnChanged += _ => returned = true;

            var report = sim.ApplyOffline(59, noticeSeconds: 60);

            Assert.That(report.Crystals, Is.EqualTo(6), "59 s a 8,5 s por cristal");
            Assert.That(sim.State.Shards, Is.EqualTo(report.Shards).Within(1e-9), "menos de 60 s: direto, sem modal (D-195)");
            Assert.That(sim.PendingOffline.HasShards, Is.False);
            Assert.That(returned, Is.False);
        }

        [Test]
        public void ReturnsWithoutCollecting_AddUp_AndKeepTheCap()
        {
            var sim = new MiningSimulation(Rules(), new SeededRandom(1), At(15, (UpgradeId.Intern, 1)), V7(),
                                           permanents: NoPermanents());

            var first = sim.ApplyOffline(9 * Hour + 12 * 60, noticeSeconds: 60);
            Assert.That(sim.PendingOffline.Capped, Is.True, "9 h 12 min fora, contou 6 h");
            Assert.That(sim.PendingOffline.SecondsCounted, Is.EqualTo(6 * Hour));

            var second = sim.ApplyOffline(10 * 60, noticeSeconds: 60);
            Assert.That(sim.PendingOffline.Shards, Is.EqualTo(first.Shards + second.Shards).Within(1e-9));
            Assert.That(sim.PendingOffline.Crystals, Is.EqualTo(first.Crystals + second.Crystals));
            Assert.That(sim.PendingOffline.SecondsAway, Is.EqualTo(9 * Hour + 22 * 60).Within(1e-9));
            Assert.That(sim.State.Shards, Is.EqualTo(0));

            sim.CollectOffline();
            Assert.That(sim.State.Shards, Is.EqualTo(first.Shards + second.Shards).Within(1e-9), "as duas, uma vez cada");
        }

        [Test]
        public void ClockBack_OrNoIntern_LeaveNothingToCollect()
        {
            var hired = new MiningSimulation(Rules(), new SeededRandom(1), At(15, (UpgradeId.Intern, 1)), V7(),
                                             permanents: NoPermanents());
            hired.ApplyOffline(-Hour, noticeSeconds: 60);
            Assert.That(hired.PendingOffline.HasShards, Is.False, "relógio voltado (D-196)");

            var alone = new MiningSimulation(Rules(), new SeededRandom(1), At(15), V7(), permanents: NoPermanents());
            alone.ApplyOffline(Hour, noticeSeconds: 60);
            Assert.That(alone.PendingOffline.HasShards, Is.False, "sem estagiário (D-139)");
        }
    }
}
