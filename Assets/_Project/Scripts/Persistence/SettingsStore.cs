using System;
using UnityEngine;

namespace MinersIdle.Persistence
{
    /// <summary>
    /// Os AJUSTES como ficam no arquivo (M9, E4, D-198): o volume dos efeitos (0 a 1), o tremor da tela e a vibração.
    /// Campos públicos para o <c>JsonUtility</c>; os valores de fábrica são os do jogo antes dos AJUSTES (volume 80%, tremor
    /// e vibração ligados), então um arquivo sem um campo abre como o jogo sempre foi.
    /// </summary>
    [Serializable]
    public sealed class SettingsData
    {
        public const int CurrentVersion = 1;
        public const float DefaultEffectsVolume = 0.8f;

        public int version = CurrentVersion;
        public float effectsVolume = DefaultEffectsVolume;
        public bool screenShake = true;
        public bool vibration = true;
    }

    /// <summary>
    /// Guarda os AJUSTES no aparelho, num arquivo à parte do save (<c>settings.json</c>, D-200): Apagar save não mexe
    /// neles. Um arquivo ilegível vale como "nenhum ajuste" (o jogo abre com os de fábrica), sem travar.
    /// </summary>
    public sealed class SettingsStore
    {
        public const string FileName = "settings.json";

        readonly ISaveStore _store;

        public SettingsStore(ISaveStore store) => _store = store ?? throw new ArgumentNullException(nameof(store));

        /// <summary>Os ajustes gravados, ou <c>null</c> se não houver (ou se o arquivo não se lê).</summary>
        public SettingsData Load()
        {
            string text;
            try
            {
                text = _store.Read();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Ajustes] Não deu para ler os ajustes: {e.Message}. Abre com os de fábrica.");
                return null;
            }
            if (string.IsNullOrWhiteSpace(text)) return null;

            try
            {
                return JsonUtility.FromJson<SettingsData>(text);
            }
            catch (ArgumentException e)
            {
                Debug.LogWarning($"[Ajustes] Ajustes ilegíveis ({e.Message}). Abre com os de fábrica.");
                return null;
            }
        }

        public void Save(SettingsData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            _store.Write(JsonUtility.ToJson(data, prettyPrint: true));
        }
    }
}
