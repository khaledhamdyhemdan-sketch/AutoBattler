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

                for (int stage = 6; stage <= 10; stage++)
                    Check(service.CompleteStage(stage), "Cannot clear boss chapter stage " + stage);
                Check(service.Data.relics.Count == 1 &&
                    service.Data.relics[0].id == "sun_disc",
                    "First chapter boss must grant its unique relic.");
                Check(service.Data.equippedRelicId == "sun_disc",
                    "First chapter relic should equip by default.");
                int relicCount = service.Data.relics.Count;
                Check(service.CompleteStage(10), "Cleared boss should remain replayable.");
                Check(service.Data.relics.Count == relicCount,
                    "Repeated boss victory must not duplicate the relic.");
                Check(service.UpgradeRelic("sun_disc") &&
                    service.Data.relics[0].level == 2,
                    "Artifact upgrade must consume chapter relic dust.");
                while (sobek.level < 10)
                    Check(service.UpgradeHero("sobek"), "Cannot reach signature unlock level.");
                Check(service.UpgradeSignatureWeapon("sobek") &&
                    sobek.signatureWeaponRank == 1,
                    "Signature weapon must unlock at level 10.");

                service.Data.lastIdleClaimTicks = DateTime.UtcNow.AddMinutes(-140).Ticks;
                int idleGold = service.ClaimIdleGold();
                Check(idleGold > 0, "Idle collection should return accumulated currency.");
                Check(service.ClaimIdleGold() == 0, "Idle collection must not double-claim elapsed time.");
                Check(service.QuestProgress("idle_1") >= 1, "AFK mission should update.");
                Check(service.WeeklyQuestProgress("idle_minutes_180") >= 100,
                    "Weekly AFK mission should track minutes.");

                Check(service.Save(), "Failed to save progress.");
                var restored = new PlayerProgressService(folder);
                Check(restored.Data.lastClearedStage == 10, "Stage progression did not reload.");
                Check(restored.FindOwnedHero("sobek").level == 10, "Hero level did not reload.");
                Check(restored.FindOwnedHero("sobek").weaponLevel == 1, "Gear progression did not reload.");
                Check(restored.FindOwnedHero("sobek").skin == "ascendant",
                    "Equipped skin did not reload.");
                Check(restored.Data.selectedTeam.Count == 5, "Formation did not reload.");
                Check(restored.Data.stageRewardsClaimed.Count == 10, "Cleared stage log did not reload.");
                Check(restored.Data.equipment.Count == 10, "Ten unique gear rewards did not reload.");
                Check(restored.Data.runes.Count == 2 &&
                    restored.Data.runes[0].assignedHeroId == "sobek",
                    "Equipped rune did not persist.");
                Check(restored.FindOwnedHero("sobek").activeSkillLevel == 2,
                    "Upgraded skills did not persist.");
                Check(restored.FindOwnedHero("sobek").signatureWeaponRank == 1,
                    "Signature weapon rank did not persist.");
                Check(restored.Data.relics.Count == 1 &&
                    restored.Data.relics[0].level == 2,
                    "Chapter relic level did not persist.");

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
                mono[2] = WorldsCatalog.FindHero("amun");
                Check(HeroElements.Calculate(mono).matchingCount == 3,
                    "Light wildcard should complete the strongest core element.");
                mono[3] = WorldsCatalog.FindHero("apep");
                ElementalTeamBonus pair = HeroElements.Calculate(mono);
                Check(pair.lightCount == 1 && pair.darkCount == 1 &&
                    pair.startingEnergy > 0f, "Light/Dark pairing must grant Equinox energy.");
                int rCount = 0, srCount = 0, ssrCount = 0, urCount = 0, exCount = 0;
                int light = 0, dark = 0;
                var urPairByRealm = new System.Collections.Generic.Dictionary<Origin, int[]>();
                foreach (HeroDefinition hero in WorldsCatalog.Heroes)
                {
                    switch (hero.rarity)
                    {
                        case Rarity.R: rCount++; break;
                        case Rarity.SR: srCount++; break;
                        case Rarity.SSR: ssrCount++; break;
                        case Rarity.UR: urCount++; break;
                        case Rarity.EX: exCount++; break;
                    }
                    if (hero.element == HeroElement.Light) light++;
                    if (hero.element == HeroElement.Darkness) dark++;
                    Check(HeroElements.IsSpecial(hero) == (hero.rarity == Rarity.UR),
                        "Light/Dark must be reserved for UR heroes.");
                    if (hero.rarity == Rarity.UR)
                    {
                        int[] count;
                        if (!urPairByRealm.TryGetValue(hero.origin, out count))
                        {
                            count = new int[2];
                            urPairByRealm.Add(hero.origin, count);
                        }
                        if (hero.element == HeroElement.Light) count[0]++;
                        else if (hero.element == HeroElement.Darkness) count[1]++;
                    }
                }
                // No global UR cap (owner decision); EX remains locked.
                Check(exCount == 0 && urCount >= 16 && urPairByRealm.Count >= 5 &&
                    rCount + srCount + ssrCount + urCount == WorldsCatalog.Heroes.Length,
                    "Rarity ladder invalid: EX must stay locked and released UR heroes must remain.");
                foreach (var realm in urPairByRealm)
                    Check(realm.Value[0] >= 1 && realm.Value[1] >= 1,
                        "Each UR mythology must have at least one Light and one Dark.");
                Check(WorldsCatalog.Heroes.Length == HeroRosterData.HeroCount &&
                    HeroAbilityCatalog.All.Length == WorldsCatalog.Heroes.Length,
                    "Every character must have a skill kit.");
                foreach (HeroDefinition hero in WorldsCatalog.Heroes)
                    Check(HeroAbilityCatalog.For(hero.id) != null,
                        "Missing skill kit: " + hero.id);
                // Every promoted hero must preserve its original persistent ID.
                string[] promotedIds = { "ra", "hades", "odin", "hel", "amaterasu" };
                foreach (string id in promotedIds)
                    Check(WorldsCatalog.FindHero(id) != null &&
                        WorldsCatalog.FindHero(id).rarity == Rarity.UR,
                        "Promoted myth hero must retain ID and new UR rank: " + id);
                Check(WorldsCatalog.FindHero("apollo") != null &&
                    HeroAbilityCatalog.For("apollo") != null,
                    "Apollo's additional UR hero kit must exist.");
                Check(URCardCatalog.Count == 16, "Every UR hero needs a premium visual concept.");
                foreach (HeroDefinition hero in WorldsCatalog.Heroes)
                {
                    URCardIdentity card = URCardCatalog.For(hero.id);
                    Check((card != null) == (hero.rarity == Rarity.UR),
                        "Premium card theme must be available exactly for UR heroes: " + hero.id);
                    if (card != null)
                        Check(card.element == hero.element &&
                            !string.IsNullOrEmpty(card.introCue),
                            "UR entrance theme or element mismatch: " + hero.id);
                }

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
