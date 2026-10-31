using UnityEngine;

namespace MinersIdle.Game
{
    /// <summary>Textos do feedback de mineração (M5, E3), em pt-BR e em dados. Asset do jogo: <c>Data/FeedbackTexts</c>.</summary>
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

        public string CriticalLabel => criticalLabel;
        public string ComboFormat => comboFormat;
        public string MilestoneFormat => milestoneFormat;
        public string CaveBannerFormat => caveBannerFormat;
    }
}
