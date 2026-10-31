using UnityEngine;

namespace MinersIdle.Game
{
    /// <summary>
    /// Textos do feedback de mineração (M5, E3) e da descida (M8: a faixa da caverna, a tela do coração da montanha), em
    /// pt-BR e em dados. Asset do jogo: <c>Data/FeedbackTexts</c>.
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

        public string CriticalLabel => criticalLabel;
        public string ComboFormat => comboFormat;
        public string MilestoneFormat => milestoneFormat;
        public string CaveBannerFormat => caveBannerFormat;
        public string HeartTitle => heartTitle;
        public string HeartBodyFormat => heartBodyFormat;
        public string HeartContinue => heartContinue;
    }
}
