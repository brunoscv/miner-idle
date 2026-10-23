using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// Botão que já aparece na tela mas ainda não funciona (D-67: AD e AJUSTES até o M9): ao tocar, mostra um aviso curto
    /// ("Em breve", texto da cena) que some sozinho. O toque no botão não conta como toque no cristal (a imagem recebe
    /// raycast).
    /// </summary>
    public sealed class ComingSoonButton : MonoBehaviour
    {
        [SerializeField] Button button;
        [Tooltip("O aviso (\"Em breve\"); começa escondido.")]
        [SerializeField] GameObject notice;
        [SerializeField, Min(0.1f)] float noticeSeconds = 1.2f;

        float _left;

        /// <summary>Se o aviso está na tela agora (para testes).</summary>
        public bool IsNoticeShown => notice.activeSelf;

        void Awake() => notice.SetActive(false);

        void OnEnable() => button.onClick.AddListener(Show);

        void OnDisable()
        {
            button.onClick.RemoveListener(Show);
            notice.SetActive(false);
        }

        void Show()
        {
            _left = noticeSeconds;
            notice.SetActive(true);
        }

        void Update()
        {
            if (_left <= 0f) return;
            _left -= Time.unscaledDeltaTime;
            if (_left <= 0f) notice.SetActive(false);
        }
    }
}
