using System;
using System.Collections.Generic;

namespace MythicChronicles
{
    [Serializable]
    public sealed class EventAdventureProgress
    {
        public string adventureId;
        public int highestCleared;      // 0 = nothing cleared yet
        public int currencyEarned;      // lifetime this rotation
        public List<int> milestonesClaimed = new List<int>();
    }

    /// <summary>
    /// Event Adventures: limited-time themed stage chains that rotate weekly
    /// (UTC). Each adventure has 10 stages, its own story flavor and an event
    /// currency that feeds a milestone reward track. Fully offline and
    /// deterministic: the active adventure is keyed by ISO week number, so
    /// every player sees the same rotation without a server.
    /// </summary>
    public static class ChroniclesEventAdventures
    {
        public const int Stages = 10;

        public sealed class AdventureDef
        {
            public string id, title, tagline, currencyName;
            public HeroElement theme;
        }

        public static readonly AdventureDef[] Rotation =
        {
            new AdventureDef
            {
                id = "scarab_tomb", title = "TOMB OF THE SCARAB KING",
                tagline = "Raid the sunken tomb before the sands reclaim it.",
                currencyName = "Sun Scarabs", theme = HeroElement.Light
            },
            new AdventureDef
            {
                id = "serpent_flood", title = "FLOOD OF THE SERPENT",
                tagline = "Hold the river temples against the rising tide.",
                currencyName = "River Pearls", theme = HeroElement.Water
            },
            new AdventureDef
            {
                id = "storm_pharoah", title = "WRATH OF THE STORM PHARAOH",
                tagline = "Break the storm cult before the sky falls.",
                currencyName = "Storm Sigils", theme = HeroElement.Wind
            },
        };

        public static int WeekIndex(DateTime utcNow)
        {
            // ISO-8601 week number, deterministic across devices.
            var cal = System.Globalization.CultureInfo.InvariantCulture.Calendar;
            return cal.GetWeekOfYear(utcNow,
                System.Globalization.CalendarWeekRule.FirstFourDayWeek,
                DayOfWeek.Monday) + utcNow.Year * 100;
        }

        public static AdventureDef Active(DateTime utcNow)
        {
            return Rotation[WeekIndex(utcNow) % Rotation.Length];
        }

        public static AdventureDef Next(DateTime utcNow)
        {
            return Rotation[(WeekIndex(utcNow) + 1) % Rotation.Length];
        }

        public static TimeSpan TimeLeft(DateTime utcNow)
        {
            // Rotation flips Monday 00:00 UTC.
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)utcNow.DayOfWeek + 7) % 7;
            if (daysUntilMonday == 0) daysUntilMonday = 7;
            DateTime flip = utcNow.Date.AddDays(daysUntilMonday);
            return flip - utcNow;
        }

        // Reuses campaign stages as enemy templates, ramping across the chain.
        public static int StageTemplateFor(int stage)
        {
            return Math.Min(60, Math.Max(1, 4 + stage * 5));
        }

        public static int CurrencyReward(int stage, bool firstClear)
        {
            int amount = 20 + stage * 10;
            return firstClear ? amount : amount / 4;
        }

        public static int GoldReward(int stage)
        {
            return 300 + stage * 150;
        }

        /// <summary>Milestone thresholds on the currency track, with rewards.</summary>
        public static readonly int[] MilestoneThresholds = { 100, 250, 500, 900, 1400, 2000 };

        public static string MilestoneRewardLabel(int threshold)
        {
            switch (threshold)
            {
                case 100: return "1,000 gold";
                case 250: return "60 gems";
                case 500: return "1 summon ticket";
                case 900: return "3,000 gold + 80 forge dust";
                case 1400: return "120 gems + 50 relic dust";
                case 2000: return "2 summon tickets + 200 gems";
                default: return "";
            }
        }

        public static string StageTitle(AdventureDef def, int stage)
        {
            return def.title + "  -  STAGE " + stage + "/" + Stages;
        }
    }
}
