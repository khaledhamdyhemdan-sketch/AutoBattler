using System;

namespace MythicChronicles
{
    [Serializable]
    public sealed class StoryMoment
    {
        public string speaker, line, scene, musicCue;
        public StoryMoment(string speaker, string line, string scene, string musicCue)
        { this.speaker = speaker; this.line = line; this.scene = scene; this.musicCue = musicCue; }
    }

    // 60 authored encounter beats. The world continuity belongs to this game,
    // not to any external hero-collector reference.
    public static class WorldsStory
    {
        private static readonly string[][] Lines =
        {
            new[]
            {
                "The dawn arrives twice over the Nile.", "Horus finds a second sun reflected in the river.",
                "Bastet hears a child calling from a city that does not exist.",
                "Anubis discovers names missing from the book of the departed.",
                "Maat's feather turns black for the first time.",
                "Isis gathers shards of an impossible calendar.",
                "Set's storm tears open the path to a foreign shore.",
                "Sekhmet confronts a legion made of living ink.",
                "Ra recognizes the mark of the Unwritten King.",
                "A nameless scribe offers the first stolen thread."
            },
            new[]
            {
                "A silent oracle speaks of six broken horizons.",
                "Athena examines a map of cities that never stood.",
                "Artemis follows a moon that rises in daylight.",
                "The stone statues begin to recite another world's history.",
                "Hades seals a gate before the forgotten dead cross it.",
                "Persephone's spring flowers bloom as stars.",
                "Heracles lifts a fallen bridge between centuries.",
                "An ancient council debates whether memory is truth.",
                "Athena chooses alliance over another war of gods.",
                "The Marble Oracle reveals the second thread."
            },
            new[]
            {
                "Snow falls through the branches of a wounded Yggdrasil.",
                "Odin's ravens return with stories from other pantheons.",
                "Skadi hunts a creature that casts no shadow.",
                "Thor hears thunder beneath a sea with no shore.",
                "Hel guards an army of unfinished memories.",
                "Freyja traces a spell through the roots of time.",
                "A bridge of frost joins the Nile to Asgard.",
                "The dead and living must protect the same gate.",
                "Odin gives up a prophecy to save the present.",
                "The Rootbound Warden releases the third thread."
            },
            new[]
            {
                "The palace mirror reflects a dawn from another century.",
                "Amaterasu feels a darkness behind the borrowed sun.",
                "Susanoo's storm sword finds the first mirror shard.",
                "Sun Wukong leaps through a corridor of changing seasons.",
                "Guanyin offers mercy to an enemy caught between ages.",
                "Nezha's fire wheels burn a path across the mist.",
                "The mirror splits one hero into a thousand possibilities.",
                "The companions reject every false future offered to them.",
                "The false dawn begins to fade.",
                "The Mirror Dragon gives up the fourth thread."
            },
            new[]
            {
                "A mist swallows the road to an unrecorded kingdom.",
                "Arthur finds a sword bearing a name he never knew.",
                "Morgana hears the voice of a storyteller beyond the veil.",
                "Merlin discovers a prophecy written after its ending.",
                "A throne room appears in six places at once.",
                "The knights defend a village forgotten by its own people.",
                "Morgana and Athena disagree on the price of certainty.",
                "The court chooses to write a new oath.",
                "The mist turns into a river of ink.",
                "The Hollow Knight returns the fifth thread."
            },
            new[]
            {
                "Every road leads to the Hall of Unwritten Days.",
                "Cleopatra proposes an alliance no empire could command.",
                "Joan raises a banner for those without a homeland.",
                "Maat measures the cost of stitching worlds together.",
                "The heroes see histories in which they were never born.",
                "The Unwritten King promises an end to every conflict.",
                "Anubis refuses a peace built on erased lives.",
                "The six threads return to the broken Loom.",
                "Each world chooses to keep its own stories.",
                "The final battle decides whether tomorrow can be written."
            }
        };

        private static readonly string[][] Speakers =
        {
            new[] { "Narrator","Horus","Bastet","Anubis","Maat","Isis","Set","Sekhmet","Ra","The Scribe" },
            new[] { "Narrator","Athena","Artemis","Narrator","Hades","Persephone","Heracles","Narrator","Athena","The Oracle" },
            new[] { "Narrator","Odin","Skadi","Thor","Hel","Freyja","Narrator","Narrator","Odin","The Warden" },
            new[] { "Narrator","Amaterasu","Susanoo","Sun Wukong","Guanyin","Nezha","Narrator","Narrator","Amaterasu","The Dragon" },
            new[] { "Narrator","Arthur","Morgana","Merlin","Narrator","Arthur","Morgana","The Court","Narrator","The Knight" },
            new[] { "Narrator","Cleopatra","Joan of Arc","Maat","Narrator","The Unwritten King","Anubis","Narrator","Narrator","Narrator" }
        };

        public static StoryMoment Get(int stageId)
        {
            if (stageId < 1 || stageId > 60) return null;
            int chapter = (stageId - 1) / 10;
            int encounter = (stageId - 1) % 10;
            string scene = WorldsCatalog.Chapters[chapter].title;
            return new StoryMoment(Speakers[chapter][encounter], Lines[chapter][encounter],
                scene, encounter == 9 ? "boss_sting" : "story_ambient");
        }
    }
}
