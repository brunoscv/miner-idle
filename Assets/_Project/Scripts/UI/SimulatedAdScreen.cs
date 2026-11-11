using System;
using MinersIdle.Core;
using MinersIdle.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// O anúncio simulado (M9b E5; D-218, D-227…D-229; mockup aprovado pelo Bruno em 2026-11-10,
    /// <c>tools/ci/out/m9b-e5-mockup.png</c>): o modal "VISUALIZANDO AD" com a telinha no centro, o tempo que falta e a
    /// recompensa. Os 30 s são de tempo real de tela: não aceleram com o DBG e param com o app minimizado (D-229). Sem saída
    /// (D-228): ao fim, só o RECOMPENSA, que fecha e entrega a recompensa a quem pediu. Como a loja, escurece e bloqueia o
    /// mapa sem pausar a mineração (D-227). Não vai para o save: fechar o app no meio perde o anúncio. Textos no
    /// <see cref="BuffCatalog"/>.
    /// </summary>
    public sealed class SimulatedAdScreen : MonoBehaviour, IRewardedAd
    {
        [SerializeField] MiningRunner runner;
        [SerializeField] BuffCatalog catalog;
        [Tooltip("O fundo que escurece (bloqueia o mapa) e o painel: ligado só com o anúncio na tela.")]
        [SerializeField] GameObject screen;
        [SerializeField] TMP_Text title;
        [Tooltip("A telinha do centro, que pulsa enquanto o anúncio passa.")]
        [SerializeField] RectTransform icon;
        [Tooltip("\"O anúncio termina em\" e o número: só enquanto falta tempo.")]
        [SerializeField] GameObject waiting;
        [SerializeField] TMP_Text waitLabel;
        [SerializeField] TMP_Text seconds;
        [SerializeField] TMP_Text doneLabel;
        [Tooltip("O preenchimento da barra: a âncora da direita anda com o tempo que passou.")]
        [SerializeField] RectTransform barFill;
        [SerializeField] TMP_Text rewardLine;
        [SerializeField] Button rewardButton;
        [SerializeField] TMP_Text rewardLabel;

        [Header("Tempo e cores")]
        [Tooltip("Quanto o anúncio simulado dura, em segundos de tela (D-218: 30).")]
        [SerializeField, Min(0.1f)] float watchSeconds = 30f;
        [Tooltip("O máximo que um quadro desconta: a volta de um app minimizado não pula o tempo (D-229).")]
        [SerializeField, Min(0.01f)] float maxStep = 0.1f;
        [SerializeField] Color secondsColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        [SerializeField] Color endingColor = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        [SerializeField] float endingSeconds = 5f;
        [Tooltip("Quanto a telinha cresce no pulso (0,06 = 6%).")]
        [SerializeField, Range(0f, 0.3f)] float iconPulse = 0.06f;

        Action _onRewarded;
        float _left;
        int _shownSeconds = -1;

        public bool IsShowing => screen.activeSelf;
        /// <summary>O tempo acabou e o RECOMPENSA está na tela.</summary>
        public bool IsDone => IsShowing && _left <= 0f;
        public float SecondsLeft => _left;
        public float WatchSeconds => watchSeconds;
        public string SecondsText => seconds.text;
        public string RewardText => rewardLine.text;

        void Awake()
        {
            title.text = catalog.AdTitle;
            waitLabel.text = catalog.AdWaitLabel;
            doneLabel.text = catalog.AdDone;
            rewardLabel.text = catalog.AdReward;
            screen.SetActive(false);
        }

        void OnEnable() => rewardButton.onClick.AddListener(Claim);
        void OnDisable() => rewardButton.onClick.RemoveListener(Claim);

        /// <summary>Abre o anúncio; a recompensa vai para <paramref name="onRewarded"/> no RECOMPENSA.</summary>
        public void Show(Action onRewarded)
        {
            if (IsShowing) return;
            _onRewarded = onRewarded;
            _left = watchSeconds;
            _shownSeconds = -1;
            var ad = runner != null && runner.Simulation != null ? runner.Simulation.Strike.Ad : AdRules.None();
            rewardLine.text = string.Format(catalog.AdRewardFormat, NumberFormatter.Format(ad.SpeedDivisor),
                                            NumberFormatter.Format(ad.Seconds));
            screen.SetActive(true);
            Refresh();
        }

        /// <summary>Atalho do debug "Pular espera" (D-234): o RECOMPENSA já.</summary>
        public void SkipWait()
        {
            if (!IsShowing) return;
            _left = 0f;
            Refresh();
        }

        /// <summary>RECOMPENSA: fecha e entrega (só depois do tempo, D-228).</summary>
        public void Claim()
        {
            if (!IsDone) return;
            screen.SetActive(false);
            var rewarded = _onRewarded;
            _onRewarded = null;
            rewarded?.Invoke();
        }

        void Update()
        {
            if (!IsShowing) return;
            float wave = Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f);
            float scale = 1f + iconPulse * (_left > 0f ? wave : 0.5f + 0.5f * wave);
            icon.localScale = new Vector3(scale, scale, 1f);
            if (_left <= 0f) return;
            _left = Mathf.Max(0f, _left - Mathf.Min(Time.unscaledDeltaTime, maxStep));
            Refresh();
        }

        void Refresh()
        {
            bool done = _left <= 0f;
            waiting.SetActive(!done);
            doneLabel.gameObject.SetActive(done);
            rewardButton.gameObject.SetActive(done);

            float passed = watchSeconds > 0f ? Mathf.Clamp01(1f - _left / watchSeconds) : 1f;
            var max = barFill.anchorMax;
            if (!Mathf.Approximately(max.x, passed)) barFill.anchorMax = new Vector2(passed, max.y);

            int whole = Mathf.CeilToInt(_left);
            if (whole == _shownSeconds) return;
            _shownSeconds = whole;
            seconds.SetText(catalog.SecondsFormat, (float)whole);
            seconds.color = whole <= endingSeconds ? endingColor : secondsColor;
        }
    }
}
