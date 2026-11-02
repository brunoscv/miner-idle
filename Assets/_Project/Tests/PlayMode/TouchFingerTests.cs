using System.Collections;
using MinersIdle.Game;
using MinersIdle.Presentation;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace MinersIdle.Tests
{
    /// <summary>
    /// Um toque por dedo (D-118) e um de cada vez (D-123), com dedos simulados numa tela de toque virtual do Input System:
    /// dedo parado, arrastando ou um aparelho que repete o começo do toque contam uma vez só; 2 ou 3 dedos juntos valem 1;
    /// um dedo de cada vez, cada encostada conta. O teto de 15 por segundo (D-119) e o alcance do toque dentro da tela
    /// (D-121) com os valores da cena.
    /// </summary>
    public class TouchFingerTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        TapInput _input;
        MiningRunner _runner;
        Touchscreen _touch;
        InputSettings _settings;
        int _tapped;

        IEnumerator LoadGame()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(
                GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null; // um quadro de layout para a UI ter posição

            _input = UnityEngine.Object.FindAnyObjectByType<TapInput>();
            _runner = UnityEngine.Object.FindAnyObjectByType<MiningRunner>();
            Assert.That(_input, Is.Not.Null, "a cena Game precisa do leitor de toque (rode o setup do projeto)");
            // Em batch o jogo nunca tem foco: sem isto, o Input System desliga a tela de toque virtual.
            _settings = InputSystem.settings;
            var settings = Object.Instantiate(_settings);
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = settings;

            _touch = InputSystem.AddDevice<Touchscreen>();
            _tapped = 0;
            _input.Tapped += OnTapped;
        }

        void OnTapped(Vector2 _) => _tapped++;

        [TearDown]
        public void TearDown()
        {
            if (_input != null) _input.Tapped -= OnTapped;
            if (_touch != null) InputSystem.RemoveDevice(_touch);
            _touch = null;
            if (_settings == null) return;
            var changed = InputSystem.settings;
            InputSystem.settings = _settings;
            Object.Destroy(changed);
            _settings = null;
        }

        /// <summary>Um ponto do corredor, fora da interface, para o dedo <paramref name="finger"/>.</summary>
        static Vector2 Spot(int finger) => new Vector2(Screen.width * (0.4f + 0.1f * (finger % 3)), Screen.height * 0.5f);

        void Send(int finger, TouchPhase phase, Vector2? position = null) =>
            InputSystem.QueueStateEvent(_touch, new TouchState
            {
                touchId = finger,
                phase = phase,
                position = position ?? Spot(finger),
                pressure = phase == TouchPhase.Ended || phase == TouchPhase.Canceled ? 0f : 1f,
            });

        static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
        }

        IEnumerator LiftAll(params int[] fingers)
        {
            foreach (int finger in fingers) Send(finger, TouchPhase.Ended);
            yield return Frames(2);
        }

        [UnityTest]
        public IEnumerator ThreeFingersHeld_CountOneTap()
        {
            yield return LoadGame();

            for (int finger = 1; finger <= 3; finger++) Send(finger, TouchPhase.Began);
            yield return Frames(120);

            Assert.That(_tapped, Is.EqualTo(1), "3 dedos juntos valem 1, e dedo parado não conta de novo");
            Assert.That(_input.FingersDown, Is.EqualTo(3));
            yield return LiftAll(1, 2, 3);
            Assert.That(_input.FingersDown, Is.EqualTo(0));
        }

        /// <summary>O que o celular do amigo do Bruno parece ter feito: o aparelho manda "começou" de novo a cada quadro.</summary>
        [UnityTest]
        public IEnumerator DeviceRepeatingBegan_StillCountsOnce()
        {
            yield return LoadGame();

            for (int frame = 0; frame < 60; frame++)
            {
                for (int finger = 1; finger <= 3; finger++) Send(finger, TouchPhase.Began);
                yield return null;
            }

            Assert.That(_tapped, Is.EqualTo(1));
            yield return LiftAll(1, 2, 3);
        }

        [UnityTest]
        public IEnumerator ThreeFingersTappingTogether_OneTapPerPress()
        {
            yield return LoadGame();

            for (int round = 0; round < 4; round++)
            {
                for (int finger = 1; finger <= 3; finger++) Send(finger, TouchPhase.Began);
                yield return Frames(2);
                yield return LiftAll(1, 2, 3); // os mesmos ids, como o Android reaproveita
            }

            // Também prova que o Rendering Debugger do URP (dois toques de 3 dedos) não abre por cima do jogo.
            Assert.That(_tapped, Is.EqualTo(4), "4 encostadas de 3 dedos juntos");
        }

        /// <summary>Um dedo de cada vez (cada um sai antes do próximo encostar): todos contam, mesmo alternando os dedos.</summary>
        [UnityTest]
        public IEnumerator OneFingerAtATime_EveryPressCounts()
        {
            yield return LoadGame();

            for (int round = 0; round < 2; round++)
                for (int finger = 1; finger <= 3; finger++)
                {
                    Send(finger, TouchPhase.Began);
                    yield return Frames(2);
                    yield return LiftAll(finger);
                }

            Assert.That(_tapped, Is.EqualTo(6));
        }

        [UnityTest]
        public IEnumerator FingerWhileAnotherIsHeld_DoesNotCount()
        {
            yield return LoadGame();

            Send(1, TouchPhase.Began);
            yield return Frames(2);
            for (int i = 0; i < 3; i++)
            {
                Send(2, TouchPhase.Began);
                yield return Frames(2);
                yield return LiftAll(2);
            }

            Assert.That(_tapped, Is.EqualTo(1), "só o dedo que encontrou a tela livre");
            yield return LiftAll(1);
        }

        [UnityTest]
        public IEnumerator TwoQuickTapsInOneFrame_CountOne()
        {
            yield return LoadGame();

            foreach (int finger in new[] { 1, 2 })
            {
                Send(finger, TouchPhase.Began);
                Send(finger, TouchPhase.Ended);
            }
            yield return Frames(3);

            Assert.That(_tapped, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DraggingFinger_CountsOnce()
        {
            yield return LoadGame();

            Send(1, TouchPhase.Began);
            yield return null;
            for (int frame = 0; frame < 30; frame++)
            {
                Send(1, TouchPhase.Moved, Spot(1) + new Vector2(frame * 2f, frame));
                yield return null;
            }

            Assert.That(_tapped, Is.EqualTo(1));
            yield return LiftAll(1);
        }

        [UnityTest]
        public IEnumerator TapInsideOneFrame_Counts()
        {
            yield return LoadGame();

            Send(1, TouchPhase.Began);
            Send(1, TouchPhase.Ended);
            yield return Frames(3);

            Assert.That(_tapped, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ThirtyTapsAtOnce_FifteenCount()
        {
            yield return LoadGame();
            int combo = _runner.Simulation.Combo;
            int counted = 0;

            for (int i = 0; i < 30; i++)
                if (_input.TapAt(Spot(i))) counted++;

            Assert.That(counted, Is.EqualTo(15), "D-119");
            Assert.That(_tapped, Is.EqualTo(30), "a faísca sai em todos");
            Assert.That(_runner.Simulation.Combo, Is.EqualTo(combo + 15));
        }

        /// <summary>Os valores do jogo (D-119, D-121, D-122) e o alcance do toque dentro do chão visível na câmera F1.</summary>
        [UnityTest]
        public IEnumerator GameValues_TapReachIsOnScreen()
        {
            yield return LoadGame();
            var simulation = _runner.Simulation;
            var stage = UnityEngine.Object.FindAnyObjectByType<DescentStage>();

            Assert.That(simulation.Strike.MaxTapsPerSecond, Is.EqualTo(15));
            Assert.That(simulation.Rules.MinCrystals, Is.EqualTo(10));
            float reachMeters = (float)simulation.Rules.TapReach * stage.MetersPerUnit;
            Assert.That(reachMeters, Is.LessThanOrEqualTo(stage.Framing.GroundAhead(DescentFraming.ReferenceAspect) + 0.01f),
                        "o alvo dentro do alcance já está inteiro na tela 9:16");
        }

        [UnityTest]
        public IEnumerator HeldFingers_ReadingTouchesDoesNotAllocate()
        {
            yield return LoadGame();
            for (int finger = 1; finger <= 3; finger++) Send(finger, TouchPhase.Began);
            yield return Frames(5);

            var recorder = Recorder.Get("GC.Alloc");
            recorder.FilterToCurrentThread();
            recorder.enabled = false;
            recorder.enabled = true;
            for (int i = 0; i < 10; i++) _input.ReadTouches();
            recorder.enabled = false;
            int allocations = recorder.sampleBlockCount;
            recorder.CollectFromAllThreads();

            Assert.That(allocations, Is.EqualTo(0));
            yield return LiftAll(1, 2, 3);
        }
    }
}
