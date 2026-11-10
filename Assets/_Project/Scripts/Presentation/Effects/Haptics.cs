using MinersIdle.Core;
using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// Vibração (M9, E4, D-199), só com o jogo aberto: curta no golpe crítico, mais longa na quebra da parede da passagem;
    /// nada no toque comum nem na quebra de um cristal. Só escuta a simulação. Desligável nos AJUSTES (<see cref="Enabled"/>,
    /// aplicado pelo <see cref="SettingsEffects"/>).
    ///
    /// No Android chama o vibrador do aparelho com a duração pedida (<c>VibrationEffect</c> a partir do Android 8). No
    /// Editor e nos testes não vibra nada, mas conta os pulsos (<see cref="PulseCount"/>). Com tempo ×20 os críticos vêm
    /// em rajada: um pulso novo só sai depois de <see cref="minGapSeconds"/> reais.
    /// </summary>
    public sealed class Haptics : MiningViewBase
    {
        [Tooltip("Duração do pulso no golpe crítico, em milissegundos.")]
        [SerializeField, Min(1)] int criticalMilliseconds = 30;
        [Tooltip("Duração do pulso na quebra da parede da passagem, em milissegundos.")]
        [SerializeField, Min(1)] int wallMilliseconds = 120;
        [Tooltip("Intervalo mínimo, em segundos reais, entre dois pulsos (a parede sempre passa na frente de um crítico).")]
        [SerializeField, Min(0f)] float minGapSeconds = 0.12f;

        float _lastPulse = float.NegativeInfinity;

        /// <summary>A vibração dos AJUSTES. Desligada, nenhum pulso sai (nem conta).</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Quantos pulsos saíram nesta cena (para os testes; não depende de haver vibrador).</summary>
        public int PulseCount { get; private set; }

        /// <summary>A duração do último pulso, em milissegundos (0 antes do primeiro).</summary>
        public int LastPulseMilliseconds { get; private set; }

        public int CriticalMilliseconds => criticalMilliseconds;
        public int WallMilliseconds => wallMilliseconds;

        protected override void Subscribe(MiningSimulation simulation)
        {
            simulation.HitLanded += OnHit;
            simulation.CrystalBroken += OnBroken;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.HitLanded -= OnHit;
            simulation.CrystalBroken -= OnBroken;
        }

        protected override void Rebuild(MiningSimulation simulation) { }

        void OnHit(MiningHit hit)
        {
            if (hit.IsCritical) Pulse(criticalMilliseconds, priority: false);
        }

        void OnBroken(Crystal crystal)
        {
            if (crystal.IsWall) Pulse(wallMilliseconds, priority: true);
        }

        /// <param name="priority">Passa por cima do intervalo mínimo (a parede, rara e mais importante).</param>
        void Pulse(int milliseconds, bool priority)
        {
            if (!Enabled) return;
            float now = Time.unscaledTime;
            if (!priority && now - _lastPulse < minGapSeconds) return;
            _lastPulse = now;
            PulseCount++;
            LastPulseMilliseconds = milliseconds;
            Vibrator.Vibrate(milliseconds);
        }

        /// <summary>O vibrador do aparelho. Fora do Android, não faz nada.</summary>
        static class Vibrator
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            const int DefaultAmplitude = -1; // VibrationEffect.DEFAULT_AMPLITUDE
            static bool _ready;
            static AndroidJavaObject _vibrator;
            static AndroidJavaClass _effects;

            public static void Vibrate(int milliseconds)
            {
                try
                {
                    if (!_ready)
                    {
                        _ready = true;
                        var activity = UnityEngine.Android.AndroidApplication.currentActivity;
                        _vibrator = activity?.Call<AndroidJavaObject>("getSystemService", "vibrator");
                        using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                            if (version.GetStatic<int>("SDK_INT") >= 26) _effects = new AndroidJavaClass("android.os.VibrationEffect");
                    }
                    if (_vibrator == null) return;

                    if (_effects != null)
                    {
                        using (var effect = _effects.CallStatic<AndroidJavaObject>("createOneShot", (long)milliseconds, DefaultAmplitude))
                            _vibrator.Call("vibrate", effect);
                    }
                    else
                    {
                        _vibrator.Call("vibrate", (long)milliseconds);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[Vibração] Não deu para vibrar ({e.Message}); desligada nesta sessão.");
                    _vibrator = null;
                }
            }
#else
            public static void Vibrate(int milliseconds) { }
#endif

            /// <summary>
            /// Nunca chamado: a referência ao <c>Handheld.Vibrate</c> é o que faz o Unity pôr a permissão VIBRATE no
            /// manifesto do Android.
            /// </summary>
            static void KeepVibratePermission() => Handheld.Vibrate();
        }
    }
}
