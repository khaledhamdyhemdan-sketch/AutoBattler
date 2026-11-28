using System;
using System.Collections.Generic;

namespace MythicChronicles
{
    public enum ChroniclesEventKind { DoubleGold, AstralRateUp }

    public sealed class ChroniclesEventInfo
    {
        public string id, title, description;
        public ChroniclesEventKind kind;
        public DateTime startUtc, endUtc;
    }

    // Limited-time live events, genre-standard rotation. All times UTC.
    // Passive events: no claim step, their effect applies while active.
    public static class ChroniclesEvents
    {
        public static readonly List<ChroniclesEventInfo> All = new List<ChroniclesEventInfo>
        {
            new ChroniclesEventInfo
            {
                id = "launch_gold_rush", kind = ChroniclesEventKind.DoubleGold,
                title = "GOLD RUSH",
                description = "Double gold from story stage victories.",
                startUtc = new DateTime(2026, 11, 24, 0, 0, 0, DateTimeKind.Utc),
                endUtc = new DateTime(2026, 12, 8, 0, 0, 0, DateTimeKind.Utc)
            },
            new ChroniclesEventInfo
            {
                id = "astral_festival", kind = ChroniclesEventKind.AstralRateUp,
                title = "ASTRAL FESTIVAL",
                description = "UR odds doubled on the Astral Covenant banner.",
                startUtc = new DateTime(2026, 11, 24, 0, 0, 0, DateTimeKind.Utc),
                endUtc = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc)
            },
        };

        public static bool IsActive(ChroniclesEventInfo info, DateTime utcNow)
        {
            return utcNow >= info.startUtc && utcNow < info.endUtc;
        }

        public static bool IsUpcoming(ChroniclesEventInfo info, DateTime utcNow)
        {
            return utcNow < info.startUtc;
        }

        public static bool KindActive(ChroniclesEventKind kind, DateTime utcNow)
        {
            foreach (ChroniclesEventInfo info in All)
                if (info.kind == kind && IsActive(info, utcNow)) return true;
            return false;
        }

        public static bool DoubleGoldActive(DateTime utcNow)
        {
            return KindActive(ChroniclesEventKind.DoubleGold, utcNow);
        }

        public static bool AstralRateUpActive(DateTime utcNow)
        {
            return KindActive(ChroniclesEventKind.AstralRateUp, utcNow);
        }

        public static string Countdown(ChroniclesEventInfo info, DateTime utcNow)
        {
            TimeSpan span = IsUpcoming(info, utcNow) ?
                info.startUtc - utcNow : info.endUtc - utcNow;
            if (span.TotalSeconds < 0) return "ENDED";
            string stamp = span.Days > 0 ? span.Days + "D " + span.Hours + "H" :
                span.Hours > 0 ? span.Hours + "H " + span.Minutes + "M" :
                span.Minutes + "M";
            return (IsUpcoming(info, utcNow) ? "STARTS IN " : "ENDS IN ") + stamp;
        }
    }
}
