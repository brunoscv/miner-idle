using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>Formato provisório pt-BR (D-11 em aberto): o que o HUD mostra.</summary>
    public class NumberFormatterTests
    {
        [TestCase(0, "0")]
        [TestCase(1, "1")]
        [TestCase(12.5, "12,5")]
        [TestCase(0.125, "0,13")]
        [TestCase(1234, "1.234")]
        [TestCase(1234.567, "1.234,57")]
        [TestCase(999999, "999.999")]
        [TestCase(-1234, "-1.234")]
        public void SmallNumbers_UsePtBrSeparators(double value, string expected)
        {
            Assert.That(NumberFormatter.Format(value), Is.EqualTo(expected));
        }

        [TestCase(1e6, "1,00M")]
        [TestCase(1234567, "1,23M")]
        [TestCase(12345678, "12,3M")]
        [TestCase(123456789, "123M")]
        [TestCase(999999.999, "1,00M")]
        [TestCase(999_600_000, "1,00B")]
        [TestCase(1e9, "1,00B")]
        [TestCase(2.5e12, "2,50T")]
        [TestCase(-3.2e6, "-3,20M")]
        public void BigNumbers_AreAbbreviatedWithThreeDigits(double value, string expected)
        {
            Assert.That(NumberFormatter.Format(value), Is.EqualTo(expected));
        }

        [TestCase(0, "0")]
        [TestCase(0.99, "0")]
        [TestCase(12234.5, "12.234")]
        [TestCase(1234.999, "1.234")]
        [TestCase(1234567.8, "1,23M")]
        public void Shards_AreWholeNumbers_RoundedDown(double shards, string expected)
        {
            Assert.That(NumberFormatter.FormatShards(shards), Is.EqualTo(expected));
        }

        [Test]
        public void BeyondTrillions_UsesScientificNotation()
        {
            Assert.That(NumberFormatter.Format(1.5e15), Is.EqualTo("1,50e15"));
        }

        [TestCase(1, "1,00s")]
        [TestCase(2.3, "2,30s")]
        [TestCase(0.5, "0,50s")]
        public void Seconds_HaveTwoDecimals(double value, string expected)
        {
            Assert.That(NumberFormatter.FormatSeconds(value), Is.EqualTo(expected));
        }

        /// <summary>M8 (E3): o tempo de jogo da tela do coração da montanha, arredondado para baixo.</summary>
        [TestCase(0, "0 s")]
        [TestCase(45.9, "45 s")]
        [TestCase(725, "12 min 05 s")]
        [TestCase(3600, "1 h 00 min")]
        [TestCase(11250, "3 h 07 min")]
        [TestCase(4_000_000, "1.111 h 06 min")]
        [TestCase(-5, "0 s")]
        public void Duration_IsHoursMinutesSeconds(double seconds, string expected)
        {
            Assert.That(NumberFormatter.FormatDuration(seconds), Is.EqualTo(expected));
        }
    }
}
