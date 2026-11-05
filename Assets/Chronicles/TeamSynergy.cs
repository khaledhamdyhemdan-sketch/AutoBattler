using System;
using System.Collections.Generic;

namespace MythicChronicles
{
    public struct TeamSynergyBonus
    {
        public float hpMultiplier;
        public float attackMultiplier;
        public float startingEnergy;
        public int largestOriginCount, uniqueOrigins;
        public string description;
    }

    public static class TeamSynergy
    {
        public static TeamSynergyBonus Calculate(IList<HeroDefinition> heroes)
        {
            var count = new Dictionary<Origin, int>();
            int largest = 0;
            if (heroes != null)
            {
                foreach (HeroDefinition hero in heroes)
                {
                    if (hero == null) continue;
                    int current;
                    count.TryGetValue(hero.origin, out current);
                    current++;
                    count[hero.origin] = current;
                    if (current > largest) largest = current;
                }
            }

            float attack = largest >= 5 ? 1.20f : largest >= 4 ? 1.15f :
                largest >= 3 ? 1.10f : largest >= 2 ? 1.06f : 1f;
            float health = largest >= 5 ? 1.20f : largest >= 4 ? 1.13f :
                largest >= 3 ? 1.08f : largest >= 2 ? 1.04f : 1f;
            float energy = count.Count >= 5 ? 20f : count.Count >= 4 ? 12f : 0f;
            string name = largest >= 5 ? "Pure Origins V" :
                largest >= 4 ? "Pure Origins IV" :
                largest >= 3 ? "Shared Origins III" :
                largest >= 2 ? "Shared Origins II" : "No same-origin bonus";
            if (energy > 0f) name += " + Cross-World Unity";
            return new TeamSynergyBonus
            {
                hpMultiplier = health, attackMultiplier = attack, startingEnergy = energy,
                largestOriginCount = largest, uniqueOrigins = count.Count, description = name
            };
        }
    }
}
