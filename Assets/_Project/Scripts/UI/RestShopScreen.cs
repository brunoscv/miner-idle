using System.Collections.Generic;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// A tela do descanso (M9d E2, D-265, GDD v8.2 §4.2): depois do DESCANSAR, o ZZZ (1,5 s; um toque pula) e a loja das
    /// permanentes com o jogo parado. No alto, o dia que terminou e o banco de lascas vermelhas; o andar, o tempo e as
    /// ganhas; a lista das permanentes (as do limite no fim); e INICIAR, que começa a run nova com o que foi comprado.
    /// Fundo: o jogo desfocado, escolhido pelo Bruno (D-280); a preta (a do GDD) segue no DBG → Dia para comparar, e é o
    /// que sobra quando a foto não sai (sem placa de vídeo). Abre pelo evento
    /// <see cref="MiningRunner.Rested"/>; quando o descanso foi o atalho do DBG ou o jogo reabriu descansando, sem o ZZZ.
    /// Os números do dia vêm do <see cref="RestSummary"/>; o banco, das permanentes. Textos em dados.
    /// </summary>
    public sealed class RestShopScreen : MiningViewBase
    {
        public enum Background { Black, Blurred }

        [SerializeField] FeedbackTexts texts;
        [SerializeField] ShopCatalog catalog;
        [SerializeField] PermanentRowView rowPrefab;
        [SerializeField] Transform rowsParent;

        [Tooltip("Tudo o que aparece com a tela aberta (o fundo, o ZZZ e o painel).")]
        [SerializeField] GameObject screen;

        [Header("Fundo (D-279, D-280)")]
        [Tooltip("A desfocada, escolhida pelo Bruno (D-280); o DBG → Dia troca para a preta.")]
        [SerializeField] Background background = Background.Blurred;
        [Tooltip("O jogo desfocado: uma foto pequena da câmera, ampliada com filtro.")]
        [SerializeField] RawImage blurred;
        [Tooltip("A câmera do mundo, para a foto do fundo desfocado.")]
        [SerializeField] Camera worldCamera;
        [Tooltip("Quantas vezes a foto é menor que a tela: quanto maior, mais desfocado.")]
        [SerializeField, Min(2)] int blurDivisor = 24;

        [Header("ZZZ")]
        [SerializeField] GameObject zzz;
        [Tooltip("A tela toda do ZZZ: um toque pula.")]
        [SerializeField] Button zzzSkip;
        [SerializeField] TMP_Text[] zzzLetters;
        [SerializeField, Min(0f)] float zzzSeconds = 1.5f;
        [Tooltip("Quanto cada Z sobe enquanto aparece, em px da referência.")]
        [SerializeField] float zzzRise = 12f;

        [Header("Painel")]
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text bankValue;
        [SerializeField] TMP_Text levelLabel;
        [SerializeField] TMP_Text levelValue;
        [SerializeField] TMP_Text timeLabel;
        [SerializeField] TMP_Text timeValue;
        [SerializeField] TMP_Text gainedLabel;
        [SerializeField] TMP_Text gainedValue;
        [SerializeField] TMP_Text note;
        [SerializeField] Button startButton;
        [SerializeField] TMP_Text startLabel;
        [Tooltip("A rolagem da lista: volta ao topo a cada descanso.")]
        [SerializeField] ScrollRect scroll;

        readonly List<PermanentRowView> _rows = new List<PermanentRowView>();
        Vector2[] _zzzHome;
        float _zzzElapsed;
        RenderTexture _blurTexture;

        public bool IsOpen => screen.activeSelf;
        public bool IsZzzShown => zzz.activeSelf;
        public bool IsPanelShown => panel.activeSelf;
        public Background BackgroundMode => background;
        public bool IsBlurredShown => blurred != null && blurred.gameObject.activeSelf;
        public IReadOnlyList<PermanentRowView> Rows => _rows;
        public string TitleText => title.text;
        public string BankText => bankValue.text;
        public string LevelText => levelValue.text;
        public string GainedText => gainedValue.text;

        void Awake()
        {
            levelLabel.text = texts.SummaryLevelLabel;
            timeLabel.text = texts.SummaryTimeLabel;
            gainedLabel.text = texts.SummaryGainedLabel;
            note.text = texts.RestShopNote;
            startLabel.text = texts.RestShopStart;
            foreach (var entry in catalog.PermanentEntries)
            {
                var row = Instantiate(rowPrefab, rowsParent);
                row.Bind(catalog, entry, Buy);
                _rows.Add(row);
            }
            _zzzHome = new Vector2[zzzLetters.Length];
            for (int i = 0; i < zzzLetters.Length; i++) _zzzHome[i] = zzzLetters[i].rectTransform.anchoredPosition;
            screen.SetActive(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            startButton.onClick.AddListener(StartRun);
            zzzSkip.onClick.AddListener(SkipZzz);
            if (runner == null) return;
            runner.Rested += OnRested;
            if (runner.PendingRest.HasValue && !IsOpen) Open(runner.PendingRest.Value, zzzFirst: false);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            startButton.onClick.RemoveListener(StartRun);
            zzzSkip.onClick.RemoveListener(SkipZzz);
            if (runner != null) runner.Rested -= OnRested;
        }

        void OnDestroy() => ReleaseBlur();

        protected override void Subscribe(MiningSimulation simulation) => simulation.PermanentsChanged += Refresh;
        protected override void Unsubscribe(MiningSimulation simulation) => simulation.PermanentsChanged -= Refresh;

        /// <summary>Uma partida que não está descansando (o INICIAR, o save apagado): a tela some.</summary>
        protected override void Rebuild(MiningSimulation simulation)
        {
            if (!simulation.IsResting) Close();
            else Refresh();
        }

        void OnRested(RestSummary summary) => Open(summary, zzzFirst: !runner.RestedQuietly);

        /// <summary>Abre com o resumo do dia; com <paramref name="zzzFirst"/>, o ZZZ antes da loja.</summary>
        public void Open(RestSummary summary, bool zzzFirst)
        {
            title.text = string.Format(texts.SummaryTitleFormat, summary.Day);
            levelValue.text = summary.Level.ToString();
            timeValue.text = NumberFormatter.FormatDuration(summary.PlaySeconds);
            gainedValue.text = string.Format(texts.SummaryGainedFormat, NumberFormatter.FormatShards(summary.RedShardsGained));
            screen.SetActive(true);
            ApplyBackground();
            if (scroll != null) scroll.verticalNormalizedPosition = 1f;
            Refresh();

            if (zzzFirst && zzzSeconds > 0f)
            {
                _zzzElapsed = 0f;
                zzz.SetActive(true);
                panel.SetActive(false);
                AnimateZzz();
            }
            else ShowPanel();
        }

        public void Close()
        {
            if (!screen.activeSelf) return;
            screen.SetActive(false);
            ReleaseBlur();
        }

        /// <summary>Um toque no ZZZ: a loja já.</summary>
        public void SkipZzz() => ShowPanel();

        /// <summary>INICIAR: a run nova começa, montada com as permanentes de agora.</summary>
        public void StartRun()
        {
            Close();
            runner.StartRun();
        }

        /// <summary>O DBG → Dia (D-279): preta ⇄ desfocada; com a tela aberta, troca na hora.</summary>
        public void ToggleBackground()
        {
            background = background == Background.Black ? Background.Blurred : Background.Black;
            if (IsOpen) ApplyBackground();
        }

        void Update()
        {
            if (!IsOpen || !zzz.activeSelf) return;
            _zzzElapsed += Time.unscaledDeltaTime;
            if (_zzzElapsed >= zzzSeconds) ShowPanel();
            else AnimateZzz();
        }

        /// <summary>Os três Z entram um depois do outro, subindo e acendendo.</summary>
        void AnimateZzz()
        {
            int count = zzzLetters.Length;
            for (int i = 0; i < count; i++)
            {
                float start = zzzSeconds * 0.5f * i / Mathf.Max(1, count);
                float t = Mathf.Clamp01((_zzzElapsed - start) / (zzzSeconds * 0.4f));
                var letter = zzzLetters[i];
                letter.alpha = t;
                letter.rectTransform.anchoredPosition = _zzzHome[i] + new Vector2(0f, zzzRise * t);
            }
        }

        void ShowPanel()
        {
            zzz.SetActive(false);
            panel.SetActive(true);
        }

        void Buy(PermanentId id) => Simulation?.PurchasePermanent(id); // avisa PermanentsChanged

        void Refresh()
        {
            var simulation = Simulation;
            if (simulation == null || !IsOpen) return;
            bankValue.text = NumberFormatter.FormatShards(simulation.Permanents.State.RedShards);
            foreach (var row in _rows) row.Refresh(simulation.PreviewPermanent(row.Permanent));
            // As que ainda sobem primeiro, na ordem da tabela; as do limite no fim (D-155).
            int index = 0;
            foreach (var row in _rows)
                if (!row.IsMaxed) Place(row.transform, ref index);
            foreach (var row in _rows)
                if (row.IsMaxed) Place(row.transform, ref index);
        }

        static void Place(Transform row, ref int index)
        {
            if (row.GetSiblingIndex() != index) row.SetSiblingIndex(index);
            index++;
        }

        // ── O fundo desfocado ────────────────────────────────────────────────────────────────────────────────

        void ApplyBackground()
        {
            bool wantBlur = background == Background.Blurred && CaptureBlur();
            if (blurred != null && blurred.gameObject.activeSelf != wantBlur) blurred.gameObject.SetActive(wantBlur);
        }

        /// <summary>
        /// Uma foto da câmera do mundo, bem pequena e ampliada com filtro bilinear: o jogo atrás, desfocado, uma vez por
        /// descanso (o jogo está parado; nada de desfoque a cada quadro no celular). <c>false</c> sem placa de vídeo (o
        /// teste em batch) ou sem câmera: o fundo fica preto.
        /// </summary>
        bool CaptureBlur()
        {
            if (blurred == null) return false;
            if (_blurTexture != null) return true;
            var camera = worldCamera != null ? worldCamera : Camera.main;
            if (camera == null || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return false;

            int width = Mathf.Max(8, Screen.width / 4), height = Mathf.Max(8, Screen.height / 4);
            var shot = RenderTexture.GetTemporary(width, height, 24);
            var previous = camera.targetTexture;
            camera.targetTexture = shot;
            camera.Render();
            camera.targetTexture = previous;

            // Encolhe em passos (cada um tira a média de 4 pixels) para o desfoque ficar macio, sem serrilhado.
            var source = shot;
            int target = Mathf.Max(2, blurDivisor / 4);
            while (target > 1 && source.width > 8)
            {
                var half = RenderTexture.GetTemporary(Mathf.Max(4, source.width / 2), Mathf.Max(4, source.height / 2), 0);
                half.filterMode = FilterMode.Bilinear;
                Graphics.Blit(source, half);
                RenderTexture.ReleaseTemporary(source);
                source = half;
                target /= 2;
            }

            _blurTexture = new RenderTexture(source.width, source.height, 0) { filterMode = FilterMode.Bilinear };
            Graphics.Blit(source, _blurTexture);
            RenderTexture.ReleaseTemporary(source);
            blurred.texture = _blurTexture;
            return true;
        }

        void ReleaseBlur()
        {
            if (_blurTexture == null) return;
            if (blurred != null) blurred.texture = null;
            _blurTexture.Release();
            Destroy(_blurTexture);
            _blurTexture = null;
        }
    }
}
