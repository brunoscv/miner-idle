using MinersIdle.Core;
using MinersIdle.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// DBG → Estagiário (M7, E1): os atalhos do estagiário, para testar sem juntar 500 lascas nem treinar por dez runs.
    /// Contratar e Demitir, +1 nível em cada melhoria dele, Treinar +1 e Promoção (o Treinar no limite e a Promoção). No
    /// alto, se ele está contratado, o dano, o intervalo, os golpes e os níveis. Fica no painel de debug: no release some
    /// junto. Só manda comandos à simulação. Os textos (rótulos e modelo) ficam na cena.
    /// </summary>
    public sealed class InternDebugPanel : MonoBehaviour
    {
        const float RefreshSeconds = 0.5f;

        [SerializeField] MiningRunner runner;
        [SerializeField] DebugMenu menu;

        [SerializeField] Button hire;
        [SerializeField] Button fire;
        [SerializeField] Button grantLevels;
        [SerializeField] Button train;
        [SerializeField] Button promote;

        [SerializeField] TMP_Text summary;
        [Tooltip("{0} contratado ou não · {1} dano · {2} intervalo · {3} golpes · {4} Picareta · {5} Velocidade · " +
                 "{6} Mineração · {7} Treinar · {8} Promoção · {9} lascas offline (multiplicador)")]
        [SerializeField, TextArea(2, 4)] string summaryFormat;
        [SerializeField] string hiredLabel;
        [SerializeField] string notHiredLabel;
        [Tooltip("No lugar do resumo, num balanço sem estagiário (GDD v5, D-172).")]
        [SerializeField] string unavailableLabel;

        readonly object[] _args = new object[10];
        float _refresh;

        void OnEnable()
        {
            hire.onClick.AddListener(Hire);
            fire.onClick.AddListener(Fire);
            grantLevels.onClick.AddListener(GrantLevels);
            train.onClick.AddListener(Train);
            promote.onClick.AddListener(Promote);
            runner.SimulationStarted += OnSimulationStarted;
            Refresh();
        }

        void OnDisable()
        {
            hire.onClick.RemoveListener(Hire);
            fire.onClick.RemoveListener(Fire);
            grantLevels.onClick.RemoveListener(GrantLevels);
            train.onClick.RemoveListener(Train);
            promote.onClick.RemoveListener(Promote);
            runner.SimulationStarted -= OnSimulationStarted;
        }

        void Update()
        {
            // Os golpes mudam jogando; só vale atualizar com o menu aberto.
            if (menu == null || !menu.IsOpen) return;
            _refresh -= Time.unscaledDeltaTime;
            if (_refresh > 0f) return;
            Refresh();
        }

        void OnSimulationStarted(MiningSimulation simulation) => Refresh();

        public void Hire() => Run(s => s.HireIntern());
        public void Fire() => Run(s => s.FireIntern());
        public void GrantLevels() => Run(s => s.GrantInternLevels());
        public void Train() => Run(s => s.GrantPermanentLevel(PermanentId.InternTraining));
        public void Promote() => Run(s => s.PromoteIntern());

        void Run(System.Func<MiningSimulation, bool> command)
        {
            var simulation = runner.Simulation;
            if (simulation != null) command(simulation);
            Refresh();
        }

        void Refresh()
        {
            _refresh = RefreshSeconds;
            var simulation = runner.Simulation;
            if (summary == null || simulation == null) return;
            if (!simulation.Economy.HasIntern)
            {
                summary.text = unavailableLabel;
                return;
            }

            var intern = simulation.InternStats;
            var state = simulation.State;
            var permanents = simulation.Permanents.State;
            _args[0] = intern.IsHired ? hiredLabel : notHiredLabel;
            _args[1] = NumberFormatter.Format(intern.Damage);
            _args[2] = NumberFormatter.FormatSeconds(intern.HitInterval);
            _args[3] = simulation.InternHits;
            _args[4] = state.LevelOf(UpgradeId.InternPickaxe);
            _args[5] = state.LevelOf(UpgradeId.InternSpeed);
            _args[6] = state.LevelOf(UpgradeId.InternMining);
            _args[7] = permanents.LevelOf(PermanentId.InternTraining);
            _args[8] = permanents.LevelOf(PermanentId.InternPromotion);
            _args[9] = NumberFormatter.Format(intern.OfflineRewardMultiplier);
            summary.text = string.Format(summaryFormat, _args);
        }
    }
}
