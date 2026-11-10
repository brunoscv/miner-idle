using System;
using System.Collections;
using System.IO;
using System.Linq;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Persistence;
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
    /// M9b, E1, na cena Game de verdade (save numa pasta temporária, relógio do teste): o DBG → Bônus dá os bônus de um
    /// combo e os lista; tocar até ×35 e minimizar termina o combo na hora (D-214); o tempo corre fora; fechar e abrir traz
    /// os bônus com o tempo que faltava.
    /// </summary>
    public class ComboBonusFlowTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        string _dir;
        DateTime _now;
        MiningRunner _runner;
        BonusDebugPanel _panel;

        MiningSimulation Simulation => _runner.Simulation;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "MinersIdleBonusFlow_" + Guid.NewGuid().ToString("N"));
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
            _panel = Object.FindAnyObjectByType<BonusDebugPanel>();
            Assert.That(_panel, Is.Not.Null, "rode o setup do projeto (DBG → Bônus)");
            _runner.UseClock(() => _now);
            _runner.TimeScale = 0f; // o teste anda o tempo
            _runner.UseSave(new SaveService(new FileSaveStore(_dir)));
            if (Simulation.Strike.ComboBonuses.Count == 0)
                Assert.Ignore("O balanço ativo não tem os marcos do combo (rode o setup: GDD v7).");
        }

        [UnityTest]
        public IEnumerator DebugShortcut_GivesTheTiers_AndTheMenuListsThem()
        {
            yield return Open();
            double damage = Simulation.Stats.Damage;

            _panel.Grant(Array.IndexOf(BonusDebugPanel.Combos, 30));
            Assert.That(Simulation.Bonuses.All.Select(b => b.Tier), Is.EquivalentTo(new[] { 10, 30 }));
            Assert.That(Simulation.Stats.Damage, Is.EqualTo(damage * 1.25).Within(1e-9 * damage));

            var debug = Object.FindAnyObjectByType<DebugPanel>();
            debug.transform.Find("Content/Buttons/Bonus").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That(_panel.SummaryText, Does.Contain("+10%").And.Contain("+15%").And.Contain("+25%"));

            Simulation.Tick(5);
            Assert.That(Simulation.Bonuses.All.Single().Tier, Is.EqualTo(30), "aos 5 s fica só o +15%");
            Simulation.Tick(5);
            Assert.That(Simulation.Bonuses.Count, Is.EqualTo(0));

            _panel.Grant(Array.IndexOf(BonusDebugPanel.Combos, 100));
            _panel.Clear();
            Assert.That(Simulation.Bonuses.Count, Is.EqualTo(0));
            Assert.That(Simulation.Stats.Damage, Is.EqualTo(damage).Within(1e-9 * damage));
        }

        [UnityTest]
        public IEnumerator Minimizing_EndsTheCombo_TheTimeRunsOutside_AndReopeningKeepsWhatWasLeft()
        {
            yield return Open();
            for (int i = 0; i < 35; i++)
            {
                Simulation.Tap();
                Simulation.Tick(0.1); // 10 toques por segundo: abaixo do teto (D-119), dentro da janela
            }
            Assert.That(Simulation.Combo, Is.EqualTo(35));

            _runner.SendMessage("OnApplicationPause", true);
            Assert.That(Simulation.Combo, Is.EqualTo(0), "D-214: ir para o fundo termina o combo");
            Assert.That(Simulation.Bonuses.All.Select(b => b.Tier), Is.EquivalentTo(new[] { 10, 30 }));

            _now = _now.AddSeconds(4);
            _runner.SendMessage("OnApplicationPause", false);
            Assert.That(Simulation.Bonuses.All.Single(b => b.Tier == 30).SecondsLeft, Is.EqualTo(6).Within(1e-6),
                        "os 4 s fora contaram");

            double left = Simulation.Bonuses.All.Single(b => b.Tier == 10).SecondsLeft;
            _runner.SaveNow();
            yield return Open(); // fecha e abre no mesmo instante
            Assert.That(Simulation.Bonuses.All.Single(b => b.Tier == 10).SecondsLeft, Is.EqualTo(left).Within(1e-6));
            Assert.That(Simulation.Bonuses.DamagePercent, Is.EqualTo(0.25).Within(1e-12));
        }
    }
}
