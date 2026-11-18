using System;
using System.Collections;
using System.IO;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Persistence;
using MinersIdle.UI;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MinersIdle.Tests
{
    /// <summary>
    /// M9e E3, na cena Game de verdade com o balanço do v8 (save numa pasta temporária, relógio do teste): o AD abre o menu
    /// do Boost (P6); ASSISTIR passa pelo anúncio simulado e dá o anúncio, que vira RENOVAR com o "falta"; na coluna, um
    /// ícone por anúncio com o selo "×4" e o tempo longo (P7); o "Raro tier 2" a partir do andar 10 e sumindo com o Raro
    /// Dourado, que tem a linha na tela do descanso (P4); e o "Bem-vindo de volta" com as horas extras e os raros.
    /// </summary>
    public class BoostFlowTests
    {
        const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        string _dir;
        DateTime _now;
        MiningRunner _runner;
        AdButtonView _button;
        BoostScreen _boost;
        SimulatedAdScreen _ad;
        BuffColumn _column;

        MiningSimulation Simulation => _runner.Simulation;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "MinersIdleBoostFlow_" + Guid.NewGuid().ToString("N"));
            _now = new DateTime(2026, 11, 18, 9, 0, 0, DateTimeKind.Utc);
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
            _button = Object.FindAnyObjectByType<AdButtonView>();
            _boost = Object.FindAnyObjectByType<BoostScreen>();
            _ad = Object.FindAnyObjectByType<SimulatedAdScreen>();
            _column = Object.FindAnyObjectByType<BuffColumn>();
            Assert.That(_boost, Is.Not.Null, "rode o setup do projeto (menu do Boost, M9e E3)");
            _runner.UseClock(() => _now);
            _runner.TimeScale = 0f; // o teste anda o tempo
            _runner.UseSave(new SaveService(new FileSaveStore(_dir)));
            if (!Simulation.Strike.Ads.IsEnabled) Assert.Ignore("O balanço ativo não tem os anúncios do Boost (GDD v8).");
            yield return null;
        }

        static Button WatchButton(BoostRowView row) => row.transform.Find("Watch").GetComponent<Button>();

        /// <summary>O anúncio simulado até o fim e o RECOMPENSA.</summary>
        IEnumerator FinishTheAd()
        {
            Assert.That(_ad.IsShowing, Is.True, "o anúncio abre");
            _ad.SkipWait();
            yield return null;
            _ad.Claim();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TheAdButton_OpensTheBoost_AndWatchingGivesTheAd()
        {
            yield return Open();
            Assert.That(_button.State, Is.EqualTo(AdButtonView.ButtonState.Free));
            _button.GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(_boost.IsOpen, Is.True, "o AD abre o menu do Boost (P6)");
            Assert.That(_ad.IsShowing, Is.False, "ainda sem anúncio");

            var shortRow = _boost.Row(AdKind.Short);
            Assert.That(shortRow.IsShown && _boost.Row(AdKind.Medium).IsShown && _boost.Row(AdKind.Big).IsShown, Is.True);
            Assert.That(_boost.Row(AdKind.RareTier2).IsShown, Is.False, "o do tier 2 só a partir do andar 10 (D-250)");
            Assert.That(shortRow.DurationText, Is.EqualTo("20 min"));
            Assert.That(shortRow.EffectText, Is.EqualTo("×2 dano, lascas e crítico"));
            Assert.That(shortRow.DetailText, Is.EqualTo("Crítico +10% · offline +1 h"));
            Assert.That(_boost.Row(AdKind.Big).DurationText, Is.EqualTo("1 h 20 min"));
            Assert.That(shortRow.ButtonText, Is.EqualTo("ASSISTIR"));
            Assert.That(shortRow.LeftText, Is.Empty);

            WatchButton(shortRow).onClick.Invoke();
            yield return null;
            Assert.That(_ad.IsShowing, Is.True);
            Assert.That(_ad.RewardText, Is.EqualTo("Recompensa: ×2 em dano, lascas e crítico por 20 min"));
            Assert.That(shortRow.CanPress, Is.False, "com o anúncio na tela, nada mais");
            yield return FinishTheAd();

            Assert.That(Simulation.AdSecondsLeft(AdKind.Short), Is.EqualTo(1200));
            Assert.That(_boost.IsOpen, Is.True, "o menu continua aberto embaixo");
            Assert.That(shortRow.ButtonText, Is.EqualTo("RENOVAR"), "D-244");
            Assert.That(shortRow.CanPress, Is.True);
            Assert.That(shortRow.LeftText, Is.EqualTo("falta 20m"));

            // A coluna: um ícone só para os quatro bônus do anúncio, com o selo e o tempo longo (P7).
            Assert.That(_column.ShownCount, Is.EqualTo(1));
            Assert.That(_column.SlotAt(0).BadgeText, Is.EqualTo("×2"));
            Assert.That(_column.SlotAt(0).SecondsText, Is.EqualTo("20m"));

            Assert.That(Simulation.WatchAd(AdKind.Big), Is.True);
            yield return null;
            Assert.That(_column.ShownCount, Is.EqualTo(2));
            Assert.That(_column.SlotAt(1).BadgeText, Is.EqualTo("×4"));
            Assert.That(_column.SlotAt(1).SecondsText, Is.EqualTo("1h20"));
            Assert.That(_boost.Row(AdKind.Big).LeftText, Is.EqualTo("falta 1h20"));
            Assert.That(Simulation.Bonuses.DamageMultiplier, Is.EqualTo(4), "vale o maior (D-251)");

            _column.ToggleDetail(1);
            yield return null;
            Assert.That(_column.DetailTitleText, Is.EqualTo("Anúncio grande"));
            Assert.That(_column.DetailFooterText, Is.EqualTo("Anúncio · falta 1h20"));

            Simulation.Tick(80 * 60 - 30);
            yield return null;
            Assert.That(_column.ShownCount, Is.EqualTo(1), "o curto acabou");
            Assert.That(_column.SlotAt(0).SecondsText, Is.EqualTo("30s"), "no último minuto, os segundos");
            Assert.That(shortRow.ButtonText, Is.EqualTo("ASSISTIR"));

            _boost.transform.Find("Screen/Panel/Back").GetComponent<Button>().onClick.Invoke();
            Assert.That(_boost.IsOpen, Is.False, "VOLTAR fecha");
        }

        [UnityTest]
        public IEnumerator TheTier2Ad_FromFloor10_AndTheGoldenRareOnTheRestScreen()
        {
            yield return Open();
            _runner.Rest(force: true);
            var rest = Object.FindAnyObjectByType<RestShopScreen>();
            PermanentRowView golden = null;
            foreach (var row in rest.Rows)
                if (row.Permanent == PermanentId.GoldenRare) golden = row;
            Assert.That(golden, Is.Not.Null, "a linha Raro Dourado (P4)");
            Simulation.AddRedShards(1000);
            Assert.That(golden.CanBuy, Is.False, "travada antes do andar 10");
            Assert.That(golden.EffectText, Is.EqualTo("Recorde no andar 10"));
            _runner.StartRun();
            yield return null;

            Simulation.SetDeepestFloor(10);
            _boost.Open();
            yield return null;
            var tier2 = _boost.Row(AdKind.RareTier2);
            Assert.That(tier2.IsShown, Is.True, "a partir do andar 10 (D-250)");
            Assert.That(tier2.EffectText, Is.EqualTo("Cartas do raro ×4 e +20%"));
            Assert.That(tier2.DetailText, Is.EqualTo("Some com o Raro Dourado"));
            Assert.That(tier2.DurationText, Is.EqualTo("1 h"));
            WatchButton(tier2).onClick.Invoke();
            yield return FinishTheAd();
            Assert.That(Simulation.RareTier, Is.EqualTo(2));
            Assert.That(_column.ShownCount, Is.EqualTo(1));
            Assert.That(_column.SlotAt(0).BadgeText, Is.Empty, "o do tier 2 não tem selo");
            _boost.Close();

            _runner.Rest(force: true);
            yield return null;
            Assert.That(golden.CanBuy, Is.True);
            Assert.That(golden.EffectText, Is.EqualTo("Cartas do raro ×2 » ×4"));
            Assert.That(Simulation.PurchasePermanent(PermanentId.GoldenRare), Is.EqualTo(PurchaseResult.Purchased));
            yield return null;
            Assert.That(golden.EffectText, Is.EqualTo("Cartas do raro ×4"));
            _runner.StartRun();
            yield return null;

            _boost.Open();
            yield return null;
            Assert.That(tier2.IsShown, Is.False, "comprado o Raro Dourado, o 4º anúncio some (D-250)");
        }

        [UnityTest]
        public IEnumerator TheWelcomeBack_ShowsTheExtraHours_AndTheRares()
        {
            yield return Open();
            var modal = Object.FindAnyObjectByType<OfflineReturnScreen>();
            var panel = Object.FindAnyObjectByType<DebugPanel>();
            Assert.That(Simulation.HireIntern(), Is.True);
            Simulation.WatchAd(AdKind.Big);
            panel.transform.Find("OfflineMenu/Content/Buttons/Away8H").GetComponent<Button>().onClick.Invoke();
            yield return null;

            var report = _runner.LastOffline.Value;
            Assert.That(report.MaxSeconds, Is.EqualTo(8 * 3600), "4 h + as 4 do grande");
            Assert.That(report.SecondsCounted, Is.EqualTo(8 * 3600));
            Assert.That(modal.IsOpen, Is.True);
            Assert.That(modal.ShowsAds, Is.True);
            Assert.That(modal.AdsText, Is.EqualTo("+4 h"));
            Assert.That(modal.ShowsCounted, Is.False, "8 h fora, 8 h de teto");
            Assert.That(modal.ShowsRares, Is.False, "antes do andar 10, nenhum raro");
            modal.Collect();

            Simulation.SkipToCave(12);
            panel.transform.Find("OfflineMenu/Content/Buttons/Away1H").GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.That(modal.IsOpen, Is.True);
            Assert.That(modal.ShowsAds, Is.False, "o grande acabou lá fora");
            Assert.That(modal.ShowsRares, Is.True);
            Assert.That(modal.RaresText, Is.EqualTo("60"), "um a cada 60 s (D-284)");
        }
    }
}
