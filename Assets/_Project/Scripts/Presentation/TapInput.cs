using System;
using System.Collections.Generic;
using MinersIdle.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Rendering;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace MinersIdle.Presentation
{
    /// <summary>
    /// Lê o dedo na tela pelo Input System (D-T12) só para a faísca no dedo (M9g, D-311): o toque não tira vida, não faz
    /// combo e não tem teto. Cada dedo que encosta fora da interface (HUD, LOJA, painéis, DBG, a faixa da bomba) solta a
    /// faísca; no Editor, o clique do mouse também. Na transição de andar, nada. A bomba se lança na faixa dela
    /// (<c>BombSlider</c>, D-318), pelos eventos da interface. Os testes passam pelo mesmo <see cref="TapAt"/>.
    /// </summary>
    public sealed class TapInput : MonoBehaviour
    {
        [SerializeField] MiningRunner runner;

        readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
        PointerEventData _pointer;

        /// <summary>Um dedo que encostou fora da interface, com a posição na tela (a faísca no dedo).</summary>
        public event Action<Vector2> Tapped;

        void Awake()
        {
            // Em build de desenvolvimento, o URP abre o Rendering Debugger com dois toques de 3 dedos, e o painel dele
            // cobre a tela e engole os toques seguintes. O nosso debug é o DBG.
            DebugManager.instance.enableRuntimeUI = false;
        }

        void OnEnable() => EnhancedTouchSupport.Enable();

        void OnDisable() => EnhancedTouchSupport.Disable();

        void Update()
        {
            var touches = Touch.activeTouches;
            for (int i = 0; i < touches.Count; i++)
                if (touches[i].phase == TouchPhase.Began) TapAt(touches[i].screenPosition);

            // Mouse só quando não houve toque no quadro (no simulador do Editor os dois podem chegar juntos).
            var mouse = Mouse.current;
            if (touches.Count == 0 && mouse != null && mouse.leftButton.wasPressedThisFrame)
                TapAt(mouse.position.ReadValue());
        }

        /// <summary>
        /// O dedo na posição de tela dada: solta a faísca e devolve <c>true</c>; sobre a interface, sem partida ou na
        /// transição de andar, nada (<c>false</c>).
        /// </summary>
        public bool TapAt(Vector2 screenPosition)
        {
            var simulation = runner != null ? runner.Simulation : null;
            if (simulation == null || IsOverUi(screenPosition)) return false;
            if (simulation.IsInFloorTransition) return false; // M9c E0: na transição de andar, nem o efeito

            Tapped?.Invoke(screenPosition);
            return true;
        }

        /// <summary>A posição está sobre algo da interface que recebe toque.</summary>
        public bool IsOverUi(Vector2 screenPosition)
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
