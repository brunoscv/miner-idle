using MinersIdle.Game;
using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// Faz o som, o tremor e a vibração obedecerem aos AJUSTES (M9, E4): a cada mudança do <see cref="GameSettings"/> (e
    /// ao ligar), passa o volume ao <see cref="SoundBoard"/>, o tremor ao <see cref="ScreenShake"/> e a vibração ao
    /// <see cref="Haptics"/>. Assim nenhum deles precisa conhecer os ajustes nem o arquivo.
    /// </summary>
    public sealed class SettingsEffects : MonoBehaviour
    {
        [SerializeField] GameSettings settings;
        [SerializeField] SoundBoard soundBoard;
        [SerializeField] ScreenShake screenShake;
        [SerializeField] Haptics haptics;

        void OnEnable()
        {
            if (settings == null)
            {
                Debug.LogError("[SettingsEffects] Sem GameSettings.", this);
                return;
            }
            settings.Changed += Apply;
            Apply();
        }

        void OnDisable()
        {
            if (settings != null) settings.Changed -= Apply;
        }

        void Apply()
        {
            if (soundBoard != null) soundBoard.Volume = settings.EffectsVolume;
            if (screenShake != null) screenShake.ShakeEnabled = settings.ScreenShake;
            if (haptics != null) haptics.Enabled = settings.Vibration;
        }
    }
}
