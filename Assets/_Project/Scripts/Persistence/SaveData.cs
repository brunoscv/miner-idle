using System;
using System.Collections.Generic;

namespace MinersIdle.Persistence
{
    /// <summary>
    /// O arquivo de save, versão 8 (o andar mais fundo e a migração para o GDD v8 desde a E1 do M9c, D-263; a janela do cristal raro desde a E4 do M9b, D-222; os bônus com tempo desde a E1 do M9b, D-181; a hora da gravação desde a E1 do M9, para o offline; Marco 4, D-43; a descida desde o M8; os 100 níveis do GDD v7 desde a E3b-2; o dia e
    /// as melhorias permanentes desde a E2 do Marco 8b): duas partes, a <b>run</b> (os campos soltos, como até a versão 3)
    /// e a <b>permanente</b> (<see cref="permanent"/>), que fica de um dia para o outro. Campos públicos porque o
    /// <c>JsonUtility</c> do Unity serializa assim. Os níveis vão pelo nome da linha (<c>"Pickaxe"</c>…), não pela
    /// posição: uma linha nova ou reordenada não estraga um save antigo.
    ///
    /// Versão 1 (M4): sem os campos da descida; abre na caverna 1, com 0 quebras nela (<see cref="SaveService"/>).
    /// Versão 2 (M8, E3): os mesmos campos da 3, com a caverna de 1 a 10. A 3 (D-129) guarda o nível de 1 a 100 no mesmo
    /// campo <c>cave</c>; um save v2 abre no mesmo nível, com o Mapa (o Letrado) que tinha. O coração da montanha do v2
    /// (a caverna 10) não vale mais: ele passou para o nível 100. Até a 3 não há a parte permanente: abre com as
    /// permanentes zeradas, no dia 1, e a run como estava. Até a 5 não há bônus com tempo: abre sem nenhum. Até a 6 não há
    /// janela do raro: abre fechada.
    ///
    /// Versão 8 (M9c E1, GDD v8): os números do jogo mudaram (a vida cresce 1,5× por andar), então um save até a 7 abre
    /// numa <b>run nova no andar 1</b> (D-263): ficam as lascas vermelhas, as permanentes e o dia, e o andar a que a run
    /// antiga chegou vira o andar mais fundo (<see cref="SavedPermanent.deepestFloor"/>). A run (lascas, melhorias, bônus,
    /// janela do raro, volta do offline pendente) recomeça.
    ///
    /// Versão 10 (M9e E1, D-283): o contador do raro (<see cref="rareTimer"/>). Até a 9 abre no zero. O Raro Dourado
    /// (D-286) vai com as outras permanentes, pelo nome: um save antigo abre sem ele.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 10;

        /// <summary>A primeira versão com os números do GDD v8 (M9c E1): as de antes abrem numa run nova (D-263).</summary>
        public const int FirstV8Version = 8;

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

        // Versão 7 (M9b, E4): a janela do cristal raro esperando o jogador (D-222). Um save até a 6 abre sem janela.
        public SavedRareWindow rareWindow = new SavedRareWindow();

        /// <summary>
        /// Save v9 (M9d, D-275): o jogador fechou o jogo descansando, antes do INICIAR. Abre de novo na tela do descanso,
        /// com o resumo do dia, sem offline. Até a 8 não há: abre numa run em andamento, como antes.
        /// </summary>
        public SavedRest rest = new SavedRest();

        /// <summary>Save v10 (M9e E1, D-283): os segundos de run desde o último raro. Até a 9: 0 (o contador recomeça).</summary>
        public double rareTimer;
    }

    /// <summary>
    /// A janela do cristal raro (M9b E4): as cartas pelo nome (<c>"Damage"</c>…), o tempo que falta para escolher, a carta
    /// que saiu no sorteio e a fila. Carta desconhecida: a janela fecha.
    /// </summary>
    [Serializable]
    public sealed class SavedRareWindow
    {
        public bool open;
        public string first;
        public string second;
        public double secondsLeft;
        public int chosen = -1;
        public bool autoPicked;
        public int queued;
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
        /// <summary>O andar em que o jogador deixou o jogo (M9c E4; 0 nos saves de antes = sem descida).</summary>
        public int fromLevel;
        /// <summary>M9e E3: o teto da volta, as horas extras dos anúncios e os raros (0 nos saves de antes).</summary>
        public double maxSeconds;
        public double extraSeconds;
        public int rares;
    }

    /// <summary>O descanso pendente (save v9, M9d): o resumo do dia que estava na tela.</summary>
    [Serializable]
    public sealed class SavedRest
    {
        public bool resting;
        public int day;
        public int level;
        public double playSeconds;
        public double shardsEarned;
        public double redShardsGained;
        public double redShardsBank;
    }

    /// <summary>A parte permanente do save (Marco 8b): banco de lascas vermelhas, dia e níveis das permanentes, pelo nome.</summary>
    [Serializable]
    public sealed class SavedPermanent
    {
        public double redShards;
        public int day = 1;
        public List<SavedLevel> levels = new List<SavedLevel>();

        /// <summary>
        /// Versão 8 (M9c E1): o andar mais fundo já alcançado, em qualquer run (o offline de 6 h a partir do andar 20,
        /// D-253). Num save até a 7, o andar da run antiga (D-263).
        /// </summary>
        public int deepestFloor = 1;
    }

    [Serializable]
    public sealed class SavedLevel
    {
        /// <summary>O nome da linha: um <c>UpgradeId</c> na run, um <c>PermanentId</c> na parte permanente.</summary>
        public string upgrade;
        public int level;
    }
}
