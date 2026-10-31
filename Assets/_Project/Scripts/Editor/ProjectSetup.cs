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
    /// retrato), URP, Input System como único input e as cenas (Bootstrap e Game). O renderer criado no M1
    /// (<c>URP_2D_Renderer</c>) saiu <b>Universal</b>, apesar do nome (achado no M6): é ele que desenha os sprites do jogo e
    /// a perspectiva da descida.
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
        internal const string UrpAssetPath = RenderingFolder + "/URP_2D.asset";
        const string RendererDataPath = RenderingFolder + "/URP_2D_Renderer.asset";

        public const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";

        /// <summary>A arte de abertura do jogo (pedida pelo Bruno em 2026-10-22).</summary>
        const string SplashPath = "Assets/_Project/Art/Splash/Splash.jpg";
        public const string GameScenePath = "Assets/_Project/Scenes/Game.unity";

        /// <summary>Valores de protótipo do loop de mineração (Marco 2, D-29).</summary>
        public const string PrototypeBalancePath = "Assets/_Project/Data/Balance_Prototipo.asset";

        /// <summary>Os números literais do GDD v1, guardados para comparação (Marco 3, D-35). Não é o asset ativo.</summary>
        public const string GddBalancePath = "Assets/_Project/Data/Balance_GDD_v1.asset";

        /// <summary>Os números do GDD v5 (D-87, D-92; toque de 33%, D-86), para comparação. Não é o asset ativo.</summary>
        public const string GddV5BalancePath = "Assets/_Project/Data/Balance_GDD_v5.asset";

        /// <summary>O balanço que o jogo usa: o GDD v5 desde a D-103 (antes, o protótipo).</summary>
        public const string ActiveBalancePath = GddV5BalancePath;

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
            ConfigureAudio();
            EnsurePrototypeBalance();
            EnsureGddBalance();
            EnsureGddV5Balance();
            EnsureGddV5Caves();
            EnsureBootstrapScene();
            EnsureSplash();
            EnsureGameScene();
            UiSkinSetup.EnsureAssets();
            CorridorArtSetup.EnsureAssets();
            BiomeArtSetup.EnsureAssets();
            CrystalArtSetup.EnsureAssets();
            DwarfArtSetup.EnsureAssets();
            DwarfVariantsSetup.EnsureAssets();
            GrayboxSetup.Ensure();
            ConfigureBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Configuração do projeto aplicada.");
        }

        /// <summary>
        /// Antes de cada build: deixa a cena e os prefabs na versão que o código espera (só cria o que falta). Sem isso, um
        /// build feito antes de rodar o setup de uma etapa nova sai com referências faltando (achado da E5). Volta para a
        /// cena que estava aberta.
        /// </summary>
        internal static void ApplyBeforeBuild()
        {
            if (!TmpEssentialsReady())
                throw new InvalidOperationException(
                    "Falta importar o TextMeshPro: rode Tools → Miners Idle → Projeto → Aplicar configuração do projeto.");

            var previous = SceneManager.GetActiveScene().path;
            ApplyAll();
            if (!Application.isBatchMode && !string.IsNullOrEmpty(previous) && previous != SceneManager.GetActiveScene().path)
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
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
        /// os dados padrão; se o caminho interno do URP sumir, cai na criação direta.
        /// </summary>
        static ScriptableRendererData Create2DRendererData() =>
            CreateRendererData("_2DRenderer", "Renderer") ?? CreateRendererDirect<Renderer2DData>(RendererDataPath);

        /// <summary>
        /// Cria um renderer do URP ao lado do <see cref="UrpAssetPath"/> (o arquivo ganha o sufixo
        /// <paramref name="suffix"/>) pelo método interno que o menu do URP usa, por isso a reflexão. <c>null</c> se o
        /// método não existir mais ou falhar.
        /// </summary>
        internal static ScriptableRendererData CreateRendererData(string rendererType, string suffix)
        {
            var method = typeof(UniversalRenderPipelineAsset).GetMethod(
                "CreateRendererAsset", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null) return null;

            var parameters = method.GetParameters();
            var args = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var type = parameters[i].ParameterType;
                if (i == 0) args[i] = UrpAssetPath;
                else if (type.IsEnum) args[i] = Enum.Parse(type, rendererType);
                else if (type == typeof(bool)) args[i] = true;
                else if (type == typeof(string)) args[i] = suffix;
                else args[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;
            }

            try
            {
                if (method.Invoke(null, args) is ScriptableRendererData created)
                {
                    Debug.Log($"[Setup] Renderer criado em {AssetDatabase.GetAssetPath(created)}.");
                    return created;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Setup] CreateRendererAsset falhou ({e.GetBaseException().Message}); criando direto.");
            }
            return null;
        }

        internal static T CreateRendererDirect<T>(string path) where T : ScriptableRendererData
        {
            Debug.LogWarning($"[Setup] Criando {typeof(T).Name} direto em {path}; conferir o renderer no Inspector.");
            var data = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(data, path);
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

        // Project Settings → Audio → DSP Buffer Size: 256 = "Best latency" (0 é o padrão, ~1024 amostras no Android).
        const int BestLatencyDspBuffer = 256;

        /// <summary>
        /// M5, E4: buffer de áudio pequeno, para o bipe do toque sair junto com o dedo. Com o padrão (1024 amostras a
        /// 48 kHz), cada buffer segura ~16 ms a mais, o que já se sente num jogo de toque.
        /// </summary>
        static void ConfigureAudio()
        {
            var audioManager = Unsupported.GetSerializedAssetInterfaceSingleton("AudioManager");
            var serialized = new SerializedObject(audioManager);
            var buffer = serialized.FindProperty("m_RequestedDSPBufferSize");
            if (buffer == null)
            {
                Debug.LogWarning("[Setup] m_RequestedDSPBufferSize não encontrado; definir à mão em Audio → DSP Buffer Size = Best latency.");
                return;
            }

            if (buffer.intValue != BestLatencyDspBuffer)
            {
                buffer.intValue = BestLatencyDspBuffer;
                serialized.ApplyModifiedProperties();
                Debug.Log("[Setup] Áudio: DSP Buffer Size = Best latency.");
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

        /// <summary>
        /// Cria o <c>Balance_GDD_v1</c>: os padrões do asset trocados pelos números do GDD v1.0 (REQ-ECO-02…07). As Botas
        /// não existem no GDD e ficam desligadas (limite 0). Se já existe, não mexe.
        /// </summary>
        static void EnsureGddBalance()
        {
            if (File.Exists(GddBalancePath)) return;

            EnsureFolder(Path.GetDirectoryName(GddBalancePath)?.Replace('\\', '/'));
            var asset = ScriptableObject.CreateInstance<MiningRulesAsset>();
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("damage").doubleValue = 0.5;
            serialized.FindProperty("hitInterval").doubleValue = 5;
            SetLine(serialized, "pickaxe", 50, 4, 19, 2);
            SetLine(serialized, "speed", 200, 3, 45, 0.1);
            SetLine(serialized, "mining", 50, 1.2, 100, 0.01);
            SetLine(serialized, "doublePickaxe", 5000, 1, 1, 2);
            SetLine(serialized, "boots", 0, 1, 0, 0);
            serialized.FindProperty("minHitInterval").doubleValue = 0.5;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(asset, GddBalancePath);
            Debug.Log($"[Setup] Valores do GDD v1 criados em {GddBalancePath}.");
        }

        /// <summary>
        /// GDD v5 §3–§4 (D-87): dano 5 × 2^(n−1) até o nível 20; intervalo de 2,0 s, −0,1 s por nível até 0,1 s; Mineração
        /// +100% por nível até +1000%; Dupla 5.000; crítico 5% × 2; toque 33% (D-86). O cristal é o da caverna 1 (D-92:
        /// vida 10 × 8^0, lascas 1 × 3^0): as cavernas entram no M8. Botas não existem no v5 (linha desligada).
        /// </summary>
        /// <summary>
        /// M8: as 10 cavernas e o Letrado no <c>Balance_GDD_v5</c> (D-92: vida × 8 e lascas × 3 por caverna; D-93: 100
        /// cristais; GDD v5: Letrado <c>100 × 10^(n−1)</c>, 10 níveis, dano × 2). Só no asset que ainda tem uma caverna só
        /// (criado antes do M8); um ajuste feito à mão depois fica.
        /// </summary>
        static void EnsureGddV5Caves()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV5BalancePath);
            if (asset == null) return;
            var serialized = new SerializedObject(asset);
            if (serialized.FindProperty("caveCount").intValue != 1) return;

            serialized.FindProperty("caveCount").intValue = 10;
            serialized.FindProperty("caveHealthGrowth").doubleValue = 8;
            serialized.FindProperty("caveRewardGrowth").doubleValue = 3;
            serialized.FindProperty("letradoBreaks").intValue = 100;
            SetLine(serialized, "letrado", 100, 10, 10, 2);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] Balance_GDD_v5: 10 cavernas e o Letrado (M8).");
        }

        static void EnsureGddV5Balance()
        {
            if (File.Exists(GddV5BalancePath)) return;

            EnsureFolder(Path.GetDirectoryName(GddV5BalancePath)?.Replace('\\', '/'));
            var asset = ScriptableObject.CreateInstance<MiningRulesAsset>();
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("crystalHealth").doubleValue = 10;
            serialized.FindProperty("crystalReward").doubleValue = 1;
            serialized.FindProperty("damage").doubleValue = 5;
            serialized.FindProperty("hitInterval").doubleValue = 2;
            SetLine(serialized, "pickaxe", 50, 4, 19, 2);
            SetLine(serialized, "speed", 50, 5, 19, 0.1);
            SetLine(serialized, "mining", 50, 1.2, 10, 1);
            SetLine(serialized, "doublePickaxe", 5000, 1, 1, 2);
            SetLine(serialized, "boots", 0, 1, 0, 0);
            serialized.FindProperty("minHitInterval").doubleValue = 0.1;
            serialized.FindProperty("critChance").doubleValue = 0.05;
            serialized.FindProperty("critMultiplier").doubleValue = 2;
            serialized.FindProperty("tapFraction").doubleValue = 0.33;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(asset, GddV5BalancePath);
            Debug.Log($"[Setup] Valores do GDD v5 criados em {GddV5BalancePath} (D-87).");
        }

        static void SetLine(SerializedObject serialized, string line, double baseCost, double costGrowth, int maxLevel, double effect)
        {
            serialized.FindProperty(line + ".baseCost").doubleValue = baseCost;
            serialized.FindProperty(line + ".costGrowth").doubleValue = costGrowth;
            serialized.FindProperty(line + ".maxLevel").intValue = maxLevel;
            serialized.FindProperty(line + ".effect").doubleValue = effect;
        }

        /// <summary>
        /// A arte de abertura na cena Bootstrap: uma imagem que cobre a tela inteira (corta as sobras, sem distorcer)
        /// enquanto o jogo carrega. O splash do Unity sai, para a arte ser a primeira coisa que aparece.
        /// </summary>
        static void EnsureSplash()
        {
            if (!File.Exists(SplashPath)) return;

            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;

            if (AssetImporter.GetAtPath(SplashPath) == null) AssetDatabase.ImportAsset(SplashPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SplashPath);
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            var bootstrap = UnityEngine.Object.FindAnyObjectByType<GameBootstrap>();
            if (bootstrap == null || GameObject.Find("Splash") != null) return;

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SplashPath);
            var canvasObject = new GameObject("Splash", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var imageObject = new GameObject("Art", typeof(RectTransform), typeof(Image), typeof(AspectRatioFitter));
            imageObject.transform.SetParent(canvasObject.transform, false);
            var image = imageObject.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            var fitter = imageObject.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; // cobre a tela; corta o que sobrar
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;

            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("splash").objectReferenceValue = canvasObject.GetComponent<CanvasGroup>();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Setup] Arte de abertura (splash) criada na cena Bootstrap; splash do Unity desligado.");
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
