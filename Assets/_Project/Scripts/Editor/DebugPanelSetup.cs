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
    /// Parte do <see cref="GrayboxSetup"/> para o painel de debug (M2 E5; compras desde o M3 E3; botão "Câmera" da descida
    /// desde o M6 E3), na cena Game já aberta:
    /// o EventSystem (sem ele nenhum botão recebe toque) e o painel com o botão "DBG" no canto de baixo. Os rótulos e os
    /// modelos de texto são dados da cena. Só cria o que falta; um painel de versão anterior (sem os botões de compra) é
    /// refeito do zero, porque é todo gerado aqui.
    /// </summary>
    static class DebugPanelSetup
    {
        const string PanelName = "DebugPanel";

        /// <summary>Modelo das estatísticas (ver o Tooltip de <c>DebugPanel.statsFormat</c>).</summary>
        const string StatsFormat =
            "Semente {0}\nTempo ×{1} · {2} min de jogo\nGolpes {3} · Quebras {4}\nLascas/min {5}\n" +
            "Cristais {6}/{7} · Alvo #{8} · Descida {9} un";

        /// <summary>Modelo do nível e próximo custo das melhorias (ver o Tooltip de <c>DebugPanel.upgradesFormat</c>).</summary>
        const string UpgradesFormat =
            "Picareta nv {0} (próx. {1}) · Velocidade nv {2} (próx. {3}) · Mineração nv {4} (próx. {5}) · " +
            "Dupla nv {6} (próx. {7}) · Botas nv {8} (próx. {9}) · Letrado nv {10} (próx. {11})";
        const string MaxedLabel = "máx.";

        /// <summary>Medição do M5 E5 (ver o Tooltip de <c>BuildInfoLabel.statsFormat</c>).</summary>
        const string FrameStatsFormat =
            "{0:0} fps (média {2:0})\n{1:1} min · lentos {3:0}\npior quadro {4:0} ms\nGC {5:0} quadros · {6:0} B";

        const float ContentHeight = 264f;
        const float TopBand = 0.70f;      // estatísticas e diagnóstico acima disto
        const float ButtonsBand = 0.53f;  // botões abaixo disto (7 fileiras); melhorias no meio

        static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.8f);
        static readonly Color ButtonColor = new Color32(0x3A, 0x2A, 0x1E, 0xFF);
        static readonly Color TextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);

        public static bool Ensure()
        {
            bool changed = EnsureEventSystem();

            var safeArea = GameObject.Find("SafeArea");
            if (safeArea == null) return changed;

            var existing = safeArea.transform.Find(PanelName);
            if (existing != null)
            {
                if (existing.Find("Content/Buttons/Camera") != null)
                    return UpdateStatsFormat(existing) | EnsureCaveButtons(existing) || changed;

                var oldInfo = existing.Find("Content/BuildInfo");
                if (oldInfo != null) oldInfo.SetParent(safeArea.transform, false);
                Object.DestroyImmediate(existing.gameObject);
                Debug.Log("[Setup] Painel de debug de versão anterior removido para ser refeito.");
            }

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
            contentRect.sizeDelta = new Vector2(-8f, ContentHeight);
            content.GetComponent<Image>().color = PanelColor;

            var stats = GrayboxSetup.CreateHudText(content.transform, "Stats", "", 7f, TextColor,
                                                   new Vector2(0f, TopBand), new Vector2(0.64f, 1f));
            stats.alignment = TextAlignmentOptions.TopLeft;
            stats.margin = new Vector4(4f, 4f, 2f, 2f);

            var frameStats = root.AddComponent<FrameStats>();

            // O rótulo de diagnóstico do M1 (versão, resolução, Safe Area, FPS) volta aqui dentro; desde o M5 (E5) com a
            // medição de quadros lentos e GC.
            var buildInfo = safeArea.transform.Find("BuildInfo");
            if (buildInfo != null)
            {
                buildInfo.SetParent(content.transform, false);
                Stretch((RectTransform)buildInfo, new Vector2(0.64f, TopBand), new Vector2(1f, 1f));
                var label = buildInfo.GetComponent<TMP_Text>();
                label.fontSize = 7f;
                label.alignment = TextAlignmentOptions.TopRight;
                label.margin = new Vector4(2f, 4f, 4f, 2f);
                buildInfo.gameObject.SetActive(true);

                var info = buildInfo.GetComponent<BuildInfoLabel>();
                GrayboxSetup.SetReferences(info, ("frameStats", frameStats));
                var infoSo = new SerializedObject(info);
                infoSo.FindProperty("statsFormat").stringValue = FrameStatsFormat;
                infoSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var upgrades = GrayboxSetup.CreateHudText(content.transform, "Upgrades", "", 7f, TextColor,
                                                      new Vector2(0f, ButtonsBand), new Vector2(1f, TopBand));
            upgrades.alignment = TextAlignmentOptions.TopLeft;
            upgrades.textWrappingMode = TextWrappingModes.Normal;
            upgrades.margin = new Vector4(4f, 2f, 4f, 2f);

            var grid = new GameObject("Buttons", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(content.transform, false);
            Stretch((RectTransform)grid.transform, Vector2.zero, new Vector2(1f, ButtonsBand));
            var layout = grid.GetComponent<GridLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 2, 4);
            layout.cellSize = new Vector2(82f, 16f);
            layout.spacing = new Vector2(3f, 3f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;

            var panel = root.AddComponent<DebugPanel>();
            GrayboxSetup.SetReferences(panel,
                ("runner", Object.FindAnyObjectByType<MiningRunner>()),
                ("tapInput", Object.FindAnyObjectByType<TapInput>()),
                ("screenShake", Object.FindAnyObjectByType<ScreenShake>()),
                ("soundBoard", Object.FindAnyObjectByType<SoundBoard>()),
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
                ("cameraButton", CreateButton(grid.transform, "Camera", "Câmera")),
                ("buyPickaxe", CreateButton(grid.transform, "BuyPickaxe", "Comprar Picareta")),
                ("buySpeed", CreateButton(grid.transform, "BuySpeed", "Comprar Velocidade")),
                ("buyMining", CreateButton(grid.transform, "BuyMining", "Comprar Mineração")),
                ("buyDoublePickaxe", CreateButton(grid.transform, "BuyDoublePickaxe", "Comprar P. Dupla")),
                ("buyBoots", CreateButton(grid.transform, "BuyBoots", "Comprar Botas")),
                ("saveNow", CreateButton(grid.transform, "SaveNow", "Salvar agora")),
                ("deleteSave", CreateButton(grid.transform, "DeleteSave", "Apagar save")),
                ("autoTap", CreateButton(grid.transform, "AutoTap", "Toques auto")),
                ("forceCritical", CreateButton(grid.transform, "ForceCritical", "Crítico sempre")),
                ("shake", CreateButton(grid.transform, "Shake", "Tremor on/off")),
                ("sound", CreateButton(grid.transform, "Sound", "Som on/off")),
                ("resetStats", CreateButton(grid.transform, "ResetStats", "Zerar medição")),
                ("fillCaveBreaks", CreateButton(grid.transform, "FillCaveBreaks", "Encher 100 cristais")),
                ("nextCave", CreateButton(grid.transform, "NextCave", "Próxima caverna")),
                ("lastCave", CreateButton(grid.transform, "LastCave", LastCaveLabel)),
                ("cameraPanel", Object.FindAnyObjectByType<DescentCameraPanel>()),
                ("frameStats", frameStats),
                ("upgrades", upgrades));

            var serialized = new SerializedObject(panel);
            serialized.FindProperty("statsFormat").stringValue = StatsFormat;
            serialized.FindProperty("upgradesFormat").stringValue = UpgradesFormat;
            serialized.FindProperty("maxedLabel").stringValue = MaxedLabel;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            content.SetActive(false);
            Debug.Log("[Setup] Painel de debug criado na cena Game.");
            return true;
        }

        /// <summary>M6 (E2): o modelo das estatísticas mudou (lote → distância descida); atualiza o texto da cena.</summary>
        static bool UpdateStatsFormat(Transform panel)
        {
            var serialized = new SerializedObject(panel.GetComponent<DebugPanel>());
            var format = serialized.FindProperty("statsFormat");
            if (format.stringValue == StatsFormat) return false;

            format.stringValue = StatsFormat;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Setup] Estatísticas do painel de debug atualizadas para a fila.");
            return true;
        }

        /// <summary>
        /// M8 (E1): "Encher 100 cristais" e "Próxima caverna" no fim da grade, e o Letrado nas melhorias; E3: "Caverna 10
        /// (fim)", ao lado deles.
        /// </summary>
        static bool EnsureCaveButtons(Transform panel)
        {
            bool changed = false;
            var serialized = new SerializedObject(panel.GetComponent<DebugPanel>());
            var format = serialized.FindProperty("upgradesFormat");
            if (format.stringValue != UpgradesFormat)
            {
                format.stringValue = UpgradesFormat;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }

            var grid = panel.Find("Content/Buttons");
            if (grid == null) return changed;

            if (grid.Find("FillCaveBreaks") == null)
            {
                GrayboxSetup.SetReferences(panel.GetComponent<DebugPanel>(),
                    ("fillCaveBreaks", CreateButton(grid, "FillCaveBreaks", "Encher 100 cristais")),
                    ("nextCave", CreateButton(grid, "NextCave", "Próxima caverna")));
                Debug.Log("[Setup] Painel de debug: atalhos das cavernas (M8).");
                changed = true;
            }

            // M8 (E3): o atalho do fim da descida.
            if (grid.Find("LastCave") == null)
            {
                GrayboxSetup.SetReferences(panel.GetComponent<DebugPanel>(),
                    ("lastCave", CreateButton(grid, "LastCave", LastCaveLabel)));
                Debug.Log("[Setup] Painel de debug: atalho da última caverna (M8).");
                changed = true;
            }
            return changed;
        }

        const string LastCaveLabel = "Caverna 10 (fim)";

        internal static bool EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return false;

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            Debug.Log("[Setup] EventSystem (Input System) criado na cena aberta.");
            return true;
        }

        internal static Button CreateButton(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = ButtonColor;
            GrayboxSetup.CreateHudText(go.transform, "Label", label, 7f, TextColor, Vector2.zero, Vector2.one);
            return go.GetComponent<Button>();
        }

        internal static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
