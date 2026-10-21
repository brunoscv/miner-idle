using System;
using System.Linq;
using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>Tipos básicos do Núcleo usados pela regra de alvo: vetor, gerador com semente e cristal.</summary>
    public class CoreBasicsTests
    {
        [Test]
        public void Vec2_Distance()
        {
            Assert.That(Vec2.Distance(new Vec2(1, 1), new Vec2(4, 5)), Is.EqualTo(5).Within(1e-12));
            Assert.That(Vec2.DistanceSquared(new Vec2(1, 1), new Vec2(4, 5)), Is.EqualTo(25));
            Assert.That(new Vec2(1, 2) + new Vec2(3, 4) - new Vec2(1, 1), Is.EqualTo(new Vec2(3, 5)));
            Assert.That(new Vec2(1, -2) * 3, Is.EqualTo(new Vec2(3, -6)));
        }

        [Test]
        public void SeededRandom_SameSeed_SameSequence()
        {
            var a = new SeededRandom(1234);
            var b = new SeededRandom(1234);
            for (int i = 0; i < 1000; i++)
                Assert.That(a.NextULong(), Is.EqualTo(b.NextULong()));
        }

        [Test]
        public void SeededRandom_DifferentSeeds_DifferentSequences()
        {
            var a = new SeededRandom(1);
            var b = new SeededRandom(2);
            var seqA = Enumerable.Range(0, 10).Select(_ => a.NextULong()).ToArray();
            var seqB = Enumerable.Range(0, 10).Select(_ => b.NextULong()).ToArray();
            Assert.That(seqA, Is.Not.EqualTo(seqB));
        }

        /// <summary>
        /// Fixa o algoritmo (SplitMix64): se este teste quebrar, partidas salvas com semente deixam de se repetir.
        /// Valores calculados fora do Unity.
        /// </summary>
        [Test]
        public void SeededRandom_KnownValues()
        {
            var r = new SeededRandom(42);
            Assert.That(r.NextULong(), Is.EqualTo(13679457532755275413UL));
            Assert.That(r.NextULong(), Is.EqualTo(2949826092126892291UL));
            Assert.That(r.NextULong(), Is.EqualTo(5139283748462763858UL));
        }

        [Test]
        public void SeededRandom_StaysInsideRanges()
        {
            var r = new SeededRandom(7);
            for (int i = 0; i < 10000; i++)
            {
                double d = r.NextDouble();
                Assert.That(d, Is.GreaterThanOrEqualTo(0).And.LessThan(1));

                int n = r.NextInt(-3, 4);
                Assert.That(n, Is.InRange(-3, 3));

                double x = r.Range(-7.5, 7.5);
                Assert.That(x, Is.GreaterThanOrEqualTo(-7.5).And.LessThan(7.5));
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInt(5, 5));
        }

        [Test]
        public void Crystal_BreaksOnce_AndHealthNeverGoesNegative()
        {
            var c = new Crystal(1, 1, Vec2.Zero, 10, 1);

            Assert.That(c.ApplyDamage(4), Is.False);
            Assert.That(c.Health, Is.EqualTo(6));
            Assert.That(c.ApplyDamage(100), Is.True, "o golpe que quebra devolve true");
            Assert.That(c.Health, Is.EqualTo(0));
            Assert.That(c.IsBroken, Is.True);
            Assert.That(c.ApplyDamage(1), Is.False, "cristal já quebrado não quebra de novo (recompensa uma vez)");
        }

        [Test]
        public void Crystal_RejectsInvalidValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Crystal(1, 0, Vec2.Zero, 10, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Crystal(1, 1, Vec2.Zero, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Crystal(1, 1, Vec2.Zero, 10, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Crystal(1, 1, Vec2.Zero, 10, 1).ApplyDamage(-1));
        }
    }
}
