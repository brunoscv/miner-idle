using TMPro;
using UnityEngine;

namespace MinersIdle.UI
{
    /// <summary>
    /// Rótulo de diagnóstico: nome, versão do build, resolução, Safe Area e FPS. Criado no Marco 1; desde o Marco 2 (E5)
    /// fica dentro do painel de debug, que não existe no release.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class BuildInfoLabel : MonoBehaviour
    {
        TMP_Text _text;
        float _accum;
        int _frames;
        float _fps;

        void Awake() => _text = GetComponent<TMP_Text>();

        void Update()
        {
            _accum += Time.unscaledDeltaTime;
            _frames++;
            if (_accum < 0.5f) return;

            _fps = _frames / _accum;
            _accum = 0f;
            _frames = 0;
            var safe = Screen.safeArea;
            _text.text =
                $"Miners Idle\nv{Application.version}\n{Screen.width}x{Screen.height}\n" +
                $"safe {safe.x:0},{safe.y:0} {safe.width:0}x{safe.height:0}\n{_fps:0} fps";
        }
    }
}
