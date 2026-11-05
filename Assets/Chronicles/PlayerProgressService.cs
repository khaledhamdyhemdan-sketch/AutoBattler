using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MythicChronicles
{
    [Serializable]
    public sealed class OwnedHero
    {
        public string id;
        public int level = 1;
        public int stars = 1;
        public int weaponLevel, armorLevel;
        public int activeSkillLevel = 1, ultimateSkillLevel = 1, passiveSkillLevel = 1;
        public int attackTalents, defenseTalents;
        public int signatureWeaponRank;
        public int fragments;
        public string skin = "default";
    }

    [Serializable]
    public sealed class RelicRank { public string id; public int level = 1; }

    [Serializable]
    public sealed class QuestCounter { public string id; public int progress; }

    [Serializable]
    public sealed class PlayerSave
    {
        public int version = 1;
        public string createdUtc;
        public string lastDailyUtc;
        public string lastWeeklyUtc;
        public long lastIdleClaimTicks;
        public int gold = 2500, gems = 350, summonTickets = 3, heroFragments, forgeDust, relicDust, nextItemSerial = 1;
        public int lastClearedStage;
        public int astralSeals;
        public int standardPity, astralPity;
        public int loginDaysClaimed;
        public string lastLoginGiftUtc = "";
        public List<SummonHistoryRecord> summonHistory = new List<SummonHistoryRecord>();
        public float musicVolume = .8f, effectsVolume = .9f;
        public bool reducePortraitMotion;
        public List<OwnedHero> heroes = new List<OwnedHero>();
        public List<string> selectedTeam = new List<string>();
        public List<int> stageRewardsClaimed = new List<int>();
        public List<int> eliteClears = new List<int>();
        public List<int> darkClears = new List<int>();
        public List<TowerProgressRecord> towerProgress = new List<TowerProgressRecord>();
        public string raidUtcDay = "";
        public string raidPendingRunId = "";
        public int raidAttemptsUsed, raidBestDamage, raidRewardTier;
        public List<QuestCounter> quests = new List<QuestCounter>();
        public List<QuestCounter> weeklyQuests = new List<QuestCounter>();
        public List<string> claimedDailyQuests = new List<string>();
        public List<string> claimedWeeklyQuests = new List<string>();
        public List<string> ownedSkins = new List<string>();
        public List<GearInstance> equipment = new List<GearInstance>();
        public List<RuneInstance> runes = new List<RuneInstance>();
        public List<RelicRank> relics = new List<RelicRank>();
        public string equippedRelicId = "";
    }

    public sealed class QuestInfo
    {
        public string id, title;
        public int target, goldReward, gemReward;
        public QuestInfo(string id, string title, int target, int goldReward, int gemReward)
        { this.id = id; this.title = title; this.target = target; this.goldReward = goldReward; this.gemReward = gemReward; }
    }

    // Local/offline save for campaign mode only. Online purchases/PvP require server-authoritative progression.
    public sealed class PlayerProgressService
    {
        public static readonly QuestInfo[] DailyQuests =
        {
            new QuestInfo("win_1", "Win 1 battle", 1, 150, 15),
            new QuestInfo("win_3", "Win 3 battles", 3, 350, 30),
            new QuestInfo("upgrade_1", "Upgrade any hero", 1, 180, 15),
            new QuestInfo("summon_1", "Summon a hero", 1, 180, 15),
            new QuestInfo("idle_1", "Collect idle earnings", 1, 120, 10)
        };
        public static readonly QuestInfo[] WeeklyQuests =
        {
            new QuestInfo("win_10", "Win 10 campaign battles", 10, 1000, 90),
            new QuestInfo("upgrade_7", "Upgrade heroes or gear 7 times", 7, 850, 65),
            new QuestInfo("summon_5", "Summon 5 times", 5, 650, 100),
            new QuestInfo("boss_3", "Defeat 3 chapter bosses", 3, 1000, 90),
            new QuestInfo("idle_minutes_180", "Claim 180 minutes of AFK rewards", 180, 550, 55)
        };
        public PlayerSave Data { get; private set; }
        public string LastError { get; private set; }
        private readonly string savePath;
        private const int IdleCapMinutes = 720; // 12 h
        private const int IdleGoldPerMinute = 4;
        private const int IdleGoldPerStagePerMinute = 1;

        public PlayerProgressService(string root = null)
        {
            string directory = string.IsNullOrEmpty(root) ? Application.persistentDataPath : root;
            savePath = Path.Combine(directory, "chronicles_progress_v1.json");
            Load();
        }

        private void Load()
        {
            Data = ReadSave(savePath);
            if (Data == null) Data = ReadSave(savePath + ".bak");
            if (Data == null)
            {
                Data = new PlayerSave();
                Data.createdUtc = DateTime.UtcNow.ToString("o");
                Data.lastIdleClaimTicks = DateTime.UtcNow.Ticks;
                foreach (var id in WorldsCatalog.StartingHeroes) Data.heroes.Add(new OwnedHero { id = id });
                Data.selectedTeam.AddRange(WorldsCatalog.StartingHeroes);
                Save();
            }
            Repair();
            ResetDailyIfNeeded();
            ResetWeeklyIfNeeded();
        }

        private PlayerSave ReadSave(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                if (string.IsNullOrEmpty(json) || !json.Contains("lastIdleClaimTicks")) return null;
                return JsonUtility.FromJson<PlayerSave>(json);
            }
            catch (Exception e) { LastError = e.Message; return null; }
        }

        private void Repair()
        {
            if (Data.heroes == null) Data.heroes = new List<OwnedHero>();
            if (Data.selectedTeam == null) Data.selectedTeam = new List<string>();
            if (Data.stageRewardsClaimed == null) Data.stageRewardsClaimed = new List<int>();
            if (Data.eliteClears == null) Data.eliteClears = new List<int>();
            if (Data.darkClears == null) Data.darkClears = new List<int>();
            if (Data.towerProgress == null)
                Data.towerProgress = new List<TowerProgressRecord>();
            if (Data.raidUtcDay == null) Data.raidUtcDay = "";
            if (Data.raidPendingRunId == null) Data.raidPendingRunId = "";
            Data.raidAttemptsUsed = Mathf.Clamp(Data.raidAttemptsUsed, 0,
                ChroniclesRaids.AttemptsPerDay);
            Data.raidBestDamage = Math.Max(0, Data.raidBestDamage);
            Data.raidRewardTier = Mathf.Clamp(Data.raidRewardTier, 0, 5);
            var seenTowers = new HashSet<string>(StringComparer.Ordinal);
            Data.towerProgress.RemoveAll(t => t == null ||
                string.IsNullOrEmpty(t.origin) ||
                !seenTowers.Add(t.origin) ||
                !Array.Exists(ChroniclesTowers.TowerOrigins,
                    origin => origin.ToString() == t.origin));
            foreach (TowerProgressRecord tower in Data.towerProgress)
                tower.highestFloor = Mathf.Clamp(tower.highestFloor, 0, ChroniclesTowers.MaxFloor);
            var seenElite = new HashSet<int>();
            Data.eliteClears.RemoveAll(id => id < 1 || id > 60 || !seenElite.Add(id));
            var seenDark = new HashSet<int>();
            Data.darkClears.RemoveAll(id => id < 1 || id > 60 ||
                !seenDark.Add(id) || !Data.eliteClears.Contains(id));
            if (Data.quests == null) Data.quests = new List<QuestCounter>();
            if (Data.weeklyQuests == null) Data.weeklyQuests = new List<QuestCounter>();
            if (Data.claimedDailyQuests == null) Data.claimedDailyQuests = new List<string>();
            if (Data.claimedWeeklyQuests == null) Data.claimedWeeklyQuests = new List<string>();
            if (Data.ownedSkins == null) Data.ownedSkins = new List<string>();
            if (Data.equipment == null) Data.equipment = new List<GearInstance>();
            if (Data.runes == null) Data.runes = new List<RuneInstance>();
            if (Data.relics == null) Data.relics = new List<RelicRank>();
            if (Data.summonHistory == null)
                Data.summonHistory = new List<SummonHistoryRecord>();
            Data.gold = Math.Max(0, Data.gold);
            Data.gems = Math.Max(0, Data.gems);
            Data.summonTickets = Math.Max(0, Data.summonTickets);
            Data.heroFragments = Math.Max(0, Data.heroFragments);
            Data.forgeDust = Math.Max(0, Data.forgeDust);
            Data.relicDust = Math.Max(0, Data.relicDust);
            Data.astralSeals = Math.Max(0, Data.astralSeals);
            Data.loginDaysClaimed = Mathf.Clamp(Data.loginDaysClaimed, 0,
                ChroniclesLoginRewards.Days);
            if (Data.lastLoginGiftUtc == null) Data.lastLoginGiftUtc = "";
            Data.standardPity = Mathf.Clamp(Data.standardPity, 0,
                ChroniclesSummonRules.StandardSsrPity - 1);
            Data.astralPity = Mathf.Clamp(Data.astralPity, 0,
                ChroniclesSummonRules.AstralUrPity - 1);
            Data.summonHistory.RemoveAll(h => h == null ||
                WorldsCatalog.FindHero(h.heroId) == null ||
                (h.banner != "Standard" && h.banner != "AstralCovenant"));
            if (Data.summonHistory.Count > ChroniclesSummonRules.HistoryLimit)
                Data.summonHistory.RemoveRange(0,
                    Data.summonHistory.Count - ChroniclesSummonRules.HistoryLimit);
            Data.nextItemSerial = Math.Max(1, Data.nextItemSerial);
            Data.lastClearedStage = Mathf.Clamp(Data.lastClearedStage, 0, 60);
            Data.musicVolume = Mathf.Clamp01(Data.musicVolume);
            Data.effectsVolume = Mathf.Clamp01(Data.effectsVolume);
            if (Data.lastIdleClaimTicks <= 0 || Data.lastIdleClaimTicks > DateTime.UtcNow.Ticks)
                Data.lastIdleClaimTicks = DateTime.UtcNow.Ticks;
            Data.heroes.RemoveAll(h => h == null || WorldsCatalog.FindHero(h.id) == null);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            Data.heroes.RemoveAll(h => !seen.Add(h.id));
            foreach (var h in Data.heroes) { h.level = Mathf.Clamp(h.level, 1, 200); h.stars = Mathf.Clamp(h.stars, 1, 6); h.weaponLevel = Mathf.Clamp(h.weaponLevel, 0, 30); h.armorLevel = Mathf.Clamp(h.armorLevel, 0, 30); if (HeroSkinCatalog.Find(h.id, h.skin) == null) h.skin = "default";
                h.activeSkillLevel = Mathf.Clamp(h.activeSkillLevel, 1, ChroniclesGrowth.SkillCap);
                h.ultimateSkillLevel = Mathf.Clamp(h.ultimateSkillLevel, 1, ChroniclesGrowth.SkillCap);
                h.passiveSkillLevel = Mathf.Clamp(h.passiveSkillLevel, 1, ChroniclesGrowth.SkillCap);
                h.attackTalents = Mathf.Clamp(h.attackTalents, 0, ChroniclesGrowth.MaxTalents);
                h.defenseTalents = Mathf.Clamp(h.defenseTalents, 0, ChroniclesGrowth.MaxTalents);
                h.signatureWeaponRank = Mathf.Clamp(h.signatureWeaponRank, 0, 5);
                if (h.attackTalents + h.defenseTalents > ChroniclesGrowth.MaxTalents)
                    h.defenseTalents = ChroniclesGrowth.MaxTalents - h.attackTalents;
            }
            var itemIds = new HashSet<string>(StringComparer.Ordinal);
            var occupiedSlots = new HashSet<string>(StringComparer.Ordinal);
            Data.equipment.RemoveAll(item => item == null ||
                string.IsNullOrEmpty(item.instanceId) || !itemIds.Add(item.instanceId) ||
                ChroniclesEquipment.Find(item.templateId) == null);
            foreach (GearInstance item in Data.equipment)
            {
                item.level = Mathf.Clamp(item.level, 0, 30);
                GearTemplate definition = ChroniclesEquipment.Find(item.templateId);
                if (!OwnsHero(item.equippedHeroId) ||
                    !occupiedSlots.Add(item.equippedHeroId + ":" + definition.slot))
                    item.equippedHeroId = "";
            }
            itemIds.Clear();
            var runeSlots = new HashSet<string>(StringComparer.Ordinal);
            Data.runes.RemoveAll(item => item == null ||
                string.IsNullOrEmpty(item.instanceId) || !itemIds.Add(item.instanceId));
            foreach (RuneInstance rune in Data.runes)
            {
                rune.level = Mathf.Clamp(rune.level, 1, 20);
                if (!OwnsHero(rune.assignedHeroId) ||
                    !runeSlots.Add(rune.assignedHeroId + ":" + rune.type))
                    rune.assignedHeroId = "";
            }
            var uniqueRelics = new HashSet<string>(StringComparer.Ordinal);
            Data.relics.RemoveAll(item => item == null ||
                ChroniclesRelics.Find(item.id) == null || !uniqueRelics.Add(item.id));
            foreach (RelicRank owned in Data.relics)
                owned.level = Mathf.Clamp(owned.level, 1, 5);
            if (!Data.relics.Exists(item => item.id == Data.equippedRelicId))
                Data.equippedRelicId = "";
            seen.Clear();
            Data.selectedTeam.RemoveAll(id => !OwnsHero(id) || !seen.Add(id));
            if (Data.selectedTeam.Count > 5) Data.selectedTeam.RemoveRange(5, Data.selectedTeam.Count - 5);
            Save();
        }

        public bool Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                string temp = savePath + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(Data, true));
                if (File.Exists(savePath))
                {
                    File.Copy(savePath, savePath + ".bak", true);
                    File.Delete(savePath);
                }
                File.Move(temp, savePath);
                LastError = null;
                return true;
            }
            catch (Exception e)
            {
                LastError = e.Message;
                Debug.LogError("[Chronicles] Save failed: " + e.Message);
                return false;
            }
        }

        private void ResetDailyIfNeeded()
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (Data.lastDailyUtc == today) return;
            Data.lastDailyUtc = today;
            Data.quests.Clear();
            Data.claimedDailyQuests.Clear();
            Save();
        }

        private void IncrementQuest(string id, int amount = 1)
        {
            ResetDailyIfNeeded();
            QuestCounter counter = Data.quests.Find(q => q.id == id);
            if (counter == null) { counter = new QuestCounter { id = id }; Data.quests.Add(counter); }
            counter.progress = Mathf.Clamp(counter.progress + amount, 0, 100000);
        }

        public int QuestProgress(string id)
        {
            ResetDailyIfNeeded();
            QuestCounter q = Data.quests.Find(x => x.id == id);
            return q == null ? 0 : q.progress;
        }

        public bool ClaimDailyQuest(string id)
        {
            ResetDailyIfNeeded();
            QuestInfo definition = Array.Find(DailyQuests, q => q.id == id);
            if (definition == null || Data.claimedDailyQuests.Contains(id) || QuestProgress(id) < definition.target) return false;
            Data.claimedDailyQuests.Add(id);
            Data.gold += definition.goldReward;
            Data.gems += definition.gemReward;
            Save();
            return true;
        }

        private void ResetWeeklyIfNeeded()
        {
            DateTime today = DateTime.UtcNow.Date;
            int daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
            string week = today.AddDays(-daysSinceMonday).ToString("yyyy-MM-dd");
            if (Data.lastWeeklyUtc == week) return;
            Data.lastWeeklyUtc = week;
            Data.weeklyQuests.Clear();
            Data.claimedWeeklyQuests.Clear();
            Save();
        }

        private void IncrementWeekly(string id, int amount = 1)
        {
            ResetWeeklyIfNeeded();
            QuestCounter counter = Data.weeklyQuests.Find(q => q.id == id);
            if (counter == null)
            {
                counter = new QuestCounter { id = id };
                Data.weeklyQuests.Add(counter);
            }
            counter.progress = Mathf.Clamp(counter.progress + amount, 0, 100000);
        }

        public int WeeklyQuestProgress(string id)
        {
            ResetWeeklyIfNeeded();
            QuestCounter counter = Data.weeklyQuests.Find(q => q.id == id);
            return counter == null ? 0 : counter.progress;
        }

        public bool ClaimWeeklyQuest(string id)
        {
            ResetWeeklyIfNeeded();
            QuestInfo definition = Array.Find(WeeklyQuests, q => q.id == id);
            if (definition == null || Data.claimedWeeklyQuests.Contains(id) ||
                WeeklyQuestProgress(id) < definition.target) return false;
            Data.claimedWeeklyQuests.Add(id);
            Data.gold += definition.goldReward;
            Data.gems += definition.gemReward;
            Save();
            return true;
        }

        public bool OwnsSkin(string heroId, string skinId)
        {
            if (skinId == "default") return OwnsHero(heroId);
            HeroSkin skin = HeroSkinCatalog.Find(heroId, skinId);
            return skin != null && OwnsHero(heroId) &&
                Data.ownedSkins.Contains(skin.OwnershipKey);
        }

        public bool UnlockSkin(string heroId, string skinId)
        {
            HeroSkin skin = HeroSkinCatalog.Find(heroId, skinId);
            if (skin == null || !OwnsHero(heroId) || OwnsSkin(heroId, skinId) ||
                Data.gems < skin.gemCost || skin.gemCost <= 0) return false;
            Data.gems -= skin.gemCost;
            Data.ownedSkins.Add(skin.OwnershipKey);
            return Save();
        }

        public bool EquipSkin(string heroId, string skinId)
        {
            OwnedHero hero = FindOwnedHero(heroId);
            if (hero == null || !OwnsSkin(heroId, skinId)) return false;
            hero.skin = skinId;
            return Save();
        }

        public bool SetReducedPortraitMotion(bool reduce)
        {
            Data.reducePortraitMotion = reduce;
            return Save();
        }

        public bool SetAudioLevels(float music, float effects)
        {
            Data.musicVolume = Mathf.Clamp01(music);
            Data.effectsVolume = Mathf.Clamp01(effects);
            return Save();
        }

        public bool OwnsHero(string id) { return Data.heroes.Exists(h => h.id == id); }
        public OwnedHero FindOwnedHero(string id) { return Data.heroes.Find(h => h.id == id); }

        public bool SetTeam(IList<string> heroIds)
        {
            if (heroIds == null || heroIds.Count != 5) return false;
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in heroIds)
                if (!OwnsHero(id) || !unique.Add(id)) return false;
            Data.selectedTeam = new List<string>(heroIds);
            return Save();
        }

        public bool UpgradeHero(string id)
        {
            OwnedHero hero = FindOwnedHero(id);
            if (hero == null || hero.level >= ChroniclesGrowth.LevelCap) return false;
            int cost = ChroniclesGrowth.LevelCost(hero.level);
            if (Data.gold < cost) return false;
            Data.gold -= cost;
            hero.level++;
            IncrementQuest("upgrade_1");
            IncrementWeekly("upgrade_7");
            Save();
            return true;
        }

        public int ResonanceLevel() { return ChroniclesGrowth.ResonanceLevel(Data); }

        public bool ResetHeroLevel(string id)
        {
            OwnedHero hero = FindOwnedHero(id);
            if (hero == null || hero.level <= 1) return false;
            int refunded = ChroniclesGrowth.RefundForLevelReset(hero.level);
            hero.level = 1; // Keeps stars, cosmetics, equipment and skill investment.
            Data.gold += refunded;
            return Save();
        }

        public bool UpgradeSkill(string id, string kind)
        {
            OwnedHero hero = FindOwnedHero(id);
            if (hero == null) return false;
            int previous;
            switch (kind)
            {
                case "active": previous = hero.activeSkillLevel; break;
                case "ultimate": previous = hero.ultimateSkillLevel; break;
                case "passive": previous = hero.passiveSkillLevel; break;
                default: return false;
            }
            if (previous >= ChroniclesGrowth.SkillCap ||
                previous >= hero.stars + 4) return false;
            int cost = ChroniclesGrowth.SkillCost(previous);
            if (Data.gold < cost) return false;
            Data.gold -= cost;
            switch (kind)
            {
                case "active": hero.activeSkillLevel++; break;
                case "ultimate": hero.ultimateSkillLevel++; break;
                case "passive": hero.passiveSkillLevel++; break;
            }
            IncrementQuest("upgrade_1");
            IncrementWeekly("upgrade_7");
            return Save();
        }

        public bool BuyTalent(string id, bool offensive)
        {
            OwnedHero hero = FindOwnedHero(id);
            if (hero == null) return false;
            int count = hero.attackTalents + hero.defenseTalents;
            int allowed = Mathf.Min(ChroniclesGrowth.MaxTalents,
                hero.level / 10 + hero.stars);
            int cost = ChroniclesGrowth.TalentCost(count);
            if (count >= allowed || Data.gold < cost) return false;
            Data.gold -= cost;
            if (offensive) hero.attackTalents++;
            else hero.defenseTalents++;
            IncrementQuest("upgrade_1");
            IncrementWeekly("upgrade_7");
            return Save();
        }

        public bool ResetTalents(string id)
        {
            OwnedHero hero = FindOwnedHero(id);
            if (hero == null) return false;
            int total = hero.attackTalents + hero.defenseTalents;
            if (total == 0) return false;
            long refund = 0;
            for (int i = 0; i < total; i++)
                refund += ChroniclesGrowth.TalentCost(i);
            if (refund > int.MaxValue - Data.gold) return false;
            hero.attackTalents = 0; hero.defenseTalents = 0;
            Data.gold += (int)refund;
            return Save();
        }

        public bool EquipGear(string instanceId, string heroId)
        {
            if (!OwnsHero(heroId)) return false;
            GearInstance chosen = Data.equipment.Find(x => x.instanceId == instanceId);
            if (chosen == null) return false;
            GearTemplate definition = ChroniclesEquipment.Find(chosen.templateId);
            if (definition == null) return false;
            foreach (GearInstance item in Data.equipment)
            {
                if (item == chosen || item.equippedHeroId != heroId) continue;
                GearTemplate existing = ChroniclesEquipment.Find(item.templateId);
                if (existing != null && existing.slot == definition.slot)
                    item.equippedHeroId = "";
            }
            chosen.equippedHeroId = heroId;
            return Save();
        }

        public bool UnequipGear(string instanceId)
        {
            GearInstance item = Data.equipment.Find(x => x.instanceId == instanceId);
            if (item == null || string.IsNullOrEmpty(item.equippedHeroId)) return false;
            item.equippedHeroId = "";
            return Save();
        }

        public bool UpgradeGear(string instanceId)
        {
            GearInstance item = Data.equipment.Find(x => x.instanceId == instanceId);
            if (item == null || item.level >= 30) return false;
            int cost = ChroniclesEquipment.UpgradeCost(item);
            if (cost < 1 || Data.gold < cost) return false;
            Data.gold -= cost;
            item.level++;
            IncrementQuest("upgrade_1");
            IncrementWeekly("upgrade_7");
            return Save();
        }

        public EquipmentBonuses GearBonus(string heroId)
        {
            return ChroniclesEquipment.Calculate(Data.equipment, heroId);
        }

        public bool UpgradeSignatureWeapon(string heroId)
        {
            OwnedHero hero = FindOwnedHero(heroId);
            if (hero == null || hero.level < 10 || hero.signatureWeaponRank >= 5) return false;
            int goldCost = 850 + hero.signatureWeaponRank * 700;
            int fragmentCost = hero.signatureWeaponRank * 10;
            if (Data.gold < goldCost || hero.fragments < fragmentCost) return false;
            Data.gold -= goldCost;
            hero.fragments -= fragmentCost;
            hero.signatureWeaponRank++;
            IncrementQuest("upgrade_1");
            IncrementWeekly("upgrade_7");
            return Save();
        }

        public bool EquipRelic(string id)
        {
            if (ChroniclesRelics.Find(id) == null ||
                !Data.relics.Exists(relic => relic.id == id)) return false;
            Data.equippedRelicId = id;
            return Save();
        }

        public bool UpgradeRelic(string id)
        {
            RelicRank rank = Data.relics.Find(x => x.id == id);
            if (rank == null || rank.level >= 5) return false;
            int dustCost = rank.level * 10;
            int goldCost = rank.level * 450;
            if (Data.relicDust < dustCost || Data.gold < goldCost) return false;
            Data.relicDust -= dustCost;
            Data.gold -= goldCost;
            rank.level++;
            IncrementQuest("upgrade_1");
            IncrementWeekly("upgrade_7");
            return Save();
        }

        public bool DismantleGear(string instanceId)
        {
            GearInstance item = Data.equipment.Find(x => x.instanceId == instanceId);
            if (item == null || !string.IsNullOrEmpty(item.equippedHeroId)) return false;
            GearTemplate definition = ChroniclesEquipment.Find(item.templateId);
            if (definition == null) return false;
            int dust = 5 + ((int)definition.rarity + 1) * 5 + item.level * 2;
            if (Data.forgeDust > int.MaxValue - dust) return false;
            Data.equipment.Remove(item);
            Data.forgeDust += dust;
            return Save();
        }

        public bool CraftGear(string templateId)
        {
            GearTemplate template = ChroniclesEquipment.Find(templateId);
            if (template == null) return false;
            int dust = 20 + (int)template.rarity * 35;
            int price = 200 + (int)template.rarity * 300;
            if (Data.forgeDust < dust || Data.gold < price ||
                Data.nextItemSerial == int.MaxValue) return false;
            Data.forgeDust -= dust;
            Data.gold -= price;
            Data.equipment.Add(new GearInstance
            {
                instanceId = "crafted_" + Data.nextItemSerial++,
                templateId = template.id, equippedHeroId = ""
            });
            return Save();
        }

        public bool AutoEquip(string heroId)
        {
            if (!OwnsHero(heroId)) return false;
            bool changed = false;
            foreach (GearSlot slot in Enum.GetValues(typeof(GearSlot)))
            {
                GearInstance best = null;
                float score = -1f;
                foreach (GearInstance item in Data.equipment)
                {
                    if (!string.IsNullOrEmpty(item.equippedHeroId) && item.equippedHeroId != heroId)
                        continue; // never steal gear from another hero
                    GearTemplate definition = ChroniclesEquipment.Find(item.templateId);
                    if (definition == null || definition.slot != slot) continue;
                    float rating = definition.attack * 4f + definition.health * .1f +
                        definition.defense * 3f + item.level * 12f;
                    if (rating > score) { score = rating; best = item; }
                }
                if (best == null) continue;
                foreach (GearInstance item in Data.equipment)
                {
                    if (item == best || item.equippedHeroId != heroId) continue;
                    GearTemplate other = ChroniclesEquipment.Find(item.templateId);
                    if (other != null && other.slot == slot)
                    {
                        item.equippedHeroId = "";
                        changed = true;
                    }
                }
                if (best.equippedHeroId != heroId)
                {
                    best.equippedHeroId = heroId;
                    changed = true;
                }
            }
            return !changed || Save();
        }

        public bool EquipRune(string instanceId, string heroId)
        {
            if (!OwnsHero(heroId)) return false;
            RuneInstance chosen = Data.runes.Find(x => x.instanceId == instanceId);
            if (chosen == null) return false;
            // One rune of each type per hero; swap old ownership cleanly.
            foreach (RuneInstance rune in Data.runes)
                if (rune != chosen && rune.assignedHeroId == heroId &&
                    rune.type == chosen.type) rune.assignedHeroId = "";
            chosen.assignedHeroId = heroId;
            return Save();
        }

        public bool UnequipRune(string instanceId)
        {
            RuneInstance item = Data.runes.Find(x => x.instanceId == instanceId);
            if (item == null || string.IsNullOrEmpty(item.assignedHeroId)) return false;
            item.assignedHeroId = "";
            return Save();
        }

        public bool UpgradeRune(string instanceId)
        {
            RuneInstance rune = Data.runes.Find(x => x.instanceId == instanceId);
            if (rune == null || rune.level >= 20) return false;
            int cost = ChroniclesGrowth.RuneUpgradeCost(rune.level);
            if (Data.gold < cost) return false;
            Data.gold -= cost;
            rune.level++;
            IncrementQuest("upgrade_1");
            IncrementWeekly("upgrade_7");
            return Save();
        }

        private void GrantStageEquipment(int id)
        {
            GearTemplate reward = ChroniclesEquipment.RewardForStage(id);
            if (reward != null)
                Data.equipment.Add(new GearInstance
                {
                    instanceId = "stage_gear_" + id,
                    templateId = reward.id,
                    level = 0,
                    equippedHeroId = ""
                });
            // First rune after the fifth stage, more as the player progresses.
            if (id % 5 == 0)
                Data.runes.Add(new RuneInstance
                {
                    instanceId = "stage_rune_" + id,
                    type = (RuneType)((id / 5 - 1) % 4),
                    level = 1,
                    assignedHeroId = ""
                });
        }

        public bool UpgradeEquipment(string id, bool weapon)
        {
            OwnedHero h = FindOwnedHero(id);
            if (h == null) return false;
            int level = weapon ? h.weaponLevel : h.armorLevel;
            if (level >= 30) return false;
            int cost = 125 + level * 75;
            if (Data.gold < cost) return false;
            Data.gold -= cost;
            if (weapon) h.weaponLevel++;
            else h.armorLevel++;
            IncrementQuest("upgrade_1");
            IncrementWeekly("upgrade_7");
            Save();
            return true;
        }

        public bool AscendHero(string id)
        {
            OwnedHero h = FindOwnedHero(id);
            if (h == null || h.stars >= 6) return false;
            int fragmentsNeeded = h.stars * 20;
            int goldNeeded = h.stars * 450;
            if (h.fragments < fragmentsNeeded || Data.gold < goldNeeded) return false;
            h.fragments -= fragmentsNeeded;
            Data.gold -= goldNeeded;
            h.stars++;
            IncrementQuest("upgrade_1");
            IncrementWeekly("upgrade_7");
            Save();
            return true;
        }

        // Free earned-currency summoning only. Epic: 75%, Legendary: 25% of rarity pools.
        // This is not a real-money offering or final commercial gacha balance.
        public int NextLoginRewardDay()
        {
            return Data.loginDaysClaimed >= ChroniclesLoginRewards.Days ?
                1 : Data.loginDaysClaimed + 1;
        }

        public bool CanClaimDailyLogin()
        {
            // Local/offline convenience. Online rewards must use server UTC.
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            return string.IsNullOrEmpty(Data.lastLoginGiftUtc) ||
                string.CompareOrdinal(today, Data.lastLoginGiftUtc) > 0;
        }

        public LoginReward ClaimDailyLogin()
        {
            if (!CanClaimDailyLogin()) return null;
            LoginReward reward = ChroniclesLoginRewards.ForDay(NextLoginRewardDay());
            if (reward == null) return null;
            string before = JsonUtility.ToJson(Data);
            try
            {
                Data.gold = checked(Data.gold + reward.gold);
                Data.gems = checked(Data.gems + reward.gems);
                Data.summonTickets = checked(Data.summonTickets + reward.tickets);
                Data.astralSeals = checked(Data.astralSeals + reward.astralSeals);
                Data.loginDaysClaimed = reward.day;
                Data.lastLoginGiftUtc = DateTime.UtcNow.ToString("yyyy-MM-dd");
                if (!Save()) throw new IOException(LastError ?? "Save failed.");
                return reward;
            }
            catch (Exception ex)
            {
                Data = JsonUtility.FromJson<PlayerSave>(before);
                LastError = ex.Message;
                return null;
            }
        }

        public string SummonHero()
        {
            SummonBatchResult result = DrawHeroes(SummonBannerType.Standard, 1);
            return result.success && result.heroIds.Count > 0 ?
                result.heroIds[0] : null;
        }

        /// <summary>
        /// Offline prototype only: currency and records live in the local save.
        /// Paid pulls/real rankings require server-authoritative transactions.
        /// A batch is deducted once, drawn once and saved once; on save failure
        /// the in-memory data is restored to its original snapshot.
        /// </summary>
        public SummonBatchResult DrawHeroes(SummonBannerType banner, int count)
        {
            var result = new SummonBatchResult();
            if (count != 1 && count != 10)
            {
                result.error = "Choose 1 or 10 summons.";
                return result;
            }
            if (banner != SummonBannerType.Standard &&
                banner != SummonBannerType.AstralCovenant)
            {
                result.error = "Unknown summon banner.";
                return result;
            }
            ResetDailyIfNeeded();
            ResetWeeklyIfNeeded(); // Avoid writing a partially completed batch at a weekly boundary.
            int tickets = banner == SummonBannerType.Standard ?
                Data.summonTickets : Data.astralSeals;
            int free = Math.Min(count, tickets);
            int paid = count - free;
            int gemCost = paid * ChroniclesSummonRules.GemCost(banner);
            if (Data.gems < gemCost)
            {
                result.error = "Not enough tickets, seals or gems.";
                return result;
            }

            string before = JsonUtility.ToJson(Data);
            try
            {
                if (banner == SummonBannerType.Standard)
                    Data.summonTickets -= free;
                else
                    Data.astralSeals -= free;
                Data.gems -= gemCost;

                for (int i = 0; i < count; i++)
                {
                    float roll = UnityEngine.Random.value;
                    int previousPity = banner == SummonBannerType.Standard ?
                        Data.standardPity : Data.astralPity;
                    bool guaranteed = banner == SummonBannerType.Standard ?
                        previousPity >= ChroniclesSummonRules.StandardSsrPity - 1 :
                        previousPity >= ChroniclesSummonRules.AstralUrPity - 1;
                    Rarity tier;
                    if (banner == SummonBannerType.Standard)
                    {
                        tier = guaranteed || roll <
                            ChroniclesSummonRules.StandardSsrRate ? Rarity.SSR :
                            roll < ChroniclesSummonRules.StandardSsrRate +
                            ChroniclesSummonRules.StandardSrRate ? Rarity.SR : Rarity.R;
                        Data.standardPity = tier == Rarity.SSR ?
                            0 : previousPity + 1;
                    }
                    else
                    {
                        tier = guaranteed || roll <
                            ChroniclesSummonRules.AstralUrRate ? Rarity.UR :
                            roll < ChroniclesSummonRules.AstralUrRate +
                            ChroniclesSummonRules.AstralSsrRate ? Rarity.SSR : Rarity.SR;
                        Data.astralPity = tier == Rarity.UR ?
                            0 : previousPity + 1;
                    }

                    List<HeroDefinition> pool = ChroniclesSummonRules.Eligible(banner, tier);
                    if (pool.Count == 0) throw new InvalidOperationException(
                        "No heroes are available for this summon tier.");
                    HeroDefinition hero = pool[UnityEngine.Random.Range(0, pool.Count)];
                    OwnedHero owned = FindOwnedHero(hero.id);
                    if (owned == null)
                        Data.heroes.Add(new OwnedHero { id = hero.id });
                    else
                    {
                        owned.fragments += 10;
                        Data.heroFragments += 10;
                    }
                    result.heroIds.Add(hero.id);
                    Data.summonHistory.Add(new SummonHistoryRecord
                    {
                        heroId = hero.id,
                        banner = banner.ToString(),
                        utc = DateTime.UtcNow.ToString("o"),
                        guaranteed = guaranteed
                    });
                    if (Data.summonHistory.Count > ChroniclesSummonRules.HistoryLimit)
                        Data.summonHistory.RemoveAt(0);
                    IncrementQuest("summon_1");
                    IncrementWeekly("summon_5");
                }
                if (!Save()) throw new IOException(LastError ?? "Save failed.");
                result.success = true;
                return result;
            }
            catch (Exception ex)
            {
                Data = JsonUtility.FromJson<PlayerSave>(before);
                result.heroIds.Clear();
                result.error = "Summon could not be saved: " + ex.Message;
                return result;
            }
        }

        private void RefreshRaidDay()
        {
            string today = ChroniclesRaids.CurrentUtcDay();
            if (Data.raidUtcDay == today) return;
            Data.raidUtcDay = today;
            Data.raidAttemptsUsed = 0;
            Data.raidBestDamage = 0;
            Data.raidRewardTier = 0;
            Data.raidPendingRunId = "";
            Save();
        }

        public int RaidAttemptsRemaining()
        {
            RefreshRaidDay();
            return Math.Max(0, ChroniclesRaids.AttemptsPerDay - Data.raidAttemptsUsed);
        }

        public string BeginRaid()
        {
            RefreshRaidDay();
            if (Data.lastClearedStage < 10 ||
                Data.raidAttemptsUsed >= ChroniclesRaids.AttemptsPerDay ||
                !string.IsNullOrEmpty(Data.raidPendingRunId)) return null;
            string before = JsonUtility.ToJson(Data);
            Data.raidAttemptsUsed++;
            Data.raidPendingRunId = Guid.NewGuid().ToString("N");
            if (Save()) return Data.raidPendingRunId;
            Data = JsonUtility.FromJson<PlayerSave>(before);
            return null;
        }

        public bool CompleteRaid(string runId, int damageScore)
        {
            RefreshRaidDay();
            if (string.IsNullOrEmpty(runId) || runId != Data.raidPendingRunId ||
                Data.lastClearedStage < 10 ||
                Data.raidAttemptsUsed < 1 || damageScore < 0) return false;
            ResetDailyIfNeeded();
            ResetWeeklyIfNeeded();
            string before = JsonUtility.ToJson(Data);
            try
            {
                int score = Math.Min(2000000, damageScore);
                Data.raidBestDamage = Math.Max(Data.raidBestDamage, score);
                int tier = ChroniclesRaids.RewardTier(Data.raidBestDamage);
                int gems = ChroniclesRaids.GemsForTierUpgrade(
                    Data.raidRewardTier, tier);
                int seals = ChroniclesRaids.SealsForTierUpgrade(
                    Data.raidRewardTier, tier);
                Data.gold = checked(Data.gold + ChroniclesRaids.GoldForAttempt(score));
                Data.gems = checked(Data.gems + gems);
                Data.astralSeals = checked(Data.astralSeals + seals);
                Data.raidRewardTier = Math.Max(Data.raidRewardTier, tier);
                Data.raidPendingRunId = ""; // consume even when no new best was reached
                if (score > 0)
                {
                    IncrementQuest("win_1");
                    IncrementQuest("win_3");
                }
                if (!Save()) throw new IOException(LastError ?? "Save failed.");
                return true;
            }
            catch (Exception ex)
            {
                Data = JsonUtility.FromJson<PlayerSave>(before);
                LastError = ex.Message;
                return false;
            }
        }

        public bool AbandonRaid(string runId)
        {
            RefreshRaidDay();
            if (string.IsNullOrEmpty(runId) || runId != Data.raidPendingRunId)
                return false;
            // The attempt is consumed when started. Prevent quitting to reroll.
            Data.raidPendingRunId = "";
            return Save();
        }

        public bool CanEnterTower(Origin origin, int floor, IList<string> team)
        {
            return ChroniclesTowers.CanEnter(Data, origin, floor, team);
        }

        public bool CompleteTower(Origin origin, int floor, IList<string> team)
        {
            if (!CanEnterTower(origin, floor, team)) return false;
            ResetDailyIfNeeded();
            ResetWeeklyIfNeeded();
            string before = JsonUtility.ToJson(Data);
            try
            {
                TrialReward reward = ChroniclesTowers.FloorReward(floor);
                if (reward == null) return false;
                TowerProgressRecord record = Data.towerProgress.Find(x =>
                    x.origin == origin.ToString());
                if (record == null)
                {
                    record = new TowerProgressRecord { origin = origin.ToString() };
                    Data.towerProgress.Add(record);
                }
                record.highestFloor = floor;
                Data.gold = checked(Data.gold + reward.gold);
                Data.gems = checked(Data.gems + reward.gems);
                Data.forgeDust = checked(Data.forgeDust + reward.forgeDust);
                Data.astralSeals = checked(Data.astralSeals + reward.astralSeals);
                IncrementQuest("win_1");
                IncrementQuest("win_3");
                if (floor % 10 == 0) IncrementWeekly("boss_3");
                if (!Save()) throw new IOException(LastError ?? "Save failed.");
                return true;
            }
            catch (Exception ex)
            {
                Data = JsonUtility.FromJson<PlayerSave>(before);
                LastError = ex.Message;
                return false;
            }
        }

        public bool CanPlayTrial(int stageId, TrialMode mode)
        {
            return ChroniclesTrials.CanPlay(Data, stageId, mode);
        }

        public bool CompleteTrial(int stageId, TrialMode mode)
        {
            if (mode == TrialMode.Normal || !CanPlayTrial(stageId, mode))
                return false;
            ResetDailyIfNeeded();
            ResetWeeklyIfNeeded();
            string before = JsonUtility.ToJson(Data);
            try
            {
                IncrementQuest("win_1");
                IncrementQuest("win_3");
                IncrementWeekly("win_10");
                if (stageId % ChroniclesTrials.StagesPerChapter == 0)
                    IncrementWeekly("boss_3");

                List<int> clearLog = mode == TrialMode.Elite ?
                    Data.eliteClears : Data.darkClears;
                if (!clearLog.Contains(stageId))
                {
                    TrialReward reward = ChroniclesTrials.FirstClearReward(stageId, mode);
                    if (reward == null) return false;
                    clearLog.Add(stageId);
                    Data.gold = checked(Data.gold + reward.gold);
                    Data.gems = checked(Data.gems + reward.gems);
                    Data.forgeDust = checked(Data.forgeDust + reward.forgeDust);
                    Data.astralSeals = checked(Data.astralSeals + reward.astralSeals);
                    if (stageId % 5 == 0)
                        Data.runes.Add(new RuneInstance
                        {
                            instanceId = "trial_" + mode + "_rune_" + stageId,
                            type = (RuneType)((stageId / 5 - 1) % 4),
                            level = 1,
                            assignedHeroId = ""
                        });
                }
                if (!Save()) throw new IOException(LastError ?? "Save failed.");
                return true;
            }
            catch (Exception ex)
            {
                Data = JsonUtility.FromJson<PlayerSave>(before);
                LastError = ex.Message;
                return false;
            }
        }

        public bool CompleteStage(int stageId)
        {
            // Replay is allowed, but stage rewards are intentionally one-time.
            if (stageId < 1 || stageId > 60 || stageId > Data.lastClearedStage + 1) return false;
            IncrementQuest("win_1");
            IncrementQuest("win_3");
            IncrementWeekly("win_10");
            if (WorldsCatalog.GetStage(stageId).boss)
                IncrementWeekly("boss_3");
            if (!Data.stageRewardsClaimed.Contains(stageId))
            {
                var stage = WorldsCatalog.GetStage(stageId);
                Data.stageRewardsClaimed.Add(stageId);
                GrantStageEquipment(stageId);
                WorldRelic rewardRelic = ChroniclesRelics.FromBossStage(stageId);
                if (rewardRelic != null && !Data.relics.Exists(x => x.id == rewardRelic.id))
                {
                    Data.relics.Add(new RelicRank { id = rewardRelic.id, level = 1 });
                    Data.relicDust += 15;
                    Data.astralSeals = Math.Min(999999, Data.astralSeals + 2);
                    if (string.IsNullOrEmpty(Data.equippedRelicId))
                        Data.equippedRelicId = rewardRelic.id;
                }
                Data.gold += stage.gold;
                Data.gems += stage.gems;
            }
            if (stageId > Data.lastClearedStage) Data.lastClearedStage = stageId;
            Save();
            return true;
        }

        public int IdleAvailableMinutes()
        {
            long ticks = Math.Max(0, DateTime.UtcNow.Ticks - Data.lastIdleClaimTicks);
            return Mathf.Clamp((int)Math.Min(IdleCapMinutes, TimeSpan.FromTicks(ticks).TotalMinutes), 0, IdleCapMinutes);
        }

        public int IdleProjectedGold()
        {
            return IdleAvailableMinutes() * (IdleGoldPerMinute + Data.lastClearedStage * IdleGoldPerStagePerMinute);
        }

        public int ClaimIdleGold()
        {
            int minutes = IdleAvailableMinutes();
            if (minutes <= 0) return 0;
            int collected = IdleProjectedGold();
            Data.gold += collected;
            // Consume only whole minutes so sub-minute earnings are preserved.
            Data.lastIdleClaimTicks = Math.Min(DateTime.UtcNow.Ticks,
                Data.lastIdleClaimTicks + TimeSpan.FromMinutes(minutes).Ticks);
            IncrementQuest("idle_1");
            IncrementWeekly("idle_minutes_180", minutes);
            Save();
            return collected;
        }
    }
}
