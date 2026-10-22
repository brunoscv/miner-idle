using Unity.Profiling;
using UnityEngine;

namespace MinersIdle.UI
{
    /// <summary>
    /// Medição de desempenho do M5 (E5) para o teste de 10 minutos no celular: quadros lentos e quadros que alocaram
    /// memória (GC). Mora no painel de debug, que não existe no release. Mede só com o DBG fechado: o próprio painel
    /// monta textos e não conta como jogo. O contador de GC é do Profiler e só existe no Editor e em Development Build.
    /// </summary>
    public sealed class FrameStats : MonoBehaviour
    {
        /// <summary>Acima disto o quadro perdeu pelo menos uma batida dos 60 fps (16,7 ms) com folga.</summary>
        public const float SlowFrameSeconds = 0.025f;

        ProfilerRecorder _gcInFrame;
        bool _measuring = true;
        bool _skipFrame;

        /// <summary>Se está medindo agora (o <see cref="DebugPanel"/> desliga enquanto está aberto).</summary>
        public bool Measuring
        {
            get => _measuring;
            set
            {
                // O quadro em que o painel fechou ainda carrega o trabalho dele: fica de fora.
                if (value && !_measuring) _skipFrame = true;
                _measuring = value;
            }
        }

        /// <summary>Se o contador de GC existe neste build.</summary>
        public bool HasGcCounter => _gcInFrame.Valid;

        public int Frames { get; private set; }
        public float Seconds { get; private set; }
        public int SlowFrames { get; private set; }
        public float WorstFrameSeconds { get; private set; }
        public int GcFrames { get; private set; }
        public long GcBytes { get; private set; }
        public long WorstGcBytes { get; private set; }

        public float AverageFps => Seconds > 0f ? Frames / Seconds : 0f;

        void OnEnable() => _gcInFrame = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");

        void OnDisable() => _gcInFrame.Dispose();

        public void ResetCounts()
        {
            Frames = SlowFrames = GcFrames = 0;
            Seconds = WorstFrameSeconds = 0f;
            GcBytes = WorstGcBytes = 0;
        }

        void Update()
        {
            if (!_measuring) return;
            if (_skipFrame)
            {
                _skipFrame = false;
                return;
            }

            float dt = Time.unscaledDeltaTime;
            Frames++;
            Seconds += dt;
            if (dt > SlowFrameSeconds) SlowFrames++;
            if (dt > WorstFrameSeconds) WorstFrameSeconds = dt;

            if (!_gcInFrame.Valid) return;
            long bytes = _gcInFrame.LastValue; // o quadro anterior inteiro
            if (bytes <= 0) return;
            GcFrames++;
            GcBytes += bytes;
            if (bytes > WorstGcBytes) WorstGcBytes = bytes;
        }
    }
}
