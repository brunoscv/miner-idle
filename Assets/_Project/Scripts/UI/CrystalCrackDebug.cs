using MinersIdle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// DBG → Andar (M9f): "Barra de vida" liga a barra que saiu no v8 (D-301), para comparar com o cristal rachando; e
    /// "Estágios" faz os cristais trocarem de desenho a cada segundo, para conferir a arte. Não fica salvo. Está
    /// dentro do painel de debug: no release some junto com ele.
    /// </summary>
    public sealed class CrystalCrackDebug : MonoBehaviour
    {
        [SerializeField] DescentStage stage;
        [SerializeField] Button healthBarToggle;
        [SerializeField] Button stagesToggle;
        [Tooltip("{0} = sim ou não.")]
        [SerializeField] string healthBarFormat = "Barra de vida: {0}";
        [Tooltip("{0} = sim ou não.")]
        [SerializeField] string stagesFormat = "Estágios: {0}";
        [SerializeField] string yesName = "sim";
        [SerializeField] string noName = "não";

        TMP_Text _healthBarLabel, _stagesLabel;

        void Awake()
        {
            if (healthBarToggle != null) _healthBarLabel = healthBarToggle.GetComponentInChildren<TMP_Text>();
            if (stagesToggle != null) _stagesLabel = stagesToggle.GetComponentInChildren<TMP_Text>();
        }

        void OnEnable()
        {
            if (healthBarToggle != null) healthBarToggle.onClick.AddListener(ToggleHealthBar);
            if (stagesToggle != null) stagesToggle.onClick.AddListener(ToggleStages);
            Refresh();
        }

        void OnDisable()
        {
            if (healthBarToggle != null) healthBarToggle.onClick.RemoveListener(ToggleHealthBar);
            if (stagesToggle != null) stagesToggle.onClick.RemoveListener(ToggleStages);
        }

        public void ToggleHealthBar()
        {
            if (stage == null) return;
            stage.ShowHealthBar = !stage.ShowHealthBar;
            Refresh();
        }

        public void ToggleStages()
        {
            if (stage == null) return;
            stage.ShowCrackStages = !stage.ShowCrackStages;
            Refresh();
        }

        void Refresh()
        {
            if (stage == null) return;
            if (_healthBarLabel != null) _healthBarLabel.text = string.Format(healthBarFormat, stage.ShowHealthBar ? yesName : noName);
            if (_stagesLabel != null) _stagesLabel.text = string.Format(stagesFormat, stage.ShowCrackStages ? yesName : noName);
        }
    }
}
