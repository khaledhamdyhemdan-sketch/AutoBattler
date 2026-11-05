using System;
using UnityEngine;

namespace MythicChronicles
{
    public sealed class AchievementDefinition
    {
        public readonly string id, title;
        public readonly int target, gold, gems, tickets, astralSeals;
        public AchievementDefinition(string id, string title, int target,
            int gold, int gems, int tickets = 0, int astralSeals = 0)
        {
            this.id = id; this.title = title; this.target = target;
            this.gold = gold; this.gems = gems;
            this.tickets = tickets; this.astralSeals = astralSeals;
        }
    }

    /// <summary>
    /// Permanent account milestones derived from persisted progression rather
    /// than daily/weekly counters. Unclaimed rewards survive calendar resets.
    /// </summary>
    public static class ChroniclesAchievements
    {
        public static readonly AchievementDefinition[] All =
        {
            new AchievementDefinition("story10", "First Chapter Complete", 10, 400, 30),
            new AchievementDefinition("story30", "Three Realms Restored", 30, 1100, 100),
            new AchievementDefinition("story60", "Keeper of the Chronicle", 60, 3500, 250, 3),
            new AchievementDefinition("elite10", "Elite Vanquisher", 10, 800, 60),
            new AchievementDefinition("dark10", "Shadowbreaker", 10, 1200, 100, 0, 1),
            new AchievementDefinition("tower10", "Climber of the Spires", 10, 900, 50),
            new AchievementDefinition("tower50", "Skywalker", 50, 2500, 170, 0, 2),
            new AchievementDefinition("heroes10", "Assemble Ten Heroes", 10, 350, 40),
            new AchievementDefinition("heroes25", "Legends Among Us", 25, 1200, 100, 2),
            new AchievementDefinition("heroes41", "The Complete Chronicle", 41, 5000, 400, 0, 3),
            new AchievementDefinition("ur1", "Awaken a Cosmic Champion", 1, 900, 80),
            new AchievementDefinition("ur5", "Council of the Cosmos", 5, 2500, 200, 0, 2),
            new AchievementDefinition("gear6", "Fully Equipped", 6, 550, 45),
            new AchievementDefinition("relic3", "Collector of Relics", 3, 1800, 140)
        };

        public static AchievementDefinition Find(string id)
        {
            foreach (AchievementDefinition item in All)
                if (item.id == id) return item;
            return null;
        }

        public static int Progress(PlayerSave save, string id)
        {
            if (save == null) return 0;
            switch (id)
            {
                case "story10":
                case "story30":
                case "story60":
                    return Mathf.Max(0, save.lastClearedStage);
                case "elite10":
                    return save.eliteClears == null ? 0 : save.eliteClears.Count;
                case "dark10":
                    return save.darkClears == null ? 0 : save.darkClears.Count;
                case "tower10":
                case "tower50":
                    int maxFloor = 0;
                    foreach (Origin origin in ChroniclesTowers.TowerOrigins)
                        maxFloor = Math.Max(maxFloor, ChroniclesTowers.Highest(save, origin));
                    return maxFloor;
                case "heroes10":
                case "heroes25":
                case "heroes41":
                    return save.heroes == null ? 0 : save.heroes.Count;
                case "ur1":
                case "ur5":
                    int total = 0;
                    if (save.heroes != null)
                        foreach (OwnedHero hero in save.heroes)
                        {
                            if (hero == null) continue;
                            HeroDefinition definition = WorldsCatalog.FindHero(hero.id);
                            if (definition != null && definition.rarity == Rarity.UR) total++;
                        }
                    return total;
                case "gear6":
                    int best = 0;
                    if (save.heroes != null)
                        foreach (OwnedHero hero in save.heroes)
                        {
                            if (hero == null) continue;
                            int count = ChroniclesEquipment.Calculate(
                                save.equipment, hero.id).equippedCount;
                            if (count > best) best = count;
                        }
                    return best;
                case "relic3":
                    return save.relics == null ? 0 : save.relics.Count;
                default:
                    return 0;
            }
        }
    }
}
