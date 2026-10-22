using System.Collections.Generic;
using System.Linq;
using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>
    /// A fila de cristais e a regra de alvo da descida (M6, E2; D-52, D-54, D-60): o alvo é sempre o primeiro da fila.
    /// Até o M5 este arquivo media a regra "lote, depois mais próximo" do mapa aberto (D-27, superada pela D-52).
    /// </summary>
    public class CrystalTrackTests
    {
        static MiningRules Rules(int initial = 0, int max = 12) =>
            new MiningRules(
                initial, firstCrystalDistance: 3, max, spawnInterval: 5, minCrystalSpacing: 2, spawnDistance: 12,
                crystalHealth: 10, crystalReward: 1, damage: 1, hitInterval: 1, walkSpeed: 4, miningRange: 1);

        [Test]
        public void EmptyQueue_HasNoFront()
        {
            var track = new CrystalTrack(Rules());
            Assert.That(track.Front, Is.Null);
            Assert.That(track.Count, Is.Zero);
        }

        [Test]
        public void Front_IsTheNearest_WhateverTheBirthOrder()
        {
            var track = new CrystalTrack(Rules());
            var far = track.SpawnAt(9);
            var near = track.SpawnAt(2);
            var middle = track.SpawnAt(5);

            Assert.That(track.Front, Is.SameAs(near));
            Assert.That(track.Crystals, Is.EqualTo(new[] { near, middle, far }));
        }

        [Test]
        public void SameDistance_KeepsBirthOrder()
        {
            var track = new CrystalTrack(Rules());
            var older = track.SpawnAt(4);
            var younger = track.SpawnAt(4);

            Assert.That(track.Crystals, Is.EqualTo(new[] { older, younger }));
        }

        /// <summary>D-60: os 10 cristais iniciais começam na tela, o primeiro a 3 un do anão, os outros a cada 2 un.</summary>
        [Test]
        public void InitialQueue_StartsAtTheFirstDistance_WithSpacing()
        {
            var track = new CrystalTrack(Rules(initial: 10));
            track.SpawnInitial(minerDistance: 0);

            Assert.That(track.Crystals.Select(c => c.Distance), Is.EqualTo(new[] { 3.0, 5, 7, 9, 11, 13, 15, 17, 19, 21 }));
            Assert.That(track.Crystals.Select(c => c.Id), Is.EqualTo(Enumerable.Range(1, 10)));
        }

        /// <summary>D-60: com a fila vazia ou curta, o novo nasce a 12 un do anão, fora da tela.</summary>
        [Test]
        public void NewCrystal_IsBornAtLeastTheSpawnDistanceAhead()
        {
            var track = new CrystalTrack(Rules());
            track.SpawnAt(3);

            var born = track.TrySpawn(minerDistance: 40);

            Assert.That(born.Distance, Is.EqualTo(52));
            Assert.That(track.Crystals.Last(), Is.SameAs(born), "no fim da fila");
        }

        /// <summary>D-54, D-60: com a fila longa, o novo entra no fim, a 2 un do último.</summary>
        [Test]
        public void NewCrystal_JoinsTheEnd_KeepingTheSpacing()
        {
            var track = new CrystalTrack(Rules(initial: 10));
            track.SpawnInitial(minerDistance: 0); // vai até 21

            var born = track.TrySpawn(minerDistance: 2);

            Assert.That(born.Distance, Is.EqualTo(23));
        }

        [Test]
        public void FullQueue_DoesNotSpawn_UnlessTheMaximumIsIgnored()
        {
            var track = new CrystalTrack(Rules(initial: 2, max: 2));
            track.SpawnInitial(minerDistance: 0);

            Assert.That(track.IsFull, Is.True);
            Assert.That(track.TrySpawn(0), Is.Null);
            Assert.That(track.TrySpawn(0, ignoreMax: true), Is.Not.Null, "o atalho do debug passa do máximo");
            Assert.That(track.Count, Is.EqualTo(3));
        }

        /// <summary>
        /// A sequência inteira na simulação: o anão quebra a fila do mais perto ao mais longe, mesmo com um cristal posto
        /// no meio depois (o "Gerar cristal" no meio da fila é só do debug e dos testes).
        /// </summary>
        [Test]
        public void FullSequence_BreaksTheQueueFromNearestToFarthest()
        {
            var rules = new MiningRules(
                initialCrystals: 3, firstCrystalDistance: 3, maxCrystals: 12, spawnInterval: 1e9, minCrystalSpacing: 2,
                spawnDistance: 12, crystalHealth: 1, crystalReward: 1, damage: 1, hitInterval: 1, walkSpeed: 4, miningRange: 1);
            var sim = new MiningSimulation(rules, new SeededRandom(1)); // cristais 1, 2, 3 a 3, 5 e 7
            var inserted = sim.SpawnCrystalAt(4);                        // cristal 4, entre o 1 e o 2
            var order = new List<int>();
            sim.CrystalBroken += c => order.Add(c.Id);

            sim.Tick(60);

            Assert.That(order, Is.EqualTo(new[] { 1, inserted.Id, 2, 3 }));
        }
    }
}
