using MinersIdle.Game;
using MinersIdle.Presentation;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="GrayboxSetup"/> para o painel de debug do Marco 2 (E5), na cena Game já aberta: o EventSystem
    /// (sem ele nenhum botão recebe toque) e o painel com o botão "DBG" no canto de baixo. Os rótulos e o modelo das
    /// estatísticas são dados da cena. Só cria o que falta.
    /// </summary>
    static class DebugPanelSetup
    {
        const string PanelName = "DebugPanel";

        /// <summary>Modelo das estatísticas (ver o Tooltip de <c>DebugPanel.statsFormat</c>).</summary>
        const string StatsFormat =
            "Semente {0}\nTempo ×{1} · {2} min de jogo\nGolpes {3} · Quebras {4}\nLascas/min {5}\n" +
            "Cristais {6}/{7} · Alvo #{8} (lote {9})";

        static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.8f);
        static readonly Color ButtonColor = new Color32(0x3A, 0x2A, 0x1E, 0xFF);
        static readonly Color TextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);

        public static bool Ensure()
        {
            bool changed = EnsureEventSystem();

            var safeArea = GameObject.Find("SafeArea");
            if (safeArea == null || safeArea.transform.Find(PanelName) != null) return changed;

            var root = new GameObject(PanelName, typeof(RectTransform));
            root.transform.SetParent(safeArea.transform, false);
            Stretch((RectTransform)root.transform, Vector2.zero, Vector2.one);

            var toggle = CreateButton(root.transform, "Toggle", "DBG");
            var toggleRect = (RectTransform)toggle.transform;
            toggleRect.anchorMin = toggleRect.anchorMax = toggleRect.pivot = Vector2.zero;
            toggleRect.anchoredPosition = new Vector2(4f, 4f);
            toggleRect.sizeDelta = new Vector2(30f, 16f);

            var content = new GameObject("Content", typeof(RectTransform), typeof(Image));
            content.transform.SetParent(root.transform, false);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 0f);
            contentRect.pivot = new Vector2(0.5f, 0f);
            contentRect.anchoredPosition = new Vector2(0f, 24f);
            contentRect.sizeDelta = new Vector2(-8f, 150f);
            content.GetComponent<Image>().color = PanelColor;

            var stats = GrayboxSetup.CreateHudText(content.transform, "Stats", "", 7f, TextColor,
                                                   new Vector2(0f, 0.56f), new Vector2(0.64f, 1f));
            stats.alignment = TextAlignmentOptions.TopLeft;
            stats.margin = new Vector4(4f, 4f, 2f, 2f);

            // O rótulo de diagnóstico do M1 (versão, resolução, Safe Area, FPS) volta aqui dentro.
            var buildInfo = safeArea.transform.Find("BuildInfo");
            if (buildInfo != null)
            {
                buildInfo.SetParent(content.transform, false);
                Stretch((RectTransform)buildInfo, new Vector2(0.64f, 0.56f), new Vector2(1f, 1f));
                var label = buildInfo.GetComponent<TMP_Text>();
                label.fontSize = 7f;
                label.alignment = TextAlignmentOptions.TopRight;
                label.margin = new Vector4(2f, 4f, 4f, 2f);
                buildInfo.gameObject.SetActive(true);
            }

            var grid = new GameObject("Buttons", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(content.transform, false);
            Stretch((RectTransform)grid.transform, Vector2.zero, new Vector2(1f, 0.56f));
            var layout = grid.GetComponent<GridLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 2, 4);
            layout.cellSize = new Vector2(82f, 18f);
            layout.spacing = new Vector2(3f, 3f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;

            var panel = root.AddComponent<DebugPanel>();
            GrayboxSetup.SetReferences(panel,
                ("runner", Object.FindAnyObjectByType<MiningRunner>()),
                ("crystals", Object.FindAnyObjectByType<CrystalFieldView>()),
                ("targetLine", Object.FindAnyObjectByType<TargetLineView>()),
                ("toggleButton", toggle),
                ("content", content),
                ("stats", stats),
                ("speed1", CreateButton(grid.transform, "Speed1", "Tempo ×1")),
                ("speed5", CreateButton(grid.transform, "Speed5", "Tempo ×5")),
                ("speed20", CreateButton(grid.transform, "Speed20", "Tempo ×20")),
                ("spawnCrystal", CreateButton(grid.transform, "SpawnCrystal", "Gerar cristal")),
                ("breakTarget", CreateButton(grid.transform, "BreakTarget", "Quebrar alvo")),
                ("addShards", CreateButton(grid.transform, "AddShards", "+100 lascas")),
                ("restartSameSeed", CreateButton(grid.transform, "RestartSameSeed", "Reiniciar (mesma)")),
                ("restartNewSeed", CreateButton(grid.transform, "RestartNewSeed", "Reiniciar (nova)")),
                ("showOrder", CreateButton(grid.transform, "ShowOrder", "Mostrar ordem")));

            var serialized = new SerializedObject(panel);
            serialized.FindProperty("statsFormat").stringValue = StatsFormat;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            content.SetActive(false);
            Debug.Log("[Setup] Painel de debug criado na cena Game.");
            return true;
        }

        static bool EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return false;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            Debug.Log("[Setup] EventSystem (Input System) criado na cena Game.");
            return true;
        }

        static Button CreateButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = ButtonColor;
            GrayboxSetup.CreateHudText(go.transform, "Label", label, 7f, TextColor, Vector2.zero, Vector2.one);
            return go.GetComponent<Button>();
        }

        static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
