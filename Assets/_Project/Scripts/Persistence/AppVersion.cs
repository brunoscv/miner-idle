using System;

namespace MinersIdle.Persistence
{
    /// <summary>
    /// Compara versões do app (M9i E4, D-337): partes separadas por ponto, cada uma comparada como número
    /// (<c>0.1.0.2611191530</c> &gt; <c>0.1.0.2611181200</c> &gt; <c>0.1.0</c>). Uma parte que falta vale 0; uma parte que
    /// não é número compara como texto. A versão vazia (um save de antes da E4) é mais velha que qualquer outra.
    /// </summary>
    public static class AppVersion
    {
        /// <summary><c>true</c> se <paramref name="saved"/> é mais velha que <paramref name="current"/>.</summary>
        public static bool IsOlder(string saved, string current)
        {
            if (string.IsNullOrWhiteSpace(current)) return false;
            if (string.IsNullOrWhiteSpace(saved)) return true;
            return Compare(saved.Trim(), current.Trim()) < 0;
        }

        /// <summary>Negativo se <paramref name="a"/> vem antes de <paramref name="b"/>, zero se iguais, positivo depois.</summary>
        public static int Compare(string a, string b)
        {
            var left = (a ?? "").Split('.');
            var right = (b ?? "").Split('.');
            int count = Math.Max(left.Length, right.Length);
            for (int i = 0; i < count; i++)
            {
                string x = i < left.Length ? left[i] : "0";
                string y = i < right.Length ? right[i] : "0";
                int result = long.TryParse(x, out long nx) && long.TryParse(y, out long ny)
                    ? nx.CompareTo(ny)
                    : string.CompareOrdinal(x, y);
                if (result != 0) return result;
            }
            return 0;
        }
    }
}
