using System;
using System.Collections.Generic;

namespace MythicChronicles
{
    [Serializable]
    public sealed class TowerProgressRecord
    {
        public string origin;
        public int highestFloor;
    }

    public static class ChroniclesTowers
    {
        public const int MaxFloor = 100;
        public const int RequiredOriginHeroes = 2;

        public static readonly Origin[] TowerOrigins =
        {
            Origin.Egypt, Origin.Greece, Origin.Norse, Origin.Japan,
            Origin.China, Origin.Folklore, Origin.History
        };

        public static bool IsTower(Origin origin)
        {
            foreach (Origin tower in TowerOrigins)
                if (tower == origin) return true;
            return false;
        }

        public static string DisplayName(Origin origin)
        {
            return origin == Origin.Egypt ? "EGYPTIAN SPIRE" :
                origin == Origin.Greece ? "OLYMPIAN TOWER" :
                origin == Origin.Norse ? "YGGDRASIL ASCENT" :
                origin == Origin.Japan ? "CELESTIAL MIRROR" :
                origin == Origin.China ? "JADE HEAVEN" :
                origin == Origin.Folklore ? "ENCHANTED KEEP" :
                origin == Origin.History ? "HALL OF LEGENDS" : "UNAVAILABLE";
        }

        public static int Highest(PlayerSave save, Origin origin)
        {
            if (save == null || save.towerProgress == null) return 0;
            TowerProgressRecord record = save.towerProgress.Find(x =>
                x != null && x.origin == origin.ToString());
            return record == null ? 0 : Math.Max(0, Math.Min(MaxFloor, record.highestFloor));
        }

        public static bool CanEnter(PlayerSave save, Origin origin, int floor,
            IList<string> team)
        {
            if (save == null || save.lastClearedStage < 10 ||
                !IsTower(origin) || floor < 1 || floor > MaxFloor ||
                floor != Highest(save, origin) + 1 ||
                team == null || team.Count != 5 || save.heroes == null) return false;
            var unique = new HashSet<string>(StringComparer.Ordinal);
            int matching = 0;
            foreach (string id in team)
            {
                HeroDefinition hero = WorldsCatalog.FindHero(id);
                if (hero == null || !unique.Add(id) ||
                    !save.heroes.Exists(x => x != null && x.id == id)) return false;
                if (hero.origin == origin) matching++;
            }
            return matching >= RequiredOriginHeroes;
        }

        // Existing six chapter environments provide the first playable tower
        // battle arenas. Each tower later receives its own production art.
        public static StageDefinition StageFor(Origin origin, int floor)
        {
            if (!IsTower(origin) || floor < 1 || floor > MaxFloor) return null;
            int chapter = origin == Origin.Egypt ? 1 :
                origin == Origin.Greece ? 2 :
                origin == Origin.Norse ? 3 :
                origin == Origin.Japan || origin == Origin.China ? 4 :
                origin == Origin.Folklore ? 5 : 6;
            int encounter = (floor - 1) % 10 + 1;
            return WorldsCatalog.GetStage((chapter - 1) * 10 + encounter);
        }

        public static TrialReward FloorReward(int floor)
        {
            if (floor < 1 || floor > MaxFloor) return null;
            return new TrialReward(200 + floor * 45,
                10 + (floor % 10 == 0 ? 70 : 0),
                10 + floor / 5,
                floor % 25 == 0 ? 1 : 0);
        }

        public static string Rank(int floor)
        {
            if (floor >= 100) return "ETERNAL SUMMIT";
            if (floor >= 75) return "TOWER LORD";
            if (floor >= 50) return "SKYWALKER";
            if (floor >= 25) return "ASCENDANT";
            if (floor >= 10) return "CLIMBER";
            return "NOVICE";
        }
    }
}
