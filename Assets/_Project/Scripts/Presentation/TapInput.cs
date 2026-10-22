using System;
using System.Collections.Generic;
using MinersIdle.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// Lê o toque na tela pelo Input System (D-T12) e o transforma no comando <c>Tap()</c> da simulação (D-47): qualquer
    /// ponto que não esteja sobre a interface (HUD, LOJA, painéis, DBG) conta. Cada dedo que encosta é um toque, sem
    /// limite. No Editor, o clique do mouse vale como toque. Os testes e o debug passam pelo mesmo <see cref="TapAt"/>.
    /// </summary>
    public sealed class TapInput : MonoBehaviour
    {
        /// <summary>Ritmo do "Toques automáticos" do debug.</summary>
        public const float AutoTapsPerSecond = 5f;

        [SerializeField] MiningRunner runner;

        readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
        PointerEventData _pointer;
        float _autoTapTimer;

        /// <summary>Atalho do debug: toca sozinho no centro da tela, 5 vezes por segundo.</summary>
        public bool AutoTap { get; set; }

        /// <summary>Um toque valeu, com a posição na tela (para o feedback da E3).</summary>
        public event Action<Vector2> Tapped;

        void Update()
        {
            bool touched = false;
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                foreach (var touch in touchscreen.touches)
                {
                    if (!touch.press.wasPressedThisFrame) continue;
                    touched = true;
                    TapAt(touch.position.ReadValue());
                }
            }

            // Mouse só quando não houve toque no quadro (no simulador do Editor os dois podem chegar juntos).
            var mouse = Mouse.current;
            if (!touched && mouse != null && mouse.leftButton.wasPressedThisFrame)
                TapAt(mouse.position.ReadValue());

            if (!AutoTap) return;
            _autoTapTimer += Time.unscaledDeltaTime;
            float period = 1f / AutoTapsPerSecond;
            while (_autoTapTimer >= period)
            {
                _autoTapTimer -= period;
                TapAt(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            }
        }

        /// <summary>Toque na posição de tela dada. Devolve <c>false</c> se ela estava sobre a interface (não conta).</summary>
        public bool TapAt(Vector2 screenPosition)
        {
            var simulation = runner != null ? runner.Simulation : null;
            if (simulation == null || IsOverUi(screenPosition)) return false;

            simulation.Tap();
            Tapped?.Invoke(screenPosition);
            return true;
        }

        bool IsOverUi(Vector2 screenPosition)
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return false;

            _pointer ??= new PointerEventData(eventSystem);
            _pointer.position = screenPosition;
            _uiHits.Clear();
            eventSystem.RaycastAll(_pointer, _uiHits);
            return _uiHits.Count > 0;
        }
    }
}
