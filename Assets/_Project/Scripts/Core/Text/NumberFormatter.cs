using System;
using System.Globalization;

namespace MinersIdle.Core
{
    /// <summary>
    /// O único formatador de números do jogo (Arquitetura Técnica, D-T4): arredonda só na exibição.
    /// pt-BR (D-41): milhar com ponto e decimal com vírgula ("1.234,5"). Desde a D-144 (2026-11-04), a partir de mil o
    /// número é abreviado com duas casas e sufixo K, M, B, T, Qa ou Qi ("1,23K", "12,35M", "3,93B"); acima do
    /// quintilhão, notação científica. Não depende da cultura do aparelho: os separadores são fixos aqui.
    /// </summary>
    public static class NumberFormatter
    {
        const double AbbreviationThreshold = 1000;
        /// <summary>Um sufixo por grupo de três algarismos: 1 = mil, 2 = milhão … 6 = quintilhão.</summary>
        static readonly string[] Suffixes = { "", "K", "M", "B", "T", "Qa", "Qi" };
        /// <summary>Centésimos do grupo: "999,99" = 99.999; a partir daqui o número sobe de grupo.</summary>
        const double GroupOverflow = 100000;

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

        /// <summary>
        /// Quantidades (dano, vida, custos): abaixo de mil, até duas casas decimais, só se houver fração ("12,5"); a
        /// partir de mil, abreviado com duas casas, arredondado (meio para longe do zero).
        /// </summary>
        public static string Format(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return value.ToString(PtBr);

            double abs = Math.Abs(value);
            string sign = value < 0 ? "-" : "";

            if (Math.Round(abs, 2) < AbbreviationThreshold) return sign + abs.ToString("#,0.##", PtBr);
            return sign + Abbreviate(abs, truncate: false);
        }

        /// <summary>
        /// Lascas (D-206, revê a D-68): até duas casas decimais, cortadas para baixo ("4,57" para 4,579), para a tela nunca
        /// mostrar mais lascas do que se tem; inteiros sem vírgula ("12"). A partir de mil, a abreviação também trunca:
        /// 1.239 lascas = "1,23K", 999.999 = "999,99K".
        /// </summary>
        public static string FormatShards(double shards)
        {
            if (double.IsNaN(shards) || double.IsInfinity(shards)) return shards.ToString(PtBr);

            // Em centésimos inteiros: o Round tira o ruído do double (0,29 × 100 = 28,999…) antes de cortar.
            double cents = Math.Floor(Math.Round(shards * 100, 6));
            double truncated = cents / 100;
            if (truncated < 0) return Format(truncated);
            if (truncated < AbbreviationThreshold) return truncated.ToString("#,0.##", PtBr);
            return Abbreviate(Math.Floor(shards), truncate: true);
        }

        /// <summary>
        /// Custo de uma compra em lascas (D-203): abaixo de mil, com até duas casas decimais como o <see cref="Format"/>
        /// ("4,2", "12,94"; os inteiros seguem sem vírgula), porque desde a D-202 a Picareta cobra a fração. A partir de mil,
        /// igual às lascas (<see cref="FormatShards"/>, D-144).
        /// </summary>
        public static string FormatCost(double cost)
        {
            if (double.IsNaN(cost) || double.IsInfinity(cost)) return cost.ToString(PtBr);
            return Math.Abs(cost) < AbbreviationThreshold ? Format(cost) : FormatShards(cost);
        }

        /// <summary>
        /// "1,23K" … "999,99Qi" para <paramref name="abs"/> ≥ 1000 (ou que arredonda para 1000). Conta em centésimos
        /// inteiros do grupo (valor ÷ 10^(3·grupo − 2)): a divisão por potência de 10 é exata quando o resultado é
        /// inteiro, e assim 12.345 vira 1.234,5 centésimos e arredonda para "12,35", sem o erro de 12,345 em binário.
        /// </summary>
        static string Abbreviate(double abs, bool truncate)
        {
            int group = Math.Max(1, (int)Math.Floor(Math.Log10(abs) / 3));
            double hundredths = Hundredths(abs, group, truncate);
            if (hundredths >= GroupOverflow)
            {
                group++;
                hundredths = Hundredths(abs, group, truncate);
            }

            if (group >= Suffixes.Length) return abs.ToString("0.00e0", PtBr);

            long units = (long)hundredths;
            return (units / 100).ToString(PtBr) + "," + (units % 100).ToString("00") + Suffixes[group];
        }

        static double Hundredths(double abs, int group, bool truncate)
        {
            double scaled = abs / Math.Pow(10, 3 * group - 2);
            return truncate ? Math.Floor(scaled) : Math.Round(scaled, MidpointRounding.AwayFromZero);
        }

        /// <summary>Tempo em segundos com duas casas ("1,00s"), como a VELOCIDADE do HUD.</summary>
        public static string FormatSeconds(double seconds) => seconds.ToString("0.00", PtBr) + "s";

        /// <summary>
        /// Tempo de jogo (M8, E3): "45 s", "12 min 05 s", "3 h 07 min"; a partir de uma hora, sem os segundos. Arredonda
        /// para baixo, como as lascas.
        /// </summary>
        public static string FormatDuration(double seconds)
        {
            long total = double.IsNaN(seconds) || seconds < 0 ? 0 : (long)Math.Min(Math.Floor(seconds), long.MaxValue);
            long hours = total / 3600, minutes = total / 60 % 60, rest = total % 60;
            if (hours > 0) return hours.ToString("#,0", PtBr) + " h " + minutes.ToString("00") + " min";
            if (minutes > 0) return minutes + " min " + rest.ToString("00") + " s";
            return rest + " s";
        }
    }
}
