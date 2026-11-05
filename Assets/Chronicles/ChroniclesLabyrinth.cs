using System;
using System.Collections.Generic;

namespace MythicChronicles
{
    public enum LabyrinthRoomKind { Battle, Elite, Guardian, Shrine, Treasure, Rest }

    [Serializable]
    public sealed class LabyrinthProgress
    {
        public string weekKey = "";
        public int nextRoom = 1;
        public int lives = 3;
        public int blessingStacks, score;
        public bool completed;
        public string pendingBattleToken = "";
        public int pendingChoice = -1;
    }

    public sealed class LabyrinthOption
    {
        public readonly int choice, stageId, room;
        public readonly LabyrinthRoomKind kind;
        public readonly string title, description;
        public bool RequiresBattle
        {
            get
            {
                return kind == LabyrinthRoomKind.Battle ||
                    kind == LabyrinthRoomKind.Elite ||
                    kind == LabyrinthRoomKind.Guardian;
            }
        }

        public LabyrinthOption(int choice, int room, int stageId,
            LabyrinthRoomKind kind, string title, string description)
        {
            this.choice = choice; this.room = room; this.stageId = stageId;
            this.kind = kind; this.title = title; this.description = description;
        }
    }

    /// <summary>
    /// Twelve-room weekly roguelite challenge with deterministic branches.
    /// This is a local PvE prototype; live time and rewards need a server.
    /// </summary>
    public static class ChroniclesLabyrinth
    {
        public const int Rooms = 12;
        public const int StartingLives = 3;
        public const int MaxBlessings = 4;
        public const float AttackPerBlessing = .045f;

        public static string WeekKey(DateTime utc)
        {
            DateTime date = utc.ToUniversalTime().Date;
            int back = ((int)date.DayOfWeek + 6) % 7;
            return date.AddDays(-back).ToString("yyyy-MM-dd");
        }

        public static string CurrentWeek()
        {
            return WeekKey(DateTime.UtcNow);
        }

        public static int Seed(string week)
        {
            // Stable FNV-1a, unlike string.GetHashCode across processes.
            unchecked
            {
                uint hash = 2166136261;
                if (week != null)
                    foreach (char c in week)
                    {
                        hash ^= c;
                        hash *= 16777619;
                    }
                return (int)(hash & 0x7fffffff);
            }
        }

        public static bool IsGuardianRoom(int room)
        {
            return room == 4 || room == 8 || room == 12;
        }

        public static List<LabyrinthOption> Options(string week, int room)
        {
            var result = new List<LabyrinthOption>();
            if (room < 1 || room > Rooms || string.IsNullOrEmpty(week))
                return result;

            int seed = Seed(week);
            int chapter = (int)(((long)seed + room * 3L) % 6) + 1;
            int encounter = (int)(((long)seed / 7 + room * 5L) % 9) + 1;
            int normalStage = (chapter - 1) * 10 + encounter;
            int guardianStage = chapter * 10;
            if (IsGuardianRoom(room))
            {
                result.Add(new LabyrinthOption(0, room, guardianStage,
                    LabyrinthRoomKind.Guardian, "FRACTURED GUARDIAN",
                    "Defeat a chapter guardian to open the next depth."));
                return result;
            }

            LabyrinthRoomKind left = room == 1 ? LabyrinthRoomKind.Battle :
                (room + seed % 3) % 3 == 0 ?
                    LabyrinthRoomKind.Elite : LabyrinthRoomKind.Battle;
            result.Add(new LabyrinthOption(0, room, normalStage, left,
                left == LabyrinthRoomKind.Elite ? "ELITE ENCOUNTER" :
                    "BATTLE PATH",
                left == LabyrinthRoomKind.Elite ?
                    "Harder opponents, increased first-clear rewards." :
                    "Fight your way forward and collect useful supplies."));

            LabyrinthRoomKind right = room % 3 == 1 ?
                LabyrinthRoomKind.Shrine : room % 3 == 2 ?
                LabyrinthRoomKind.Treasure : LabyrinthRoomKind.Rest;
            result.Add(new LabyrinthOption(1, room, 0, right,
                right == LabyrinthRoomKind.Shrine ? "BLESSING SHRINE" :
                right == LabyrinthRoomKind.Treasure ? "ANCIENT TREASURY" :
                    "SANCTUARY",
                right == LabyrinthRoomKind.Shrine ?
                    "Gain +4.5% party attack for this week's run." :
                right == LabyrinthRoomKind.Treasure ?
                    "Collect a cache of gold and forging materials." :
                    "Restore one lost life, up to the run's maximum."));
            return result;
        }

        public static LabyrinthOption Choice(string week, int room, int choice)
        {
            foreach (LabyrinthOption option in Options(week, room))
                if (option.choice == choice) return option;
            return null;
        }

        public static int RewardGold(LabyrinthOption option)
        {
            if (option == null) return 0;
            return option.kind == LabyrinthRoomKind.Guardian ? 650 + option.room * 90 :
                option.kind == LabyrinthRoomKind.Elite ? 330 + option.room * 50 :
                option.kind == LabyrinthRoomKind.Battle ? 190 + option.room * 30 :
                option.kind == LabyrinthRoomKind.Treasure ? 500 + option.room * 40 : 0;
        }

        public static int RewardForgeDust(LabyrinthOption option)
        {
            if (option == null) return 0;
            return option.kind == LabyrinthRoomKind.Guardian ? 30 + option.room * 3 :
                option.kind == LabyrinthRoomKind.Elite ? 19 :
                option.kind == LabyrinthRoomKind.Treasure ? 27 :
                option.kind == LabyrinthRoomKind.Battle ? 8 : 0;
        }

        public static int RewardSeals(LabyrinthOption option)
        {
            return option != null && option.kind == LabyrinthRoomKind.Guardian ?
                (option.room == 12 ? 2 : 1) : 0;
        }
    }
}
