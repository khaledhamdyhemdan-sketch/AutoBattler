using System;
using System.Collections.Generic;
using UnityEngine;

namespace MythicChronicles
{
    public enum PortraitSpecies { Human, Falcon, Jackal, Lion, Cat, Crocodile, Serpent, Wolf, Cosmic }
    public enum PortraitProp { Staff, Spear, Sword, Bow, Hammer, Trident, Scepter, Scythe, Mirror, Fan, Orb, Book, Banner, Claws, None }
    public enum PortraitCrown { Sun, Pharaoh, Laurel, Horn, Rune, Helm, Halo, Lotus, Crescent, Royal, None }

    /// <summary>
    /// 41 individual procedural development-art identities. These are fallback
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
        public static readonly PortraitIdentity[] All =
        {
            // Egypt: ten original heroes, plus the rare Amun and Apep.
            new PortraitIdentity("horus", PortraitSpecies.Falcon, PortraitProp.Spear, PortraitCrown.Pharaoh, false, "BD916B", "142953", "D5B46B", "203B83", "76D9F7", "Sky falcon with lapis wings and a golden protective spear"),
            new PortraitIdentity("anubis", PortraitSpecies.Jackal, PortraitProp.Staff, PortraitCrown.Pharaoh, false, "262736", "171B34", "B89C6E", "231B42", "7D75F2", "Jackal-headed guardian with spectral weighing staff"),
            new PortraitIdentity("ra", PortraitSpecies.Human, PortraitProp.Scepter, PortraitCrown.Sun, false, "BD805D", "11254D", "F8C04F", "2B4294", "FFC45D", "Solar sovereign bearing the Nile barque and falcon-feather mantle"),
            new PortraitIdentity("sobek", PortraitSpecies.Crocodile, PortraitProp.Trident, PortraitCrown.Pharaoh, false, "587E62", "213D3F", "BBAB6D", "285B69", "7CCED8", "River crocodile guardian, bronze armor and Nile spear"),
            new PortraitIdentity("set", PortraitSpecies.Human, PortraitProp.Spear, PortraitCrown.Horn, false, "9C6348", "222041", "D3A26B", "8B393F", "F66A38", "Desert storm warrior with angular helm and sand spear"),
            new PortraitIdentity("isis", PortraitSpecies.Human, PortraitProp.Staff, PortraitCrown.Sun, true, "C58C6B", "171936", "E6C37D", "F2E3D2", "F8D78B", "Winged protector with gold crescent throne and white linen"),
            new PortraitIdentity("sekhmet", PortraitSpecies.Lion, PortraitProp.Claws, PortraitCrown.Sun, true, "C88A50", "C36B32", "D0A25C", "9E3B31", "FE6438", "Lioness of battle in crimson armor and glowing claws"),
            new PortraitIdentity("nephthys", PortraitSpecies.Human, PortraitProp.Orb, PortraitCrown.Crescent, true, "B98E81", "1E213F", "8A79B5", "454080", "B5A5ED", "Night guardian with midnight veil and moonlight"),
            new PortraitIdentity("bastet", PortraitSpecies.Cat, PortraitProp.Claws, PortraitCrown.Royal, true, "252D42", "181729", "BFA66F", "79517C", "A7D5ED", "Feline moon assassin, sleek dark ears and gold collar"),
            new PortraitIdentity("maat", PortraitSpecies.Human, PortraitProp.Fan, PortraitCrown.Halo, true, "B98060", "31233A", "DFC78E", "D2B9C3", "E8E8FA", "Lady of balance with feather halo and measured scales"),
            new PortraitIdentity("amun", PortraitSpecies.Human, PortraitProp.Staff, PortraitCrown.Sun, false, "AF825E", "282435", "E1C080", "E9E0CA", "FAE6AC", "Hidden creator, tall plumed crown, luminous white temple linen"),
            new PortraitIdentity("apep", PortraitSpecies.Serpent, PortraitProp.None, PortraitCrown.Horn, false, "44245C", "21102D", "A46A86", "21152F", "D44AF0", "Giant black-violet cobra wrapped around an eclipsed sun"),
            // Greece.
            new PortraitIdentity("athena", PortraitSpecies.Human, PortraitProp.Spear, PortraitCrown.Helm, true, "C79378", "57413A", "C8C3B8", "576F94", "D6CC81", "Strategist in crested bronze helm with owl and aegis"),
            new PortraitIdentity("artemis", PortraitSpecies.Human, PortraitProp.Bow, PortraitCrown.Crescent, true, "D4A18B", "3D344B", "A4B4A2", "5D7880", "C9E3EE", "Moon huntress with silver bow and woodland cloak"),
            new PortraitIdentity("hades", PortraitSpecies.Human, PortraitProp.Staff, PortraitCrown.Royal, false, "B9998E", "151525", "584C66", "252136", "9A61CC", "Solemn dark underworld king, obsidian crown and Stygian key"),
            new PortraitIdentity("persephone", PortraitSpecies.Human, PortraitProp.Orb, PortraitCrown.Lotus, true, "DBA591", "513449", "A58B98", "A6506E", "8DE2A8", "Spring queen with pomegranate blossoms and winter ivy"),
            new PortraitIdentity("heracles", PortraitSpecies.Human, PortraitProp.Hammer, PortraitCrown.None, false, "B67F5B", "563A2C", "AF966D", "654B39", "F9B96F", "Lion-skin champion with immense club and heroic posture"),
            new PortraitIdentity("zeus", PortraitSpecies.Human, PortraitProp.Scepter, PortraitCrown.Royal, false, "C99A7C", "E6E6E9", "C8B68C", "3A5C91", "BDE7FE", "Olympian lightning sovereign with white beard and sky lightning"),
            new PortraitIdentity("nyx", PortraitSpecies.Human, PortraitProp.Orb, PortraitCrown.Crescent, true, "B895AC", "292148", "7E6692", "24203F", "9674D8", "Ancient star-cloaked primordial night with floating constellations"),
            new PortraitIdentity("apollo", PortraitSpecies.Human, PortraitProp.Bow, PortraitCrown.Laurel, false, "D4A782", "DDB879", "DCBC75", "FFF2D3", "FEDA69", "Laurel-crowned golden-haired solar oracle and archer"),
            // Norse.
            new PortraitIdentity("freyja", PortraitSpecies.Human, PortraitProp.Staff, PortraitCrown.Royal, true, "DBA79D", "CD9E5C", "C1B0A2", "7F466C", "EDB7D2", "Seidr queen with feather mantle, amber necklace and runes"),
            new PortraitIdentity("thor", PortraitSpecies.Human, PortraitProp.Hammer, PortraitCrown.Helm, false, "C08B73", "B9684C", "9BA9BC", "39568A", "65D7FB", "Red-bearded thunder warrior, runic hammer and iron armor"),
            new PortraitIdentity("odin", PortraitSpecies.Human, PortraitProp.Spear, PortraitCrown.Rune, false, "AE988E", "D2D4DF", "8DA5B7", "263A60", "69D5E9", "One-eyed Allfather in fur cloak with Gungnir and two ravens"),
            new PortraitIdentity("hel", PortraitSpecies.Human, PortraitProp.Scythe, PortraitCrown.Crescent, true, "AAA8B4", "282337", "736E82", "292638", "8C7EC8", "Queen of quiet dead, half-living shadow, frost and dark silk"),
            new PortraitIdentity("skadi", PortraitSpecies.Human, PortraitProp.Bow, PortraitCrown.Helm, true, "D0B8AB", "E3DBD1", "99AFBF", "526E89", "A0E3F2", "Winter huntress with white fur and icy longbow"),
            new PortraitIdentity("baldr", PortraitSpecies.Human, PortraitProp.Sword, PortraitCrown.Halo, false, "D6B3A1", "E3CA9D", "EAD9BB", "F3E9D6", "FEE7A5", "Bright Nordic champion with birch-leaf halo and pale cloak"),
            new PortraitIdentity("fenrir", PortraitSpecies.Wolf, PortraitProp.Claws, PortraitCrown.None, false, "8C92AC", "343B5D", "8E8FAB", "292B47", "8AB9FF", "Enormous silver-blue wolf breaking cosmic runic chains"),
            // Japan.
            new PortraitIdentity("amaterasu", PortraitSpecies.Human, PortraitProp.Mirror, PortraitCrown.Sun, true, "E9BFAF", "232032", "D9B87C", "F9E9DC", "FFBA89", "Solar kami with sacred mirror and vermilion ceremonial robes"),
            new PortraitIdentity("susanoo", PortraitSpecies.Human, PortraitProp.Sword, PortraitCrown.Helm, false, "B68E7F", "33364A", "99B2BE", "315C79", "76DAED", "Storm sword-bearer in dark indigo Japanese armor"),
            new PortraitIdentity("izanagi", PortraitSpecies.Human, PortraitProp.Spear, PortraitCrown.Halo, false, "C69F91", "E0D9D1", "C8DAD3", "A8C2C0", "A3EBE7", "Creator of first islands with primordial spear and sea mist"),
            new PortraitIdentity("izanami", PortraitSpecies.Human, PortraitProp.Fan, PortraitCrown.Royal, true, "D0A6A8", "1E1B36", "A65F8E", "3C253C", "D469C2", "Yomi queen in dark lacquered robes with lantern petals"),
            // China.
            new PortraitIdentity("sunwukong", PortraitSpecies.Human, PortraitProp.Staff, PortraitCrown.Royal, false, "BA845D", "8B5737", "CBAB64", "B14E3B", "F8D373", "Monkey king with golden circlet, red scarf and magic staff"),
            new PortraitIdentity("guanyin", PortraitSpecies.Human, PortraitProp.Orb, PortraitCrown.Halo, true, "DDAD9C", "1C1D32", "D8DCC5", "E5E6DA", "A1EACB", "Compassionate lotus-robed healer with jade grace"),
            new PortraitIdentity("nezha", PortraitSpecies.Human, PortraitProp.Spear, PortraitCrown.Halo, false, "DEAA8D", "33212A", "D6A263", "A63E39", "F7984A", "Fiery youth riding twin flaming wheels"),
            new PortraitIdentity("nuwa", PortraitSpecies.Human, PortraitProp.Orb, PortraitCrown.Halo, true, "DAAA93", "2B3337", "83BEB8", "E7E4C9", "A8F9D4", "Sky mender with colorful celestial stones and jade ribbons"),
            new PortraitIdentity("hundun", PortraitSpecies.Cosmic, PortraitProp.Orb, PortraitCrown.None, false, "46275E", "1C1738", "7545A2", "241E3E", "A251E6", "Faceless primordial chaos with nebula shards and violet light"),
            // Folklore and history.
            new PortraitIdentity("morgana", PortraitSpecies.Human, PortraitProp.Orb, PortraitCrown.Royal, true, "C6A6B4", "29213A", "9B729C", "482445", "D57DD8", "Arcane sorceress with thorn crown and mirror sigils"),
            new PortraitIdentity("merlin", PortraitSpecies.Human, PortraitProp.Staff, PortraitCrown.Rune, false, "BCA6A2", "DEE0E1", "A5A4BC", "3E4C78", "98C2FA", "Elder wizard with silver beard, constellation staff and long robe"),
            new PortraitIdentity("arthur", PortraitSpecies.Human, PortraitProp.Sword, PortraitCrown.Royal, false, "C29B85", "755D4B", "B9B7BD", "324F78", "FFE2AB", "Round-table king bearing bright Excalibur and royal cloak"),
            new PortraitIdentity("cleopatra", PortraitSpecies.Human, PortraitProp.Scepter, PortraitCrown.Pharaoh, true, "B78265", "191928", "E0B75A", "2A6F73", "F3CA7A", "Strategic Ptolemaic ruler with lapis jewelry and royal linen"),
            new PortraitIdentity("joan", PortraitSpecies.Human, PortraitProp.Banner, PortraitCrown.Helm, true, "D5B29E", "5B493F", "BFC3C8", "455D84", "F8C990", "Young banner-bearer in silver armor with flame-lit standard")
        };

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
