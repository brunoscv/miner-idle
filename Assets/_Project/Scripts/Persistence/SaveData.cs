using System;
using System.Collections.Generic;

namespace MinersIdle.Persistence
{
    /// <summary>
    /// O arquivo de save, versão 4 (Marco 4, D-43; a descida desde o M8; os 100 níveis do GDD v7 desde a E3b-2; o dia e
    /// as melhorias permanentes desde a E2 do Marco 8b): duas partes, a <b>run</b> (os campos soltos, como até a versão 3)
    /// e a <b>permanente</b> (<see cref="permanent"/>), que fica de um dia para o outro. Campos públicos porque o
    /// <c>JsonUtility</c> do Unity serializa assim. Os níveis vão pelo nome da linha (<c>"Pickaxe"</c>…), não pela
    /// posição: uma linha nova ou reordenada não estraga um save antigo.
    ///
    /// Versão 1 (M4): sem os campos da descida; abre na caverna 1, com 0 quebras nela (<see cref="SaveService"/>).
    /// Versão 2 (M8, E3): os mesmos campos da 3, com a caverna de 1 a 10. A 3 (D-129) guarda o nível de 1 a 100 no mesmo
    /// campo <c>cave</c>; um save v2 abre no mesmo nível, com o Mapa (o Letrado) que tinha. O coração da montanha do v2
    /// (a caverna 10) não vale mais: ele passou para o nível 100. Até a 3 não há a parte permanente: abre com as
    /// permanentes zeradas, no dia 1, e a run como estava.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 4;

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

        // Versão 4 (Marco 8b, E2): o que fica de um dia para o outro.
        public SavedPermanent permanent = new SavedPermanent();
    }

    /// <summary>A parte permanente do save (Marco 8b): banco de lascas vermelhas, dia e níveis das permanentes, pelo nome.</summary>
    [Serializable]
    public sealed class SavedPermanent
    {
        public double redShards;
        public int day = 1;
        public List<SavedLevel> levels = new List<SavedLevel>();
    }

    [Serializable]
    public sealed class SavedLevel
    {
        /// <summary>O nome da linha: um <c>UpgradeId</c> na run, um <c>PermanentId</c> na parte permanente.</summary>
        public string upgrade;
        public int level;
    }
}
