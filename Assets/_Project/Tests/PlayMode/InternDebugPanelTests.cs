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
    /// E1 do M7: o DBG → Estagiário na cena Game (GDD v7). Contratar, +1 nível nas melhorias dele, Treinar, Promoção e
    /// Demitir mandam os comandos à simulação, e o resumo no alto mostra o estagiário. Com a Promoção, o descanso já
    /// começa a run nova com ele.
    /// </summary>
    public class InternDebugPanelTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        DebugPanel _panel;
        MiningRunner _runner;

        MiningSimulation Simulation => _runner.Simulation;

        IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _panel = Object.FindAnyObjectByType<DebugPanel>();
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            Assert.That(_panel.transform.Find("InternMenu")?.GetComponent<InternDebugPanel>(), Is.Not.Null,
                        "menu Estagiário (rode o setup do projeto)");
            if (!Simulation.Economy.HasIntern) Assert.Ignore("O balanço ativo não tem estagiário (rode o setup: GDD v7).");
        }

        void Click(string name)
        {
            var target = _panel.transform.Find("InternMenu/Content/Buttons/" + name);
            Assert.That(target, Is.Not.Null, "botão não encontrado: " + name);
            target.GetComponent<Button>().onClick.Invoke();
        }

        string Summary => _panel.transform.Find("InternMenu/Content/Summary").GetComponent<TMP_Text>().text;

        [UnityTest]
        public IEnumerator Opens_FromTheDbg_WithTheSummary()
        {
            yield return Load();
            _panel.transform.Find("Toggle").GetComponent<Button>().onClick.Invoke();
            _panel.transform.Find("Content/Buttons/Intern").GetComponent<Button>().onClick.Invoke();
            Assert.That(_panel.transform.Find("InternMenu").GetComponent<DebugMenu>().IsOpen, Is.True);
            Assert.That(Summary, Does.StartWith("Estagiário: não contratado · dano 1 · intervalo 2,00s"));

            Click("Hire");
            Assert.That(Simulation.IsInternHired, Is.True);
            Assert.That(Summary, Does.StartWith("Estagiário: contratado"));
        }

        [UnityTest]
        public IEnumerator GrantLevels_Train_AndFire()
        {
            yield return Load();
            Click("GrantInternLevels"); // contrata antes, se preciso
            Assert.That(Simulation.IsInternHired, Is.True);
            Assert.That(Simulation.InternStats.Damage, Is.EqualTo(5), "Picareta 1: 5");
            Assert.That(Simulation.InternStats.HitInterval, Is.EqualTo(1.9).Within(1e-9), "Velocidade 1: 1,9 s");
            Assert.That(Summary, Does.Contain("dano 5 · intervalo 1,90s"));

            Click("Train");
            Assert.That(Simulation.Permanents.State.LevelOf(PermanentId.InternTraining), Is.EqualTo(1));
            Assert.That(Simulation.InternStats.Damage, Is.EqualTo(5.5).Within(1e-9), "+10% na hora");

            Click("Fire");
            Assert.That(Simulation.IsInternHired, Is.False);
            Assert.That(Simulation.State.LevelOf(UpgradeId.InternPickaxe), Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Promotion_StartsTheNextRun_WithTheIntern()
        {
            yield return Load();
            Click("Promote");
            Assert.That(Simulation.Permanents.State.LevelOf(PermanentId.InternTraining), Is.EqualTo(10));
            Assert.That(Simulation.Permanents.State.LevelOf(PermanentId.InternPromotion), Is.EqualTo(1));
            Assert.That(Simulation.IsInternHired, Is.False, "a Promoção vale na run seguinte");

            _runner.Rest(force: true);
            Assert.That(Simulation.IsInternHired, Is.True, "a run nova já começa com ele (D-169)");
            Assert.That(Simulation.InternStats.Damage, Is.EqualTo(2).Within(1e-9), "Dano Base 1 × Treinar 10");

            // Ele bate no cristal do anão com o relógio dele: em ×20, alguns golpes logo. No nível 1 o anão quebra cada
            // cristal com um golpe (e bate primeiro, no mesmo instante): no 30 o cristal aguenta vários.
            Simulation.SkipToCave(30);
            long hits = Simulation.InternHits;
            _runner.TimeScale = 20f;
            float waited = 0f;
            while (Simulation.InternHits == hits && waited < 3f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            _runner.TimeScale = 1f;
            Assert.That(Simulation.InternHits, Is.GreaterThan(hits), "o estagiário bate");
        }
    }
}
