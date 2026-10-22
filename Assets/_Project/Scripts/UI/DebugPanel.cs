using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// Painel de debug do Marco 2 (E5): tempo ×1/×5/×20, gerar cristal, quebrar o alvo, +100 lascas, reiniciar com a
    /// mesma semente ou com uma nova, "Mostrar ordem" e estatísticas. Desde o M3 (E3, D-37): comprar cada melhoria e ver
    /// nível e próximo custo. Desde o M4 (E3): salvar agora e apagar o save; "Reiniciar" mantém o progresso. Desde o M5
    /// (E2): toques automáticos e crítico sempre; tremor on/off (E3), som on/off (E4) e a medição de desempenho
    /// (<see cref="FrameStats"/>, E5), que só conta com o painel fechado. Os atalhos chamam os mesmos comandos das regras.
    /// Só existe no Editor e em Development Build: no release o objeto se destrói ao acordar.
    /// Os textos (rótulos e o modelo das estatísticas) ficam na cena.
    /// </summary>
    public sealed class DebugPanel : MonoBehaviour
    {
        public const double ShardsToAdd = 100;
        const float StatsRefreshSeconds = 0.5f;

        [SerializeField] MiningRunner runner;
        [SerializeField] CrystalFieldView crystals;
        [SerializeField] TargetLineView targetLine;
        [SerializeField] TapInput tapInput;
        [SerializeField] ScreenShake screenShake;
        [SerializeField] SoundBoard soundBoard;
        [SerializeField] FrameStats frameStats;

        [SerializeField] Button toggleButton;
        [SerializeField] GameObject content;
        [SerializeField] TMP_Text stats;
        [Tooltip("{0} semente · {1} escala do tempo · {2} minutos de jogo · {3} golpes · {4} quebras · {5} lascas/min · " +
                 "{6} cristais na tela · {7} máximo · {8} alvo (id) · {9} lote do alvo")]
        [SerializeField, TextArea(3, 8)] string statsFormat;
        [SerializeField] TMP_Text upgrades;
        [Tooltip("Pares nível/próximo custo, na ordem: {0}/{1} Picareta · {2}/{3} Velocidade · {4}/{5} Mineração · " +
                 "{6}/{7} Picareta Dupla · {8}/{9} Botas")]
        [SerializeField, TextArea(2, 6)] string upgradesFormat;
        [Tooltip("Mostrado no lugar do próximo custo quando a linha está no limite.")]
        [SerializeField] string maxedLabel;

        [SerializeField] Button speed1;
        [SerializeField] Button speed5;
        [SerializeField] Button speed20;
        [SerializeField] Button spawnCrystal;
        [SerializeField] Button breakTarget;
        [SerializeField] Button addShards;
        [SerializeField] Button restartSameSeed;
        [SerializeField] Button restartNewSeed;
        [SerializeField] Button showOrder;
        [SerializeField] Button buyPickaxe;
        [SerializeField] Button buySpeed;
        [SerializeField] Button buyMining;
        [SerializeField] Button buyDoublePickaxe;
        [SerializeField] Button buyBoots;
        [SerializeField] Button saveNow;
        [SerializeField] Button deleteSave;
        [SerializeField] Button autoTap;
        [SerializeField] Button forceCritical;
        [SerializeField] Button shake;
        [SerializeField] Button sound;
        [SerializeField] Button resetStats;

        readonly object[] _upgradeArgs = new object[10];

        float _refresh;

        public bool IsOpen => content.activeSelf;

        void Awake()
        {
            if (!Debug.isDebugBuild)
            {
                gameObject.SetActive(false); // evita o OnEnable antes da destruição
                Destroy(gameObject);
                return;
            }

            content.SetActive(false);
        }

        void OnEnable()
        {
            toggleButton.onClick.AddListener(Toggle);
            speed1.onClick.AddListener(SetSpeed1);
            speed5.onClick.AddListener(SetSpeed5);
            speed20.onClick.AddListener(SetSpeed20);
            spawnCrystal.onClick.AddListener(SpawnCrystal);
            breakTarget.onClick.AddListener(BreakTarget);
            addShards.onClick.AddListener(AddShards);
            restartSameSeed.onClick.AddListener(RestartSameSeed);
            restartNewSeed.onClick.AddListener(RestartNewSeed);
            showOrder.onClick.AddListener(ToggleOrder);
            buyPickaxe.onClick.AddListener(BuyPickaxe);
            buySpeed.onClick.AddListener(BuySpeed);
            buyMining.onClick.AddListener(BuyMining);
            buyDoublePickaxe.onClick.AddListener(BuyDoublePickaxe);
            buyBoots.onClick.AddListener(BuyBoots);
            saveNow.onClick.AddListener(SaveNow);
            deleteSave.onClick.AddListener(DeleteSave);
            autoTap.onClick.AddListener(ToggleAutoTap);
            forceCritical.onClick.AddListener(ToggleForceCritical);
            shake.onClick.AddListener(ToggleShake);
            sound.onClick.AddListener(ToggleSound);
            resetStats.onClick.AddListener(ResetStats);
        }

        void OnDisable()
        {
            toggleButton.onClick.RemoveListener(Toggle);
            speed1.onClick.RemoveListener(SetSpeed1);
            speed5.onClick.RemoveListener(SetSpeed5);
            speed20.onClick.RemoveListener(SetSpeed20);
            spawnCrystal.onClick.RemoveListener(SpawnCrystal);
            breakTarget.onClick.RemoveListener(BreakTarget);
            addShards.onClick.RemoveListener(AddShards);
            restartSameSeed.onClick.RemoveListener(RestartSameSeed);
            restartNewSeed.onClick.RemoveListener(RestartNewSeed);
            showOrder.onClick.RemoveListener(ToggleOrder);
            buyPickaxe.onClick.RemoveListener(BuyPickaxe);
            buySpeed.onClick.RemoveListener(BuySpeed);
            buyMining.onClick.RemoveListener(BuyMining);
            buyDoublePickaxe.onClick.RemoveListener(BuyDoublePickaxe);
            buyBoots.onClick.RemoveListener(BuyBoots);
            saveNow.onClick.RemoveListener(SaveNow);
            deleteSave.onClick.RemoveListener(DeleteSave);
            autoTap.onClick.RemoveListener(ToggleAutoTap);
            forceCritical.onClick.RemoveListener(ToggleForceCritical);
            shake.onClick.RemoveListener(ToggleShake);
            sound.onClick.RemoveListener(ToggleSound);
            resetStats.onClick.RemoveListener(ResetStats);
        }

        void Update()
        {
            if (!content.activeSelf) return;

            _refresh -= Time.unscaledDeltaTime;
            if (_refresh > 0f) return;
            _refresh = StatsRefreshSeconds;
            RefreshStats();
        }

        void Toggle()
        {
            content.SetActive(!content.activeSelf);
            _refresh = 0f;
            if (frameStats != null) frameStats.Measuring = !content.activeSelf; // o painel aberto não conta como jogo
        }

        void SetSpeed1() => runner.TimeScale = 1f;
        void SetSpeed5() => runner.TimeScale = 5f;
        void SetSpeed20() => runner.TimeScale = 20f;
        void SpawnCrystal() => runner.Simulation?.SpawnCrystalNow();
        void BreakTarget() => runner.Simulation?.BreakTarget();
        void AddShards() => runner.Simulation?.AddShards(ShardsToAdd);
        void RestartSameSeed() => runner.StartNew(runner.Seed);
        void RestartNewSeed() => runner.StartNew(MiningRunner.NewSeed());

        void SaveNow() => runner.SaveNow();
        void DeleteSave() => runner.ResetProgress();
        void ToggleAutoTap() => tapInput.AutoTap = !tapInput.AutoTap;
        void ToggleForceCritical() => runner.ForceCritical = !runner.ForceCritical;
        void ToggleShake() => screenShake.ShakeEnabled = !screenShake.ShakeEnabled;
        void ToggleSound() => soundBoard.SoundEnabled = !soundBoard.SoundEnabled;
        void ResetStats() => frameStats.ResetCounts();

        void BuyPickaxe() => Buy(UpgradeId.Pickaxe);
        void BuySpeed() => Buy(UpgradeId.Speed);
        void BuyMining() => Buy(UpgradeId.Mining);
        void BuyDoublePickaxe() => Buy(UpgradeId.DoublePickaxe);
        void BuyBoots() => Buy(UpgradeId.Boots);

        void Buy(UpgradeId id)
        {
            runner.Simulation?.Purchase(id);
            _refresh = 0f; // mostra o nível e o custo novos já no próximo quadro
        }

        void ToggleOrder()
        {
            crystals.ShowOrder = !crystals.ShowOrder;
            targetLine.Visible = crystals.ShowOrder;
        }

        void RefreshStats()
        {
            var simulation = runner.Simulation;
            if (simulation == null) return;

            var state = simulation.State;
            double minutes = simulation.Elapsed / 60;
            var target = simulation.Miner.Target;
            stats.text = string.Format(statsFormat,
                runner.Seed,
                NumberFormatter.Format(runner.TimeScale),
                NumberFormatter.Format(minutes),
                NumberFormatter.Format(state.Hits),
                NumberFormatter.Format(state.Breaks),
                NumberFormatter.Format(minutes > 0 ? state.Shards / minutes : 0),
                simulation.Crystals.Count,
                simulation.Rules.MaxCrystals,
                target != null ? target.Id.ToString() : "-",
                target != null ? target.Batch.ToString() : "-");

            var economy = simulation.Economy;
            int i = 0;
            foreach (UpgradeId id in System.Enum.GetValues(typeof(UpgradeId)))
            {
                double? cost = Purchases.NextCost(state, economy, id);
                _upgradeArgs[i++] = state.LevelOf(id);
                _upgradeArgs[i++] = cost.HasValue ? NumberFormatter.Format(cost.Value) : maxedLabel;
            }
            upgrades.text = string.Format(upgradesFormat, _upgradeArgs);
        }
    }
}
