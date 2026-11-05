#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MythicChronicles.Editor
{
    /// <summary>
    /// Execute as Tools > Chronicles > Run All Offline Checks or:
    /// Unity -batchmode -projectPath <root>
    ///   -executeMethod MythicChronicles.Editor.ChroniclesAllChecks.Run
    ///   -logFile <editor.log> -quit
    ///
    /// Requires a matching Unity Editor installation. Does not validate an APK,
    /// licensed artwork, network play or a production economy.
    /// </summary>
    public static class ChroniclesAllChecks
    {
        [MenuItem("Tools/Chronicles/Run All Offline Checks")]
        public static void Run()
        {
            var failures = new List<string>();
            RunStep("Catalog", ChroniclesProjectSetup.ValidateGameCatalog, failures);
            RunStep("Progress", ChroniclesProgressVerification.RunChecks, failures);
            RunStep("Trials", ChroniclesTrialsVerification.Run, failures);
            RunStep("Towers", ChroniclesTowerVerification.Run, failures);
            RunStep("Summons", ChroniclesSummonVerification.Run, failures);
            RunStep("Login", ChroniclesLoginVerification.Run, failures);
            RunStep("World Boss", ChroniclesRaidVerification.Run, failures);
            RunStep("Milestones", ChroniclesMilestoneVerification.Run, failures);
            RunStep("Achievements", ChroniclesAchievementVerification.Run, failures);
            RunStep("Labyrinth", ChroniclesLabyrinthVerification.Run, failures);
            RunStep("Arena Training", ChroniclesArenaTrainingVerification.Run, failures);
            RunStep("Portrait and English Audit", ChroniclesPortraitAudit.Audit, failures);

            if (failures.Count > 0)
                throw new InvalidOperationException(
                    "[Chronicles] Offline suite FAILED: " + string.Join(" | ", failures));
            Debug.Log("[Chronicles] Offline checks completed without observed errors. " +
                "Original portrait imports, actual Android builds, online safety and playtesting " +
                "are distinct release requirements.");
        }

        private static void RunStep(string label, Action run, List<string> failures)
        {
            int loggedErrors = 0;
            Application.LogCallback handler = (message, stack, type) =>
            {
                if (type == LogType.Error ||
                    type == LogType.Exception ||
                    type == LogType.Assert)
                    loggedErrors++;
            };
            Application.logMessageReceived += handler;
            try
            {
                run();
                if (loggedErrors > 0)
                    failures.Add(label + ": " + loggedErrors + " Unity Console errors.");
                else
                    Debug.Log("[Chronicles] PASS: " + label);
            }
            catch (Exception ex)
            {
                failures.Add(label + ": " + ex.Message);
                Debug.LogError("[Chronicles] FAIL: " + label + " - " + ex);
            }
            finally
            {
                Application.logMessageReceived -= handler;
            }
        }
    }
}
#endif
