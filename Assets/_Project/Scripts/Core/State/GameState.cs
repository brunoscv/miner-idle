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

        /// <summary>A caverna em que o anão está (M8; 1 = a primeira).</summary>
        public int Cave { get; private set; } = 1;

        /// <summary>Cristais quebrados na caverna atual (M8): a trava do Letrado (D-93); toda quebra conta (D-95).</summary>
        public long CaveBreaks { get; private set; }

        /// <summary>Segundos de jogo simulados desde o começo (M8, E3): a tela do coração da montanha mostra (D-112).</summary>
        public double PlaySeconds { get; private set; }

        /// <summary>Lascas ganhas nas quebras desde o começo (M8, E3; D-112). As do debug e as gastas não mudam a conta.</summary>
        public double ShardsEarned { get; private set; }

        /// <summary>A última parede caiu: o anão chegou ao coração da montanha (M8, D-112). O jogo segue na última caverna.</summary>
        public bool ReachedHeart { get; private set; }

        /// <summary>Nível da melhoria = compras feitas (D-38).</summary>
        public int LevelOf(UpgradeId id) => _levels[(int)id];

        /// <summary>
        /// Recria o estado a partir de um save (Marco 4; a descida desde o M8). Corrige o que estiver fora do lugar em vez
        /// de falhar: lascas negativas ou inválidas viram 0, contadores negativos viram 0, níveis ficam entre 0 e o limite
        /// da linha em <paramref name="economy"/> (um asset novo pode ter limite menor que o do save), a caverna fica entre
        /// 1 e <paramref name="caveCount"/>.
        ///
        /// O Letrado acompanha a caverna (M8, E3): o nível <i>n</i> leva à caverna <i>n+1</i>, então na caverna <i>c</i>
        /// ele está em <i>c−1</i> (ainda não comprado) ou <i>c</i> (comprado, a parede ainda por quebrar; uma parede de pé
        /// não é salva, e o botão de passar volta). Um nível fora disso vai para o mais perto. O coração da montanha só vale
        /// na última caverna, com o último Letrado.
        /// </summary>
        public static GameState Restore(double shards, long hits, long breaks,
                                        Func<UpgradeId, int> levelOf, EconomyRules economy,
                                        int caveCount = 1, int cave = 1, long caveBreaks = 0, double playSeconds = 0,
                                        double shardsEarned = 0, bool reachedHeart = false)
        {
            if (levelOf == null) throw new ArgumentNullException(nameof(levelOf));
            if (economy == null) throw new ArgumentNullException(nameof(economy));

            var state = new GameState
            {
                Shards = NonNegative(shards),
                Hits = Math.Max(0, hits),
                Breaks = Math.Max(0, breaks),
                Cave = Math.Min(Math.Max(1, cave), Math.Max(1, caveCount)),
                CaveBreaks = Math.Max(0, caveBreaks),
                PlaySeconds = NonNegative(playSeconds),
                ShardsEarned = NonNegative(shardsEarned),
            };
            foreach (UpgradeId id in Enum.GetValues(typeof(UpgradeId)))
                state._levels[(int)id] = Math.Min(Math.Max(0, levelOf(id)), economy.Get(id).MaxLevel);

            int letrado = (int)UpgradeId.Letrado;
            int letradoMax = economy.Get(UpgradeId.Letrado).MaxLevel;
            state._levels[letrado] = Math.Max(Math.Min(state._levels[letrado], state.Cave), Math.Min(state.Cave - 1, letradoMax));
            state.ReachedHeart = reachedHeart && state.Cave == caveCount && state._levels[letrado] >= state.Cave;
            return state;
        }

        static double NonNegative(double value) => double.IsNaN(value) || double.IsInfinity(value) || value < 0 ? 0 : value;

        internal void RegisterHit() => Hits++;

        internal void AddShards(double amount) => Shards += amount;

        /// <summary>Gasta lascas; nunca deixa o saldo negativo (a folga de arredondamento da compra não vira dívida).</summary>
        internal void SpendShards(double amount) => Shards = Math.Max(0, Shards - amount);

        internal void RaiseLevel(UpgradeId id) => _levels[(int)id]++;

        internal void RegisterBreak(double reward)
        {
            Breaks++;
            CaveBreaks++;
            Shards += reward;
            ShardsEarned += reward;
        }

        internal void AddPlayTime(double seconds) => PlaySeconds += seconds;

        internal void ReachHeart() => ReachedHeart = true;

        /// <summary>Entra na caverna: a contagem da trava do Letrado recomeça.</summary>
        internal void EnterCave(int cave)
        {
            Cave = cave;
            CaveBreaks = 0;
        }

        /// <summary>Atalho do debug: quebras da caverna sem quebrar cristal.</summary>
        internal void SetCaveBreaks(long breaks) => CaveBreaks = Math.Max(0, breaks);
    }
}
