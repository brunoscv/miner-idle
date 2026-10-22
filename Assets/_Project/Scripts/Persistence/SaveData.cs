using System;
using System.Collections.Generic;

namespace MinersIdle.Persistence
{
    /// <summary>
    /// O arquivo de save, versão 1 (Marco 4, D-43): só o progresso. Campos públicos porque o <c>JsonUtility</c> do Unity
    /// serializa assim. Os níveis vão pelo nome da linha (<c>"Pickaxe"</c>…), não pela posição: uma linha nova ou
    /// reordenada não estraga um save antigo.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public double shards;
        public long hits;
        public long breaks;
        public List<SavedLevel> levels = new List<SavedLevel>();
    }

    [Serializable]
    public sealed class SavedLevel
    {
        public string upgrade;
        public int level;
    }
}
