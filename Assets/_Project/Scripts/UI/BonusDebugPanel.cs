using System;
using System.Text;
using MinersIdle.Core;
using MinersIdle.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// DBG → Bônus (M9b, E1; D-217): dá os bônus de um combo terminado em ×10, ×30, ×50 ou ×100 sem tocar (pela mesma regra
    /// do jogo, <see cref="MiningSimulation.GrantComboBonuses"/>), limpa os ativos e, no alto, mostra o combo de agora, a
    /// regra do acúmulo, o dano a mais somado e cada bônus ativo com o tempo que falta. Fica no painel de debug: no release
    /// some junto. Só manda comandos à simulação do <see cref="MiningRunner"/>. Os textos ficam na cena.
    /// </summary>
    public sealed class BonusDebugPanel : MonoBehaviour
    {
        /// <summary>Os combos dos botões, na ordem de <see cref="grant"/>.</summary>
        public static readonly int[] Combos = { 10, 30, 50, 100 };

        /// <summary>De quanto em quanto tempo real a lista é refeita com o menu aberto (o tempo que falta muda sempre).</summary>
        const float RefreshSeconds = 0.1f;

        [SerializeField] MiningRunner runner;

        [Tooltip("Combo ×10, ×30, ×50 e ×100, na ordem de Combos.")]
        [SerializeField] Button[] grant = Array.Empty<Button>();
        [SerializeField] Button clear;

        [SerializeField] TMP_Text summary;
        [Tooltip("A primeira linha. {0} combo de agora · {1} regra do acúmulo · {2} dano a mais somado")]
        [SerializeField] string headerFormat;
        [SerializeField] string cumulativeLabel;
        [SerializeField] string highestOnlyLabel;
        [Tooltip("Sem bônus ativo.")]
        [SerializeField] string noneLabel;
        [Tooltip("Um bônus de dano. {0} o efeito · {1} o tempo que falta · {2} o marco")]
        [SerializeField] string damageFormat;
        [Tooltip("O crítico garantido. {1} o tempo que falta · {2} o marco")]
        [SerializeField] string criticalFormat;
        [Tooltip("Entre um bônus e outro.")]
        [SerializeField] string separator = " · ";

        readonly StringBuilder _text = new StringBuilder(256);
        UnityEngine.Events.UnityAction[] _grant = Array.Empty<UnityEngine.Events.UnityAction>();
        DebugMenu _menu;
        float _refresh;

        void Awake()
        {
            _menu = GetComponent<DebugMenu>();
            _grant = new UnityEngine.Events.UnityAction[grant.Length];
            for (int i = 0; i < grant.Length; i++)
            {
                int index = i;
                _grant[i] = () => Grant(index);
            }
        }

        void OnEnable()
        {
            for (int i = 0; i < grant.Length; i++)
                if (grant[i] != null) grant[i].onClick.AddListener(_grant[i]);
            if (clear != null) clear.onClick.AddListener(Clear);
            _refresh = 0f;
        }

        void OnDisable()
        {
            for (int i = 0; i < grant.Length; i++)
                if (grant[i] != null) grant[i].onClick.RemoveListener(_grant[i]);
            if (clear != null) clear.onClick.RemoveListener(Clear);
        }

        void Update()
        {
            if (_menu != null && !_menu.IsOpen) return;
            _refresh -= Time.unscaledDeltaTime;
            if (_refresh > 0f) return;
            _refresh = RefreshSeconds;
            Refresh();
        }

        /// <summary>Os bônus de um combo terminado no marco do botão <paramref name="index"/> (de <see cref="Combos"/>).</summary>
        public void Grant(int index)
        {
            if (index < 0 || index >= Combos.Length || runner.Simulation == null) return;
            runner.Simulation.GrantComboBonuses(Combos[index]);
            _refresh = 0f;
        }

        public void Clear()
        {
            runner.Simulation?.ClearBonuses();
            _refresh = 0f;
        }

        /// <summary>O texto do alto do menu (também para os testes).</summary>
        public string SummaryText => summary != null ? summary.text : "";

        void Refresh()
        {
            var simulation = runner.Simulation;
            if (summary == null || simulation == null) return;

            var bonuses = simulation.Bonuses;
            _text.Clear();
            _text.AppendFormat(headerFormat, simulation.Combo,
                               simulation.ComboStacking == ComboStacking.Cumulative ? cumulativeLabel : highestOnlyLabel,
                               NumberFormatter.FormatPercent(bonuses.DamagePercent));
            _text.Append('\n');
            if (bonuses.Count == 0) _text.Append(noneLabel);
            for (int i = 0; i < bonuses.Count; i++)
            {
                var bonus = bonuses.All[i];
                if (i > 0) _text.Append(separator);
                _text.AppendFormat(bonus.Kind == BonusKind.GuaranteedCritical ? criticalFormat : damageFormat,
                                   NumberFormatter.FormatPercent(bonus.Value), Seconds(bonus.SecondsLeft), bonus.Tier);
            }
            summary.SetText(_text);
        }

        /// <summary>O tempo que falta com um décimo ("4,3 s"): no DBG, para ver o fim exato.</summary>
        static string Seconds(double seconds) => Math.Max(0, seconds).ToString("0.0", PtBr) + " s";

        static readonly System.Globalization.CultureInfo PtBr = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
    }
}
