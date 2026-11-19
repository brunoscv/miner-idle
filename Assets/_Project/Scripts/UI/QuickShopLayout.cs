using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// O botão do DBG que liga a loja rápida (M9i E8, D-341; <c>DBG → Loja → Loja rápida</c>). Ligada, a tela muda em
    /// quatro lugares, juntos:
    /// <list type="number">
    /// <item>a loja rápida (<see cref="QuickShopView"/>) aparece acima do Descansar e do DESCER;</item>
    /// <item>Descansar e DESCER ficam mais largos e alinhados com os cards (as poses "ligada" vêm do setup);</item>
    /// <item>a barra do andar volta para o alto e o DBG vai para cima dos cards;</item>
    /// <item>a janela do raro sobe até a altura da LOJA e fica por cima da coluna e da barra: ela passa para logo depois
    /// da LOJA na hierarquia (os modais e o DBG, que vêm depois, continuam por cima dela).</item>
    /// </list>
    /// Desligada, tudo volta às poses que a cena tinha ao abrir. A escolha fica lembrada entre aberturas (P9), nas
    /// preferências do aparelho, fora do save; num build de lançamento ela nunca liga.
    /// </summary>
    public sealed class QuickShopLayout : MonoBehaviour
    {
        [Serializable]
        public struct Pose
        {
            public RectTransform target;
            public Vector2 anchorMin;
            public Vector2 anchorMax;
            public Vector2 pivot;
            public Vector2 anchoredPosition;
            public Vector2 sizeDelta;

            public static Pose Of(RectTransform rect) => new Pose
            {
                target = rect, anchorMin = rect.anchorMin, anchorMax = rect.anchorMax, pivot = rect.pivot,
                anchoredPosition = rect.anchoredPosition, sizeDelta = rect.sizeDelta,
            };

            public void Apply()
            {
                if (target == null) return;
                target.anchorMin = anchorMin;
                target.anchorMax = anchorMax;
                target.pivot = pivot;
                target.anchoredPosition = anchoredPosition;
                target.sizeDelta = sizeDelta;
            }
        }

        [SerializeField] GameObject quickShop;
        [Tooltip("Como cada peça fica com a loja rápida ligada (o setup preenche).")]
        [SerializeField] Pose[] onPoses = Array.Empty<Pose>();
        [Tooltip("A janela do raro: com a loja rápida ligada, passa para logo depois de rareAbove (a LOJA, com o tile da coluna).")]
        [SerializeField] Transform rareWindow;
        [SerializeField] Transform rareAbove;
        [Header("DBG")]
        [SerializeField] Button debugButton;
        [SerializeField] TMP_Text debugLabel;
        [Tooltip("{0} = sim ou não.")]
        [SerializeField] string labelFormat = "Loja rápida: {0}";
        [SerializeField] string yesName = "sim";
        [SerializeField] string noName = "não";
        [SerializeField] string prefsKey = "dbg.quickShop";

        Pose[] _offPoses = Array.Empty<Pose>();
        int _rareIndex = -1;

        public bool IsOn { get; private set; }

        void Awake()
        {
            _offPoses = new Pose[onPoses.Length];
            for (int i = 0; i < onPoses.Length; i++)
                _offPoses[i] = onPoses[i].target != null ? Pose.Of(onPoses[i].target) : default;
            bool allowed = Debug.isDebugBuild || Application.isEditor;
            Apply(allowed && ReadPref());
        }

        void OnEnable()
        {
            if (debugButton != null) debugButton.onClick.AddListener(Toggle);
        }

        void OnDisable()
        {
            if (debugButton != null) debugButton.onClick.RemoveListener(Toggle);
        }

        public void Toggle() => SetOn(!IsOn);

        public void SetOn(bool on)
        {
            Apply(on);
            try
            {
                PlayerPrefs.SetInt(prefsKey, on ? 1 : 0);
                PlayerPrefs.Save();
            }
            catch (Exception) { /* sem preferências, vale só nesta abertura */ }
        }

        bool ReadPref()
        {
            try { return PlayerPrefs.GetInt(prefsKey, 0) == 1; }
            catch (Exception) { return false; }
        }

        void Apply(bool on)
        {
            IsOn = on;
            var poses = on ? onPoses : _offPoses;
            foreach (var pose in poses) pose.Apply();
            if (quickShop != null && quickShop.activeSelf != on) quickShop.SetActive(on);
            MoveRareWindow(on);
            if (debugLabel != null) debugLabel.text = string.Format(labelFormat, on ? yesName : noName);
        }

        void MoveRareWindow(bool on)
        {
            if (rareWindow == null || rareAbove == null || rareWindow.parent != rareAbove.parent) return;
            if (on)
            {
                if (_rareIndex < 0) _rareIndex = rareWindow.GetSiblingIndex();
                int above = rareAbove.GetSiblingIndex();
                if (rareWindow.GetSiblingIndex() < above) rareWindow.SetSiblingIndex(above); // a LOJA sobe uma casa
            }
            else if (_rareIndex >= 0)
            {
                rareWindow.SetSiblingIndex(_rareIndex);
                _rareIndex = -1;
            }
        }
    }
}
