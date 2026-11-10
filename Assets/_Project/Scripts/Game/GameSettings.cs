using System;
using MinersIdle.Persistence;
using UnityEngine;

namespace MinersIdle.Game
{
    /// <summary>
    /// Os AJUSTES da sessão (M9, E4, D-198): volume dos efeitos, tremor da tela e vibração. Quem toca o som, treme a câmera
    /// e vibra escuta <see cref="Changed"/> (o <c>SettingsEffects</c>, na apresentação); a tela AJUSTES só troca os valores.
    ///
    /// Arquivo (D-200): o <c>GameBootstrap</c> entrega o <see cref="SettingsStore"/> (<see cref="UseStore"/>), à parte do
    /// save; daí em diante os ajustes abrem dele e são gravados quando mudam (o volume, ao soltar o deslizador ou ao fechar a
    /// tela, para não gravar a cada passo do arraste). Aberta direto (Play na cena Game, testes), a cena usa os de fábrica e
    /// não grava nada. Apagar save não passa por aqui.
    /// </summary>
    public sealed class GameSettings : MonoBehaviour
    {
        SettingsStore _store;
        SettingsData _data = new SettingsData();
        bool _dirty;

        /// <summary>Algum ajuste mudou (ou abriu do arquivo). Quem obedece lê os valores de novo.</summary>
        public event Action Changed;

        /// <summary>Se estes ajustes vão para o arquivo (só quando vieram pelo <c>GameBootstrap</c>).</summary>
        public bool HasStore => _store != null;

        /// <summary>Volume dos efeitos, de 0 a 1 (fábrica: 0,8, o volume de antes dos AJUSTES).</summary>
        public float EffectsVolume
        {
            get => _data.effectsVolume;
            set
            {
                float volume = float.IsNaN(value) ? SettingsData.DefaultEffectsVolume : Mathf.Clamp01(value);
                if (Mathf.Approximately(volume, _data.effectsVolume)) return;
                _data.effectsVolume = volume;
                _dirty = true; // grava no Flush: o deslizador muda a cada passo do arraste
                Notify();
            }
        }

        public bool ScreenShake
        {
            get => _data.screenShake;
            set
            {
                if (value == _data.screenShake) return;
                _data.screenShake = value;
                _dirty = true;
                Flush();
                Notify();
            }
        }

        public bool Vibration
        {
            get => _data.vibration;
            set
            {
                if (value == _data.vibration) return;
                _data.vibration = value;
                _dirty = true;
                Flush();
                Notify();
            }
        }

        /// <summary>
        /// Liga o arquivo dos ajustes e abre com o que está nele (chamado pelo <c>GameBootstrap</c>). Sem arquivo, ou com um
        /// ilegível, ficam os de fábrica.
        /// </summary>
        public void UseStore(SettingsStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            var loaded = store.Load();
            _data = loaded ?? new SettingsData();
            _data.effectsVolume = float.IsNaN(_data.effectsVolume) ? SettingsData.DefaultEffectsVolume : Mathf.Clamp01(_data.effectsVolume);
            _dirty = false;
            Debug.Log($"[Ajustes] Efeitos {Mathf.RoundToInt(_data.effectsVolume * 100f)}%, tremor {(_data.screenShake ? "ligado" : "desligado")}, " +
                      $"vibração {(_data.vibration ? "ligada" : "desligada")}" + (loaded == null ? " (de fábrica)." : "."));
            Notify();
        }

        /// <summary>Grava os ajustes, se mudaram (sem arquivo ligado, não faz nada). Um erro de disco vai para o log.</summary>
        public void Flush()
        {
            if (!_dirty || _store == null) return;
            try
            {
                _data.version = SettingsData.CurrentVersion;
                _store.Save(_data);
                _dirty = false;
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Flush();
        }

        void OnApplicationQuit() => Flush();

        void OnDisable() => Flush();

        void Notify()
        {
            try { Changed?.Invoke(); }
            catch (Exception e) { Debug.LogException(e, this); }
        }
    }
}
