using UnityEngine;

namespace MinersIdle.Game
{
    /// <summary>
    /// Textos do feedback de mineração (M5, E3), da descida (M8: a faixa da caverna, a tela do coração da montanha) e do
    /// descanso (Marco 8b, E3: o botão, o modal e o resumo do dia, D-151…D-154), em pt-BR e em dados. Asset do jogo: <c>Data/FeedbackTexts</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "Miners Idle/Textos do feedback", fileName = "FeedbackTexts")]
    public sealed class FeedbackTexts : ScriptableObject
    {
        [SerializeField] string criticalLabel;
        [Tooltip("{0} = combo atual.")]
        [SerializeField] string comboFormat;
        [Tooltip("{0} = combo no marco (10, 20…).")]
        [SerializeField] string milestoneFormat;
        [Tooltip("A faixa da caverna nova (M8, D-111). {0} = número da caverna; {1} = nome do bioma.")]
        [SerializeField] string caveBannerFormat;

        [Header("Coração da montanha (M8, E3, D-112)")]
        [SerializeField] string heartTitle;
        [Tooltip("{0} = tempo de jogo; {1} = lascas coletadas.")]
        [TextArea, SerializeField] string heartBodyFormat;
        [SerializeField] string heartContinue;

        [Header("Descansar (Marco 8b, E3, D-151…D-154)")]
        [Tooltip("O botão da barra de baixo, o do modal e o da tela do coração.")]
        [SerializeField] string restLabel;
        [SerializeField] string restTitle;
        [Tooltip("{0} = o dia de agora.")]
        [SerializeField] string dayFormat;
        [SerializeField] string restLevelLabel;
        [SerializeField] string restTimeLabel;
        [SerializeField] string restShardsLabel;
        [SerializeField] string restTakeLabel;
        [Tooltip("{0} = lascas vermelhas que o próximo nível somaria.")]
        [SerializeField] string restNextFormat;
        [Tooltip("No último nível, quando não há próximo.")]
        [SerializeField] string restNoNext;
        [TextArea, SerializeField] string restNote;
        [SerializeField] string restContinue;

        [Header("Resumo do dia (Marco 8b, E3, D-154)")]
        [Tooltip("{0} = o dia que terminou.")]
        [SerializeField] string summaryTitleFormat;
        [SerializeField] string summaryLevelLabel;
        [SerializeField] string summaryTimeLabel;
        [SerializeField] string summaryGainedLabel;
        [Tooltip("{0} = lascas vermelhas ganhas no descanso.")]
        [SerializeField] string summaryGainedFormat;
        [SerializeField] string summaryBankLabel;
        [SerializeField] string summaryStart;
        [Tooltip("O botão que abre a loja na aba das permanentes (E4, D-154).")]
        [SerializeField] string summaryPermanents;

        [Header("Bem-vindo de volta (M9, E2, D-197)")]
        [SerializeField] string offlineTitle;
        [SerializeField] string offlineAwayLabel;
        [SerializeField] string offlineCountedLabel;
        [Tooltip("{0} = o tempo que contou (o teto, 6 h).")]
        [SerializeField] string offlineCountedFormat;
        [SerializeField] string offlineCrystalsLabel;
        [SerializeField] string offlineBroughtLabel;
        [Tooltip("{0} = as lascas que ele trouxe.")]
        [SerializeField] string offlineShardsFormat;
        [Tooltip("{0} = o nível em que ele minerou.")]
        [TextArea, SerializeField] string offlineNoteFormat;
        [Tooltip("{0} = o teto (6 h).")]
        [TextArea, SerializeField] string offlineCappedNoteFormat;
        [SerializeField] string offlineCollect;

        public string CriticalLabel => criticalLabel;
        public string ComboFormat => comboFormat;
        public string MilestoneFormat => milestoneFormat;
        public string CaveBannerFormat => caveBannerFormat;
        public string HeartTitle => heartTitle;
        public string HeartBodyFormat => heartBodyFormat;
        public string HeartContinue => heartContinue;

        public string RestLabel => restLabel;
        public string RestTitle => restTitle;
        public string DayFormat => dayFormat;
        public string RestLevelLabel => restLevelLabel;
        public string RestTimeLabel => restTimeLabel;
        public string RestShardsLabel => restShardsLabel;
        public string RestTakeLabel => restTakeLabel;
        public string RestNextFormat => restNextFormat;
        public string RestNoNext => restNoNext;
        public string RestNote => restNote;
        public string RestContinue => restContinue;

        public string SummaryTitleFormat => summaryTitleFormat;
        public string SummaryLevelLabel => summaryLevelLabel;
        public string SummaryTimeLabel => summaryTimeLabel;
        public string SummaryGainedLabel => summaryGainedLabel;
        public string SummaryGainedFormat => summaryGainedFormat;
        public string SummaryBankLabel => summaryBankLabel;
        public string SummaryStart => summaryStart;
        public string SummaryPermanents => summaryPermanents;

        public string OfflineTitle => offlineTitle;
        public string OfflineAwayLabel => offlineAwayLabel;
        public string OfflineCountedLabel => offlineCountedLabel;
        public string OfflineCountedFormat => offlineCountedFormat;
        public string OfflineCrystalsLabel => offlineCrystalsLabel;
        public string OfflineBroughtLabel => offlineBroughtLabel;
        public string OfflineShardsFormat => offlineShardsFormat;
        public string OfflineNoteFormat => offlineNoteFormat;
        public string OfflineCappedNoteFormat => offlineCappedNoteFormat;
        public string OfflineCollect => offlineCollect;
    }
}
