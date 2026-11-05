using System;
using UnityEngine;

namespace MythicChronicles
{
    // Offline solo score challenge. This is NOT a real global leaderboard.
    public static class ChroniclesRaids
    {
        public const int AttemptsPerDay = 3;
        public const int DurationSeconds = 90;

        public static string CurrentUtcDay()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd");
        }

        public static int ChapterForDay(DateTime utc)
        {
            // ISO date-based rotation makes the daily encounter predictable.
            int days = (int)(utc.Date - new DateTime(2026, 1, 1)).TotalDays;
            int index = ((days % 6) + 6) % 6;
            return index + 1;
        }

        public static StageDefinition StageForDay(DateTime utc)
        {
            int chapter = ChapterForDay(utc);
            return WorldsCatalog.GetStage(chapter * 10);
        }

        public static int RewardTier(int score)
        {
            if (score >= 150000) return 5;
            if (score >= 80000) return 4;
            if (score >= 40000) return 3;
            if (score >= 15000) return 2;
            if (score >= 3000) return 1;
            return 0;
        }

        public static string Rank(int score)
        {
            switch (RewardTier(score))
            {
                case 5: return "MYTHIC SLAYER";
                case 4: return "WORLD BREAKER";
                case 3: return "LEGENDARY HUNTER";
                case 2: return "MASTER HUNTER";
                case 1: return "BOSS HUNTER";
                default: return "CHALLENGER";
            }
        }

        public static int GoldForAttempt(int score)
        {
            return 150 + Mathf.Min(1000, Mathf.Max(0, score) / 200);
        }

        // Each tier awards the difference since the previous achieved tier
        // for the UTC day, not the full tier prize after every attempt.
        public static int GemsForTierUpgrade(int previousTier, int newTier)
        {
            return Mathf.Max(0, newTier - previousTier) * 12;
        }

        public static int SealsForTierUpgrade(int previousTier, int newTier)
        {
            int oldSeals = previousTier >= 5 ? 2 :
                previousTier >= 3 ? 1 : 0;
            int newSeals = newTier >= 5 ? 2 :
                newTier >= 3 ? 1 : 0;
            return newSeals - oldSeals;
        }
    }
}
