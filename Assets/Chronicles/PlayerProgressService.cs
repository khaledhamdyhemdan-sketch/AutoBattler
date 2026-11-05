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
        public int fragments;
        public string skin = "default";
    }

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
        public int gold = 2500, gems = 350, summonTickets = 3, heroFragments;
        public int lastClearedStage;
        public float musicVolume = .8f, effectsVolume = .9f;
        public List<OwnedHero> heroes = new List<OwnedHero>();
        public List<string> selectedTeam = new List<string>();
        public List<int> stageRewardsClaimed = new List<int>();
        public List<QuestCounter> quests = new List<QuestCounter>();
        public List<QuestCounter> weeklyQuests = new List<QuestCounter>();
        public List<string> claimedDailyQuests = new List<string>();
        public List<string> claimedWeeklyQuests = new List<string>();
        public List<string> ownedSkins = new List<string>();
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
                Data.selectedTeam.AddRange(new[] { "sobek", "anubis", "sekhmet", "isis", "ra" });
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
            if (Data.quests == null) Data.quests = new List<QuestCounter>();
            if (Data.weeklyQuests == null) Data.weeklyQuests = new List<QuestCounter>();
            if (Data.claimedDailyQuests == null) Data.claimedDailyQuests = new List<string>();
            if (Data.claimedWeeklyQuests == null) Data.claimedWeeklyQuests = new List<string>();
            if (Data.ownedSkins == null) Data.ownedSkins = new List<string>();
            Data.gold = Math.Max(0, Data.gold);
            Data.gems = Math.Max(0, Data.gems);
            Data.summonTickets = Math.Max(0, Data.summonTickets);
            Data.heroFragments = Math.Max(0, Data.heroFragments);
            Data.lastClearedStage = Mathf.Clamp(Data.lastClearedStage, 0, 60);
            Data.musicVolume = Mathf.Clamp01(Data.musicVolume);
            Data.effectsVolume = Mathf.Clamp01(Data.effectsVolume);
            if (Data.lastIdleClaimTicks <= 0 || Data.lastIdleClaimTicks > DateTime.UtcNow.Ticks)
                Data.lastIdleClaimTicks = DateTime.UtcNow.Ticks;
            Data.heroes.RemoveAll(h => h == null || WorldsCatalog.FindHero(h.id) == null);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            Data.heroes.RemoveAll(h => !seen.Add(h.id));
            foreach (var h in Data.heroes) { h.level = Mathf.Clamp(h.level, 1, 200); h.stars = Mathf.Clamp(h.stars, 1, 6); h.weaponLevel = Mathf.Clamp(h.weaponLevel, 0, 30); h.armorLevel = Mathf.Clamp(h.armorLevel, 0, 30); if (HeroSkinCatalog.Find(h.id, h.skin) == null) h.skin = "default"; }
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
            if (hero == null || hero.level >= 200) return false;
            int cost = 75 + (hero.level * 35);
            if (Data.gold < cost) return false;
            Data.gold -= cost;
            hero.level++;
            IncrementQuest("upgrade_1");
            IncrementWeekly("upgrade_7");
            Save();
            return true;
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
        public string SummonHero()
        {
            ResetDailyIfNeeded();
            if (Data.summonTickets > 0) Data.summonTickets--;
            else if (Data.gems >= 120) Data.gems -= 120;
            else return null;

            Rarity tier = UnityEngine.Random.value < .25f ? Rarity.Legendary : Rarity.Epic;
            var candidates = new List<HeroDefinition>();
            foreach (var hero in WorldsCatalog.Heroes)
                if (hero.rarity == tier) candidates.Add(hero);
            if (candidates.Count == 0) return null;
            HeroDefinition selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            OwnedHero owned = FindOwnedHero(selected.id);
            if (owned == null) Data.heroes.Add(new OwnedHero { id = selected.id });
            else { owned.fragments += 10; Data.heroFragments += 10; }
            IncrementQuest("summon_1");
            IncrementWeekly("summon_5");
            Save();
            return selected.id;
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
