#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using MythicChronicles;

namespace MythicChronicles.Editor
{
    public static class ChroniclesProgressVerification
    {
        [MenuItem("Tools/Chronicles/Run Offline Progress Checks")]
        public static void RunChecks()
        {
            string folder = Path.Combine(Path.GetTempPath(),
                "chronicles_test_" + Guid.NewGuid().ToString("N"));
            try
            {
                var service = new PlayerProgressService(folder);
                Check(service.Data.heroes.Count == 10, "Starter collection must contain ten heroes.");
                Check(service.Data.lastClearedStage == 0, "New player must begin before stage 1.");
                Check(!service.CompleteStage(2), "Locked stages must not be claimable.");

                int beforeFirst = service.Data.gold;
                Check(service.CompleteStage(1), "Stage 1 must be claimable on victory.");
                int firstReward = WorldsCatalog.GetStage(1).gold;
                Check(service.Data.gold == beforeFirst + firstReward, "Stage 1 rewards mismatch.");
                Check(service.CompleteStage(1), "Replaying a cleared stage should be allowed.");
                Check(service.Data.gold == beforeFirst + firstReward,
                    "Replays must not grant first-clear rewards twice.");

                Check(service.ClaimDailyQuest("win_1"), "Winning a stage should complete the first daily quest.");
                int rewardedGold = service.Data.gold;
                Check(!service.ClaimDailyQuest("win_1"), "Daily quest must not be claimable twice.");
                Check(service.Data.gold == rewardedGold, "Duplicate daily claim changed currency.");

                Check(!service.SetTeam(new[] { "horus", "horus", "ra", "isis", "set" }),
                    "Duplicate team heroes must be rejected.");
                Check(service.SetTeam(new[] { "horus", "anubis", "ra", "isis", "set" }),
                    "Valid five-hero formation must save.");

                OwnedHero horus = service.FindOwnedHero("horus");
                Check(horus != null, "Starter Horus is missing.");
                Check(service.UpgradeHero("horus") && horus.level == 2, "Hero level upgrade failed.");
                Check(service.UpgradeEquipment("horus", true) && horus.weaponLevel == 1,
                    "Weapon upgrade did not persist.");
                Check(!service.AscendHero("horus"), "Ascension must require fragments.");
                int tickets = service.Data.summonTickets;
                Check(service.SummonHero() != null, "Available summon ticket failed.");
                Check(service.Data.summonTickets == tickets - 1, "Summon ticket was not consumed.");

                service.Data.lastIdleClaimTicks = DateTime.UtcNow.AddMinutes(-140).Ticks;
                int idleGold = service.ClaimIdleGold();
                Check(idleGold > 0, "Idle collection should return accumulated currency.");
                Check(service.ClaimIdleGold() == 0, "Idle collection must not double-claim elapsed time.");
                Check(service.QuestProgress("idle_1") >= 1, "AFK mission should update.");
                Check(service.WeeklyQuestProgress("idle_minutes_180") >= 100,
                    "Weekly AFK mission should track minutes.");

                Check(service.Save(), "Failed to save progress.");
                var restored = new PlayerProgressService(folder);
                Check(restored.Data.lastClearedStage == 1, "Stage progression did not reload.");
                Check(restored.FindOwnedHero("horus").level == 2, "Hero level did not reload.");
                Check(restored.FindOwnedHero("horus").weaponLevel == 1, "Gear progression did not reload.");
                Check(restored.Data.selectedTeam.Count == 5, "Formation did not reload.");
                Check(restored.Data.stageRewardsClaimed.Count == 1, "Cleared stage log did not reload.");

                Debug.Log("[Chronicles] Offline progress checks passed. " +
                    "This does NOT mean that the game has passed a Unity/Android playtest.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[Chronicles] Offline progress check failed: " + ex);
                throw;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(folder)) Directory.Delete(folder, true);
                }
                catch (Exception cleanup) { Debug.LogWarning("[Chronicles] Test cleanup: " + cleanup.Message); }
            }
        }

        private static void Check(bool isTrue, string message)
        {
            if (!isTrue) throw new InvalidOperationException(message);
        }
    }
}
#endif
