using System.Collections.Generic;
using System.Linq;
using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>Regra de alvo "lote, depois mais próximo" (D-27, REQ-MAP-03).</summary>
    public class TargetSelectorTests
    {
        static Crystal C(int id, int batch, double x, double y) => new Crystal(id, batch, new Vec2(x, y), 10, 1);

        [Test]
        public void NoCrystals_GivesNoTarget()
        {
            Assert.That(TargetSelector.Select(new List<Crystal>(), Vec2.Zero), Is.Null);
        }

        [Test]
        public void OnlyBrokenCrystals_GivesNoTarget()
        {
            var a = C(1, 1, 1, 0);
            a.ApplyDamage(a.MaxHealth);

            Assert.That(TargetSelector.Select(new[] { a }, Vec2.Zero), Is.Null);
        }

        [Test]
        public void LowerBatch_WinsEvenWhenFarAway()
        {
            var farOld = C(1, 1, 7, 10);
            var nearNew = C(2, 2, 0.5, 0);

            Assert.That(TargetSelector.Select(new[] { nearNew, farOld }, Vec2.Zero), Is.SameAs(farOld));
        }

        [Test]
        public void SameBatch_NearestWins()
        {
            var far = C(1, 1, 5, 0);
            var near = C(2, 1, 0, -2);

            Assert.That(TargetSelector.Select(new[] { far, near }, Vec2.Zero), Is.SameAs(near));
        }

        [Test]
        public void ExactDistanceTie_EarliestBornWins()
        {
            var younger = C(5, 1, 3, 0);
            var older = C(3, 1, -3, 0);

            Assert.That(TargetSelector.Select(new[] { younger, older }, Vec2.Zero), Is.SameAs(older));
        }

        [Test]
        public void BrokenCrystal_IsIgnored()
        {
            var nearestButBroken = C(1, 1, 1, 0);
            nearestButBroken.ApplyDamage(nearestButBroken.MaxHealth);
            var other = C(2, 1, 4, 0);

            Assert.That(TargetSelector.Select(new[] { nearestButBroken, other }, Vec2.Zero), Is.SameAs(other));
        }

        [Test]
        public void ListOrder_DoesNotChangeTheChoice()
        {
            var crystals = new List<Crystal> { C(1, 2, 0.1, 0), C(2, 1, 6, 6), C(3, 1, 2, 2), C(4, 1, -2, -2), C(5, 3, 0, 0.2) };
            var expected = TargetSelector.Select(crystals, Vec2.Zero);

            for (int shift = 1; shift < crystals.Count; shift++)
            {
                var rotated = crystals.Skip(shift).Concat(crystals.Take(shift)).ToList();
                Assert.That(TargetSelector.Select(rotated, Vec2.Zero), Is.SameAs(expected));
                rotated.Reverse();
                Assert.That(TargetSelector.Select(rotated, Vec2.Zero), Is.SameAs(expected));
            }

            Assert.That(expected.Id, Is.EqualTo(3), "lote 1; (2,2) e (-2,-2) empatam na distância → id menor");
        }

        /// <summary>
        /// O exemplo do Bruno: o lote inicial é limpo de vizinho em vizinho; depois a fila segue a ordem de nascimento,
        /// mesmo com um cristal novo colado no anão.
        /// </summary>
        [Test]
        public void FullSequence_ClearsInitialBatchByProximity_ThenFollowsBirthOrder()
        {
            var crystals = new List<Crystal>
            {
                C(1, 1, 5, 0),
                C(2, 1, 1, 0),
                C(3, 1, 6, 1),
                C(4, 2, 0.5, 0),  // nasceu depois, colado na posição inicial do anão
                C(5, 3, -8, 0),   // longe
                C(6, 4, 2, 0),    // perto, mas nasceu depois do 5
            };

            var minerPosition = Vec2.Zero;
            var order = new List<int>();
            for (var target = TargetSelector.Select(crystals, minerPosition); target != null; target = TargetSelector.Select(crystals, minerPosition))
            {
                order.Add(target.Id);
                minerPosition = target.Position;  // o anão anda até o alvo
                target.ApplyDamage(target.MaxHealth);
            }

            Assert.That(order, Is.EqualTo(new[] { 2, 1, 3, 4, 5, 6 }));
        }
    }
}
