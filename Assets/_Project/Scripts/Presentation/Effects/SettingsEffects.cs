using MinersIdle.Game;
using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// Faz o som, o tremor e a vibração obedecerem aos AJUSTES (M9, E4): a cada mudança do <see cref="GameSettings"/> (e
    /// ao ligar), passa o volume ao <see cref="SoundBoard"/>, o tremor ao <see cref="ScreenShake"/> e a vibração ao
    /// <see cref="Haptics"/>. Assim nenhum deles precisa conhecer os ajustes nem o arquivo. Desde o M9c E3, também o avanço
    /// automático ao <see cref="MiningRunner"/>, que o passa a cada partida; desde a M9i (#23), a tela sempre acesa ao
    /// aparelho (<c>Screen.sleepTimeout</c>).
    /// </summary>
    public sealed class SettingsEffects : MonoBehaviour
    {
        [SerializeField] GameSettings settings;
        [SerializeField] SoundBoard soundBoard;
        [SerializeField] ScreenShake screenShake;
        [SerializeField] Haptics haptics;
        [SerializeField] MiningRunner runner;

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
            if (runner != null) runner.AutoAdvance = settings.AutoAdvance;
            // M9i #23 (D-344): só vale com o app na frente; em segundo plano o aparelho volta ao tempo de tela dele.
            Screen.sleepTimeout = settings.KeepScreenOn ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
        }
    }
}
