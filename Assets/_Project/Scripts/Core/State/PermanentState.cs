using System;

namespace MinersIdle.Core
{
    /// <summary>
    /// O que fica de um dia para o outro (Marco 8b, D-137): o banco de lascas vermelhas, os níveis das melhorias
    /// permanentes e o dia. Separado do <see cref="GameState"/> (a run), que Descansar troca por um novo. Classe simples
    /// (D-T3), nunca dentro de ScriptableObject; muda só pela simulação (<see cref="MiningSimulation"/>), que avisa.
    /// </summary>
    public sealed class PermanentState
    {
        readonly int[] _levels = new int[Enum.GetValues(typeof(PermanentId)).Length];

        /// <summary>Lascas vermelhas já levadas para casa (o banco).</summary>
        public double RedShards { get; private set; }

        /// <summary>O dia em curso: começa no 1 e soma um a cada descanso (o resumo do dia mostra, D-154).</summary>
        public int Day { get; private set; } = 1;

        /// <summary>Nível da melhoria permanente = compras feitas (D-38).</summary>
        public int LevelOf(PermanentId id) => _levels[(int)id];

        /// <summary>
        /// Recria o estado a partir de um save (E2). Corrige o que estiver fora do lugar em vez de falhar: banco inválido
        /// vira 0, dia abaixo de 1 vira 1, níveis ficam entre 0 e o limite da linha em <paramref name="rules"/>. Uma linha
        /// desligada nas regras (o GDD v5, que não tem permanentes) guarda o nível como veio: sem efeito ali, e de volta no
        /// balanço que a tem.
        /// </summary>
        public static PermanentState Restore(double redShards, int day, Func<PermanentId, int> levelOf, PermanentRules rules)
        {
            if (levelOf == null) throw new ArgumentNullException(nameof(levelOf));
            if (rules == null) throw new ArgumentNullException(nameof(rules));

            var state = new PermanentState
            {
                RedShards = double.IsNaN(redShards) || double.IsInfinity(redShards) || redShards < 0 ? 0 : redShards,
                Day = Math.Max(1, day),
            };
            foreach (PermanentId id in Enum.GetValues(typeof(PermanentId)))
            {
                var line = rules.Get(id);
                state._levels[(int)id] = Math.Min(Math.Max(0, levelOf(id)), line.IsEnabled ? line.MaxLevel : int.MaxValue);
            }
            return state;
        }

        internal void AddRedShards(double amount) => RedShards += amount;

        /// <summary>Gasta lascas vermelhas; nunca deixa o banco negativo (a folga de arredondamento não vira dívida).</summary>
        internal void SpendRedShards(double amount) => RedShards = Math.Max(0, RedShards - amount);

        internal void RaiseLevel(PermanentId id) => _levels[(int)id]++;

        internal void NextDay() => Day++;

        /// <summary>Atalho do debug: banco, níveis e dia do zero.</summary>
        internal void Clear()
        {
            RedShards = 0;
            Day = 1;
            Array.Clear(_levels, 0, _levels.Length);
        }
    }
}
