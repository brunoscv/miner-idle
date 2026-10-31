using System;
using System.Collections.Generic;

namespace MinersIdle.Persistence
{
    /// <summary>
    /// O arquivo de save, versão 2 (Marco 4, D-43; a descida desde o M8): só o progresso. Campos públicos porque o
    /// <c>JsonUtility</c> do Unity serializa assim. Os níveis vão pelo nome da linha (<c>"Pickaxe"</c>…), não pela
    /// posição: uma linha nova ou reordenada não estraga um save antigo.
    ///
    /// Versão 1 (M4): sem os campos da descida; abre na caverna 1, com 0 quebras nela (<see cref="SaveService"/>).
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 2;

        /// <summary>A versão mais antiga que ainda abre (migrada ao carregar).</summary>
        public const int OldestVersion = 1;

        public int version = CurrentVersion;
        public double shards;
        public long hits;
        public long breaks;
        public List<SavedLevel> levels = new List<SavedLevel>();

        // Versão 2 (M8, E3)
        public int cave = 1;
        public long caveBreaks;
        public double playSeconds;
        public double shardsEarned;
        public bool reachedHeart;
    }

    [Serializable]
    public sealed class SavedLevel
    {
        public string upgrade;
        public int level;
    }
}
