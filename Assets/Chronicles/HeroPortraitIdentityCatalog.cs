using System;
using System.Collections.Generic;
using UnityEngine;

namespace MythicChronicles
{
    public enum PortraitSpecies { Human, Falcon, Jackal, Lion, Cat, Crocodile, Serpent, Wolf, Cosmic }
    public enum PortraitProp { Staff, Spear, Sword, Bow, Hammer, Trident, Scepter, Scythe, Mirror, Fan, Orb, Book, Banner, Claws, None }
    public enum PortraitCrown { Sun, Pharaoh, Laurel, Horn, Rune, Helm, Halo, Lotus, Crescent, Royal, None }

    /// <summary>
    /// One procedural development-art identity per roster hero. These are fallback
    /// illustrations, not final anime artwork. Production portraits override
    /// them by Resources/Chronicles/Portraits/Static/{id}.
    /// </summary>
    public sealed class PortraitIdentity
    {
        public readonly string heroId;
        public readonly PortraitSpecies species;
        public readonly PortraitProp prop;
        public readonly PortraitCrown crown;
        public readonly bool feminine;
        public readonly Color skin, hair, armor, fabric, highlight;
        public readonly string visualNote;
        public PortraitIdentity(string heroId, PortraitSpecies species,
            PortraitProp prop, PortraitCrown crown, bool feminine,
            string skin, string hair, string armor, string fabric,
            string highlight, string visualNote)
        {
            this.heroId = heroId; this.species = species;
            this.prop = prop; this.crown = crown; this.feminine = feminine;
            this.skin = Hex(skin); this.hair = Hex(hair);
            this.armor = Hex(armor); this.fabric = Hex(fabric);
            this.highlight = Hex(highlight); this.visualNote = visualNote;
        }
        private static Color Hex(string value)
        {
            Color color;
            return ColorUtility.TryParseHtmlString("#" + value, out color)
                ? color : Color.white;
        }
    }

    public static class HeroPortraitIdentityCatalog
    {
        // Generated from shared/heroes/hero-roster.json (see tools/generate-hero-data.mjs).
        public static readonly PortraitIdentity[] All = HeroRosterData.FallbackPortraits;

        private static readonly Dictionary<string, PortraitIdentity> ById = Build();
        private static Dictionary<string, PortraitIdentity> Build()
        {
            var dict = new Dictionary<string, PortraitIdentity>(StringComparer.Ordinal);
            foreach (PortraitIdentity profile in All) dict.Add(profile.heroId, profile);
            return dict;
        }
        public static PortraitIdentity For(string id)
        {
            PortraitIdentity value;
            return id != null && ById.TryGetValue(id, out value) ? value : null;
        }
    }
}
