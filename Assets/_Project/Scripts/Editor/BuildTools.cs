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

        static string BuildAndroid(bool development)
        {
            Directory.CreateDirectory(OutputFolder);
            var path = Path.GetFullPath(Path.Combine(OutputFolder, DevApkName));

            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = path,
                target = BuildTarget.Android,
                options = development ? BuildOptions.Development : BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[Build] {summary.result}, {summary.totalSize / (1024f * 1024f):0.0} MB, " +
                      $"{summary.totalTime.TotalSeconds:0} s, {summary.totalErrors} erro(s).");
            if (summary.result != BuildResult.Succeeded)
                throw new Exception($"Build falhou: {summary.result} ({summary.totalErrors} erros).");
            return path;
        }
    }
}
