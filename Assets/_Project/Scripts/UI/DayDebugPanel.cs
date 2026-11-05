using MinersIdle.Core;
using MinersIdle.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// DBG → Dia (Marco 8b, D-159): os atalhos do dia e das melhorias permanentes, para testar sem jogar runs inteiras.
    /// +10 e +100 lascas vermelhas, Descansar agora (em qualquer nível, sem modal), +1 nível em cada permanente e Zerar
    /// permanentes. No alto, o dia, o banco, o que o jogador leva se descansar agora e o nível de cada permanente. Fica
    /// no painel de debug: no release some junto. Só manda comandos ao <see cref="MiningRunner"/> e à simulação. Os
    /// textos (rótulos e modelo) ficam na cena.
    /// </summary>
    public sealed class DayDebugPanel : MonoBehaviour
    {
        const float RefreshSeconds = 0.5f;

        [SerializeField] MiningRunner runner;
        [SerializeField] DebugMenu menu;

        [SerializeField] Button addRed10;
        [SerializeField] Button addRed100;
        [SerializeField] Button restNow;
        [SerializeField] Button grantLevels;
        [SerializeField] Button resetPermanents;

        [SerializeField] TMP_Text summary;
        [Tooltip("{0} dia · {1} banco · {2} leva se descansar agora · {3} nível da run · {4}…{8} níveis de Memória, Sangue " +
                 "da Montanha, Coração Ardente, Dedos Nervosos e Alma dos Anões")]
        [SerializeField, TextArea(2, 4)] string summaryFormat;

        readonly object[] _args = new object[9];
        float _refresh;

        void OnEnable()
        {
            addRed10.onClick.AddListener(AddRed10);
            addRed100.onClick.AddListener(AddRed100);
            restNow.onClick.AddListener(RestNow);
            grantLevels.onClick.AddListener(GrantLevels);
            resetPermanents.onClick.AddListener(ResetPermanents);
            runner.SimulationStarted += OnSimulationStarted;
            Refresh();
        }

        void OnDisable()
        {
            addRed10.onClick.RemoveListener(AddRed10);
            addRed100.onClick.RemoveListener(AddRed100);
            restNow.onClick.RemoveListener(RestNow);
            grantLevels.onClick.RemoveListener(GrantLevels);
            resetPermanents.onClick.RemoveListener(ResetPermanents);
            runner.SimulationStarted -= OnSimulationStarted;
        }

        void Update()
        {
            // O nível (e o que se leva) muda jogando; só vale atualizar com o menu aberto.
            if (menu == null || !menu.IsOpen) return;
            _refresh -= Time.unscaledDeltaTime;
            if (_refresh > 0f) return;
            Refresh();
        }

        void OnSimulationStarted(MiningSimulation simulation) => Refresh();

        public void AddRed10() => AddRed(10);
        public void AddRed100() => AddRed(100);

        void AddRed(double amount)
        {
            runner.Simulation?.AddRedShards(amount);
            Refresh();
        }

        /// <summary>Descansa agora, em qualquer nível e sem modal: o dia avança e a run recomeça.</summary>
        public void RestNow()
        {
            runner.Rest(force: true);
            Refresh();
        }

        public void GrantLevels()
        {
            runner.Simulation?.GrantPermanentLevels();
            Refresh();
        }

        public void ResetPermanents()
        {
            runner.Simulation?.ResetPermanents();
            Refresh();
        }

        void Refresh()
        {
            _refresh = RefreshSeconds;
            var simulation = runner.Simulation;
            if (summary == null || simulation == null) return;

            var permanents = simulation.Permanents;
            _args[0] = permanents.State.Day;
            _args[1] = NumberFormatter.FormatShards(permanents.State.RedShards);
            _args[2] = NumberFormatter.FormatShards(simulation.PendingRedShards);
            _args[3] = simulation.State.Cave;
            _args[4] = permanents.State.LevelOf(PermanentId.Memory);
            _args[5] = permanents.State.LevelOf(PermanentId.MountainBlood);
            _args[6] = permanents.State.LevelOf(PermanentId.BurningHeart);
            _args[7] = permanents.State.LevelOf(PermanentId.NervousFingers);
            _args[8] = permanents.State.LevelOf(PermanentId.DwarfSoul);
            summary.text = string.Format(summaryFormat, _args);
        }
    }
}
