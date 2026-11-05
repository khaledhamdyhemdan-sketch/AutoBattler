#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MythicChronicles.Editor
{
    public static class ChroniclesMilestoneVerification
    {
        [MenuItem("Tools/Chronicles/Test Campaign Milestone Chests")]
        public static void Run()
        {
            string folder = Path.Combine(Path.GetTempPath(),
                "chronicles_milestones_" + Guid.NewGuid().ToString("N"));
            try
            {
                var player = new PlayerProgressService(folder);
                Check(!player.CanClaimCampaignMilestone(20),
                    "Locked chest should not be accessible.");
                Check(!player.ClaimCampaignMilestone(20),
                    "A player cannot claim unreached milestones.");
                Check(ChroniclesMilestones.At(19) == null &&
                    ChroniclesMilestones.At(20) != null &&
                    ChroniclesMilestones.At(40) != null &&
                    ChroniclesMilestones.At(60) != null,
                    "Expected stage 20, 40 and 60 only.");

                // Simulate legitimate saved progress in this isolated test file
                // to verify retroactive eligibility and unique claims.
                player.Data.lastClearedStage = 60;
                int gold = player.Data.gold;
                int seals = player.Data.astralSeals;
                Check(player.CanClaimCampaignMilestone(20), "Old saves may claim reached milestones.");
                Check(player.ClaimCampaignMilestone(20), "Stage 20 reward not claimable.");
                Check(!player.CanClaimCampaignMilestone(20) &&
                    !player.ClaimCampaignMilestone(20),
                    "A milestone reward must not be claimable twice.");
                Check(player.ClaimCampaignMilestone(40) &&
                    player.ClaimCampaignMilestone(60),
                    "Stage 40/60 rewards failed.");
                Check(player.Data.gold == gold + 3000 + 6000 + 10000 &&
                    player.Data.astralSeals == seals + 1 + 2 + 3 &&
                    player.Data.campaignMilestonesClaimed.Count == 3,
                    "Campaign reward totals were miscounted.");

                var restored = new PlayerProgressService(folder);
                Check(restored.Data.campaignMilestonesClaimed.Count == 3 &&
                    !restored.CanClaimCampaignMilestone(60),
                    "Milestone claims were not persisted to the save.");
                Debug.Log("[Chronicles] Milestone reward offline checks passed. " +
                    "This does not replace a Unity or Android playtest.");
            }
            finally
            {
                try { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
                catch (Exception e) { Debug.LogWarning(e.Message); }
            }
        }

        private static void Check(bool passed, string message)
        {
            if (!passed) throw new InvalidOperationException(message);
        }
    }
}
#endif
