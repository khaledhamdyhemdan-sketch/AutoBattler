using UnityEngine;
using AutoBattler.Core;

namespace AutoBattler.Testing
{
    /// <summary>
    /// Diagnostic script to verify Auto-Battler setup
    /// </summary>
    public class GameSetupTest : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("=== AUTO-BATTLER SETUP TEST ===");
            
            // Check GameManager
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                Debug.Log($"✓ GameManager found - Current Phase: {gameManager.CurrentPhase}");
                Debug.Log($"  Gold: {gameManager.PlayerGold}, Level: {gameManager.PlayerLevel}");
            }
            else
            {
                Debug.LogError("✗ GameManager not found!");
            }

            // Check CombatManager
            var combatManager = CombatManager.Instance;
            if (combatManager != null)
            {
                Debug.Log("✓ CombatManager found");
            }
            else
            {
                Debug.LogError("✗ CombatManager not found!");
            }

            // Check ShopManager
            var shopManager = ShopManager.Instance;
            if (shopManager != null)
            {
                Debug.Log("✓ ShopManager found");
            }
            else
            {
                Debug.LogError("✗ ShopManager not found!");
            }

            // Check BattlefieldGrid
            var battlefieldGrid = FindFirstObjectByType<BattlefieldGrid>();
            if (battlefieldGrid != null)
            {
                Debug.Log("✓ BattlefieldGrid found");
            }
            else
            {
                Debug.LogError("✗ BattlefieldGrid not found!");
            }

            // Check Camera
            var camera = Camera.main;
            if (camera != null)
            {
                var cameraController = camera.GetComponent<BattleCameraController>();
                if (cameraController != null)
                {
                    Debug.Log($"✓ BattleCameraController found - Distance: {cameraController.CurrentDistance:F1}");
                }
                else
                {
                    Debug.LogWarning("! BattleCameraController not found on Main Camera");
                }
            }
            else
            {
                Debug.LogError("✗ Main Camera not found!");
            }

            // Count units
            var playerUnits = GameObject.FindGameObjectsWithTag("Player");
            var enemyUnits = GameObject.FindGameObjectsWithTag("Enemy");
            Debug.Log($"Units: {playerUnits.Length} player units, {enemyUnits.Length} enemy units");

            // Check unit controllers
            var allUnits = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            Debug.Log($"✓ Found {allUnits.Length} UnitController components");
            foreach (var unit in allUnits)
            {
                if (unit.UnitStats != null)
                {
                    Debug.Log($"  - {unit.UnitStats.unitName} (Team: {unit.Team})");
                }
                else
                {
                    Debug.LogWarning($"  - Unit without stats assigned! (Team: {unit.Team})");
                }
            }

            Debug.Log("=== SETUP TEST COMPLETE ===");
        }

        [ContextMenu("Test Phase Transition")]
        public void TestPhaseTransition()
        {
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                Debug.Log($"Current Phase: {gameManager.CurrentPhase}");
                Debug.Log("Triggering phase transition...");
                
                if (gameManager.CurrentPhase == GamePhase.Preparation)
                {
                    // Simulate starting combat
                    Debug.Log("Starting combat phase...");
                }
                else
                {
                    Debug.Log("Already in combat or results phase");
                }
            }
        }

        [ContextMenu("Log Unit Positions")]
        public void LogUnitPositions()
        {
            var allUnits = FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            Debug.Log($"=== Unit Positions ({allUnits.Length} total) ===");
            foreach (var unit in allUnits)
            {
                string name = unit.UnitStats != null ? unit.UnitStats.unitName : "Unknown";
                Debug.Log($"{name} ({unit.Team}): Position {unit.transform.position}");
            }
        }
    }
}
