using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using MinersIdle.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9g E3 (D-310, D-311, D-314, D-316, D-318): a Bomba na cena Game, no GDD v8. O dedo é simulado com os eventos de
    /// ponteiro na faixa (o mesmo caminho do toque de verdade): arrastar da esquerda para a direita com a bomba cheia lança,
    /// e cada cristal no alcance perde a % da vida máxima; vazia, para a esquerda ou para cima, não lança. Tocar no corredor
    /// não tira vida. No descanso, a linha Bomba vende a Pequena Vermelha (20%); o DBG → Bomba → Automática lança sozinho.
    /// O teste anda o tempo da simulação (o relógio do jogo parado).
    /// </summary>
    public class BombFlowTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        MiningRunner _runner;
        BombSlider _slider;

        MiningSimulation Simulation => _runner.Simulation;

        IEnumerator LoadGame()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            _slider = Object.FindAnyObjectByType<BombSlider>();
            Assert.That(_slider, Is.Not.Null, "a cena precisa da faixa da bomba (rode o setup do projeto)");
            if (!Simulation.HasBomb) Assert.Ignore("O balanço ativo não tem bomba (rode o setup do projeto: GDD v8).");
            _runner.TimeScale = 0f; // o teste anda o tempo
            WalkUntilACrystalIsInReach();
        }

        /// <summary>O anão anda até haver cristal no alcance (sem cristal, a bomba não sai, D-307).</summary>
        void WalkUntilACrystalIsInReach()
        {
            for (int i = 0; i < 200 && !Simulation.HasCrystalInBombReach; i++) Simulation.Tick(0.05);
            Assert.That(Simulation.HasCrystalInBombReach, Is.True, "um cristal no alcance da bomba");
        }

        List<Crystal> CrystalsInReach()
        {
            double reach = Simulation.Bombs.Reach;
            return Simulation.Crystals.Where(c => c.Distance - Simulation.Miner.Distance <= reach + 1e-9).ToList();
        }

        /// <summary>O centro de um ponto local do retângulo, na tela (a interface é Overlay: sem câmera).</summary>
        static Vector2 OnScreen(RectTransform rect, Vector2 local) =>
            RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(local));

        Vector2 KnobOnScreen => OnScreen(_slider.Knob, _slider.Knob.rect.center);

        /// <summary>O dedo encosta em <paramref name="from"/>, arrasta em passos até <paramref name="to"/> e solta.</summary>
        void Drag(Vector2 from, Vector2 to, int steps = 8)
        {
            var data = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = from,
                pressPosition = from,
            };
            var target = _slider.gameObject;
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, data, ExecuteEvents.beginDragHandler);
            for (int i = 1; i <= steps; i++)
            {
                var previous = data.position;
                data.position = Vector2.Lerp(from, to, i / (float)steps);
                data.delta = data.position - previous;
                ExecuteEvents.Execute(target, data, ExecuteEvents.dragHandler);
            }
            ExecuteEvents.Execute(target, data, ExecuteEvents.endDragHandler);
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerUpHandler);
        }

        /// <summary>Da bomba até o fim da faixa, da esquerda para a direita (D-310, D-318).</summary>
        void SwipeRight()
        {
            var track = _slider.Track;
            var end = OnScreen(track, new Vector2(track.rect.xMax + 20f, track.rect.center.y));
            Drag(KnobOnScreen, new Vector2(end.x, KnobOnScreen.y));
        }

        [UnityTest]
        public IEnumerator SwipingRight_WithTheBombFull_Throws_AndEveryCrystalInReachLosesTheFraction()
        {
            yield return LoadGame();
            var sound = Object.FindAnyObjectByType<SoundBoard>();
            Assert.That(Simulation.IsBombCharged, Is.True, "o jogo abre com a bomba cheia (D-313)");
            double fraction = Simulation.BombFraction;
            Assert.That(fraction, Is.EqualTo(0.1).Within(1e-9), "a Pequena, de graça: 10% (D-240)");

            Assert.That(CrystalsInReach(), Is.Not.Empty);
            var healthBefore = Simulation.Crystals.ToDictionary(c => c, c => c.Health);
            var hits = new Dictionary<Crystal, double>();
            var landed = new List<Crystal>();
            void OnBombHit(MiningHit hit) => hits[hit.Crystal] = hit.Damage;
            void OnLanded(IReadOnlyList<Crystal> targets) => landed.AddRange(targets);
            Simulation.BombHitLanded += OnBombHit;
            Simulation.BombLanded += OnLanded;
            int explosions = sound != null ? sound.PlayedCount(Sfx.Explosion) : 0;
            try
            {
                SwipeRight();
                Assert.That(_slider.Launches, Is.EqualTo(1), "o deslize lança");
                Assert.That(Simulation.IsBombInFlight, Is.True, "no ar por 0,4 s (D-308)");
                Assert.That(hits, Is.Empty, "o dano só entra na chegada");

                Simulation.Tick(Simulation.Bombs.FlightSeconds + 1e-6);
                yield return null;
            }
            finally
            {
                Simulation.BombHitLanded -= OnBombHit;
                Simulation.BombLanded -= OnLanded;
            }

            // No v8 o golpe do anão pode quebrar o alvo no meio do voo: vale a fila no alcance na chegada (D-308).
            Assert.That(landed, Is.Not.Empty, "a bomba caiu em algum cristal");
            Assert.That(hits.Keys, Is.EquivalentTo(landed), "todos os cristais no alcance, cada um uma vez (D-307)");
            foreach (var crystal in landed)
                Assert.That(hits[crystal], Is.EqualTo(crystal.MaxHealth * fraction).Within(1e-9), "10% da vida máxima, sem crítico");
            foreach (var crystal in Simulation.Crystals)
                if (!landed.Contains(crystal) && crystal != Simulation.Miner.Target && healthBefore.TryGetValue(crystal, out var before))
                    Assert.That(crystal.Health, Is.EqualTo(before), "fora do alcance, nada");
            if (sound != null && sound.SoundEnabled)
                Assert.That(sound.PlayedCount(Sfx.Explosion), Is.EqualTo(explosions + 1), "o \"bum\" da explosão (D-316)");
            Assert.That(Simulation.IsBombCharged, Is.False, "a carga recomeça");
        }

        [UnityTest]
        public IEnumerator SwipingRight_WhileCharging_DoesNotThrow()
        {
            yield return LoadGame();
            SwipeRight();
            Simulation.Tick(Simulation.Bombs.FlightSeconds + 1e-6);
            yield return new WaitForSecondsRealtime(0.3f); // o botão volta ao começo
            Assert.That(_slider.Launches, Is.EqualTo(1));
            Assert.That(Simulation.IsBombCharged, Is.False, "carregando");
            WalkUntilACrystalIsInReach();

            SwipeRight();
            Assert.That(_slider.Launches, Is.EqualTo(1), "vazia, não sai");
            Assert.That(Simulation.IsBombInFlight, Is.False);
        }

        [UnityTest]
        public IEnumerator SwipingLeftOrUp_DoesNotThrow()
        {
            yield return LoadGame();
            var start = KnobOnScreen;
            Drag(start, start + new Vector2(-120f, 0f));
            Assert.That(_slider.Launches, Is.EqualTo(0), "para a esquerda");
            Drag(start, start + new Vector2(0f, 200f));
            Assert.That(_slider.Launches, Is.EqualTo(0), "para cima");
            Drag(start, start + new Vector2(0f, -200f));
            Assert.That(_slider.Launches, Is.EqualTo(0), "para baixo");
            Assert.That(Simulation.IsBombCharged, Is.True, "a bomba continua cheia");
            Assert.That(Simulation.IsBombInFlight, Is.False);
        }

        [UnityTest]
        public IEnumerator TappingTheCorridor_TakesNoLife()
        {
            yield return LoadGame();
            var tap = Object.FindAnyObjectByType<TapInput>();
            Assert.That(tap, Is.Not.Null);
            var health = Simulation.Crystals.Select(c => c.Health).ToList();
            var points = new[]
            {
                new Vector2(Screen.width * 0.5f, Screen.height * 0.5f),
                new Vector2(Screen.width * 0.6f, Screen.height * 0.6f),
                new Vector2(Screen.width * 0.4f, Screen.height * 0.4f),
            };
            for (int i = 0; i < 20; i++) tap.TapAt(points[i % points.Length]);
            yield return null;

            for (int i = 0; i < health.Count; i++)
                Assert.That(Simulation.Crystals[i].Health, Is.EqualTo(health[i]), "o toque não bate (D-311)");
        }

        [UnityTest]
        public IEnumerator BuyingTheSmallRedBomb_AtRest_TakesTwentyPercent()
        {
            yield return LoadGame();
            var rest = Object.FindAnyObjectByType<RestShopScreen>();
            Simulation.SkipToCave(12);
            var modal = Object.FindAnyObjectByType<RestScreen>();
            modal.Open();
            modal.transform.Find("Screen/Panel/Confirm").GetComponent<Button>().onClick.Invoke();
            rest.SkipZzz();
            Simulation.AddRedShards(100); // o DBG → Dia → +100 vermelhas

            var row = rest.Rows.First(r => r.Permanent == PermanentId.Bomb);
            Assert.That(row.TitleText, Is.EqualTo("Bomba"));
            Assert.That(row.LevelText, Is.EqualTo("nv 0/7"));
            Assert.That(row.EffectText, Is.EqualTo("Pequena 10% » 20%"), "do mockup (D-314)");
            Assert.That(row.ButtonText, Is.EqualTo("11"), "10 + 2^0 (D-255)");
            Assert.That(row.CanBuy, Is.True);
            row.transform.Find("Buy").GetComponent<Button>().onClick.Invoke();

            Assert.That(Simulation.Permanents.State.LevelOf(PermanentId.Bomb), Is.EqualTo(1));
            Assert.That(row.LevelText, Is.EqualTo("nv 1/7"));
            Assert.That(row.EffectText, Is.EqualTo("Pequena Vermelha 20% » 30%"));
            Assert.That(row.ButtonText, Is.EqualTo("12"), "10 + 2^1");
            Assert.That(row.IconSprite, Is.Not.Null);
            Assert.That(row.IconSprite.name, Does.EndWith("_1"), "o ícone é a bomba comprada");
            Assert.That(Simulation.BombFraction, Is.EqualTo(0.2).Within(1e-9));

            rest.transform.Find("Screen/Panel/Start").GetComponent<Button>().onClick.Invoke();
            Assert.That(_runner.IsResting, Is.False);
            WalkUntilACrystalIsInReach();
            Simulation.FillBomb();
            var hits = new List<MiningHit>();
            void OnBombHit(MiningHit hit) => hits.Add(hit);
            Simulation.BombHitLanded += OnBombHit;
            try
            {
                SwipeRight();
                Simulation.Tick(Simulation.Bombs.FlightSeconds + 1e-6);
            }
            finally
            {
                Simulation.BombHitLanded -= OnBombHit;
            }
            Assert.That(hits, Is.Not.Empty, "a bomba saiu");
            foreach (var hit in hits)
                Assert.That(hit.Damage, Is.EqualTo(hit.Crystal.MaxHealth * 0.2).Within(1e-9), "20% da vida máxima");

            // Na LOJA da run, a linha é só consulta: a bomba de agora.
            var shop = Object.FindAnyObjectByType<ShopView>();
            shop.OpenPermanents();
            var consult = shop.PermanentRows.First(r => r.Permanent == PermanentId.Bomb);
            Assert.That(consult.IsConsult, Is.True);
            Assert.That(consult.EffectText, Is.EqualTo("Pequena Vermelha 20%"));
        }

        [UnityTest]
        public IEnumerator TheDebugAutoBomb_ThrowsEveryTimeItFills()
        {
            yield return LoadGame();
            var panel = Object.FindAnyObjectByType<BombDebugPanel>(FindObjectsInactive.Include);
            Assert.That(panel, Is.Not.Null, "DBG → Bomba (rode o setup do projeto)");
            int thrown = 0;
            void OnThrown(IReadOnlyList<Crystal> targets) => thrown++;
            Simulation.BombThrown += OnThrown;
            try
            {
                panel.ToggleAuto();
                Assert.That(Simulation.AutoBomb, Is.True);
                Simulation.Tick(0.01);
                Assert.That(thrown, Is.EqualTo(1), "cheia, sai na hora");
                for (int i = 0; i < 2; i++)
                {
                    Simulation.Tick(Simulation.Bombs.FlightSeconds + 1e-6);
                    WalkUntilACrystalIsInReach();
                    Simulation.Tick(Simulation.BombChargeLeft + 0.01);
                }
                Assert.That(thrown, Is.GreaterThanOrEqualTo(3), "e de novo a cada carga de 5 s");
            }
            finally
            {
                Simulation.BombThrown -= OnThrown;
                panel.ToggleAuto();
            }
            Assert.That(Simulation.AutoBomb, Is.False, "desliga no mesmo botão");
            yield return null;
        }
    }
}
