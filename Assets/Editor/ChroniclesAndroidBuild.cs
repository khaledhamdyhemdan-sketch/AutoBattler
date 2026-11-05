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
            PlayerSettings.defaultInterfaceOrientation = ScreenOrientation.Portrait;

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
    }
}
#endif
