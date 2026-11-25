using MinersIdle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// DBG → Efeitos → "Efeitos: limpos/antigos" (M9i #24, D-346): troca os efeitos do golpe entre o jeito limpo (faíscas no
    /// ritmo do golpe, crítico discreto, tetos; o padrão) e o de antes, para comparar no aparelho. Não fica gravado: toda
    /// abertura começa no limpo. Está dentro do painel de debug: no release some junto com ele.
    /// </summary>
    public sealed class DebugCalmEffects : MonoBehaviour
    {
        [SerializeField] Button toggle;
        [SerializeField] MiningFeedbackView feedback;
        [Tooltip("{0} = limpos ou antigos.")]
        [SerializeField] string labelFormat = "Efeitos: {0}";
        [SerializeField] string calmName = "limpos";
        [SerializeField] string oldName = "antigos";

        TMP_Text _label;

        void Awake()
        {
            if (toggle != null) _label = toggle.GetComponentInChildren<TMP_Text>();
            Refresh();
        }

        void OnEnable()
        {
            if (toggle != null) toggle.onClick.AddListener(Toggle);
        }

        void OnDisable()
        {
            if (toggle != null) toggle.onClick.RemoveListener(Toggle);
        }

        public void Toggle()
        {
            if (feedback == null) return;
            feedback.Calm = !feedback.Calm;
            Refresh();
        }

        void Refresh()
        {
            if (_label != null && feedback != null)
                _label.text = string.Format(labelFormat, feedback.Calm ? calmName : oldName);
        }
    }
}
