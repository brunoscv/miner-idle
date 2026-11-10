using System;
using MinersIdle.Core;
using MinersIdle.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// DBG → Offline (M9, E1): simula o jogo fechado sem fechar (1 min, 10 min, 1 h, 6 h e 8 h; o de 8 h mostra o corte
    /// no teto de 6 h), "Voltar o relógio 1 h" (a volta com o relógio do aparelho atrasado, D-196) e, no alto, a conta do
    /// último offline: o tempo fora e o que contou, os cristais, os golpes e o tempo por cristal e as lascas. Fica no
    /// painel de debug: no release some junto. Só manda comandos ao <see cref="MiningRunner"/>. Os textos ficam na cena.
    /// </summary>
    public sealed class OfflineDebugPanel : MonoBehaviour
    {
        /// <summary>Os tempos dos botões de simular, em segundos, na ordem de <see cref="simulate"/>.</summary>
        public static readonly double[] SimulatedSeconds = { 60, 10 * 60, 3600, 6 * 3600, 8 * 3600 };

        /// <summary>Quanto o "Voltar o relógio" volta.</summary>
        public static readonly TimeSpan ClockBack = TimeSpan.FromHours(1);

        [SerializeField] MiningRunner runner;

        [Tooltip("Simular 1 min, 10 min, 1 h, 6 h e 8 h fora, na ordem de SimulatedSeconds.")]
        [SerializeField] Button[] simulate = Array.Empty<Button>();
        [SerializeField] Button clockBack;

        [SerializeField] TMP_Text summary;
        [Tooltip("Antes do primeiro offline.")]
        [SerializeField] string noneLabel;
        [Tooltip("{0} tempo fora · {1} tempo que contou · {2} cristais · {3} golpes por cristal · {4} tempo por cristal · " +
                 "{5} lascas por cristal · {6} lascas · {7} aviso (teto ou relógio)")]
        [SerializeField, TextArea(2, 4)] string summaryFormat;
        [Tooltip("Sem estagiário contratado: nada offline (D-139). {0} = tempo fora.")]
        [SerializeField] string notHiredFormat;
        [Tooltip("O aviso {7} quando passou do teto.")]
        [SerializeField] string cappedLabel;
        [Tooltip("O aviso {7} quando o relógio voltou.")]
        [SerializeField] string clockBackLabel;

        readonly object[] _args = new object[8];
        UnityEngine.Events.UnityAction[] _simulate = Array.Empty<UnityEngine.Events.UnityAction>();

        void Awake()
        {
            _simulate = new UnityEngine.Events.UnityAction[simulate.Length];
            for (int i = 0; i < simulate.Length; i++)
            {
                int index = i;
                _simulate[i] = () => Simulate(index);
            }
        }

        void OnEnable()
        {
            for (int i = 0; i < simulate.Length; i++)
                if (simulate[i] != null) simulate[i].onClick.AddListener(_simulate[i]);
            clockBack.onClick.AddListener(TurnClockBack);
            runner.OfflineApplied += OnOfflineApplied;
            Refresh();
        }

        void OnDisable()
        {
            for (int i = 0; i < simulate.Length; i++)
                if (simulate[i] != null) simulate[i].onClick.RemoveListener(_simulate[i]);
            clockBack.onClick.RemoveListener(TurnClockBack);
            runner.OfflineApplied -= OnOfflineApplied;
        }

        /// <summary>Simular o tempo fora do botão <paramref name="index"/> (de <see cref="SimulatedSeconds"/>).</summary>
        public void Simulate(int index)
        {
            if (index < 0 || index >= SimulatedSeconds.Length) return;
            runner.SimulateAway(SimulatedSeconds[index]);
        }

        public void TurnClockBack() => runner.TurnClockBack(ClockBack);

        void OnOfflineApplied(OfflineReport report) => Refresh();

        void Refresh()
        {
            if (summary == null) return;
            if (!runner.LastOffline.HasValue)
            {
                summary.text = noneLabel;
                return;
            }

            var report = runner.LastOffline.Value;
            if (!report.Hired)
            {
                summary.text = string.Format(notHiredFormat, Duration(report.SecondsAway));
                return;
            }
            _args[0] = Duration(report.SecondsAway);
            _args[1] = NumberFormatter.FormatDuration(report.SecondsCounted);
            _args[2] = NumberFormatter.Format(report.Crystals);
            _args[3] = NumberFormatter.Format(report.HitsPerCrystal);
            _args[4] = NumberFormatter.FormatSeconds(report.SecondsPerCrystal);
            _args[5] = NumberFormatter.FormatShards(report.ShardsPerCrystal);
            _args[6] = NumberFormatter.FormatShards(report.Shards);
            _args[7] = report.ClockWentBack ? clockBackLabel : report.Capped ? cappedLabel : "";
            summary.text = string.Format(summaryFormat, _args);
        }

        /// <summary>O tempo fora, com o sinal quando o relógio voltou ("−1 h 00 min").</summary>
        static string Duration(double seconds) =>
            seconds < 0 ? "−" + NumberFormatter.FormatDuration(-seconds) : NumberFormatter.FormatDuration(seconds);
    }
}
