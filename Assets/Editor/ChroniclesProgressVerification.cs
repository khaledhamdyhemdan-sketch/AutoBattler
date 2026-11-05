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
                Check(service.Data.equipment.Count == 1,
                    "First clear must generate one item and replay must not duplicate it.");
                GearInstance firstGear = service.Data.equipment[0];
                Check(service.EquipGear(firstGear.instanceId, "horus"),
                    "Hero should equip the first gear drop.");
                Check(service.GearBonus("horus").equippedCount == 1,
                    "Equipped gear must contribute combat bonuses.");
                service.Data.gold = 100000; // test-only currency in isolated directory
                Check(service.UpgradeGear(firstGear.instanceId) && firstGear.level == 1,
                    "Equipped gear enhancement failed.");
                Check(service.UnequipGear(firstGear.instanceId),
                    "Unequipping gear failed.");
                Check(service.GearBonus("horus").equippedCount == 0,
                    "Unequipped gear must not contribute stats.");
                Check(service.EquipGear(firstGear.instanceId, "horus"),
                    "Re-equipping acquired gear failed.");
                Check(ChroniclesEquipment.RewardForStage(31) != null,
                    "Deterministic campaign gear table is incomplete.");

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

                HeroSkin alternate = HeroSkinCatalog.Find("horus", "ascendant");
                Check(alternate != null, "Hero alternate skin metadata missing.");
                int beforeSkinGems = service.Data.gems;
                Check(service.UnlockSkin("horus", "ascendant"), "Unable to unlock owned hero skin.");
                Check(service.Data.gems == beforeSkinGems - alternate.gemCost,
                    "Incorrect gem charge for skin.");
                Check(!service.UnlockSkin("horus", "ascendant"), "Skin must not charge a second time.");
                Check(service.EquipSkin("horus", "ascendant"), "Owned skin must be equippable.");

                Check(service.UpgradeSkill("horus", "active"), "Skill upgrade did not work.");
                Check(service.FindOwnedHero("horus").activeSkillLevel == 2,
                    "Active skill level must increase.");
                Check(service.BuyTalent("horus", true), "Attack talent cannot be purchased.");
                Check(service.ResetTalents("horus"), "Talent reset should refund investment.");
                Check(ChroniclesGrowth.EffectiveLevel(service.Data, horus) >= 2,
                    "Resonance must not reduce trained hero level.");
                int beforeRefund = service.Data.gold;
                int expectedRefund = ChroniclesGrowth.RefundForLevelReset(horus.level);
                Check(service.ResetHeroLevel("horus"), "Level reset failed.");
                Check(horus.level == 1 && service.Data.gold == beforeRefund + expectedRefund,
                    "Level reset did not refund the exact training gold.");
                Check(!service.ResetHeroLevel("horus"), "Double reset must not produce gold.");
                Check(service.UpgradeHero("horus"), "Hero level upgrade after reset failed.");
                for (int stage = 2; stage <= 5; stage++)
                    Check(service.CompleteStage(stage), "Sequential stage reward failed at " + stage);
                Check(service.Data.runes.Count == 1, "Stage five should award one rune.");
                RuneInstance rune = service.Data.runes[0];
                Check(service.EquipRune(rune.instanceId, "horus"), "Rune equip failed.");
                Check(service.UpgradeRune(rune.instanceId), "Rune enhancement failed.");
                Check(service.UnequipRune(rune.instanceId), "Rune removal failed.");
                Check(service.EquipRune(rune.instanceId, "horus"), "Rune re-equip failed.");
                Check(ChroniclesGrowth.Calculate(service.Data, horus).attackMultiplier > 1f,
                    "Attack talent/rune modifiers should be observable.");

                service.Data.lastIdleClaimTicks = DateTime.UtcNow.AddMinutes(-140).Ticks;
                int idleGold = service.ClaimIdleGold();
                Check(idleGold > 0, "Idle collection should return accumulated currency.");
                Check(service.ClaimIdleGold() == 0, "Idle collection must not double-claim elapsed time.");
                Check(service.QuestProgress("idle_1") >= 1, "AFK mission should update.");
                Check(service.WeeklyQuestProgress("idle_minutes_180") >= 100,
                    "Weekly AFK mission should track minutes.");

                Check(service.Save(), "Failed to save progress.");
                var restored = new PlayerProgressService(folder);
                Check(restored.Data.lastClearedStage == 5, "Stage progression did not reload.");
                Check(restored.FindOwnedHero("horus").level == 2, "Hero level did not reload.");
                Check(restored.FindOwnedHero("horus").weaponLevel == 1, "Gear progression did not reload.");
                Check(restored.FindOwnedHero("horus").skin == "ascendant",
                    "Equipped skin did not reload.");
                Check(restored.Data.selectedTeam.Count == 5, "Formation did not reload.");
                Check(restored.Data.stageRewardsClaimed.Count == 5, "Cleared stage log did not reload.");
                Check(restored.Data.equipment.Count == 5, "Five unique gear rewards did not reload.");
                Check(restored.Data.runes.Count == 1 &&
                    restored.Data.runes[0].assignedHeroId == "horus",
                    "Equipped rune did not persist.");
                Check(restored.FindOwnedHero("horus").activeSkillLevel == 2,
                    "Upgraded skills did not persist.");

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
