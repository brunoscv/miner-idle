using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// DBG → Andar → "Recorde: n" e "Auto: ligado" (M9c E3): o andar mais fundo direto, passando pelos marcos (1 → 5 → 10 →
    /// 20 → 1; abaixo do andar de agora, a partida volta a ele), e o ajuste do avanço automático (o mesmo dos AJUSTES).
    /// Está dentro do painel de debug: no release some junto com ele.
    /// </summary>
    public sealed class FloorMilestoneDebug : MiningViewBase
    {
        static readonly int[] Marks = { 1, 5, 10, 20 };

        [SerializeField] GameSettings settings;
        [SerializeField] Button record;
        [Tooltip("{0} = o andar mais fundo de agora.")]
        [SerializeField] string recordFormat = "Recorde: {0}";
        [SerializeField] Button auto;
        [Tooltip("{0} = ligado ou desligado.")]
        [SerializeField] string autoFormat = "Auto: {0}";
        [SerializeField] string onName = "ligado";
        [SerializeField] string offName = "desligado";

        TMP_Text _recordLabel, _autoLabel;

        void Awake()
        {
            if (record != null) _recordLabel = record.GetComponentInChildren<TMP_Text>();
            if (auto != null) _autoLabel = auto.GetComponentInChildren<TMP_Text>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (record != null) record.onClick.AddListener(NextRecord);
            if (auto != null) auto.onClick.AddListener(ToggleAuto);
            if (settings != null) settings.Changed += Refresh;
            Refresh();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (record != null) record.onClick.RemoveListener(NextRecord);
            if (auto != null) auto.onClick.RemoveListener(ToggleAuto);
            if (settings != null) settings.Changed -= Refresh;
        }

        protected override void Subscribe(MiningSimulation simulation)
        {
            simulation.CaveChanged += OnFloorChanged;
            simulation.PermanentsChanged += Refresh;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.CaveChanged -= OnFloorChanged;
            simulation.PermanentsChanged -= Refresh;
        }

        protected override void Rebuild(MiningSimulation simulation) => Refresh();

        void OnFloorChanged(int floor) => Refresh();

        /// <summary>O marco depois do recorde de agora (depois do último, volta ao 1).</summary>
        public static int NextMark(int deepest)
        {
            foreach (int mark in Marks)
                if (mark > deepest) return mark;
            return Marks[0];
        }

        /// <summary>O recorde vai ao próximo marco.</summary>
        public void NextRecord()
        {
            var simulation = Simulation;
            if (simulation == null) return;
            simulation.SetDeepestFloor(NextMark(simulation.DeepestFloor));
            Refresh();
        }

        /// <summary>Liga e desliga o avanço automático (o ajuste dos AJUSTES, que fica gravado).</summary>
        public void ToggleAuto()
        {
            if (settings != null) settings.AutoAdvance = !settings.AutoAdvance; // avisa Changed → Refresh
        }

        void Refresh()
        {
            var simulation = Simulation;
            if (_recordLabel != null && simulation != null) _recordLabel.text = string.Format(recordFormat, simulation.DeepestFloor);
            if (_autoLabel != null && settings != null) _autoLabel.text = string.Format(autoFormat, settings.AutoAdvance ? onName : offName);
        }
    }
}
