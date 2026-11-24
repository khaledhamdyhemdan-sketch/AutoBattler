using System;
using System.Collections.Generic;

namespace MythicChronicles
{
    [Serializable]
    public sealed class ActiveBounty
    {
        public string id;
        public List<string> heroIds = new List<string>();
        public long finishTicks;
    }

    public sealed class BountyDefinition
    {
        public readonly string id, name;
        public readonly int hours, minHeroes, requiredPower;
        public readonly int gold, gems, forgeDust;

        public BountyDefinition(string id, string name, int hours,
            int minHeroes, int requiredPower, int gold, int gems, int forgeDust)
        {
            this.id = id; this.name = name; this.hours = hours;
            this.minHeroes = minHeroes; this.requiredPower = requiredPower;
            this.gold = gold; this.gems = gems; this.forgeDust = forgeDust;
        }

        public string RewardLabel
        {
            get
            {
                var parts = new List<string> { gold + " gold" };
                if (gems > 0) parts.Add(gems + " gems");
                if (forgeDust > 0) parts.Add(forgeDust + " forge dust");
                return string.Join(" • ", parts.ToArray());
            }
        }
    }

    /// <summary>
    /// Offline bounty board: dispatch owned heroes on timed missions.
    /// A deterministic set of six bounties refreshes every UTC day.
    /// </summary>
    public static class ChroniclesBounty
    {
        public const int DailySlots = 6;

        private static readonly BountyDefinition[] Pool =
        {
            new BountyDefinition("escort", "Escort the caravan", 2, 1, 15, 900, 15, 0),
            new BountyDefinition("scout", "Scout the dunes", 4, 2, 30, 1600, 0, 40),
            new BountyDefinition("tomb", "Map the sunken tomb", 6, 2, 55, 2400, 30, 0),
            new BountyDefinition("hunt", "Hunt the sand wurm", 8, 3, 90, 3600, 0, 90),
            new BountyDefinition("relic", "Recover the relic", 10, 3, 140, 5200, 55, 0),
            new BountyDefinition("omen", "Silence the omen", 12, 3, 200, 7500, 0, 160),
            new BountyDefinition("caravan", "Guard the gold barge", 3, 1, 25, 1300, 0, 30),
            new BountyDefinition("oasis", "Cleanse the oasis", 5, 2, 45, 2000, 25, 0),
            new BountyDefinition("cipher", "Decode the cipher", 7, 2, 75, 3000, 0, 70),
            new BountyDefinition("siege", "Break the siege", 12, 3, 260, 9000, 90, 200)
        };

        // Deterministic daily picks so every player day has the same texture.
        public static List<BountyDefinition> DailyBounties(DateTime utcNow)
        {
            var picks = new List<BountyDefinition>();
            int day = (int)(utcNow.Date - new DateTime(2026, 1, 1)).TotalDays;
            for (int i = 0; i < DailySlots; i++)
                picks.Add(Pool[(day * 7 + i * 3) % Pool.Length]);
            return picks;
        }

        public static BountyDefinition Find(string id)
        {
            foreach (BountyDefinition def in Pool)
                if (def.id == id) return def;
            return null;
        }
    }
}
