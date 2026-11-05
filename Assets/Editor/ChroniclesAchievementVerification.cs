#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MythicChronicles.Editor
{
    public static class ChroniclesAchievementVerification
    {
        [MenuItem("Tools/Chronicles/Test Permanent Achievements")]
        public static void Run()
        {
            string folder = Path.Combine(Path.GetTempPath(),
                "chronicles_achievements_" + Guid.NewGuid().ToString("N"));
            try
            {
                var player = new PlayerProgressService(folder);
                Check(ChroniclesAchievements.All.Length == 14, "Expected 14 achievements.");
                Check(!player.CanClaimAchievement("story10"),
                    "New players must not claim campaign achievements.");
                Check(!player.ClaimAchievement("invalid-id"),
                    "Undefined achievements must not be claimable.");

                player.Data.lastClearedStage = 30;
                Check(player.CanClaimAchievement("story10") &&
                    player.CanClaimAchievement("story30") &&
                    !player.CanClaimAchievement("story60"),
                    "Campaign achievements should derive from cleared stages.");
                int gold = player.Data.gold;
                int gems = player.Data.gems;
                Check(player.ClaimAchievement("story10"),
                    "Valid campaign achievement could not be claimed.");
                Check(player.Data.gold == gold + 400 &&
                    player.Data.gems == gems + 30,
                    "Achievement rewards incorrect.");
                Check(!player.ClaimAchievement("story10"),
                    "Claiming the same achievement twice must fail.");
                Check(player.ClaimAchievement("story30"),
                    "Second milestone claim failed.");

                Check(ChroniclesAchievements.Progress(player.Data, "ur1") == 0,
                    "Starter team must not contain a UR hero.");
                player.Data.heroes.Add(new OwnedHero { id = "odin" });
                Check(player.CanClaimAchievement("ur1"),
                    "A collected UR must unlock the UR milestone.");
                Check(player.ClaimAchievement("ur1"), "UR milestone reward failed.");

                Check(player.Save(), "Save failed.");
                var restored = new PlayerProgressService(folder);
                Check(restored.Data.claimedAchievements.Count == 3 &&
                    !restored.CanClaimAchievement("ur1") &&
                    !restored.CanClaimAchievement("story10"),
                    "Permanent achievements must not reset after reload.");
                Debug.Log("[Chronicles] Achievement save/claim checks passed. " +
                    "Run inside Unity; source presence alone is not a playtest.");
            }
            finally
            {
                try { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
                catch (Exception ex) { Debug.LogWarning(ex.Message); }
            }
        }

        private static void Check(bool ok, string message)
        {
            if (!ok) throw new InvalidOperationException(message);
        }
    }
}
#endif
