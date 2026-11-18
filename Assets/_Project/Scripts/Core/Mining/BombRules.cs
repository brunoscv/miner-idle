using System;
using System.Collections.Generic;

namespace MinersIdle.Core
{
    /// <summary>
    /// A Bomba do GDD v8 (M9g, D-240, D-307, D-308), somente leitura: as 8 bombas (a fração da vida máxima que cada uma
    /// tira, na ordem da compra), a carga, o voo e o alcance. A bomba não acaba: lançada, carrega de novo. Não pega
    /// crítico nem bônus (D-255). Os valores vêm do balanço; sem eles (<see cref="None"/>), não há bomba (o jogo até o v7).
    /// </summary>
    public sealed class BombRules
    {
        /// <param name="fractions">
        /// A fração da vida máxima de cada bomba, da que vem de graça à última compra (GDD v8: 0,1 0,2 0,3 0,4 0,5 0,6 0,7 1).
        /// </param>
        /// <param name="chargeSeconds">Segundos de jogo para a bomba encher (GDD v8: 5).</param>
        /// <param name="flightSeconds">Segundos no ar, do lançamento à explosão (D-308: 0,4); 0 = explode na hora.</param>
        /// <param name="reach">
        /// Até onde, à frente do anão, a bomba atinge os cristais, em unidades (D-307: o que a tela mostra do corredor).
        /// </param>
        public BombRules(IEnumerable<double> fractions, double chargeSeconds, double flightSeconds, double reach)
        {
            var list = fractions == null ? new List<double>() : new List<double>(fractions);
            foreach (var f in list)
                if (double.IsNaN(f) || f <= 0 || f > 1) throw new ArgumentOutOfRangeException(nameof(fractions), "Cada bomba tira de 0 a 100%.");
            if (double.IsNaN(chargeSeconds) || chargeSeconds < 0) throw new ArgumentOutOfRangeException(nameof(chargeSeconds));
            if (double.IsNaN(flightSeconds) || flightSeconds < 0) throw new ArgumentOutOfRangeException(nameof(flightSeconds));
            if (double.IsNaN(reach) || reach < 0) throw new ArgumentOutOfRangeException(nameof(reach));

            Fractions = list;
            ChargeSeconds = chargeSeconds;
            FlightSeconds = flightSeconds;
            Reach = reach;
        }

        /// <summary>A fração da vida máxima de cada bomba, na ordem da compra.</summary>
        public IReadOnlyList<double> Fractions { get; }

        public int Count => Fractions.Count;
        public double ChargeSeconds { get; }
        public double FlightSeconds { get; }
        public double Reach { get; }

        /// <summary>O balanço tem bomba: alguma bomba e uma carga.</summary>
        public bool IsEnabled => Fractions.Count > 0 && ChargeSeconds > 0;

        /// <summary>A fração da bomba <paramref name="index"/> (0 = a de graça), presa entre a primeira e a última.</summary>
        public double FractionAt(int index) =>
            Fractions.Count == 0 ? 0 : Fractions[Math.Max(0, Math.Min(Fractions.Count - 1, index))];

        /// <summary>Sem bomba: o jogo até o GDD v7 e os testes antigos.</summary>
        public static BombRules None() => new BombRules(null, 0, 0, 0);
    }
}
