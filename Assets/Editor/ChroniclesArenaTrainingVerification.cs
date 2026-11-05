#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MythicChronicles.Editor
{
    public static class ChroniclesArenaTrainingVerification
    {
        [MenuItem("Tools/Chronicles/Test Offline Arena Training")]
        public static void Run()
        {
            string folder = Path.Combine(Path.GetTempPath(),
                "chronicles_arena_test_" + Guid.NewGuid().ToString("N"));
            try
            {
                var player = new PlayerProgressService(folder);
                Check(player.BeginArenaTraining() == null,
                    "Arena should be locked until the first chapter is cleared.");
                Check(ChroniclesArenaTraining.Tier(900) == "BRONZE" &&
                    ChroniclesArenaTraining.Tier(1600) == "DIAMOND" &&
                    ChroniclesArenaTraining.Tier(2400) == "MYTHIC",
                    "Offline rank tier thresholds are incorrect.");
                for (int rating = 700; rating <= 5000; rating += 100)
                {
                    int stageId = ChroniclesArenaTraining.StageFor(rating, 2);
                    StageDefinition stage = WorldsCatalog.GetStage(stageId);
                    Check(stage != null && !stage.boss,
                        "Arena NPC match must use a non-guardian stage.");
                }
                player.Data.lastClearedStage = 10;
                Check(player.Save(), "Could not save Arena unlock.");
                string first = player.BeginArenaTraining();
                Check(!string.IsNullOrEmpty(first) &&
                    player.ArenaAttemptsRemaining() == 4,
                    "First NPC training match should consume one of five attempts.");
                Check(player.BeginArenaTraining() == null,
                    "Cannot start overlapping NPC matches.");
                Check(!player.CompleteArenaTraining("invalid-token", true),
                    "Forged match token must not award rating.");
                int gold = player.Data.gold;
                Check(player.CompleteArenaTraining(first, true),
                    "Arena win was not saved.");
                Check(player.Data.arenaTraining.rating == 922 &&
                    player.Data.arenaTraining.wins == 1 &&
                    player.Data.gold == gold + 260,
                    "Victory did not update rating or rewards correctly.");
                Check(!player.CompleteArenaTraining(first, true),
                    "Match cannot be completed or rewarded twice.");

                string second = player.BeginArenaTraining();
                Check(player.CompleteArenaTraining(second, false),
                    "Loss result did not save.");
                Check(player.Data.arenaTraining.rating == 910 &&
                    player.Data.arenaTraining.losses == 1,
                    "Loss must reduce rating by twelve.");

                string third = player.BeginArenaTraining();
                Check(!string.IsNullOrEmpty(third), "Third match missing.");
                var recovered = new PlayerProgressService(folder);
                Check(recovered.ArenaTrainingState().rating == 898 &&
                    recovered.Data.arenaTraining.losses == 2 &&
                    recovered.ArenaAttemptsRemaining() == 2 &&
                    string.IsNullOrEmpty(recovered.Data.arenaTraining.pendingMatchToken),
                    "Interrupted match must count as a loss without consuming more than one attempt.");

                while (recovered.ArenaAttemptsRemaining() > 0)
                {
                    string token = recovered.BeginArenaTraining();
                    Check(!string.IsNullOrEmpty(token) &&
                        recovered.CompleteArenaTraining(token, true),
                        "Remaining NPC matches should complete.");
                }
                Check(recovered.BeginArenaTraining() == null,
                    "Five-match cap must be enforced.");
                var reload = new PlayerProgressService(folder);
                Check(reload.Data.arenaTraining.attemptsToday == 5 &&
                    reload.Data.arenaTraining.wins == 3 &&
                    reload.Data.arenaTraining.losses == 2,
                    "NPC results did not survive disk reload.");
                reload.Data.arenaTraining.utcDay = "2020-01-01";
                Check(reload.Save() && reload.ArenaAttemptsRemaining() == 5,
                    "Daily attempt reset failed.");
                Check(reload.Data.arenaTraining.wins == 3 &&
                    reload.Data.arenaTraining.losses == 2,
                    "Daily attempts reset must not erase historic match record.");
                Debug.Log("[Chronicles] Offline Arena Training state checks passed. " +
                    "These are not online/PvP ranking tests.");
            }
            finally
            {
                try { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
                catch (Exception ex) { Debug.LogWarning(ex.Message); }
            }
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
#endif
