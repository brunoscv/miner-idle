using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// DBG → Efeitos → "Esconder HUD": tira da tela o HUD do alto (LASCAS, DANO, CAVERNA, VELOCIDADE) e todos os botões do jogo (Loja, Ajustes, Descansar, PASSAR, AD, buffs…) para
    /// ver ou gravar o jogo limpo. Cada botão ganha um CanvasGroup invisível e sem raycast (não mexe no objeto, então os
    /// scripts que o mostram e escondem seguem funcionando); apertar de novo desfaz. Os botões do próprio DBG ficam. Só pega
    /// os botões que existem na hora do toque (listas e janelas criadas depois aparecem normais). Está dentro do painel de
    /// debug: no release some junto com ele.
    /// </summary>
    public sealed class DebugHudHider : MonoBehaviour
    {
        [SerializeField] Button toggle;
        [Tooltip("O painel de debug: seus botões nunca são escondidos.")]
        [SerializeField] Transform debugRoot;
        [SerializeField] string hideLabel = "Esconder HUD";
        [SerializeField] string showLabel = "Mostrar HUD";

        readonly List<CanvasGroup> _hidden = new List<CanvasGroup>();
        TMP_Text _label;

        public bool IsHidden { get; private set; }

        void Awake()
        {
            if (toggle != null) _label = toggle.GetComponentInChildren<TMP_Text>();
        }

        void OnEnable()
        {
            if (toggle != null) toggle.onClick.AddListener(Toggle);
        }

        void OnDisable()
        {
            if (toggle != null) toggle.onClick.RemoveListener(Toggle);
        }

        public void Toggle()
        {
            if (IsHidden) Restore();
            else Hide();
            if (_label != null) _label.text = IsHidden ? showLabel : hideLabel;
        }

        void Hide()
        {
            foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include))
            {
                if (debugRoot != null && button.transform.IsChildOf(debugRoot)) continue;
                HideObject(button.gameObject);
            }
            // O HUD do alto (LASCAS, DANO, CAVERNA, VELOCIDADE).
            foreach (var hud in FindObjectsByType<HudView>(FindObjectsInactive.Include)) HideObject(hud.gameObject);
            // A janela do cristal raro: o fundo escuro e o painel (sem raycast escondido, os toques passam para o jogo).
            foreach (var window in FindObjectsByType<RareWindowView>(FindObjectsInactive.Include))
                if (window.Screen != null) HideObject(window.Screen);
            IsHidden = true;
        }

        void HideObject(GameObject target)
        {
            var group = target.GetComponent<CanvasGroup>();
            if (group == null) group = target.AddComponent<CanvasGroup>();
            else if (group.alpha == 0f) return; // já estava invisível por outro motivo: não mexe
            group.alpha = 0f;
            group.blocksRaycasts = false;
            _hidden.Add(group);
        }

        void Restore()
        {
            foreach (var group in _hidden)
            {
                if (group == null) continue;
                group.alpha = 1f;
                group.blocksRaycasts = true;
            }
            _hidden.Clear();
            IsHidden = false;
        }
    }
}
