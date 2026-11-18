using System;
using MinersIdle.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.UI
{
    /// <summary>
    /// Um ícone da coluna de buffs (M9b, E2): o anel âmbar que esvazia, o ícone e os segundos ao lado. Só desenha o que a
    /// <see cref="BuffColumn"/> manda; o texto dos segundos muda só quando o número muda (sem alocar por quadro).
    /// </summary>
    public sealed class BuffSlot : MonoBehaviour
    {
        [SerializeField] Image ring;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text seconds;
        [SerializeField] Button button;
        [SerializeField] CanvasGroup group;
        [Tooltip("M9e E3 (P7): o selo do anúncio (\"×4\"), no canto do ícone. Vazio = sem selo.")]
        [SerializeField] TMP_Text badge;
        [Tooltip("O que liga e desliga com o selo (o fundo escuro e o texto).")]
        [SerializeField] GameObject badgeRoot;

        long _shownKey = long.MinValue;
        int _colorState = -1;

        public Button Button => button;
        public RectTransform Rect => (RectTransform)transform;
        public float Fill => ring.fillAmount;
        public string SecondsText => seconds.text;
        public Color SecondsColor => seconds.color;
        public Sprite Icon => icon.sprite;
        public float Alpha => group.alpha;
        public string BadgeText => badge != null && badgeRoot != null && badgeRoot.activeSelf ? badge.text : "";

        public void Bind(Sprite sprite, string badgeText = null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            _shownKey = long.MinValue;
            if (badge == null || badgeRoot == null) return;
            bool show = !string.IsNullOrEmpty(badgeText);
            if (badgeRoot.activeSelf != show) badgeRoot.SetActive(show);
            if (show && badge.text != badgeText) badge.text = badgeText;
        }

        /// <param name="format">Os segundos no último minuto (\"{0}s\"); daí para cima, \"19m\" e \"1h18\" (M9e E3, P7).</param>
        /// <param name="ending">Nos últimos segundos: o número em âmbar e o ícone piscando (<paramref name="alpha"/>).</param>
        public void Draw(double secondsLeft, double duration, string format, bool ending, Color normal, Color endingColor, float alpha)
        {
            ring.fillAmount = duration > 0 ? Mathf.Clamp01((float)(secondsLeft / duration)) : 0f;
            long key = NumberFormatter.TimerKey(secondsLeft);
            if (key != _shownKey)
            {
                _shownKey = key;
                // No último minuto, sem alocar; acima dele o texto muda uma vez por minuto.
                if (key >= 0) seconds.SetText(format, key);
                else seconds.text = NumberFormatter.FormatTimer(secondsLeft);
            }
            int state = ending ? 1 : 0;
            if (state != _colorState)
            {
                _colorState = state;
                seconds.color = ending ? endingColor : normal;
            }
            group.alpha = alpha;
        }
    }
}
