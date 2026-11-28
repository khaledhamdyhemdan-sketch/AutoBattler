using System;
using System.Collections.Generic;

namespace MythicChronicles
{
    [Serializable]
    public sealed class ElementalGateProgress
    {
        public string element;
        public int unlockedLevel = 1;
        public List<int> firstClears = new List<int>();
    }

    /// <summary>
    /// Elemental Gates: daily-rotating, element-restricted dungeons that drop
    /// forge dust, relic dust and gems. A gate only opens to teams whose five
    /// heroes all share its element (Light/Darkness gates are the weekend
    /// chase). Six gates rotate across the UTC week; Sunday opens them all.
    /// </summary>
    public static class ChroniclesElementalGates
    {
        public const int Levels = 5;
        public const int DailyAttempts = 3;

        public static string GateName(HeroElement element)
        {
            switch (element)
            {
                case HeroElement.Fire: return "Gate of Embers";
                case HeroElement.Water: return "Gate of Tides";
                case HeroElement.Nature: return "Gate of Roots";
                case HeroElement.Wind: return "Gate of Storms";
                case HeroElement.Light: return "Gate of Dawn";
                default: return "Gate of Dusk";
            }
        }

        // Monday..Saturday rotate one gate; Sunday opens every gate.
        public static bool IsOpen(HeroElement element, DateTime utcNow)
        {
            if (utcNow.DayOfWeek == DayOfWeek.Sunday) return true;
            int slot = ((int)utcNow.DayOfWeek + 6) % 7; // Monday=0
            return slot == ElementSlot(element);
        }

        private static int ElementSlot(HeroElement element)
        {
            switch (element)
            {
                case HeroElement.Fire: return 0;
                case HeroElement.Water: return 1;
                case HeroElement.Nature: return 2;
                case HeroElement.Wind: return 3;
                case HeroElement.Light: return 4;
                default: return 5;
            }
        }

        public static string OpensLabel(HeroElement element)
        {
            string[] days = { "MON", "TUE", "WED", "THU", "FRI", "SAT" };
            return "OPENS " + days[ElementSlot(element)] + " + SUN";
        }

        // Reuses an existing campaign stage as the enemy template per level.
        public static int StageTemplateFor(int level)
        {
            return Math.Min(60, Math.Max(1, level * 10 + 5));
        }

        public static int RewardForgeDust(int level, bool firstClear)
        {
            int dust = 25 * level;
            return firstClear ? dust : dust / 2;
        }

        public static int RewardRelicDust(int level, bool firstClear)
        {
            int dust = 15 * level;
            return firstClear ? dust : dust / 2;
        }

        public static int RewardGems(int level)
        {
            return level * 8;
        }

        public static int RewardGold(int level)
        {
            return 400 * level;
        }

        public static string RewardLabel(int level, bool firstClear)
        {
            return RewardGold(level) + " gold • " + RewardGems(level) +
                " gems • " + RewardForgeDust(level, firstClear) + " forge • " +
                RewardRelicDust(level, firstClear) + " relic";
        }
    }
}
