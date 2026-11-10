using System;
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

        int _shownSeconds = -1;
        int _colorState = -1;

        public Button Button => button;
        public RectTransform Rect => (RectTransform)transform;
        public float Fill => ring.fillAmount;
        public string SecondsText => seconds.text;
        public Color SecondsColor => seconds.color;
        public Sprite Icon => icon.sprite;
        public float Alpha => group.alpha;

        public void Bind(Sprite sprite)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            _shownSeconds = -1;
        }

        /// <param name="ending">Nos últimos segundos: o número em âmbar e o ícone piscando (<paramref name="alpha"/>).</param>
        public void Draw(double secondsLeft, double duration, string format, bool ending, Color normal, Color endingColor, float alpha)
        {
            ring.fillAmount = duration > 0 ? Mathf.Clamp01((float)(secondsLeft / duration)) : 0f;
            int whole = (int)Math.Ceiling(Math.Max(0, secondsLeft) - 1e-9);
            if (whole != _shownSeconds)
            {
                _shownSeconds = whole;
                seconds.SetText(format, whole);
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
