#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MythicChronicles.Editor
{
    public static class ChroniclesTowerVerification
    {
        [MenuItem("Tools/Chronicles/Test Seven Faction Towers")]
        public static void Run()
        {
            string folder = Path.Combine(Path.GetTempPath(),
                "chronicles_towers_test_" + Guid.NewGuid().ToString("N"));
            try
            {
                var player = new PlayerProgressService(folder);
                Verify(ChroniclesTowers.TowerOrigins.Length == 7,
                    "There must be seven distinct faction towers.");
                Verify(!ChroniclesTowers.IsTower(Origin.EastAsia),
                    "Japan and China should have independent towers.");
                Verify(!player.CanEnterTower(Origin.Egypt, 1,
                    player.Data.selectedTeam),
                    "Towers must unlock after normal chapter one.");

                for (int id = 1; id <= 10; id++)
                    Verify(player.CompleteStage(id),
                        "Normal chapter one did not unlock tower floor one.");
                Verify(player.CanEnterTower(Origin.Egypt, 1,
                    player.Data.selectedTeam),
                    "Two Egyptian starter heroes should meet tower origin conditions.");
                Verify(!player.CanEnterTower(Origin.History, 1,
                    player.Data.selectedTeam),
                    "Other tower origins must require their own heroes.");
                Verify(!player.CanEnterTower(Origin.Egypt, 2,
                    player.Data.selectedTeam),
                    "Cannot skip the first floor.");

                int goldBefore = player.Data.gold;
                Verify(player.CompleteTower(Origin.Egypt, 1,
                    player.Data.selectedTeam), "Floor one failed.");
                Verify(ChroniclesTowers.Highest(player.Data, Origin.Egypt) == 1 &&
                    player.Data.gold > goldBefore,
                    "Tower floor progress or reward did not register.");
                int goldAfter = player.Data.gold;
                Verify(!player.CompleteTower(Origin.Egypt, 1,
                    player.Data.selectedTeam) && player.Data.gold == goldAfter,
                    "A tower floor may not pay a duplicate first-clear reward.");
                Verify(player.CanEnterTower(Origin.Egypt, 2,
                    player.Data.selectedTeam), "Floor two should now unlock.");

                // Ensure five heroes with only one Egyptian cannot enter.
                foreach (string id in new[] { "athena", "freyja", "thor", "guanyin" })
                    player.Data.heroes.Add(new OwnedHero { id = id });
                string[] wrong = { "sobek", "athena", "freyja", "thor", "guanyin" };
                Verify(!player.CanEnterTower(Origin.Egypt, 2, wrong),
                    "Lineup with fewer than two matching heroes should be rejected.");
                Verify(ChroniclesTowers.StageFor(Origin.China, 17) != null &&
                    ChroniclesTowers.StageFor(Origin.Japan, 17) != null,
                    "Chinese and Japanese tower stages must be available.");
                Verify(ChroniclesTowers.Rank(100) == "ETERNAL SUMMIT",
                    "Last tower rank must unlock at floor 100.");

                Verify(player.Save(), "Tower progress save failed.");
                var restored = new PlayerProgressService(folder);
                Verify(ChroniclesTowers.Highest(restored.Data, Origin.Egypt) == 1,
                    "Tower rank did not persist across reload.");
                Debug.Log("[Chronicles] Faction tower offline checks passed. " +
                    "No online leaderboard or completed final art is certified.");
            }
            finally
            {
                try
                {
                    if (Directory.Exists(folder)) Directory.Delete(folder, true);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[Chronicles] Tower test cleanup: " + ex.Message);
                }
            }
        }

        private static void Verify(bool ok, string message)
        {
            if (!ok) throw new InvalidOperationException(message);
        }
    }
}
#endif
