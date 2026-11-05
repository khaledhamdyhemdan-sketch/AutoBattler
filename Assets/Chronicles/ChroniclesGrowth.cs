using System;
using System.Collections.Generic;

namespace MythicChronicles
{
    public enum RuneType { Strength, Vitality, Focus, Ward }

    [Serializable]
    public sealed class RuneInstance
    {
        public string instanceId;
        public RuneType type;
        public int level = 1;
        public string assignedHeroId;
    }

    public struct HeroGrowthStats
    {
        public int effectiveLevel;
        public float attackMultiplier, hpMultiplier, defenseMultiplier;
        public float skillMultiplier, ultimateMultiplier, passiveMultiplier;
    }

    public static class ChroniclesGrowth
    {
        public const int LevelCap = 200;
        public const int SkillCap = 10;
        public const int MaxTalents = 20;

        public static int LevelCost(int currentLevel) { return 75 + currentLevel * 35; }
        public static int SkillCost(int currentSkillLevel) { return 150 + currentSkillLevel * 105; }
        public static int TalentCost(int purchased) { return 250 + purchased * 125; }
        public static int RuneUpgradeCost(int level) { return 100 + level * 90; }

        // Synchronize low-level heroes with the 5th highest owned level.
        // Players still upgrade their top five normally; no extra account investment required.
        public static int ResonanceLevel(PlayerSave data)
        {
            if (data == null || data.heroes == null || data.heroes.Count < 5) return 1;
            var levels = new List<int>();
            foreach (OwnedHero hero in data.heroes)
                if (hero != null) levels.Add(Math.Max(1, hero.level));
            levels.Sort((a, b) => b.CompareTo(a));
            return Math.Min(LevelCap, levels[4]);
        }

        public static int EffectiveLevel(PlayerSave data, OwnedHero hero)
        {
            if (hero == null) return 1;
            return Math.Min(LevelCap, Math.Max(hero.level, ResonanceLevel(data)));
        }

        public static int RefundForLevelReset(int currentLevel)
        {
            int total = 0;
            for (int i = 1; i < Math.Min(LevelCap, currentLevel); i++)
                total += LevelCost(i);
            return total;
        }

        public static HeroGrowthStats Calculate(PlayerSave data, OwnedHero hero)
        {
            HeroGrowthStats stats = new HeroGrowthStats
            {
                effectiveLevel = EffectiveLevel(data, hero),
                attackMultiplier = 1f,
                hpMultiplier = 1f,
                defenseMultiplier = 1f,
                skillMultiplier = 1f,
                ultimateMultiplier = 1f,
                passiveMultiplier = 1f
            };
            if (hero == null) return stats;
            stats.attackMultiplier += hero.attackTalents * .012f;
            stats.hpMultiplier += hero.defenseTalents * .017f;
            stats.skillMultiplier += (Math.Max(1, hero.activeSkillLevel) - 1) * .075f;
            stats.ultimateMultiplier += (Math.Max(1, hero.ultimateSkillLevel) - 1) * .080f;
            stats.passiveMultiplier += (Math.Max(1, hero.passiveSkillLevel) - 1) * .045f;
            if (data == null || data.runes == null) return stats;
            foreach (RuneInstance rune in data.runes)
            {
                if (rune == null || rune.assignedHeroId != hero.id) continue;
                int lvl = Math.Max(1, Math.Min(20, rune.level));
                switch (rune.type)
                {
                    case RuneType.Strength: stats.attackMultiplier += .014f * lvl; break;
                    case RuneType.Vitality: stats.hpMultiplier += .020f * lvl; break;
                    case RuneType.Focus: stats.skillMultiplier += .015f * lvl; break;
                    case RuneType.Ward: stats.defenseMultiplier += .018f * lvl; break;
                }
            }
            return stats;
        }
    }
}
