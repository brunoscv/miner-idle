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
    /// Lê o toque na tela pelo Input System (D-T12) e o transforma no comando <c>Tap()</c> da simulação (D-47): qualquer
    /// ponto que não esteja sobre a interface (HUD, LOJA, painéis, DBG) conta. No Editor, o clique do mouse vale como
    /// toque. Os testes e o debug passam pelo mesmo <see cref="TapAt"/>.
    ///
    /// Um toque por dedo (D-118): o dedo vale quando encosta e só volta a valer depois de sair da tela; parado ou
    /// arrastando, não conta de novo. A leitura é a do <c>EnhancedTouch</c>, guardando os dedos (<c>touchId</c>) que já
    /// contaram: no celular de um testador, a leitura direta do <c>Touchscreen</c> contava o dedo parado a cada quadro.
    /// Um toque de cada vez (D-123): o dedo só vale se encostou com a tela livre; 2 ou 3 dedos juntos valem 1 toque, e
    /// um dedo que encosta enquanto outro está na tela não conta. O teto de toques por segundo (D-119) é da regra, na
    /// simulação.
    /// </summary>
    public sealed class TapInput : MonoBehaviour
    {
        /// <summary>Ritmo do "Toques automáticos" do debug.</summary>
        public const float AutoTapsPerSecond = 5f;

        /// <summary>Dedos acompanhados ao mesmo tempo; um dedo a mais que isso não conta.</summary>
        const int MaxFingers = 10;

        [SerializeField] MiningRunner runner;

        readonly List<RaycastResult> _uiHits = new List<RaycastResult>();
        readonly int[] _fingers = new int[MaxFingers];
        readonly bool[] _fingerSeen = new bool[MaxFingers];
        int _fingerCount;
        PointerEventData _pointer;
        float _autoTapTimer;

        TapCounts _counting;
        float _countingSince;

        /// <summary>Atalho do debug: toca sozinho no centro da tela, 5 vezes por segundo.</summary>
        public bool AutoTap { get; set; }

        /// <summary>Um toque fora da interface, com a posição na tela (a faísca no dedo sai mesmo acima do teto).</summary>
        public event Action<Vector2> Tapped;

        /// <summary>O que o aparelho mandou e o que valeu no último segundo completo (para o DBG).</summary>
        public TapCounts LastSecond { get; private set; }

        /// <summary>Dedos encostados agora.</summary>
        public int FingersDown => _fingerCount;

        void Awake()
        {
            // Em build de desenvolvimento, o URP abre o Rendering Debugger com dois toques de 3 dedos, e o painel dele
            // cobre a tela e engole os toques seguintes. Bater com 3 dedos é jogar; o nosso debug é o DBG.
            DebugManager.instance.enableRuntimeUI = false;
        }

        void OnEnable() => EnhancedTouchSupport.Enable();

        void OnDisable() => EnhancedTouchSupport.Disable();

        void Update()
        {
            bool touched = ReadTouches();

            // Mouse só quando não houve toque no quadro (no simulador do Editor os dois podem chegar juntos).
            var mouse = Mouse.current;
            if (!touched && mouse != null && mouse.leftButton.wasPressedThisFrame)
                TapAt(mouse.position.ReadValue());

            float now = Time.unscaledTime;
            if (now - _countingSince >= 1f)
            {
                LastSecond = _counting;
                _counting = default;
                _countingSince = now;
            }

            if (!AutoTap) return;
            _autoTapTimer += Time.unscaledDeltaTime;
            float period = 1f / AutoTapsPerSecond;
            while (_autoTapTimer >= period)
            {
                _autoTapTimer -= period;
                TapAt(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            }
        }

        /// <summary>
        /// Os dedos deste quadro: conta o dedo que aparece pela primeira vez (normalmente na fase <c>Began</c>; também um
        /// toque que começou e acabou entre dois quadros), se a tela estava livre (D-123), e esquece o que saiu da tela.
        /// É o que cada quadro faz (os testes medem que não aloca). <c>true</c> se havia algum dedo.
        /// </summary>
        public bool ReadTouches()
        {
            var touches = Touch.activeTouches;
            for (int i = 0; i < _fingerCount; i++) _fingerSeen[i] = false;
            bool tappedThisFrame = false; // dois dedos que encostam no mesmo quadro: só o primeiro

            // Primeiro os dedos que saíram: um id solto e encostado de novo no mesmo quadro é um toque novo.
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                var phase = touch.phase;
                if (phase != TouchPhase.Ended && phase != TouchPhase.Canceled) continue;

                int finger = IndexOfFinger(touch.touchId);
                if (finger >= 0) ForgetFinger(finger);
                else if (phase == TouchPhase.Ended) // encostou e soltou entre dois quadros
                    TapIfAlone(touch.screenPosition, _fingerCount == 0, ref tappedThisFrame);
            }

            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                var phase = touch.phase;
                if (phase == TouchPhase.Ended || phase == TouchPhase.Canceled) continue;
                if (phase == TouchPhase.Began) _counting.Began++;

                int finger = IndexOfFinger(touch.touchId);
                if (finger >= 0)
                {
                    _fingerSeen[finger] = true;
                    if (phase == TouchPhase.Began) _counting.Repeated++; // o aparelho repetiu o começo: não conta
                    continue;
                }

                if (_fingerCount == MaxFingers) continue; // sem lugar para guardar, contaria a cada quadro
                bool alone = _fingerCount == 0;
                _fingers[_fingerCount] = touch.touchId;
                _fingerSeen[_fingerCount] = true;
                _fingerCount++;
                TapIfAlone(touch.screenPosition, alone, ref tappedThisFrame);
            }

            // Esquece os dedos que sumiram da lista sem passar por Ended.
            for (int i = _fingerCount - 1; i >= 0; i--)
                if (!_fingerSeen[i]) ForgetFinger(i);

            return touches.Count > 0;
        }

        /// <summary>
        /// O toque de um dedo novo vale se ele encontrou a tela livre (<paramref name="alone"/>) e nenhum outro dedo já
        /// contou neste quadro (D-123).
        /// </summary>
        void TapIfAlone(Vector2 screenPosition, bool alone, ref bool tappedThisFrame)
        {
            if (!alone || tappedThisFrame)
            {
                _counting.Together++;
                return;
            }
            tappedThisFrame = true;
            TapAt(screenPosition);
        }

        void ForgetFinger(int index)
        {
            _fingerCount--;
            _fingers[index] = _fingers[_fingerCount];
            _fingerSeen[index] = _fingerSeen[_fingerCount];
        }

        int IndexOfFinger(int touchId)
        {
            for (int i = 0; i < _fingerCount; i++)
                if (_fingers[i] == touchId) return i;
            return -1;
        }

        /// <summary>
        /// Toque na posição de tela dada. Devolve <c>false</c> se ela estava sobre a interface (não conta) ou se o toque
        /// passou do teto por segundo (D-119: só a faísca).
        /// </summary>
        public bool TapAt(Vector2 screenPosition)
        {
            var simulation = runner != null ? runner.Simulation : null;
            if (simulation == null || IsOverUi(screenPosition)) return false;

            bool counted = simulation.Tap();
            if (counted) _counting.Counted++;
            else _counting.Capped++;
            Tapped?.Invoke(screenPosition);
            return counted;
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

    /// <summary>Contagem de toques num intervalo, para o diagnóstico do DBG (D-118, D-119).</summary>
    public struct TapCounts
    {
        /// <summary>Fases <c>Began</c> que o aparelho mandou.</summary>
        public int Began;
        /// <summary><c>Began</c> de um dedo que já estava na tela: o aparelho repetindo o começo (não conta).</summary>
        public int Repeated;
        /// <summary>Dedos que encostaram com outro na tela, ou junto com outro no mesmo quadro (D-123: não contam).</summary>
        public int Together;
        /// <summary>Toques que valeram na regra.</summary>
        public int Counted;
        /// <summary>Toques acima do teto por segundo (só a faísca).</summary>
        public int Capped;
    }
}
