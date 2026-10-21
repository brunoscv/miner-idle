using System;
using System.Collections.Generic;

namespace MinersIdle.Core
{
    /// <summary>
    /// Regra de prioridade de alvo (D-27, REQ-MAP-03): <b>menor lote</b>; no mesmo lote, <b>o mais próximo</b> de
    /// <c>from</c>; empate exato de distância, <b>o que nasceu primeiro</b> (menor <see cref="Crystal.Id"/>).
    /// Cristais quebrados são ignorados. Função pura e sem alocação: pode rodar a cada quebra no loop de mineração.
    /// </summary>
    public static class TargetSelector
    {
        /// <summary>Devolve o próximo alvo, ou <c>null</c> se não houver cristal inteiro.</summary>
        public static Crystal Select(IReadOnlyList<Crystal> crystals, Vec2 from)
        {
            if (crystals == null) throw new ArgumentNullException(nameof(crystals));

            Crystal best = null;
            double bestDistance = 0;

            for (int i = 0; i < crystals.Count; i++)
            {
                var candidate = crystals[i];
                if (candidate == null || candidate.IsBroken) continue;

                double distance = Vec2.DistanceSquared(from, candidate.Position);
                if (best == null || IsBetter(candidate, distance, best, bestDistance))
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }

        static bool IsBetter(Crystal candidate, double candidateDistance, Crystal best, double bestDistance)
        {
            if (candidate.Batch != best.Batch) return candidate.Batch < best.Batch;
            if (candidateDistance != bestDistance) return candidateDistance < bestDistance;
            return candidate.Id < best.Id;
        }
    }
}
