using MinersIdle.Game;
using MinersIdle.Presentation;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="GrayboxSetup"/> para a descida em corrida na cena Game (M6, E3; D-52, D-61), com a cena já
    /// aberta: tira a vista de cima do mapa (chão, cristais, anão, linha até o alvo), cria o palco da descida
    /// (<see cref="DescentStage"/> com a câmera de perspectiva, longe da cena 2D) e o painel de câmera do DBG
    /// (<see cref="DescentCameraPanel"/>). Garante também um renderer Universal no URP (o do projeto já é, apesar do nome
    /// <c>URP_2D_Renderer</c>). Como o resto do setup, só cria o que falta.
    /// </summary>
    static class DescentSetup
    {
        const string ShaderPath = "Assets/_Project/Shaders/DescentUnlit.shader";
        const string ScreenShaderName = "Universal Render Pipeline/Unlit";
        const string UniversalRendererPath = "Assets/_Project/Settings/Rendering/URP_2D_Universal_Renderer.asset";
        const string MiningRootName = "Mining";
        const string StageName = "Descent";
        const string CameraPanelName = "CameraPanel";

        /// <summary>O último objeto que a versão atual do painel de câmera cria (a linha da intensidade dos efeitos, E4).</summary>
        const string CameraPanelMarker = "Content/Rows/Row5";

        /// <summary>O 3D fica bem abaixo da cena 2D, fora da vista da câmera de cima.</summary>
        static readonly Vector3 StagePosition = new Vector3(0f, -1000f, 0f);

        /// <summary>As views da vista de cima do mapa (M2 a M6 E2), que saem na E3 (D-61).</summary>
        static readonly string[] MapViewNames = { "Map", "Crystals", "Miner", "TargetLine" };

        static readonly Color FogColor = new Color32(0x14, 0x0D, 0x09, 0xFF);
        static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.8f);
        static readonly Color TextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);

        /// <summary>Rótulos das linhas de ajuste, na ordem de <see cref="DescentCameraPanel.Setting"/>.</summary>
        static readonly string[] RowFormats =
        {
            "Altura da câmera: {0} m",
            "Inclinação: {0}°",
            "Distância até o anão: {0} m",
            "Campo de visão: {0}°",
            "Largura do corredor: {0} m",
            "Intensidade dos efeitos: ×{0}",
        };

        /// <summary>Modelo da linha de medidas (ver o Tooltip de <c>DescentCameraPanel.readoutFormat</c>).</summary>
        const string ReadoutFormat =
            "Anão: cabeça {0}% · pés {1}% do topo · {2}% da tela\nChão à frente: {3} m (~{4} s até o cristal)\nRender {5}×{6}";

        public static bool EnsureInGameScene()
        {
            int rendererIndex = EnsureUniversalRenderer();
            if (rendererIndex < 0) return false;

            var mining = GameObject.Find(MiningRootName);
            if (mining == null) return false;

            bool changed = RemoveMapViews(mining.transform);
            changed |= EnsureStage(mining, rendererIndex);
            changed |= EnsureCameraPanel();
            return changed;
        }

        /// <summary>O índice de um renderer Universal na lista do URP; se não houver, acrescenta um sem mexer no padrão.</summary>
        static int EnsureUniversalRenderer()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(ProjectSetup.UrpAssetPath);
            if (pipeline == null)
            {
                Debug.LogError($"[Setup] URP não encontrado em {ProjectSetup.UrpAssetPath}; descida não criada.");
                return -1;
            }

            var serialized = new SerializedObject(pipeline);
            var list = serialized.FindProperty("m_RendererDataList");
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue is UniversalRendererData)
                    return i;

            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(UniversalRendererPath)
                       ?? ProjectSetup.CreateRendererData("UniversalRenderer", "Universal_Renderer")
                       ?? ProjectSetup.CreateRendererDirect<UniversalRendererData>(UniversalRendererPath);

            int index = list.arraySize;
            list.arraySize++;
            list.GetArrayElementAtIndex(index).objectReferenceValue = data;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Setup] Renderer Universal adicionado ao URP (índice {index}), para a descida.");
            return index;
        }

        static bool RemoveMapViews(Transform mining)
        {
            bool removed = false;
            foreach (var name in MapViewNames)
            {
                var view = mining.Find(name);
                if (view == null) continue;
                Object.DestroyImmediate(view.gameObject);
                removed = true;
            }
            if (removed) Debug.Log("[Setup] Vista de cima do mapa removida da cena Game (D-61).");
            return removed;
        }

        static bool EnsureStage(GameObject mining, int rendererIndex)
        {
            if (mining.transform.Find(StageName) != null) return false;

            var root = new GameObject(StageName);
            root.transform.SetParent(mining.transform, false);
            root.transform.localPosition = StagePosition;

            var sceneCamera = new GameObject("DescentCamera", typeof(Camera)).GetComponent<Camera>();
            sceneCamera.transform.SetParent(root.transform, false);
            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.backgroundColor = FogColor;
            sceneCamera.nearClipPlane = 0.1f;
            sceneCamera.farClipPlane = 45f;
            sceneCamera.depth = -10; // desenha a textura antes da câmera da tela usá-la
            var cameraData = sceneCamera.GetUniversalAdditionalCameraData();
            cameraData.SetRenderer(rendererIndex);
            cameraData.renderPostProcessing = false;
            cameraData.renderShadows = false;

            var stage = root.AddComponent<DescentStage>();
            GrayboxSetup.SetReferences(stage,
                ("runner", mining.GetComponent<MiningRunner>()),
                ("shader", AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath)),
                ("screenShader", Shader.Find(ScreenShaderName)),
                ("sceneCamera", sceneCamera),
                ("screenCamera", Camera.main));

            Debug.Log("[Setup] Descida criada na cena Game (câmera da D-50, corredor 3D).");
            return true;
        }

        /// <summary>O painel de câmera do DBG: abaixo do HUD, fica aberto com o DBG fechado.</summary>
        static bool EnsureCameraPanel()
        {
            var safeArea = GameObject.Find("SafeArea");
            if (safeArea == null) return false;

            var existing = safeArea.transform.Find(CameraPanelName);
            if (existing != null)
            {
                if (existing.Find(CameraPanelMarker) != null) return false;
                Object.DestroyImmediate(existing.gameObject);
                Debug.Log("[Setup] Painel de câmera de versão anterior removido para ser refeito.");
            }

            var root = new GameObject(CameraPanelName, typeof(RectTransform));
            root.transform.SetParent(safeArea.transform, false);
            DebugPanelSetup.Stretch((RectTransform)root.transform, Vector2.zero, Vector2.one);

            var content = new GameObject("Content", typeof(RectTransform), typeof(Image));
            content.transform.SetParent(root.transform, false);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = new Vector2(0f, -42f); // logo abaixo do HUD
            contentRect.sizeDelta = new Vector2(-8f, 166f);
            content.GetComponent<Image>().color = PanelColor; // recebe raycast: tocar no painel não vira toque no jogo

            var readout = GrayboxSetup.CreateHudText(content.transform, "Readout", "", 7f, TextColor,
                                                     new Vector2(0f, 0.75f), new Vector2(1f, 1f));
            readout.alignment = TextAlignmentOptions.TopLeft;
            readout.margin = new Vector4(4f, 4f, 4f, 2f);

            var rowsRoot = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup));
            rowsRoot.transform.SetParent(content.transform, false);
            DebugPanelSetup.Stretch((RectTransform)rowsRoot.transform, new Vector2(0f, 0.2f), new Vector2(1f, 0.75f));
            var rowsLayout = rowsRoot.GetComponent<VerticalLayoutGroup>();
            rowsLayout.padding = new RectOffset(4, 4, 1, 1);
            rowsLayout.spacing = 1f;
            rowsLayout.childControlWidth = rowsLayout.childControlHeight = true;
            rowsLayout.childForceExpandWidth = rowsLayout.childForceExpandHeight = true;

            var rows = new (TMP_Text label, Button minus, Button plus)[RowFormats.Length];
            for (int i = 0; i < RowFormats.Length; i++)
            {
                var row = new GameObject($"Row{i}", typeof(RectTransform));
                row.transform.SetParent(rowsRoot.transform, false);
                var label = GrayboxSetup.CreateHudText(row.transform, "Label", "", 7f, TextColor, Vector2.zero, new Vector2(0.62f, 1f));
                label.alignment = TextAlignmentOptions.Left;
                var minus = DebugPanelSetup.CreateButton(row.transform, "Minus", "-");
                DebugPanelSetup.Stretch((RectTransform)minus.transform, new Vector2(0.63f, 0f), new Vector2(0.8f, 1f));
                var plus = DebugPanelSetup.CreateButton(row.transform, "Plus", "+");
                DebugPanelSetup.Stretch((RectTransform)plus.transform, new Vector2(0.82f, 0f), new Vector2(0.99f, 1f));
                rows[i] = (label, minus, plus);
            }

            var grid = new GameObject("Buttons", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(content.transform, false);
            DebugPanelSetup.Stretch((RectTransform)grid.transform, Vector2.zero, new Vector2(1f, 0.2f));
            var gridLayout = grid.GetComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(4, 4, 4, 4);
            gridLayout.cellSize = new Vector2(82f, 16f);
            gridLayout.spacing = new Vector2(3f, 3f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;

            var panel = root.AddComponent<DescentCameraPanel>();
            GrayboxSetup.SetReferences(panel,
                ("stage", Object.FindAnyObjectByType<DescentStage>()),
                ("runner", Object.FindAnyObjectByType<MiningRunner>()),
                ("feedback", Object.FindAnyObjectByType<MiningFeedbackView>()),
                ("content", content),
                ("readout", readout),
                ("resolution", DebugPanelSetup.CreateButton(grid.transform, "Resolution", "Resolução")),
                ("resetFraming", DebugPanelSetup.CreateButton(grid.transform, "ResetFraming", "Padrão F1")),
                ("close", DebugPanelSetup.CreateButton(grid.transform, "Close", "Fechar")));

            var serialized = new SerializedObject(panel);
            serialized.FindProperty("readoutFormat").stringValue = ReadoutFormat;
            var rowsProperty = serialized.FindProperty("rows");
            rowsProperty.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++)
            {
                var element = rowsProperty.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("label").objectReferenceValue = rows[i].label;
                element.FindPropertyRelative("format").stringValue = RowFormats[i];
                element.FindPropertyRelative("minus").objectReferenceValue = rows[i].minus;
                element.FindPropertyRelative("plus").objectReferenceValue = rows[i].plus;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();

            content.SetActive(false);

            // O DBG aponta para o painel: um painel refeito precisa ser ligado de novo.
            var debugPanel = Object.FindAnyObjectByType<DebugPanel>();
            if (debugPanel != null) GrayboxSetup.SetReferences(debugPanel, ("cameraPanel", panel));

            Debug.Log("[Setup] Painel de câmera da descida criado na cena Game.");
            return true;
        }
    }
}
