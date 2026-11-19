using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using static MinersIdle.EditorTools.RareWindowSetup;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// O menu do AD (o <see cref="BoostScreen"/>) mais alto e com letra maior (M9i E7, #22, pedido do Bruno em 2026-11-19:
    /// "as opções estão muito achatadas, as letras pequenas demais, o botão de assistir pequeno"):
    /// <list type="bullet">
    /// <item>cada linha passa de 38 para <b>52 px</b>, o ícone de 32 para 36;</item>
    /// <item>o nome do anúncio em <b>11 px</b> (era 8,25), a duração, o bônus e o detalhe em <b>8,25</b> (eram 7);</item>
    /// <item>o ASSISTIR de 66×18 para <b>70×26</b>, com a letra em 11 (era 8,25);</item>
    /// <item>a janela cresce para baixo (o alto fica onde está, logo abaixo do HUD): de 248 para 305 px, e de 252 para
    /// 262 de largura.</item>
    /// </list>
    /// Roda depois do <see cref="ModalV8Setup"/> (a barra do título de 20 px já está na janela). Idempotente.
    /// </summary>
    static class BoostV8Setup
    {
        // Medidas na tela de 270, contadas do alto à esquerda da janela (como no BoostSetup).
        const float PanelWidth = 262f;     // era 252: 4 px de margem; o nome maior ("Anúncio grande") e a duração cabem
        const float RowLeft = 6f;
        const float RowTop = 46f;          // logo abaixo da nota (a barra do título da E3 já empurrou tudo 12 px)
        const float RowHeight = 52f;
        const float RowGap = 3f;
        const float IconBox = 36f;
        const float IconInset = 4f;
        const float TextLeft = 46f;
        const float ButtonWidth = 70f;
        const float ButtonHeight = 26f;
        const float ButtonTop = 6f;
        const float Inner = 4f;            // da borda da linha ao botão e do texto ao botão
        const float BackWidth = 110f;
        const float BackHeight = 24f;
        const float BackGap = 10f;
        const float BottomPad = 8f;
        const float NoteTop = 30f;

        const float TitleText = 11f;       // o próximo da grade da Pixelify (2,75 × 4)
        const float BodyText = 33f / 4f;   // 8,25 (2,75 × 3)
        const float MinText = 22f / 4f;    // 5,5: o bônus e o detalhe encolhem antes de passar da linha

        public static bool EnsureInScene()
        {
            var view = Object.FindAnyObjectByType<BoostScreen>(FindObjectsInactive.Include);
            var panel = view != null ? view.transform.Find("Screen/Panel") as RectTransform : null;
            if (panel == null) return false;

            var rows = new System.Collections.Generic.List<RectTransform>();
            foreach (Transform child in panel)
                if (child.GetComponent<BoostRowView>() != null) rows.Add((RectTransform)child);
            if (rows.Count == 0 || (Mathf.Approximately(rows[0].sizeDelta.y, RowHeight)
                                    && Mathf.Approximately(panel.sizeDelta.x, PanelWidth)))
                return false;

            float width = PanelWidth;
            float rowWidth = width - 2f * RowLeft;
            for (int i = 0; i < rows.Count; i++)
                Row(rows[i], RowTop + i * (RowHeight + RowGap), rowWidth);

            float rowsBottom = RowTop + rows.Count * RowHeight + (rows.Count - 1) * RowGap;
            float height = rowsBottom + BackGap + BackHeight + BottomPad;
            if (panel.Find("Back") is RectTransform back)
            {
                Place(back, (width - BackWidth) / 2f, rowsBottom + BackGap, BackWidth, BackHeight);
                EditorUtility.SetDirty(back);
            }
            if (panel.Find("Note") is RectTransform note && note.GetComponent<TMP_Text>() is TMP_Text noteText)
            {
                Place(note, 8f, NoteTop, width - 16f, 12f);
                noteText.fontSize = BodyText;
                EditorUtility.SetDirty(noteText);
            }

            // O alto da janela fica onde está (pivô no meio): cresce só para baixo.
            float top = panel.anchoredPosition.y + panel.sizeDelta.y * (1f - panel.pivot.y);
            panel.sizeDelta = new Vector2(width, height);
            panel.anchoredPosition = new Vector2(panel.anchoredPosition.x, top - height * (1f - panel.pivot.y));
            EditorUtility.SetDirty(panel);

            Debug.Log($"[Setup] Menu do AD com as linhas de {RowHeight} px, letra maior e o ASSISTIR de {ButtonWidth}×{ButtonHeight} (M9i E7, #22).");
            return true;
        }

        static void Row(RectTransform row, float top, float rowWidth)
        {
            Place(row, RowLeft, top, rowWidth, RowHeight);
            float iconTop = (RowHeight - IconBox) / 2f;
            if (row.Find("Frame") is RectTransform frame)
            {
                Place(frame, Inner, iconTop, IconBox, IconBox);
                if (frame.Find("Icon") is RectTransform icon)
                    Place(icon, IconInset, IconInset, IconBox - 2f * IconInset, IconBox - 2f * IconInset);
            }

            float buttonLeft = rowWidth - ButtonWidth - Inner;
            float textWidth = buttonLeft - TextLeft - Inner;
            Text(row, "Title", TextLeft, 5f, textWidth, 14f, TitleText, BodyText);
            Text(row, "Duration", TextLeft, 5f, textWidth, 14f, BodyText, 0f);
            Text(row, "Effect", TextLeft, 21f, textWidth, 12f, BodyText, MinText);
            Text(row, "Detail", TextLeft, 34f, textWidth, 12f, BodyText, MinText);

            if (row.Find("Watch") is RectTransform watch)
            {
                Place(watch, buttonLeft, ButtonTop, ButtonWidth, ButtonHeight);
                if (watch.Find("Label")?.GetComponent<TMP_Text>() is TMP_Text label)
                {
                    label.fontSize = TitleText;
                    EditorUtility.SetDirty(label);
                }
            }
            if (row.Find("Left") is RectTransform left)
                Place(left, buttonLeft, ButtonTop + ButtonHeight + 3f, ButtonWidth, 11f);
            EditorUtility.SetDirty(row.gameObject);
        }

        /// <summary>Um texto da linha no lugar novo; com <paramref name="min"/> &gt; 0, encolhe até ele antes de transbordar.</summary>
        static void Text(RectTransform row, string name, float left, float top, float width, float height, float size, float min)
        {
            if (!(row.Find(name) is RectTransform rect) || !(rect.GetComponent<TMP_Text>() is TMP_Text text)) return;
            Place(rect, left, top, width, height);
            text.enableAutoSizing = min > 0f;
            text.fontSize = size;
            if (min > 0f)
            {
                text.fontSizeMin = min;
                text.fontSizeMax = size;
            }
            EditorUtility.SetDirty(text);
        }
    }
}
