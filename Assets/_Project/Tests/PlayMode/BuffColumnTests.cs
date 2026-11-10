using System;
using System.Collections;
using System.IO;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Persistence;
using MinersIdle.Presentation;
using MinersIdle.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9b, E2, na cena Game de verdade: a coluna de buffs à esquerda (um ícone por bônus, o anel esvaziando, os segundos,
    /// o piscar no fim), o detalhe ao tocar no ícone, o toque na coluna sem virar toque no mapa, o texto do ganho com o
    /// "FRENESI!" e o DANO do HUD em âmbar (D-215, D-216). Reabrir o jogo traz os ícones com o tempo certo.
    /// </summary>
    public class BuffColumnTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        string _dir;
        DateTime _now;
        MiningRunner _runner;
        BuffColumn _column;
        HudView _hud;
        MiningFeedbackView _feedback;

        MiningSimulation Simulation => _runner.Simulation;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "MinersIdleBuffColumn_" + Guid.NewGuid().ToString("N"));
            _now = new DateTime(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        IEnumerator Open()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            _column = Object.FindAnyObjectByType<BuffColumn>();
            _hud = Object.FindAnyObjectByType<HudView>();
            _feedback = Object.FindAnyObjectByType<MiningFeedbackView>();
            Assert.That(_column, Is.Not.Null, "rode o setup do projeto (coluna de buffs)");
            _runner.UseClock(() => _now);
            _runner.TimeScale = 0f; // o teste anda o tempo
            _runner.UseSave(new SaveService(new FileSaveStore(_dir)));
            if (Simulation.Strike.ComboBonuses.Count == 0)
                Assert.Ignore("O balanço ativo não tem os marcos do combo (rode o setup: GDD v7).");
            yield return null;
        }

        [UnityTest]
        public IEnumerator StoppingAtThirtyFive_PutsTwoIcons_ThatEmptyAndLeaveInOrder_AndTheDamageIsAmber()
        {
            yield return Open();
            Assert.That(_column.ShownCount, Is.EqualTo(0));
            Assert.That(_hud.IsDamageBoosted, Is.False);

            Simulation.GrantComboBonuses(35);
            yield return null;

            Assert.That(_column.ShownCount, Is.EqualTo(2));
            Assert.That(_column.SlotAt(0).SecondsText, Is.EqualTo("5s"));
            Assert.That(_column.SlotAt(1).SecondsText, Is.EqualTo("10s"));
            Assert.That(_column.SlotAt(0).Icon, Is.Not.Null, "o ícone do catálogo");
            Assert.That(_column.SlotAt(1).Fill, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(_hud.IsDamageBoosted, Is.True, "D-216: o DANO em âmbar");
            Assert.That(_feedback.LastGainText, Does.Contain("25%"));

            Simulation.Tick(3);
            yield return null;
            Assert.That(_column.SlotAt(1).Fill, Is.EqualTo(0.7f).Within(1e-3f), "o anel esvazia");
            Assert.That(_column.SlotAt(0).SecondsText, Is.EqualTo("2s"));
            Assert.That(_column.SlotAt(0).SecondsColor, Is.Not.EqualTo(_column.SlotAt(1).SecondsColor), "os últimos 5 s em âmbar");

            Simulation.Tick(2);
            yield return null;
            Assert.That(_column.ShownCount, Is.EqualTo(1), "o +10% saiu aos 5 s");
            Assert.That(_column.SlotAt(0).SecondsText, Is.EqualTo("5s"), "o +15% subiu para o primeiro lugar");

            Simulation.Tick(5);
            yield return null;
            Assert.That(_column.ShownCount, Is.EqualTo(0));
            Assert.That(_hud.IsDamageBoosted, Is.False, "a cor volta");
        }

        [UnityTest]
        public IEnumerator EndingIcon_Blinks()
        {
            yield return Open();
            Simulation.GrantComboBonuses(10);
            Simulation.Tick(3);
            float min = 1f, max = 0f;
            for (float t = 0; t < 0.8f; t += Time.unscaledDeltaTime)
            {
                yield return null;
                min = Mathf.Min(min, _column.SlotAt(0).Alpha);
                max = Mathf.Max(max, _column.SlotAt(0).Alpha);
            }
            Assert.That(min, Is.LessThan(0.9f));
            Assert.That(max, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator TappingAnIcon_ShowsTheDetailForTwoSeconds_WithoutCountingAsAMapTap()
        {
            yield return Open();
            Simulation.GrantComboBonuses(57);
            yield return null;
            var critical = _column.SlotAt(2);
            int combo = Simulation.Combo;

            var tapInput = Object.FindAnyObjectByType<TapInput>();
            var screen = RectTransformUtility.WorldToScreenPoint(null, critical.Rect.position);
            Assert.That(tapInput.TapAt(screen), Is.False, "o toque na coluna não vale no mapa");
            Assert.That(Simulation.Combo, Is.EqualTo(combo));

            critical.Button.onClick.Invoke();
            Assert.That(_column.IsDetailOpen, Is.True);
            Assert.That(_column.DetailIndex, Is.EqualTo(2));
            Assert.That(_column.DetailTitleText, Is.Not.Empty);
            Assert.That(_column.DetailFooterText, Does.Contain("50").And.Contain("20"));
            Assert.That(_runner.TimeScale, Is.EqualTo(0f), "não pausa nem muda o tempo");

            yield return new WaitForSecondsRealtime(BuffColumn.DetailSeconds + 0.2f);
            Assert.That(_column.IsDetailOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator Frenzy_ShowsTheHeadline_AndTheSum()
        {
            yield return Open();
            Simulation.GrantComboBonuses(104);
            yield return null;

            Assert.That(_feedback.LastHeadline, Is.Not.Null.And.Not.Empty);
            Assert.That(_feedback.LastGainText, Does.Contain("75%"));
            Assert.That(_column.ShownCount, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator Reopening_BringsTheIconsBack_WithTheTimeLeft()
        {
            yield return Open();
            Simulation.GrantComboBonuses(50);
            Simulation.Tick(4);
            _runner.SaveNow();

            yield return Open();
            Assert.That(_column.ShownCount, Is.EqualTo(3), "o ×10 (ainda 1 s), o ×30 e o crítico");
            Assert.That(_column.SlotAt(0).SecondsText, Is.EqualTo("1s"));
            Assert.That(_column.SlotAt(2).SecondsText, Is.EqualTo("16s"));
        }
    }
}
