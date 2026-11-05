#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MythicChronicles.Editor
{
    public static class ChroniclesTrialsVerification
    {
        [MenuItem("Tools/Chronicles/Test Elite & Dark Trial Progress")]
        public static void Run()
        {
            string folder = Path.Combine(Path.GetTempPath(),
                "chronicles_trials_test_" + Guid.NewGuid().ToString("N"));
            try
            {
                var player = new PlayerProgressService(folder);
                Check(!player.CanPlayTrial(1, TrialMode.Elite),
                    "Elite must be locked until its normal chapter boss is defeated.");
                Check(!player.CanPlayTrial(1, TrialMode.Dark),
                    "Dark must be locked until its Elite chapter boss is defeated.");
                Check(!player.CompleteTrial(1, TrialMode.Elite),
                    "Locked Elite battle must not grant a reward.");
                for (int stage = 1; stage <= 10; stage++)
                    Check(player.CompleteStage(stage), "Cannot progress normal stage " + stage);

                Check(player.CanPlayTrial(1, TrialMode.Elite),
                    "Elite stage one should unlock at first normal chapter boss.");
                Check(!player.CanPlayTrial(2, TrialMode.Elite),
                    "Elite stage two must wait for its predecessor.");
                int goldBefore = player.Data.gold;
                Check(player.CompleteTrial(1, TrialMode.Elite),
                    "Elite first clear failed.");
                int goldAfter = player.Data.gold;
                Check(goldAfter > goldBefore, "Elite clear must award extra gold.");
                Check(player.CompleteTrial(1, TrialMode.Elite) &&
                    player.Data.gold == goldAfter,
                    "Replaying Elite cannot duplicate first-clear rewards.");

                for (int stage = 2; stage <= 10; stage++)
                    Check(player.CompleteTrial(stage, TrialMode.Elite),
                        "Elite trial failed at stage " + stage);
                Check(!player.CanPlayTrial(11, TrialMode.Elite),
                    "Chapter two Elite must remain locked before normal chapter two boss.");
                Check(player.CanPlayTrial(1, TrialMode.Dark),
                    "Dark chapter one must unlock after the Elite chapter boss.");
                Check(!player.CanPlayTrial(2, TrialMode.Dark),
                    "Dark stages must follow their own sequence.");

                for (int stage = 1; stage <= 10; stage++)
                    Check(player.CompleteTrial(stage, TrialMode.Dark),
                        "Dark trial failed at stage " + stage);
                Check(player.Data.eliteClears.Count == 10 &&
                    player.Data.darkClears.Count == 10,
                    "Clears must be tracked separately.");
                Check(player.Data.astralSeals >= 7,
                    "Normal, Elite and Dark boss milestones should award seals.");
                Check(ChroniclesTrials.NextAvailable(player.Data, TrialMode.Elite, 1) == -1 &&
                    ChroniclesTrials.NextAvailable(player.Data, TrialMode.Dark, 1) == -1,
                    "Completed challenge chapters should not offer extra first clears.");
                Check(ChroniclesTrials.EnemyHealthMultiplier(TrialMode.Dark) >
                    ChroniclesTrials.EnemyHealthMultiplier(TrialMode.Elite),
                    "Dark must have a higher enemy difficulty than Elite.");

                Check(player.Save(), "Cannot save trial progress.");
                var restored = new PlayerProgressService(folder);
                Check(restored.Data.eliteClears.Count == 10 &&
                    restored.Data.darkClears.Count == 10,
                    "Elite/Dark clears must survive save reload.");
                Check(restored.Data.astralSeals == player.Data.astralSeals,
                    "Milestone seals must persist.");

                Debug.Log("[Chronicles] Offline Elite/Dark stage checks passed. " +
                    "Unity battle visuals and actual stage balance still need playtests.");
            }
            finally
            {
                try
                {
                    if (Directory.Exists(folder)) Directory.Delete(folder, true);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[Chronicles] Trial test cleanup: " + e.Message);
                }
            }
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
#endif
