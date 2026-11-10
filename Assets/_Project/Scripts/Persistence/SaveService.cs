using System;
using System.Collections.Generic;
using MinersIdle.Core;
using UnityEngine;

namespace MinersIdle.Persistence
{
    public enum SaveLoadStatus
    {
        /// <summary>Não havia save: jogo novo.</summary>
        NewGame,
        Loaded,
        /// <summary>O save era ilegível ou de versão desconhecida: foi separado e começou um jogo novo.</summary>
        Recovered,
        /// <summary>
        /// O save estava estragado ou faltando e o backup estava bom (M9, E3): abriu o backup, o progresso de uma gravação
        /// antes. O estragado foi separado (<c>.bad</c>).
        /// </summary>
        LoadedBackup,
    }

    /// <summary>
    /// Carrega e salva o progresso (Marco 4, E3; D-43, D-44). Nunca deixa um save ruim travar o jogo: o arquivo é separado
    /// (<see cref="ISaveStore.SetAsideBad"/>) e começa um jogo novo, com aviso no log. M8 (E3): a versão 2 guarda a
    /// descida (caverna, quebras nela, tempo de jogo, lascas ganhas, coração da montanha); a versão 1 abre migrada, na
    /// caverna 1 com 0 quebras. Marco 8b (E2): a versão 4 guarda também a parte permanente (banco de lascas vermelhas,
    /// dia e níveis das permanentes); até a 3, ela abre zerada. Um arquivo só: Apagar save apaga as duas partes.
    /// M9 (E3): com o save principal estragado, faltando ou de versão futura, abre o backup (o save de uma gravação antes,
    /// <see cref="ISaveStore.ReadBackup"/>); só com os dois ruins começa um jogo novo.
    /// </summary>
    public sealed class SaveService
    {
        static readonly UpgradeId[] Lines = (UpgradeId[])Enum.GetValues(typeof(UpgradeId));
        static readonly PermanentId[] PermanentLines = (PermanentId[])Enum.GetValues(typeof(PermanentId));

        readonly ISaveStore _store;

        public SaveService(ISaveStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>
        /// A hora (UTC) em que o save carregado foi gravado (save v5, M9): de onde o offline conta. <c>null</c> sem save,
        /// com um save ruim ou até a versão 4 (sem hora: nenhum offline na primeira abertura).
        /// </summary>
        public DateTime? LoadedSavedAtUtc { get; private set; }

        /// <summary>
        /// Carrega o progresso, com os níveis cortados nos limites de <paramref name="economy"/> e a caverna nas
        /// <paramref name="caveCount"/> do balanço ativo.
        /// </summary>
        public GameState Load(EconomyRules economy, out SaveLoadStatus status, int caveCount = 1) =>
            Load(economy, PermanentRules.None, out _, out status, caveCount);

        /// <summary>
        /// Carrega as duas partes (Marco 8b): a run, como no outro <c>Load</c>, e a parte permanente, com os níveis cortados
        /// nos limites de <paramref name="permanentRules"/> (<see cref="PermanentState.Restore"/>). Sem save, ou com um save
        /// ruim, as duas começam do zero.
        /// </summary>
        public GameState Load(EconomyRules economy, PermanentRules permanentRules, out PermanentState permanent,
                              out SaveLoadStatus status, int caveCount = 1)
        {
            if (permanentRules == null) throw new ArgumentNullException(nameof(permanentRules));
            permanent = new PermanentState();
            LoadedSavedAtUtc = null;

            // O principal; estragado ou faltando, o backup (M9, E3); os dois ruins, jogo novo.
            string text = ReadSafely(_store.Read, "o save", out bool readFailed);
            if (TryParse(text, out var data, out string why))
            {
                status = SaveLoadStatus.Loaded;
                return Restore(data, economy, permanentRules, caveCount, out permanent);
            }

            bool hadSave = readFailed || !string.IsNullOrWhiteSpace(text);
            if (hadSave && !readFailed)
            {
                Debug.LogWarning($"[Save] Save {why}: guardado à parte.");
                _store.SetAsideBad();
            }

            string backup = ReadSafely(_store.ReadBackup, "o backup", out bool backupFailed);
            if (TryParse(backup, out data, out string backupWhy))
            {
                Debug.LogWarning("[Save] Abrindo o backup (o save de uma gravação antes).");
                status = SaveLoadStatus.LoadedBackup;
                return Restore(data, economy, permanentRules, caveCount, out permanent);
            }

            bool hadBackup = backupFailed || !string.IsNullOrWhiteSpace(backup);
            if (!hadSave && !hadBackup)
            {
                status = SaveLoadStatus.NewGame;
                return new GameState();
            }

            Debug.LogWarning(hadBackup
                ? $"[Save] O backup também não abre ({backupWhy}); começando um jogo novo."
                : "[Save] Sem backup; começando um jogo novo.");
            status = SaveLoadStatus.Recovered;
            return new GameState();
        }

        /// <summary>Lê um dos arquivos; um erro de leitura vira aviso no log e <c>null</c>.</summary>
        static string ReadSafely(Func<string> read, string what, out bool failed)
        {
            failed = false;
            try
            {
                return read();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Não deu para ler {what} ({e.Message}).");
                failed = true;
                return null;
            }
        }

        /// <summary>O texto vira um save que esta versão abre; senão, <paramref name="why"/> diz o motivo.</summary>
        static bool TryParse(string text, out SaveData data, out string why)
        {
            data = null;
            why = "vazio";
            if (string.IsNullOrWhiteSpace(text)) return false;
            try
            {
                data = JsonUtility.FromJson<SaveData>(text);
            }
            catch (Exception)
            {
                data = null;
            }

            if (data == null)
            {
                why = "ilegível";
                return false;
            }
            if (data.version < SaveData.OldestVersion || data.version > SaveData.CurrentVersion)
            {
                why = $"de versão desconhecida ({data.version})";
                data = null;
                return false;
            }
            return true;
        }

        /// <summary>O estado da run e a parte permanente a partir de um save que abre.</summary>
        GameState Restore(SaveData data, EconomyRules economy, PermanentRules permanentRules, int caveCount,
                          out PermanentState permanent)
        {
            var levels = new Dictionary<string, int>();
            foreach (var saved in data.levels ?? new List<SavedLevel>())
                if (saved != null && saved.upgrade != null) levels[saved.upgrade] = saved.level;

            permanent = RestorePermanent(data, permanentRules);
            if (data.version >= 5 && data.savedAtUtcTicks > DateTime.MinValue.Ticks && data.savedAtUtcTicks <= DateTime.MaxValue.Ticks)
                LoadedSavedAtUtc = new DateTime(data.savedAtUtcTicks, DateTimeKind.Utc);
            Func<UpgradeId, int> levelOf = id => levels.TryGetValue(id.ToString(), out int level) ? level : 0; // linha desconhecida: ignorada
            if (data.version == 1)
            {
                // Antes da descida: caverna 1, contagem do zero; as lascas de agora são o que se sabe das ganhas.
                Debug.Log("[Save] Save da versão 1: aberto na caverna 1 (a versão 2 grava a descida).");
                return GameState.Restore(data.shards, data.hits, data.breaks, levelOf, economy, caveCount,
                                         shardsEarned: data.shards);
            }

            if (data.version == 2)
            {
                // As 10 cavernas viram os 10 primeiros níveis (D-129); o coração ficou no nível 100.
                Debug.Log($"[Save] Save da versão 2: aberto no nível {data.cave} (a versão 3 vai até o nível 100).");
                // O Restore só mantém o coração na última caverna: o da caverna 10 do v2 cai com 100 níveis.
                return GameState.Restore(data.shards, data.hits, data.breaks, levelOf, economy, caveCount,
                                         data.cave, data.caveBreaks, data.playSeconds, data.shardsEarned, data.reachedHeart);
            }

            var pending = data.version >= 5 && data.pendingOffline != null
                ? new OfflineReturn(data.pendingOffline.secondsAway, data.pendingOffline.secondsCounted,
                                    data.pendingOffline.crystals, data.pendingOffline.shards, data.pendingOffline.level)
                : default;
            return GameState.Restore(data.shards, data.hits, data.breaks, levelOf, economy, caveCount,
                                     data.cave, data.caveBreaks, data.playSeconds, data.shardsEarned, data.reachedHeart,
                                     pending, data.version >= 6 ? RestoreBonuses(data.bonuses) : null);
        }

        /// <summary>Os bônus do save v6 (M9b). Tipo ou origem desconhecidos: o bônus fica de fora; os valores, o <see cref="GameState"/> confere.</summary>
        static List<ActiveBonus> RestoreBonuses(List<SavedBonus> saved)
        {
            var bonuses = new List<ActiveBonus>();
            if (saved == null) return bonuses;
            foreach (var bonus in saved)
            {
                if (bonus == null || !Enum.TryParse(bonus.kind, out BonusKind kind) || !Enum.IsDefined(typeof(BonusKind), kind)
                    || !Enum.TryParse(bonus.source, out BonusSource source) || !Enum.IsDefined(typeof(BonusSource), source))
                    continue;
                bonuses.Add(new ActiveBonus(kind, bonus.value, bonus.duration, bonus.secondsLeft, source, bonus.tier));
            }
            return bonuses;
        }

        /// <summary>
        /// A parte permanente do save. Até a versão 3 não existe: zerada, no dia 1. Valores fora do lugar são corrigidos
        /// pelo <see cref="PermanentState.Restore"/>, sem mexer na run.
        /// </summary>
        static PermanentState RestorePermanent(SaveData data, PermanentRules rules)
        {
            if (data.version < 4 || data.permanent == null) return new PermanentState();

            var levels = new Dictionary<string, int>();
            foreach (var saved in data.permanent.levels ?? new List<SavedLevel>())
                if (saved != null && saved.upgrade != null) levels[saved.upgrade] = saved.level;
            return PermanentState.Restore(data.permanent.redShards, data.permanent.day,
                                          id => levels.TryGetValue(id.ToString(), out int level) ? level : 0, rules);
        }

        /// <param name="permanent">
        /// A parte permanente (Marco 8b). Sem ela, grava zerada: quem joga sempre passa a dele (o <c>MiningRunner</c>).
        /// </param>
        /// <param name="savedAtUtc">A hora da gravação (save v5, M9), do relógio do jogo; sem ela, o save vai sem hora.</param>
        public void Save(GameState state, PermanentState permanent = null, DateTime? savedAtUtc = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            var data = new SaveData
            {
                savedAtUtcTicks = savedAtUtc?.ToUniversalTime().Ticks ?? 0,
                shards = state.Shards, hits = state.Hits, breaks = state.Breaks,
                cave = state.Cave, caveBreaks = state.CaveBreaks, playSeconds = state.PlaySeconds,
                shardsEarned = state.ShardsEarned, reachedHeart = state.ReachedHeart,
                pendingOffline = new SavedOffline
                {
                    secondsAway = state.PendingOffline.SecondsAway, secondsCounted = state.PendingOffline.SecondsCounted,
                    crystals = state.PendingOffline.Crystals, shards = state.PendingOffline.Shards,
                    level = state.PendingOffline.Level,
                },
            };
            foreach (var id in Lines)
                data.levels.Add(new SavedLevel { upgrade = id.ToString(), level = state.LevelOf(id) });
            foreach (var bonus in state.Bonuses.All)
                data.bonuses.Add(new SavedBonus
                {
                    kind = bonus.Kind.ToString(), source = bonus.Source.ToString(), tier = bonus.Tier,
                    value = bonus.Value, duration = bonus.Duration, secondsLeft = bonus.SecondsLeft,
                });
            if (permanent != null)
            {
                data.permanent.redShards = permanent.RedShards;
                data.permanent.day = permanent.Day;
                foreach (var id in PermanentLines)
                    data.permanent.levels.Add(new SavedLevel { upgrade = id.ToString(), level = permanent.LevelOf(id) });
            }

            _store.Write(JsonUtility.ToJson(data, prettyPrint: true));
        }

        public void Delete() => _store.Delete();
    }
}
