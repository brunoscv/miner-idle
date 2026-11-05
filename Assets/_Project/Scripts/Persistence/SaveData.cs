using System;
using System.Collections.Generic;

namespace MinersIdle.Persistence
{
    /// <summary>
    /// O arquivo de save, versão 3 (Marco 4, D-43; a descida desde o M8; os 100 níveis do GDD v7 desde a E3b-2): só o progresso. Campos públicos porque o
    /// <c>JsonUtility</c> do Unity serializa assim. Os níveis vão pelo nome da linha (<c>"Pickaxe"</c>…), não pela
    /// posição: uma linha nova ou reordenada não estraga um save antigo.
    ///
    /// Versão 1 (M4): sem os campos da descida; abre na caverna 1, com 0 quebras nela (<see cref="SaveService"/>).
    /// Versão 2 (M8, E3): os mesmos campos da 3, com a caverna de 1 a 10. A 3 (D-129) guarda o nível de 1 a 100 no mesmo
    /// campo <c>cave</c>; um save v2 abre no mesmo nível, com o Mapa (o Letrado) que tinha. O coração da montanha do v2
    /// (a caverna 10) não vale mais: ele passou para o nível 100.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 3;

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
