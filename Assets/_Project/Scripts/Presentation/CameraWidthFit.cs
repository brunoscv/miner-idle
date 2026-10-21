using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// Ajusta a câmera ortográfica para mostrar sempre a mesma <b>largura</b> de mundo, como o Canvas (que casa pela
    /// largura). Sem isso, num celular mais alto que 9:16 (o Moto G75 é ~9:20) as bordas do mapa ficam fora da tela.
    /// A sobra vai para a altura. Provisório até o Art Spike decidir resolução e Pixel Perfect (D-14).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraWidthFit : MonoBehaviour
    {
        [Tooltip("Largura visível em unidades de mundo: 270 px ÷ PPU 16 = 16,875 (D-14, provisório).")]
        [SerializeField] float worldWidth = 16.875f;

        Camera _camera;
        int _width;
        int _height;

        void Awake() => _camera = GetComponent<Camera>();

        void LateUpdate()
        {
            if (Screen.width == _width && Screen.height == _height) return;

            _width = Screen.width;
            _height = Screen.height;
            if (_width <= 0 || _height <= 0) return;

            _camera.orthographicSize = worldWidth * 0.5f * _height / _width;
        }
    }
}
