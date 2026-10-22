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
    /// (<see cref="ISaveStore.SetAsideBad"/>) e começa um jogo novo, com aviso no log. Backup e migração entre versões
    /// ficam para o M8.
    /// </summary>
    public sealed class SaveService
    {
        static readonly UpgradeId[] Lines = (UpgradeId[])Enum.GetValues(typeof(UpgradeId));

        readonly ISaveStore _store;

        public SaveService(ISaveStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        /// <summary>Carrega o progresso, com os níveis cortados nos limites de <paramref name="economy"/>.</summary>
        public GameState Load(EconomyRules economy, out SaveLoadStatus status)
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

            if (data == null || data.version != SaveData.CurrentVersion)
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
            return GameState.Restore(
                data.shards, data.hits, data.breaks,
                id => levels.TryGetValue(id.ToString(), out int level) ? level : 0, // linha desconhecida no save: ignorada
                economy);
        }

        public void Save(GameState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            var data = new SaveData { shards = state.Shards, hits = state.Hits, breaks = state.Breaks };
            foreach (var id in Lines)
                data.levels.Add(new SavedLevel { upgrade = id.ToString(), level = state.LevelOf(id) });

            _store.Write(JsonUtility.ToJson(data, prettyPrint: true));
        }

        public void Delete() => _store.Delete();
    }
}
