using System;
using System.IO;
using System.Linq;
using System.Reflection;
using MinersIdle.Game;
using MinersIdle.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// A configuração do projeto, escrita em código para ser reproduzível (Marco 1): identidade, Android (IL2CPP, ARM64,
    /// retrato), URP com o Renderer 2D, Input System como único input e as cenas de base (Bootstrap e Game).
    ///
    /// Só cria o que falta: rodar de novo não apaga ajustes feitos à mão.
    ///
    /// Pelo menu: Tools → Miners Idle → Projeto → Aplicar configuração do projeto.
    /// Em batch (com o Editor fechado):
    ///   Unity.exe -batchmode -nographics -projectPath &lt;pasta&gt; -executeMethod MinersIdle.EditorTools.ProjectSetup.RunBatch
    /// </summary>
    public static class ProjectSetup
    {
        const string RenderingFolder = "Assets/_Project/Settings/Rendering";
        const string UrpAssetPath = RenderingFolder + "/URP_2D.asset";
        const string RendererDataPath = RenderingFolder + "/URP_2D_Renderer.asset";

        public const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        public const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        /// <summary>Valores de protótipo do loop de mineração (Marco 2, D-29).</summary>
        public const string PrototypeBalancePath = "Assets/_Project/Data/Balance_Prototipo.asset";

        public const string PackageId = "com.brunocarvalho.minersidle";
        public const string VersionName = "0.1.0";

        // Valor de PlayerSettings.activeInputHandler: 0 = legado, 1 = Input System, 2 = os dois.
        const int InputSystemOnly = 1;

        // Cor de fundo provisória (carvão da paleta do GDD §8.2).
        static readonly Color Charcoal = new Color32(0x15, 0x19, 0x1C, 0xFF);

        [MenuItem("Tools/Miners Idle/Projeto/Aplicar configuração do projeto")]
        public static void Apply()
        {
            // O TextMeshPro precisa dos "Essentials" antes de criar qualquer texto; a importação é assíncrona.
            if (!TmpEssentialsReady())
            {
                ImportTmpEssentials(ApplyAll, error => Debug.LogError($"[Setup] {error}"));
                return;
            }

            ApplyAll();
        }

        static void ApplyAll()
        {
            ConfigureIdentity();
            ConfigureAndroid();
            ConfigureUrp();
            ConfigureInputSystem();
            EnsurePrototypeBalance();
            EnsureBootstrapScene();
            EnsureGameScene();
            GrayboxSetup.Ensure();
            ConfigureBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Configuração do projeto aplicada.");
        }

        /// <summary>Entrada para o modo batch: aplica e sai com código 0 (ok) ou 1 (erro).</summary>
        public static void RunBatch()
        {
            void Finish(int code) => EditorApplication.Exit(code);

            try
            {
                if (TmpEssentialsReady())
                {
                    ApplyAll();
                    Finish(0);
                    return;
                }

                // Em batch o Editor continua rodando depois deste método; saímos quando a importação termina.
                ImportTmpEssentials(
                    () =>
                    {
                        try { ApplyAll(); Finish(0); }
                        catch (Exception e) { Debug.LogException(e); Finish(1); }
                    },
                    error => { Debug.LogError($"[Setup] {error}"); Finish(1); });
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Finish(1);
            }
        }

        static void ConfigureIdentity()
        {
            PlayerSettings.companyName = "Bruno Carvalho";
            PlayerSettings.productName = "Miners Idle";
            PlayerSettings.bundleVersion = VersionName;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, PackageId);
        }

        /// <summary>Retrato travado; IL2CPP + ARM64 (o Moto G75 é só 64 bits e a Play Store exige 64 bits).</summary>
        static void ConfigureAndroid()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.bundleVersionCode = 1;
        }

        static void ConfigureUrp()
        {
            EnsureFolder(RenderingFolder);

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath);
            if (pipeline == null)
            {
                var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(RendererDataPath)
                                   ?? Create2DRendererData();
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, UrpAssetPath);
                Debug.Log($"[Setup] URP criado em {UrpAssetPath}.");
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;

            // Cada nível de qualidade aponta para o mesmo pipeline (sem isso, um nível vazio cai no padrão).
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(current, false);
        }

        /// <summary>
        /// Cria o Renderer 2D pelo mesmo caminho do menu "Create → Rendering → URP Asset (with 2D Renderer)", que já liga
        /// os dados padrão. O método é interno no URP, por isso a reflexão; se sumir, cai na criação direta.
        /// </summary>
        static ScriptableRendererData Create2DRendererData()
        {
            var method = typeof(UniversalRenderPipelineAsset).GetMethod(
                "CreateRendererAsset", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method != null)
            {
                var parameters = method.GetParameters();
                var args = new object[parameters.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    var type = parameters[i].ParameterType;
                    if (i == 0) args[i] = UrpAssetPath;
                    else if (type.IsEnum) args[i] = Enum.Parse(type, "_2DRenderer");
                    else if (type == typeof(bool)) args[i] = true;
                    else if (type == typeof(string)) args[i] = "Renderer";
                    else args[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;
                }

                try
                {
                    if (method.Invoke(null, args) is ScriptableRendererData created)
                    {
                        Debug.Log($"[Setup] Renderer 2D criado em {AssetDatabase.GetAssetPath(created)}.");
                        return created;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Setup] CreateRendererAsset falhou ({e.GetBaseException().Message}); criando direto.");
                }
            }

            Debug.LogWarning("[Setup] Criando o Renderer2DData direto; conferir o renderer no Inspector.");
            var data = ScriptableObject.CreateInstance<Renderer2DData>();
            AssetDatabase.CreateAsset(data, RendererDataPath);
            return data;
        }

        static void ConfigureInputSystem()
        {
            var playerSettings = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
            var serialized = new SerializedObject(playerSettings);
            var handler = serialized.FindProperty("activeInputHandler");
            if (handler == null)
            {
                Debug.LogWarning("[Setup] activeInputHandler não encontrado; definir à mão em Player → Active Input Handling.");
                return;
            }

            if (handler.intValue != InputSystemOnly)
            {
                handler.intValue = InputSystemOnly;
                serialized.ApplyModifiedProperties();
                Debug.Log("[Setup] Active Input Handling = Input System (vale ao reabrir o Editor).");
            }
        }

        const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        /// <summary>O TextMeshPro precisa dos "Essentials" (fonte e estilos padrão) para exibir qualquer texto.</summary>
        static bool TmpEssentialsReady() => File.Exists(TmpSettingsPath);

        /// <summary>
        /// Importa o pacote "TMP Essential Resources" sem janela e chama <paramref name="done"/> quando terminar.
        /// (TMP_PackageResourceImporter.ImportResources não avisa quando acaba, por isso o pacote é importado direto.)
        /// </summary>
        static void ImportTmpEssentials(Action done, Action<string> failed)
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
            var package = info == null ? null : Path.Combine(info.resolvedPath, "Package Resources", "TMP Essential Resources.unitypackage");
            if (package == null || !File.Exists(package))
            {
                failed($"Pacote TMP Essential Resources não encontrado ({package}).");
                return;
            }

            void Cleanup()
            {
                AssetDatabase.importPackageCompleted -= OnCompleted;
                AssetDatabase.importPackageFailed -= OnFailed;
                AssetDatabase.importPackageCancelled -= OnCancelled;
            }
            void OnCompleted(string name) { Cleanup(); AssetDatabase.Refresh(); Debug.Log("[Setup] TextMeshPro Essentials importados."); done(); }
            void OnFailed(string name, string error) { Cleanup(); failed($"Importação do TMP falhou: {error}"); }
            void OnCancelled(string name) { Cleanup(); failed("Importação do TMP cancelada."); }

            AssetDatabase.importPackageCompleted += OnCompleted;
            AssetDatabase.importPackageFailed += OnFailed;
            AssetDatabase.importPackageCancelled += OnCancelled;
            AssetDatabase.ImportPackage(package, false);
        }

        /// <summary>Cria o asset com os valores padrão do plano do Marco 2; se já existe, não mexe (pode ter ajustes).</summary>
        internal static void EnsurePrototypeBalance()
        {
            if (File.Exists(PrototypeBalancePath)) return;

            EnsureFolder(Path.GetDirectoryName(PrototypeBalancePath)?.Replace('\\', '/'));
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<MiningRulesAsset>(), PrototypeBalancePath);
            Debug.Log($"[Setup] Valores de protótipo criados em {PrototypeBalancePath}.");
        }

        static void EnsureBootstrapScene()
        {
            if (File.Exists(BootstrapScenePath)) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            new GameObject("Bootstrap").AddComponent<GameBootstrap>();
            SaveScene(scene, BootstrapScenePath);
        }

        /// <summary>A cena Game do Marco 1: câmera 2D, Canvas e um painel de Safe Area com o rótulo de diagnóstico.</summary>
        static void EnsureGameScene()
        {
            if (File.Exists(GameScenePath)) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();

            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(270f, 480f); // provisório (D-14, a validar no Art Spike)
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            var safe = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaController), typeof(Image));
            safe.transform.SetParent(canvasObject.transform, false);
            var safeRect = (RectTransform)safe.transform;
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.offsetMin = safeRect.offsetMax = Vector2.zero;
            var safeImage = safe.GetComponent<Image>();
            safeImage.color = new Color(0.949f, 0.663f, 0f, 0.15f); // âmbar #F2A900 translúcido: mostra o Safe Area
            safeImage.raycastTarget = false;

            var labelObject = new GameObject("BuildInfo", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(BuildInfoLabel));
            labelObject.transform.SetParent(safe.transform, false);
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(0f, -8f);
            labelRect.sizeDelta = new Vector2(0f, 90f);
            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.text = "Miners Idle";
            label.fontSize = 14f;
            label.alignment = TextAlignmentOptions.Top;
            label.color = new Color32(0xF5, 0xF7, 0xF8, 0xFF);
            label.raycastTarget = false;

            SaveScene(scene, GameScenePath);
        }

        static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 15f; // 480 px de altura de referência ÷ PPU 16 ÷ 2 (provisório, D-14)
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Charcoal;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        internal static void ConfigureBuildScenes()
        {
            EditorBuildSettings.scenes = new[] { BootstrapScenePath, GameScenePath }
                .Where(File.Exists)
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToArray();
        }

        static void SaveScene(Scene scene, string path)
        {
            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[Setup] Cena criada: {path}.");
        }

        /// <summary>AssetDatabase.CreateAsset não cria pastas intermediárias.</summary>
        internal static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder)) return;

            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
