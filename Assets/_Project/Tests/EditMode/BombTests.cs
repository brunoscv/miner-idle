using System.Collections.Generic;
using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9g E1 (D-240, D-255, D-307, D-308, D-309, D-313): a Bomba no núcleo. Carga de 5 s, voo de 0,4 s com o dano na
    /// chegada, a % da vida máxima em todos os cristais no alcance (os de fora, nada), sem crítico; quem quebra paga e conta
    /// na barra. A carga para no descanso e na transição, e a run começa cheia. A linha Bomba: 7 compras a 10 + 2^(n−1).
    /// Os números estão escritos à mão, como no GDD v8.
    /// </summary>
    public class BombTests
    {
        static readonly double[] V8Bombs = { 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 1 };

        /// <summary>Cristais de 100 de vida que não nascem sozinhos; o anão quase nunca bate (o intervalo é enorme).</summary>
        static MiningRules Rules(int caveCount = 1) =>
            new MiningRules(
                initialCrystals: 0, firstCrystalDistance: 1, maxCrystals: 12, spawnInterval: 1e9, minCrystalSpacing: 1,
                spawnDistance: 40, crystalHealth: 100, crystalReward: 3, damage: 1, hitInterval: 1e9, walkSpeed: 4,
                miningRange: 1, caveCount: caveCount);

        static StrikeRules Strike(double reach = 4, double flight = 0.4) =>
            new StrikeRules(0, 1.5, 0.33, 1, bombs: new BombRules(V8Bombs, 5, flight, reach));

        /// <summary>Só a linha Bomba ligada (as outras desligadas), como no GDD v8: 7 compras, 10 + 2^(n−1).</summary>
        static PermanentRules BombLine()
        {
            var lines = new UpgradeDefinition[(int)PermanentId.Bomb + 1];
            lines[(int)PermanentId.Bomb] = new UpgradeDefinition(10, 2, 7, 1, CostFormula.OffsetExponential);
            return new PermanentRules(10, new RedShardTable(10, 10, 5, 50, 10, 1), lines);
        }

        static MiningSimulation Sim(int bomb = 0, double reach = 4, double flight = 0.4, int caveCount = 1)
        {
            var rules = BombLine();
            var permanent = PermanentState.Restore(1000, 1, id => id == PermanentId.Bomb ? bomb : 0, rules);
            return new MiningSimulation(Rules(caveCount), new SeededRandom(3), null, null, Strike(reach, flight),
                                        new Permanents(rules, permanent));
        }

        // ── A carga (D-313) ──────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void TheRunStartsWithTheBombFull_AndThrowingEmptiesIt_AndItRefillsIn5Seconds()
        {
            var sim = Sim();
            sim.SpawnCrystalAt(sim.Miner.Distance + 1);

            Assert.That(sim.HasBomb, Is.True);
            Assert.That(sim.BombCharge, Is.EqualTo(1), "D-313: abre cheia");
            Assert.That(sim.ThrowBomb(), Is.True);
            Assert.That(sim.BombCharge, Is.EqualTo(0));
            Assert.That(sim.ThrowBomb(), Is.False, "vazia não sai");

            sim.Tick(2.5);
            Assert.That(sim.BombCharge, Is.EqualTo(0.5).Within(1e-9));
            Assert.That(sim.BombChargeLeft, Is.EqualTo(2.5).Within(1e-9));
            Assert.That(sim.CanThrowBomb, Is.False);

            sim.Tick(2.5);
            Assert.That(sim.IsBombCharged, Is.True, "5 s depois, cheia de novo");
            Assert.That(sim.CanThrowBomb, Is.True);
        }

        [Test]
        public void TheChargeStops_WhileResting_AndDuringTheFloorTransition()
        {
            var sim = Sim(caveCount: 3);
            sim.SpawnCrystalAt(sim.Miner.Distance + 1);
            sim.ThrowBomb();
            sim.Tick(1);

            sim.IsResting = true;
            sim.Tick(10);
            Assert.That(sim.BombCharge, Is.EqualTo(0.2).Within(1e-9), "descansando, a carga não anda");
            sim.IsResting = false;

            sim.FloorTransitionSeconds = 3;
            sim.FillFloorBar();
            Assert.That(sim.AdvanceFloor(), Is.True);
            Assert.That(sim.IsInFloorTransition, Is.True);
            sim.Tick(2);
            Assert.That(sim.BombCharge, Is.EqualTo(0.2).Within(1e-9), "na transição, a carga não anda");
        }

        [Test]
        public void WithoutACrystalInReach_TheBombDoesNotGo()
        {
            var sim = Sim(reach: 4);
            Assert.That(sim.CanThrowBomb, Is.False, "fila vazia");

            sim.SpawnCrystalAt(sim.Miner.Distance + 10);
            Assert.That(sim.HasCrystalInBombReach, Is.False);
            Assert.That(sim.ThrowBomb(), Is.False, "D-307: sem cristal no alcance, não sai");
            Assert.That(sim.BombCharge, Is.EqualTo(1), "e a carga fica");
        }

        // ── O voo e o dano (D-307, D-308, D-255) ────────────────────────────────────────────────────────────────

        [Test]
        public void TheDamageLandsAfterTheFlight_AsAFractionOfTheMaxHealth_EvenOnAHurtCrystal()
        {
            var sim = Sim();
            var crystal = sim.SpawnCrystalAt(sim.Miner.Distance + 1);
            crystal.ApplyDamage(50);
            var hits = new List<MiningHit>();
            sim.BombHitLanded += hits.Add;

            sim.ThrowBomb();
            Assert.That(sim.IsBombInFlight, Is.True);
            sim.Tick(0.39);
            Assert.That(crystal.Health, Is.EqualTo(50), "no ar, nada ainda");

            sim.Tick(0.01);
            Assert.That(sim.IsBombInFlight, Is.False);
            Assert.That(crystal.Health, Is.EqualTo(40).Within(1e-9), "a Pequena tira 10% da vida máxima (100), não da que sobra");
            Assert.That(hits.Count, Is.EqualTo(1));
            Assert.That(hits[0].Damage, Is.EqualTo(10).Within(1e-9));
            Assert.That(hits[0].IsCritical, Is.False, "sem crítico");
        }

        [Test]
        public void EveryCrystalInReachTakesTheHit_AndTheOnesBeyondDoNot()
        {
            var sim = Sim(bomb: 2, reach: 4); // a Normal: 30%
            double at = sim.Miner.Distance;
            var a = sim.SpawnCrystalAt(at + 1);
            var b = sim.SpawnCrystalAt(at + 2.5);
            var c = sim.SpawnCrystalAt(at + 4);
            var far = sim.SpawnCrystalAt(at + 6);
            IReadOnlyList<Crystal> thrownAt = null;
            sim.BombThrown += list => thrownAt = new List<Crystal>(list);

            sim.ThrowBomb();
            Assert.That(thrownAt, Is.EqualTo(new[] { a, b, c }), "voa para os três da tela");
            sim.Tick(0.4);

            Assert.That(a.Health, Is.EqualTo(70).Within(1e-9));
            Assert.That(b.Health, Is.EqualTo(70).Within(1e-9));
            Assert.That(c.Health, Is.EqualTo(70).Within(1e-9), "no limite do alcance também");
            Assert.That(far.Health, Is.EqualTo(100), "fora do alcance, nada");
        }

        [Test]
        public void TheBarrelBreaksEveryCrystalInReach_AndEachBreakPaysAndCounts()
        {
            var sim = Sim(bomb: 7); // o Barril: 100%
            double at = sim.Miner.Distance;
            sim.SpawnCrystalAt(at + 1);
            sim.SpawnCrystalAt(at + 2);
            sim.SpawnCrystalAt(at + 3);
            var far = sim.SpawnCrystalAt(at + 8);
            int broken = 0;
            sim.CrystalBroken += _ => broken++;

            Assert.That(sim.BombFraction, Is.EqualTo(1));
            sim.ThrowBomb();
            sim.Tick(0.4);

            Assert.That(broken, Is.EqualTo(3), "os três da tela quebram");
            Assert.That(sim.State.Shards, Is.EqualTo(9).Within(1e-9), "D-309: cada quebra paga como qualquer outra (3 × 3)");
            Assert.That(sim.State.CaveBreaks, Is.EqualTo(3), "e conta na barra do andar");
            Assert.That(sim.Crystals, Is.EqualTo(new[] { far }));
            sim.Tick(0.01);
            Assert.That(sim.Miner.Target, Is.SameAs(far), "o anão segue para o próximo");
        }

        [Test]
        public void TheAutomaticBomb_ThrowsWheneverItFills()
        {
            var sim = Sim(bomb: 0);
            var crystal = sim.SpawnCrystalAt(sim.Miner.Distance + 1);
            int thrown = 0;
            sim.BombThrown += _ => thrown++;
            sim.AutoBomb = true;

            sim.Tick(0.4);
            Assert.That(thrown, Is.EqualTo(1), "cheia: sai na hora");
            Assert.That(crystal.Health, Is.EqualTo(90).Within(1e-9));

            sim.Tick(5);
            Assert.That(thrown, Is.EqualTo(2), "5 s depois, outra");
            Assert.That(crystal.Health, Is.EqualTo(80).Within(1e-9));
        }

        // ── A linha Bomba (D-255, D-314) ────────────────────────────────────────────────────────────────────────

        [Test]
        public void TheBombLine_Has7Purchases_At10Plus2PowNMinus1_AndEachOneIsTheNextBomb()
        {
            var rules = BombLine();
            var permanent = PermanentState.Restore(1000, 1, _ => 0, rules);
            var permanents = new Permanents(rules, permanent);
            var sim = new MiningSimulation(Rules(), new SeededRandom(3), null, null, Strike(), permanents);
            sim.IsResting = true;

            var costs = new List<double>();
            var fractions = new List<double> { sim.BombFraction };
            for (int i = 0; i < 7; i++)
            {
                costs.Add(permanents.NextCost(PermanentId.Bomb).Value);
                Assert.That(sim.PurchasePermanent(PermanentId.Bomb), Is.EqualTo(PurchaseResult.Purchased));
                fractions.Add(sim.BombFraction);
            }

            Assert.That(costs, Is.EqualTo(new double[] { 11, 12, 14, 18, 26, 42, 74 }));
            Assert.That(fractions, Is.EqualTo(V8Bombs), "da Pequena ao Barril");
            Assert.That(permanents.NextCost(PermanentId.Bomb), Is.Null, "no Barril, acabou");
            Assert.That(sim.BombIndex, Is.EqualTo(7));
        }

        [Test]
        public void WithoutBombRules_ThereIsNoBomb()
        {
            var sim = new MiningSimulation(Rules(), new SeededRandom(3));
            sim.SpawnCrystalAt(sim.Miner.Distance + 1);
            Assert.That(sim.HasBomb, Is.False);
            Assert.That(sim.CanThrowBomb, Is.False);
            Assert.That(sim.ThrowBomb(), Is.False);
        }
    }
}
