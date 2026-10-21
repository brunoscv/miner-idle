namespace MinersIdle.Core
{
    /// <summary>
    /// Marcador do assembly do Núcleo: as regras do jogo (fórmulas, estados, simulação) vivem aqui, em C# puro,
    /// sem referência à UnityEngine (asmdef com "No Engine References"). Ver docs/planning/Arquitetura Tecnica.md, D-T1.
    /// </summary>
    public static class CoreAssembly
    {
        public const string Name = "MinersIdle.Core";
    }
}
