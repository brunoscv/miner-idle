using System;
using MinersIdle.Core;
using MinersIdle.Game;
using MinersIdle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// A janela do cristal raro (M9b E4; D-184…D-186, D-221…D-225; mockup aprovado pelo Bruno em 2026-11-10,
    /// <c>tools/ci/out/m9b-e4-mockup.png</c>): na metade de baixo da tela, as 2 cartas sorteadas, a barra dos 30 s e o
    /// "+N raro" da fila. Tocar numa carta escolhe; sem escolha, o Núcleo sorteia e a janela mostra a carta que saiu
    /// (destacada, a outra apagada) até o FECHAR. Como a loja, escurece e bloqueia os toques no mapa sem pausar o jogo
    /// (D-223). O tempo e a regra são do Núcleo (<see cref="MiningSimulation.RareWindow"/>); aqui só se desenha. Textos no
    /// <see cref="BuffCatalog"/>.
    /// </summary>
    public sealed class RareWindowView : MiningViewBase
    {
        /// <summary>Uma carta da janela: a moldura, o ícone, o nome, o efeito, a duração e para quem vale.</summary>
        [Serializable]
        public sealed class CardSlot
        {
            public Button button;
            public Image frame;
            public Image icon;
            public TMP_Text title;
            public TMP_Text text;
            public TMP_Text seconds;
            public TMP_Text note;
            [Tooltip("Apaga a carta que não saiu no sorteio.")]
            public CanvasGroup group;
        }

        [SerializeField] BuffCatalog catalog;
        [Tooltip("O fundo que escurece (bloqueia os toques no mapa) e o painel: ligado só com a janela aberta.")]
        [SerializeField] GameObject screen;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text queue;
        [SerializeField] TMP_Text subtitle;
        [SerializeField] CardSlot[] cards = new CardSlot[2];

        [Header("Escolhendo")]
        [Tooltip("A barra do tempo, o número e a dica: só enquanto o jogador escolhe.")]
        [SerializeField] GameObject choosing;
        [Tooltip("O preenchimento da barra: a âncora da direita anda com o tempo que falta.")]
        [SerializeField] RectTransform timerFill;
        [SerializeField] TMP_Text timerSeconds;
        [SerializeField] TMP_Text hint;

        [Header("Sorteio")]
        [SerializeField] Button closeButton;
        [SerializeField] TMP_Text closeLabel;

        [Header("Molduras e cores")]
        [SerializeField] Sprite cardFrame;
        [SerializeField] Sprite chosenFrame;
        [SerializeField] Color subtitleColor = new Color32(0xBE, 0xB4, 0xAA, 0xFF);
        [SerializeField] Color pickedColor = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        [SerializeField] Color internNoteColor = new Color32(0x8F, 0xD0, 0x6A, 0xFF);
        [SerializeField] Color dwarfNoteColor = new Color32(0xBE, 0xB4, 0xAA, 0xFF);
        [SerializeField] Color secondsColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        [SerializeField] Color endingColor = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        [Tooltip("Abaixo disto, o número do tempo fica âmbar.")]
        [SerializeField] float endingSeconds = 10f;
        [Tooltip("A transparência da carta que não saiu no sorteio.")]
        [SerializeField, Range(0f, 1f)] float dimAlpha = 0.4f;

        UnityAction[] _choose = Array.Empty<UnityAction>();
        int _shownSeconds = -1;
        RareWindow _window;

        public bool IsOpen => screen.activeSelf;
        public bool IsChoosing => IsOpen && choosing.activeSelf;
        public bool ShowsClose => IsOpen && closeButton.gameObject.activeSelf;
        public string CardTitle(int index) => cards[index].title.text;
        public string SubtitleText => subtitle.text;
        public string QueueText => queue.gameObject.activeSelf ? queue.text : "";
        public string SecondsText => timerSeconds.text;
        public bool IsCardDimmed(int index) => cards[index].group.alpha < 1f;

        void Awake()
        {
            title.text = catalog.RareTitle;
            hint.text = catalog.RareHint;
            closeLabel.text = catalog.RareClose;
            _choose = new UnityAction[cards.Length];
            for (int i = 0; i < cards.Length; i++)
            {
                int index = i;
                _choose[i] = () => Choose(index);
            }
            screen.SetActive(false); // o OnEnable (logo depois) abre, se o save trouxe uma janela esperando (D-222)
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            for (int i = 0; i < cards.Length; i++) cards[i].button.onClick.AddListener(_choose[i]);
            closeButton.onClick.AddListener(Close);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            for (int i = 0; i < cards.Length; i++) cards[i].button.onClick.RemoveListener(_choose[i]);
            closeButton.onClick.RemoveListener(Close);
        }

        protected override void Subscribe(MiningSimulation simulation) => simulation.RareWindowChanged += Show;
        protected override void Unsubscribe(MiningSimulation simulation) => simulation.RareWindowChanged -= Show;

        /// <summary>Uma partida nova: a janela que o save trouxe (D-222), ou nenhuma (o descanso a apaga, D-224).</summary>
        protected override void Rebuild(MiningSimulation simulation) => Show(simulation.RareWindow);

        /// <summary>Tocou na carta: ela entra (o Núcleo fecha a janela ou abre a próxima da fila).</summary>
        public void Choose(int index) => Simulation?.ChooseRareCard(index);

        /// <summary>FECHAR depois do sorteio.</summary>
        public void Close() => Simulation?.CloseRareWindow();

        void Update()
        {
            if (Simulation == null || !_window.IsChoosing) return;
            // O tempo corre no Núcleo; a barra e o número só leem (o evento não sai a cada quadro).
            var window = Simulation.RareWindow;
            ShowSeconds(window.SecondsLeft);
        }

        void Show(RareWindow window)
        {
            _window = window;
            if (!window.IsOpen || catalog == null)
            {
                screen.SetActive(false);
                return;
            }

            queue.gameObject.SetActive(window.Queued > 0);
            if (window.Queued > 0) queue.SetText(catalog.RareQueueFormat, (float)window.Queued);

            bool picking = window.IsChoosing;
            subtitle.color = picking ? subtitleColor : pickedColor;
            if (picking) subtitle.text = string.Format(catalog.RareChooseFormat, CardSecondsText(window.First));
            else subtitle.text = catalog.RarePicked;

            for (int i = 0; i < cards.Length; i++)
            {
                var card = window.CardAt(i);
                var slot = cards[i];
                var entry = catalog.Find(BonusSource.Rare, (int)card);
                slot.icon.sprite = entry?.Icon;
                slot.icon.enabled = slot.icon.sprite != null;
                slot.title.text = entry != null ? entry.Title : card.ToString();
                slot.text.text = entry != null ? entry.CardText : "";
                slot.note.text = entry != null ? entry.CardNote : "";
                slot.note.color = entry != null && entry.ForIntern ? internNoteColor : dwarfNoteColor;
                slot.seconds.text = CardSecondsText(card);
                bool chosen = !picking && window.Chosen == i;
                slot.frame.sprite = chosen && chosenFrame != null ? chosenFrame : cardFrame;
                slot.group.alpha = picking || chosen ? 1f : dimAlpha;
                slot.button.interactable = picking;
            }

            choosing.SetActive(picking);
            closeButton.gameObject.SetActive(!picking);
            _shownSeconds = -1;
            if (picking) ShowSeconds(window.SecondsLeft);
            screen.SetActive(true);
        }

        void ShowSeconds(double left)
        {
            double total = Simulation != null ? Simulation.Strike.Rare.ChooseSeconds : 30;
            float fraction = total > 0 ? Mathf.Clamp01((float)(left / total)) : 0f;
            var max = timerFill.anchorMax;
            if (!Mathf.Approximately(max.x, fraction)) timerFill.anchorMax = new Vector2(fraction, max.y);

            int seconds = Mathf.CeilToInt((float)left);
            if (seconds == _shownSeconds) return;
            _shownSeconds = seconds;
            timerSeconds.SetText(catalog.SecondsFormat, (float)seconds);
            timerSeconds.color = seconds <= endingSeconds ? endingColor : secondsColor;
        }

        /// <summary>A duração da carta no balanço ("60 s").</summary>
        string CardSecondsText(RareCard card)
        {
            double seconds = Simulation != null && Simulation.Strike.Rare.Has(card) ? Simulation.Strike.Rare.Get(card).Seconds : 0;
            return string.Format(catalog.CardSecondsFormat, NumberFormatter.Format(seconds));
        }
    }
}
