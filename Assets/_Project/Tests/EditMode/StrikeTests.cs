using System;
using System.Collections.Generic;
using System.Linq;
using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>Marco 5, E1: toque (D-47, D-48), crítico (REQ-MIN-03, REQ-PRO-02) e combo (REQ-MIN-04) nas regras.</summary>
    public class StrikeTests
    {
        static MiningRules Rules(double health = 10, int initial = 0) =>
            new MiningRules(
                initial, firstCrystalDistance: 3, maxCrystals: 12, spawnInterval: 1e9, minCrystalSpacing: 2, spawnDistance: 12,
                crystalHealth: health, crystalReward: 1, damage: 1, hitInterval: 1, walkSpeed: 4, miningRange: 1);

        static EconomyRules Economy() => new EconomyRules(
            pickaxe: new UpgradeDefinition(1, 1, 10, 2),
            speed: new UpgradeDefinition(0, 1, 0, 0),
            mining: new UpgradeDefinition(0, 1, 0, 0),
            doublePickaxe: new UpgradeDefinition(1, 1, 1, 2),
            boots: new UpgradeDefinition(0, 1, 0, 0),
            minHitInterval: 0.5);

        /// <summary>Os valores do GDD: 5%, ×2, 10%, 1 s.</summary>
        static StrikeRules Gdd() => new StrikeRules(0.05, 2, 0.1, 1);
        static StrikeRules NoCrit() => new StrikeRules(0, 2, 0.1, 1);

        static MiningSimulation Sim(StrikeRules strike, double health = 10, int initial = 0, ulong critSeed = 7) =>
            new MiningSimulation(Rules(health, initial), new SeededRandom(critSeed), economy: Economy(), strike: strike);

        [Test]
        public void Tap_TakesTenPercentOfAHit_FromTheCurrentTarget()
        {
            var sim = Sim(NoCrit());
            var crystal = sim.SpawnCrystalAt(1);
            sim.Tick(0); // escolhe o alvo
            var hits = new List<MiningHit>();
            sim.HitLanded += hits.Add;

            sim.Tap();

            Assert.That(crystal.Health, Is.EqualTo(9.9).Within(1e-12));
            Assert.That(hits.Single().IsTap, Is.True);
            Assert.That(hits.Single().Damage, Is.EqualTo(0.1).Within(1e-12));
            Assert.That(sim.State.Hits, Is.EqualTo(0), "toque não é golpe de picareta");
        }

        [Test]
        public void Tap_CountsTheDoublePickaxe()
        {
            var sim = Sim(NoCrit());
            sim.AddShards(2);
            sim.Purchase(UpgradeId.Pickaxe);       // dano 2
            sim.Purchase(UpgradeId.DoublePickaxe); // dano 4
            var crystal = sim.SpawnCrystalAt(1);
            sim.Tick(0);

            sim.Tap();

            Assert.That(crystal.Health, Is.EqualTo(10 - 0.4).Within(1e-12));
        }

        [Test]
        public void Tap_WorksWhileTheMinerIsStillWalking()
        {
            var sim = Sim(NoCrit());
            var crystal = sim.SpawnCrystalAt(10);
            sim.Tick(0.5);
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Running));

            sim.Tap();

            Assert.That(crystal.Health, Is.EqualTo(9.9).Within(1e-12));
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Running), "o anão continua correndo");
        }

        [Test]
        public void Tap_WithoutTarget_OnlyCountsForTheCombo()
        {
            var sim = Sim(NoCrit());
            int hits = 0;
            sim.HitLanded += _ => hits++;

            sim.Tap();

            Assert.That(hits, Is.EqualTo(0));
            Assert.That(sim.Combo, Is.EqualTo(1));
        }

        [Test]
        public void TenTaps_BreakACrystalOfOneHealth_AndPayOnce()
        {
            var sim = Sim(NoCrit(), health: 1);
            sim.SpawnCrystalAt(10); // longe: só os toques quebram
            sim.Tick(0);
            int broken = 0;
            sim.CrystalBroken += _ => broken++;

            for (int i = 0; i < 10; i++) sim.Tap(); // 0,1 × 10 em double = 0,9999999999999999

            Assert.That(broken, Is.EqualTo(1), "o resto de arredondamento não impede a quebra");
            Assert.That(sim.State.Breaks, Is.EqualTo(1));
            Assert.That(sim.State.Shards, Is.EqualTo(1));
            Assert.That(sim.Miner.Target, Is.Null);

            sim.Tap();
            Assert.That(sim.State.Shards, Is.EqualTo(1), "sem alvo, sem pagamento extra");
        }

        /// <summary>REQ-PRO-02: em 100.000 golpes, cerca de 5% são críticos, e o crítico dá o dobro.</summary>
        [Test]
        public void HundredThousandHits_AboutFivePercentCritical()
        {
            var sim = Sim(Gdd(), health: 1e12);
            sim.SpawnCrystalAt(1);
            int hits = 0, critical = 0;
            sim.HitLanded += hit =>
            {
                hits++;
                if (!hit.IsCritical) return;
                critical++;
                Assert.That(hit.Damage, Is.EqualTo(2), "crítico = dano ×2");
            };

            sim.Tick(100_000);

            Assert.That(hits, Is.EqualTo(100_000));
            Assert.That(critical, Is.InRange(4_700, 5_300), "5% ± ~4 desvios-padrão");
        }

        [Test]
        public void Tap_CanBeCritical()
        {
            var sim = Sim(NoCrit());
            var crystal = sim.SpawnCrystalAt(1);
            sim.Tick(0);
            sim.ForceCritical = true; // atalho do debug

            sim.Tap();

            Assert.That(crystal.Health, Is.EqualTo(10 - 0.2).Within(1e-12));
        }

        [Test]
        public void Combo_GrowsWithTaps_AndResetsAfterOneSecondWithout()
        {
            var sim = Sim(NoCrit());
            var changes = new List<int>();
            var milestones = new List<int>();
            sim.ComboChanged += changes.Add;
            sim.ComboMilestone += milestones.Add;

            sim.Tap();
            sim.Tick(0.5);
            sim.Tap();
            Assert.That(sim.Combo, Is.EqualTo(2));

            sim.Tick(0.99);
            Assert.That(sim.Combo, Is.EqualTo(2), "ainda dentro de 1 s do último toque");
            sim.Tick(0.02);
            Assert.That(sim.Combo, Is.EqualTo(0));
            Assert.That(changes, Is.EqualTo(new[] { 1, 2, 0 }));

            for (int i = 0; i < 25; i++) sim.Tap();
            Assert.That(milestones, Is.EqualTo(new[] { 10, 20 }));
        }

        [Test]
        public void ComboReset_HappensAtTheSameTime_HoweverTimeIsSliced()
        {
            var small = Sim(NoCrit());
            var big = Sim(NoCrit());
            double resetSmall = -1, resetBig = -1;
            small.ComboChanged += c => { if (c == 0) resetSmall = small.Elapsed; };
            big.ComboChanged += c => { if (c == 0) resetBig = big.Elapsed; };

            small.Tap();
            big.Tap();
            for (int i = 0; i < 300; i++) small.Tick(1.0 / 60);
            big.Tick(5);

            Assert.That(resetSmall, Is.EqualTo(1).Within(1e-6));
            Assert.That(resetBig, Is.EqualTo(1).Within(1e-6));
        }

        /// <summary>
        /// Os nascimentos são do relógio do jogo (D-54): com ou sem toques e críticos, o cristal N nasce no mesmo instante.
        /// (No mapa aberto, até o M5, o teste conferia o lugar sorteado; na fila, o lugar acompanha o anão, D-60, e quem
        /// toca corre mais, então só o instante é o mesmo.) Sem máximo, para a fila cheia não segurar nascimentos.
        /// </summary>
        [Test]
        public void TapsAndCriticals_DoNotChangeWhenCrystalsAreBorn()
        {
            MiningRules FreeSpawning() => new MiningRules(
                initialCrystals: 0, firstCrystalDistance: 3, maxCrystals: 1000, spawnInterval: 1, minCrystalSpacing: 0, spawnDistance: 0,
                crystalHealth: 3, crystalReward: 1, damage: 1, hitInterval: 1, walkSpeed: 4, miningRange: 1);

            var calm = new MiningSimulation(FreeSpawning(), new SeededRandom(3), economy: Economy(), strike: NoCrit());
            var tapping = new MiningSimulation(FreeSpawning(), new SeededRandom(99), economy: Economy(), strike: Gdd());
            var born = new Dictionary<int, double>();
            calm.CrystalSpawned += c => born[c.Id] = calm.Elapsed;
            int compared = 0;
            tapping.CrystalSpawned += c =>
            {
                Assert.That(tapping.Elapsed, Is.EqualTo(born[c.Id]).Within(1e-9), $"cristal {c.Id}");
                compared++;
            };

            for (int i = 0; i < 200; i++)
            {
                calm.Tick(0.5);
                tapping.Tick(0.5);
                tapping.Tap();
            }

            Assert.That(compared, Is.GreaterThan(90));
            Assert.That(tapping.State.Breaks, Is.GreaterThan(calm.State.Breaks), "tocar ajuda");
        }

        [Test]
        public void InvalidStrikeRules_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StrikeRules(-0.1, 2, 0.1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StrikeRules(1.1, 2, 0.1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StrikeRules(0.05, 0.5, 0.1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StrikeRules(0.05, 2, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StrikeRules(0.05, 2, 0.1, 0));
        }
    }
}
