using System;
using System.Collections.Generic;

namespace MythicChronicles
{
    /// <summary>
    /// LOCAL-FIRST TOURNAMENT PROTOTYPE. A weekly 8-seat single-elimination
    /// bracket: the player plus seven deterministic NPC rivals. The player
    /// fights real battles each round (quarterfinal, semifinal, final);
    /// NPC-vs-NPC results resolve deterministically so the bracket feels
    /// alive offline. Swaps to live brackets when the backend arrives.
    /// </summary>
    public static class ChroniclesTournament
    {
        public const int Rounds = 3; // QF, SF, F

        public static readonly string[] RivalNames =
        {
            "NileFang", "Solaris", "KemetRose", "StormScribe",
            "DeltaWolf", "AshesOfRa", "MoonlitSphinx"
        };

        public static readonly string[] RoundNames = { "QUARTERFINAL", "SEMIFINAL", "FINAL" };

        public static string WeekKey(DateTime utcNow)
        {
            DateTime today = utcNow.Date;
            int daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
            return today.AddDays(-daysSinceMonday).ToString("yyyy-MM-dd");
        }

        public static DateTime WeekEnd(DateTime utcNow)
        {
            DateTime today = utcNow.Date;
            int daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
            return today.AddDays(7 - daysSinceMonday);
        }

        static int Hash(string text)
        {
            unchecked
            {
                int h = 5381;
                foreach (char c in text) h = h * 33 + c;
                return h & 0x7fffffff;
            }
        }

        // The rival the player faces in a round (0-based round).
        public static string OpponentFor(string weekKey, int round)
        {
            int h = Hash("tourn:" + weekKey + ":" + round);
            return RivalNames[h % RivalNames.Length];
        }

        // Enemy difficulty grows per round, on the arena stage ladder.
        public static int StageFor(int round)
        {
            return ChroniclesArenaTraining.StageFor(1200 + round * 250, round + 1);
        }

        public static int EnemyRatingFor(int round)
        {
            return 1200 + round * 250;
        }

        public static string PlacementLabel(int roundsWon, bool eliminated)
        {
            if (roundsWon >= Rounds) return "CHAMPION";
            if (!eliminated) return RoundNames[roundsWon] + " - in progress";
            switch (roundsWon)
            {
                case 2: return "FINALIST";
                case 1: return "SEMIFINALIST";
                default: return "QUARTERFINALIST";
            }
        }

        public static string RewardLabel(int roundsWon, bool eliminated)
        {
            if (roundsWon >= Rounds) return "300 gems + 2 summon tickets";
            if (!eliminated) return "Finish the bracket to claim";
            switch (roundsWon)
            {
                case 2: return "150 gems + 1 summon ticket";
                case 1: return "75 gems";
                default: return "30 gems";
            }
        }
    }
}
