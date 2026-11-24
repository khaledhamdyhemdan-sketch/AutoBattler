#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MythicChronicles.Editor
{
    public static class ChroniclesAndroidBuild
    {
        private const string MainScene = "Assets/Chronicles/Chronicles_Main.unity";

        [MenuItem("Tools/Chronicles/Build Android APK")]
        public static void BuildApk() { Build(false); }

        [MenuItem("Tools/Chronicles/Build Android AAB")]
        public static void BuildAab() { Build(true); }

        private static void Build(bool bundle)
        {
            if (!File.Exists(MainScene))
                throw new InvalidOperationException("Main game scene is missing. Run Tools/Chronicles/Create Main Game Scene.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Android Build Support is not installed in Unity Hub.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException(
                    "Switch to Android build target before building. " +
                    "The local PowerShell script supplies -buildTarget Android.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScene) == null)
                throw new InvalidOperationException(
                    "The main Chronicles scene cannot be imported by Unity.");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            if (bundle && (!PlayerSettings.Android.useCustomKeystore ||
                           string.IsNullOrEmpty(PlayerSettings.Android.keystoreName)))
                throw new InvalidOperationException(
                    "Configure your release signing keystore locally in Unity Player Settings. Never commit private keystores or passwords.");

            string folder = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Chronicles");
            Directory.CreateDirectory(folder);
            bool previousBundle = EditorUserBuildSettings.buildAppBundle;
            bool previousCustomKeystore = PlayerSettings.Android.useCustomKeystore;
            // Development APKs use Unity's debug signing. The owner's
            // release keystore stays untouched and is never stored in Git.
            if (!bundle) PlayerSettings.Android.useCustomKeystore = false;
            EditorUserBuildSettings.buildAppBundle = bundle;
            string destination = Path.Combine(folder, bundle ? "Chronicles_Android.aab" : "Chronicles_Android.apk");
            try
            {
                var settings = new BuildPlayerOptions
                {
                    scenes = new[] { MainScene },
                    locationPathName = destination,
                    target = BuildTarget.Android,
                    options = bundle ? BuildOptions.None :
                        BuildOptions.Development | BuildOptions.AllowDebugging
                };
                BuildReport report = BuildPipeline.BuildPlayer(settings);
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException(
                        "Android build failed: " + report.summary.result +
                        " (see the Unity Console and Editor log for diagnostic errors).");
                Debug.Log("[Chronicles] Android build succeeded: " + destination);
            }
            finally
            {
                EditorUserBuildSettings.buildAppBundle = previousBundle;
                PlayerSettings.Android.useCustomKeystore = previousCustomKeystore;
            }
        }

        /// <summary>
        /// CI entry point: builds the release-signed APK using the keystore
        /// supplied through environment variables (never committed). Falls
        /// back to debug signing when the variables are absent.
        /// </summary>
        public static void BuildApkFromCi()
        {
            if (!File.Exists(MainScene))
                throw new InvalidOperationException("Main game scene is missing.");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            string ks = Environment.GetEnvironmentVariable("KEYSTORE_PATH");
            string ksPass = Environment.GetEnvironmentVariable("KEYSTORE_PASS");
            string alias = Environment.GetEnvironmentVariable("KEYALIAS_NAME");
            string aliasPass = Environment.GetEnvironmentVariable("KEYALIAS_PASS");
            bool signed = !string.IsNullOrEmpty(ks) && File.Exists(ks);
            Debug.Log("[Chronicles] CI keystore path='" + ks + "' exists=" +
                (ks != null && File.Exists(ks)) +
                (signed ? "" : " -> debug signing fallback"));
            if (signed)
            {
                // Unity validates the keystore relative to the project in some
                // pipeline paths, so copy it inside the workspace.
                string localKs = Path.Combine(Directory.GetCurrentDirectory(),
                    "Builds", "Chronicles", "ci.keystore");
                Directory.CreateDirectory(Path.GetDirectoryName(localKs));
                File.Copy(ks, localKs, true);
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = localKs;
                PlayerSettings.Android.keystorePass = ksPass;
                PlayerSettings.Android.keyaliasName = alias;
                PlayerSettings.Android.keyaliasPass = aliasPass;
                Debug.Log("[Chronicles] CI signing with keystore '" + localKs +
                    "' alias='" + alias + "' fileExists=" + File.Exists(localKs));
            }
            else
            {
                PlayerSettings.Android.useCustomKeystore = false;
                Debug.LogWarning("[Chronicles] CI keystore not found; building with debug signing.");
            }
            string folder = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Chronicles");
            Directory.CreateDirectory(folder);
            string destination = Path.Combine(folder, "Chronicles_Android.apk");
            EditorUserBuildSettings.buildAppBundle = false;
            var settings = new BuildPlayerOptions
            {
                scenes = new[] { MainScene },
                locationPathName = destination,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(settings);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException(
                    "Android build failed: " + report.summary.result);
            Debug.Log("[Chronicles] CI Android build succeeded: " + destination);
            EditorApplication.Exit(0);
        }
    }
}

#endif
