using System.Collections;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace MinersIdle.Tests
{
    /// <summary>
    /// E1 do Marco 8b (D-159): o DBG → Dia e o Descansar do <see cref="MiningRunner"/> na cena Game (GDD v7): as lascas
    /// vermelhas vão para o banco, o dia avança e a run recomeça do nível 1 com as permanentes valendo.
    /// </summary>
    public class DayDebugPanelTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        DebugPanel _panel;
        MiningRunner _runner;

        IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _panel = Object.FindAnyObjectByType<DebugPanel>();
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            Assert.That(_panel.transform.Find("DayMenu")?.GetComponent<DayDebugPanel>(), Is.Not.Null,
                        "menu Dia (rode o setup do projeto)");
        }

        void Click(string name)
        {
            var target = _panel.transform.Find("DayMenu/Content/Buttons/" + name);
            Assert.That(target, Is.Not.Null, "botão não encontrado: " + name);
            target.GetComponent<Button>().onClick.Invoke();
        }

        [UnityTest]
        public IEnumerator Opens_FromTheDbg_WithTheSummary()
        {
            yield return Load();
            _panel.transform.Find("Toggle").GetComponent<Button>().onClick.Invoke();
            _panel.transform.Find("Content/Buttons/Day").GetComponent<Button>().onClick.Invoke();
            Assert.That(_panel.transform.Find("DayMenu").GetComponent<DebugMenu>().IsOpen, Is.True);

            _runner.Simulation.SkipToCave(12);
            yield return new WaitForSecondsRealtime(0.6f); // aberto, o menu atualiza a cada 0,5 s
            var summary = _panel.transform.Find("DayMenu/Content/Summary").GetComponent<TMP_Text>();
            Assert.That(summary.text, Does.StartWith("Dia 1 · banco 0 · leva agora 7 (nível 12)"));
        }

        [UnityTest]
        public IEnumerator RestNow_TakesTheRedShards_AndStartsTheNextDay()
        {
            yield return Load();
            var permanent = _runner.Simulation.Permanents.State;
            _runner.Simulation.AddShards(1000);
            _runner.Simulation.Purchase(UpgradeId.Pickaxe);
            _runner.Simulation.SkipToCave(12);
            RestSummary? rested = null;
            bool startedFirst = false;
            _runner.SimulationStarted += _ => startedFirst = rested == null;
            _runner.Rested += s => rested = s;

            Click("RestNow");

            Assert.That(rested?.RedShardsGained, Is.EqualTo(7));
            Assert.That(startedFirst, Is.True, "a run nova já está de pé quando o resumo sai");
            Assert.That(_runner.Simulation.State.Cave, Is.EqualTo(1));
            Assert.That(_runner.Simulation.State.Shards, Is.EqualTo(0));
            Assert.That(_runner.Simulation.State.LevelOf(UpgradeId.Pickaxe), Is.EqualTo(0));
            Assert.That(_runner.Simulation.Permanents.State, Is.SameAs(permanent), "as permanentes passam para a run nova");
            Assert.That(permanent.RedShards, Is.EqualTo(7));
            Assert.That(permanent.Day, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator Shortcuts_GiveRedShards_AndLevels_ThatLastIntoTheNextRun()
        {
            yield return Load();
            double damage = _runner.Simulation.Stats.Damage;

            Click("AddRed100");
            Click("AddRed10");
            Assert.That(_runner.Simulation.Permanents.State.RedShards, Is.EqualTo(110));
            Click("GrantPermanents");
            Assert.That(_runner.Simulation.Stats.Damage, Is.EqualTo(damage * 1.1).Within(1e-9), "Sangue da Montanha 1");
            Assert.That(_runner.Simulation.MapLock, Is.EqualTo(90), "Memória 1");

            Click("RestNow");
            Assert.That(_runner.Simulation.State.LevelOf(UpgradeId.Pickaxe), Is.EqualTo(1), "Alma dos Anões 1");
            Assert.That(_runner.Simulation.MapLock, Is.EqualTo(90));

            Click("ResetPermanents");
            Assert.That(_runner.Simulation.Permanents.State.RedShards, Is.EqualTo(0));
            Assert.That(_runner.Simulation.Permanents.State.Day, Is.EqualTo(1));
            Assert.That(_runner.Simulation.MapLock, Is.EqualTo(100));
        }
    }
}
