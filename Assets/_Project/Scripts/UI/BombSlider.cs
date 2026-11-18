using MinersIdle.Core;
using MinersIdle.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// A faixa da Bomba (M9g, D-310, mockups da E0): no centro da tela, acima do Descansar e do Próximo Andar. A bomba
    /// carrega em 5 s (a faixa enche e mostra os segundos); cheia, o jogador arrasta o botão da bomba da esquerda para a
    /// direita, e no fim da faixa ela é lançada (<see cref="MiningSimulation.ThrowBomb"/>). Soltar antes do fim devolve o
    /// botão ao começo. Dois estilos, trocados no DBG → Bomba: o do trilho no quadrado (A+C, o escolhido pelo Bruno) e o de
    /// destravar (C). A regra é da simulação; aqui só se lê a carga e se manda o lançamento.
    /// </summary>
    public sealed class BombSlider : MiningViewBase, IPointerDownHandler, IDragHandler, IPointerUpHandler, IBeginDragHandler
    {
        [Tooltip("O que some sem bomba no balanço (o v7).")]
        [SerializeField] GameObject content;
        [Tooltip("A faixa: o botão anda dentro dela.")]
        [SerializeField] RectTransform track;
        [Tooltip("O botão que se arrasta, com a bomba.")]
        [SerializeField] RectTransform knob;
        [SerializeField] Image knobImage;
        [SerializeField] Sprite knobOn, knobOff;
        [Tooltip("A vagoneta na frente da bomba (só no A+C).")]
        [SerializeField] Image cartImage;
        [SerializeField] Sprite cartOn, cartOff;
        [Tooltip("A faixa acesa (imagem Filled, horizontal): os dormentes no A+C, o laranja no C.")]
        [SerializeField] Image fill;
        [Tooltip("Cheia, a parte acesa segue o botão (o C); senão, mostra a carga (o A+C, todo aceso cheio).")]
        [SerializeField] bool fillFollowsKnob;
        [Tooltip("A bomba em uso: um quadro da folha das 8 bombas.")]
        [SerializeField] RawImage bomb;
        [SerializeField] Texture bombSheet;
        [Tooltip("Quadros na folha (D-240: 8) e o corpo da bomba no quadro de 48, em px a partir de baixo à esquerda.")]
        [SerializeField] int bombFrames = 8;
        [SerializeField] RectInt bombBody = new RectInt(14, 15, 20, 28);
        [SerializeField] Color bombChargingColor = new Color(0.55f, 0.55f, 0.55f, 0.6f);
        [Tooltip("As setas à frente do botão, quando cheia.")]
        [SerializeField] Image chevrons;
        [SerializeField] Image star;
        [SerializeField] Sprite starOn, starOff;
        [Tooltip("\"ARRASTE\", com a bomba cheia e o botão no começo.")]
        [SerializeField] TMP_Text readyText;
        [Tooltip("Os segundos até encher.")]
        [SerializeField] TMP_Text secondsText;
        [Tooltip("{0} = segundos que faltam, arredondados para cima.")]
        [SerializeField] string secondsFormat = "{0}s";

        [Header("Gesto")]
        [Tooltip("Folga do botão até a borda da faixa, em px da referência.")]
        [SerializeField] float inset = 4f;
        [Tooltip("Onde o arrasto lança (fração do caminho; 1 = o fim).")]
        [SerializeField, Range(0.5f, 1f)] float launchAt = 0.95f;
        [Tooltip("Segundos para o botão voltar ao começo depois de soltar ou lançar.")]
        [SerializeField] float returnSeconds = 0.18f;
        [Tooltip("Folga em volta do botão para pegar o dedo, em px da referência.")]
        [SerializeField] float grabPadding = 8f;

        bool _dragging;
        float _grabOffset;
        float _knobX;          // posição do centro do botão na faixa (local)
        int _shownSeconds = -1;
        int _shownBomb = -1;
        bool _shownCharged;
        bool _hasShown;

        /// <summary>Onde o botão está no caminho: 0 no começo, 1 no fim.</summary>
        public float KnobProgress => Mathf.InverseLerp(MinX, MaxX, _knobX);

        /// <summary>O dedo está arrastando a bomba.</summary>
        public bool IsDragging => _dragging;

        /// <summary>Quantas bombas o deslize lançou (para os testes).</summary>
        public int Launches { get; private set; }

        float HalfKnob => knob.rect.width * 0.5f;
        float MinX => track.rect.xMin + inset + HalfKnob;
        float MaxX => track.rect.xMax - inset - HalfKnob;

        protected override void OnEnable()
        {
            base.OnEnable();
            _knobX = MinX;
            _dragging = false;
            _hasShown = false;
            PlaceKnob();
        }

        protected override void Rebuild(MiningSimulation simulation)
        {
            _hasShown = false;
            Refresh();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanGrab()) return;
            var camera = eventData.pressEventCamera;
            var area = knob.rect;
            area.xMin -= grabPadding; area.xMax += grabPadding; area.yMin -= grabPadding; area.yMax += grabPadding;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(knob, eventData.position, camera, out var inKnob)
                || !area.Contains(inKnob))
                return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(track, eventData.position, camera, out var local)) return;
            _dragging = true;
            _grabOffset = local.x - _knobX;
        }

        public void OnBeginDrag(PointerEventData eventData) { } // segura o arrasto aqui (nenhum painel rola por baixo)

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(track, eventData.position, eventData.pressEventCamera, out var local))
                return;
            DragTo(local.x - _grabOffset);
        }

        public void OnPointerUp(PointerEventData eventData) => _dragging = false; // volta sozinho no Update

        /// <summary>
        /// Move o botão para <paramref name="x"/> (local na faixa); no fim do caminho, lança. É o que o dedo faz (os testes
        /// e o debug passam por aqui). Devolve se lançou.
        /// </summary>
        public bool DragTo(float x)
        {
            _knobX = Mathf.Clamp(x, MinX, MaxX);
            PlaceKnob();
            if (KnobProgress < launchAt) return false;

            _dragging = false;
            var simulation = Simulation;
            if (simulation == null || !simulation.ThrowBomb()) return false; // sem cristal no alcance: só volta
            Launches++;
            Refresh();
            return true;
        }

        /// <summary>O deslize inteiro, do começo ao fim (atalho dos testes): devolve se lançou.</summary>
        public bool Swipe()
        {
            if (!CanGrab()) return false;
            _dragging = true;
            return DragTo(MaxX);
        }

        bool CanGrab()
        {
            var simulation = Simulation;
            return simulation != null && simulation.HasBomb && simulation.IsBombCharged && !simulation.IsBombInFlight;
        }

        void Update()
        {
            if (Simulation == null) return;
            if (!_dragging && _knobX > MinX)
            {
                float speed = (MaxX - MinX) / Mathf.Max(0.01f, returnSeconds);
                _knobX = Mathf.Max(MinX, _knobX - speed * Time.unscaledDeltaTime);
                PlaceKnob();
            }
            Refresh();
        }

        void PlaceKnob()
        {
            if (knob == null) return;
            var position = knob.anchoredPosition;
            position.x = _knobX;
            knob.anchoredPosition = position;
            if (chevrons != null)
            {
                var rect = chevrons.rectTransform;
                var c = rect.anchoredPosition;
                c.x = _knobX + HalfKnob + 6f;
                rect.anchoredPosition = c;
            }
        }

        void Refresh()
        {
            var simulation = Simulation;
            if (simulation == null) return;
            bool has = simulation.HasBomb;
            if (content != null && content.activeSelf != has) content.SetActive(has);
            if (!has) return;

            bool charged = simulation.IsBombCharged && !simulation.IsBombInFlight;
            if (!_hasShown || charged != _shownCharged)
            {
                _shownCharged = charged;
                if (knobImage != null) knobImage.sprite = charged ? knobOn : knobOff;
                if (cartImage != null) cartImage.sprite = charged ? cartOn : cartOff;
                if (bomb != null) bomb.color = charged ? Color.white : bombChargingColor;
                if (star != null) star.sprite = charged ? starOn : starOff;
                if (secondsText != null) secondsText.gameObject.SetActive(!charged);
                if (chevrons != null) chevrons.gameObject.SetActive(charged);
            }
            if (readyText != null)
            {
                bool ready = charged && !_dragging && KnobProgress < 0.02f;
                if (readyText.gameObject.activeSelf != ready) readyText.gameObject.SetActive(ready);
            }

            int index = simulation.BombIndex;
            if ((!_hasShown || index != _shownBomb) && bomb != null && bombSheet != null)
            {
                _shownBomb = index;
                float w = bombSheet.width, h = bombSheet.height;
                float frame = w / Mathf.Max(1, bombFrames);
                bomb.uvRect = new Rect((index * frame + bombBody.x) / w, bombBody.y / h, bombBody.width / w, bombBody.height / h);
            }

            if (!charged && secondsText != null)
            {
                int seconds = Mathf.CeilToInt((float)simulation.BombChargeLeft);
                if (seconds != _shownSeconds)
                {
                    _shownSeconds = seconds;
                    secondsText.SetText(secondsFormat, seconds);
                }
            }

            if (fill != null)
                fill.fillAmount = !charged ? (float)simulation.BombCharge
                                : fillFollowsKnob ? Mathf.InverseLerp(track.rect.xMin, track.rect.xMax, _knobX) : 1f;

            if (charged && chevrons != null)
            {
                // As setas pulsam, para chamar o dedo.
                float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 6f);
                var color = Color.white;
                color.a = pulse;
                chevrons.color = color;
            }
            _hasShown = true;
        }
    }
}
