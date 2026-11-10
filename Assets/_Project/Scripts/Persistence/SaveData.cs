using System;
using System.Collections.Generic;

namespace MinersIdle.Persistence
{
    /// <summary>
    /// O arquivo de save, versão 6 (os bônus com tempo desde a E1 do M9b, D-181; a hora da gravação desde a E1 do M9, para o offline; Marco 4, D-43; a descida desde o M8; os 100 níveis do GDD v7 desde a E3b-2; o dia e
    /// as melhorias permanentes desde a E2 do Marco 8b): duas partes, a <b>run</b> (os campos soltos, como até a versão 3)
    /// e a <b>permanente</b> (<see cref="permanent"/>), que fica de um dia para o outro. Campos públicos porque o
    /// <c>JsonUtility</c> do Unity serializa assim. Os níveis vão pelo nome da linha (<c>"Pickaxe"</c>…), não pela
    /// posição: uma linha nova ou reordenada não estraga um save antigo.
    ///
    /// Versão 1 (M4): sem os campos da descida; abre na caverna 1, com 0 quebras nela (<see cref="SaveService"/>).
    /// Versão 2 (M8, E3): os mesmos campos da 3, com a caverna de 1 a 10. A 3 (D-129) guarda o nível de 1 a 100 no mesmo
    /// campo <c>cave</c>; um save v2 abre no mesmo nível, com o Mapa (o Letrado) que tinha. O coração da montanha do v2
    /// (a caverna 10) não vale mais: ele passou para o nível 100. Até a 3 não há a parte permanente: abre com as
    /// permanentes zeradas, no dia 1, e a run como estava. Até a 5 não há bônus com tempo: abre sem nenhum.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 6;

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

        // Versão 5 (M9, E1): a hora em que foi gravado, para o offline. Em ticks UTC; 0 = sem hora (um save até a 4).
        public long savedAtUtcTicks;

        // Versão 5 (M9, E2): a volta do offline que ainda espera o COLETAR (D-197). Um save sem ela abre sem volta.
        public SavedOffline pendingOffline = new SavedOffline();

        // Versão 6 (M9b, E1): os bônus com tempo ativos e quanto falta de cada um (D-181). Um save até a 5 abre sem bônus.
        public List<SavedBonus> bonuses = new List<SavedBonus>();
    }

    /// <summary>
    /// Um bônus com tempo ativo (M9b). O tipo e a origem vão pelo nome (<c>"DamagePercent"</c>, <c>"Combo"</c>), como os
    /// níveis: um tipo novo não estraga o save, e um tipo que sumiu é ignorado.
    /// </summary>
    [Serializable]
    public sealed class SavedBonus
    {
        public string kind;
        public string source;
        public int tier;
        public double value;
        public double duration;
        public double secondsLeft;
    }

    /// <summary>A volta do offline esperando o COLETAR (M9, E2): fechar no meio do modal não paga duas vezes.</summary>
    [Serializable]
    public sealed class SavedOffline
    {
        public double secondsAway;
        public double secondsCounted;
        public long crystals;
        public double shards;
        public int level;
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
