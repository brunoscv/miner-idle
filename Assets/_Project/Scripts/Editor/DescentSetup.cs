using System.IO;
using MinersIdle.Presentation;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Parte do <see cref="ProjectSetup"/> para a cena da descida (M6, E1; D-51): garante um renderer Universal (3D) no
    /// URP (o do projeto já é, apesar do nome <c>URP_2D_Renderer</c>) e cria a cena <c>Descida</c> inteira: câmera de perspectiva desenhando numa textura de baixa
    /// resolução, o <see cref="DescentStage"/>, a imagem na tela, uma maquete do HUD e o painel de debug com os
    /// controles da câmera. Como o resto do setup, só cria o que falta; a cena é toda gerada aqui, então uma versão
    /// anterior (sem o <see cref="Marker"/>) é refeita do zero.
    /// </summary>
    static class DescentSetup
    {
        public const string ScenePath = "Assets/_Project/Scenes/Descida.unity";
        const string ShaderPath = "Assets/_Project/Shaders/DescentUnlit.shader";
        const string UniversalRendererPath = "Assets/_Project/Settings/Rendering/URP_2D_Universal_Renderer.asset";

        /// <summary>O último objeto que esta versão do setup cria, a partir do Canvas.</summary>
        const string Marker = "SafeArea/DescentDebug/Content/Buttons/Back";

        static readonly Color FogColor = new Color32(0x14, 0x0D, 0x09, 0xFF);
        static readonly Color HudPanelColor = new Color32(0x2B, 0x1D, 0x14, 0xE6);
        static readonly Color HudLabelColor = new Color32(0xF2, 0xA9, 0x00, 0xFF);
        static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.8f);
        static readonly Color TextColor = new Color32(0xF5, 0xF7, 0xF8, 0xFF);

        /// <summary>Rótulos das linhas de ajuste, na ordem de <see cref="DescentDebugPanel.Setting"/>.</summary>
        static readonly string[] RowFormats =
        {
            "Altura da câmera: {0} m",
            "Inclinação: {0}°",
            "Distância até o anão: {0} m",
            "Campo de visão: {0}°",
            "Largura do corredor: {0} m",
            "Velocidade: {0} m/s",
        };

        /// <summary>Modelo da linha de medidas (ver o Tooltip de <c>DescentDebugPanel.readoutFormat</c>).</summary>
        const string ReadoutFormat =
            "Anão: cabeça {0}% · pés {1}% do topo · {2}% da tela\nChão à frente: {3} m (~{4} s até o cristal)\nRender {5}×{6} · {7} fps";

        public static void Ensure()
        {
            int rendererIndex = EnsureUniversalRenderer();
            if (rendererIndex < 0) return;
            EnsureScene(rendererIndex);
        }

        /// <summary>
        /// O índice de um renderer Universal na lista do URP; se não houver (projeto com o Renderer 2D de verdade),
        /// acrescenta um, sem mexer no padrão. Em 2026-10-22 o renderer do projeto já é o Universal (índice 0).
        /// </summary>
        static int EnsureUniversalRenderer()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(ProjectSetup.UrpAssetPath);
            if (pipeline == null)
            {
                Debug.LogError($"[Setup] URP não encontrado em {ProjectSetup.UrpAssetPath}; cena da descida não criada.");
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
            Debug.Log($"[Setup] Renderer Universal adicionado ao URP (índice {index}), só para a cena da descida.");
            return index;
        }

        static void EnsureScene(int rendererIndex)
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[Setup] Cena da descida não alterada: havia mudanças não salvas.");
                return;
            }

            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var existingCanvas = GameObject.Find("Canvas");
                if (existingCanvas != null && existingCanvas.transform.Find(Marker) != null) return;
                Debug.Log("[Setup] Cena da descida de versão anterior: refeita.");
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Uma câmera só limpa a tela (a imagem da descida a cobre inteira); a outra desenha a descida na textura.
            var screenCamera = new GameObject("ScreenCamera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            screenCamera.orthographic = true;
            screenCamera.clearFlags = CameraClearFlags.SolidColor;
            screenCamera.backgroundColor = Color.black;
            screenCamera.cullingMask = 0;
            screenCamera.depth = -1;

            var sceneCamera = new GameObject("DescentCamera", typeof(Camera)).GetComponent<Camera>();
            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.backgroundColor = FogColor;
            sceneCamera.nearClipPlane = 0.1f;
            sceneCamera.farClipPlane = 45f;
            sceneCamera.depth = -2;
            var cameraData = sceneCamera.GetUniversalAdditionalCameraData();
            cameraData.SetRenderer(rendererIndex);
            cameraData.renderPostProcessing = false;
            cameraData.renderShadows = false;

            var stage = new GameObject("Descent").AddComponent<DescentStage>();
            GrayboxSetup.SetReferences(stage,
                ("shader", AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath)),
                ("sceneCamera", sceneCamera));

            var canvas = CreateCanvas();

            var screen = new GameObject("Screen", typeof(RectTransform), typeof(RawImage), typeof(DescentScreen));
            screen.transform.SetParent(canvas.transform, false);
            DebugPanelSetup.Stretch((RectTransform)screen.transform, Vector2.zero, Vector2.one);
            screen.GetComponent<RawImage>().raycastTarget = false;
            GrayboxSetup.SetReferences(screen.GetComponent<DescentScreen>(), ("stage", stage));

            var safeArea = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaController));
            safeArea.transform.SetParent(canvas.transform, false);
            DebugPanelSetup.Stretch((RectTransform)safeArea.transform, Vector2.zero, Vector2.one);

            var hud = CreateHudMock(safeArea.transform);
            CreateDebugPanel(safeArea.transform, stage, hud);
            DebugPanelSetup.EnsureEventSystem();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[Setup] Cena criada: {ScenePath}.");
        }

        static GameObject CreateCanvas()
        {
            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(270f, 480f); // a mesma do jogo (D-14)
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            return canvasObject;
        }

        /// <summary>Maquete do HUD do jogo (só para julgar o enquadramento): não funciona e não bloqueia toque.</summary>
        static GameObject CreateHudMock(Transform parent)
        {
            var hud = new GameObject("Hud", typeof(RectTransform));
            hud.transform.SetParent(parent, false);
            DebugPanelSetup.Stretch((RectTransform)hud.transform, Vector2.zero, Vector2.one);

            var top = new GameObject("Top", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            top.transform.SetParent(hud.transform, false);
            var topRect = (RectTransform)top.transform;
            topRect.anchorMin = new Vector2(0f, 1f);
            topRect.anchorMax = new Vector2(1f, 1f);
            topRect.pivot = new Vector2(0.5f, 1f);
            topRect.anchoredPosition = new Vector2(0f, -4f);
            topRect.sizeDelta = new Vector2(-8f, 34f); // o mesmo HUD do jogo
            var layout = top.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 3f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;

            GrayboxSetup.CreateHudPanel(top.transform, "Lascas", "LASCAS").text = "1.234";
            GrayboxSetup.CreateHudPanel(top.transform, "Dano", "DANO").text = "12,5";
            GrayboxSetup.CreateHudPanel(top.transform, "Caverna", "CAVERNA").text = "1";
            GrayboxSetup.CreateHudPanel(top.transform, "Velocidade", "VELOCIDADE").text = "2,30s";

            CreateMockButton(hud.transform, "Shop", "LOJA", new Vector2(1f, 1f), new Vector2(-4f, -42f), new Vector2(52f, 30f));
            CreateMockButton(hud.transform, "Ad", "AD", new Vector2(1f, 1f), new Vector2(-4f, -76f), new Vector2(52f, 30f));
            CreateMockButton(hud.transform, "NextCave", "PASSAR DE CAVERNA", new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(130f, 26f));

            foreach (var image in hud.GetComponentsInChildren<Image>(true)) image.raycastTarget = false;
            return hud;
        }

        static void CreateMockButton(Transform parent, string name, string label, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = HudPanelColor;
            GrayboxSetup.CreateHudText(go.transform, "Label", label, 8f, HudLabelColor, Vector2.zero, Vector2.one);
        }

        static void CreateDebugPanel(Transform parent, DescentStage stage, GameObject hud)
        {
            var root = new GameObject("DescentDebug", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            DebugPanelSetup.Stretch((RectTransform)root.transform, Vector2.zero, Vector2.one);

            var toggle = DebugPanelSetup.CreateButton(root.transform, "Toggle", "DBG");
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
            contentRect.sizeDelta = new Vector2(-8f, 232f);
            content.GetComponent<Image>().color = PanelColor;

            var readout = GrayboxSetup.CreateHudText(content.transform, "Readout", "", 7f, TextColor,
                                                     new Vector2(0f, 0.78f), new Vector2(1f, 1f));
            readout.alignment = TextAlignmentOptions.TopLeft;
            readout.margin = new Vector4(4f, 4f, 4f, 2f);

            var rowsRoot = new GameObject("Rows", typeof(RectTransform), typeof(VerticalLayoutGroup));
            rowsRoot.transform.SetParent(content.transform, false);
            DebugPanelSetup.Stretch((RectTransform)rowsRoot.transform, new Vector2(0f, 0.30f), new Vector2(1f, 0.78f));
            var rowsLayout = rowsRoot.GetComponent<VerticalLayoutGroup>();
            rowsLayout.padding = new RectOffset(4, 4, 2, 2);
            rowsLayout.spacing = 2f;
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
            DebugPanelSetup.Stretch((RectTransform)grid.transform, Vector2.zero, new Vector2(1f, 0.30f));
            var gridLayout = grid.GetComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(4, 4, 4, 4);
            gridLayout.cellSize = new Vector2(82f, 16f);
            gridLayout.spacing = new Vector2(3f, 3f);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;

            var panel = root.AddComponent<DescentDebugPanel>();
            GrayboxSetup.SetReferences(panel,
                ("stage", stage),
                ("toggleButton", toggle),
                ("content", content),
                ("hud", hud),
                ("readout", readout),
                ("intern", DebugPanelSetup.CreateButton(grid.transform, "Intern", "Estagiário")),
                ("hudToggle", DebugPanelSetup.CreateButton(grid.transform, "HudToggle", "HUD")),
                ("pause", DebugPanelSetup.CreateButton(grid.transform, "Pause", "Pausar")),
                ("resolution", DebugPanelSetup.CreateButton(grid.transform, "Resolution", "Resolução")),
                ("resetFraming", DebugPanelSetup.CreateButton(grid.transform, "ResetFraming", "Padrão D-50")),
                ("back", DebugPanelSetup.CreateButton(grid.transform, "Back", "Voltar ao jogo")));

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
        }
    }
}
