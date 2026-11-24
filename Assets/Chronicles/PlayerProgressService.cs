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
        public int version = 2;
        public string createdUtc;
        public string lastDailyUtc;
        public string lastWeeklyUtc;
        public long lastIdleClaimTicks;
        public int gold = 2500, gems = 350, summonTickets = 3, heroFragments, forgeDust, relicDust, nextItemSerial = 1;
        public int lastClearedStage;
        public int astralSeals;
        public int standardPity, astralPity;
        public string wishlistHeroId = "";
        public int loginDaysClaimed;
        public string fastBattleUtcDay = "";
        public int fastBattleUsed;
        public int legacyWar, legacyGuard, legacyFortune;
        public List<ElementalGateProgress> elementalGates = new List<ElementalGateProgress>();
        public string gateUtcDay = "";
        public int gateAttemptsUsed;
        public List<EventAdventureProgress> eventAdventures = new List<EventAdventureProgress>();
        public string adventurePendingRunId = "";
        public string bountyUtcDay = "";
        public List<ActiveBounty> activeBounties = new List<ActiveBounty>();
        public List<string> claimedBountyIds = new List<string>();
        public string lastLoginGiftUtc = "";
        public List<SummonHistoryRecord> summonHistory = new List<SummonHistoryRecord>();
        public float musicVolume = .8f, effectsVolume = .9f;
        public bool reducePortraitMotion;
        public int tutorialStep;
        public bool tutorialCompleted;
        public List<OwnedHero> heroes = new List<OwnedHero>();
        public List<string> selectedTeam = new List<string>();
        public List<int> stageRewardsClaimed = new List<int>();
        public List<int> campaignMilestonesClaimed = new List<int>();
        public List<string> claimedAchievements = new List<string>();
        public string shopUtcDay = "";
        public List<ShopPurchaseCounter> shopPurchases = new List<ShopPurchaseCounter>();
        public List<RewardMail> mailbox = new List<RewardMail>();
        public bool welcomeMailGranted;
        public List<int> eliteClears = new List<int>();
        public List<int> darkClears = new List<int>();
        public List<TowerProgressRecord> towerProgress = new List<TowerProgressRecord>();
        public LabyrinthProgress labyrinth = new LabyrinthProgress();
        public ArenaTrainingProgress arenaTraining = new ArenaTrainingProgress();
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
            new QuestInfo("idle_1", "Collect idle earnings", 1, 120, 10),
            new QuestInfo("gates_1", "Clear an Elemental Gate", 1, 220, 20),
            new QuestInfo("adventure_1", "Clear an Event Adventure stage", 1, 220, 20)
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
            if (Data.campaignMilestonesClaimed == null)
                Data.campaignMilestonesClaimed = new List<int>();
            if (Data.claimedAchievements == null)
                Data.claimedAchievements = new List<string>();
            if (Data.shopUtcDay == null) Data.shopUtcDay = "";
            if (Data.shopPurchases == null)
                Data.shopPurchases = new List<ShopPurchaseCounter>();
            var knownOffers = new HashSet<string>(StringComparer.Ordinal);
            Data.shopPurchases.RemoveAll(x => x == null ||
                ChroniclesShop.Find(x.offerId) == null ||
                !knownOffers.Add(x.offerId));
            foreach (ShopPurchaseCounter counter in Data.shopPurchases)
                counter.quantity = Mathf.Clamp(counter.quantity, 0,
                    ChroniclesShop.Find(counter.offerId).dailyLimit);
            if (Data.mailbox == null) Data.mailbox = new List<RewardMail>();
            var knownMail = new HashSet<string>(StringComparer.Ordinal);
            Data.mailbox.RemoveAll(x => x == null || string.IsNullOrEmpty(x.id) ||
                !knownMail.Add(x.id));
            if (!Data.welcomeMailGranted)
            {
                if (!knownMail.Contains("welcome_v1"))
                    Data.mailbox.Add(new RewardMail
                    {
                        id = "welcome_v1", subject = "Welcome to the Chronicles",
                        body = "Begin your journey across the mythic worlds.",
                        createdUtc = DateTime.UtcNow.ToString("o"),
                        gems = 60, tickets = 1
                    });
                Data.welcomeMailGranted = true;
            }
            var uniqueAchievements = new HashSet<string>(StringComparer.Ordinal);
            Data.claimedAchievements.RemoveAll(id =>
                ChroniclesAchievements.Find(id) == null || !uniqueAchievements.Add(id));
            if (Data.eliteClears == null) Data.eliteClears = new List<int>();
            if (Data.darkClears == null) Data.darkClears = new List<int>();
            if (Data.towerProgress == null)
                Data.towerProgress = new List<TowerProgressRecord>();
            if (Data.labyrinth == null) Data.labyrinth = new LabyrinthProgress();
            if (Data.arenaTraining == null)
                Data.arenaTraining = new ArenaTrainingProgress();
            if (Data.arenaTraining.utcDay == null) Data.arenaTraining.utcDay = "";
            if (Data.arenaTraining.pendingMatchToken == null)
                Data.arenaTraining.pendingMatchToken = "";
            Data.arenaTraining.rating = Mathf.Clamp(Data.arenaTraining.rating, 0, 5000);
            Data.arenaTraining.bestRating = Math.Max(Data.arenaTraining.rating,
                Mathf.Clamp(Data.arenaTraining.bestRating, 0, 5000));
            Data.arenaTraining.attemptsToday = Mathf.Clamp(
                Data.arenaTraining.attemptsToday, 0,
                ChroniclesArenaTraining.DailyAttempts);
            Data.arenaTraining.wins = Math.Max(0, Data.arenaTraining.wins);
            Data.arenaTraining.losses = Math.Max(0, Data.arenaTraining.losses);
            if (!string.IsNullOrEmpty(Data.arenaTraining.pendingMatchToken))
            {
                // The game closed mid-match. Consume the already-started attempt.
                Data.arenaTraining.pendingMatchToken = "";
                Data.arenaTraining.losses++;
                Data.arenaTraining.rating = Math.Max(0,
                    Data.arenaTraining.rating - ChroniclesArenaTraining.DefeatRating);
            }
            Data.labyrinth.nextRoom = Mathf.Clamp(Data.labyrinth.nextRoom, 1,
                ChroniclesLabyrinth.Rooms + 1);
            Data.labyrinth.lives = Mathf.Clamp(Data.labyrinth.lives, 0,
                ChroniclesLabyrinth.StartingLives);
            Data.labyrinth.blessingStacks = Mathf.Clamp(Data.labyrinth.blessingStacks, 0,
                ChroniclesLabyrinth.MaxBlessings);
            Data.labyrinth.score = Math.Max(0, Data.labyrinth.score);
            if (Data.labyrinth.pendingBattleToken == null)
                Data.labyrinth.pendingBattleToken = "";
            // An interrupted saved battle counts as a defeat; never award
            // progress when a result callback has not been recorded.
            if (!string.IsNullOrEmpty(Data.labyrinth.pendingBattleToken))
            {
                Data.labyrinth.pendingBattleToken = "";
                Data.labyrinth.pendingChoice = -1;
                if (Data.labyrinth.weekKey == ChroniclesLabyrinth.CurrentWeek())
                    Data.labyrinth.lives = Math.Max(0, Data.labyrinth.lives - 1);
            }
            if (Data.raidUtcDay == null) Data.raidUtcDay = "";
            if (Data.raidPendingRunId == null) Data.raidPendingRunId = "";
            Data.raidAttemptsUsed = Mathf.Clamp(Data.raidAttemptsUsed, 0,
                ChroniclesRaids.AttemptsPerDay);
            Data.raidBestDamage = Math.Max(0, Data.raidBestDamage);
            Data.raidRewardTier = Mathf.Clamp(Data.raidRewardTier, 0, 5);
            // The app was closed during a raid. The attempt was already spent
            // when it began; discard its stale token so other attempts stay usable.
            if (!string.IsNullOrEmpty(Data.raidPendingRunId))
                Data.raidPendingRunId = "";
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
            if (Data.version < 2 && Data.lastClearedStage > 4)
                Data.tutorialCompleted = true; // don't interrupt returning campaign players
            Data.version = 2;
            Data.tutorialStep = Mathf.Clamp(Data.tutorialStep, 0, ChroniclesTutorial.StepCount);
            if (Data.tutorialStep == ChroniclesTutorial.StepCount)
                Data.tutorialCompleted = true;
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

        public bool AdvanceTutorial()
        {
            if (Data.tutorialCompleted) return false;
            string original = JsonUtility.ToJson(Data);
            Data.tutorialStep = Mathf.Min(ChroniclesTutorial.StepCount,
                Data.tutorialStep + 1);
            if (Data.tutorialStep >= ChroniclesTutorial.StepCount)
                Data.tutorialCompleted = true;
            if (Save()) return true;
            Data = JsonUtility.FromJson<PlayerSave>(original);
            return false;
        }

        public bool SkipTutorial()
        {
            string original = JsonUtility.ToJson(Data);
            Data.tutorialStep = ChroniclesTutorial.StepCount;
            Data.tutorialCompleted = true;
            if (Save()) return true;
            Data = JsonUtility.FromJson<PlayerSave>(original);
            return false;
        }

        public bool RestartTutorial()
        {
            string original = JsonUtility.ToJson(Data);
            Data.tutorialStep = 0;
            Data.tutorialCompleted = false;
            if (Save()) return true;
            Data = JsonUtility.FromJson<PlayerSave>(original);
            return false;
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
                        float urRate = ChroniclesSummonRules.EffectiveAstralUrRate(
                            DateTime.UtcNow);
                        tier = guaranteed || roll < urRate ? Rarity.UR :
                            roll < urRate +
                            ChroniclesSummonRules.AstralSsrRate ? Rarity.SSR : Rarity.SR;
                        Data.astralPity = tier == Rarity.UR ?
                            0 : previousPity + 1;
                    }

                    List<HeroDefinition> pool = ChroniclesSummonRules.Eligible(banner, tier);
                    if (pool.Count == 0) throw new InvalidOperationException(
                        "No heroes are available for this summon tier.");
                    HeroDefinition hero = ChroniclesSummonRules.PickFromPool(
                        banner, tier, pool, Data.wishlistHeroId);
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

        private void ResetShopDay()
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (Data.shopUtcDay == today) return;
            Data.shopUtcDay = today;
            Data.shopPurchases.Clear();
            Save();
        }

        public int ShopRemaining(string offerId)
        {
            ResetShopDay();
            ShopOffer offer = ChroniclesShop.Find(offerId);
            if (offer == null) return 0;
            ShopPurchaseCounter used = Data.shopPurchases.Find(
                x => x.offerId == offerId);
            return Mathf.Max(0, offer.dailyLimit - (used == null ? 0 : used.quantity));
        }

        public bool BuyShopOffer(string offerId)
        {
            ShopOffer offer = ChroniclesShop.Find(offerId);
            if (offer == null || ShopRemaining(offerId) <= 0) return false;
            if (offer.currency == ShopCurrency.Gold && Data.gold < offer.cost)
                return false;
            if (offer.currency == ShopCurrency.Gems && Data.gems < offer.cost)
                return false;
            string before = JsonUtility.ToJson(Data);
            try
            {
                if (offer.currency == ShopCurrency.Gold)
                    Data.gold -= offer.cost;
                else if (offer.currency == ShopCurrency.Gems)
                    Data.gems -= offer.cost;
                switch (offer.reward)
                {
                    case ShopReward.Gold:
                        Data.gold = checked(Data.gold + offer.amount); break;
                    case ShopReward.Gems:
                        Data.gems = checked(Data.gems + offer.amount); break;
                    case ShopReward.SummonTicket:
                        Data.summonTickets = checked(Data.summonTickets + offer.amount); break;
                    case ShopReward.AstralSeal:
                        Data.astralSeals = checked(Data.astralSeals + offer.amount); break;
                    case ShopReward.ForgeDust:
                        Data.forgeDust = checked(Data.forgeDust + offer.amount); break;
                    case ShopReward.RelicDust:
                        Data.relicDust = checked(Data.relicDust + offer.amount); break;
                }
                ShopPurchaseCounter used = Data.shopPurchases.Find(
                    x => x.offerId == offer.id);
                if (used == null)
                {
                    used = new ShopPurchaseCounter { offerId = offer.id };
                    Data.shopPurchases.Add(used);
                }
                used.quantity++;
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

        public int UnclaimedMailCount()
        {
            int count = 0;
            if (Data.mailbox == null) return count;
            foreach (RewardMail message in Data.mailbox)
                if (!message.claimed) count++;
            return count;
        }

        public bool ClaimMailReward(string mailId)
        {
            if (string.IsNullOrEmpty(mailId)) return false;
            RewardMail mail = Data.mailbox.Find(
                x => x != null && x.id == mailId);
            if (mail == null || mail.claimed) return false;
            string before = JsonUtility.ToJson(Data);
            try
            {
                Data.gold = checked(Data.gold + mail.gold);
                Data.gems = checked(Data.gems + mail.gems);
                Data.summonTickets = checked(Data.summonTickets + mail.tickets);
                Data.astralSeals = checked(Data.astralSeals + mail.astralSeals);
                Data.forgeDust = checked(Data.forgeDust + mail.forgeDust);
                Data.relicDust = checked(Data.relicDust + mail.relicDust);
                mail.claimed = true;
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

        public bool CanClaimAchievement(string id)
        {
            AchievementDefinition achievement = ChroniclesAchievements.Find(id);
            return achievement != null &&
                !Data.claimedAchievements.Contains(id) &&
                ChroniclesAchievements.Progress(Data, id) >= achievement.target;
        }

        public bool ClaimAchievement(string id)
        {
            if (!CanClaimAchievement(id)) return false;
            AchievementDefinition achievement = ChroniclesAchievements.Find(id);
            string before = JsonUtility.ToJson(Data);
            try
            {
                Data.gold = checked(Data.gold + achievement.gold);
                Data.gems = checked(Data.gems + achievement.gems);
                Data.summonTickets = checked(Data.summonTickets + achievement.tickets);
                Data.astralSeals = checked(Data.astralSeals + achievement.astralSeals);
                Data.claimedAchievements.Add(id);
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

        public bool CanClaimCampaignMilestone(int stage)
        {
            return ChroniclesMilestones.At(stage) != null &&
                Data.lastClearedStage >= stage &&
                !Data.campaignMilestonesClaimed.Contains(stage);
        }

        public bool ClaimCampaignMilestone(int stage)
        {
            if (!CanClaimCampaignMilestone(stage)) return false;
            CampaignMilestoneReward reward = ChroniclesMilestones.At(stage);
            string before = JsonUtility.ToJson(Data);
            try
            {
                Data.gold = checked(Data.gold + reward.gold);
                Data.gems = checked(Data.gems + reward.gems);
                Data.summonTickets = checked(Data.summonTickets + reward.tickets);
                Data.astralSeals = checked(Data.astralSeals + reward.astralSeals);
                Data.forgeDust = checked(Data.forgeDust + reward.forgeDust);
                Data.campaignMilestonesClaimed.Add(stage);
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

        private void RefreshArenaDay()
        {
            string today = ChroniclesArenaTraining.CurrentDay();
            if (Data.arenaTraining != null && Data.arenaTraining.utcDay == today)
                return;
            if (Data.arenaTraining == null)
                Data.arenaTraining = new ArenaTrainingProgress();
            Data.arenaTraining.utcDay = today;
            Data.arenaTraining.attemptsToday = 0;
            Data.arenaTraining.pendingMatchToken = "";
            Save();
        }

        public ArenaTrainingProgress ArenaTrainingState()
        {
            RefreshArenaDay();
            return Data.arenaTraining;
        }

        public int ArenaAttemptsRemaining()
        {
            ArenaTrainingProgress state = ArenaTrainingState();
            return Math.Max(0, ChroniclesArenaTraining.DailyAttempts -
                state.attemptsToday);
        }

        public string BeginArenaTraining()
        {
            if (Data.lastClearedStage < 10 || ArenaAttemptsRemaining() <= 0 ||
                !string.IsNullOrEmpty(Data.arenaTraining.pendingMatchToken))
                return null;
            string before = JsonUtility.ToJson(Data);
            Data.arenaTraining.attemptsToday++;
            Data.arenaTraining.pendingMatchToken = Guid.NewGuid().ToString("N");
            if (Save()) return Data.arenaTraining.pendingMatchToken;
            Data = JsonUtility.FromJson<PlayerSave>(before);
            return null;
        }

        public bool CompleteArenaTraining(string token, bool victory)
        {
            RefreshArenaDay();
            ArenaTrainingProgress arena = Data.arenaTraining;
            if (string.IsNullOrEmpty(token) || token != arena.pendingMatchToken)
                return false;
            string before = JsonUtility.ToJson(Data);
            try
            {
                arena.pendingMatchToken = "";
                arena.rating = Mathf.Clamp(arena.rating +
                    (victory ? ChroniclesArenaTraining.VictoryRating :
                        -ChroniclesArenaTraining.DefeatRating), 0, 5000);
                arena.bestRating = Math.Max(arena.bestRating, arena.rating);
                if (victory) arena.wins++;
                else arena.losses++;
                Data.gold = checked(Data.gold + ChroniclesArenaTraining.Gold(victory));
                Data.gems = checked(Data.gems + ChroniclesArenaTraining.Gems(victory));
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

        public bool AbandonArenaTraining(string token)
        {
            return CompleteArenaTraining(token, false);
        }

        public bool CancelArenaTrainingStart(string token)
        {
            ArenaTrainingProgress arena = ArenaTrainingState();
            if (string.IsNullOrEmpty(token) || token != arena.pendingMatchToken)
                return false;
            string before = JsonUtility.ToJson(Data);
            arena.pendingMatchToken = "";
            arena.attemptsToday = Math.Max(0, arena.attemptsToday - 1);
            if (Save()) return true;
            Data = JsonUtility.FromJson<PlayerSave>(before);
            return false;
        }

        private void RefreshLabyrinthWeek()
        {
            string week = ChroniclesLabyrinth.CurrentWeek();
            if (Data.labyrinth != null && Data.labyrinth.weekKey == week)
                return;
            Data.labyrinth = new LabyrinthProgress { weekKey = week };
            Save();
        }

        public LabyrinthProgress LabyrinthState()
        {
            RefreshLabyrinthWeek();
            return Data.labyrinth;
        }

        public LabyrinthOption LabyrinthChoice(int choice)
        {
            LabyrinthProgress current = LabyrinthState();
            if (Data.lastClearedStage < 10 || current.completed ||
                current.lives <= 0 ||
                !string.IsNullOrEmpty(current.pendingBattleToken))
                return null;
            return ChroniclesLabyrinth.Choice(
                current.weekKey, current.nextRoom, choice);
        }

        public bool ChooseLabyrinthEvent(int choice)
        {
            LabyrinthOption option = LabyrinthChoice(choice);
            if (option == null || option.RequiresBattle) return false;
            string before = JsonUtility.ToJson(Data);
            try
            {
                LabyrinthProgress current = Data.labyrinth;
                if (option.kind == LabyrinthRoomKind.Shrine)
                    current.blessingStacks = Math.Min(
                        ChroniclesLabyrinth.MaxBlessings, current.blessingStacks + 1);
                if (option.kind == LabyrinthRoomKind.Rest)
                    current.lives = Math.Min(
                        ChroniclesLabyrinth.StartingLives, current.lives + 1);
                Data.gold = checked(Data.gold + ChroniclesLabyrinth.RewardGold(option));
                Data.forgeDust = checked(Data.forgeDust +
                    ChroniclesLabyrinth.RewardForgeDust(option));
                AdvanceLabyrinth(option);
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

        public string BeginLabyrinthBattle(int choice)
        {
            LabyrinthOption option = LabyrinthChoice(choice);
            if (option == null || !option.RequiresBattle) return null;
            string before = JsonUtility.ToJson(Data);
            Data.labyrinth.pendingChoice = choice;
            Data.labyrinth.pendingBattleToken = Guid.NewGuid().ToString("N");
            if (Save()) return Data.labyrinth.pendingBattleToken;
            Data = JsonUtility.FromJson<PlayerSave>(before);
            return null;
        }

        public bool CompleteLabyrinthBattle(string token, bool victory)
        {
            RefreshLabyrinthWeek();
            LabyrinthProgress current = Data.labyrinth;
            if (string.IsNullOrEmpty(token) || token != current.pendingBattleToken)
                return false;
            LabyrinthOption option = ChroniclesLabyrinth.Choice(
                current.weekKey, current.nextRoom, current.pendingChoice);
            if (option == null || !option.RequiresBattle) return false;
            ResetDailyIfNeeded();
            ResetWeeklyIfNeeded();
            string before = JsonUtility.ToJson(Data);
            try
            {
                current.pendingBattleToken = "";
                current.pendingChoice = -1;
                if (victory)
                {
                    Data.gold = checked(Data.gold + ChroniclesLabyrinth.RewardGold(option));
                    Data.forgeDust = checked(Data.forgeDust +
                        ChroniclesLabyrinth.RewardForgeDust(option));
                    Data.astralSeals = checked(Data.astralSeals +
                        ChroniclesLabyrinth.RewardSeals(option));
                    IncrementQuest("win_1");
                    IncrementQuest("win_3");
                    IncrementWeekly("win_10");
                    if (option.kind == LabyrinthRoomKind.Guardian)
                        IncrementWeekly("boss_3");
                    AdvanceLabyrinth(option);
                }
                else
                    current.lives = Math.Max(0, current.lives - 1);
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

        public bool AbandonLabyrinthBattle(string token)
        {
            return CompleteLabyrinthBattle(token, false);
        }

        public bool CancelLabyrinthStart(string token)
        {
            // Only use when creating a battle fails before gameplay begins.
            LabyrinthProgress current = LabyrinthState();
            if (string.IsNullOrEmpty(token) || token != current.pendingBattleToken)
                return false;
            string before = JsonUtility.ToJson(Data);
            current.pendingBattleToken = "";
            current.pendingChoice = -1;
            if (Save()) return true;
            Data = JsonUtility.FromJson<PlayerSave>(before);
            return false;
        }

        private void AdvanceLabyrinth(LabyrinthOption option)
        {
            LabyrinthProgress current = Data.labyrinth;
            current.score = checked(current.score +
                (option.kind == LabyrinthRoomKind.Guardian ? 350 :
                 option.kind == LabyrinthRoomKind.Elite ? 180 :
                 option.kind == LabyrinthRoomKind.Battle ? 100 : 65));
            current.nextRoom++;
            if (current.nextRoom > ChroniclesLabyrinth.Rooms)
            {
                current.completed = true;
                // Grant weekly completion bonus in the SAME persisted transaction.
                Data.gems = checked(Data.gems + 180);
                Data.astralSeals = checked(Data.astralSeals + 2);
            }
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
                Data.gold += stage.gold *
                    (ChroniclesEvents.DoubleGoldActive(DateTime.UtcNow) ? 2 : 1);
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

        public const int FastBattleDailyLimit = 3;
        public const int FastBattleMinutes = 120;

        public int FastBattlesRemaining()
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            return Data.fastBattleUtcDay == today ?
                Mathf.Max(0, FastBattleDailyLimit - Data.fastBattleUsed) :
                FastBattleDailyLimit;
        }

        // Instant 2h of idle income, a limited number of times per UTC day.
        public int FastBattle()
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (Data.fastBattleUtcDay != today)
            {
                Data.fastBattleUtcDay = today;
                Data.fastBattleUsed = 0;
            }
            if (Data.fastBattleUsed >= FastBattleDailyLimit) return 0;
            int collected = FastBattleMinutes *
                (IdleGoldPerMinute + Data.lastClearedStage * IdleGoldPerStagePerMinute);
            Data.fastBattleUsed++;
            collected += (int)(collected * ChroniclesGrowth.LegacyGoldBonus(Data));
            Data.gold += collected;
            IncrementQuest("idle_1");
            IncrementWeekly("idle_minutes_180", FastBattleMinutes);
            Save();
            return collected;
        }

        public ElementalGateProgress GateState(HeroElement element)
        {
            foreach (ElementalGateProgress gate in Data.elementalGates)
                if (gate.element == element.ToString()) return gate;
            var created = new ElementalGateProgress { element = element.ToString() };
            Data.elementalGates.Add(created);
            return created;
        }

        public int GateAttemptsRemaining()
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            return Data.gateUtcDay == today ?
                Mathf.Max(0, ChroniclesElementalGates.DailyAttempts -
                    Data.gateAttemptsUsed) :
                ChroniclesElementalGates.DailyAttempts;
        }

        // Team check: all five selected heroes must match the gate element.
        public bool GateTeamMatches(HeroElement element)
        {
            if (Data.selectedTeam.Count != 5) return false;
            foreach (string id in Data.selectedTeam)
                if (HeroElements.For(id) != element) return false;
            return true;
        }

        public bool CanEnterGate(HeroElement element)
        {
            return ChroniclesElementalGates.IsOpen(element, DateTime.UtcNow) &&
                GateAttemptsRemaining() > 0 && GateTeamMatches(element);
        }

        // Consumes one attempt; returns "" when entry is not allowed.
        public string BeginGate(HeroElement element)
        {
            if (!CanEnterGate(element)) return "";
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (Data.gateUtcDay != today)
            {
                Data.gateUtcDay = today;
                Data.gateAttemptsUsed = 0;
            }
            Data.gateAttemptsUsed++;
            Save();
            return element.ToString();
        }

        public void AbandonGate(HeroElement element)
        {
            // Attempts stay consumed (genre standard for failed entries),
            // but a battle that never started refunds the attempt.
            if (Data.gateAttemptsUsed > 0) Data.gateAttemptsUsed--;
            Save();
        }

        public bool CompleteGate(string runElement, bool victory, out string rewardLabel)
        {
            rewardLabel = "";
            HeroElement element;
            if (!System.Enum.TryParse(runElement, out element)) return false;
            ElementalGateProgress gate = GateState(element);
            if (!victory) { Save(); return true; }
            int level = Mathf.Clamp(gate.unlockedLevel, 1,
                ChroniclesElementalGates.Levels);
            bool firstClear = !gate.firstClears.Contains(level);
            if (firstClear) gate.firstClears.Add(level);
            Data.gold += ChroniclesElementalGates.RewardGold(level);
            Data.gems += ChroniclesElementalGates.RewardGems(level);
            Data.forgeDust += ChroniclesElementalGates.RewardForgeDust(level, firstClear);
            Data.relicDust += ChroniclesElementalGates.RewardRelicDust(level, firstClear);
            rewardLabel = ChroniclesElementalGates.RewardLabel(level, firstClear);
            if (gate.unlockedLevel < ChroniclesElementalGates.Levels)
                gate.unlockedLevel++;
            IncrementQuest("gates_1");
            Save();
            return true;
        }

        public EventAdventureProgress AdventureState(string adventureId)
        {
            foreach (EventAdventureProgress saved in Data.eventAdventures)
                if (saved.adventureId == adventureId) return saved;
            var created = new EventAdventureProgress { adventureId = adventureId };
            Data.eventAdventures.Add(created);
            return created;
        }

        // Stage 1..Stages; entry allowed for the next uncleared stage or a replay.
        public bool CanEnterAdventureStage(string adventureId, int stage)
        {
            if (stage < 1 || stage > ChroniclesEventAdventures.Stages) return false;
            if (ChroniclesEventAdventures.Active(DateTime.UtcNow).id != adventureId)
                return false; // only the live adventure is playable
            return stage <= AdventureState(adventureId).highestCleared + 1;
        }

        public string BeginAdventureStage(string adventureId, int stage)
        {
            if (!CanEnterAdventureStage(adventureId, stage)) return "";
            string runId = adventureId + ":" + stage;
            Data.adventurePendingRunId = runId;
            Save();
            return runId;
        }

        public void AbandonAdventure()
        {
            Data.adventurePendingRunId = "";
            Save();
        }

        public bool CompleteAdventureStage(string runId, bool victory,
            out string rewardLabel)
        {
            rewardLabel = "";
            if (string.IsNullOrEmpty(runId) || runId != Data.adventurePendingRunId)
                return false;
            Data.adventurePendingRunId = "";
            string[] parts = runId.Split(':');
            int stage;
            if (parts.Length != 2 || !int.TryParse(parts[1], out stage)) { Save(); return false; }
            var def = ChroniclesEventAdventures.Active(DateTime.UtcNow);
            if (def.id != parts[0]) { Save(); return false; }
            EventAdventureProgress state = AdventureState(def.id);
            if (!victory) { Save(); return true; }
            bool firstClear = stage > state.highestCleared;
            if (firstClear) state.highestCleared = stage;
            int currency = ChroniclesEventAdventures.CurrencyReward(stage, firstClear);
            int gold = ChroniclesEventAdventures.GoldReward(stage);
            state.currencyEarned += currency;
            Data.gold += gold;
            rewardLabel = gold + " gold + " + currency + " " + def.currencyName;
            IncrementQuest("adventure_1");
            Save();
            return true;
        }

        public bool ClaimAdventureMilestone(int threshold)
        {
            var def = ChroniclesEventAdventures.Active(DateTime.UtcNow);
            EventAdventureProgress state = AdventureState(def.id);
            if (state.currencyEarned < threshold ||
                state.milestonesClaimed.Contains(threshold)) return false;
            state.milestonesClaimed.Add(threshold);
            switch (threshold)
            {
                case 100: Data.gold += 1000; break;
                case 250: Data.gems += 60; break;
                case 500: Data.summonTickets += 1; break;
                case 900: Data.gold += 3000; Data.forgeDust += 80; break;
                case 1400: Data.gems += 120; Data.relicDust += 50; break;
                case 2000: Data.summonTickets += 2; Data.gems += 200; break;
                default: return false;
            }
            Save();
            return true;
        }

        public bool SpendLegacyPoint(int branch)
        {
            int earned = ChroniclesGrowth.LegacyPointsEarned(Data);
            if (ChroniclesGrowth.LegacyPointsSpent(Data) >= earned) return false;
            switch (branch)
            {
                case 0:
                    if (Data.legacyWar >= ChroniclesGrowth.LegacyCap) return false;
                    Data.legacyWar++; break;
                case 1:
                    if (Data.legacyGuard >= ChroniclesGrowth.LegacyCap) return false;
                    Data.legacyGuard++; break;
                case 2:
                    if (Data.legacyFortune >= ChroniclesGrowth.LegacyCap) return false;
                    Data.legacyFortune++; break;
                default: return false;
            }
            Save();
            return true;
        }

        public ActiveBounty FindActiveBounty(string bountyId)
        {
            foreach (ActiveBounty bounty in Data.activeBounties)
                if (bounty.id == bountyId) return bounty;
            return null;
        }

        // Sum of the effective levels of the N strongest owned heroes.
        public int BestDispatchPower(int heroCount)
        {
            var levels = new List<int>();
            foreach (OwnedHero hero in Data.heroes)
                if (hero != null)
                    levels.Add(ChroniclesGrowth.EffectiveLevel(Data, hero));
            levels.Sort((a, b) => b.CompareTo(a));
            int power = 0;
            for (int i = 0; i < levels.Count && i < heroCount; i++)
                power += levels[i];
            return power;
        }

        private void ResetBountyDayIfNeeded()
        {
            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            if (Data.bountyUtcDay == today) return;
            Data.bountyUtcDay = today;
            Data.claimedBountyIds.Clear();
        }

        public bool StartBounty(string bountyId)
        {
            BountyDefinition def = ChroniclesBounty.Find(bountyId);
            if (def == null) return false;
            ResetBountyDayIfNeeded();
            if (FindActiveBounty(bountyId) != null ||
                Data.claimedBountyIds.Contains(bountyId)) return false;
            if (Data.heroes.Count < def.minHeroes) return false;
            // Auto-dispatch the strongest owned heroes.
            var ranked = new List<OwnedHero>(Data.heroes);
            ranked.Sort((a, b) =>
                ChroniclesGrowth.EffectiveLevel(Data, b)
                    .CompareTo(ChroniclesGrowth.EffectiveLevel(Data, a)));
            var picks = new List<string>();
            int power = 0;
            for (int i = 0; i < ranked.Count && picks.Count < def.minHeroes; i++)
            {
                picks.Add(ranked[i].id);
                power += ChroniclesGrowth.EffectiveLevel(Data, ranked[i]);
            }
            if (picks.Count < def.minHeroes || power < def.requiredPower)
                return false;
            Data.activeBounties.Add(new ActiveBounty
            {
                id = bountyId,
                heroIds = picks,
                finishTicks = DateTime.UtcNow.Ticks +
                    TimeSpan.FromHours(def.hours).Ticks
            });
            Save();
            return true;
        }

        public bool ClaimBounty(string bountyId)
        {
            ActiveBounty running = FindActiveBounty(bountyId);
            BountyDefinition def = ChroniclesBounty.Find(bountyId);
            if (running == null || def == null ||
                running.finishTicks > DateTime.UtcNow.Ticks) return false;
            ResetBountyDayIfNeeded();
            Data.activeBounties.Remove(running);
            Data.claimedBountyIds.Add(bountyId);
            Data.gold += def.gold;
            Data.gems += def.gems;
            Data.forgeDust += def.forgeDust;
            Save();
            return true;
        }

        public int ClaimIdleGold()
        {
            int minutes = IdleAvailableMinutes();
            if (minutes <= 0) return 0;
            int collected = IdleProjectedGold();
            collected += (int)(collected * ChroniclesGrowth.LegacyGoldBonus(Data));
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
