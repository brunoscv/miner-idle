using UnityEngine;

namespace MinersIdle.UI
{
    /// <summary>
    /// Ajusta o RectTransform ao Safe Area do aparelho (notch, recortes, barra de gestos).
    /// Colocar num painel que preenche o Canvas; todo o HUD entra como filho dele.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaController : MonoBehaviour
    {
        RectTransform _rect;
        Rect _lastSafeArea;
        Vector2Int _lastScreen;

        void OnEnable()
        {
            _rect = (RectTransform)transform;
            Apply();
        }

        void Update()
        {
            // Barato, e cobre dobráveis e mudança de tamanho de janela.
            if (Screen.safeArea != _lastSafeArea || Screen.width != _lastScreen.x || Screen.height != _lastScreen.y)
                Apply();
        }

        void Apply()
        {
            _lastSafeArea = Screen.safeArea;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);
            ComputeAnchors(_lastSafeArea, Screen.width, Screen.height, out var min, out var max);
            _rect.anchorMin = min;
            _rect.anchorMax = max;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }

        /// <summary>Converte o Safe Area (em pixels) em âncoras normalizadas (0–1). Pura, para testar sem aparelho.</summary>
        public static void ComputeAnchors(Rect safeArea, int screenWidth, int screenHeight, out Vector2 min, out Vector2 max)
        {
            if (screenWidth <= 0 || screenHeight <= 0)
            {
                min = Vector2.zero;
                max = Vector2.one;
                return;
            }

            min = new Vector2(safeArea.xMin / screenWidth, safeArea.yMin / screenHeight);
            max = new Vector2(safeArea.xMax / screenWidth, safeArea.yMax / screenHeight);
        }
    }
}
