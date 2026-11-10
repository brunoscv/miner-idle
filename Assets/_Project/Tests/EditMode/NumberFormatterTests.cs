using MinersIdle.Core;
using NUnit.Framework;

namespace MinersIdle.Tests
{
    /// <summary>Formato pt-BR (D-41) com as abreviações da D-144: o que o HUD e a loja mostram.</summary>
    public class NumberFormatterTests
    {
        [TestCase(0, "0")]
        [TestCase(1, "1")]
        [TestCase(12.5, "12,5")]
        [TestCase(0.125, "0,13")]
        [TestCase(999, "999")]
        [TestCase(999.5, "999,5")]
        [TestCase(-12.5, "-12,5")]
        public void BelowAThousand_UsesPtBrDecimals(double value, string expected)
        {
            Assert.That(NumberFormatter.Format(value), Is.EqualTo(expected));
        }

        /// <summary>D-144: a partir de mil, duas casas e K/M/B/T/Qa/Qi; ao arredondar para 1.000,00, sobe de grupo.</summary>
        [TestCase(1000, "1,00K")]
        [TestCase(1234, "1,23K")]
        [TestCase(1234.567, "1,23K")]
        [TestCase(12345, "12,35K")]
        [TestCase(999.996, "1,00K")]
        [TestCase(999999, "1,00M")]
        [TestCase(1e6, "1,00M")]
        [TestCase(1234567, "1,23M")]
        [TestCase(12345678, "12,35M")]
        [TestCase(123456789, "123,46M")]
        [TestCase(999_600_000, "999,60M")]
        [TestCase(999_999_999, "1,00B")]
        [TestCase(1e9, "1,00B")]
        [TestCase(3927274772.24, "3,93B")]
        [TestCase(2.5e12, "2,50T")]
        [TestCase(190.7e12, "190,70T")]
        [TestCase(1e15, "1,00Qa")]
        [TestCase(1.5e18, "1,50Qi")]
        [TestCase(-1234, "-1,23K")]
        [TestCase(-3.2e6, "-3,20M")]
        public void FromAThousand_IsAbbreviatedWithTwoDecimals(double value, string expected)
        {
            Assert.That(NumberFormatter.Format(value), Is.EqualTo(expected));
        }

        /// <summary>D-206 + D-144: lascas com até duas casas, truncadas, para nunca mostrar mais do que se tem.</summary>
        [TestCase(0, "0")]
        [TestCase(0.99, "0,99")]
        [TestCase(0.29, "0,29")]
        [TestCase(4.579, "4,57")]
        [TestCase(4.2, "4,2")]
        [TestCase(12, "12")]
        [TestCase(999.9, "999,9")]
        [TestCase(999.999, "999,99")]
        [TestCase(1239, "1,23K")]
        [TestCase(1230, "1,23K")]
        [TestCase(12234.5, "12,23K")]
        [TestCase(999999, "999,99K")]
        [TestCase(1234567.8, "1,23M")]
        [TestCase(1_999_999_999, "1,99B")]
        public void Shards_KeepTwoDecimals_RoundedDown(double shards, string expected)
        {
            Assert.That(NumberFormatter.FormatShards(shards), Is.EqualTo(expected));
        }

        /// <summary>D-203: o custo mostra a fração abaixo de mil (a Picareta da D-202 cobra 4,20); de mil em diante, como as lascas.</summary>
        [TestCase(0, "0")]
        [TestCase(4.2, "4,2")]
        [TestCase(12.9356, "12,94")]
        [TestCase(69.1908, "69,19")]
        [TestCase(417.5097, "417,51")]
        [TestCase(50, "50")]
        [TestCase(999.994, "999,99")]
        [TestCase(999.996, "1,00K")]
        [TestCase(3705.92, "3,70K")]
        [TestCase(79721.23, "79,72K")]
        public void Costs_ShowTheDecimals_BelowAThousand(double cost, string expected)
        {
            Assert.That(NumberFormatter.FormatCost(cost), Is.EqualTo(expected));
        }

        [Test]
        public void BeyondQuintillions_UsesScientificNotation()
        {
            Assert.That(NumberFormatter.Format(1.5e21), Is.EqualTo("1,50e21"));
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
