#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MythicChronicles.Editor
{
    public static class ChroniclesRaidVerification
    {
        [MenuItem("Tools/Chronicles/Test World Boss Raid")]
        public static void Run()
        {
            string folder = Path.Combine(Path.GetTempPath(),
                "chronicles_raid_test_" + Guid.NewGuid().ToString("N"));
            try
            {
                var service = new PlayerProgressService(folder);
                Check(service.RaidAttemptsRemaining() == 3,
                    "A new player should have three daily attempts.");
                Check(service.BeginRaid() == null,
                    "World Boss should stay locked until chapter one is cleared.");
                service.Data.lastClearedStage = 10;
                Check(service.Save(), "Cannot save test unlock state.");
                Check(ChroniclesRaids.StageForDay(DateTime.UtcNow).boss,
                    "Daily challenge must use a guardian boss stage.");

                string run1 = service.BeginRaid();
                Check(!string.IsNullOrEmpty(run1), "First raid must start.");
                Check(service.RaidAttemptsRemaining() == 2,
                    "An attempt is consumed at raid start.");
                Check(service.BeginRaid() == null,
                    "Cannot start a second raid while the first is pending.");
                Check(!service.CompleteRaid("forged-token", 100000),
                    "An unknown run ID must not award rewards.");

                int gemsBefore = service.Data.gems;
                int sealsBefore = service.Data.astralSeals;
                Check(service.CompleteRaid(run1, 20000), "First raid result failed.");
                Check(service.Data.raidBestDamage == 20000 &&
                    ChroniclesRaids.Rank(service.Data.raidBestDamage) == "MASTER HUNTER",
                    "Best score or personal rank was not recorded.");
                Check(service.Data.gems == gemsBefore + 24,
                    "Rank two should grant only two tiers of gem milestones.");
                Check(!service.CompleteRaid(run1, 20000),
                    "A completed run cannot be claimed twice.");

                string run2 = service.BeginRaid();
                Check(!string.IsNullOrEmpty(run2), "Second attempt missing.");
                int secondGems = service.Data.gems;
                Check(service.CompleteRaid(run2, 18000),
                    "Lower-score raid should still finish.");
                Check(service.Data.raidBestDamage == 20000 &&
                    service.Data.gems == secondGems,
                    "A lower score must not repeat milestone gems.");

                string run3 = service.BeginRaid();
                Check(!string.IsNullOrEmpty(run3), "Third attempt missing.");
                Check(service.CompleteRaid(run3, 90000), "Third attempt did not complete.");
                Check(service.Data.astralSeals == sealsBefore + 1,
                    "Crossing tier three must grant one total Astral Seal.");
                Check(service.Data.raidRewardTier == 4 &&
                    service.RaidAttemptsRemaining() == 0,
                    "Three-attempt cap or milestone tier is incorrect.");
                Check(service.BeginRaid() == null,
                    "Daily attempts must not be exceeded.");

                Check(service.Save(), "Raid progress save failed.");
                var restored = new PlayerProgressService(folder);
                Check(restored.Data.raidBestDamage == 90000 &&
                    restored.RaidAttemptsRemaining() == 0 &&
                    restored.Data.raidRewardTier == 4,
                    "Best damage, attempts and tier did not persist.");

                restored.Data.raidUtcDay = "2020-01-01";
                Check(restored.Save(), "Failed to prepare daily rollover test.");
                Check(restored.RaidAttemptsRemaining() == 3 &&
                    restored.Data.raidBestDamage == 0 &&
                    restored.Data.raidRewardTier == 0,
                    "Daily reset must clear the old score, attempts and milestones.");
                Debug.Log("[Chronicles] Offline World Boss checks passed. " +
                    "Live rankings, anti-cheat and real UTC require a backend.");
            }
            finally
            {
                try { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
                catch (Exception e) { Debug.LogWarning(e.Message); }
            }
        }

        private static void Check(bool pass, string message)
        {
            if (!pass) throw new InvalidOperationException(message);
        }
    }
}
#endif
