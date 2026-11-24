using System;
using System.Collections.Generic;

namespace MythicChronicles
{
    /// <summary>
    /// Local-first leaderboards. The player is seeded into a field of
    /// deterministic NPC rivals whose scores drift weekly around the player's
    /// own, so the board feels alive without a server. When a real backend
    /// arrives, only the rival-source function changes; ranks, rewards and
    /// the page stay as they are.
    /// </summary>
    public static class ChroniclesLeaderboards
    {
        public struct Entry
        {
            public string name;
            public int score;
            public bool isPlayer;
        }

        static readonly string[] RivalNames =
        {
            "NileFang", "Solaris", "KemetRose", "StormScribe", "DeltaWolf",
            "AshesOfRa", "MoonlitSphinx", "ThebanGuard", "LotusBlade", "AnkhRunner",
            "ValkyrieSif", "RuneCaster", "JotunBane", "FenrirHowl", "AsgardDawn",
            "OlympianFox", "DelphiSeer", "SpartanVow", "Ambrosia", "Titanfall",
            "SakuraRonin", "KamiWhisper", "Tsukuyomi", "OniHunter", "DragonJade",
            "SilkRoadSage", "BambooShadow", "PhoenixWing", "Terracotta", "JadeEmpress",
            "HorusEye", "SobekJaw", "BastetClaw", "ScarabLord", "PapyrusMage",
            "MemphisKnight", "GizaWatcher", "SaharaWind", "CrocodileDun", "Nefertari"
        };

        static int Hash(string text)
        {
            unchecked
            {
                int h = 5381;
                foreach (char c in text) h = h * 33 + c;
                return h & 0x7fffffff;
            }
        }

        static int WeekSeed(DateTime utcNow)
        {
            int days = (int)(utcNow.Date - new DateTime(2026, 1, 5)).TotalDays;
            return days / 7;
        }

        /// <summary>
        /// Builds a board around the player's score. Each rival scores a
        /// deterministic weekly value in a band around playerScore so ranks
        /// stay contestable: some chase from below, a few sit ahead.
        /// </summary>
        public static List<Entry> Build(string boardId, string playerName,
            int playerScore, DateTime utcNow)
        {
            var entries = new List<Entry>(RivalNames.Length + 1);
            int week = WeekSeed(utcNow);
            for (int i = 0; i < RivalNames.Length; i++)
            {
                int h = Hash(boardId + ":" + RivalNames[i] + ":" + week);
                // Band: -35%..+45% around the player, shaped so ~8 lead.
                float factor = .65f + (h % 1000) / 1000f * .8f;
                int score = Math.Max(1, (int)(playerScore * factor) + (h % 37));
                entries.Add(new Entry { name = RivalNames[i], score = score });
            }
            entries.Add(new Entry
            { name = playerName, score = Math.Max(1, playerScore), isPlayer = true });
            entries.Sort((a, b) => b.score.CompareTo(a.score));
            return entries;
        }

        public static int PlayerRank(List<Entry> board)
        {
            for (int i = 0; i < board.Count; i++)
                if (board[i].isPlayer) return i + 1;
            return board.Count;
        }

        public static string RankRewardLabel(int rank)
        {
            if (rank == 1) return "500 gems";
            if (rank <= 3) return "300 gems";
            if (rank <= 10) return "150 gems";
            if (rank <= 25) return "75 gems";
            return "25 gems";
        }
    }
}
