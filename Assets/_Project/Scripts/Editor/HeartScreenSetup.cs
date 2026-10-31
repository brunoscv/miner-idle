using MinersIdle.Game;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="GrayboxSetup"/> para o fim da descida (M8, E3, D-112): a tela "Coração da montanha" na cena
    /// Game já aberta, por cima de tudo no Safe Area. Graybox nas cores da loja, até a arte. Só cria o que falta.
    /// </summary>
    static class HeartScreenSetup
    {
        const string TextsPath = "Assets/_Project/Data/FeedbackTexts.asset";
        const string RootName = "HeartScreen";

        static readonly Color DimColor = new Color(0f, 0f, 0f, 0.75f);
        static readonly Color PanelColor = new Color32(0x1B, 0x13, 0x0D, 0xF5);
        static readonly Color ButtonColor = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        static readonly Color TitleColor = new Color32(0xFF, 0xD0, 0x6A, 0xFF);
        static readonly Color TextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        static readonly Color DarkText = new Color32(0x15, 0x19, 0x1C, 0xFF);

        public static bool EnsureInScene()
        {
            var safeArea = GameObject.Find("SafeArea");
            if (safeArea == null || safeArea.transform.Find(RootName) != null) return false;

            var root = new GameObject(RootName, typeof(RectTransform));
            root.transform.SetParent(safeArea.transform, false);
            root.transform.SetAsLastSibling();
            Stretch((RectTransform)root.transform);

            var screen = new GameObject("Screen", typeof(RectTransform), typeof(Image));
            screen.transform.SetParent(root.transform, false);
            Stretch((RectTransform)screen.transform);
            screen.GetComponent<Image>().color = DimColor; // raycastTarget ligado: bloqueia o mapa, não pausa nada

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(screen.transform, false);
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(220f, 170f);
            panel.GetComponent<Image>().color = PanelColor;

            var title = GrayboxSetup.CreateHudText(panel.transform, "Title", "", 13f, TitleColor,
                                                   new Vector2(0f, 0.74f), new Vector2(1f, 1f));
            var body = GrayboxSetup.CreateHudText(panel.transform, "Body", "", 8f, TextColor,
                                                  new Vector2(0f, 0.28f), new Vector2(1f, 0.74f));
            body.textWrappingMode = TextWrappingModes.Normal;
            body.margin = new Vector4(10f, 0f, 10f, 0f);

            var button = new GameObject("Continue", typeof(RectTransform), typeof(Image), typeof(Button));
            button.transform.SetParent(panel.transform, false);
            var buttonRect = (RectTransform)button.transform;
            buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, 12f);
            buttonRect.sizeDelta = new Vector2(120f, 26f);
            button.GetComponent<Image>().color = ButtonColor;
            var label = GrayboxSetup.CreateHudText(button.transform, "Label", "", 9f, DarkText, Vector2.zero, Vector2.one);

            screen.SetActive(false);

            var view = root.AddComponent<HeartScreen>();
            GrayboxSetup.SetReferences(view,
                ("runner", Object.FindAnyObjectByType<MiningRunner>()),
                ("texts", AssetDatabase.LoadAssetAtPath<FeedbackTexts>(TextsPath)),
                ("screen", screen), ("title", title), ("body", body),
                ("continueButton", button.GetComponent<Button>()), ("continueLabel", label));

            Debug.Log("[Setup] Tela do coração da montanha criada na cena Game (M8).");
            return true;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
