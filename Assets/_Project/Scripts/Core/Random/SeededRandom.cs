using System;

namespace MinersIdle.Core
{
    /// <summary>
    /// Gerador SplitMix64 com semente. Não usa <c>System.Random</c> porque a sequência dele pode mudar entre versões
    /// do .NET/Mono/IL2CPP; esta é só aritmética de inteiros e dá a mesma sequência no Editor e no celular.
    /// </summary>
    public sealed class SeededRandom : IRandom
    {
        const double UnitScale = 1.0 / (1UL << 53);

        ulong _state;

        public SeededRandom(ulong seed)
        {
            Seed = seed;
            _state = seed;
        }

        public ulong Seed { get; }

        public ulong NextULong()
        {
            unchecked
            {
                _state += 0x9E3779B97F4A7C15UL;
                ulong z = _state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        public double NextDouble() => (NextULong() >> 11) * UnitScale;

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "maxExclusive precisa ser maior que minInclusive.");

            long span = (long)maxExclusive - minInclusive;
            long offset = (long)(NextDouble() * span);
            return (int)(minInclusive + Math.Min(offset, span - 1));
        }

        public double Range(double min, double max) => min + NextDouble() * (max - min);
    }
}
