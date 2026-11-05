using System;
using System.Collections.Generic;

namespace MythicChronicles
{
    // An original shared world: mythology, folklore and historical legends.
    // "Chronicles" is an INTERNAL working title, not the game's published name.
    public enum Origin { Egypt, Greece, Norse, EastAsia, Folklore, History, Japan, China }
    public enum CombatRole { Tank, Warrior, Assassin, Mage, Support, Healer, Controller }
    // Launch ranks: R -> SR -> SSR -> UR. EX is reserved and cannot be obtained.
    // Aliases preserve compatibility with earlier development data and source.
    public enum Rarity { R = 0, SR = 1, SSR = 2, UR = 3, EX = 4,
        Rare = R, Epic = SR, Legendary = SSR }

    [Serializable]
    public sealed class HeroDefinition
    {
        public string id, name, story;
        public Origin origin;
        public CombatRole role;
        public Rarity rarity;
        public HeroElement element;
        public HeroDefinition(string id, string name, Origin origin, CombatRole role, Rarity rarity, string story)
        {
            this.id = id; this.name = name; this.origin = origin;
            this.role = role; this.rarity = rarity; this.story = story;
            this.element = HeroElements.For(id);
        }
        // Roster heroes carry their element explicitly (generated from shared/heroes/hero-roster.json).
        public HeroDefinition(string id, string name, Origin origin, CombatRole role, Rarity rarity,
            HeroElement element, string story)
        {
            this.id = id; this.name = name; this.origin = origin;
            this.role = role; this.rarity = rarity; this.story = story;
            this.element = element;
        }
    }

    public sealed class ChapterDefinition
    {
        public int number;
        public string title, summary, boss;
        public Origin origin;
        public ChapterDefinition(int number, string title, Origin origin, string summary, string boss)
        { this.number = number; this.title = title; this.origin = origin; this.summary = summary; this.boss = boss; }
    }

    public sealed class StageDefinition
    {
        public int id, chapter, encounter, recommendedPower, gold, gems;
        public bool boss;
        public string name, narrative;
        public StageDefinition(int id, ChapterDefinition chapter, int encounter)
        {
            this.id = id; this.chapter = chapter.number; this.encounter = encounter;
            boss = encounter == 10;
            name = boss ? "Guardian of " + chapter.title : chapter.title + " - " + encounter;
            recommendedPower = 850 + id * 145 + (boss ? 650 : 0);
            gold = 75 + id * 18 + (boss ? 500 : 0);
            gems = boss ? 50 : (encounter == 5 ? 10 : 0);
            narrative = boss ? "A fractured guardian blocks the passage. Restore this chapter of the Loom." : chapter.summary;
        }
    }

    public static class WorldsCatalog
    {
        // Every character is an original reinterpretation; no other game's artwork, names or kits are copied.
        // Roster data lives in shared/heroes/hero-roster.json; edit it there and run
        // `node tools/generate-hero-data.mjs` to regenerate HeroRosterData.g.cs.
        public static readonly HeroDefinition[] Heroes = HeroRosterData.Heroes;

        public static readonly ChapterDefinition[] Chapters =
        {
            new ChapterDefinition(1, "The Broken Nile", Origin.Egypt, "An unseen force tears the first thread of history.", "The Scribe Without a Name"),
            new ChapterDefinition(2, "The Starless Agora", Origin.Greece, "Ancient oracles begin to remember futures that never happened.", "The Marble Oracle"),
            new ChapterDefinition(3, "Ashes of Yggdrasil", Origin.Norse, "The great tree sheds leaves from worlds not yet born.", "The Rootbound Warden"),
            new ChapterDefinition(4, "The Mirror of Dawn", Origin.EastAsia, "The rising sun reflects the wrong century.", "The Mirror Dragon"),
            new ChapterDefinition(5, "The Crown in the Mist", Origin.Folklore, "A kingdom's stories are being rewritten.", "The Hollow Knight"),
            new ChapterDefinition(6, "The Last Chronicle", Origin.History, "Heroes from every age unite to repair the Loom of Ages.", "The Unwritten King")
        };

        private static readonly Dictionary<string, HeroDefinition> ById = BuildIndex();
        private static Dictionary<string, HeroDefinition> BuildIndex()
        {
            var index = new Dictionary<string, HeroDefinition>(StringComparer.Ordinal);
            foreach (var h in Heroes) index.Add(h.id, h);
            return index;
        }

        public static HeroDefinition FindHero(string id)
        {
            HeroDefinition h;
            return !string.IsNullOrEmpty(id) && ById.TryGetValue(id, out h) ? h : null;
        }

        public static StageDefinition GetStage(int stage)
        {
            if (stage < 1 || stage > 60) return null;
            int chapter = (stage - 1) / 10;
            return new StageDefinition(stage, Chapters[chapter], (stage - 1) % 10 + 1);
        }

        // Starter team provides all four ordinary elements and a healer,
        // without granting scarce Light/Dark UR units at sign-up.
        public static readonly string[] StartingHeroes =
            { "sobek", "bastet", "set", "nephthys", "persephone" };
    }
}
