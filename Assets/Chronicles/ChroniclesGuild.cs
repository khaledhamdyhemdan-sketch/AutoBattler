using System;
using System.Collections.Generic;

namespace MythicChronicles
{
    [Serializable]
    public sealed class GuildState
    {
        public int contributionTotal;
        public string contributionUtcDay = "";
        public int contributionToday;
        public string guildBossUtcDay = "";
        public int guildBossDamageToday;
        public int guildBossAttacksToday;
        public string guildBossPendingRunId = "";
        public List<int> guildBossMilestonesClaimed = new List<int>();
        public string weeklyChestClaimed = "";
    }

    /// <summary>
    /// LOCAL-FIRST GUILD PROTOTYPE. The player's guild is simulated offline:
    /// deterministic NPC members whose activity accrues over the UTC week, a
    /// daily guild boss whose total damage combines the player's real battle
    /// damage with simulated member damage, and a weekly guild chest. When a
    /// backend arrives, the member/damage sources swap for live data; the
    /// milestones, chest and boss battle flow stay.
    /// </summary>
    public static class ChroniclesGuild
    {
        public const string GuildName = "SONS OF THE NILE";
        public const int MemberCount = 24;
        public const int BossDailyAttacks = 2;
        public const int DailyContributionCap = 100;

        public static readonly int[] BossMilestones = { 50000, 150000, 400000, 900000, 1800000 };

        public static string BossMilestoneLabel(int damage)
        {
            switch (damage)
            {
                case 50000: return "500 gold + 20 gems";
                case 150000: return "1,200 gold + 40 forge dust";
                case 400000: return "80 gems + 30 relic dust";
                case 900000: return "2 summon tickets";
                case 1800000: return "300 gems + 3 summon tickets";
                default: return "";
            }
        }

        static int Hash(string text)
        {
            unchecked
            {
                int h = 5381;
                foreach (char c in text) h = h * 33 + c;
                return h & 0x7fffffff;
            }
        }

        static readonly string[] MemberNames =
        {
            "NileFang", "Solaris", "KemetRose", "StormScribe", "DeltaWolf",
            "AshesOfRa", "MoonlitSphinx", "ThebanGuard", "LotusBlade", "AnkhRunner",
            "HorusEye", "SobekJaw", "BastetClaw", "ScarabLord", "PapyrusMage",
            "MemphisKnight", "GizaWatcher", "SaharaWind", "Nefertari", "AnubisGate",
            "SetStorm", "IsisLight", "RaDawn"
        };

        public static string MemberName(int index)
        {
            return MemberNames[Math.Abs(index) % MemberNames.Length];
        }

        // Simulated member contribution for the UTC week so far, deterministic.
        public static int MemberContributionWeek(DateTime utcNow)
        {
            DateTime today = utcNow.Date;
            int dayOfWeek = ((int)today.DayOfWeek + 6) % 7;
            int weekSeed = (int)(today - new DateTime(2026, 1, 5)).TotalDays / 7;
            int total = 0;
            for (int i = 0; i < MemberCount - 1; i++)
            {
                int h = Hash("guild:" + i + ":" + weekSeed);
                int daily = 20 + h % 60;
                total += daily * (dayOfWeek + 1);
            }
            return total;
        }

        // Simulated member boss damage for the UTC day so far.
        public static int MemberBossDamageToday(DateTime utcNow)
        {
            int daySeed = (int)(utcNow.Date - new DateTime(2026, 1, 5)).TotalDays;
            int total = 0;
            for (int i = 0; i < MemberCount - 1; i++)
            {
                int h = Hash("boss:" + i + ":" + daySeed);
                total += 3000 + h % 22000;
            }
            return total;
        }

        public static int GuildLevel(int contributionTotal)
        {
            return Math.Min(20, 1 + contributionTotal / 1000);
        }

        public static int WeeklyChestTier(int weeklyContribution)
        {
            if (weeklyContribution >= 8000) return 3;
            if (weeklyContribution >= 4000) return 2;
            if (weeklyContribution >= 1500) return 1;
            return 0;
        }

        public static string WeeklyChestLabel(int tier)
        {
            switch (tier)
            {
                case 3: return "GOLDEN CHEST: 500 gems + 3 tickets";
                case 2: return "SILVER CHEST: 250 gems + 1 ticket";
                case 1: return "BRONZE CHEST: 100 gems";
                default: return "No chest - guild needs 1,500 weekly contribution";
            }
        }
    }
}
