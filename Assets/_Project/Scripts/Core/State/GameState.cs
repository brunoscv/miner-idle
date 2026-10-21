using System;

namespace MinersIdle.Core
{
    /// <summary>
    /// Estado do jogo que vale guardar (Arquitetura Técnica, D-T3: classe simples, nunca dentro de ScriptableObject).
    /// Contadores do loop e níveis das melhorias; o save entra no M4.
    /// </summary>
    public sealed class GameState
    {
        readonly int[] _levels = new int[Enum.GetValues(typeof(UpgradeId)).Length];

        /// <summary>A moeda única (LASCAS do HUD).</summary>
        public double Shards { get; private set; }

        /// <summary>Golpes de picareta dados (D-30: o "contar picaretas", exibido no debug).</summary>
        public long Hits { get; private set; }

        /// <summary>Cristais quebrados.</summary>
        public long Breaks { get; private set; }

        /// <summary>Nível da melhoria = compras feitas (D-38).</summary>
        public int LevelOf(UpgradeId id) => _levels[(int)id];

        internal void RegisterHit() => Hits++;

        internal void AddShards(double amount) => Shards += amount;

        /// <summary>Gasta lascas; nunca deixa o saldo negativo (a folga de arredondamento da compra não vira dívida).</summary>
        internal void SpendShards(double amount) => Shards = Math.Max(0, Shards - amount);

        internal void RaiseLevel(UpgradeId id) => _levels[(int)id]++;

        internal void RegisterBreak(double reward)
        {
            Breaks++;
            Shards += reward;
        }
    }
}
