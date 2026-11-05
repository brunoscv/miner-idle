using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// Um menu do DBG (Loja, Efeitos, Caverna; 2026-11-05, D-147): abre em cima da tela, como os controles da câmera, e fica
    /// aberto com o DBG fechado, para usar olhando o jogo. Só mostra e esconde; os botões de dentro são ligados pelo
    /// <see cref="DebugPanel"/>, que também fecha os outros menus quando um abre. Filho do painel de debug: no release
    /// some junto com ele.
    /// </summary>
    public sealed class DebugMenu : MonoBehaviour
    {
        [SerializeField] GameObject content;
        [SerializeField] Button close;

        public bool IsOpen => content.activeSelf;

        void Awake() => content.SetActive(false);

        void OnEnable() => close.onClick.AddListener(Hide);

        void OnDisable() => close.onClick.RemoveListener(Hide);

        public void Show() => content.SetActive(true);

        public void Hide() => content.SetActive(false);
    }
}
