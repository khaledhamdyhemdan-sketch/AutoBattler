using System;
using System.Collections.Generic;
using UnityEngine;

namespace MythicChronicles
{
    // Art direction data for the elite UR gallery and future shader / card-fx pipeline.
    // All text describes ORIGINAL interpretation, not canonical mythological alignment.
    public sealed class URCardIdentity
    {
        public readonly string heroId, title, scene, aura, introCue;
        public readonly HeroElement element;
        public readonly Color border, secondary;

        public URCardIdentity(string heroId, string title, HeroElement element,
            string scene, string aura, string introCue, Color border, Color secondary)
        {
            this.heroId = heroId; this.title = title; this.element = element;
            this.scene = scene; this.aura = aura; this.introCue = introCue;
            this.border = border; this.secondary = secondary;
        }
    }

    public static class URCardCatalog
    {
        private static readonly Color Solar = new Color(1f, .80f, .31f);
        private static readonly Color Sky = new Color(.70f, .91f, 1f);
        private static readonly Color Void = new Color(.55f, .28f, .83f);
        private static readonly Color Crimson = new Color(.81f, .20f, .33f);
        private static readonly Dictionary<string, URCardIdentity> Cards = Build();

        private static Dictionary<string, URCardIdentity> Build()
        {
            URCardIdentity[] entries =
            {
                new URCardIdentity("ra", "The Solar Sovereign", HeroElement.Light,
                    "Golden solar barque rising over the cosmic Nile", "Radiant wings and orbiting sun discs",
                    "A second sunrise floods the screen", Solar, Sky),
                new URCardIdentity("amun", "The Hidden Creator", HeroElement.Light,
                    "Emerging from luminous linen veils and temple columns", "Invisible glyphs resolving into gold",
                    "The veiled figure reveals an unseen sun", Solar, Sky),
                new URCardIdentity("apep", "The Horizon Serpent", HeroElement.Darkness,
                    "Primordial serpent coiling around a devoured sun", "Obsidian scales, shattered eclipse",
                    "A solar eclipse closes as a serpent emerges", Void, Crimson),
                new URCardIdentity("apollo", "The Delphic Sun", HeroElement.Light,
                    "Laurel and radiant lyre beside an ancient Greek sanctuary", "White marble, amber arrows and sunlight",
                    "A golden arrow lights an oracle's arch", Solar, Sky),
                new URCardIdentity("zeus", "The Thunder Throne", HeroElement.Light,
                    "Olympian clouds and celestial lightning", "Electric-white bolts and royal gold",
                    "Thunder tears through the heavens", Sky, Solar),
                new URCardIdentity("hades", "King of the Underworld", HeroElement.Darkness,
                    "Obsidian throne beyond the river of shades", "Black iron and deep violet flame",
                    "A silent gate opens below the earth", Void, Crimson),
                new URCardIdentity("nyx", "Primordial Night", HeroElement.Darkness,
                    "Endless constellations in a starless sky", "Indigo void and slowly extinguishing stars",
                    "The sky folds into the first night", Void, Sky),
                new URCardIdentity("baldr", "Hope of the Nine Realms", HeroElement.Light,
                    "Glowing birch grove beneath the northern aurora", "Pearl-white blossoms and amber runes",
                    "An aurora parts to reveal renewed dawn", Sky, Solar),
                new URCardIdentity("odin", "Allfather of the Runes", HeroElement.Light,
                    "The one-eyed wanderer, spear Gungnir, two ravens and Yggdrasil", "Aurora-lit runes and weathered kingly bronze",
                    "The ravens circle as an ancient rune ignites", Sky, Solar),
                new URCardIdentity("hel", "Queen of the Quiet Dead", HeroElement.Darkness,
                    "Gate of Hel's realm beneath frost-covered roots", "Half-living, half-shadow sovereign with pale spectral fire",
                    "The gates of the dead silently open", Void, Sky),
                new URCardIdentity("fenrir", "Fatebreaker Wolf", HeroElement.Darkness,
                    "Enormous wolf tearing free of rune-bound chains", "Abyssal fur, red eclipse and broken silver",
                    "The chain breaks and a dark moon rises", Void, Crimson),
                new URCardIdentity("amaterasu", "Mirror of the Rising Sun", HeroElement.Light,
                    "Sacred mirror framed by a field of vermilion gates", "White-gold rays and soft blossom light",
                    "A celestial mirror restores the morning", Solar, Crimson),
                new URCardIdentity("izanagi", "Maker of the First Shores", HeroElement.Light,
                    "Primordial sea and island creation in luminous mist", "Pearl foam, jade radiance and sacred spear",
                    "The sea rises into an island of light", Sky, Solar),
                new URCardIdentity("izanami", "Sovereign of Yomi", HeroElement.Darkness,
                    "Underworld gate with funeral lanterns and falling petals", "Ink-dark silk, violet flame and antique gold",
                    "A thousand lanterns dim as the Yomi gate opens", Void, Crimson),
                new URCardIdentity("nuwa", "Weaver of the Sky", HeroElement.Light,
                    "A celestial vault repaired with five-colored stones", "Jade dragon ribbons and multicolored starlight",
                    "Fragments of the heavens merge into a luminous whole", Sky, Solar),
                new URCardIdentity("hundun", "Formless Beginning", HeroElement.Darkness,
                    "An unshaped cosmic void before earth and sky", "Dark amethyst nebula with unstable geometric shards",
                    "The void folds inward to form a living presence", Void, Crimson)
            };
            var index = new Dictionary<string, URCardIdentity>(StringComparer.Ordinal);
            foreach (URCardIdentity entry in entries) index.Add(entry.heroId, entry);
            return index;
        }

        public static URCardIdentity For(string heroId)
        {
            URCardIdentity card;
            return heroId != null && Cards.TryGetValue(heroId, out card) ? card : null;
        }

        public static int Count { get { return Cards.Count; } }
    }
}
