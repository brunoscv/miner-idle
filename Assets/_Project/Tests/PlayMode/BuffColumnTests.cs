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
    /// o piscar no fim), o detalhe ao tocar no ícone, o toque na coluna sem virar toque no mapa, o texto do ganho e o DANO
    /// do HUD em âmbar (D-215, D-216). Reabrir o jogo traz os ícones com o tempo certo. Até o M9g os bônus vinham dos marcos
    /// do combo; o combo saiu (D-311) e os testes usam as cartas do raro, com a duração do balanço ativo.
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
            if (!Simulation.Strike.Rare.Has(RareCard.Damage) || !Simulation.Strike.Rare.Has(RareCard.Shards))
                Assert.Ignore("O balanço ativo não tem as cartas Dano e Lascas (rode o setup do projeto).");
            yield return null;
        }

        /// <summary>Dá a carta e devolve a duração dela no balanço ativo (30 s no GDD v8).</summary>
        double GrantCard(RareCard card)
        {
            Simulation.GrantRareCard(card);
            var bonus = Simulation.Bonuses.All[Simulation.Bonuses.Count - 1];
            Assume.That(bonus.Duration, Is.GreaterThan(BuffColumn.EndingSeconds + 6), "a carta dura mais que o teste anda");
            return bonus.Duration;
        }

        [UnityTest]
        public IEnumerator TwoCards_PutTwoIcons_ThatEmptyAndLeaveInOrder_AndTheDamageIsAmber()
        {
            yield return Open();
            Assert.That(_column.ShownCount, Is.EqualTo(0));
            Assert.That(_hud.IsDamageBoosted, Is.False);

            double damage = GrantCard(RareCard.Damage);
            Simulation.Tick(5);
            Simulation.GrantRareCard(RareCard.Shards);
            yield return null;

            Assert.That(_column.ShownCount, Is.EqualTo(2));
            Assert.That(_column.SlotAt(0).SecondsText, Is.EqualTo(Math.Ceiling(damage - 5) + "s"));
            Assert.That(_column.SlotAt(0).Icon, Is.Not.Null, "o ícone do catálogo");
            Assert.That(_column.SlotAt(0).Fill, Is.EqualTo((float)((damage - 5) / damage)).Within(1e-3f), "o anel esvazia");
            Assert.That(_column.SlotAt(1).Fill, Is.EqualTo(1f).Within(1e-4f));
            Assert.That(_hud.IsDamageBoosted, Is.True, "D-216: o DANO em âmbar");
            Assert.That(_feedback.LastGainText, Is.Not.Null.And.Not.Empty, "a carta sobe do anão");

            Simulation.Tick(damage - 5 - 2);
            yield return null;
            Assert.That(_column.SlotAt(0).SecondsText, Is.EqualTo("2s"));
            Assert.That(_column.SlotAt(0).SecondsColor, Is.Not.EqualTo(_column.SlotAt(1).SecondsColor), "os últimos 5 s em âmbar");

            Simulation.Tick(2.01);
            yield return null;
            Assert.That(_column.ShownCount, Is.EqualTo(1), "o Dano saiu");
            Assert.That(_hud.IsDamageBoosted, Is.False, "a cor volta");

            Simulation.Tick(damage);
            yield return null;
            Assert.That(_column.ShownCount, Is.EqualTo(0), "as Lascas também");
        }

        [UnityTest]
        public IEnumerator EndingIcon_Blinks()
        {
            yield return Open();
            double seconds = GrantCard(RareCard.Damage);
            Simulation.Tick(seconds - 3);
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
            GrantCard(RareCard.Damage);
            double seconds = GrantCard(RareCard.Shards);
            // M9i E1: a coluna desceu para baixo do AJUSTES; na tela do batch (640×480, deitada) o ícone ficaria fora
            // da tela. O teste mede o toque no ícone, não o lugar da coluna: sobe ela para dentro da tela.
            var root = (RectTransform)_column.transform;
            root.anchoredPosition = new Vector2(root.anchoredPosition.x, -60f);
            yield return null;
            var shards = _column.SlotAt(1);

            var tapInput = Object.FindAnyObjectByType<TapInput>();
            var screen = RectTransformUtility.WorldToScreenPoint(null, shards.Rect.position);
            Assert.That(tapInput.TapAt(screen), Is.False, "o toque na coluna não vale no mapa (nem a faísca)");

            shards.Button.onClick.Invoke();
            Assert.That(_column.IsDetailOpen, Is.True);
            Assert.That(_column.DetailIndex, Is.EqualTo(1));
            Assert.That(_column.DetailTitleText, Is.Not.Empty);
            Assert.That(_column.DetailFooterText, Does.Contain(Math.Ceiling(seconds).ToString()));
            Assert.That(_runner.TimeScale, Is.EqualTo(0f), "não pausa nem muda o tempo");

            yield return new WaitForSecondsRealtime(BuffColumn.DetailSeconds + 0.2f);
            Assert.That(_column.IsDetailOpen, Is.False);
        }

        [UnityTest]
        public IEnumerator Reopening_BringsTheIconsBack_WithTheTimeLeft()
        {
            yield return Open();
            double seconds = GrantCard(RareCard.Damage);
            Simulation.Tick(4);
            Simulation.GrantRareCard(RareCard.Shards);
            _runner.SaveNow();

            yield return Open();
            Assert.That(_column.ShownCount, Is.EqualTo(2), "o Dano e as Lascas");
            Assert.That(_column.SlotAt(0).SecondsText, Is.EqualTo(Math.Ceiling(seconds - 4) + "s"));
            Assert.That(_column.SlotAt(1).SecondsText, Is.EqualTo(Math.Ceiling(seconds) + "s"));
        }
    }
}
