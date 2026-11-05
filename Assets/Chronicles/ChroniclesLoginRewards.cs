using System;

namespace MythicChronicles
{
    public sealed class LoginReward
    {
        public readonly int day, gold, gems, tickets, astralSeals;
        public LoginReward(int day, int gold, int gems, int tickets, int astralSeals)
        {
            this.day = day; this.gold = gold; this.gems = gems;
            this.tickets = tickets; this.astralSeals = astralSeals;
        }
        public string Label
        {
            get
            {
                if (astralSeals > 0) return astralSeals + " ASTRAL SEAL";
                if (tickets > 0) return tickets + " SUMMON TICKET";
                if (gems > 0) return gems + " GEMS";
                return gold + " GOLD";
            }
        }
    }

    public static class ChroniclesLoginRewards
    {
        public const int Days = 28;
        public static LoginReward ForDay(int day)
        {
            if (day < 1 || day > Days) return null;
            // Stronger rewards at 7-day milestones; no missing-day penalty.
            if (day == 7) return new LoginReward(day, 0, 0, 2, 0);
            if (day == 14) return new LoginReward(day, 0, 0, 0, 2);
            if (day == 21) return new LoginReward(day, 0, 0, 4, 0);
            if (day == 28) return new LoginReward(day, 0, 0, 0, 3);
            switch (day % 4)
            {
                case 1: return new LoginReward(day, 250 + day * 15, 0, 0, 0);
                case 2: return new LoginReward(day, 0, 30 + day * 2, 0, 0);
                case 3: return new LoginReward(day, 0, 0, 1, 0);
                default: return new LoginReward(day, 350 + day * 12, 0, 0, 0);
            }
        }
    }
}
