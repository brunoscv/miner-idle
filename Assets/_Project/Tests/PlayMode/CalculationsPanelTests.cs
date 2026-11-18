using System;
using System.Collections;
using System.IO;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Persistence;
using MinersIdle.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M8 E3b-3 (D-143): DBG → Cálculos troca a taxa e o incremento da Picareta (mantém o progresso), tem Reiniciar run e
    /// Padrão GDD, e o aviso fixo na tela diz o cálculo em uso. Desde o v8 (M9c E1, D-256) o balanço é um só, o GDD v8: o
    /// seletor de balanço e a Picareta pela caverna saíram. A escolha volta ao abrir de novo pelo caminho do save; aberta
    /// direto (como aqui), a cena não grava nada.
    /// </summary>
    public class CalculationsPanelTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        DebugPanel _panel;
        MiningRunner _runner;
        TMP_Text _warning;
        string _dir;

        [SetUp]
        public void SetUp() => _dir = Path.Combine(Path.GetTempPath(), "MinersIdleCalcPanel_" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
        }

        IEnumerator Load()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            _panel = Object.FindAnyObjectByType<DebugPanel>();
            _runner = Object.FindAnyObjectByType<MiningRunner>();
            Assert.That(_panel.transform.Find("CalcMenu")?.GetComponent<CalculationsPanel>(), Is.Not.Null,
                        "menu Cálculos (rode o setup do projeto)");
            _warning = _panel.transform.Find("CalcWarning")?.GetComponentInChildren<TMP_Text>();
            Assert.That(_warning, Is.Not.Null, "aviso do cálculo na tela");
        }

        void Click(string name)
        {
            var target = _panel.transform.Find("CalcMenu/Content/Buttons/" + name);
            Assert.That(target, Is.Not.Null, "botão não encontrado: " + name);
            target.GetComponent<Button>().onClick.Invoke();
        }

        string Label(string name) => _panel.transform.Find("CalcMenu/Content/Buttons/" + name).GetComponentInChildren<TMP_Text>().text;

        [UnityTest]
        public IEnumerator Opens_FromTheDbg_AndStartsOnTheGdd()
        {
            yield return Load();
            _panel.transform.Find("Toggle").GetComponent<Button>().onClick.Invoke();
            _panel.transform.Find("Content/Buttons/Calc").GetComponent<Button>().onClick.Invoke();
            Assert.That(_panel.transform.Find("CalcMenu").GetComponent<DebugMenu>().IsOpen, Is.True, "Cálculos abre em cima");

            Assert.That(_runner.Calculation.IsDefault, Is.True);
            Assert.That(_runner.Rules.name, Is.EqualTo("Balance_GDD_v8"), "o jogo abre no GDD v8");
            Assert.That(_warning.text, Is.EqualTo("Cálculo: GDD v8"));
            Assert.That(_panel.transform.Find("CalcMenu/Content/Buttons/Balance"), Is.Null, "sem seletor de balanço (D-256)");
            Assert.That(Label("GrowthValue"), Is.EqualTo("Taxa 1,2"));
            Assert.That(Label("StepValue"), Is.EqualTo("Incremento 0"), "o Dano Base não sobe com a compra (D-202)");
            Assert.That(_warning.transform.IsChildOf(_panel.transform), Is.True, "o aviso é do DBG: some no release");

            yield return null; // o menu aberto atualiza a linha da Picareta
            var pickaxe = _panel.transform.Find("CalcMenu/Content/Pickaxe").GetComponent<TMP_Text>();
            Assert.That(pickaxe.text, Does.Contain("dano agora 2"), "Dano Base 1 + 1,2^0");
            Assert.That(pickaxe.text, Does.StartWith("Picareta nv 1 "), "nível 1 = sem compra (D-207)");
        }

        [UnityTest]
        public IEnumerator Pickaxe_GrowthAndStep_KeepTheProgress_AndShowInTheWarning()
        {
            yield return Load();
            _runner.Simulation.SkipToCave(10);
            _runner.Simulation.AddShards(1000);
            Assert.That(_runner.Simulation.Purchase(UpgradeId.Pickaxe), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(_runner.Simulation.Purchase(UpgradeId.Pickaxe), Is.EqualTo(PurchaseResult.Purchased));
            double shards = _runner.Simulation.State.Shards;
            Color normal = _warning.color;

            Click("GrowthPlus");
            Click("GrowthPlus");
            Assert.That(_runner.Calculation.PickaxeCaveGrowth, Is.EqualTo(1.22).Within(1e-9));
            Assert.That(_runner.Simulation.State.Cave, Is.EqualTo(10), "a taxa não recomeça a run");
            Assert.That(_runner.Simulation.State.Shards, Is.EqualTo(shards));
            Assert.That(_runner.Simulation.Stats.Damage, Is.EqualTo(1 + Math.Pow(1.22, 2)).Within(1e-9),
                        "Picareta nível 3 (D-202): Dano Base 1 + 1,22^(3 − 1); a caverna não entra");
            Assert.That(_runner.Simulation.Preview(UpgradeId.Pickaxe).NextCost, Is.EqualTo(7 * (1 + Math.Pow(1.22, 2))).Within(1e-9),
                        "o custo acompanha a taxa: 7 × o dano (D-202, P5)");
            Assert.That(Label("GrowthValue"), Is.EqualTo("Taxa 1,22"));
            Assert.That(_warning.text, Is.EqualTo("Cálculo: GDD v8 · Picareta taxa 1,22"));
            Assert.That(_warning.color, Is.Not.EqualTo(normal), "fora do GDD, em âmbar");

            Click("StepPlus");
            Assert.That(_runner.Calculation.PickaxeStep, Is.EqualTo(1));
            double before = _runner.Simulation.Stats.Damage;
            Assert.That(_runner.Simulation.Purchase(UpgradeId.Pickaxe), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(_runner.Simulation.Stats.Damage - before, Is.EqualTo(1 + Math.Pow(1.22, 3) - Math.Pow(1.22, 2)).Within(1e-9),
                        "a compra soma 1 no Dano Base e sobe a Picareta para o nível 4");
            Assert.That(_runner.Simulation.Preview(UpgradeId.Pickaxe).NextCost, Is.EqualTo(7 * (1 + 1 * 3 + Math.Pow(1.22, 3))).Within(1e-9),
                        "3 compras com incremento 1: 7 × (Dano Base 4 + 1,22^3)");
            Assert.That(_warning.text, Is.EqualTo("Cálculo: GDD v8 · Picareta taxa 1,22 · Picareta incremento 1"));

            Click("GrowthValue"); // tocar no valor volta ao GDD
            Click("StepMinus");
            Assert.That(_runner.Calculation.IsDefault, Is.True, "de volta aos números do GDD, não conta como troca");
            Assert.That(_warning.text, Is.EqualTo("Cálculo: GDD v8"));
            Assert.That(_warning.color, Is.EqualTo(normal));
            Assert.That(_runner.Simulation.State.LevelOf(UpgradeId.Pickaxe), Is.EqualTo(3), "as compras ficaram");
        }

        /// <summary>
        /// D-201 → D-256: o botão "Picareta" (compra × caverna) saiu do DBG; a Picareta é sempre pela compra, e o andar não
        /// entra no dano.
        /// </summary>
        [UnityTest]
        public IEnumerator PickaxeButton_IsGone_AndThePickaxeIsAlwaysByPurchase()
        {
            yield return Load();
            Assert.That(_panel.transform.Find("CaveMenu/Content/Buttons/PickaxeByCave"), Is.Null,
                        "sem o botão Picareta no DBG → Andar (rode o setup do projeto)");

            _runner.Simulation.SkipToCave(10);
            _runner.Simulation.AddShards(100);
            Assert.That(_runner.Simulation.Purchase(UpgradeId.Pickaxe), Is.EqualTo(PurchaseResult.Purchased));
            Assert.That(_runner.Simulation.Stats.Damage, Is.EqualTo(1 + 1.2).Within(1e-9),
                        "pela compra: Dano Base 1 + 1,2^1 (D-202); o andar 10 não entra");
        }

        /// <summary>M9g (D-311): o botão "Acúmulo" (os marcos do combo, D-217) saiu do DBG → Cálculos com o combo.</summary>
        [UnityTest]
        public IEnumerator ComboStackingButton_IsGone()
        {
            yield return Load();
            Assert.That(_panel.transform.Find("CalcMenu/Content/Buttons/ComboStacking"), Is.Null,
                        "sem o botão Acúmulo no DBG → Cálculos (rode o setup do projeto)");
        }

        /// <summary>D-256: sem o seletor, Padrão GDD não troca de balanço nem recomeça a run; a taxa da Picareta vale no v8.</summary>
        [UnityTest]
        public IEnumerator ResetDefaults_StaysOnTheGddV8_AndKeepsTheRun()
        {
            yield return Load();
            var rules = _runner.Rules;
            _runner.Simulation.SkipToCave(5);
            _runner.Simulation.AddShards(500);
            Assert.That(_panel.transform.Find("CalcMenu/Content/Buttons/GrowthPlus").GetComponent<Button>().interactable, Is.True,
                        "no v8 a Picareta tem taxa e incremento");

            Click("GrowthPlus");
            Click("ResetDefaults");
            Assert.That(_runner.Calculation.IsDefault, Is.True);
            Assert.That(_runner.Rules, Is.SameAs(rules), "o mesmo balanço");
            Assert.That(_runner.Simulation.Rules.CaveCount, Is.EqualTo(100));
            Assert.That(_runner.Simulation.State.Cave, Is.EqualTo(5), "não recomeça a run");
            Assert.That(_runner.Simulation.State.Shards, Is.EqualTo(500));
            Assert.That(_warning.text, Is.EqualTo("Cálculo: GDD v8"));
        }

        [UnityTest]
        public IEnumerator RestartRun_GoesBackToLevelOne_WithNothing()
        {
            yield return Load();
            var simulation = _runner.Simulation;
            simulation.AddShards(1000);
            simulation.Purchase(UpgradeId.Pickaxe);
            simulation.SkipToCave(7);
            Click("GrowthPlus");

            Click("RestartRun");
            Assert.That(_runner.Simulation, Is.Not.SameAs(simulation), "fila nova");
            Assert.That(_runner.Simulation.State.Cave, Is.EqualTo(1));
            Assert.That(_runner.Simulation.State.Shards, Is.EqualTo(0));
            Assert.That(_runner.Simulation.State.LevelOf(UpgradeId.Pickaxe), Is.EqualTo(0), "melhorias base no zero");
            Assert.That(_runner.Calculation.PickaxeCaveGrowth, Is.EqualTo(1.21).Within(1e-9), "o cálculo escolhido fica");
        }

        /// <summary>A escolha fica no aparelho (arquivo à parte do save) e volta ao abrir de novo, já com o progresso nela.</summary>
        [UnityTest]
        public IEnumerator Choice_SurvivesReopening_ThroughTheSavePath()
        {
            yield return Load();
            var calculations = new CalculationStore(new FileSaveStore(_dir, CalculationStore.FileName));
            _runner.UseSave(new SaveService(new FileSaveStore(_dir)), calculations);
            Click("GrowthPlus");
            _runner.Simulation.AddShards(42);
            _runner.SaveNow();
            Assert.That(calculations.Load()?.pickaxeCaveGrowth, Is.EqualTo(1.21).Within(1e-9), "gravou a escolha");

            yield return Load();
            Assert.That(_runner.Calculation.IsDefault, Is.True, "aberta direto, a cena não lê a escolha");
            _runner.UseSave(new SaveService(new FileSaveStore(_dir)),
                            new CalculationStore(new FileSaveStore(_dir, CalculationStore.FileName)));
            Assert.That(_runner.Calculation.PickaxeCaveGrowth, Is.EqualTo(1.21).Within(1e-9), "reabre com a taxa escolhida");
            Assert.That(_runner.Simulation.State.Shards, Is.EqualTo(42), "com o progresso jogado nela");
            Assert.That(_warning.text, Is.EqualTo("Cálculo: GDD v8 · Picareta taxa 1,21"));
        }
    }
}
