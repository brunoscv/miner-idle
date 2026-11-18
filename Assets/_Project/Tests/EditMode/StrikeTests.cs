using System;
using System.Collections.Generic;
using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>
    /// Marco 5, E1: crítico (REQ-MIN-03, REQ-PRO-02) nas regras. O toque (D-47, D-48) e o combo (REQ-MIN-04) saíram no M9g
    /// (D-311), com os testes deles.
    /// </summary>
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

        /// <summary>Os valores do GDD: 5%, ×2.</summary>
        static StrikeRules Gdd() => new StrikeRules(0.05, 2);
        static StrikeRules NoCrit() => new StrikeRules(0, 2);

        static MiningSimulation Sim(StrikeRules strike, double health = 10, int initial = 0, ulong critSeed = 7) =>
            new MiningSimulation(Rules(health, initial), new SeededRandom(critSeed), economy: Economy(), strike: strike);

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

        /// <summary>
        /// Os nascimentos são do relógio do jogo (D-54): com ou sem bombas e críticos, o cristal N nasce no mesmo instante.
        /// (No mapa aberto, até o M5, o teste conferia o lugar sorteado; na fila, o lugar acompanha o anão, D-60, e quem
        /// quebra mais corre mais, então só o instante é o mesmo. Até o M9g era o toque; a bomba automática o substitui.)
        /// Sem máximo, para a fila cheia não segurar nascimentos.
        /// </summary>
        [Test]
        public void BombsAndCriticals_DoNotChangeWhenCrystalsAreBorn()
        {
            MiningRules FreeSpawning() => new MiningRules(
                initialCrystals: 0, firstCrystalDistance: 3, maxCrystals: 1000, spawnInterval: 1, minCrystalSpacing: 0, spawnDistance: 0,
                crystalHealth: 3, crystalReward: 1, damage: 1, hitInterval: 1, walkSpeed: 4, miningRange: 1);

            var calm = new MiningSimulation(FreeSpawning(), new SeededRandom(3), economy: Economy(), strike: NoCrit());
            var tapping = new MiningSimulation(FreeSpawning(), new SeededRandom(99), economy: Economy(),
                                               strike: new StrikeRules(0.05, 2, bombs: new BombRules(new[] { 0.5 }, 1, 0.4, 4)));
            tapping.AutoBomb = true;
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
            }

            Assert.That(compared, Is.GreaterThan(90));
            Assert.That(tapping.State.Breaks, Is.GreaterThan(calm.State.Breaks), "a bomba ajuda");
        }

        [Test]
        public void InvalidStrikeRules_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StrikeRules(-0.1, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StrikeRules(1.1, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StrikeRules(0.05, 0.5));
        }
    }
}
