using TMPro;
using UnityEngine;

namespace MinersIdle.UI
{
    /// <summary>
    /// Rótulo de diagnóstico: nome, versão do build, resolução, Safe Area e FPS. Criado no Marco 1; desde o Marco 2 (E5)
    /// fica dentro do painel de debug, que não existe no release. Desde o M5 (E5) mostra também a medição do
    /// <see cref="FrameStats"/> (quadros lentos e GC), sem alocar: o cabeçalho só é remontado quando a tela muda, e os
    /// números entram pelo <c>SetText</c> do TextMeshPro.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class BuildInfoLabel : MonoBehaviour
    {
        [SerializeField] FrameStats frameStats;
        [Tooltip("Formato do TextMeshPro ({n:casas}): {0} fps agora · {1} minutos medidos · {2} fps médios · " +
                 "{3} quadros lentos · {4} pior quadro (ms) · {5} quadros com GC · {6} bytes de GC · {7} maior GC num quadro. " +
                 "Sem contador de GC no build, 5 a 7 saem -1.")]
        [SerializeField, TextArea(3, 6)] string statsFormat;

        TMP_Text _text;
        float _accum;
        int _frames;
        float _fps;
        string _format;
        Rect _safe;
        Vector2Int _screen;

        void Awake() => _text = GetComponent<TMP_Text>();

        void Update()
        {
            _accum += Time.unscaledDeltaTime;
            _frames++;
            if (_accum < 0.5f) return;

            _fps = _frames / _accum;
            _accum = 0f;
            _frames = 0;

            if (_format == null || Screen.safeArea != _safe || Screen.width != _screen.x || Screen.height != _screen.y)
                BuildFormat();

            if (frameStats == null)
            {
                _text.SetText(_format, _fps, 0, 0, 0, 0, 0, 0, 0);
                return;
            }

            bool gc = frameStats.HasGcCounter;
            _text.SetText(_format, _fps, frameStats.Seconds / 60f, frameStats.AverageFps, frameStats.SlowFrames,
                          frameStats.WorstFrameSeconds * 1000f,
                          gc ? frameStats.GcFrames : -1, gc ? frameStats.GcBytes : -1, gc ? frameStats.WorstGcBytes : -1);
        }

        /// <summary>Cabeçalho fixo + formato dos números. Aloca, mas só ao abrir e quando a tela muda.</summary>
        void BuildFormat()
        {
            _safe = Screen.safeArea;
            _screen = new Vector2Int(Screen.width, Screen.height);
            string header =
                $"Miners Idle\nv{Application.version}\n{Screen.width}x{Screen.height}\n" +
                $"safe {_safe.x:0},{_safe.y:0} {_safe.width:0}x{_safe.height:0}\n";
            _format = header + (string.IsNullOrEmpty(statsFormat) ? "{0:0} fps" : statsFormat);
        }
    }
}
