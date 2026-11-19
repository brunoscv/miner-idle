using MinersIdle.Game;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MinersIdle.UI
{
    /// <summary>
    /// M9i E5 (#17): o Voltar do Android (no Input System, a tecla Esc; no Editor, o Esc do teclado). Primeiro, a tela de
    /// cima responde (<see cref="IBackHandler"/>: fecha a LOJA, os AJUSTES, o Boost…, ou fica parada numa escolha). Sem tela
    /// aberta, o primeiro Voltar mostra "Toque de novo para sair"; um segundo em até <see cref="WindowSeconds"/> abre
    /// "Sair do jogo?" (<see cref="ExitGameScreen"/>).
    /// </summary>
    public sealed class BackButtonHandler : MonoBehaviour
    {
        public const float WindowSeconds = 2f;

        [SerializeField] FeedbackTexts texts;
        [Tooltip("As telas que respondem ao Voltar, na ordem da cena (a de baixo da lista é a de cima na tela).")]
        [SerializeField] MonoBehaviour[] handlers = new MonoBehaviour[0];
        [SerializeField] ExitGameScreen exit;
        [Tooltip("O aviso \"Toque de novo para sair\".")]
        [SerializeField] GameObject notice;
        [SerializeField] TMP_Text noticeText;

        float _noticeLeft;

        public bool IsNoticeShown => notice != null && notice.activeSelf;

        void Awake()
        {
            if (noticeText != null) noticeText.text = texts.BackAgainNotice;
            if (notice != null) notice.SetActive(false);
        }

        void Update()
        {
            if (_noticeLeft > 0f)
            {
                _noticeLeft -= Time.unscaledDeltaTime;
                if (_noticeLeft <= 0f) HideNotice();
            }
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Back();
        }

        /// <summary>Um Voltar (a tecla, e os testes).</summary>
        public void Back()
        {
            for (int i = handlers.Length - 1; i >= 0; i--)
            {
                if (handlers[i] is IBackHandler handler && handlers[i].isActiveAndEnabled && handler.HandleBack())
                {
                    HideNotice();
                    return;
                }
            }

            if (_noticeLeft > 0f)
            {
                HideNotice();
                exit.Open();
                return;
            }
            _noticeLeft = WindowSeconds;
            if (notice != null) notice.SetActive(true);
        }

        void HideNotice()
        {
            _noticeLeft = 0f;
            if (notice != null) notice.SetActive(false);
        }
    }
}
