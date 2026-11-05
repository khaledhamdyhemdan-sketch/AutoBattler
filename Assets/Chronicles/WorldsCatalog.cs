using System;
using System.Collections.Generic;

namespace MythicChronicles
{
    // An original shared world: mythology, folklore and historical legends.
    // "Chronicles" is an INTERNAL working title, not the game's published name.
    public enum Origin { Egypt, Greece, Norse, EastAsia, Folklore, History }
    public enum CombatRole { Tank, Warrior, Assassin, Mage, Support, Healer, Controller }
    public enum Rarity { Rare, Epic, Legendary }

    [Serializable]
    public sealed class HeroDefinition
    {
        public string id, name, story;
        public Origin origin;
        public CombatRole role;
        public Rarity rarity;
        public HeroDefinition(string id, string name, Origin origin, CombatRole role, Rarity rarity, string story)
        {
            this.id = id; this.name = name; this.origin = origin;
            this.role = role; this.rarity = rarity; this.story = story;
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
        public static readonly HeroDefinition[] Heroes =
        {
            new HeroDefinition("horus", "Horus", Origin.Egypt, CombatRole.Assassin, Rarity.Legendary, "A sky sentinel tracking fractures between ages."),
            new HeroDefinition("anubis", "Anubis", Origin.Egypt, CombatRole.Tank, Rarity.Legendary, "The gatekeeper carries the names of lost worlds."),
            new HeroDefinition("ra", "Ra", Origin.Egypt, CombatRole.Mage, Rarity.Legendary, "His solar barque guides the wandering timelines."),
            new HeroDefinition("sobek", "Sobek", Origin.Egypt, CombatRole.Tank, Rarity.Epic, "Guardian of the life-bearing river."),
            new HeroDefinition("set", "Set", Origin.Egypt, CombatRole.Warrior, Rarity.Epic, "The tempest refuses to serve a false destiny."),
            new HeroDefinition("isis", "Isis", Origin.Egypt, CombatRole.Healer, Rarity.Legendary, "She binds shattered destinies with living light."),
            new HeroDefinition("sekhmet", "Sekhmet", Origin.Egypt, CombatRole.Warrior, Rarity.Legendary, "Her wrath protects rather than destroys the innocent."),
            new HeroDefinition("nephthys", "Nephthys", Origin.Egypt, CombatRole.Controller, Rarity.Epic, "Keeper of the night paths."),
            new HeroDefinition("bastet", "Bastet", Origin.Egypt, CombatRole.Assassin, Rarity.Epic, "A moonlit hunter between city rooftops."),
            new HeroDefinition("maat", "Maat", Origin.Egypt, CombatRole.Support, Rarity.Legendary, "The feather that measures the balance of ages."),
            new HeroDefinition("athena", "Athena", Origin.Greece, CombatRole.Support, Rarity.Legendary, "A strategist who reads battles as shifting constellations."),
            new HeroDefinition("artemis", "Artemis", Origin.Greece, CombatRole.Assassin, Rarity.Epic, "The huntress pursues stolen moonlight."),
            new HeroDefinition("hades", "Hades", Origin.Greece, CombatRole.Controller, Rarity.Legendary, "A sovereign guarding the silence between worlds."),
            new HeroDefinition("persephone", "Persephone", Origin.Greece, CombatRole.Healer, Rarity.Legendary, "Bloom and winter answer her call."),
            new HeroDefinition("heracles", "Heracles", Origin.Greece, CombatRole.Warrior, Rarity.Epic, "A champion whose strength must serve wisdom."),
            new HeroDefinition("freyja", "Freyja", Origin.Norse, CombatRole.Mage, Rarity.Legendary, "Seidr threads cross the broken sky."),
            new HeroDefinition("thor", "Thor", Origin.Norse, CombatRole.Warrior, Rarity.Legendary, "Thunder cleaves rifts rather than mountains."),
            new HeroDefinition("odin", "Odin", Origin.Norse, CombatRole.Support, Rarity.Legendary, "A wanderer trading certainty for knowledge."),
            new HeroDefinition("hel", "Hel", Origin.Norse, CombatRole.Controller, Rarity.Epic, "Guardian of the forgotten threshold."),
            new HeroDefinition("skadi", "Skadi", Origin.Norse, CombatRole.Assassin, Rarity.Epic, "A winter huntress who follows no king."),
            new HeroDefinition("amaterasu", "Amaterasu", Origin.EastAsia, CombatRole.Mage, Rarity.Legendary, "A sun spirit weaving dawn across kingdoms."),
            new HeroDefinition("susanoo", "Susanoo", Origin.EastAsia, CombatRole.Warrior, Rarity.Epic, "The storm sword seeks a new oath."),
            new HeroDefinition("sunwukong", "Sun Wukong", Origin.EastAsia, CombatRole.Assassin, Rarity.Legendary, "The trickster leaps across impossible eras."),
            new HeroDefinition("guanyin", "Guanyin", Origin.EastAsia, CombatRole.Healer, Rarity.Legendary, "Mercy offered without allegiance."),
            new HeroDefinition("nezha", "Nezha", Origin.EastAsia, CombatRole.Warrior, Rarity.Epic, "Fire wheels blaze through ruptured horizons."),
            new HeroDefinition("morgana", "Morgana", Origin.Folklore, CombatRole.Controller, Rarity.Legendary, "A sorceress who negotiates with prophecy."),
            new HeroDefinition("merlin", "Merlin", Origin.Folklore, CombatRole.Mage, Rarity.Legendary, "The keeper of an unfinished future."),
            new HeroDefinition("arthur", "Arthur", Origin.Folklore, CombatRole.Tank, Rarity.Epic, "A reluctant king defending those beyond his realm."),
            new HeroDefinition("cleopatra", "Cleopatra", Origin.History, CombatRole.Support, Rarity.Epic, "A fictionalized queen of strategy and diplomacy."),
            new HeroDefinition("joan", "Joan of Arc", Origin.History, CombatRole.Warrior, Rarity.Epic, "A fictionalized standard-bearer between forgotten battles.")
        };

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

        public static readonly string[] StartingHeroes = { "horus", "anubis", "ra", "sobek", "set", "isis", "sekhmet", "nephthys", "bastet", "maat" };
    }
}
