using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MinersIdle.EditorTools
{
    /// <summary>
    /// Build de desenvolvimento para Android (APK). Pelo menu ou em batch:
    ///   Unity.exe -batchmode -nographics -projectPath &lt;pasta&gt; -buildTarget Android
    ///             -executeMethod MinersIdle.EditorTools.BuildTools.BuildAndroidDevBatch -logFile Logs/build.log
    /// A saída fica em tools/build/out/ (ignorada pelo git).
    /// </summary>
    public static class BuildTools
    {
        const string OutputFolder = "tools/build/out";
        const string DevApkName = "MinersIdle-dev.apk";

        [MenuItem("Tools/Miners Idle/Build/APK de desenvolvimento (Android)")]
        public static void BuildAndroidDev()
        {
            var path = BuildAndroid(development: true);
            Debug.Log($"[Build] APK de desenvolvimento: {path}");
        }

        /// <summary>
        /// Gera o APK, instala no celular conectado por USB e abre o jogo (opção "Build And Run" do Unity). Precisa de um
        /// aparelho aparecendo em <c>adb devices</c>.
        /// </summary>
        [MenuItem("Tools/Miners Idle/Build/Rodar no celular (Android)")]
        public static void BuildAndRunAndroidDev()
        {
            var path = BuildAndroid(development: true, run: true);
            Debug.Log($"[Build] Instalado e aberto no celular: {path}");
        }

        public static void BuildAndroidDevBatch()
        {
            try
            {
                BuildAndroid(development: true);
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>A versão de um build: a do projeto e a data e a hora do build (<c>0.1.0.2611191530</c>).</summary>
        internal static string StampedVersion(DateTime when) => $"{ProjectSetup.VersionName}.{when:yyMMddHHmm}";

        static string BuildAndroid(bool development, bool run = false)
        {
            ProjectSetup.ApplyBeforeBuild();

            Directory.CreateDirectory(OutputFolder);
            var path = Path.GetFullPath(Path.Combine(OutputFolder, DevApkName));

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = path,
                target = BuildTarget.Android,
                options = (development ? BuildOptions.Development : BuildOptions.None) |
                          (run ? BuildOptions.AutoRunPlayer : BuildOptions.None),
            };

            // M9i E4 (D-337): cada build leva a data e a hora na versão (0.1.0.AAMMDDhhmm), para o jogo saber que um save
            // veio de um build mais velho. Volta para a versão do projeto no fim: o ProjectSettings não muda no git.
            string stamped = StampedVersion(DateTime.Now);
            PlayerSettings.bundleVersion = stamped;
            BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(options);
            }
            finally
            {
                PlayerSettings.bundleVersion = ProjectSetup.VersionName;
            }
            Debug.Log($"[Build] Versão {stamped}.");
            var summary = report.summary;
            Debug.Log($"[Build] {summary.result}, {summary.totalSize / (1024f * 1024f):0.0} MB, " +
                      $"{summary.totalTime.TotalSeconds:0} s, {summary.totalErrors} erro(s).");
            if (summary.result != BuildResult.Succeeded)
                throw new Exception($"Build falhou: {summary.result} ({summary.totalErrors} erros).");
            return path;
        }
    }
}
