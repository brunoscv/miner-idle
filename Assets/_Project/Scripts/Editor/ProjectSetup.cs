using System;
using System.IO;
using System.Linq;
using System.Reflection;
using MinersIdle.Core;
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

        /// <summary>Os números do GDD v7 (D-130, D-133…D-136): o balanço do jogo da E3b do M8 ao M9b. Fica para comparação.</summary>
        public const string GddV7BalancePath = "Assets/_Project/Data/Balance_GDD_v7.asset";

        /// <summary>Os números do GDD v8 (M9c E1, D-236…D-256): o balanço do jogo.</summary>
        public const string GddV8BalancePath = "Assets/_Project/Data/Balance_GDD_v8.asset";

        /// <summary>
        /// O balanço que o jogo usa: o GDD v8 desde o M9c (antes, o v7 pela D-130, o v5 pela D-103 e o protótipo). O seletor
        /// de balanço do DBG saiu no v8 (D-256): os assets de antes ficam só para comparação.
        /// </summary>
        public const string ActiveBalancePath = GddV8BalancePath;

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
            EnsureGddV7Balance();
            EnsureGddV7HundredLevels();
            EnsureGddV7Permanents();
            EnsureGddV7Intern();
            EnsureGddV7RareCrystal();
            EnsureGddV7AdBoost();
            EnsureGddV8Balance();
            EnsureGddV8FloorMilestones();
            EnsureGddV8RestShop();
            EnsureGddV8RareCrystal();
            EnsureGddV8Ads();
            EnsureGddV8Bomb();
            EnsureBootstrapScene();
            EnsureSplash();
            EnsureGameScene();
            UiSkinSetup.EnsureAssets();
            CorridorArtSetup.EnsureAssets();
            BiomeArtSetup.EnsureAssets();
            SupportArtSetup.EnsureAssets();
            CrystalArtSetup.EnsureAssets();
            CrystalSheetSetup.EnsureAssets(); // a folha rachando do v8 nos biomas (M9f), depois dos biomas e dos cristais
            BombArtSetup.EnsureAssets(); // as 8 bombas a partir do icon_bomb do Bruno (M9g, D-315)
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
            serialized.FindProperty("floorBreaks").intValue = 100; // o Letrado (a linha) saiu no v8 (D-256)
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
            serialized.FindProperty("minCrystals").intValue = 10; // D-122: a fila repõe cada quebra
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(asset, GddV5BalancePath);
            Debug.Log($"[Setup] Valores do GDD v5 criados em {GddV5BalancePath} (D-87).");
        }

        /// <summary>
        /// Cria o <c>Balance_GDD_v7</c> (E3b do M8, D-130) a partir do v5 (a fila, o toque, o golpe e as cavernas iguais),
        /// com os números do GDD v7 e das decisões de 2026-11-04: vida <c>1 × 1,25^(c−1)</c> e lascas = vida × 1,2 (D-134);
        /// Picareta pela compra com Dano Base fixo em 1 e taxa 1,2, custo 2,1 × o dano da Picareta, até o nível 50 (D-202);
        /// Velocidade <c>50 × 2^(n−1)</c> de 2,0 s até 0,1 s (D-135); Mineração <c>20 × 5^(n−1)</c>; Chance de Crítico
        /// <c>50 × 2^(n−1)</c>, +5%, 10 níveis, com base de 0% no golpe e 5% no toque (D-136); a Dupla desligada (D-136).
        /// Até a E3b-2, as 10 cavernas e o Letrado continuam, já com custo zero e sem ×2 de dano (o Mapa da D-128).
        /// </summary>
        static void EnsureGddV7Balance()
        {
            if (File.Exists(GddV7BalancePath)) return;
            if (!AssetDatabase.CopyAsset(GddV5BalancePath, GddV7BalancePath))
            {
                Debug.LogError($"[Setup] Não consegui copiar {GddV5BalancePath} para {GddV7BalancePath}.");
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV7BalancePath);
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("crystalHealth").doubleValue = 1;
            serialized.FindProperty("crystalReward").doubleValue = 1.2;
            serialized.FindProperty("caveHealthGrowth").doubleValue = 1.25;
            serialized.FindProperty("caveRewardGrowth").doubleValue = 1.25;
            serialized.FindProperty("damage").doubleValue = 1;
            serialized.FindProperty("hitInterval").doubleValue = 2;
            serialized.FindProperty("pickaxeFormula").enumValueIndex = (int)PickaxeFormula.LevelScaled; // D-201
            serialized.FindProperty("pickaxeCaveGrowth").doubleValue = 1.2;
            // D-202: custo = 2,1 × o dano da Picareta (o crescimento da linha não vale: o custo usa a taxa e o Dano Base acima).
            SetLine(serialized, "pickaxe", 2.1, 1.2, 49, 0, CostFormula.DamageScaled);
            SetLine(serialized, "speed", 50, 2, 19, 0.1);
            SetLine(serialized, "mining", 20, 5, 10, 1);
            SetLine(serialized, "doublePickaxe", 5000, 1, 0, 2);
            SetLine(serialized, "boots", 0, 1, 0, 0);
            SetLine(serialized, "critical", 50, 2, 10, 0.05);
            serialized.FindProperty("minHitInterval").doubleValue = 0.1;
            serialized.FindProperty("critChance").doubleValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Setup] Valores do GDD v7 criados em {GddV7BalancePath} (D-130).");
        }

        /// <summary>
        /// E3b-2 do M8 (D-128, D-129): o v7 desce 100 níveis, um bioma a cada 10; o Letrado vira o Mapa, de graça (só a
        /// trava dos 100 cristais), sem o ×2 do dano, até o nível 100 (o último leva à parede do coração, D-112).
        /// </summary>
        static void EnsureGddV7HundredLevels()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV7BalancePath);
            if (asset == null) return;
            var serialized = new SerializedObject(asset);
            if (serialized.FindProperty("caveCount").intValue == 100 && serialized.FindProperty("levelsPerBiome").intValue == 10) return;

            serialized.FindProperty("caveCount").intValue = 100;
            serialized.FindProperty("levelsPerBiome").intValue = 10;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] GDD v7: 100 níveis, bioma a cada 10 e o Mapa de graça até o nível 100 (D-128, D-129).");
        }

        /// <summary>
        /// Marco 8b (D-138, D-152, D-158): no v7, o Descansar a partir do nível 10, as lascas vermelhas da tabela do §3.1
        /// (5 nos níveis 10–40 múltiplos de 10, 10 nos 50–100, 1 nos outros) e as 5 permanentes do anão, todas
        /// <c>10 + 2^(n−1)</c>: Memória (−10 cristais, 10 níveis), Sangue da Montanha (+10% de dano, 20), Coração Ardente
        /// (+50% de lascas, 20), Dedos Nervosos (+5% de crítico, 10) e Alma dos Anões (+1 nível de Picareta, 10).
        /// </summary>
        static void EnsureGddV7Permanents()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV7BalancePath);
            if (asset == null) return;
            var serialized = new SerializedObject(asset);
            if (serialized.FindProperty("restLevel").intValue == 10) return;

            serialized.FindProperty("restLevel").intValue = 10;
            serialized.FindProperty("redShardMilestoneStep").intValue = 10;
            serialized.FindProperty("redShardMilestone").doubleValue = 5;
            serialized.FindProperty("redShardBigMilestoneFrom").intValue = 50;
            serialized.FindProperty("redShardBigMilestone").doubleValue = 10;
            serialized.FindProperty("redShardPerLevel").doubleValue = 1;
            SetLine(serialized, "memory", 10, 2, 10, 10, CostFormula.OffsetExponential);
            SetLine(serialized, "mountainBlood", 10, 2, 20, 0.1, CostFormula.OffsetExponential);
            SetLine(serialized, "burningHeart", 10, 2, 20, 0.5, CostFormula.OffsetExponential);
            SetLine(serialized, "nervousFingers", 10, 2, 10, 0.05, CostFormula.OffsetExponential);
            SetLine(serialized, "dwarfSoul", 10, 2, 10, 1, CostFormula.OffsetExponential);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] GDD v7: Descansar a partir do nível 10, lascas vermelhas e as 5 permanentes do anão (Marco 8b, D-158).");
        }

        /// <summary>
        /// M7 (GDD v7 §4.1, §4.2, §5.4; D-165, D-205, D-207): no v7, o Estagiário (500, compra única), a árvore dele — Picareta
        /// <c>50 × 4^(n−1)</c> com dano <c>5 × 2^(n−1)</c>, nível 1 sem compra, até o nível 20 (19 compras), Velocidade <c>50 × 5^(n−1)</c>, −0,1 s a partir de
        /// 2,0 s até o piso de 0,1 s (19 compras), Mineração <c>50 × 1,2^(n−1)</c>, +100% das lascas offline (10) — e as
        /// permanentes dele: Treinar <c>10 + 2^(n−1)</c>, +10% de dano (10), e Promoção (50, compra única).
        /// </summary>
        static void EnsureGddV7Intern()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV7BalancePath);
            if (asset == null) return;
            var serialized = new SerializedObject(asset);
            if (serialized.FindProperty("intern.maxLevel").intValue == 1) return;

            SetLine(serialized, "intern", 500, 1, 1, 1);
            SetLine(serialized, "internPickaxe", 50, 4, 19, 5); // 19 compras = nível 20 (D-207)
            serialized.FindProperty("internPickaxeDamageGrowth").doubleValue = 2;
            SetLine(serialized, "internSpeed", 50, 5, 19, 0.1);
            SetLine(serialized, "internMining", 50, 1.2, 10, 1);
            SetLine(serialized, "internTraining", 10, 2, 10, 0.1, CostFormula.OffsetExponential);
            SetLine(serialized, "internPromotion", 50, 1, 1, 1);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] GDD v7: o Estagiário (500), a árvore dele e o Treinar e a Promoção (M7, D-164, D-165).");
        }

        /// <summary>
        /// M9b E5: o boost do AD no balanço do GDD v7 (§7.2, D-187): intervalo ÷ 2,5 por 30 s. Só preenche se está vazio
        /// (não desfaz um ajuste).
        /// </summary>
        static void EnsureGddV7AdBoost()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV7BalancePath);
            if (asset == null) return;
            var serialized = new SerializedObject(asset);
            var divisor = serialized.FindProperty("adSpeedDivisor");
            var seconds = serialized.FindProperty("adBoostSeconds");
            if (divisor.doubleValue != 0 || seconds.doubleValue != 0) return;
            divisor.doubleValue = 2.5;
            seconds.doubleValue = 30;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] GDD v7: boost do AD com velocidade ×2,5 por 30 s (M9b E5, D-187).");
        }

        /// <summary>
        /// M9b E4: o cristal raro no balanço do GDD v7 (D-97, D-184…D-186, D-190): 15% por cristal (2% no ritmo lento do
        /// DBG), 30 s para escolher e as 4 cartas de 60 s. Só preenche se as cartas estão vazias (não desfaz um ajuste).
        /// </summary>
        static void EnsureGddV7RareCrystal()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV7BalancePath);
            if (asset == null) return;
            var serialized = new SerializedObject(asset);
            var list = serialized.FindProperty("rareCards");
            if (list.arraySize > 0) return;

            (RareCard card, double value)[] cards =
            {
                (RareCard.Damage, 2), (RareCard.Speed, 2), (RareCard.Shards, 2), (RareCard.Critical, 0.25),
            };
            serialized.FindProperty("rareChance").doubleValue = 0.15;
            serialized.FindProperty("rareSlowChance").doubleValue = 0.02;
            serialized.FindProperty("rareChooseSeconds").doubleValue = 30;
            list.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++)
            {
                var item = list.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("card").enumValueIndex = (int)cards[i].card;
                item.FindPropertyRelative("value").doubleValue = cards[i].value;
                item.FindPropertyRelative("seconds").doubleValue = 60;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] GDD v7: cristal raro com 15% por cristal (2% no ritmo lento), 30 s para escolher e as 4 cartas de 60 s (M9b E4).");
        }

        /// <summary>
        /// Cria o <c>Balance_GDD_v8</c> (M9c E1) a partir do v7 (velocidade, mineração, crítico, estagiário, permanentes,
        /// combo, raro e AD iguais), com os números do GDD v8: 100 andares, um bioma a cada 10 (D-237); vida
        /// <c>1 × 1,5^(n−1)</c> (D-237); lascas = vida × 0,2 × max(1, andar − 1) (D-238); crítico ×1,5, e acima de 100% de
        /// chance +1 ponto por 1% (D-249); Picareta a 7 × o dano dela (P5, D-245); o Próximo Andar pede andar × 10 cristais
        /// (D-239), e a Memória tira 5 por nível, até o nível 100, custo <c>10 + 2^(n−1)</c> (D-239); offline 4 h, e 6 h a
        /// partir do andar 20 (D-253). Só cria se não existe: um ajuste feito à mão no Inspector fica.
        /// </summary>
        static void EnsureGddV8Balance()
        {
            if (File.Exists(GddV8BalancePath)) return;
            if (!AssetDatabase.CopyAsset(GddV7BalancePath, GddV8BalancePath))
            {
                Debug.LogError($"[Setup] Não consegui copiar {GddV7BalancePath} para {GddV8BalancePath}.");
                return;
            }

            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV8BalancePath);
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("caveCount").intValue = 100;
            serialized.FindProperty("levelsPerBiome").intValue = 10;
            serialized.FindProperty("crystalHealth").doubleValue = 1;
            serialized.FindProperty("caveHealthGrowth").doubleValue = 1.5;
            serialized.FindProperty("rewardFormula").enumValueIndex = (int)RewardFormula.FloorScaled;
            serialized.FindProperty("crystalReward").doubleValue = 0.2;
            serialized.FindProperty("caveRewardGrowth").doubleValue = 1; // não entra na fórmula do v8
            serialized.FindProperty("critMultiplier").doubleValue = 1.5;
            serialized.FindProperty("pickaxeFormula").enumValueIndex = (int)PickaxeFormula.LevelScaled;
            serialized.FindProperty("pickaxeCaveGrowth").doubleValue = 1.2;
            SetLine(serialized, "pickaxe", 7, 1.2, 49, 0, CostFormula.DamageScaled);
            serialized.FindProperty("floorBreaks").intValue = 10;
            SetLine(serialized, "memory", 10, 2, 100, 5, CostFormula.OffsetExponential);
            serialized.FindProperty("offlineMaxHours").doubleValue = 4;
            serialized.FindProperty("offlineDeepHours").doubleValue = 6;
            serialized.FindProperty("offlineDeepFloor").intValue = 20;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Setup] Valores do GDD v8 criados em {GddV8BalancePath} (M9c E1).");
        }

        /// <summary>
        /// M9d (GDD v8.2 §4.2, D-248, D-252, D-277): as duas permanentes novas — Dedo Fogoso, <c>10 + 2^(n−1)</c>, +0,5 no
        /// dano do crítico, até o 10; Atalho, <c>50 + 2^(n−1)</c>, −0,5 m entre cristais, até o 5 —, a Promoção pedindo o
        /// Treinar no 5 e os cristais a <b>3 m</b> (6 unidades) um do outro, no lugar de 1 m. Só preenche se o Dedo Fogoso
        /// ainda estiver desligado: um ajuste feito à mão no Inspector fica.
        /// </summary>
        static void EnsureGddV8RestShop()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV8BalancePath);
            if (asset == null) return;
            var serialized = new SerializedObject(asset);
            if (serialized.FindProperty("fieryFinger.maxLevel").intValue != 0) return;
            SetLine(serialized, "fieryFinger", 10, 2, 10, 0.5, CostFormula.OffsetExponential);
            SetLine(serialized, "shortcut", 50, 2, 5, 0.5, CostFormula.OffsetExponential);
            serialized.FindProperty("internPromotionTrainingLevel").intValue = 5;
            serialized.FindProperty("minCrystalSpacing").doubleValue = 3 * Permanents.UnitsPerMeter;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] GDD v8: Dedo Fogoso e Atalho, Promoção com o Treinar 5, cristais a 3 m (M9d, D-277).");
        }

        /// <summary>
        /// M9e E1 (GDD v8.2 §3.4, D-243, D-283, D-286): o raro do v8 — o contador de 60 s no lugar da chance; as cartas Dano
        /// ×2, Lascas ×2, +10% de crítico e Dano de Crítico ×2, de 30 s; o tier 2 com ×4, ×4, +20% e ×4; e o Raro Dourado,
        /// compra única de 500 lascas vermelhas. Só preenche se o contador ainda estiver em 0: um ajuste feito à mão fica.
        /// </summary>
        static void EnsureGddV8RareCrystal()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV8BalancePath);
            if (asset == null) return;
            var serialized = new SerializedObject(asset);
            var interval = serialized.FindProperty("rareInterval");
            if (interval.doubleValue != 0) return;
            interval.doubleValue = 60;
            SetCards(serialized.FindProperty("rareCards"), 30,
                     (RareCard.Damage, 2), (RareCard.Shards, 2), (RareCard.Critical, 0.10), (RareCard.CritDamage, 2));
            SetCards(serialized.FindProperty("rareTier2Cards"), 30,
                     (RareCard.Damage, 4), (RareCard.Shards, 4), (RareCard.Critical, 0.20), (RareCard.CritDamage, 4));
            serialized.FindProperty("rareChooseSeconds").doubleValue = 30;
            SetLine(serialized, "goldenRare", 500, 1, 1, 1);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] GDD v8: raro a cada 60 s, cartas do v8 de 30 s, tier 2 e o Raro Dourado de 500 vermelhas (M9e E1, D-283, D-286).");
        }

        /// <summary>
        /// M9e E2 (GDD v8.2 §3.5; D-244, D-250, D-254, D-290): os anúncios do Boost — curto ×2 / +10% / 20 min / +1 h,
        /// médio ×3 / +15% / 40 min / +2 h, grande ×4 / +20% / 80 min / +4 h —, o "tier 2 por 1 h" e o teto de 8 h do
        /// offline; e sai o boost de velocidade do v7 (D-290). Só preenche se os anúncios estiverem vazios.
        /// </summary>
        static void EnsureGddV8Ads()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV8BalancePath);
            if (asset == null) return;
            var serialized = new SerializedObject(asset);
            var list = serialized.FindProperty("adOffers");
            if (list.arraySize > 0) return;
            (AdKind kind, double multiplier, double crit, double minutes, double hours)[] offers =
            {
                (AdKind.Short, 2, 0.10, 20, 1), (AdKind.Medium, 3, 0.15, 40, 2), (AdKind.Big, 4, 0.20, 80, 4),
            };
            list.arraySize = offers.Length;
            for (int i = 0; i < offers.Length; i++)
            {
                var item = list.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("kind").intValue = (int)offers[i].kind;
                item.FindPropertyRelative("multiplier").doubleValue = offers[i].multiplier;
                item.FindPropertyRelative("critChance").doubleValue = offers[i].crit;
                item.FindPropertyRelative("minutes").doubleValue = offers[i].minutes;
                item.FindPropertyRelative("offlineHours").doubleValue = offers[i].hours;
            }
            serialized.FindProperty("adRareTier2Minutes").doubleValue = 60;
            serialized.FindProperty("adOfflineCapHours").doubleValue = 8;
            serialized.FindProperty("adSpeedDivisor").doubleValue = 0;
            serialized.FindProperty("adBoostSeconds").doubleValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] GDD v8: anúncios curto, médio e grande, o tier 2 por 1 h, teto de 8 h; sai o AD de velocidade (M9e E2, D-290).");
        }

        /// <summary>
        /// M9g E1 (GDD v8 §2.1; D-240, D-255, D-307, D-308): as 8 bombas (10%, 20%, 30%, 40%, 50%, 60%, 70% e 100% da vida
        /// máxima), a carga de 5 s, o voo de 0,4 s, o alcance de 4 un e a linha Bomba no descanso, 7 compras a 10 + 2^(n−1)
        /// lascas vermelhas. Só preenche se as bombas estiverem vazias: um ajuste feito à mão fica.
        /// </summary>
        static void EnsureGddV8Bomb()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV8BalancePath);
            if (asset == null) return;
            var serialized = new SerializedObject(asset);
            var list = serialized.FindProperty("bombFractions");
            if (list == null || list.arraySize > 0) return;
            double[] fractions = { 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 1 };
            list.arraySize = fractions.Length;
            for (int i = 0; i < fractions.Length; i++) list.GetArrayElementAtIndex(i).doubleValue = fractions[i];
            serialized.FindProperty("bombChargeSeconds").doubleValue = 5;
            serialized.FindProperty("bombFlightSeconds").doubleValue = 0.4;
            serialized.FindProperty("bombReach").doubleValue = 4;
            SetLine(serialized, "bomb", 10, 2, 7, 1, CostFormula.OffsetExponential);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] GDD v8: as 8 bombas, carga de 5 s, voo de 0,4 s, alcance de 4 un e a linha Bomba (M9g E1).");
        }

        static void SetCards(SerializedProperty list, double seconds, params (RareCard card, double value)[] cards)
        {
            list.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++)
            {
                var item = list.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("card").enumValueIndex = (int)cards[i].card;
                item.FindPropertyRelative("value").doubleValue = cards[i].value;
                item.FindPropertyRelative("seconds").doubleValue = seconds;
            }
        }

        /// <summary>
        /// Os marcos de andar do GDD v8 (M9c E3, D-253): o avanço automático e o estagiário no andar 5 (P6, P7), o raro
        /// depois do 10. Só preenche se os três estiverem em 0: um ajuste feito à mão no Inspector fica.
        /// </summary>
        static void EnsureGddV8FloorMilestones()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MiningRulesAsset>(GddV8BalancePath);
            if (asset == null) return;
            var serialized = new SerializedObject(asset);
            var auto = serialized.FindProperty("autoAdvanceFloor");
            var intern = serialized.FindProperty("internFloor");
            var rare = serialized.FindProperty("rareFloor");
            if (auto.intValue != 0 || intern.intValue != 0 || rare.intValue != 0) return;
            auto.intValue = 5;
            intern.intValue = 5;
            rare.intValue = 10;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("[Setup] GDD v8: marcos de andar — avanço automático e estagiário no 5, raro depois do 10 (M9c E3).");
        }

        static void SetLine(SerializedObject serialized, string line, double baseCost, double costGrowth, int maxLevel, double effect,
                            CostFormula formula = CostFormula.Exponential)
        {
            serialized.FindProperty(line + ".costFormula").enumValueIndex = (int)formula;
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
