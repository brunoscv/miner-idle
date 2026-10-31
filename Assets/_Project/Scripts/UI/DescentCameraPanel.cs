using System;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// Controles da câmera da descida no DBG do jogo (M6, E3; D-61): os − e + da E1 para altura, inclinação, distância,
    /// campo de visão e largura do corredor (mais fáceis de acertar no celular que um slider), a intensidade dos efeitos
    /// (E4), Resolução (270/360/aparelho),
    /// Padrão (F1, D-69) e Fechar. Uma linha mostra onde o anão fica na tela e quanto chão se vê à frente dele, para passar os
    /// números adiante. Abre pelo botão "Câmera" do DBG e fica aberto com o DBG fechado, para ajustar olhando o jogo.
    /// Os textos (rótulos e modelos) ficam na cena. Só no Editor e em Development Build.
    /// </summary>
    public sealed class DescentCameraPanel : MonoBehaviour
    {
        /// <summary>A ordem das linhas de ajuste no painel.</summary>
        public enum Setting
        {
            Height,
            Tilt,
            Distance,
            FieldOfView,
            CorridorWidth,
            EffectIntensity,
        }

        [Serializable]
        public sealed class Row
        {
            public TMP_Text label;
            [Tooltip("{0} = valor (formato pt-BR).")]
            public string format;
            public Button minus;
            public Button plus;
        }

        /// <summary>Passo de cada ajuste, na ordem de <see cref="Setting"/>.</summary>
        static readonly float[] Steps = { 0.1f, 1f, 0.1f, 1f, 0.1f, 0.25f };

        /// <summary>As resoluções do botão Resolução: a de referência (D-14), a do plano B e a do aparelho (0).</summary>
        static readonly int[] Resolutions = { 270, 360, 0 };

        const float RefreshSeconds = 0.5f;

        [SerializeField] DescentStage stage;
        [SerializeField] MiningRunner runner;
        [SerializeField] MiningFeedbackView feedback;
        [SerializeField] GameObject content;
        [SerializeField] Row[] rows = new Row[6];

        [SerializeField] TMP_Text readout;
        [Tooltip("{0} cabeça (% do topo) · {1} pés · {2} altura do anão (% da tela) · {3} chão à frente (m) · " +
                 "{4} segundos até o cristal · {5}×{6} resolução do render")]
        [SerializeField, TextArea(2, 5)] string readoutFormat;

        [SerializeField] Button resolution;
        [SerializeField] Button resetFraming;
        [SerializeField] Button close;
        [Tooltip("Liga e desliga o estagiário ao lado do anão (M1b E7; o de verdade é do M7).")]
        [SerializeField] Button intern;
        [Tooltip("Troca o anão pelas folhas com menos pixels, para comparar (não fica salvo).")]
        [SerializeField] Button lowDwarf;
        [Tooltip("Passa pelas 10 cavernas (M1b E8, D-102); o botão mostra o nome da caverna (não fica salvo).")]
        [SerializeField, FormerlySerializedAs("floor")] Button biome;

        UnityAction[] _minus;
        UnityAction[] _plus;
        float _refresh;
        TMP_Text _biomeLabel;

        public bool IsOpen => content.activeSelf;

        void Awake()
        {
            if (!Debug.isDebugBuild)
            {
                gameObject.SetActive(false); // evita o OnEnable antes da destruição
                Destroy(gameObject);
                return;
            }

            // Os delegados são criados uma vez, para poder tirar as mesmas instâncias no OnDisable.
            _minus = new UnityAction[rows.Length];
            _plus = new UnityAction[rows.Length];
            for (int i = 0; i < rows.Length; i++)
            {
                var setting = (Setting)i;
                _minus[i] = () => Change(setting, -1);
                _plus[i] = () => Change(setting, +1);
            }

            if (biome != null) _biomeLabel = biome.GetComponentInChildren<TMP_Text>();
            content.SetActive(false);
        }

        void OnEnable()
        {
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].minus.onClick.AddListener(_minus[i]);
                rows[i].plus.onClick.AddListener(_plus[i]);
            }
            resolution.onClick.AddListener(NextResolution);
            resetFraming.onClick.AddListener(ResetFraming);
            close.onClick.AddListener(Toggle);
            if (intern != null) intern.onClick.AddListener(ToggleIntern);
            if (lowDwarf != null) lowDwarf.onClick.AddListener(ToggleLowDwarf);
            if (biome != null) biome.onClick.AddListener(NextBiome);
        }

        void OnDisable()
        {
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].minus.onClick.RemoveListener(_minus[i]);
                rows[i].plus.onClick.RemoveListener(_plus[i]);
            }
            resolution.onClick.RemoveListener(NextResolution);
            resetFraming.onClick.RemoveListener(ResetFraming);
            close.onClick.RemoveListener(Toggle);
            if (intern != null) intern.onClick.RemoveListener(ToggleIntern);
            if (lowDwarf != null) lowDwarf.onClick.RemoveListener(ToggleLowDwarf);
            if (biome != null) biome.onClick.RemoveListener(NextBiome);
        }

        void Update()
        {
            if (!content.activeSelf) return;
            _refresh -= Time.unscaledDeltaTime;
            if (_refresh > 0f) return;
            _refresh = RefreshSeconds;
            Refresh();
        }

        /// <summary>Abre ou fecha (o botão "Câmera" do DBG e o "Fechar" daqui).</summary>
        void ToggleIntern() => stage.ShowIntern = !stage.ShowIntern;

        void ToggleLowDwarf() => stage.UseLowDwarf = !stage.UseLowDwarf;

        void NextBiome()
        {
            stage.NextBiome();
            _refresh = 0f;
        }

        public void Toggle()
        {
            content.SetActive(!content.activeSelf);
            _refresh = 0f;
        }

        void Change(Setting setting, int direction)
        {
            float delta = Steps[(int)setting] * direction;
            var framing = stage.Framing;
            switch (setting)
            {
                case Setting.Height: framing.height = Round(framing.height + delta); break;
                case Setting.Tilt: framing.tilt = Round(framing.tilt + delta); break;
                case Setting.Distance: framing.distance = Round(framing.distance + delta); break;
                case Setting.FieldOfView: framing.fieldOfView = Round(framing.fieldOfView + delta); break;
                case Setting.CorridorWidth: framing.corridorWidth = Round(framing.corridorWidth + delta); break;
                case Setting.EffectIntensity: feedback.Intensity = feedback.Intensity + delta; break;
            }
            if (setting != Setting.EffectIntensity) stage.Framing = framing;
            _refresh = 0f;
        }

        /// <summary>Uma casa decimal: somar 0,1 dez vezes não pode virar 2,3999 (a intensidade anda em 0,25, exata em float).</summary>
        static float Round(float value) => Mathf.Round(value * 10f) / 10f;

        void NextResolution()
        {
            int current = Array.IndexOf(Resolutions, stage.InternalWidth);
            stage.InternalWidth = Resolutions[(current + 1) % Resolutions.Length];
            _refresh = 0f;
        }

        void ResetFraming()
        {
            stage.ResetFraming();
            _refresh = 0f;
        }

        void Refresh()
        {
            var framing = stage.Framing;
            float[] values =
            {
                framing.height, framing.tilt, framing.distance, framing.fieldOfView, framing.corridorWidth, feedback.Intensity,
            };
            for (int i = 0; i < rows.Length && i < values.Length; i++)
                rows[i].label.text = string.Format(rows[i].format, NumberFormatter.Format(values[i]));

            float aspect = stage.Aspect;
            float head = framing.FromTop(DescentFraming.DwarfHeight, framing.distance, aspect);
            float feet = framing.FromTop(0f, framing.distance, aspect);
            float ground = framing.GroundAhead(aspect);

            // Corrida e alcance da regra, em metros (D-58).
            var simulation = runner.Simulation;
            double speed = simulation != null ? simulation.Stats.WalkSpeed * stage.MetersPerUnit : 0;
            double reach = simulation != null ? simulation.Rules.MiningRange * stage.MetersPerUnit : 0;
            double seconds = speed > 0 ? Math.Max(0, ground - reach) / speed : 0;

            if (_biomeLabel != null && stage.Biome != null) _biomeLabel.text = stage.Biome.DisplayName;

            readout.text = string.Format(readoutFormat,
                Mathf.RoundToInt(head * 100f), Mathf.RoundToInt(feet * 100f), Mathf.RoundToInt((feet - head) * 100f),
                NumberFormatter.Format(Math.Round(ground, 1)), NumberFormatter.Format(Math.Round(seconds, 1)),
                stage.TargetWidth, stage.TargetHeight);
        }
    }
}
