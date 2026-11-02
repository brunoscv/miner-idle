using System;
using System.Collections.Generic;
using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>
    /// Correções do toque e da fila (2026-11-01): o teto de 15 toques por segundo (D-119), o toque só no alvo que já está
    /// na tela (D-121) e a fila que repõe cada quebra (D-122), para a tela nunca ficar sem cristais (D-120).
    /// </summary>
    public class TapGuardTests
    {
        const int Cap = 15;
        const double Reach = 4;

        static MiningRules Rules(double health = 1e9, int initial = 0, int minCrystals = 0, double tapReach = double.PositiveInfinity,
                                 double damage = 1) =>
            new MiningRules(
                initial, firstCrystalDistance: 3, maxCrystals: 12, spawnInterval: 1e9, minCrystalSpacing: 2, spawnDistance: 12,
                crystalHealth: health, crystalReward: 1, damage: damage, hitInterval: 1, walkSpeed: 4, miningRange: 1,
                minCrystals: minCrystals, tapReach: tapReach);

        static StrikeRules Capped(int cap = Cap) => new StrikeRules(0, 2, 0.1, 1, maxTapsPerSecond: cap);

        static MiningSimulation Sim(MiningRules rules, StrikeRules strike) =>
            new MiningSimulation(rules, new SeededRandom(7), strike: strike);

        [Test]
        public void HundredTapsAtOnce_OnlyFifteenCount()
        {
            var sim = Sim(Rules(), Capped());
            sim.SpawnCrystalAt(2);
            sim.Tick(0);
            int hits = 0, counted = 0;
            sim.HitLanded += _ => hits++;

            for (int i = 0; i < 100; i++)
                if (sim.Tap()) counted++;

            Assert.That(counted, Is.EqualTo(Cap));
            Assert.That(hits, Is.EqualTo(Cap), "só os toques que valeram dão dano");
            Assert.That(sim.Combo, Is.EqualTo(Cap), "o toque acima do teto não soma no combo");
        }

        [Test]
        public void AfterOneSecond_TapsCountAgain()
        {
            var sim = Sim(Rules(), Capped());
            for (int i = 0; i < Cap; i++) Assert.That(sim.Tap(), Is.True);

            sim.Tick(0.999);
            Assert.That(sim.Tap(), Is.False, "ainda dentro do segundo dos 15");
            sim.Tick(0.002);
            Assert.That(sim.Tap(), Is.True);
            Assert.That(sim.Tap(), Is.True, "os 15 saíram juntos da janela");
        }

        /// <summary>Vinte toques por segundo durante 3 s: valem 15 em cada segundo, nem um a mais.</summary>
        [Test]
        public void TwentyTapsPerSecond_FifteenCountInEverySecond()
        {
            var sim = Sim(Rules(), Capped());
            var times = new List<double>();
            for (int i = 0; i < 60; i++)
            {
                if (sim.Tap()) times.Add(sim.Elapsed);
                sim.Tick(0.05);
            }

            Assert.That(times.Count, Is.EqualTo(3 * Cap));
            for (int i = Cap; i < times.Count; i++)
                Assert.That(times[i] - times[i - Cap], Is.GreaterThanOrEqualTo(1 - 1e-6), $"toque {i}");
        }

        [Test]
        public void TapAboveTheCap_IsNeitherCriticalNorDamage()
        {
            var sim = Sim(Rules(), Capped());
            var crystal = sim.SpawnCrystalAt(2);
            sim.Tick(0);
            sim.ForceCritical = true;
            for (int i = 0; i < Cap; i++) sim.Tap();
            double health = crystal.Health;
            var events = 0;
            sim.HitLanded += _ => events++;
            sim.ComboChanged += _ => events++;
            sim.ComboMilestone += _ => events++;

            Assert.That(sim.Tap(), Is.False);

            Assert.That(crystal.Health, Is.EqualTo(health));
            Assert.That(events, Is.EqualTo(0), "nenhum evento sai do toque ignorado");
        }

        [Test]
        public void WithoutCap_EveryTapCounts()
        {
            var sim = Sim(Rules(), Capped(0));
            for (int i = 0; i < 1000; i++) Assert.That(sim.Tap(), Is.True);
            Assert.That(sim.Combo, Is.EqualTo(1000));
        }

        [Test]
        public void TapOnATargetBeyondReach_OnlyCountsForTheCombo()
        {
            var sim = Sim(Rules(tapReach: Reach), Capped(0));
            var crystal = sim.SpawnCrystalAt(10); // 10 un à frente: fora da tela
            sim.Tick(0);
            Assert.That(sim.Miner.Target, Is.SameAs(crystal));
            int hits = 0;
            sim.HitLanded += _ => hits++;

            Assert.That(sim.Tap(), Is.True, "o toque vale, só não acerta");
            Assert.That(hits, Is.EqualTo(0));
            Assert.That(crystal.Health, Is.EqualTo(1e9));
            Assert.That(sim.Combo, Is.EqualTo(1));

            sim.Tick((10 - Reach) / 4 + 0.01); // correu até o cristal entrar no alcance
            sim.Tap();
            Assert.That(hits, Is.EqualTo(1), "dentro do alcance, com o anão ainda correndo");
            Assert.That(sim.Miner.State, Is.EqualTo(MinerState.Running));
        }

        [Test]
        public void TapReachIsInclusive()
        {
            var sim = Sim(Rules(tapReach: Reach), Capped(0));
            var crystal = sim.SpawnCrystalAt(Reach);
            sim.Tick(0);

            sim.Tap();

            Assert.That(crystal.Health, Is.LessThan(1e9));
        }

        [Test]
        public void WithMinimumTen_EveryBreakIsReplaced()
        {
            var sim = Sim(Rules(health: 1, initial: 10, minCrystals: 10, damage: 100), Capped(0));
            for (int i = 0; i < 300; i++)
            {
                sim.Tick(0.1);
                Assert.That(sim.Crystals.Count, Is.GreaterThanOrEqualTo(10), $"passo {i}");
            }
            Assert.That(sim.State.Breaks, Is.GreaterThanOrEqualTo(20), "30 s, um golpe por segundo quebra");
        }

        [Test]
        public void InvalidValues_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StrikeRules(0.05, 2, 0.1, 1, maxTapsPerSecond: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Rules(tapReach: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Rules(tapReach: double.NaN));
        }

        /// <summary>
        /// D-120, o caso do amigo do Bruno: anão forte (um toque quebra) e o jogador tocando sem parar, 60 vezes por
        /// segundo (um toque a cada quadro), por 2 minutos, com os valores do jogo (teto 15/s, alcance 4 un, fila de 10). Nenhum cristal quebra
        /// fora do alcance do toque, e o tempo seguido sem cristal no alcance nunca passa do de correr de um cristal ao
        /// próximo (2 un a 4 un/s). Antes, com o toque a qualquer distância e a fila de 4, os cristais quebravam antes de
        /// entrar na tela e o anão corria sem fim num corredor vazio.
        /// </summary>
        [Test]
        public void FastTappingStrongMiner_TheScreenNeverRunsOutOfCrystals()
        {
            const double frame = 1.0 / 60;
            var rules = Rules(health: 10, initial: 10, minCrystals: 10, tapReach: Reach, damage: 1000);
            var sim = Sim(rules, new StrikeRules(0, 2, 0.33, 1, maxTapsPerSecond: Cap));
            int brokenOutside = 0;
            sim.CrystalBroken += c => { if (c.Distance - sim.Miner.Distance > Reach + 1e-6) brokenOutside++; };

            double empty = 0, longestEmpty = 0;
            for (int i = 0; i < 120 * 60; i++)
            {
                sim.Tap();
                sim.Tick(frame);

                bool inReach = false;
                foreach (var crystal in sim.Crystals)
                    if (crystal.Distance - sim.Miner.Distance <= Reach) { inReach = true; break; }
                empty = inReach ? 0 : empty + frame;
                longestEmpty = Math.Max(longestEmpty, empty);
            }

            Assert.That(brokenOutside, Is.EqualTo(0), "nenhum cristal quebra antes de aparecer");
            Assert.That(longestEmpty, Is.LessThanOrEqualTo(rules.MinCrystalSpacing / rules.WalkSpeed + 2 * frame));
            // 60 tentativas por segundo gastam os 15 do teto no primeiro quarto de segundo; o cristal que entra no alcance
            // depois espera a janela abrir (ou a picareta): ~1 quebra por segundo.
            Assert.That(sim.State.Breaks, Is.GreaterThan(100), "o jogo continua andando");
        }
    }
}
