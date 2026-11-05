using System;
using UnityEngine;

namespace MythicChronicles
{
    [Serializable]
    public sealed class ArenaTrainingProgress
    {
        public string utcDay = "";
        public int rating = 900;
        public int bestRating = 900;
        public int attemptsToday;
        public int wins, losses;
        public string pendingMatchToken = "";
    }

    /// <summary>
    /// OFFLINE PvE practice mode, not real Arena PvP or a global leaderboard.
    /// NPC lineups use story hero pools until a trusted server is available.
    /// </summary>
    public static class ChroniclesArenaTraining
    {
        public const int DailyAttempts = 5;
        public const int StartingRating = 900;
        public const int VictoryRating = 22;
        public const int DefeatRating = 12;

        public static string CurrentDay()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd");
        }

        public static string Tier(int rating)
        {
            if (rating >= 2400) return "MYTHIC";
            if (rating >= 2200) return "LEGEND";
            if (rating >= 2000) return "GRANDMASTER";
            if (rating >= 1800) return "MASTER";
            if (rating >= 1600) return "DIAMOND";
            if (rating >= 1400) return "PLATINUM";
            if (rating >= 1200) return "GOLD";
            if (rating >= 1000) return "SILVER";
            return "BRONZE";
        }

        public static int StageFor(int rating, int attemptNumber)
        {
            int chapter = Math.Max(0, Math.Min(5, (Math.Max(0, rating) - 700) / 310));
            int encounter = ((Math.Max(1, attemptNumber) * 3 +
                Math.Max(0, rating) / 100) % 9) + 1;
            return chapter * 10 + encounter; // excludes guardians
        }

        public static float EnemyMultiplier(int rating)
        {
            return Mathf.Clamp(1f +
                (Math.Max(0, rating) - StartingRating) * .00042f,
                .9f, 2.25f);
        }

        public static int Gold(bool victory)
        {
            return victory ? 260 : 70;
        }

        public static int Gems(bool victory)
        {
            return victory ? 12 : 0;
        }
    }
}
