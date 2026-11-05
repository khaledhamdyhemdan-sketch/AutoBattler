using System;
using System.Collections.Generic;

namespace MythicChronicles
{
    public enum TrialMode { Normal, Elite, Dark }

    public sealed class TrialReward
    {
        public readonly int gold, gems, forgeDust, astralSeals;
        public TrialReward(int gold, int gems, int forgeDust, int astralSeals)
        {
            this.gold = gold; this.gems = gems;
            this.forgeDust = forgeDust; this.astralSeals = astralSeals;
        }
        public string Description
        {
            get
            {
                return gold + " Gold • " + gems + " Gems • " +
                    forgeDust + " Forge Dust" +
                    (astralSeals > 0 ? " • " + astralSeals + " Astral Seals" : "");
            }
        }
    }

    /// <summary>
    /// 60 story battles plus 60 Elite and 60 Dark variations (ten per chapter).
    /// Elite requires the normal chapter boss; Dark requires its Elite boss.
    /// Additional difficulties use independent local first-clear records.
    /// </summary>
    public static class ChroniclesTrials
    {
        public const int Chapters = 6;
        public const int StagesPerChapter = 10;

        public static float EnemyHealthMultiplier(TrialMode mode)
        {
            return mode == TrialMode.Dark ? 2.35f :
                mode == TrialMode.Elite ? 1.65f : 1f;
        }
        public static float EnemyAttackMultiplier(TrialMode mode)
        {
            return mode == TrialMode.Dark ? 2f :
                mode == TrialMode.Elite ? 1.45f : 1f;
        }

        public static bool CanPlay(PlayerSave save, int stageId, TrialMode mode)
        {
            if (save == null || stageId < 1 || stageId > 60) return false;
            if (mode == TrialMode.Normal) return stageId <= save.lastClearedStage + 1;
            if (mode != TrialMode.Elite && mode != TrialMode.Dark) return false;
            if (save.eliteClears == null || save.darkClears == null) return false;
            int chapter = (stageId - 1) / StagesPerChapter + 1;
            int first = (chapter - 1) * StagesPerChapter + 1;
            int last = chapter * StagesPerChapter;
            if (save.lastClearedStage < last) return false;
            if (mode == TrialMode.Elite)
                return stageId == first || save.eliteClears.Contains(stageId - 1);
            if (!save.eliteClears.Contains(last)) return false;
            return stageId == first || save.darkClears.Contains(stageId - 1);
        }

        public static int NextAvailable(PlayerSave save, TrialMode mode, int chapter)
        {
            if (save == null || chapter < 1 || chapter > Chapters ||
                mode == TrialMode.Normal) return -1;
            List<int> cleared = mode == TrialMode.Elite ?
                save.eliteClears : save.darkClears;
            if (cleared == null) return -1;
            int start = (chapter - 1) * StagesPerChapter + 1;
            for (int i = start; i < start + StagesPerChapter; i++)
                if (!cleared.Contains(i)) return CanPlay(save, i, mode) ? i : -1;
            return -1; // completed chapter
        }

        public static TrialReward FirstClearReward(int stageId, TrialMode mode)
        {
            if (stageId < 1 || stageId > 60) return null;
            bool boss = stageId % StagesPerChapter == 0;
            if (mode == TrialMode.Elite)
                return new TrialReward(250 + 25 * stageId,
                    15 + (boss ? 80 : 0), 8 + stageId / 10, boss ? 2 : 0);
            if (mode == TrialMode.Dark)
                return new TrialReward(600 + 50 * stageId,
                    25 + (boss ? 150 : 0), 14 + stageId / 5, boss ? 3 : 0);
            return null;
        }

        public static int Cleared(PlayerSave save, TrialMode mode, int chapter)
        {
            if (save == null || chapter < 1 || chapter > Chapters) return 0;
            if (mode == TrialMode.Normal)
                return Math.Max(0, Math.Min(StagesPerChapter,
                    save.lastClearedStage - (chapter - 1) * StagesPerChapter));
            List<int> records = mode == TrialMode.Elite ?
                save.eliteClears : save.darkClears;
            if (records == null) return 0;
            int start = (chapter - 1) * StagesPerChapter;
            int count = 0;
            for (int id = start + 1; id <= start + StagesPerChapter; id++)
                if (records.Contains(id)) count++;
            return count;
        }

        public static string CampaignRank(PlayerSave save)
        {
            if (save == null) return "WANDERER";
            int cleared = save.lastClearedStage +
                (save.eliteClears == null ? 0 : save.eliteClears.Count) +
                (save.darkClears == null ? 0 : save.darkClears.Count);
            if (cleared >= 180) return "MYTHBOUND";
            if (cleared >= 140) return "REALM GUARDIAN";
            if (cleared >= 100) return "NIGHTBREAKER";
            if (cleared >= 60) return "VANGUARD";
            if (cleared >= 30) return "CONQUEROR";
            if (cleared >= 10) return "TRAILBLAZER";
            return "WANDERER";
        }
    }
}
