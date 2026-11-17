using MinersIdle.Core;
using UnityEngine;

namespace MinersIdle.Presentation
{
    /// <summary>As transições de andar que o DBG deixa escolher (M9c E0, D-267), na ordem em que entraram: C, B, A.</summary>
    public enum FloorTransitionStyle
    {
        /// <summary>Sem transição: a caverna nova começa na hora (o jogo até o M9c).</summary>
        None = 0,
        /// <summary>C — a câmera vem pela caverna: nasce lá na frente, baixa, e voa pelo corredor até o anão.</summary>
        CaveFlight = 1,
        /// <summary>B — a câmera desce: sobe saindo do corredor, o cartão no escuro, e desce do alto até o anão, focando.</summary>
        CameraDrop = 2,
        /// <summary>A B com mosaico de pixels no lugar do desfoque (o plano B da P10, para comparar).</summary>
        CameraDropMosaic = 3,
        /// <summary>A — desfoque e cartão: a câmera fica; a descida desfoca e escurece, o cartão, e volta a focar.</summary>
        BlurCard = 4,
    }

    /// <summary>
    /// A transição de andar (M9c E0, D-266/D-267): o que se vê enquanto a simulação está parada entre uma caverna e a
    /// próxima. Quem manda no tempo é a <see cref="MiningSimulation"/> (<see cref="MiningSimulation.FloorTransitionLeft"/>):
    /// esta view só lê quanto já passou e põe a câmera, o escuro, o desfoque e o cartão no lugar. Ela diz à simulação
    /// quanto a transição escolhida dura (um comando, como a loja). Com o tempo do DBG acelerado, ela acelera junto.
    ///
    /// <b>C — a câmera vem pela caverna:</b> a descida escurece; no escuro o bioma troca e o cartão da caverna nova
    /// aparece no meio da tela; a câmera nasce lá na frente, baixa e quase reta, e voa pelo corredor, por cima da fila,
    /// subindo e inclinando até o lugar de jogo, clareando no caminho e freando no fim.
    ///
    /// <b>B — a câmera desce</b> (P10, ~2,5 s): a câmera sobe um pouco e a descida desfoca e escurece; no escuro, o
    /// cartão, um tremor leve e um "vum" grave (os dois obedecem aos AJUSTES); a câmera desce do alto até o lugar de
    /// jogo, freando, enquanto o desfoque some. Ela mira sempre o mesmo ponto do chão que a câmera de jogo mira, então o
    /// movimento é relativo ao enquadramento e continua valendo quando o Bloco G trocar a câmera.
    ///
    /// <b>A — desfoque e cartão</b> (P10, a mais simples): a câmera não sai do lugar; a descida desfoca e escurece (sem
    /// chegar ao preto: a caverna fica atrás, borrada), o cartão aparece, o bioma troca no ponto mais desfocado, e tudo
    /// volta a focar. O desfoque é o mesmo da B.
    /// </summary>
    public sealed class FloorTransition : MiningViewBase
    {
        [SerializeField] DescentStage stage;
        [Tooltip("A faixa da caverna nova: na transição ela vira o cartão, no meio da tela.")]
        [SerializeField] CaveBanner banner;
        [Tooltip("O tremor da B (desligado nos AJUSTES, não treme).")]
        [SerializeField] ScreenShake screenShake;
        [Tooltip("O som da B (desligado nos AJUSTES, não toca).")]
        [SerializeField] SoundBoard sound;
        [Tooltip("A transição em uso (o DBG troca; não fica salvo). A B é a escolhida (D-268).")]
        [SerializeField] FloorTransitionStyle style = FloorTransitionStyle.CameraDrop;

        [Header("C — a câmera vem pela caverna")]
        [Tooltip("Duração total, em segundos de jogo.")]
        [SerializeField, Min(0.5f)] float flightSeconds = 2.8f;
        [Tooltip("Quanto dura o escurecer do começo.")]
        [SerializeField, Min(0.05f)] float flightFadeOutSeconds = 0.45f;
        [Tooltip("Quanto o cartão fica no escuro antes do voo.")]
        [SerializeField, Min(0f)] float flightCardSeconds = 0.7f;
        [Tooltip("Onde o voo nasce: metros à frente da câmera de jogo, no corredor (o corredor vai até 13).")]
        [SerializeField, Range(2f, 12f)] float flightStartAhead = 12f;
        [Tooltip("Altura da câmera no começo do voo (m).")]
        [SerializeField, Range(0.6f, 3f)] float flightStartHeight = 1.5f;
        [Tooltip("Inclinação da câmera no começo do voo (graus; a de jogo é a do enquadramento).")]
        [SerializeField, Range(-10f, 30f)] float flightStartTilt = 4f;
        [Tooltip("Que parte do voo clareia (0–1).")]
        [SerializeField, Range(0.05f, 1f)] float flightFadeInPart = 0.4f;
        [Tooltip("Que parte do voo o cartão leva para sumir (0–1).")]
        [SerializeField, Range(0.05f, 1f)] float flightCardOutPart = 0.35f;

        [Header("B — a câmera desce")]
        [Tooltip("Duração total, em segundos de jogo (a P10 pede ~2,5).")]
        [SerializeField, Min(0.5f)] float dropSeconds = 2.5f;
        [Tooltip("Saída: a câmera sobe, a descida desfoca e escurece.")]
        [SerializeField, Min(0.05f)] float dropOutSeconds = 0.6f;
        [Tooltip("O cartão no escuro, com o tremor e o som.")]
        [SerializeField, Min(0f)] float dropCardSeconds = 0.8f;
        [Tooltip("Quanto a câmera sobe na saída (m acima da altura de jogo).")]
        [SerializeField, Range(0f, 4f)] float dropRise = 1.2f;
        [Tooltip("De que altura ela desce na chegada (m acima da altura de jogo).")]
        [SerializeField, Range(1f, 12f)] float dropFrom = 6f;
        [Tooltip("Quanto a descida desfoca no máximo: a textura dividida por este número (vale também para a A).")]
        [SerializeField, Range(2, 16)] int blurMaxFactor = 6;
        [Tooltip("Que parte da chegada clareia (0–1).")]
        [SerializeField, Range(0.05f, 1f)] float dropFadeInPart = 0.5f;
        [Tooltip("Que parte da chegada leva para focar (0–1).")]
        [SerializeField, Range(0.05f, 1f)] float dropFocusPart = 0.8f;
        [Tooltip("Que parte da chegada o cartão leva para sumir (0–1).")]
        [SerializeField, Range(0.05f, 1f)] float dropCardOutPart = 0.35f;
        [Tooltip("O tremor leve no cartão: amplitude (unidades de mundo) e duração.")]
        [SerializeField, Min(0f)] float dropShake = 0.3f;
        [SerializeField, Min(0f)] float dropShakeSeconds = 0.35f;

        [Header("A — desfoque e cartão")]
        [Tooltip("Duração total, em segundos de jogo.")]
        [SerializeField, Min(0.5f)] float blurCardSeconds = 2f;
        [Tooltip("Saída: a descida desfoca e escurece.")]
        [SerializeField, Min(0.05f)] float blurCardOutSeconds = 0.5f;
        [Tooltip("O cartão sobre a caverna borrada.")]
        [SerializeField, Min(0f)] float blurCardHoldSeconds = 0.8f;
        [Tooltip("Quanto escurece (0–1; 1 = preto). Abaixo de 1, a caverna borrada fica atrás do cartão.")]
        [SerializeField, Range(0f, 1f)] float blurCardDarkness = 0.8f;
        [Tooltip("Mosaico de pixels no lugar do desfoque suave.")]
        [SerializeField] bool blurCardMosaic;
        [Tooltip("Que parte da volta leva para focar (0–1).")]
        [SerializeField, Range(0.05f, 1f)] float blurCardFocusPart = 1f;
        [Tooltip("Que parte da volta o cartão leva para sumir (0–1).")]
        [SerializeField, Range(0.05f, 1f)] float blurCardCardOutPart = 0.5f;

        [Header("Cartão")]
        [Tooltip("Altura do cartão na tela (0 = base, 1 = topo).")]
        [SerializeField, Range(0f, 1f)] float cardHeight = 0.58f;
        [Tooltip("Quanto o cartão leva para aparecer no escuro.")]
        [SerializeField, Min(0.01f)] float cardFadeInSeconds = 0.2f;
        [Tooltip("DBG → Andar → Transição curta (M9c E2): a duração da transição comprimida.")]
        [SerializeField, Min(0.05f)] float shortSeconds = 0.3f;

        bool _playing;
        bool _short;
        bool _biomeSwapped;
        bool _cardBeatDone;
        FloorTransitionStyle _playingStyle;

        /// <summary>A transição em uso. Trocar vale para a próxima descida.</summary>
        public FloorTransitionStyle Style
        {
            get => style;
            set
            {
                style = value;
                ApplyDuration();
            }
        }

        /// <summary>
        /// DBG → Andar → Transição curta (M9c E2): a mesma transição comprimida em <see cref="ShortSeconds"/> (a animação
        /// estica para caber no tempo do núcleo). Para repetir a passagem sem esperar; não fica salvo.
        /// </summary>
        public bool IsShort
        {
            get => _short;
            set
            {
                _short = value;
                ApplyDuration();
            }
        }

        /// <summary>A duração da transição curta, em segundos de jogo.</summary>
        public float ShortSeconds => shortSeconds;

        /// <summary>Se a transição está tocando agora (para testes).</summary>
        public bool IsPlaying => _playing;

        /// <summary>A duração, em segundos de jogo, de cada transição.</summary>
        public float DurationOf(FloorTransitionStyle value)
        {
            switch (value)
            {
                case FloorTransitionStyle.CaveFlight: return flightSeconds;
                case FloorTransitionStyle.CameraDrop:
                case FloorTransitionStyle.CameraDropMosaic: return dropSeconds;
                case FloorTransitionStyle.BlurCard: return blurCardSeconds;
                default: return 0f;
            }
        }

        protected override void Subscribe(MiningSimulation simulation)
        {
            simulation.FloorTransitionStarted += OnStarted;
            simulation.FloorTransitionEnded += OnEnded;
        }

        protected override void Unsubscribe(MiningSimulation simulation)
        {
            simulation.FloorTransitionStarted -= OnStarted;
            simulation.FloorTransitionEnded -= OnEnded;
            if (_playing) Finish();
        }

        protected override void Rebuild(MiningSimulation simulation)
        {
            ApplyDuration();
            if (_playing) Finish();
            if (simulation.IsInFloorTransition) OnStarted(simulation.State.Cave);
        }

        void ApplyDuration()
        {
            if (Simulation == null) return;
            float full = DurationOf(style);
            Simulation.FloorTransitionSeconds = _short && full > 0f ? Mathf.Min(shortSeconds, full) : full;
        }

        void OnStarted(int cave)
        {
            _playing = true;
            _biomeSwapped = false;
            _cardBeatDone = false;
            _playingStyle = style; // o DBG pode trocar no meio: esta segue com a de quando começou
        }

        void OnEnded() => Finish();

        void Finish()
        {
            _playing = false;
            if (stage != null)
            {
                stage.ApplyPendingBiome();
                stage.ResetCameraPose();
                stage.SetTransitionDarkness(0f);
                stage.SetTransitionBlur(0f, 1, false);
            }
            if (banner != null) banner.Release();
        }

        void LateUpdate()
        {
            if (!_playing || Simulation == null || stage == null) return;
            if (!Simulation.IsInFloorTransition)
            {
                Finish();
                return;
            }

            // Segundos de jogo desde o começo, escalados para a duração da transição que está tocando (se a duração
            // mudou no Inspector no meio, a animação estica para caber no tempo do núcleo).
            double length = Simulation.FloorTransitionLength;
            float seconds = (float)(length - Simulation.FloorTransitionLeft);
            float duration = DurationOf(_playingStyle);
            float scaled = length > 0 && duration > 0 ? seconds * duration / (float)length : seconds;
            switch (_playingStyle)
            {
                case FloorTransitionStyle.CaveFlight: PlayCaveFlight(scaled); break;
                case FloorTransitionStyle.CameraDrop: PlayCameraDrop(scaled, mosaic: false); break;
                case FloorTransitionStyle.CameraDropMosaic: PlayCameraDrop(scaled, mosaic: true); break;
                case FloorTransitionStyle.BlurCard: PlayBlurCard(scaled); break;
            }
        }

        void PlayCaveFlight(float seconds)
        {
            var framing = stage.Framing;
            var home = new Vector3(0f, framing.height, 0f);
            float cardStart = flightFadeOutSeconds;
            float flightStart = flightFadeOutSeconds + flightCardSeconds;

            if (seconds < cardStart)
            {
                // 1. Saída: a descida escurece, a câmera ainda no lugar.
                stage.SetCameraPose(home, framing.tilt);
                stage.SetTransitionDarkness(Smooth(seconds / flightFadeOutSeconds));
                HoldCard(0f);
                return;
            }

            SwapBiomeInTheDark();
            var start = new Vector3(0f, flightStartHeight, -flightStartAhead);
            if (seconds < flightStart)
            {
                // 2. O cartão no escuro; a câmera já espera lá na frente.
                stage.SetCameraPose(start, flightStartTilt);
                stage.SetTransitionDarkness(1f);
                HoldCard(Mathf.Clamp01((seconds - cardStart) / cardFadeInSeconds));
                return;
            }

            // 3. O voo: sai rápido do escuro e freia ao chegar.
            float flight = Mathf.Max(0.01f, flightSeconds - flightStart);
            float u = Mathf.Clamp01((seconds - flightStart) / flight);
            float eased = 1f - Mathf.Pow(1f - u, 3f);
            stage.SetCameraPose(Vector3.LerpUnclamped(start, home, eased), Mathf.LerpUnclamped(flightStartTilt, framing.tilt, eased));
            stage.SetTransitionDarkness(1f - Smooth(u / flightFadeInPart));
            HoldCard(1f - Smooth(u / flightCardOutPart));
        }

        void PlayCameraDrop(float seconds, bool mosaic)
        {
            var framing = stage.Framing;
            float cardStart = dropOutSeconds;
            float arrivalStart = dropOutSeconds + dropCardSeconds;

            if (seconds < cardStart)
            {
                // 1. Saída: sobe um pouco, desfoca e escurece.
                float u = Smooth(seconds / dropOutSeconds);
                PlaceAbove(framing, dropRise * u);
                stage.SetTransitionBlur(u, blurMaxFactor, mosaic);
                stage.SetTransitionDarkness(u);
                HoldCard(0f);
                return;
            }

            SwapBiomeInTheDark();
            if (seconds < arrivalStart)
            {
                // 2. O cartão no escuro, com o tremor e o "vum"; a câmera já espera lá no alto.
                if (!_cardBeatDone)
                {
                    _cardBeatDone = true;
                    if (screenShake != null && dropShake > 0f) screenShake.Shake(dropShake, dropShakeSeconds);
                    if (sound != null) sound.Play(Sfx.FloorDown);
                }
                PlaceAbove(framing, dropFrom);
                stage.SetTransitionBlur(1f, blurMaxFactor, mosaic);
                stage.SetTransitionDarkness(1f);
                HoldCard(Mathf.Clamp01((seconds - cardStart) / cardFadeInSeconds));
                return;
            }

            // 3. A chegada: desce do alto, freando no fim, clareando e focando no caminho.
            float arrival = Mathf.Max(0.01f, dropSeconds - arrivalStart);
            float v = Mathf.Clamp01((seconds - arrivalStart) / arrival);
            float eased = 1f - Mathf.Pow(1f - v, 3f);
            PlaceAbove(framing, Mathf.LerpUnclamped(dropFrom, 0f, eased));
            stage.SetTransitionBlur(1f - Smooth(v / dropFocusPart), blurMaxFactor, mosaic);
            stage.SetTransitionDarkness(1f - Smooth(v / dropFadeInPart));
            HoldCard(1f - Smooth(v / dropCardOutPart));
        }

        void PlayBlurCard(float seconds)
        {
            float cardStart = blurCardOutSeconds;
            float focusStart = blurCardOutSeconds + blurCardHoldSeconds;

            if (seconds < cardStart)
            {
                // 1. Saída: desfoca e escurece, a câmera no lugar.
                float u = Smooth(seconds / blurCardOutSeconds);
                stage.SetTransitionBlur(u, blurMaxFactor, blurCardMosaic);
                stage.SetTransitionDarkness(u * blurCardDarkness);
                HoldCard(0f);
                return;
            }

            SwapBiomeInTheDark(); // no ponto mais borrado e escuro
            if (seconds < focusStart)
            {
                // 2. O cartão sobre a caverna borrada.
                stage.SetTransitionBlur(1f, blurMaxFactor, blurCardMosaic);
                stage.SetTransitionDarkness(blurCardDarkness);
                HoldCard(Mathf.Clamp01((seconds - cardStart) / cardFadeInSeconds));
                return;
            }

            // 3. Volta a focar e clarear; o cartão sai antes.
            float back = Mathf.Max(0.01f, blurCardSeconds - focusStart);
            float v = Mathf.Clamp01((seconds - focusStart) / back);
            stage.SetTransitionBlur(1f - Smooth(v / blurCardFocusPart), blurMaxFactor, blurCardMosaic);
            stage.SetTransitionDarkness(blurCardDarkness * (1f - Smooth(v)));
            HoldCard(1f - Smooth(v / blurCardCardOutPart));
        }

        /// <summary>
        /// A câmera <paramref name="extra"/> metros acima da de jogo, inclinada para mirar o mesmo ponto do chão que a de
        /// jogo mira (o anão fica no quadro enquanto ela sobe e desce).
        /// </summary>
        void PlaceAbove(DescentFraming framing, float extra)
        {
            float height = framing.height + extra;
            float tilt = framing.tilt;
            if (framing.tilt > 1f)
            {
                float aim = framing.height / Mathf.Tan(framing.tilt * Mathf.Deg2Rad); // onde a câmera de jogo mira, no chão
                tilt = Mathf.Atan2(height, aim) * Mathf.Rad2Deg;
            }
            stage.SetCameraPose(new Vector3(0f, height, 0f), tilt);
        }

        void SwapBiomeInTheDark()
        {
            if (_biomeSwapped) return;
            stage.ApplyPendingBiome();
            _biomeSwapped = true;
        }

        void HoldCard(float alpha)
        {
            if (banner != null) banner.Hold(alpha, cardHeight);
        }

        static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
