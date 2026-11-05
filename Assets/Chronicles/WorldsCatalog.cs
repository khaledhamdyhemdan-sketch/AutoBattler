using System;
using System.Collections.Generic;

namespace MythicChronicles
{
    // An original shared world: mythology, folklore and historical legends.
    // "Chronicles" is an INTERNAL working title, not the game's published name.
    public enum Origin { Egypt, Greece, Norse, EastAsia, Folklore, History, Japan, China }
    public enum CombatRole { Tank, Warrior, Assassin, Mage, Support, Healer, Controller }
    // R -> SR -> SSR are launch ranks; the two future ranks remain hidden/locked.
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
            new HeroDefinition("horus", "Horus", Origin.Egypt, CombatRole.Assassin, Rarity.SSR, "A sky sentinel tracking fractures between ages."),
            new HeroDefinition("anubis", "Anubis", Origin.Egypt, CombatRole.Tank, Rarity.SSR, "The gatekeeper carries the names of lost worlds."),
            new HeroDefinition("ra", "Ra", Origin.Egypt, CombatRole.Mage, Rarity.SSR, "His solar barque guides the wandering timelines."),
            new HeroDefinition("sobek", "Sobek", Origin.Egypt, CombatRole.Tank, Rarity.R, "Guardian of the life-bearing river."),
            new HeroDefinition("set", "Set", Origin.Egypt, CombatRole.Warrior, Rarity.SR, "The tempest refuses to serve a false destiny."),
            new HeroDefinition("isis", "Isis", Origin.Egypt, CombatRole.Healer, Rarity.SSR, "She binds shattered destinies with living light."),
            new HeroDefinition("sekhmet", "Sekhmet", Origin.Egypt, CombatRole.Warrior, Rarity.SSR, "Her wrath protects rather than destroys the innocent."),
            new HeroDefinition("nephthys", "Nephthys", Origin.Egypt, CombatRole.Controller, Rarity.SR, "Keeper of the night paths."),
            new HeroDefinition("bastet", "Bastet", Origin.Egypt, CombatRole.Assassin, Rarity.R, "A moonlit hunter between city rooftops."),
            new HeroDefinition("maat", "Maat", Origin.Egypt, CombatRole.Support, Rarity.SSR, "The feather that measures the balance of ages."),
            new HeroDefinition("athena", "Athena", Origin.Greece, CombatRole.Support, Rarity.SSR, "A strategist who reads battles as shifting constellations."),
            new HeroDefinition("artemis", "Artemis", Origin.Greece, CombatRole.Assassin, Rarity.SR, "The huntress pursues stolen moonlight."),
            new HeroDefinition("hades", "Hades", Origin.Greece, CombatRole.Controller, Rarity.SSR, "A sovereign guarding the silence between worlds."),
            new HeroDefinition("persephone", "Persephone", Origin.Greece, CombatRole.Healer, Rarity.SSR, "Bloom and winter answer her call."),
            new HeroDefinition("heracles", "Heracles", Origin.Greece, CombatRole.Warrior, Rarity.SSR, "A champion whose strength must serve wisdom."),
            new HeroDefinition("freyja", "Freyja", Origin.Norse, CombatRole.Mage, Rarity.SSR, "Seidr threads cross the broken sky."),
            new HeroDefinition("thor", "Thor", Origin.Norse, CombatRole.Warrior, Rarity.SSR, "Thunder cleaves rifts rather than mountains."),
            new HeroDefinition("odin", "Odin", Origin.Norse, CombatRole.Support, Rarity.SSR, "A wanderer trading certainty for knowledge."),
            new HeroDefinition("hel", "Hel", Origin.Norse, CombatRole.Controller, Rarity.SR, "Guardian of the forgotten threshold."),
            new HeroDefinition("skadi", "Skadi", Origin.Norse, CombatRole.Assassin, Rarity.SR, "A winter huntress who follows no king."),
            new HeroDefinition("amaterasu", "Amaterasu", Origin.Japan, CombatRole.Mage, Rarity.SSR, "A sun spirit weaving dawn across kingdoms."),
            new HeroDefinition("susanoo", "Susanoo", Origin.Japan, CombatRole.Warrior, Rarity.SR, "The storm sword seeks a new oath."),
            new HeroDefinition("sunwukong", "Sun Wukong", Origin.China, CombatRole.Assassin, Rarity.SSR, "The trickster leaps across impossible eras."),
            new HeroDefinition("guanyin", "Guanyin", Origin.China, CombatRole.Healer, Rarity.SSR, "Mercy offered without allegiance."),
            new HeroDefinition("nezha", "Nezha", Origin.China, CombatRole.Warrior, Rarity.SSR, "Fire wheels blaze through ruptured horizons."),
            new HeroDefinition("morgana", "Morgana", Origin.Folklore, CombatRole.Controller, Rarity.SSR, "A sorceress who negotiates with prophecy."),
            new HeroDefinition("merlin", "Merlin", Origin.Folklore, CombatRole.Mage, Rarity.SSR, "The keeper of an unfinished future."),
            new HeroDefinition("arthur", "Arthur", Origin.Folklore, CombatRole.Tank, Rarity.SSR, "A reluctant king defending those beyond his realm."),
            new HeroDefinition("cleopatra", "Cleopatra", Origin.History, CombatRole.Support, Rarity.SR, "A fictionalized queen of strategy and diplomacy."),
            new HeroDefinition("joan", "Joan of Arc", Origin.History, CombatRole.Warrior, Rarity.SR, "A fictionalized standard-bearer between forgotten battles."),
            // Ten additional UR figures: a Light/Dark pair from each of five mythologies.
            // Light/Dark represent cosmic attunements, not good/evil.
            new HeroDefinition("amun", "Amun", Origin.Egypt, CombatRole.Mage, Rarity.UR, "The hidden creator reveals the first ray beyond time."),
            new HeroDefinition("apep", "Apep", Origin.Egypt, CombatRole.Controller, Rarity.UR, "Primordial serpent of unmaking, bound to the midnight river."),
            new HeroDefinition("zeus", "Zeus", Origin.Greece, CombatRole.Mage, Rarity.UR, "A sky sovereign who calls thunder across divided ages."),
            new HeroDefinition("nyx", "Nyx", Origin.Greece, CombatRole.Controller, Rarity.UR, "The ancient night that existed before any throne."),
            new HeroDefinition("baldr", "Baldr", Origin.Norse, CombatRole.Support, Rarity.UR, "The luminous dream of a world that may yet be reborn."),
            new HeroDefinition("fenrir", "Fenrir", Origin.Norse, CombatRole.Warrior, Rarity.UR, "The great wolf challenges the chains of fate itself."),
            new HeroDefinition("izanagi", "Izanagi", Origin.Japan, CombatRole.Tank, Rarity.UR, "A primordial creator who forges refuge between worlds."),
            new HeroDefinition("izanami", "Izanami", Origin.Japan, CombatRole.Mage, Rarity.UR, "The primordial queen who commands the forgotten threshold."),
            new HeroDefinition("nuwa", "Nüwa", Origin.China, CombatRole.Healer, Rarity.UR, "A divine restorer who mends the scattered sky."),
            new HeroDefinition("hundun", "Hundun", Origin.China, CombatRole.Tank, Rarity.UR, "Embodiment of primordial chaos before form and division.")
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

        // Starter team provides all four ordinary elements and a healer,
        // without granting scarce Light/Dark UR units at sign-up.
        public static readonly string[] StartingHeroes =
            { "sobek", "bastet", "set", "nephthys", "persephone" };
    }
}
