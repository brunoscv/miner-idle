using System.Collections.Generic;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// A E3 do M9i (modais, mockup aprovado em 2026-11-19, D-336), na cena Game já montada. Em toda janela de madeira
    /// (<c>panels/window</c> do kit):
    /// <list type="bullet">
    /// <item>a janela passa a ser <c>window_title</c>: o mesmo desenho com a faixa do título de 20 px (era 8);</item>
    /// <item>o título vai a 11 px (era 8,25), centrado na faixa; o resto da barra (saldo, dia, fila, ícone) só desce
    /// para o meio da faixa nova;</item>
    /// <item>o X tem 24 px (o <c>icon_close</c> a 1,5×) e área de toque de 32 px; LOJA, Boost e Descansar ganham o X,
    /// ligado ao <c>Close</c> da tela, e o botão de baixo continua;</item>
    /// <item>o corpo desce 12 px: a janela de altura fixa cresce 12 px (para baixo; a que é presa embaixo, como a do raro,
    /// para cima), a que vai de ponta a ponta (LOJA, a tela do descanso) fica do mesmo tamanho e o miolo encolhe, e a que
    /// cresce com o conteúdo ganha 12 px no espaço de cima.</item>
    /// </list>
    /// Roda por último no setup. A janela que já usa <c>window_title</c> não muda mais: idempotente.
    /// </summary>
    static class ModalV8Setup
    {
        internal const string WindowPath = "Assets/_Project/Art/UI/window_title.png";

        // Referência 270 (D-14). A faixa do título começa a 6 px do alto da janela (as 6 linhas da moldura do desenho).
        const float BarTop = 6f;
        const float OldBarHeight = 8f;
        const float BarHeight = 20f;
        const float Grow = BarHeight - OldBarHeight;
        const float BarCenter = BarTop + BarHeight / 2f;
        const float TitleSize = 11f;     // 44/4: o próximo tamanho nítido da Pixelify depois de 8,25
        const float CloseSize = 24f;     // o icon_close (16) a 1,5×: 6 px de aparelho por pixel de arte no G75
        const float CloseTouch = 32f;
        const float CloseRight = 7f;     // da borda direita da janela até o X (a moldura do desenho)
        const float CloseGap = 4f;       // entre o texto da barra e o X
        static readonly Vector4 WindowBorder = new Vector4(8f, 8f, 8f, 16f + Grow); // esquerda, baixo, direita, cima

        public static bool EnsureInScene()
        {
            UiSkinSetup.ConfigureSprite(WindowPath, WindowBorder);
            var window = Load(WindowPath);
            var oldWindow = Load(UiSkinSetup.WoodFolder + "/panels/window.png");
            var close = Load(UiSkinSetup.WoodFolder + "/icons/icon_close.png");
            var safeArea = GameObject.Find("SafeArea")?.transform;
            if (window == null || oldWindow == null || close == null || safeArea == null) return false;

            // As telas que ganham o X (fecham sem escolher nada; o botão de baixo continua).
            var newX = new Dictionary<Transform, UnityAction>();
            AddX(newX, Object.FindAnyObjectByType<ShopView>(FindObjectsInactive.Include), v => v.Close);
            AddX(newX, Object.FindAnyObjectByType<BoostScreen>(FindObjectsInactive.Include), v => v.Close);
            AddX(newX, Object.FindAnyObjectByType<RestScreen>(FindObjectsInactive.Include), v => v.Close);

            int count = 0;
            foreach (var image in safeArea.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite != oldWindow) continue;
                var panel = (RectTransform)image.transform;
                UnityAction closeAction = null;
                foreach (var pair in newX)
                    if (panel.IsChildOf(pair.Key)) closeAction = pair.Value;
                Restyle(panel, image, window, close, closeAction);
                count++;
            }
            if (count == 0) return false;
            Debug.Log($"[Setup] Modais com a barra do título de 20 px, o título em 11 px e o X de 24 px (M9i E3): {count} janelas.");
            return true;
        }

        static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        static void AddX<T>(Dictionary<Transform, UnityAction> map, T view, System.Func<T, UnityAction> close)
            where T : MonoBehaviour
        {
            if (view == null) return;
            var screen = new SerializedObject(view).FindProperty("screen").objectReferenceValue as GameObject;
            if (screen != null) map[screen.transform] = close(view);
        }

        static void Restyle(RectTransform panel, Image image, Sprite window, Sprite closeSprite, UnityAction closeAction)
        {
            image.sprite = window;
            image.type = Image.Type.Sliced;

            // Quem é da barra e quem é do corpo, antes de mexer em qualquer coisa.
            var layout = panel.GetComponent<VerticalLayoutGroup>();
            var bar = new List<RectTransform>();
            var body = new List<RectTransform>();
            RectTransform existingX = null;
            foreach (Transform child in panel)
            {
                var rect = (RectTransform)child;
                var picture = child.GetComponent<Image>();
                if (picture != null && picture.sprite == closeSprite) { existingX = rect; continue; }
                if (layout != null && !IgnoresLayout(rect)) continue; // o layout cuida (o espaço de cima cresce)
                if (IsInBar(rect)) bar.Add(rect);
                else body.Add(rect);
            }

            // A janela cresce o que a barra cresceu (menos a que vai de ponta a ponta, que encolhe o miolo).
            if (layout != null)
            {
                layout.padding = new RectOffset(layout.padding.left, layout.padding.right,
                                                layout.padding.top + (int)Grow, layout.padding.bottom);
            }
            else if (Mathf.Approximately(panel.anchorMin.y, panel.anchorMax.y))
            {
                // O alto fica no lugar (cresce para baixo); presa embaixo (pivô 0), cresce para cima.
                panel.sizeDelta += new Vector2(0f, Grow);
                if (panel.pivot.y > 0f) panel.anchoredPosition -= new Vector2(0f, Grow * (1f - panel.pivot.y));
            }

            foreach (var rect in body) MoveBody(rect);

            bool hasX = existingX != null || closeAction != null;
            foreach (var rect in bar) MoveBar(rect, panel, hasX);

            if (existingX != null) StyleX(existingX, closeSprite);
            else if (closeAction != null)
            {
                var go = new GameObject("CloseX", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(panel, false);
                var button = go.GetComponent<Button>();
                UnityEventTools.AddPersistentListener(button.onClick, closeAction);
                StyleX((RectTransform)go.transform, closeSprite);
            }
        }

        static bool IgnoresLayout(RectTransform rect)
        {
            var element = rect.GetComponent<LayoutElement>();
            return element != null && element.ignoreLayout;
        }

        /// <summary>Preso no alto da janela e todo dentro da faixa velha (com folga): título, saldo, ícone, fila.</summary>
        static bool IsInBar(RectTransform rect)
        {
            if (!Mathf.Approximately(rect.anchorMin.y, 1f) || !Mathf.Approximately(rect.anchorMax.y, 1f)) return false;
            float top = rect.anchoredPosition.y + (1f - rect.pivot.y) * rect.sizeDelta.y;
            float bottom = top - rect.sizeDelta.y;
            return top >= -BarTop - 2f && bottom >= -(BarTop + OldBarHeight + 8f);
        }

        /// <summary>
        /// O corpo desce 12 px em relação ao alto da janela. Um ponto de âncora na altura <c>a</c> (0 embaixo, 1 em cima)
        /// precisa descer Grow·a, nos três tipos de janela: a que cresce para baixo já levou o ponto Grow·(1 − a) para
        /// baixo; a que cresce para cima levou o alto Grow para cima e o ponto Grow·a; a que cresce com o conteúdo (pivô
        /// no meio) levou o alto Grow/2 para cima e o ponto Grow·(a − ½); a que vai de ponta a ponta não mexeu em nada.
        /// </summary>
        static void MoveBody(RectTransform rect)
        {
            rect.offsetMax -= new Vector2(0f, Grow * rect.anchorMax.y);
            rect.offsetMin -= new Vector2(0f, Grow * rect.anchorMin.y);
        }

        /// <summary>Desce para o meio da faixa nova; o título cresce para 20 px de altura e 11 px de letra.</summary>
        static void MoveBar(RectTransform rect, RectTransform panel, bool hasX)
        {
            float top = rect.anchoredPosition.y + (1f - rect.pivot.y) * rect.sizeDelta.y;
            var text = rect.GetComponent<TMP_Text>();
            bool isTitle = rect.name == "Title" && text != null;
            if (isTitle)
            {
                text.fontSize = TitleSize;
                text.alignment = TextAlignmentOptions.MidlineLeft;
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, BarHeight);
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -BarTop - BarHeight + rect.pivot.y * BarHeight);
            }
            else
            {
                float center = top - rect.sizeDelta.y / 2f;
                rect.anchoredPosition += new Vector2(0f, -BarCenter - center);
            }

            // Com o X, nada da barra passa por baixo dele.
            if (!hasX) return;
            float limit = CloseRight + CloseSize + CloseGap;
            if (Mathf.Approximately(rect.anchorMax.x, 1f))
            {
                // Preso na direita (esticado ou não): a borda direita para antes do X.
                float right = Mathf.Approximately(rect.anchorMin.x, rect.anchorMax.x)
                    ? rect.anchoredPosition.x + (1f - rect.pivot.x) * rect.sizeDelta.x
                    : rect.offsetMax.x;
                if (right > -limit)
                {
                    if (Mathf.Approximately(rect.anchorMin.x, rect.anchorMax.x))
                        rect.anchoredPosition -= new Vector2(right + limit, 0f);
                    else rect.offsetMax = new Vector2(-limit, rect.offsetMax.y);
                }
            }
            else if (Mathf.Approximately(rect.anchorMin.x, 0f) && Mathf.Approximately(rect.anchorMax.x, 0f))
            {
                // Preso na esquerda com largura: encolhe até antes do X.
                float width = panel.rect.width;
                float right = rect.anchoredPosition.x + (1f - rect.pivot.x) * rect.sizeDelta.x;
                if (width > 0f && right > width - limit)
                    rect.sizeDelta -= new Vector2(right - (width - limit), 0f);
            }
        }

        /// <summary>O X: a área de toque de 32 px, transparente, com o desenho de 24 px no meio.</summary>
        static void StyleX(RectTransform rect, Sprite closeSprite)
        {
            rect.anchorMin = rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-CloseRight - CloseSize / 2f, -BarCenter);
            rect.sizeDelta = new Vector2(CloseTouch, CloseTouch);
            var touch = rect.GetComponent<Image>();
            touch.sprite = null;
            touch.color = new Color(1f, 1f, 1f, 0f);
            touch.raycastTarget = true;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(rect, false);
            var icon = iconGo.GetComponent<Image>();
            icon.sprite = closeSprite;
            icon.raycastTarget = false;
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(CloseSize, CloseSize);

            var button = rect.GetComponent<Button>();
            button.targetGraphic = icon;
            button.transition = Selectable.Transition.ColorTint;
        }
    }
}
