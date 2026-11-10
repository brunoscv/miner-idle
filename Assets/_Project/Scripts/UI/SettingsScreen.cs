using System.Text;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// A tela AJUSTES (M9, E4, D-198; mockup aprovado pelo Bruno em 2026-11-10): volume dos efeitos, tremor da tela e
    /// vibração, que só trocam os valores do <see cref="GameSettings"/> (quem obedece é o <c>SettingsEffects</c>); os
    /// Créditos (a lista do <c>CREDITS.md</c>, em dados); e o Apagar save, em dois passos: o botão abre a confirmação e só
    /// o APAGAR dela chama o <see cref="MiningRunner.ResetProgress"/>. Os ajustes ficam (arquivo à parte, D-200).
    ///
    /// Abre pelo botão AJUSTES do HUD (que deixou de ser "Em breve", D-67). Como a loja, escurece e bloqueia o mapa sem
    /// pausar a mineração. Fechar grava os ajustes (o volume só é gravado aqui, não a cada passo do deslizador). Textos em
    /// dados.
    /// </summary>
    public sealed class SettingsScreen : MonoBehaviour
    {
        /// <summary>Passos do deslizador: de 5% em 5%.</summary>
        public const int VolumeSteps = 20;

        [SerializeField] MiningRunner runner;
        [SerializeField] GameSettings settings;
        [SerializeField] FeedbackTexts texts;
        [Tooltip("Opcional: toca um golpe ao mexer no volume, para ouvir como ficou.")]
        [SerializeField] SoundBoard soundBoard;
        [Tooltip("O botão AJUSTES do HUD.")]
        [SerializeField] Button openButton;

        [Header("AJUSTES")]
        [SerializeField] GameObject screen;
        [SerializeField] TMP_Text title;
        [SerializeField] Button closeButton;
        [SerializeField] TMP_Text volumeLabel;
        [SerializeField] Slider volumeSlider;
        [SerializeField] TMP_Text volumeValue;
        [SerializeField] TMP_Text shakeLabel;
        [SerializeField] Button shakeButton;
        [SerializeField] TMP_Text vibrationLabel;
        [SerializeField] Button vibrationButton;
        [SerializeField] Sprite toggleOn;
        [SerializeField] Sprite toggleOff;
        [SerializeField] Button creditsButton;
        [SerializeField] TMP_Text creditsLabel;
        [SerializeField] Button deleteButton;
        [SerializeField] TMP_Text deleteLabel;

        [Header("Confirmação do Apagar save")]
        [SerializeField] GameObject confirm;
        [SerializeField] TMP_Text confirmTitle;
        [SerializeField] TMP_Text confirmBody;
        [SerializeField] TMP_Text confirmWarning;
        [SerializeField] Button cancelButton;
        [SerializeField] TMP_Text cancelLabel;
        [SerializeField] Button confirmButton;
        [SerializeField] TMP_Text confirmLabel;

        [Header("Créditos")]
        [SerializeField] GameObject credits;
        [SerializeField] TMP_Text creditsTitle;
        [SerializeField] Button creditsCloseButton;
        [SerializeField] TMP_Text creditsList;
        [SerializeField] Button backButton;
        [SerializeField] TMP_Text backLabel;
        [Tooltip("A cor dos títulos dos grupos nos Créditos (os itens ficam na cor do texto).")]
        [SerializeField] Color creditsHeaderColor = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        [Tooltip("O recuo dos itens dos Créditos em relação aos títulos, em pixels da tela de referência.")]
        [SerializeField, Min(0f)] float creditsIndent = 6f;

        public bool IsOpen => screen.activeSelf;
        public bool IsConfirmOpen => confirm.activeSelf;
        public bool IsCreditsOpen => credits.activeSelf;
        public string VolumeText => volumeValue.text;
        public bool ShowsShakeOn => ((Image)shakeButton.targetGraphic).sprite == toggleOn;
        public bool ShowsVibrationOn => ((Image)vibrationButton.targetGraphic).sprite == toggleOn;
        public string CreditsText => creditsList.text;

        void Awake()
        {
            title.text = texts.SettingsTitle;
            volumeLabel.text = texts.SettingsVolumeLabel;
            shakeLabel.text = texts.SettingsShakeLabel;
            vibrationLabel.text = texts.SettingsVibrationLabel;
            creditsLabel.text = texts.SettingsCredits;
            deleteLabel.text = texts.SettingsDeleteSave;
            confirmTitle.text = texts.DeleteTitle;
            confirmBody.text = texts.DeleteBody;
            confirmWarning.text = texts.DeleteWarning;
            cancelLabel.text = texts.DeleteCancel;
            confirmLabel.text = texts.DeleteConfirm;
            creditsTitle.text = texts.CreditsTitle;
            backLabel.text = texts.CreditsBack;
            creditsList.text = CreditsMarkup();

            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = VolumeSteps;
            volumeSlider.wholeNumbers = true;
            screen.SetActive(false);
            confirm.SetActive(false);
            credits.SetActive(false);
        }

        void OnEnable()
        {
            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            volumeSlider.onValueChanged.AddListener(OnVolumeSlider);
            shakeButton.onClick.AddListener(ToggleShake);
            vibrationButton.onClick.AddListener(ToggleVibration);
            creditsButton.onClick.AddListener(OpenCredits);
            deleteButton.onClick.AddListener(AskDelete);
            cancelButton.onClick.AddListener(CancelDelete);
            confirmButton.onClick.AddListener(ConfirmDelete);
            creditsCloseButton.onClick.AddListener(Close);
            backButton.onClick.AddListener(CloseCredits);
            settings.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            openButton.onClick.RemoveListener(Open);
            closeButton.onClick.RemoveListener(Close);
            volumeSlider.onValueChanged.RemoveListener(OnVolumeSlider);
            shakeButton.onClick.RemoveListener(ToggleShake);
            vibrationButton.onClick.RemoveListener(ToggleVibration);
            creditsButton.onClick.RemoveListener(OpenCredits);
            deleteButton.onClick.RemoveListener(AskDelete);
            cancelButton.onClick.RemoveListener(CancelDelete);
            confirmButton.onClick.RemoveListener(ConfirmDelete);
            creditsCloseButton.onClick.RemoveListener(Close);
            backButton.onClick.RemoveListener(CloseCredits);
            settings.Changed -= Refresh;
        }

        public void Open()
        {
            Refresh();
            confirm.SetActive(false);
            credits.SetActive(false);
            screen.SetActive(true);
        }

        /// <summary>Fecha tudo (o X) e grava os ajustes.</summary>
        public void Close()
        {
            screen.SetActive(false);
            confirm.SetActive(false);
            credits.SetActive(false);
            settings.Flush();
        }

        /// <summary>O deslizador em passos de 5% (0 a <see cref="VolumeSteps"/>).</summary>
        public void SetVolumeStep(int step) => volumeSlider.value = Mathf.Clamp(step, 0, VolumeSteps);

        public void ToggleShake() => settings.ScreenShake = !settings.ScreenShake;
        public void ToggleVibration() => settings.Vibration = !settings.Vibration;

        public void OpenCredits()
        {
            screen.SetActive(false);
            credits.SetActive(true);
        }

        /// <summary>VOLTAR dos Créditos: de volta aos AJUSTES.</summary>
        public void CloseCredits()
        {
            credits.SetActive(false);
            screen.SetActive(true);
        }

        /// <summary>APAGAR SAVE: o primeiro passo só pergunta.</summary>
        public void AskDelete() => confirm.SetActive(true);

        public void CancelDelete() => confirm.SetActive(false);

        /// <summary>APAGAR da confirmação: o progresso volta ao nível 1, os ajustes ficam, e a tela fecha no jogo novo.</summary>
        public void ConfirmDelete()
        {
            runner.ResetProgress();
            Close();
        }

        void OnVolumeSlider(float step)
        {
            float volume = step / VolumeSteps;
            bool changed = !Mathf.Approximately(volume, settings.EffectsVolume);
            settings.EffectsVolume = volume;
            if (changed && soundBoard != null && screen.activeInHierarchy) soundBoard.Play(Sfx.Hit);
        }

        /// <summary>Os controles mostram os ajustes de agora (também quando mudam por fora, como no DBG).</summary>
        void Refresh()
        {
            int step = Mathf.RoundToInt(settings.EffectsVolume * VolumeSteps);
            volumeSlider.SetValueWithoutNotify(step);
            volumeValue.text = string.Format(texts.SettingsVolumeFormat, Mathf.RoundToInt(settings.EffectsVolume * 100f));
            ((Image)shakeButton.targetGraphic).sprite = settings.ScreenShake ? toggleOn : toggleOff;
            ((Image)vibrationButton.targetGraphic).sprite = settings.Vibration ? toggleOn : toggleOff;
        }

        /// <summary>A lista dos Créditos num texto só: os títulos em âmbar, os itens recuados.</summary>
        string CreditsMarkup()
        {
            var markup = new StringBuilder();
            string header = ColorUtility.ToHtmlStringRGB(creditsHeaderColor);
            string indent = creditsIndent.ToString(System.Globalization.CultureInfo.InvariantCulture);
            foreach (var line in texts.Credits)
            {
                if (markup.Length > 0) markup.Append('\n');
                if (line.Header) markup.Append("<color=#").Append(header).Append('>').Append(line.Text).Append("</color>");
                else markup.Append("<indent=").Append(indent).Append('>').Append(line.Text).Append("</indent>");
            }
            return markup.ToString();
        }
    }
}
