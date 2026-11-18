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
    /// regra do acúmulo, o dano a mais somado e cada bônus ativo com o tempo que falta. M9b E4 (D-226): Próximo raro, Janela
    /// agora e uma carta direta de cada tipo, e a janela do raro no alto. M9b E5 (D-234): AD agora e Pular espera. M9e E1
    /// (D-285): Raro agora, Contador 5 s, Tier 2 e a carta Dano de Crítico; no alto, os segundos até o raro e o tier. As
    /// cartas que o balanço não tem ficam escondidas. Fica no painel de debug: no release
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

        [Header("Cristal raro (M9b E4, D-226)")]
        [Tooltip("O próximo cristal que nascer é raro.")]
        [SerializeField] Button nextRare;
        [Tooltip("Abre a janela das cartas sem quebrar nada (com uma aberta, entra na fila).")]
        [SerializeField] Button openWindow;
        [Tooltip("Uma carta direto, na ordem do enum RareCard (Dano, Rápido, Lascas, Crítico, Dano de Crítico).")]
        [SerializeField] Button[] cards = Array.Empty<Button>();
        [Tooltip("O nome de cada carta no alto do menu, na ordem do enum RareCard.")]
        [SerializeField] string[] cardLabels = Array.Empty<string>();
        [Tooltip("Uma carta ativa. {0} o nome · {1} o tempo que falta · {2} o efeito (×2, +10%)")]
        [SerializeField] string rareCardFormat;
        [Tooltip("A janela aberta. {0} e {1} as cartas · {2} o tempo para escolher · {3} raros na fila")]
        [SerializeField] string windowFormat;
        [Tooltip("A janela mostrando o sorteio. {0} a carta que saiu · {1} raros na fila")]
        [SerializeField] string windowPickedFormat;
        [Tooltip("Somado ao alto com o próximo raro marcado.")]
        [SerializeField] string nextRareLabel;

        [Header("Contador do raro (M9e E1, D-283, D-285)")]
        [Tooltip("Um raro já, num cristal que nasce agora (sem esperar o contador).")]
        [SerializeField] Button rareNow;
        [Tooltip("Troca o contador entre o das regras e o curto (debugIntervalSeconds).")]
        [SerializeField] Button rareCounter;
        [Tooltip("Liga e desliga o tier 2 das cartas sem o Raro Dourado.")]
        [SerializeField] Button rareTier2;
        [Tooltip("O contador curto do DBG, em segundos.")]
        [SerializeField] double debugIntervalSeconds = 5;
        [Tooltip("O rótulo do botão do contador. {0} o intervalo (60 s)")]
        [SerializeField] string counterButtonFormat;
        [Tooltip("O rótulo do botão do tier 2. {0} sim ou não")]
        [SerializeField] string tier2ButtonFormat;
        [SerializeField] string yesLabel;
        [SerializeField] string noLabel;
        [Tooltip("O contador andando. {0} o tempo até o raro · {1} o intervalo · {2} o tier")]
        [SerializeField] string counterFormat;
        [Tooltip("O contador parado antes do andar do raro. {0} o andar · {1} o tier")]
        [SerializeField] string counterLockedFormat;

        [Header("AD (M9b E5, D-234)")]
        [Tooltip("O boost do AD direto, sem o anúncio (com um ativo, nada: D-188).")]
        [SerializeField] Button adNow;
        [Tooltip("Com o anúncio na tela, mostra o RECOMPENSA já.")]
        [SerializeField] Button adSkip;
        [SerializeField] SimulatedAdScreen adScreen;
        [Tooltip("O boost do AD ativo. {0} quanto a velocidade multiplica · {1} o tempo que falta")]
        [SerializeField] string adFormat;

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
        UnityEngine.Events.UnityAction[] _cards = Array.Empty<UnityEngine.Events.UnityAction>();
        TMP_Text _counterLabel, _tier2Label;
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
            _cards = new UnityEngine.Events.UnityAction[cards.Length];
            for (int i = 0; i < cards.Length; i++)
            {
                var card = (RareCard)i;
                _cards[i] = () => GrantCard(card);
            }
            if (rareCounter != null) _counterLabel = rareCounter.GetComponentInChildren<TMP_Text>(true);
            if (rareTier2 != null) _tier2Label = rareTier2.GetComponentInChildren<TMP_Text>(true);
        }

        void OnEnable()
        {
            for (int i = 0; i < grant.Length; i++)
                if (grant[i] != null) grant[i].onClick.AddListener(_grant[i]);
            if (clear != null) clear.onClick.AddListener(Clear);
            if (nextRare != null) nextRare.onClick.AddListener(NextRare);
            if (adNow != null) adNow.onClick.AddListener(AdNow);
            if (adSkip != null) adSkip.onClick.AddListener(SkipAd);
            if (openWindow != null) openWindow.onClick.AddListener(OpenWindow);
            for (int i = 0; i < cards.Length; i++)
                if (cards[i] != null) cards[i].onClick.AddListener(_cards[i]);
            if (rareNow != null) rareNow.onClick.AddListener(RareNow);
            if (rareCounter != null) rareCounter.onClick.AddListener(ToggleCounter);
            if (rareTier2 != null) rareTier2.onClick.AddListener(ToggleTier2);
            _refresh = 0f;
            RefreshButtons();
        }

        void OnDisable()
        {
            for (int i = 0; i < grant.Length; i++)
                if (grant[i] != null) grant[i].onClick.RemoveListener(_grant[i]);
            if (clear != null) clear.onClick.RemoveListener(Clear);
            if (nextRare != null) nextRare.onClick.RemoveListener(NextRare);
            if (adNow != null) adNow.onClick.RemoveListener(AdNow);
            if (adSkip != null) adSkip.onClick.RemoveListener(SkipAd);
            if (openWindow != null) openWindow.onClick.RemoveListener(OpenWindow);
            for (int i = 0; i < cards.Length; i++)
                if (cards[i] != null) cards[i].onClick.RemoveListener(_cards[i]);
            if (rareNow != null) rareNow.onClick.RemoveListener(RareNow);
            if (rareCounter != null) rareCounter.onClick.RemoveListener(ToggleCounter);
            if (rareTier2 != null) rareTier2.onClick.RemoveListener(ToggleTier2);
        }

        void Update()
        {
            if (_menu != null && !_menu.IsOpen) return;
            _refresh -= Time.unscaledDeltaTime;
            if (_refresh > 0f) return;
            _refresh = RefreshSeconds;
            Refresh();
            RefreshButtons();
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

        /// <summary>Próximo raro (D-226): o próximo cristal que nascer na fila é raro.</summary>
        public void NextRare()
        {
            if (runner.Simulation == null) return;
            runner.Simulation.ForceNextRare = true;
            _refresh = 0f;
        }

        /// <summary>Raro agora (D-285): um cristal nasce já, no fim da fila, e é raro; o contador recomeça.</summary>
        public void RareNow()
        {
            var simulation = runner.Simulation;
            if (simulation == null) return;
            simulation.ForceNextRare = true;
            simulation.SpawnCrystalNow();
            _refresh = 0f;
        }

        /// <summary>Contador 5 s (D-285): troca o intervalo do raro entre o das regras e o curto do DBG.</summary>
        public void ToggleCounter()
        {
            var simulation = runner.Simulation;
            if (simulation == null) return;
            simulation.RareIntervalOverride = simulation.RareIntervalOverride > 0 ? 0 : debugIntervalSeconds;
            _refresh = 0f;
            RefreshButtons();
        }

        /// <summary>Tier 2 (M9e E1): as cartas no tier 2 sem o Raro Dourado; de novo, volta.</summary>
        public void ToggleTier2()
        {
            var simulation = runner.Simulation;
            if (simulation == null) return;
            simulation.ForceRareTier2 = !simulation.ForceRareTier2;
            _refresh = 0f;
            RefreshButtons();
        }

        /// <summary>O rótulo do botão do contador (também para os testes).</summary>
        public string CounterButtonText => _counterLabel != null ? _counterLabel.text : "";

        /// <summary>O rótulo do botão do tier 2 (também para os testes).</summary>
        public string Tier2ButtonText => _tier2Label != null ? _tier2Label.text : "";

        /// <summary>Os rótulos dos botões que trocam, e as cartas que o balanço não tem escondidas.</summary>
        void RefreshButtons()
        {
            var simulation = runner != null ? runner.Simulation : null;
            if (simulation == null) return;
            var rare = simulation.Strike.Rare;
            for (int i = 0; i < cards.Length; i++)
            {
                bool has = rare.Has((RareCard)i);
                if (cards[i] != null && cards[i].gameObject.activeSelf != has) cards[i].gameObject.SetActive(has);
            }
            if (_counterLabel != null && !string.IsNullOrEmpty(counterButtonFormat))
                _counterLabel.text = string.Format(counterButtonFormat, WholeSeconds(simulation.RareInterval));
            if (_tier2Label != null && !string.IsNullOrEmpty(tier2ButtonFormat))
                _tier2Label.text = string.Format(tier2ButtonFormat, simulation.RareTier >= 2 ? yesLabel : noLabel);
        }

        /// <summary>Janela agora (D-226): a janela das cartas, como se um raro tivesse quebrado.</summary>
        public void OpenWindow()
        {
            runner.Simulation?.OpenRareWindow();
            _refresh = 0f;
        }

        /// <summary>A carta direto, sem janela (D-226).</summary>
        public void GrantCard(RareCard card)
        {
            runner.Simulation?.GrantRareCard(card);
            _refresh = 0f;
        }

        /// <summary>AD agora (D-234): o boost direto, sem anúncio; com um ativo, nada (D-188).</summary>
        public void AdNow()
        {
            runner.Simulation?.GrantAdBoost();
            _refresh = 0f;
        }

        /// <summary>Pular espera (D-234): o anúncio na tela mostra o RECOMPENSA já.</summary>
        public void SkipAd()
        {
            if (adScreen != null) adScreen.SkipWait();
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
                if (bonus.Source == BonusSource.Rare)
                    _text.AppendFormat(rareCardFormat ?? "{0} {1}", CardLabel((RareCard)bonus.Tier), Seconds(bonus.SecondsLeft),
                                       Effect(bonus));
                else if (bonus.Source == BonusSource.Ad)
                    _text.AppendFormat(adFormat ?? "{0} {1}", NumberFormatter.Format(bonus.Value), Seconds(bonus.SecondsLeft));
                else
                    _text.AppendFormat(bonus.Kind == BonusKind.GuaranteedCritical ? criticalFormat : damageFormat,
                                       NumberFormatter.FormatPercent(bonus.Value), Seconds(bonus.SecondsLeft), bonus.Tier);
            }

            var window = simulation.RareWindow;
            if (window.IsChoosing && !string.IsNullOrEmpty(windowFormat))
                _text.Append('\n').AppendFormat(windowFormat, CardLabel(window.First), CardLabel(window.Second),
                                                 Seconds(window.SecondsLeft), window.Queued);
            else if (window.IsOpen && !string.IsNullOrEmpty(windowPickedFormat))
                _text.Append('\n').AppendFormat(windowPickedFormat, CardLabel(window.CardAt(window.Chosen)), window.Queued);
            if (simulation.ForceNextRare && !string.IsNullOrEmpty(nextRareLabel)) _text.Append('\n').Append(nextRareLabel);
            var rare = simulation.Strike.Rare;
            if (rare.IsEnabled && rare.UsesCounter)
            {
                if (simulation.IsRareCounting && !string.IsNullOrEmpty(counterFormat))
                    _text.Append('\n').AppendFormat(counterFormat, Seconds(simulation.RareCountdown),
                                                     WholeSeconds(simulation.RareInterval), simulation.RareTier);
                else if (!string.IsNullOrEmpty(counterLockedFormat))
                    _text.Append('\n').AppendFormat(counterLockedFormat, simulation.Rules.RareFloor, simulation.RareTier);
            }
            summary.SetText(_text);
        }

        /// <summary>O efeito de uma carta: "×2" nos multiplicadores, "+10%" na chance de crítico.</summary>
        static string Effect(ActiveBonus bonus) => NumberFormatter.FormatEffect(bonus.Kind, bonus.Value);

        static string WholeSeconds(double seconds) => Math.Round(seconds).ToString("0", PtBr) + " s";

        string CardLabel(RareCard card) => (int)card < cardLabels.Length ? cardLabels[(int)card] : card.ToString();

        /// <summary>O tempo que falta com um décimo ("4,3 s"): no DBG, para ver o fim exato.</summary>
        static string Seconds(double seconds) => Math.Max(0, seconds).ToString("0.0", PtBr) + " s";

        static readonly System.Globalization.CultureInfo PtBr = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
    }
}
