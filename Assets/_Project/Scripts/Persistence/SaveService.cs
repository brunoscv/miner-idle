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
    }

    /// <summary>
    /// Carrega e salva o progresso (Marco 4, E3; D-43, D-44). Nunca deixa um save ruim travar o jogo: o arquivo é separado
    /// (<see cref="ISaveStore.SetAsideBad"/>) e começa um jogo novo, com aviso no log. M8 (E3): a versão 2 guarda a
    /// descida (caverna, quebras nela, tempo de jogo, lascas ganhas, coração da montanha); a versão 1 abre migrada, na
    /// caverna 1 com 0 quebras.
    /// </summary>
    public sealed class SaveService
    {
        static readonly UpgradeId[] Lines = (UpgradeId[])Enum.GetValues(typeof(UpgradeId));

        readonly ISaveStore _store;

        public SaveService(ISaveStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>
        /// Carrega o progresso, com os níveis cortados nos limites de <paramref name="economy"/> e a caverna nas
        /// <paramref name="caveCount"/> do balanço ativo.
        /// </summary>
        public GameState Load(EconomyRules economy, out SaveLoadStatus status, int caveCount = 1)
        {
            string text;
            try
            {
                text = _store.Read();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Não deu para ler o save ({e.Message}); começando um jogo novo.");
                status = SaveLoadStatus.Recovered;
                return new GameState();
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                status = SaveLoadStatus.NewGame;
                return new GameState();
            }

            SaveData data = null;
            try
            {
                data = JsonUtility.FromJson<SaveData>(text);
            }
            catch (Exception)
            {
                // tratado abaixo como save ruim
            }

            if (data == null || data.version < SaveData.OldestVersion || data.version > SaveData.CurrentVersion)
            {
                string why = data == null ? "ilegível" : $"de versão desconhecida ({data.version})";
                Debug.LogWarning($"[Save] Save {why}: guardado à parte e começando um jogo novo.");
                _store.SetAsideBad();
                status = SaveLoadStatus.Recovered;
                return new GameState();
            }

            var levels = new Dictionary<string, int>();
            foreach (var saved in data.levels ?? new List<SavedLevel>())
                if (saved != null && saved.upgrade != null) levels[saved.upgrade] = saved.level;

            status = SaveLoadStatus.Loaded;
            Func<UpgradeId, int> levelOf = id => levels.TryGetValue(id.ToString(), out int level) ? level : 0; // linha desconhecida: ignorada
            if (data.version == 1)
            {
                // Antes da descida: caverna 1, contagem do zero; as lascas de agora são o que se sabe das ganhas.
                Debug.Log("[Save] Save da versão 1: aberto na caverna 1 (a versão 2 grava a descida).");
                return GameState.Restore(data.shards, data.hits, data.breaks, levelOf, economy, caveCount,
                                         shardsEarned: data.shards);
            }

            return GameState.Restore(data.shards, data.hits, data.breaks, levelOf, economy, caveCount,
                                     data.cave, data.caveBreaks, data.playSeconds, data.shardsEarned, data.reachedHeart);
        }

        public void Save(GameState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            var data = new SaveData
            {
                shards = state.Shards, hits = state.Hits, breaks = state.Breaks,
                cave = state.Cave, caveBreaks = state.CaveBreaks, playSeconds = state.PlaySeconds,
                shardsEarned = state.ShardsEarned, reachedHeart = state.ReachedHeart,
            };
            foreach (var id in Lines)
                data.levels.Add(new SavedLevel { upgrade = id.ToString(), level = state.LevelOf(id) });

            _store.Write(JsonUtility.ToJson(data, prettyPrint: true));
        }

        public void Delete() => _store.Delete();
    }
}
