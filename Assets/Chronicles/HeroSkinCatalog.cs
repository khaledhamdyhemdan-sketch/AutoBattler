using System;
using System.Collections.Generic;

namespace MythicChronicles
{
    public sealed class HeroSkin
    {
        public readonly string heroId, id, displayName, visualTheme;
        public readonly int gemCost;
        public HeroSkin(string heroId, string id, string displayName, string visualTheme, int gemCost)
        {
            this.heroId = heroId; this.id = id; this.displayName = displayName;
            this.visualTheme = visualTheme; this.gemCost = gemCost;
        }
        public string OwnershipKey { get { return heroId + ":" + id; } }
        public string ResourcePath { get { return "Chronicles/Heroes/" + heroId + "/" + id; } }
    }

    public static class HeroSkinCatalog
    {
        // One optional costume concept per hero, keyed by hero ID in
        // shared/heroes/hero-roster.json. ALL require original production-ready visual assets.
        // These definitions implement entitlement/equip logic, not missing animations or art.

        private static readonly Dictionary<string, List<HeroSkin>> ByHero = Build();

        private static Dictionary<string, List<HeroSkin>> Build()
        {
            var result = new Dictionary<string, List<HeroSkin>>(StringComparer.Ordinal);
            for (int i = 0; i < WorldsCatalog.Heroes.Length; i++)
            {
                HeroDefinition hero = WorldsCatalog.Heroes[i];
                string alternateId = "ascendant";
                var skins = new List<HeroSkin>
                {
                    new HeroSkin(hero.id, "default", "Original Appearance",
                        "Authentic original design for this crossover hero.", 0)
                };
                string altName;
                if (HeroRosterData.AlternateSkinNames.TryGetValue(hero.id, out altName))
                    skins.Add(new HeroSkin(hero.id, alternateId, altName,
                        "Alternative animated fantasy costume: " + altName, 280));
                result[hero.id] = skins;
            }
            return result;
        }

        public static IList<HeroSkin> For(string heroId)
        {
            List<HeroSkin> result;
            return heroId != null && ByHero.TryGetValue(heroId, out result)
                ? result.AsReadOnly() : new List<HeroSkin>();
        }

        public static HeroSkin Find(string heroId, string skinId)
        {
            foreach (HeroSkin skin in For(heroId))
                if (skin.id == skinId) return skin;
            return null;
        }
    }
}
