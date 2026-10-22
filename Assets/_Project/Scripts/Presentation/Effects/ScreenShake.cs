using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// Tremor da câmera (M5, E3): desloca a câmera em volta da posição de base e volta. Só ilustra; desligável no DBG
    /// (e nas Configurações, no M8). Um tremor novo só substitui o atual se for mais forte.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class ScreenShake : MonoBehaviour
    {
        Vector3 _basePosition;
        float _amplitude;
        float _duration;
        float _remaining;

        public bool ShakeEnabled { get; set; } = true;
        public bool IsShaking => _remaining > 0f;

        void Awake() => _basePosition = transform.localPosition;

        /// <param name="amplitude">Deslocamento máximo, em unidades de mundo.</param>
        public void Shake(float amplitude, float duration)
        {
            if (!ShakeEnabled || duration <= 0f) return;
            float current = _remaining > 0f ? _amplitude * (_remaining / _duration) : 0f;
            if (amplitude < current) return;

            _amplitude = amplitude;
            _duration = duration;
            _remaining = duration;
        }

        void LateUpdate()
        {
            if (_remaining <= 0f) return;

            _remaining -= Time.unscaledDeltaTime;
            if (_remaining <= 0f || !ShakeEnabled)
            {
                _remaining = 0f;
                transform.localPosition = _basePosition;
                return;
            }

            Vector2 offset = Random.insideUnitCircle * (_amplitude * (_remaining / _duration));
            transform.localPosition = _basePosition + new Vector3(offset.x, offset.y, 0f);
        }
    }
}
