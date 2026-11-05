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
                Check(service.Data.heroes.Count == 5, "Starter collection must contain five accessible heroes.");
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
                Check(service.EquipGear(firstGear.instanceId, "sobek"),
                    "Hero should equip the first gear drop.");
                Check(service.GearBonus("sobek").equippedCount == 1,
                    "Equipped gear must contribute combat bonuses.");
                service.Data.gold = 100000; // test-only currency in isolated directory
                Check(service.UpgradeGear(firstGear.instanceId) && firstGear.level == 1,
                    "Equipped gear enhancement failed.");
                Check(service.UnequipGear(firstGear.instanceId),
                    "Unequipping gear failed.");
                Check(service.GearBonus("sobek").equippedCount == 0,
                    "Unequipped gear must not contribute stats.");
                Check(service.EquipGear(firstGear.instanceId, "sobek"),
                    "Re-equipping acquired gear failed.");
                Check(ChroniclesEquipment.RewardForStage(31) != null,
                    "Deterministic campaign gear table is incomplete.");

                Check(service.ClaimDailyQuest("win_1"), "Winning a stage should complete the first daily quest.");
                int rewardedGold = service.Data.gold;
                Check(!service.ClaimDailyQuest("win_1"), "Daily quest must not be claimable twice.");
                Check(service.Data.gold == rewardedGold, "Duplicate daily claim changed currency.");

                Check(!service.SetTeam(new[] { "sobek", "sobek", "ra", "isis", "set" }),
                    "Duplicate team heroes must be rejected.");
                Check(service.SetTeam(WorldsCatalog.StartingHeroes),
                    "Valid five-hero formation must save.");

                OwnedHero sobek = service.FindOwnedHero("sobek");
                Check(sobek != null, "Starter Sobek is missing.");
                Check(service.UpgradeHero("sobek") && sobek.level == 2, "Hero level upgrade failed.");
                Check(service.UpgradeEquipment("sobek", true) && sobek.weaponLevel == 1,
                    "Weapon upgrade did not persist.");
                Check(!service.AscendHero("sobek"), "Ascension must require fragments.");
                int tickets = service.Data.summonTickets;
                Check(service.SummonHero() != null, "Available summon ticket failed.");
                Check(service.Data.summonTickets == tickets - 1, "Summon ticket was not consumed.");

                HeroSkin alternate = HeroSkinCatalog.Find("sobek", "ascendant");
                Check(alternate != null, "Hero alternate skin metadata missing.");
                int beforeSkinGems = service.Data.gems;
                Check(service.UnlockSkin("sobek", "ascendant"), "Unable to unlock owned hero skin.");
                Check(service.Data.gems == beforeSkinGems - alternate.gemCost,
                    "Incorrect gem charge for skin.");
                Check(!service.UnlockSkin("sobek", "ascendant"), "Skin must not charge a second time.");
                Check(service.EquipSkin("sobek", "ascendant"), "Owned skin must be equippable.");

                Check(service.UpgradeSkill("sobek", "active"), "Skill upgrade did not work.");
                Check(service.FindOwnedHero("sobek").activeSkillLevel == 2,
                    "Active skill level must increase.");
                Check(service.BuyTalent("sobek", true), "Attack talent cannot be purchased.");
                Check(service.ResetTalents("sobek"), "Talent reset should refund investment.");
                Check(ChroniclesGrowth.EffectiveLevel(service.Data, sobek) >= 2,
                    "Resonance must not reduce trained hero level.");
                int beforeRefund = service.Data.gold;
                int expectedRefund = ChroniclesGrowth.RefundForLevelReset(sobek.level);
                Check(service.ResetHeroLevel("sobek"), "Level reset failed.");
                Check(sobek.level == 1 && service.Data.gold == beforeRefund + expectedRefund,
                    "Level reset did not refund the exact training gold.");
                Check(!service.ResetHeroLevel("sobek"), "Double reset must not produce gold.");
                Check(service.UpgradeHero("sobek"), "Hero level upgrade after reset failed.");
                for (int stage = 2; stage <= 5; stage++)
                    Check(service.CompleteStage(stage), "Sequential stage reward failed at " + stage);
                Check(service.Data.runes.Count == 1, "Stage five should award one rune.");
                RuneInstance rune = service.Data.runes[0];
                Check(service.EquipRune(rune.instanceId, "sobek"), "Rune equip failed.");
                Check(service.UpgradeRune(rune.instanceId), "Rune enhancement failed.");
                Check(service.UnequipRune(rune.instanceId), "Rune removal failed.");
                Check(service.EquipRune(rune.instanceId, "sobek"), "Rune re-equip failed.");
                Check(ChroniclesGrowth.Calculate(service.Data, sobek).attackMultiplier > 1f,
                    "Attack talent/rune modifiers should be observable.");
                Check(service.AutoEquip("sobek"), "Auto-equip should work for an owned hero.");
                Check(service.GearBonus("sobek").equippedCount >= 1,
                    "Auto-equip should fill open slots.");
                GearInstance equipped = service.Data.equipment.Find(x =>
                    x.equippedHeroId == "sobek");
                Check(equipped != null, "Auto-equip left hero empty.");
                Check(service.AutoEquip("bastet"), "Auto-equip another hero failed.");
                Check(equipped.equippedHeroId == "sobek",
                    "Auto-equip must not steal another hero's equipment.");

                service.Data.forgeDust = 500; // Test-only materials; no live grant
                Check(service.CraftGear("sun_weapon"), "Forging known gear failed.");
                GearInstance crafted = service.Data.equipment.Find(x =>
                    x.instanceId.StartsWith("crafted_", StringComparison.Ordinal));
                Check(crafted != null, "Crafted gear should have a stable instance ID.");
                int dustBeforeSalvage = service.Data.forgeDust;
                Check(service.DismantleGear(crafted.instanceId), "Unequipped crafted gear salvage failed.");
                Check(service.Data.forgeDust > dustBeforeSalvage,
                    "Dismantling must add forge dust.");
                Check(!service.DismantleGear(crafted.instanceId), "Gear cannot be dismantled twice.");

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
                Check(restored.FindOwnedHero("sobek").level == 2, "Hero level did not reload.");
                Check(restored.FindOwnedHero("sobek").weaponLevel == 1, "Gear progression did not reload.");
                Check(restored.FindOwnedHero("sobek").skin == "ascendant",
                    "Equipped skin did not reload.");
                Check(restored.Data.selectedTeam.Count == 5, "Formation did not reload.");
                Check(restored.Data.stageRewardsClaimed.Count == 5, "Cleared stage log did not reload.");
                Check(restored.Data.equipment.Count == 5, "Five unique gear rewards did not reload.");
                Check(restored.Data.runes.Count == 1 &&
                    restored.Data.runes[0].assignedHeroId == "sobek",
                    "Equipped rune did not persist.");
                Check(restored.FindOwnedHero("sobek").activeSkillLevel == 2,
                    "Upgraded skills did not persist.");

                Check(HeroElements.DamageMultiplier(HeroElement.Fire, HeroElement.Nature) > 1f,
                    "Fire should counter Nature.");
                Check(HeroElements.DamageMultiplier(HeroElement.Nature, HeroElement.Fire) < 1f,
                    "Nature should be disadvantaged against Fire.");
                Check(HeroElements.DamageMultiplier(HeroElement.Nature, HeroElement.Wind) > 1f,
                    "Nature should counter Wind.");
                Check(HeroElements.DamageMultiplier(HeroElement.Wind, HeroElement.Water) > 1f,
                    "Wind should counter Water.");
                Check(HeroElements.DamageMultiplier(HeroElement.Water, HeroElement.Fire) > 1f,
                    "Water should counter Fire.");
                Check(HeroElements.DamageMultiplier(HeroElement.Light, HeroElement.Darkness) > 1f &&
                    HeroElements.DamageMultiplier(HeroElement.Darkness, HeroElement.Light) > 1f,
                    "Light and Darkness must counter each other.");
                var mono = new System.Collections.Generic.List<HeroDefinition>
                {
                    WorldsCatalog.FindHero("set"), WorldsCatalog.FindHero("sekhmet"),
                    WorldsCatalog.FindHero("heracles"), WorldsCatalog.FindHero("sobek"),
                    WorldsCatalog.FindHero("bastet")
                };
                Check(HeroElements.Calculate(mono).matchingCount == 3,
                    "Three Fire heroes must activate a covenant.");
                mono[2] = WorldsCatalog.FindHero("isis");
                Check(HeroElements.Calculate(mono).matchingCount == 3,
                    "Light wildcard should complete the strongest core element.");
                mono[3] = WorldsCatalog.FindHero("anubis");
                ElementalTeamBonus pair = HeroElements.Calculate(mono);
                Check(pair.lightCount == 1 && pair.darkCount == 1 &&
                    pair.startingEnergy > 0f, "Light/Dark pairing must grant Equinox energy.");
                int rCount = 0, srCount = 0, ssrCount = 0, rareElements = 0;
                foreach (HeroDefinition hero in WorldsCatalog.Heroes)
                {
                    switch (hero.rarity)
                    {
                        case Rarity.R: rCount++; break;
                        case Rarity.SR: srCount++; break;
                        case Rarity.SSR: ssrCount++; break;
                    }
                    if (HeroElements.IsSpecial(hero))
                    {
                        rareElements++;
                        Check(hero.rarity == Rarity.SSR,
                            "Light/Dark must be top available rank.");
                    }
                }
                Check(rCount == 2 && srCount == 8 && ssrCount == 20 && rareElements == 4,
                    "Launch rarity or special-element distribution mismatch.");
                foreach (string starterId in WorldsCatalog.StartingHeroes)
                    Check(!HeroElements.IsSpecial(WorldsCatalog.FindHero(starterId)),
                        "A rare Light/Dark hero must not be a free starter.");

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
