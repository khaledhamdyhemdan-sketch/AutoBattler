#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MythicChronicles.Editor
{
    public static class ChroniclesLabyrinthVerification
    {
        [MenuItem("Tools/Chronicles/Test Weekly Labyrinth")]
        public static void Run()
        {
            string folder = Path.Combine(Path.GetTempPath(),
                "chronicles_labyrinth_test_" + Guid.NewGuid().ToString("N"));
            try
            {
                Check(ChroniclesLabyrinth.WeekKey(
                    new DateTime(2026, 11, 5, 8, 0, 0, DateTimeKind.Utc)) ==
                    "2026-11-02", "Monday UTC week calculation failed.");
                string currentWeek = ChroniclesLabyrinth.CurrentWeek();
                Check(ChroniclesLabyrinth.Options(currentWeek, 4).Count == 1 &&
                    ChroniclesLabyrinth.Options(currentWeek, 8)[0].kind ==
                        LabyrinthRoomKind.Guardian &&
                    ChroniclesLabyrinth.Options(currentWeek, 12)[0].kind ==
                        LabyrinthRoomKind.Guardian,
                    "Guardian chambers must be exactly at rooms 4, 8 and 12.");
                for (int room = 1; room <= ChroniclesLabyrinth.Rooms; room++)
                {
                    var options = ChroniclesLabyrinth.Options(currentWeek, room);
                    Check(options.Count == (ChroniclesLabyrinth.IsGuardianRoom(room) ? 1 : 2),
                        "Incorrect room branch count for room " + room);
                    foreach (LabyrinthOption option in options)
                        if (option.RequiresBattle)
                            Check(WorldsCatalog.GetStage(option.stageId) != null,
                                "Labyrinth path references nonexistent story stage.");
                }

                var player = new PlayerProgressService(folder);
                Check(player.LabyrinthChoice(0) == null,
                    "Labyrinth must unlock after chapter one.");
                player.Data.lastClearedStage = 10;
                Check(player.Save(), "Cannot save challenge unlock.");
                LabyrinthProgress run = player.LabyrinthState();
                Check(run.nextRoom == 1 && run.lives == 3 && run.blessingStacks == 0,
                    "New week run should start at room 1 with three lives.");

                // First room offers a battle and a shrine; choosing the shrine must
                // give exactly one blessing and cannot be repeated for extra power.
                LabyrinthOption choice = player.LabyrinthChoice(1);
                Check(choice != null && choice.kind == LabyrinthRoomKind.Shrine,
                    "Room 1 must offer a blessing branch.");
                Check(player.ChooseLabyrinthEvent(1),
                    "Taking the blessing branch failed.");
                Check(player.LabyrinthState().nextRoom == 2 &&
                    player.LabyrinthState().blessingStacks == 1,
                    "Shrine choice did not advance once.");
                Check(!player.ChooseLabyrinthEvent(1),
                    "Room two's treasure must not be treated as the old shrine.");
                // Labyrinth room 2 has a treasure route, which should only pay once.
                int priorGold = player.Data.gold;
                Check(player.ChooseLabyrinthEvent(1) &&
                    player.Data.gold > priorGold, "Treasury reward was not granted.");
                int goldAfterTreasury = player.Data.gold;
                Check(player.LabyrinthState().nextRoom == 3 &&
                    player.Data.gold == goldAfterTreasury,
                    "Treasury room did not advance cleanly.");

                // Room 3 is a Rest that cannot add a fourth life.
                Check(player.ChooseLabyrinthEvent(1) &&
                    player.LabyrinthState().lives == 3,
                    "Rest must cap the expedition at three lives.");
                string token = player.BeginLabyrinthBattle(0);
                Check(!string.IsNullOrEmpty(token), "Guardian encounter did not start.");
                Check(player.BeginLabyrinthBattle(0) == null,
                    "Cannot start a second encounter while one is pending.");
                Check(!player.CompleteLabyrinthBattle("fake-token", true),
                    "Forged token must not award guardian treasure.");
                Check(player.CompleteLabyrinthBattle(token, false),
                    "A defeated guardian attempt must be recorded.");
                Check(player.LabyrinthState().nextRoom == 4 &&
                    player.LabyrinthState().lives == 2,
                    "Defeat must spend one life without advancing.");
                Check(!player.CompleteLabyrinthBattle(token, true),
                    "A spent guardian token must not be claimable twice.");

                string repeat = player.BeginLabyrinthBattle(0);
                Check(player.CompleteLabyrinthBattle(repeat, true),
                    "A retried guardian victory must advance once.");
                Check(player.LabyrinthState().nextRoom == 5,
                    "Guardian victory did not unlock room five.");

                // Test an interrupted saved encounter. Closing/reloading the app
                // should cost one life but leave further attempts usable.
                string secondToken = player.BeginLabyrinthBattle(0);
                Check(!string.IsNullOrEmpty(secondToken), "Room five battle missing.");
                var reopened = new PlayerProgressService(folder);
                LabyrinthProgress recovered = reopened.LabyrinthState();
                Check(recovered.nextRoom == 5 && recovered.lives == 1 &&
                    string.IsNullOrEmpty(recovered.pendingBattleToken),
                    "A crashed battle must be cleared and marked as a loss.");

                // Continue on the noncombat routes when possible and defeat
                // the three guardian rooms until room 12 is completed.
                for (int room = recovered.nextRoom; room <= ChroniclesLabyrinth.Rooms; room++)
                {
                    LabyrinthOption side = reopened.LabyrinthChoice(1);
                    if (side != null && !side.RequiresBattle)
                    {
                        Check(reopened.ChooseLabyrinthEvent(1),
                            "Cannot complete noncombat room " + room);
                        continue;
                    }
                    string battleToken = reopened.BeginLabyrinthBattle(0);
                    Check(!string.IsNullOrEmpty(battleToken) &&
                        reopened.CompleteLabyrinthBattle(battleToken, true),
                        "Cannot finish battle room " + room);
                }

                Check(reopened.LabyrinthState().completed &&
                    reopened.LabyrinthState().nextRoom == 13,
                    "All twelve chambers must finish the weekly run.");
                int finalGems = reopened.Data.gems;
                Check(reopened.BeginLabyrinthBattle(0) == null &&
                    !reopened.ChooseLabyrinthEvent(1) &&
                    reopened.Data.gems == finalGems,
                    "Completed weekly run must not be farmable.");
                var restored = new PlayerProgressService(folder);
                Check(restored.LabyrinthState().completed &&
                    restored.Data.gems == finalGems,
                    "Run completion and final rewards did not survive reload.");
                Debug.Log("[Chronicles] Labyrinth progression tests passed. " +
                    "This verifies local rules only; real Unity battles are not simulated.");
            }
            finally
            {
                try { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
                catch (Exception e) { Debug.LogWarning(e.Message); }
            }
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
#endif
