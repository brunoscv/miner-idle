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
    /// mesma semente ou com uma nova e estatísticas. Desde o M3 (E3, D-37): comprar cada melhoria e ver
    /// nível e próximo custo. Desde o M4 (E3): salvar agora e apagar o save; "Reiniciar" mantém o progresso. Desde o M5
    /// (E2): toques automáticos e crítico sempre (desde 2026-11-01, o diagnóstico do toque nas estatísticas, D-118); tremor on/off (E3), som on/off (E4) e a medição de desempenho
    /// (<see cref="FrameStats"/>, E5), que só conta com o painel fechado. Desde o M6 (E3): "Câmera" abre os controles da
    /// câmera da descida (<see cref="DescentCameraPanel"/>) no lugar do "Mostrar ordem" do mapa. Os atalhos chamam os mesmos comandos das regras.
    /// Desde 2026-11-05 (D-147) os atalhos ficam em menus que abrem em cima, como a câmera: Loja (lascas, cristais, compras,
    /// níveis), Efeitos (toques, crítico, tremor, som) e Caverna (quebrar, próxima, salvar, reiniciar, apagar, medição); no
    /// DBG ficam o tempo, as estatísticas e os botões dos menus. Um menu aberto fecha os outros e a câmera. Desde a E3b-3 do
    /// M8 (D-143), o menu Cálculos (<see cref="CalculationsPanel"/>).
    /// Só existe no Editor e em Development Build: no release o objeto se destrói ao acordar.
    /// Os textos (rótulos e o modelo das estatísticas) ficam na cena.
    /// </summary>
    public sealed class DebugPanel : MonoBehaviour
    {
        public const double ShardsToAdd = 100;
        const float StatsRefreshSeconds = 0.5f;

        [SerializeField] MiningRunner runner;
        [SerializeField] TapInput tapInput;
        [SerializeField] ScreenShake screenShake;
        [SerializeField] SoundBoard soundBoard;
        [SerializeField] FrameStats frameStats;

        [SerializeField] Button toggleButton;
        [SerializeField] GameObject content;
        [SerializeField] TMP_Text stats;
        [Tooltip("{0} semente · {1} escala do tempo · {2} minutos de jogo · {3} golpes · {4} quebras · {5} lascas/min · " +
                 "{6} cristais na fila · {7} máximo · {8} alvo (id) · {9} distância descida (un) · toques no último " +
                 "segundo (D-118, D-119): {10} valeram · {11} acima do teto · {12} começos do aparelho · " +
                 "{13} começos repetidos · {14} dedos na tela agora · {15} dedos que encostaram com outro na tela (D-123)")]
        [SerializeField, TextArea(3, 8)] string statsFormat;
        [SerializeField] TMP_Text upgrades;
        [Tooltip("Pares nível/próximo custo, na ordem de UpgradeId: {0}/{1} Picareta · {2}/{3} Velocidade · {4}/{5} Mineração · " +
                 "{6}/{7} Picareta Dupla · {8}/{9} Botas · {10}/{11} Mapa · {12}/{13} Crítico")]
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
        [SerializeField] Button cameraButton;
        [SerializeField] Button buyPickaxe;
        [SerializeField] Button buySpeed;
        [SerializeField] Button buyMining;
        [Tooltip("E3b do M8: no lugar do antigo Comprar Botas (linha desligada desde o GDD v5).")]
        [SerializeField] Button buyCritical;
        [SerializeField] Button saveNow;
        [SerializeField] Button deleteSave;
        [SerializeField] Button autoTap;
        [SerializeField] Button forceCritical;
        [SerializeField] Button shake;
        [SerializeField] Button sound;
        [SerializeField] Button resetStats;
        [Tooltip("M8: a trava do Letrado cumprida (os 100 cristais da caverna).")]
        [SerializeField] Button fillCaveBreaks;
        [Tooltip("M8: a próxima caverna agora, com o Letrado dela de graça.")]
        [SerializeField] Button nextCave;
        [Tooltip("M8 (E3): a última caverna, com os 100 cristais e as lascas do último Letrado (para testar o fim).")]
        [SerializeField] Button lastCave;
        [Tooltip("M8 (E3b-2): dez níveis abaixo de uma vez, com os Mapas de graça (para ver a troca de bioma).")]
        [SerializeField] Button tenLevels;
        [SerializeField] DescentCameraPanel cameraPanel;

        [Header("Menus (D-147)")]
        [SerializeField] Button shopButton;
        [SerializeField] DebugMenu shopMenu;
        [SerializeField] Button effectsButton;
        [SerializeField] DebugMenu effectsMenu;
        [SerializeField] Button caveButton;
        [SerializeField] DebugMenu caveMenu;
        [Tooltip("DBG → Cálculos (M8 E3b-3, D-143): balanço, taxa e incremento da Picareta, Reiniciar run.")]
        [SerializeField] Button calcButton;
        [SerializeField] DebugMenu calcMenu;
        [Tooltip("DBG → Dia (Marco 8b, D-159): lascas vermelhas, Descansar agora, permanentes.")]
        [SerializeField] Button dayButton;
        [SerializeField] DebugMenu dayMenu;
        [Tooltip("DBG → Estagiário (M7): contratar, demitir, níveis dele, Treinar e Promoção.")]
        [SerializeField] Button internButton;
        [SerializeField] DebugMenu internMenu;

        readonly object[] _statsArgs = new object[16];

        /// <summary>Nível e próximo custo de cada linha, na ordem de <see cref="UpgradeId"/> (o Letrado entrou no M8).</summary>
        readonly object[] _upgradeArgs = new object[2 * System.Enum.GetValues(typeof(UpgradeId)).Length];

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
            cameraButton.onClick.AddListener(ToggleCamera);
            buyPickaxe.onClick.AddListener(BuyPickaxe);
            buySpeed.onClick.AddListener(BuySpeed);
            buyMining.onClick.AddListener(BuyMining);
            buyCritical.onClick.AddListener(BuyCritical);
            saveNow.onClick.AddListener(SaveNow);
            deleteSave.onClick.AddListener(DeleteSave);
            autoTap.onClick.AddListener(ToggleAutoTap);
            forceCritical.onClick.AddListener(ToggleForceCritical);
            shake.onClick.AddListener(ToggleShake);
            sound.onClick.AddListener(ToggleSound);
            resetStats.onClick.AddListener(ResetStats);
            if (fillCaveBreaks != null) fillCaveBreaks.onClick.AddListener(FillCaveBreaks);
            if (nextCave != null) nextCave.onClick.AddListener(NextCave);
            if (lastCave != null) lastCave.onClick.AddListener(GoToLastCave);
            if (tenLevels != null) tenLevels.onClick.AddListener(SkipTenLevels);
            if (shopButton != null) shopButton.onClick.AddListener(ToggleShop);
            if (effectsButton != null) effectsButton.onClick.AddListener(ToggleEffects);
            if (caveButton != null) caveButton.onClick.AddListener(ToggleCave);
            if (calcButton != null) calcButton.onClick.AddListener(ToggleCalc);
            if (dayButton != null) dayButton.onClick.AddListener(ToggleDay);
            if (internButton != null) internButton.onClick.AddListener(ToggleIntern);
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
            cameraButton.onClick.RemoveListener(ToggleCamera);
            buyPickaxe.onClick.RemoveListener(BuyPickaxe);
            buySpeed.onClick.RemoveListener(BuySpeed);
            buyMining.onClick.RemoveListener(BuyMining);
            buyCritical.onClick.RemoveListener(BuyCritical);
            saveNow.onClick.RemoveListener(SaveNow);
            deleteSave.onClick.RemoveListener(DeleteSave);
            autoTap.onClick.RemoveListener(ToggleAutoTap);
            forceCritical.onClick.RemoveListener(ToggleForceCritical);
            shake.onClick.RemoveListener(ToggleShake);
            sound.onClick.RemoveListener(ToggleSound);
            resetStats.onClick.RemoveListener(ResetStats);
            if (fillCaveBreaks != null) fillCaveBreaks.onClick.RemoveListener(FillCaveBreaks);
            if (nextCave != null) nextCave.onClick.RemoveListener(NextCave);
            if (lastCave != null) lastCave.onClick.RemoveListener(GoToLastCave);
            if (tenLevels != null) tenLevels.onClick.RemoveListener(SkipTenLevels);
            if (shopButton != null) shopButton.onClick.RemoveListener(ToggleShop);
            if (effectsButton != null) effectsButton.onClick.RemoveListener(ToggleEffects);
            if (caveButton != null) caveButton.onClick.RemoveListener(ToggleCave);
            if (calcButton != null) calcButton.onClick.RemoveListener(ToggleCalc);
            if (dayButton != null) dayButton.onClick.RemoveListener(ToggleDay);
            if (internButton != null) internButton.onClick.RemoveListener(ToggleIntern);
        }

        void Update()
        {
            // A Loja mostra nível e custo das melhorias, e fica aberta com o DBG fechado.
            if (!content.activeSelf && !IsMenuOpen(shopMenu)) return;

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

        /// <summary>A Loja, os Efeitos, a Caverna (D-147) ou os Cálculos (E3b-3): abre fechando os outros menus e a câmera; aberto, fecha.</summary>
        void ToggleMenu(DebugMenu menu)
        {
            if (menu == null) return;
            bool open = !menu.IsOpen;
            CloseMenus();
            if (cameraPanel != null && cameraPanel.IsOpen) cameraPanel.Toggle();
            if (open) menu.Show();
            _refresh = 0f;
        }

        void CloseMenus()
        {
            if (shopMenu != null) shopMenu.Hide();
            if (effectsMenu != null) effectsMenu.Hide();
            if (caveMenu != null) caveMenu.Hide();
            if (calcMenu != null) calcMenu.Hide();
            if (dayMenu != null) dayMenu.Hide();
            if (internMenu != null) internMenu.Hide();
        }

        static bool IsMenuOpen(DebugMenu menu) => menu != null && menu.IsOpen;

        void ToggleShop() => ToggleMenu(shopMenu);
        void ToggleEffects() => ToggleMenu(effectsMenu);
        void ToggleCave() => ToggleMenu(caveMenu);
        void ToggleCalc() => ToggleMenu(calcMenu);
        void ToggleDay() => ToggleMenu(dayMenu);
        void ToggleIntern() => ToggleMenu(internMenu);

        void SetSpeed1() => runner.TimeScale = 1f;
        void SetSpeed5() => runner.TimeScale = 5f;
        void SetSpeed20() => runner.TimeScale = 20f;
        void SpawnCrystal() => runner.Simulation?.SpawnCrystalNow();
        void BreakTarget() => runner.Simulation?.BreakTarget();
        void AddShards() => runner.Simulation?.AddShards(ShardsToAdd);
        void FillCaveBreaks() => runner.Simulation?.FillCaveBreaks();
        void NextCave() => runner.Simulation?.SkipToNextCave();
        void SkipTenLevels() => runner.Simulation?.SkipToCave(runner.Simulation.State.Cave + 10);

        /// <summary>A última caverna, a trava cumprida e lascas para o último Letrado: falta comprar, DESCER e quebrar.</summary>
        void GoToLastCave()
        {
            var simulation = runner.Simulation;
            if (simulation == null) return;

            simulation.SkipToCave(simulation.Rules.CaveCount);
            simulation.FillCaveBreaks();
            double? cost = simulation.Preview(UpgradeId.Letrado).NextCost;
            if (cost != null && simulation.State.Shards < cost.Value) simulation.AddShards(cost.Value - simulation.State.Shards);
        }
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
        void BuyCritical() => Buy(UpgradeId.Critical);

        void Buy(UpgradeId id)
        {
            runner.Simulation?.Purchase(id);
            _refresh = 0f; // mostra o nível e o custo novos já no próximo quadro
        }

        /// <summary>
        /// M6 (E3): os controles da câmera da descida (D-61), num painel que fica aberto com o DBG fechado. Abrir fecha os
        /// menus (D-147), que ocupam o mesmo lugar.
        /// </summary>
        void ToggleCamera()
        {
            if (!cameraPanel.IsOpen) CloseMenus();
            cameraPanel.Toggle();
        }

        void RefreshStats()
        {
            var simulation = runner.Simulation;
            if (simulation == null) return;

            var state = simulation.State;
            double minutes = simulation.Elapsed / 60;
            var target = simulation.Miner.Target;
            var taps = tapInput.LastSecond;
            _statsArgs[0] = runner.Seed;
            _statsArgs[1] = NumberFormatter.Format(runner.TimeScale);
            _statsArgs[2] = NumberFormatter.Format(minutes);
            _statsArgs[3] = NumberFormatter.Format(state.Hits);
            _statsArgs[4] = NumberFormatter.Format(state.Breaks);
            _statsArgs[5] = NumberFormatter.Format(minutes > 0 ? state.Shards / minutes : 0);
            _statsArgs[6] = simulation.Crystals.Count;
            _statsArgs[7] = simulation.Rules.MaxCrystals;
            _statsArgs[8] = target != null ? target.Id.ToString() : "-";
            _statsArgs[9] = NumberFormatter.Format(simulation.Miner.Distance);
            _statsArgs[10] = taps.Counted;
            _statsArgs[11] = taps.Capped;
            _statsArgs[12] = taps.Began;
            _statsArgs[13] = taps.Repeated;
            _statsArgs[14] = tapInput.FingersDown;
            _statsArgs[15] = taps.Together;
            stats.text = string.Format(statsFormat, _statsArgs);

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
