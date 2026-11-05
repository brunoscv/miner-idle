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
    /// refeito do zero, porque é todo gerado aqui. Desde 2026-11-05 (D-147) os atalhos vão para os menus Loja, Efeitos e
    /// Caverna (<see cref="DebugMenu"/>), que abrem em cima como a câmera; os botões são os mesmos objetos, só mudam de pai.
    /// </summary>
    static class DebugPanelSetup
    {
        const string PanelName = "DebugPanel";

        /// <summary>Modelo das estatísticas (ver o Tooltip de <c>DebugPanel.statsFormat</c>).</summary>
        const string StatsFormat =
            "Semente {0}\nTempo ×{1} · {2} min de jogo\nGolpes {3} · Quebras {4}\nLascas/min {5}\n" +
            "Cristais {6}/{7} · Alvo #{8} · Descida {9} un\n" +
            "Toques/s {10} valeram · {11} acima do teto\nDedos: {12} começos · {13} repetidos · {15} juntos · {14} na tela";

        /// <summary>Modelo do nível e próximo custo das melhorias (ver o Tooltip de <c>DebugPanel.upgradesFormat</c>).</summary>
        /// <remarks>A Dupla e as Botas saíram no GDD v7 (D-136): os índices {6}–{9} ficam sem uso.</remarks>
        const string UpgradesFormat =
            "Picareta nv {0} (próx. {1}) · Velocidade nv {2} (próx. {3})\nMineração nv {4} (próx. {5}) · " +
            "Crítico nv {12} (próx. {13})\nMapa nv {10} (próx. {11})";
        const string MaxedLabel = "máx.";

        /// <summary>Medição do M5 E5 (ver o Tooltip de <c>BuildInfoLabel.statsFormat</c>).</summary>
        const string FrameStatsFormat =
            "{0:0} fps (média {2:0})\n{1:1} min · lentos {3:0}\npior quadro {4:0} ms\nGC {5:0} quadros · {6:0} B";

        const float ContentHeight = 264f;
        // Marco 8b (E3, mockup aprovado): o DBG e o aviso do cálculo ficam acima da barra de baixo (Descansar e PASSAR).
        static readonly Vector2 TogglePosition = new Vector2(6f, 54f);
        internal static readonly Vector2 WarningPosition = new Vector2(40f, 54f);
        const float ContentBottom = 74f;
        const float TopBand = 0.70f;      // estatísticas e diagnóstico acima disto
        const float ButtonsBand = 0.53f;  // botões abaixo disto (7 fileiras); melhorias no meio

        static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.8f);
        static readonly Color ButtonColor = new Color32(0x3A, 0x2A, 0x1E, 0xFF);
        static readonly Color TextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);

        /// <summary>
        /// Marco 8b (E3): o botão DBG, o painel aberto e o aviso do cálculo sobem para cima da barra de baixo, onde antes
        /// ficavam por baixo do Descansar. Só no build de teste (o painel some no release). Devolve se mudou algo.
        /// </summary>
        internal static bool LiftAboveTheBar(Transform safeArea)
        {
            var panel = safeArea.Find(PanelName);
            if (panel == null || !(panel.Find("Toggle") is RectTransform toggle) || toggle.anchoredPosition == TogglePosition)
                return false;

            toggle.anchoredPosition = TogglePosition;
            if (panel.Find("Content") is RectTransform content) content.anchoredPosition = new Vector2(0f, ContentBottom);
            if (panel.Find("CalcWarning") is RectTransform warning) warning.anchoredPosition = WarningPosition;
            EditorUtility.SetDirty(panel.gameObject);
            Debug.Log("[Setup] Botão DBG e aviso do cálculo acima da barra de baixo (Marco 8b, E3).");
            return true;
        }

        public static bool Ensure()
        {
            bool changed = EnsureEventSystem();

            var safeArea = GameObject.Find("SafeArea");
            if (safeArea == null) return changed;

            var existing = safeArea.transform.Find(PanelName);
            if (existing != null)
            {
                if (existing.Find("Content/Buttons/Camera") != null)
                    return UpdateStatsFormat(existing) | EnsureCaveButtons(existing) | EnsureMenus(existing) || changed;

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
            toggleRect.anchoredPosition = TogglePosition;
            toggleRect.sizeDelta = new Vector2(30f, 16f);

            var content = new GameObject("Content", typeof(RectTransform), typeof(Image));
            content.transform.SetParent(root.transform, false);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 0f);
            contentRect.pivot = new Vector2(0.5f, 0f);
            contentRect.anchoredPosition = new Vector2(0f, ContentBottom);
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
                ("buyCritical", CreateButton(grid.transform, BuyCriticalName, BuyCriticalLabel)),
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
                ("tenLevels", CreateButton(grid.transform, TenLevelsName, TenLevelsLabel)),
                ("cameraPanel", Object.FindAnyObjectByType<DescentCameraPanel>()),
                ("frameStats", frameStats),
                ("upgrades", upgrades));

            var serialized = new SerializedObject(panel);
            serialized.FindProperty("statsFormat").stringValue = StatsFormat;
            serialized.FindProperty("upgradesFormat").stringValue = UpgradesFormat;
            serialized.FindProperty("maxedLabel").stringValue = MaxedLabel;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            content.SetActive(false);
            EnsureMenus(root.transform);
            Debug.Log("[Setup] Painel de debug criado na cena Game.");
            return true;
        }

        // ── Menus (D-147) ────────────────────────────────────────────────────────────────────────────────────────────

        /// <summary>Altura do DBG só com o tempo, as estatísticas e os botões dos menus.</summary>
        const float MenuContentHeight = 150f;
        const float MenuButtonsBand = 0.42f; // 3 fileiras de botões abaixo disto; estatísticas acima

        /// <summary>Um menu: nome do objeto, nome e rótulo do botão no DBG, altura e os botões que moram nele, na ordem.</summary>
        static readonly (string name, string button, string label, float height, string[] buttons)[] Menus =
        {
            ("ShopMenu", "Shop", "Loja", 128f, new[]
            {
                "SpawnCrystal", "AddShards", "FillCaveBreaks", "BuyPickaxe", "BuySpeed", "BuyMining", BuyCriticalName,
                TenLevelsName, "LastCave",
            }),
            ("EffectsMenu", "Effects", "Efeitos", 48f, new[] { "AutoTap", "ForceCritical", "Shake", "Sound" }),
            ("CaveMenu", "Cave", "Caverna", 68f, new[]
            {
                "BreakTarget", "NextCave", "SaveNow", "RestartSameSeed", "RestartNewSeed", "DeleteSave", "ResetStats",
            }),
            // Os botões dos Cálculos (E3b-3) nascem no próprio menu (CalculationsSetup), não vêm da grade do DBG.
            (CalcMenuName, "Calc", "Cálculos", CalcMenuHeight, System.Array.Empty<string>()),
            // Os do Dia (Marco 8b) também nascem no próprio menu (DayDebugSetup).
            (DayMenuName, "Day", "Dia", DayMenuHeight, System.Array.Empty<string>()),
        };

        internal const string DayMenuName = "DayMenu";

        /// <summary>O menu Dia: o resumo do dia e das permanentes no alto (<see cref="DayTextHeight"/>) e 2 fileiras de botões.</summary>
        internal const float DayMenuHeight = 80f;
        internal const float DayTextHeight = 32f;

        internal const string CalcMenuName = "CalcMenu";

        /// <summary>O menu Cálculos: a linha da Picareta no alto (<see cref="CalcTextHeight"/>) e 4 fileiras de botões.</summary>
        internal const float CalcMenuHeight = 106f;
        internal const float CalcTextHeight = 24f;

        /// <summary>A ordem dos botões que ficam no DBG.</summary>
        static readonly string[] MainButtons = { "Speed1", "Speed5", "Speed20", "Camera", "Shop", "Effects", "Cave", "Calc", "Day" };

        /// <summary>Altura da faixa do nível e custo das melhorias, no alto da Loja.</summary>
        const float ShopUpgradesHeight = 34f;

        /// <summary>
        /// Cria o que falta dos menus e põe neles os botões que ainda estão na grade do DBG (os de versões anteriores e os
        /// que algum passo do setup criou lá). Tira o "Comprar P. Dupla" (linha que saiu no GDD v7, D-136).
        /// </summary>
        static bool EnsureMenus(Transform panel)
        {
            var debugPanel = panel.GetComponent<DebugPanel>();
            var content = (RectTransform)panel.Find("Content");
            var grid = content != null ? content.Find("Buttons") : null;
            if (debugPanel == null || grid == null) return false;
            bool changed = false;

            var dupla = grid.Find("BuyDoublePickaxe");
            if (dupla != null)
            {
                Object.DestroyImmediate(dupla.gameObject);
                Debug.Log("[Setup] Painel de debug: \"Comprar P. Dupla\" removido (a Dupla saiu no GDD v7, D-136).");
                changed = true;
            }

            foreach (var (name, buttonName, label, height, buttons) in Menus)
            {
                var menu = panel.Find(name);
                if (menu == null)
                {
                    menu = CreateMenu(panel, name, height,
                                      name == "ShopMenu" ? ShopUpgradesHeight : name == CalcMenuName ? CalcTextHeight
                                      : name == DayMenuName ? DayTextHeight : 0f);
                    var open = CreateButton(grid, buttonName, label);
                    GrayboxSetup.SetReferences(debugPanel, (MenuField(name, "Button"), open),
                                               (MenuField(name, "Menu"), menu.GetComponent<DebugMenu>()));
                    Debug.Log($"[Setup] Painel de debug: menu \"{label}\" (D-147).");
                    changed = true;
                }

                var menuButtons = menu.Find("Content/Buttons");
                foreach (var b in buttons)
                {
                    var button = grid.Find(b);
                    if (button == null) continue;
                    button.SetParent(menuButtons, false);
                    changed = true;
                }
                // Na ordem da lista, com o Fechar por último.
                int index = 0;
                foreach (var b in buttons)
                {
                    var child = menuButtons.Find(b);
                    if (child != null) child.SetSiblingIndex(index++);
                }
                var close = menuButtons.Find("Close");
                if (close != null) close.SetAsLastSibling();
            }

            // O nível e o custo das melhorias vão para o alto da Loja.
            var upgrades = content.Find("Upgrades");
            var shopContent = panel.Find("ShopMenu/Content");
            if (upgrades != null && shopContent != null)
            {
                upgrades.SetParent(shopContent, false);
                var rect = (RectTransform)upgrades;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.offsetMin = new Vector2(0f, -ShopUpgradesHeight);
                rect.offsetMax = Vector2.zero;
                changed = true;
            }

            for (int i = 0; i < MainButtons.Length; i++)
            {
                var child = grid.Find(MainButtons[i]);
                if (child != null && child.GetSiblingIndex() != i)
                {
                    child.SetSiblingIndex(i);
                    changed = true;
                }
            }

            // O DBG fica só com o tempo, as estatísticas e os botões dos menus.
            if (!Mathf.Approximately(content.sizeDelta.y, MenuContentHeight))
            {
                content.sizeDelta = new Vector2(content.sizeDelta.x, MenuContentHeight);
                Stretch((RectTransform)grid, Vector2.zero, new Vector2(1f, MenuButtonsBand));
                var stats = content.Find("Stats");
                if (stats != null) Stretch((RectTransform)stats, new Vector2(0f, MenuButtonsBand), new Vector2(0.64f, 1f));
                var info = content.Find("BuildInfo");
                if (info != null) Stretch((RectTransform)info, new Vector2(0.64f, MenuButtonsBand), new Vector2(1f, 1f));
                changed = true;
            }
            return changed;
        }

        /// <summary>"ShopMenu" + "Button" → "shopButton"; + "Menu" → "shopMenu" (os campos do DebugPanel).</summary>
        static string MenuField(string menuName, string suffix)
        {
            string stem = menuName.Substring(0, menuName.Length - "Menu".Length);
            return char.ToLowerInvariant(stem[0]) + stem.Substring(1) + suffix;
        }

        /// <summary>Um menu em cima da tela, logo abaixo do HUD, como os controles da câmera; o Fechar já vem na grade.</summary>
        static Transform CreateMenu(Transform panel, string name, float height, float textTop)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(panel, false);
            Stretch((RectTransform)root.transform, Vector2.zero, Vector2.one);

            var content = new GameObject("Content", typeof(RectTransform), typeof(Image));
            content.transform.SetParent(root.transform, false);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = new Vector2(0f, -42f); // logo abaixo do HUD, como a câmera
            contentRect.sizeDelta = new Vector2(-8f, height);
            content.GetComponent<Image>().color = PanelColor; // recebe raycast: tocar no menu não vira toque no jogo

            var grid = new GameObject("Buttons", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(content.transform, false);
            var gridRect = (RectTransform)grid.transform;
            Stretch(gridRect, Vector2.zero, Vector2.one);
            gridRect.offsetMax = new Vector2(0f, -textTop);
            var layout = grid.GetComponent<GridLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.cellSize = new Vector2(82f, 16f);
            layout.spacing = new Vector2(3f, 3f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;

            var close = CreateButton(grid.transform, "Close", "Fechar");
            var menu = root.AddComponent<DebugMenu>();
            GrayboxSetup.SetReferences(menu, ("content", content), ("close", close));
            return root.transform;
        }

        /// <summary>Um botão do painel onde quer que esteja (na grade do DBG ou já dentro de um menu).</summary>
        static Transform FindButton(Transform panel, string name)
        {
            var inGrid = panel.Find("Content/Buttons/" + name);
            if (inGrid != null) return inGrid;
            foreach (var menu in Menus)
            {
                var inMenu = panel.Find($"{menu.name}/Content/Buttons/{name}");
                if (inMenu != null) return inMenu;
            }
            return null;
        }

        /// <summary>
        /// O modelo das estatísticas mudou (M6 E2: lote → distância descida; 2026-11-01: o diagnóstico do toque, D-118);
        /// atualiza o texto da cena.
        /// </summary>
        static bool UpdateStatsFormat(Transform panel)
        {
            var serialized = new SerializedObject(panel.GetComponent<DebugPanel>());
            var format = serialized.FindProperty("statsFormat");
            if (format.stringValue == StatsFormat) return false;

            format.stringValue = StatsFormat;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[Setup] Estatísticas do painel de debug atualizadas (diagnóstico do toque).");
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

            if (FindButton(panel, "FillCaveBreaks") == null)
            {
                GrayboxSetup.SetReferences(panel.GetComponent<DebugPanel>(),
                    ("fillCaveBreaks", CreateButton(grid, "FillCaveBreaks", "Encher 100 cristais")),
                    ("nextCave", CreateButton(grid, "NextCave", "Próxima caverna")));
                Debug.Log("[Setup] Painel de debug: atalhos das cavernas (M8).");
                changed = true;
            }

            // E3b do M8: o Comprar Botas (linha desligada desde o GDD v5) vira Comprar Crítico, no mesmo lugar.
            var boots = grid.Find("BuyBoots");
            if (FindButton(panel, BuyCriticalName) == null && boots != null)
            {
                boots.name = BuyCriticalName;
                boots.GetComponentInChildren<TMP_Text>().text = BuyCriticalLabel;
                GrayboxSetup.SetReferences(panel.GetComponent<DebugPanel>(), ("buyCritical", boots.GetComponent<Button>()));
                Debug.Log("[Setup] Painel de debug: Comprar Crítico no lugar de Comprar Botas (E3b do M8).");
                changed = true;
            }

            // M8 (E3): o atalho do fim da descida.
            if (FindButton(panel, "LastCave") == null)
            {
                GrayboxSetup.SetReferences(panel.GetComponent<DebugPanel>(),
                    ("lastCave", CreateButton(grid, "LastCave", LastCaveLabel)));
                Debug.Log("[Setup] Painel de debug: atalho da última caverna (M8).");
                changed = true;
            }

            // E3b-2 do M8 (D-129): os 100 níveis do v7. O fim vira "Nível 100 (fim)" e entra o "+10 níveis", para ver os biomas.
            var lastCaveLabel = FindButton(panel, "LastCave")?.GetComponentInChildren<TMP_Text>();
            if (lastCaveLabel != null && lastCaveLabel.text != LastCaveLabel)
            {
                lastCaveLabel.text = LastCaveLabel;
                changed = true;
            }
            if (FindButton(panel, TenLevelsName) == null)
            {
                GrayboxSetup.SetReferences(panel.GetComponent<DebugPanel>(),
                    ("tenLevels", CreateButton(grid, TenLevelsName, TenLevelsLabel)));
                Debug.Log("[Setup] Painel de debug: \"Nível 100 (fim)\" e \"+10 níveis\" (E3b-2 do M8).");
                changed = true;
            }
            return changed;
        }

        const string LastCaveLabel = "Nível 100 (fim)";
        const string TenLevelsName = "TenLevels";
        const string TenLevelsLabel = "+10 níveis";
        const string BuyCriticalName = "BuyCritical";
        const string BuyCriticalLabel = "Comprar Crítico";

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
