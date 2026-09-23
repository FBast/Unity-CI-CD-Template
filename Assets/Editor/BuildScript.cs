using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Pas de namespace : le workflow appelle -executeMethod BuildScript.Build.
// Si tu ajoutes un namespace, adapte l'appel en <Namespace>.BuildScript.Build.
public static class BuildScript
{
    // Unity.exe -batchmode -buildTarget <cible> -executeMethod BuildScript.Build -customBuildPath <dossier>
    public static void Build()
    {
        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        string outDir = GetArg("-customBuildPath") ?? "Build";
        string name = PlayerSettings.productName;

        string path = target switch
        {
            BuildTarget.StandaloneWindows64 => $"{outDir}/{name}.exe",
            BuildTarget.StandaloneLinux64   => $"{outDir}/{name}.x86_64",
            BuildTarget.StandaloneOSX       => $"{outDir}/{name}.app",
            BuildTarget.Android             => $"{outDir}/{name}.{(EditorUserBuildSettings.buildAppBundle ? "aab" : "apk")}",
            _                               => outDir // WebGL et autres : un dossier
        };

        // Android : mots de passe du keystore lus depuis les secrets GitHub
        if (target == BuildTarget.Android)
        {
            string ksPass = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASS");
            string aliasPass = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_PASS");
            if (!string.IsNullOrEmpty(ksPass)) PlayerSettings.Android.keystorePass = ksPass;
            if (!string.IsNullOrEmpty(aliasPass)) PlayerSettings.Android.keyaliasPass = aliasPass;
        }

        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            Debug.LogError("[BuildScript] Aucune scene active dans les Build Settings.");
            EditorApplication.Exit(1);
            return;
        }

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = path,
            target = target,
            targetGroup = BuildPipeline.GetBuildTargetGroup(target),
            options = BuildOptions.None
        };

        BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;
        Debug.Log($"[BuildScript] {summary.result} | {summary.totalErrors} error(s) | {summary.totalSize / (1024 * 1024)} MB | {summary.totalTime}");

        EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    private static string GetArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
