using System;
using System.Collections.Generic;

namespace MythicChronicles
{
    public enum HeroElement { Fire, Water, Nature, Wind, Light, Darkness }

    public struct ElementalTeamBonus
    {
        public float attackMultiplier, healthMultiplier, startingEnergy;
        public int matchingCount, lightCount, darkCount;
        public HeroElement core;
        public string description;
    }

    // Element is deliberately independent of pantheon/origin and acquisition rarity.
    // Four ordinary elements form a counter loop; Light and Darkness are rare
    // wildcards that also unlock a complementary two-hero resonance.
    public static class HeroElements
    {
        public const float AdvantageBonus = .15f;
        public const float DisadvantagePenalty = .12f;
        public const float LightDarkRivalryBonus = .18f;

        private static readonly Dictionary<string, HeroElement> Map =
            new Dictionary<string, HeroElement>(StringComparer.Ordinal)
            {
                { "horus", HeroElement.Wind },
                { "anubis", HeroElement.Wind },
                { "ra", HeroElement.Light },
                { "sobek", HeroElement.Water },
                { "set", HeroElement.Fire },
                { "isis", HeroElement.Water },
                { "sekhmet", HeroElement.Fire },
                { "nephthys", HeroElement.Wind },
                { "bastet", HeroElement.Nature },
                { "maat", HeroElement.Nature },
                { "athena", HeroElement.Wind },
                { "artemis", HeroElement.Nature },
                { "hades", HeroElement.Darkness },
                { "persephone", HeroElement.Nature },
                { "heracles", HeroElement.Fire },
                { "freyja", HeroElement.Nature },
                { "thor", HeroElement.Wind },
                { "odin", HeroElement.Light },
                { "hel", HeroElement.Darkness },
                { "skadi", HeroElement.Water },
                { "amaterasu", HeroElement.Light },
                { "susanoo", HeroElement.Wind },
                { "sunwukong", HeroElement.Fire },
                { "guanyin", HeroElement.Water },
                { "nezha", HeroElement.Fire },
                { "morgana", HeroElement.Nature },
                { "merlin", HeroElement.Wind },
                { "arthur", HeroElement.Water },
                { "cleopatra", HeroElement.Water },
                { "joan", HeroElement.Fire },
                { "amun", HeroElement.Light },
                { "apep", HeroElement.Darkness },
                { "zeus", HeroElement.Light },
                { "nyx", HeroElement.Darkness },
                { "baldr", HeroElement.Light },
                { "fenrir", HeroElement.Darkness },
                { "izanagi", HeroElement.Light },
                { "izanami", HeroElement.Darkness },
                { "nuwa", HeroElement.Light },
                { "hundun", HeroElement.Darkness },
                { "apollo", HeroElement.Light }
            };

        public static bool IsSpecial(HeroElement kind)
        {
            return kind == HeroElement.Light || kind == HeroElement.Darkness;
        }
        public static bool IsSpecial(HeroDefinition hero)
        {
            return hero != null && IsSpecial(For(hero.id));
        }
        public static HeroElement For(string heroId)
        {
            HeroElement kind;
            return heroId != null && Map.TryGetValue(heroId, out kind)
                ? kind : HeroElement.Nature;
        }

        // Fire beats Nature, Nature beats Wind, Wind beats Water, Water beats Fire.
        public static float DamageMultiplier(HeroElement attacking, HeroElement defending)
        {
            if (IsSpecial(attacking) && IsSpecial(defending))
            {
                if (attacking == defending) return 1f;
                return 1f + LightDarkRivalryBonus; // each can punish the other
            }
            if (IsSpecial(attacking) || IsSpecial(defending)) return 1f;
            if (attacking == defending) return 1f;
            if ((attacking == HeroElement.Fire && defending == HeroElement.Nature) ||
                (attacking == HeroElement.Nature && defending == HeroElement.Wind) ||
                (attacking == HeroElement.Wind && defending == HeroElement.Water) ||
                (attacking == HeroElement.Water && defending == HeroElement.Fire))
                return 1f + AdvantageBonus;
            if ((defending == HeroElement.Fire && attacking == HeroElement.Nature) ||
                (defending == HeroElement.Nature && attacking == HeroElement.Wind) ||
                (defending == HeroElement.Wind && attacking == HeroElement.Water) ||
                (defending == HeroElement.Water && attacking == HeroElement.Fire))
                return 1f - DisadvantagePenalty;
            return 1f;
        }

        public static ElementalTeamBonus Calculate(IList<HeroDefinition> team)
        {
            int[] coreCounts = new int[4];
            int light = 0, darkness = 0;
            if (team != null)
            {
                foreach (HeroDefinition hero in team)
                {
                    if (hero == null) continue;
                    HeroElement element = For(hero.id);
                    if (element == HeroElement.Light) light++;
                    else if (element == HeroElement.Darkness) darkness++;
                    else coreCounts[(int)element]++;
                }
            }
            int highest = 0, secondHighest = 0, bestIndex = 0;
            for (int i = 0; i < 4; i++)
            {
                if (coreCounts[i] > highest)
                {
                    secondHighest = highest;
                    highest = coreCounts[i];
                    bestIndex = i;
                }
                else if (coreCounts[i] > secondHighest) secondHighest = coreCounts[i];
            }
            // A special hero fills the strongest basic elemental party (not all elements at once).
            int matching = highest > 0 ? Math.Min(5, highest + light + darkness) : 0;
            float baseBonus = matching >= 5 ? .16f :
                matching >= 4 ? .12f :
                matching >= 3 && secondHighest >= 2 ? .11f :
                matching >= 3 ? .08f : 0f;
            int specials = light + darkness;
            float specialBonus = specials * .02f;
            bool eclipsePair = light > 0 && darkness > 0;
            if (eclipsePair) specialBonus += .05f;
            // Limit stacked team bonuses: rarity should not invalidate normal units.
            float teamBonus = Math.Min(.30f, baseBonus + specialBonus);
            float energy = eclipsePair ? 12f : specials > 0 ? 4f : 0f;
            string effect = matching >= 3 ?
                ((HeroElement)bestIndex).ToString() + " covenant " + matching + "/5" :
                "No elemental covenant";
            if (light > 0) effect += " + Radiance";
            if (darkness > 0) effect += " + Eclipse";
            if (eclipsePair) effect += " + Equinox";
            return new ElementalTeamBonus
            {
                attackMultiplier = 1f + teamBonus, healthMultiplier = 1f + teamBonus,
                startingEnergy = energy, matchingCount = matching,
                lightCount = light, darkCount = darkness,
                core = (HeroElement)bestIndex, description = effect
            };
        }
    }
}
