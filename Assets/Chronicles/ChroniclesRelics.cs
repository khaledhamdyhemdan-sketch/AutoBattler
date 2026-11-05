using System;
using System.Collections.Generic;

namespace MythicChronicles
{
    public sealed class WorldRelic
    {
        public readonly string id, name, description;
        public readonly int chapter;
        public readonly float attackBonus, hpBonus, startingEnergy;
        public WorldRelic(string id, string name, string description, int chapter,
            float attackBonus, float hpBonus, float startingEnergy)
        {
            this.id = id; this.name = name; this.description = description; this.chapter = chapter;
            this.attackBonus = attackBonus; this.hpBonus = hpBonus;
            this.startingEnergy = startingEnergy;
        }
    }

    public static class ChroniclesRelics
    {
        // Original legendary artifacts unlocked by first-clear chapter boss victories.
        public static readonly WorldRelic[] All =
        {
            new WorldRelic("sun_disc", "Disc of Dawn",
                "Solar disc recovered from the river of lost time.", 1, .07f, 0f, 0f),
            new WorldRelic("oracle_lens", "Oracle Lens",
                "A fractured lens that sees through possible futures.", 2, 0f, 0f, 18f),
            new WorldRelic("world_root", "Root of the Worlds",
                "A living branch that strengthens all who defend it.", 3, 0f, .10f, 0f),
            new WorldRelic("mirror_shard", "Mirror of Dawn",
                "Mirror of multiple timelines and their impossible paths.", 4, .045f, .05f, 0f),
            new WorldRelic("crown_oath", "Crown of the Unbroken Oath",
                "A forgotten crown whose radiance binds companions.", 5, 0f, .07f, 8f),
            new WorldRelic("loom_heart", "Heart of the Loom",
                "A restored pulse that belongs to every era.", 6, .055f, .055f, 10f)
        };
        private static readonly Dictionary<string, WorldRelic> ById = Index();
        private static Dictionary<string, WorldRelic> Index()
        {
            var results = new Dictionary<string, WorldRelic>(StringComparer.Ordinal);
            foreach (WorldRelic relic in All) results.Add(relic.id, relic);
            return results;
        }
        public static WorldRelic Find(string id)
        {
            WorldRelic relic;
            return id != null && ById.TryGetValue(id, out relic) ? relic : null;
        }
        public static WorldRelic FromBossStage(int stageId)
        {
            if (stageId < 1 || stageId > 60 || stageId % 10 != 0) return null;
            return All[stageId / 10 - 1];
        }
    }
}
