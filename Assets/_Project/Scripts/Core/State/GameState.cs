namespace MinersIdle.Core
{
    /// <summary>
    /// Estado do jogo que vale guardar (Arquitetura Técnica, D-T3: classe simples, nunca dentro de ScriptableObject).
    /// No M2 só tem os contadores do loop; o save entra no M4.
    /// </summary>
    public sealed class GameState
    {
        /// <summary>A moeda única (LASCAS do HUD).</summary>
        public double Shards { get; private set; }

        /// <summary>Golpes de picareta dados (D-30: o "contar picaretas", exibido no debug).</summary>
        public long Hits { get; private set; }

        /// <summary>Cristais quebrados.</summary>
        public long Breaks { get; private set; }

        internal void RegisterHit() => Hits++;

        internal void AddShards(double amount) => Shards += amount;

        internal void RegisterBreak(double reward)
        {
            Breaks++;
            Shards += reward;
        }
    }
}
