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

        /// <summary>
        /// Recria o estado a partir de um save (Marco 4). Corrige o que estiver fora do lugar em vez de falhar: lascas
        /// negativas ou inválidas viram 0, contadores negativos viram 0, níveis ficam entre 0 e o limite da linha em
        /// <paramref name="economy"/> (um asset novo pode ter limite menor que o do save).
        /// </summary>
        public static GameState Restore(double shards, long hits, long breaks,
                                        Func<UpgradeId, int> levelOf, EconomyRules economy)
        {
            if (levelOf == null) throw new ArgumentNullException(nameof(levelOf));
            if (economy == null) throw new ArgumentNullException(nameof(economy));

            var state = new GameState
            {
                Shards = double.IsNaN(shards) || double.IsInfinity(shards) || shards < 0 ? 0 : shards,
                Hits = Math.Max(0, hits),
                Breaks = Math.Max(0, breaks),
            };
            foreach (UpgradeId id in Enum.GetValues(typeof(UpgradeId)))
                state._levels[(int)id] = Math.Min(Math.Max(0, levelOf(id)), economy.Get(id).MaxLevel);
            return state;
        }

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
