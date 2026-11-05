using System;
using System.Collections.Generic;

namespace MythicChronicles
{
    public enum SummonBannerType { Standard, AstralCovenant }

    [Serializable]
    public sealed class SummonHistoryRecord
    {
        public string heroId;
        public string banner;
        public string utc;
        public bool guaranteed;
    }

    public sealed class SummonBatchResult
    {
        public readonly List<string> heroIds = new List<string>();
        public bool success;
        public string error = "";
    }

    /// <summary>
    /// Experimental OFFLINE banner data. Percentages are tier probabilities,
    /// not individual character probabilities; within each tier every eligible
    /// hero is equally likely. Do NOT treat these as live-service/paid rates.
    /// </summary>
    public static class ChroniclesSummonRules
    {
        public const int StandardCostGems = 120;
        public const int AstralCostGems = 300;
        public const int StandardSsrPity = 40;
        public const int AstralUrPity = 70;
        public const float StandardRRate = .20f;
        public const float StandardSrRate = .65f;
        public const float StandardSsrRate = .15f;
        public const float AstralSrRate = .700f;
        public const float AstralSsrRate = .285f;
        public const float AstralUrRate = .015f;
        public const int HistoryLimit = 60;

        public static string DisplayName(SummonBannerType banner)
        {
            return banner == SummonBannerType.AstralCovenant ?
                "ASTRAL COVENANT - LIGHT & DARK UR" : "STANDARD SUMMON";
        }

        public static string Rates(SummonBannerType banner)
        {
            return banner == SummonBannerType.AstralCovenant ?
                "SR 70.0% | SSR 28.5% | UR 1.5% | 70-pull UR pity" :
                "R 20% | SR 65% | SSR 15% | 40-pull SSR pity";
        }

        public static int GemCost(SummonBannerType banner)
        {
            return banner == SummonBannerType.AstralCovenant ?
                AstralCostGems : StandardCostGems;
        }

        public static List<HeroDefinition> Eligible(SummonBannerType banner, Rarity rarity)
        {
            var pool = new List<HeroDefinition>();
            foreach (HeroDefinition hero in WorldsCatalog.Heroes)
            {
                if (hero.rarity != rarity) continue;
                if (banner == SummonBannerType.Standard &&
                    (HeroElements.IsSpecial(hero) || rarity == Rarity.UR))
                    continue;
                if (banner == SummonBannerType.AstralCovenant && rarity == Rarity.R)
                    continue;
                pool.Add(hero);
            }
            return pool;
        }
    }
}
