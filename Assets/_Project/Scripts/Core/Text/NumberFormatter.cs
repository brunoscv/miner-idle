using System;
using System.Globalization;

namespace MinersIdle.Core
{
    /// <summary>
    /// O único formatador de números do jogo (Arquitetura Técnica, D-T4): arredonda só na exibição.
    /// Formato <b>provisório</b> pt-BR até a decisão D-11 (M4): milhar com ponto e decimal com vírgula ("1.234,5"); a
    /// partir de 1 milhão, três algarismos e sufixo M/B/T ("1,23M", "12,3B", "123T"); acima disso, notação científica.
    /// Não depende da cultura do aparelho: os separadores são fixos aqui.
    /// </summary>
    public static class NumberFormatter
    {
        const double AbbreviationThreshold = 1e6;
        static readonly string[] Suffixes = { "M", "B", "T" };

        static readonly NumberFormatInfo PtBr = new NumberFormatInfo
        {
            NumberGroupSeparator = ".",
            NumberDecimalSeparator = ",",
            NumberGroupSizes = new[] { 3 },
            NegativeSign = "-",
            PositiveInfinitySymbol = "∞",
            NegativeInfinitySymbol = "-∞",
            NaNSymbol = "?",
        };

        /// <summary>Quantidades (lascas, dano): até duas casas decimais, só se houver fração.</summary>
        public static string Format(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return value.ToString(PtBr);

            double abs = Math.Abs(value);
            string sign = value < 0 ? "-" : "";

            if (Math.Round(abs, 2) < AbbreviationThreshold) return sign + abs.ToString("#,0.##", PtBr);

            int group = (int)Math.Floor(Math.Log10(abs) / 3); // 2 = milhão, 3 = bilhão, 4 = trilhão
            double scaled = abs / Math.Pow(1000, group);
            string digits = ThreeSignificant(scaled, out bool overflowed);
            if (overflowed)
            {
                group++;
                digits = ThreeSignificant(scaled / 1000, out _);
            }

            int suffix = group - 2;
            if (suffix >= Suffixes.Length) return sign + abs.ToString("0.00e0", PtBr);
            return sign + digits + Suffixes[suffix];
        }

        /// <summary>
        /// Lascas (D-68): sempre inteiro, arredondado para baixo ("12.234"), para a tela nunca mostrar mais lascas do que
        /// se tem. A fração continua no estado. Acima de 1 milhão, a abreviação de <see cref="Format"/>.
        /// </summary>
        public static string FormatShards(double shards) => Format(Math.Floor(shards));

        /// <summary>Tempo em segundos com duas casas ("1,00s"), como a VELOCIDADE do HUD.</summary>
        public static string FormatSeconds(double seconds) => seconds.ToString("0.00", PtBr) + "s";

        /// <summary>1,23 / 12,3 / 123. <paramref name="overflowed"/> quando o arredondamento chega a 1000 (999,95 → 1000).</summary>
        static string ThreeSignificant(double scaled, out bool overflowed)
        {
            // Limiares de arredondamento de ToString (meio para longe do zero): 9,995 vira "10,0", 999,5 vira "1000".
            string format = scaled < 9.995 ? "0.00" : scaled < 99.95 ? "0.0" : "0";
            overflowed = scaled >= 999.5;
            return scaled.ToString(format, PtBr);
        }
    }
}
