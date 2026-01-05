using UnityEngine;
using UnityEngine.InputSystem;
using AutoBattler.Core;

namespace AutoBattler.Testing
{
    /// <summary>
    /// Quick setup script for testing combat with multiple units
    /// Attach this to a GameObject in the scene for easy battle testing
    /// Uses new Input System
    /// </summary>
    public class QuickBattleSetup : MonoBehaviour
    {
        [Header("Battle Configuration")]
        [SerializeField] private int playerUnitCount = 3;
        [SerializeField] private int enemyUnitCount = 3;
        [SerializeField] private bool setupOnStart = true;
        [SerializeField] private bool autoStartCombat = false;
        [SerializeField] private float autoStartDelay = 2f;

        [Header("Keyboard Controls (New Input System)")]
        [Tooltip("Key to spawn player unit (default: P)")]
        [SerializeField] private Key spawnPlayerKey = Key.P;
        [Tooltip("Key to spawn enemy unit (default: E)")]
        [SerializeField] private Key spawnEnemyKey = Key.E;
        [Tooltip("Key to start combat (default: Space)")]
        [SerializeField] private Key startCombatKey = Key.Space;
        [Tooltip("Key to clear all units (default: C)")]
        [SerializeField] private Key clearUnitsKey = Key.C;

        private void Start()
        {
            if (setupOnStart)
            {
                SetupBattle();

                if (autoStartCombat)
                {
                    Invoke(nameof(StartCombat), autoStartDelay);
                }
            }
        }

        private void Update()
        {
            HandleKeyboardInput();
        }

        private void HandleKeyboardInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // Spawn single player unit
            if (keyboard[spawnPlayerKey].wasPressedThisFrame)
            {
                SpawnPlayerUnit();
            }

            // Spawn single enemy unit
            if (keyboard[spawnEnemyKey].wasPressedThisFrame)
            {
                SpawnEnemyUnit();
            }

            // Start combat
            if (keyboard[startCombatKey].wasPressedThisFrame)
            {
                StartCombat();
            }

            // Clear all units
            if (keyboard[clearUnitsKey].wasPressedThisFrame)
            {
                ClearAllUnits();
            }
        }

        #region Public Methods

        [ContextMenu("Setup Battle")]
        public void SetupBattle()
        {
            if (UnitSpawner.Instance != null)
            {
                UnitSpawner.Instance.SetupBattle(playerUnitCount, enemyUnitCount);
                Debug.Log($"[QuickBattleSetup] Battle setup: {playerUnitCount} vs {enemyUnitCount}");
            }
            else
            {
                Debug.LogError("[QuickBattleSetup] UnitSpawner.Instance not found!");
            }
        }

        [ContextMenu("Start Combat")]
        public void StartCombat()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TransitionToPhase(GamePhase.Combat);
                Debug.Log("[QuickBattleSetup] Combat started!");
            }
            else
            {
                Debug.LogError("[QuickBattleSetup] GameManager.Instance not found!");
            }
        }

        [ContextMenu("Spawn Player Unit")]
        public void SpawnPlayerUnit()
        {
            if (UnitSpawner.Instance != null)
            {
                UnitSpawner.Instance.SpawnPlayerUnit();
            }
        }

        [ContextMenu("Spawn Enemy Unit")]
        public void SpawnEnemyUnit()
        {
            if (UnitSpawner.Instance != null)
            {
                UnitSpawner.Instance.SpawnEnemyUnit();
            }
        }

        [ContextMenu("Clear All Units")]
        public void ClearAllUnits()
        {
            if (UnitSpawner.Instance != null)
            {
                UnitSpawner.Instance.ClearAllUnits();
                Debug.Log("[QuickBattleSetup] All units cleared!");
            }
        }

        #endregion

        private void OnGUI()
        {
            // Display controls on screen
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.normal.textColor = Color.white;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("=== Battle Controls ===", style);
            GUILayout.Label($"[{spawnPlayerKey}] - Spawn Player Unit", style);
            GUILayout.Label($"[{spawnEnemyKey}] - Spawn Enemy Unit", style);
            GUILayout.Label($"[{startCombatKey}] - Start Combat", style);
            GUILayout.Label($"[{clearUnitsKey}] - Clear All Units", style);
            GUILayout.EndArea();
        }
    }
}
