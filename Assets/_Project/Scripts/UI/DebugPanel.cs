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
    /// mesma semente ou com uma nova, "Mostrar ordem" e estatísticas. Os atalhos chamam os mesmos comandos das regras.
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

        [SerializeField] Button toggleButton;
        [SerializeField] GameObject content;
        [SerializeField] TMP_Text stats;
        [Tooltip("{0} semente · {1} escala do tempo · {2} minutos de jogo · {3} golpes · {4} quebras · {5} lascas/min · " +
                 "{6} cristais na tela · {7} máximo · {8} alvo (id) · {9} lote do alvo")]
        [SerializeField, TextArea(3, 8)] string statsFormat;

        [SerializeField] Button speed1;
        [SerializeField] Button speed5;
        [SerializeField] Button speed20;
        [SerializeField] Button spawnCrystal;
        [SerializeField] Button breakTarget;
        [SerializeField] Button addShards;
        [SerializeField] Button restartSameSeed;
        [SerializeField] Button restartNewSeed;
        [SerializeField] Button showOrder;

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
        }

        void SetSpeed1() => runner.TimeScale = 1f;
        void SetSpeed5() => runner.TimeScale = 5f;
        void SetSpeed20() => runner.TimeScale = 20f;
        void SpawnCrystal() => runner.Simulation?.SpawnCrystalNow();
        void BreakTarget() => runner.Simulation?.BreakTarget();
        void AddShards() => runner.Simulation?.AddShards(ShardsToAdd);
        void RestartSameSeed() => runner.StartNew(runner.Seed);
        void RestartNewSeed() => runner.StartNew(MiningRunner.NewSeed());

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
        }
    }
}
