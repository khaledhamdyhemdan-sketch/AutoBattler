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

        public static float EffectiveAstralUrRate(System.DateTime utcNow)
        {
            return AstralUrRate * (ChroniclesEvents.AstralRateUpActive(utcNow) ? 2f : 1f);
        }

        public static string Rates(SummonBannerType banner)
        {
            if (banner == SummonBannerType.AstralCovenant)
                return ChroniclesEvents.AstralRateUpActive(System.DateTime.UtcNow) ?
                    "SR 70.0% | SSR 28.5% | UR 3.0% FESTIVAL | 70-pull UR pity" :
                    "SR 70.0% | SSR 28.5% | UR 1.5% | 70-pull UR pity";
            return "R 20% | SR 65% | SSR 15% | 40-pull SSR pity";
        }

        public static int GemCost(SummonBannerType banner)
        {
            return banner == SummonBannerType.AstralCovenant ?
                AstralCostGems : StandardCostGems;
        }

        // Wishlist: a chosen UR wins half of all UR hits on the Astral banner.
        public const float WishlistUrShare = .5f;

        public static HeroDefinition PickFromPool(SummonBannerType banner,
            Rarity tier, List<HeroDefinition> pool, string wishHeroId)
        {
            if (banner == SummonBannerType.AstralCovenant && tier == Rarity.UR &&
                !string.IsNullOrEmpty(wishHeroId) &&
                UnityEngine.Random.value < WishlistUrShare)
            {
                foreach (HeroDefinition candidate in pool)
                    if (candidate.id == wishHeroId) return candidate;
            }
            return pool[UnityEngine.Random.Range(0, pool.Count)];
        }

        public static string WishlistLabel(string wishHeroId)
        {
            if (string.IsNullOrEmpty(wishHeroId)) return "WISHLIST: NONE";
            HeroDefinition hero = WorldsCatalog.FindHero(wishHeroId);
            return hero == null ? "WISHLIST: NONE" :
                "WISH UP 50%: " + hero.name.ToUpperInvariant();
        }

        // Cycles to the next eligible UR; returns "" after the last one.
        public static string NextWishlistPick(string currentId)
        {
            List<HeroDefinition> pool = Eligible(
                SummonBannerType.AstralCovenant, Rarity.UR);
            if (pool.Count == 0) return "";
            if (string.IsNullOrEmpty(currentId)) return pool[0].id;
            for (int i = 0; i < pool.Count; i++)
                if (pool[i].id == currentId)
                    return i + 1 < pool.Count ? pool[i + 1].id : "";
            return pool[0].id;
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
