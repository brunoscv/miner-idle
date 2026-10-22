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

        public string CriticalLabel => criticalLabel;
        public string ComboFormat => comboFormat;
        public string MilestoneFormat => milestoneFormat;
    }
}
