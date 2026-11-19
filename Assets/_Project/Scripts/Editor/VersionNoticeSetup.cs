using MinersIdle.Game;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// M9i E4 (D-333, D-337): o aviso de versão nova na cena Game. É uma cópia da confirmação do Apagar save dos AJUSTES
    /// (a mesma janela, já com a barra da E3): o título, o texto, as duas versões no lugar do aviso em âmbar, CONTINUAR na
    /// madeira e RECOMEÇAR no rubi. Fica logo acima do "Bem-vindo de volta". E o atalho DBG → Offline → "Aviso de
    /// versão". Roda depois do <see cref="ModalV8Setup"/>. Idempotente.
    /// </summary>
    static class VersionNoticeSetup
    {
        const string RootName = "VersionNotice";
        const string TextsPath = "Assets/_Project/Data/FeedbackTexts.asset";
        const string DebugButtonName = "VersionNotice";
        const string DebugButtonLabel = "Aviso de versão";
        static readonly Color MutedText = new Color32(0xBE, 0xB4, 0xAA, 0xFF);

        public static bool EnsureInScene()
        {
            bool changed = EnsureTexts();
            var safeArea = GameObject.Find("SafeArea")?.transform;
            var texts = AssetDatabase.LoadAssetAtPath<FeedbackTexts>(TextsPath);
            var settings = Object.FindAnyObjectByType<SettingsScreen>(FindObjectsInactive.Include);
            var runner = Object.FindAnyObjectByType<MiningRunner>();
            if (safeArea == null || texts == null || settings == null || runner == null) return changed;

            var root = safeArea.Find(RootName);
            if (root == null)
            {
                var confirm = new SerializedObject(settings).FindProperty("confirm").objectReferenceValue as GameObject;
                if (confirm == null) return changed;
                root = CreateNotice(safeArea, confirm, runner, texts);
                changed = true;
            }
            changed |= EnsureDebugButton(root.GetComponent<VersionNoticeScreen>());
            return changed;
        }

        static Transform CreateNotice(Transform safeArea, GameObject confirm, MiningRunner runner, FeedbackTexts texts)
        {
            var root = new GameObject(RootName, typeof(RectTransform));
            root.transform.SetParent(safeArea, false);
            // Acima do "Bem-vindo de volta" (o save velho vem antes das lascas do offline).
            var offline = safeArea.Find("OfflineReturn");
            if (offline != null) root.transform.SetSiblingIndex(offline.GetSiblingIndex() + 1);
            else root.transform.SetAsLastSibling();
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;

            var screen = Object.Instantiate(confirm, root.transform);
            screen.name = "Screen";
            screen.SetActive(false);
            var panel = screen.transform.Find("Panel");
            var numbers = panel.Find("Warning").GetComponent<TMP_Text>();
            numbers.name = "Numbers";
            numbers.color = MutedText;
            var continueButton = panel.Find("Buttons/Cancel").GetComponent<Button>();
            continueButton.name = "Continue";
            var restartButton = panel.Find("Buttons/Delete").GetComponent<Button>();
            restartButton.name = "Restart";

            var view = root.AddComponent<VersionNoticeScreen>();
            GrayboxSetup.SetReferences(view, ("runner", runner), ("texts", texts), ("screen", screen),
                ("title", panel.Find("Title").GetComponent<TMP_Text>()), ("body", panel.Find("Body").GetComponent<TMP_Text>()),
                ("numbers", numbers),
                ("restartButton", restartButton), ("restartLabel", restartButton.transform.Find("Label").GetComponent<TMP_Text>()),
                ("continueButton", continueButton), ("continueLabel", continueButton.transform.Find("Label").GetComponent<TMP_Text>()));
            Debug.Log("[Setup] Aviso de versão nova (RECOMEÇAR e CONTINUAR) na cena Game (M9i E4, D-337).");
            return root.transform;
        }

        /// <summary>DBG → Offline → "Aviso de versão", antes do Fechar (a grade de 3 fileiras tinha uma vaga).</summary>
        static bool EnsureDebugButton(VersionNoticeScreen view)
        {
            if (view == null) return false;
            var so = new SerializedObject(view);
            var field = so.FindProperty("debugOpen");
            if (field.objectReferenceValue != null) return false;
            var debugPanel = Object.FindAnyObjectByType<DebugPanel>(FindObjectsInactive.Include);
            var grid = debugPanel != null ? debugPanel.transform.Find(DebugPanelSetup.OfflineMenuName + "/Content/Buttons") : null;
            if (grid == null) return false;
            var existing = grid.Find(DebugButtonName);
            var button = existing != null ? existing.GetComponent<Button>()
                                          : DebugPanelSetup.CreateButton(grid, DebugButtonName, DebugButtonLabel);
            grid.Find("Close")?.SetAsLastSibling();
            field.objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Setup] Painel de debug: \"Aviso de versão\" no menu Offline (M9i E4).");
            return true;
        }

        static bool EnsureTexts()
        {
            var texts = AssetDatabase.LoadAssetAtPath<FeedbackTexts>(TextsPath);
            if (texts == null || !string.IsNullOrEmpty(texts.VersionTitle)) return false;
            var so = new SerializedObject(texts);
            so.FindProperty("versionTitle").stringValue = "VERSÃO NOVA";
            so.FindProperty("versionBody").stringValue =
                "Versão nova do jogo — para ver tudo o que mudou, recomendamos recomeçar.";
            so.FindProperty("versionNumbersFormat").stringValue = "Seu save: {0} · Jogo: {1}";
            so.FindProperty("versionUnknown").stringValue = "anterior";
            so.FindProperty("versionRestart").stringValue = "RECOMEÇAR";
            so.FindProperty("versionContinue").stringValue = "CONTINUAR";
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(texts);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Textos do aviso de versão nova (M9i E4).");
            return true;
        }
    }
}
