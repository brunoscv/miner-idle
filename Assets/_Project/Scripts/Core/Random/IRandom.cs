namespace MinersIdle.Core
{
    /// <summary>
    /// Fonte de aleatoriedade injetada no Núcleo (Arquitetura Técnica, D-T2). Com a mesma semente, a partida inteira
    /// se repete: é o que permite reproduzir um bug e testar sem sorte.
    /// </summary>
    public interface IRandom
    {
        /// <summary>Número em [0, 1).</summary>
        double NextDouble();

        /// <summary>Inteiro em [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).</summary>
        int NextInt(int minInclusive, int maxExclusive);

        /// <summary>Número em [<paramref name="min"/>, <paramref name="max"/>).</summary>
        double Range(double min, double max);
    }
}
