using System.Collections.Generic;
using MinersIdle.Game;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// M9i E5 (#17, #18), na cena Game:
    /// <list type="bullet">
    /// <item>"Sair do jogo?" (<see cref="ExitGameScreen"/>): mais uma cópia da confirmação do Apagar save (a janela da E3),
    /// com FICAR na madeira e SAIR no rubi, acima do aviso de versão;</item>
    /// <item>o aviso "Toque de novo para sair", no estilo do aviso do Descansar trancado, embaixo no meio;</item>
    /// <item>o <see cref="BackButtonHandler"/>, com as telas que respondem ao Voltar na ordem da cena (refeita a cada setup:
    /// uma tela nova entra sozinha);</item>
    /// <item>a bomba lê o modo dos AJUSTES (deslizar ou tocar) e ganha o "TOQUE".</item>
    /// </list>
    /// Roda depois do <see cref="VersionNoticeSetup"/>. Idempotente.
    /// </summary>
    static class BackAndBombSetup
    {
        const string ExitRootName = "ExitGame";
        const string BackName = "BackButton";
        const string TextsPath = "Assets/_Project/Data/FeedbackTexts.asset";
        const string TapReady = "TOQUE";

        // O aviso: como o do Descansar trancado (HudColumnV8Setup), no meio, acima do conjunto de baixo (a faixa da bomba
        // e, desde a M9i E7, os botões em cima dela: alto em 99).
        const float NoticeWidth = 150f;
        const float NoticeHeight = 18f;
        const float NoticeBottom = 104f;
        const float NoticeLabelSize = 33f / 4f;
        static readonly Color NoticeTextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
        static readonly Color MutedText = new Color32(0xBE, 0xB4, 0xAA, 0xFF);

        public static bool EnsureInScene()
        {
            bool changed = EnsureTexts();
            var safeArea = GameObject.Find("SafeArea")?.transform;
            var texts = AssetDatabase.LoadAssetAtPath<FeedbackTexts>(TextsPath);
            var settingsView = Object.FindAnyObjectByType<SettingsScreen>(FindObjectsInactive.Include);
            var runner = Object.FindAnyObjectByType<MiningRunner>();
            if (safeArea == null || texts == null || settingsView == null || runner == null) return changed;

            var exitRoot = safeArea.Find(ExitRootName);
            if (exitRoot == null)
            {
                var confirm = new SerializedObject(settingsView).FindProperty("confirm").objectReferenceValue as GameObject;
                if (confirm == null) return changed;
                exitRoot = CreateExit(safeArea, confirm, runner, texts);
                changed = true;
            }
            changed |= EnsureBackHandler(safeArea, exitRoot.GetComponent<ExitGameScreen>(), texts);
            changed |= EnsureBomb();
            return changed;
        }

        static Transform CreateExit(Transform safeArea, GameObject confirm, MiningRunner runner, FeedbackTexts texts)
        {
            var root = new GameObject(ExitRootName, typeof(RectTransform));
            root.transform.SetParent(safeArea, false);
            var version = safeArea.Find("VersionNotice");
            if (version != null) root.transform.SetSiblingIndex(version.GetSiblingIndex() + 1);
            else root.transform.SetAsLastSibling();
            Stretch((RectTransform)root.transform);

            var screen = Object.Instantiate(confirm, root.transform);
            screen.name = "Screen";
            screen.SetActive(false);
            var panel = screen.transform.Find("Panel");
            var saved = panel.Find("Warning").GetComponent<TMP_Text>();
            saved.name = "Saved";
            saved.color = MutedText;
            var stay = panel.Find("Buttons/Cancel").GetComponent<Button>();
            stay.name = "Stay";
            var leave = panel.Find("Buttons/Delete").GetComponent<Button>();
            leave.name = "Leave";

            var view = root.AddComponent<ExitGameScreen>();
            GrayboxSetup.SetReferences(view, ("runner", runner), ("texts", texts), ("screen", screen),
                ("title", panel.Find("Title").GetComponent<TMP_Text>()), ("body", panel.Find("Body").GetComponent<TMP_Text>()),
                ("saved", saved),
                ("stayButton", stay), ("stayLabel", stay.transform.Find("Label").GetComponent<TMP_Text>()),
                ("leaveButton", leave), ("leaveLabel", leave.transform.Find("Label").GetComponent<TMP_Text>()));
            Debug.Log("[Setup] \"Sair do jogo?\" com FICAR e SAIR na cena Game (M9i E5, #17).");
            return root.transform;
        }

        /// <summary>O tratador do Voltar e o aviso; a lista das telas é refeita sempre que muda.</summary>
        static bool EnsureBackHandler(Transform safeArea, ExitGameScreen exit, FeedbackTexts texts)
        {
            if (exit == null) return false;
            bool changed = false;
            var backTransform = safeArea.Find(BackName);
            BackButtonHandler handler;
            if (backTransform == null)
            {
                var go = new GameObject(BackName, typeof(RectTransform), typeof(BackButtonHandler));
                go.transform.SetParent(safeArea, false);
                go.transform.SetAsLastSibling();
                Stretch((RectTransform)go.transform);
                handler = go.GetComponent<BackButtonHandler>();
                var (notice, text) = Notice(go.transform);
                GrayboxSetup.SetReferences(handler, ("texts", texts), ("exit", exit), ("notice", notice), ("noticeText", text));
                Debug.Log("[Setup] O Voltar do Android: o aviso \"Toque de novo para sair\" e o \"Sair do jogo?\" (M9i E5).");
                changed = true;
            }
            else handler = backTransform.GetComponent<BackButtonHandler>();

            // As telas que respondem ao Voltar, na ordem da cena (a última é a de cima).
            var found = new List<MonoBehaviour>();
            foreach (var behaviour in safeArea.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour is IBackHandler) found.Add(behaviour);
            var so = new SerializedObject(handler);
            var list = so.FindProperty("handlers");
            bool same = list.arraySize == found.Count;
            for (int i = 0; same && i < found.Count; i++) same = list.GetArrayElementAtIndex(i).objectReferenceValue == found[i];
            if (same) return changed;
            list.arraySize = found.Count;
            for (int i = 0; i < found.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[Setup] Voltar: {found.Count} telas respondem a ele.");
            return true;
        }

        static (GameObject notice, TMP_Text text) Notice(Transform parent)
        {
            var notice = new GameObject("Notice", typeof(RectTransform), typeof(Image));
            notice.transform.SetParent(parent, false);
            var rect = (RectTransform)notice.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, NoticeBottom);
            rect.sizeDelta = new Vector2(NoticeWidth, NoticeHeight);
            var image = notice.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(UiSkinSetup.WoodFolder + "/panels/tooltip.png");
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            if (image.sprite == null) image.color = new Color32(0x18, 0x10, 0x0A, 0xE6);
            var text = GrayboxSetup.CreateHudText(notice.transform, "Text", "", NoticeLabelSize, NoticeTextColor,
                                                  Vector2.zero, Vector2.one);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            UiSkinSetup.ApplyGameFont(notice.transform);
            notice.SetActive(false);
            return (notice, text);
        }

        /// <summary>A bomba lê o modo dos AJUSTES e ganha o "TOQUE" no lugar do "ARRASTE".</summary>
        static bool EnsureBomb()
        {
            // Os dois estilos da faixa (o de agora e o do desbloqueio) são BombSliders: os dois obedecem.
            var settings = Object.FindAnyObjectByType<GameSettings>(FindObjectsInactive.Include);
            if (settings == null) return false;
            int wired = 0;
            foreach (var slider in Object.FindObjectsByType<BombSlider>(FindObjectsInactive.Include))
            {
                var so = new SerializedObject(slider);
                var field = so.FindProperty("settings");
                if (field.objectReferenceValue == settings) continue;
                field.objectReferenceValue = settings;
                so.FindProperty("tapReadyText").stringValue = TapReady;
                so.ApplyModifiedPropertiesWithoutUndo();
                wired++;
            }
            if (wired == 0) return false;
            Debug.Log($"[Setup] A bomba obedece aos AJUSTES: deslizar ou tocar ({wired} faixa(s), M9i E5, #18).");
            return true;
        }

        static bool EnsureTexts()
        {
            var texts = AssetDatabase.LoadAssetAtPath<FeedbackTexts>(TextsPath);
            if (texts == null || !string.IsNullOrEmpty(texts.ExitTitle)) return false;
            var so = new SerializedObject(texts);
            so.FindProperty("backAgainNotice").stringValue = "Toque de novo para sair";
            so.FindProperty("exitTitle").stringValue = "SAIR DO JOGO?";
            so.FindProperty("exitInternFormat").stringValue =
                "O estagiário continua minerando com o jogo fechado: até {0} de offline.";
            so.FindProperty("exitNoIntern").stringValue =
                "Sem o estagiário, nada é minerado com o jogo fechado. Ele aparece na LOJA.";
            so.FindProperty("exitSaved").stringValue = "O progresso fica salvo.";
            so.FindProperty("exitStay").stringValue = "FICAR";
            so.FindProperty("exitLeave").stringValue = "SAIR";
            so.FindProperty("settingsBombLabel").stringValue = "Lançar a bomba";
            so.FindProperty("settingsBombSwipe").stringValue = "DESLIZAR";
            so.FindProperty("settingsBombTap").stringValue = "TOCAR";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(texts);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Textos do Voltar, do \"Sair do jogo?\" e da bomba por toque (M9i E5).");
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
