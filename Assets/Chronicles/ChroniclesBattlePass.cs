using System;
using System.Collections.Generic;

namespace MythicChronicles
{
    /// <summary>
    /// Season Pass (free track). A monthly UTC season with 30 levels; XP comes
    /// from playing - quest claims and battle victories. Every level has a free
    /// reward. Built so a paid premium track can plug in later: rewards are
    /// data rows, and the claim path already tracks them per level.
    /// </summary>
    public static class ChroniclesBattlePass
    {
        public const int Levels = 30;
        public const int XpPerLevel = 100;

        public const int XpPerDailyQuestClaim = 20;
        public const int XpPerWeeklyQuestClaim = 60;
        public const int XpPerBattleWin = 6;

        public static string SeasonKey(DateTime utcNow)
        {
            return utcNow.ToString("yyyy-MM");
        }

        public static DateTime SeasonEnd(DateTime utcNow)
        {
            return new DateTime(utcNow.Year, utcNow.Month, 1, 0, 0, 0,
                DateTimeKind.Utc).AddMonths(1);
        }

        public static int LevelFor(int seasonXp)
        {
            return Math.Min(Levels, Math.Max(0, seasonXp / XpPerLevel));
        }

        public static int XpIntoLevel(int seasonXp)
        {
            return seasonXp % XpPerLevel;
        }

        /// <summary>Free-track reward per level (1-based).</summary>
        public static string RewardLabel(int level)
        {
            if (level % 10 == 0) return "2 summon tickets + 150 gems";
            if (level % 5 == 0) return "1 summon ticket + 80 gems";
            if (level % 3 == 0) return "60 forge dust + 30 relic dust";
            return (400 + level * 60) + " gold + " + (10 + level) + " gems";
        }

        public static void GrantReward(PlayerSave data, int level)
        {
            if (level % 10 == 0) { data.summonTickets += 2; data.gems += 150; return; }
            if (level % 5 == 0) { data.summonTickets += 1; data.gems += 80; return; }
            if (level % 3 == 0) { data.forgeDust += 60; data.relicDust += 30; return; }
            data.gold += 400 + level * 60;
            data.gems += 10 + level;
        }
    }
}
