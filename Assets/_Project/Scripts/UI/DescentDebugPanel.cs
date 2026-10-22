using System;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// Painel de debug da cena da descida (M6, E1): os mesmos controles do simulador da proposta, em botões − e + (mais
    /// fáceis de acertar no celular que um slider), mais Estagiário, HUD, Pausar, Resolução, Padrão D-50 e Voltar ao jogo.
    /// Uma linha mostra onde o anão fica na tela e quanto chão se vê à frente dele, para o Bruno e o dev principal
    /// passarem os números. Os textos (rótulos e modelos) ficam na cena. Só no Editor e em Development Build.
    /// </summary>
    public sealed class DescentDebugPanel : MonoBehaviour
    {
        /// <summary>A ordem das linhas de ajuste no painel.</summary>
        public enum Setting
        {
            Height,
            Tilt,
            Distance,
            FieldOfView,
            CorridorWidth,
            RunSpeed,
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
        static readonly float[] Steps = { 0.1f, 1f, 0.1f, 1f, 0.1f, 0.1f };

        /// <summary>As resoluções do botão Resolução: a de referência (D-14), a do plano B e a do aparelho (0).</summary>
        static readonly int[] Resolutions = { 270, 360, 0 };

        const float RefreshSeconds = 0.5f;

        [SerializeField] DescentStage stage;
        [SerializeField] Button toggleButton;
        [SerializeField] GameObject content;
        [SerializeField] GameObject hud;
        [SerializeField] Row[] rows = new Row[6];

        [SerializeField] TMP_Text readout;
        [Tooltip("{0} cabeça (% do topo) · {1} pés · {2} altura do anão (% da tela) · {3} chão à frente (m) · " +
                 "{4} segundos até o cristal · {5}×{6} resolução do render · {7} fps")]
        [SerializeField, TextArea(2, 5)] string readoutFormat;

        [SerializeField] Button intern;
        [SerializeField] Button hudToggle;
        [SerializeField] Button pause;
        [SerializeField] Button resolution;
        [SerializeField] Button resetFraming;
        [SerializeField] Button back;

        UnityAction[] _minus;
        UnityAction[] _plus;
        float _refresh;
        int _frames;
        float _fpsTime;
        float _fps;

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

            content.SetActive(false);
        }

        void OnEnable()
        {
            toggleButton.onClick.AddListener(Toggle);
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].minus.onClick.AddListener(_minus[i]);
                rows[i].plus.onClick.AddListener(_plus[i]);
            }
            intern.onClick.AddListener(ToggleIntern);
            hudToggle.onClick.AddListener(ToggleHud);
            pause.onClick.AddListener(TogglePause);
            resolution.onClick.AddListener(NextResolution);
            resetFraming.onClick.AddListener(ResetFraming);
            back.onClick.AddListener(BackToGame);
        }

        void OnDisable()
        {
            toggleButton.onClick.RemoveListener(Toggle);
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i].minus.onClick.RemoveListener(_minus[i]);
                rows[i].plus.onClick.RemoveListener(_plus[i]);
            }
            intern.onClick.RemoveListener(ToggleIntern);
            hudToggle.onClick.RemoveListener(ToggleHud);
            pause.onClick.RemoveListener(TogglePause);
            resolution.onClick.RemoveListener(NextResolution);
            resetFraming.onClick.RemoveListener(ResetFraming);
            back.onClick.RemoveListener(BackToGame);
        }

        void Update()
        {
            _frames++;
            _fpsTime += Time.unscaledDeltaTime;
            if (_fpsTime >= RefreshSeconds)
            {
                _fps = _frames / _fpsTime;
                _frames = 0;
                _fpsTime = 0f;
            }

            if (!content.activeSelf) return;
            _refresh -= Time.unscaledDeltaTime;
            if (_refresh > 0f) return;
            _refresh = RefreshSeconds;
            Refresh();
        }

        void Toggle()
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
                case Setting.RunSpeed: stage.RunSpeed = Round(stage.RunSpeed + delta); break;
            }
            if (setting != Setting.RunSpeed) stage.Framing = framing;
            _refresh = 0f;
        }

        /// <summary>Uma casa decimal: somar 0,1 dez vezes não pode virar 2,3999.</summary>
        static float Round(float value) => Mathf.Round(value * 10f) / 10f;

        void ToggleIntern() => stage.ShowIntern = !stage.ShowIntern;
        void ToggleHud() => hud.SetActive(!hud.activeSelf);
        void TogglePause() => stage.Paused = !stage.Paused;

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

        /// <summary>Volta pelo Bootstrap, que reabre o jogo com o save (como ao abrir o app).</summary>
        static void BackToGame() => SceneManager.LoadScene(GameBootstrap.BootstrapSceneName);

        void Refresh()
        {
            var framing = stage.Framing;
            float[] values =
            {
                framing.height, framing.tilt, framing.distance, framing.fieldOfView, framing.corridorWidth, stage.RunSpeed,
            };
            for (int i = 0; i < rows.Length && i < values.Length; i++)
                rows[i].label.text = string.Format(rows[i].format, NumberFormatter.Format(values[i]));

            float aspect = stage.Aspect;
            float head = framing.FromTop(DescentFraming.DwarfHeight, framing.distance, aspect);
            float feet = framing.FromTop(0f, framing.distance, aspect);
            float ground = framing.GroundAhead(aspect);
            float seconds = stage.RunSpeed > 0f ? Mathf.Max(0f, ground - (float)DescentPreview.Reach) / stage.RunSpeed : 0f;
            readout.text = string.Format(readoutFormat,
                Mathf.RoundToInt(head * 100f), Mathf.RoundToInt(feet * 100f), Mathf.RoundToInt((feet - head) * 100f),
                NumberFormatter.Format(Math.Round(ground, 1)), NumberFormatter.Format(Math.Round(seconds, 1)),
                stage.TargetWidth, stage.TargetHeight, Mathf.RoundToInt(_fps));
        }
    }
}
