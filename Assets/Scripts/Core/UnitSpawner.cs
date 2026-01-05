using UnityEngine;
using System.Collections.Generic;
using AutoBattler.Data;

namespace AutoBattler.Core
{
    /// <summary>
    /// Handles spawning of player and enemy units
    /// </summary>
    public class UnitSpawner : MonoBehaviour
    {
        public static UnitSpawner Instance { get; private set; }

        [Header("Unit Prefabs")]
        [SerializeField] private GameObject unitPrefab;

        [Header("Spawn Settings")]
        [SerializeField] private Transform playerSpawnParent;
        [SerializeField] private Transform enemySpawnParent;

        [Header("Default Unit Stats")]
        [SerializeField] private UnitStatsSO defaultPlayerStats;
        [SerializeField] private UnitStatsSO defaultEnemyStats;

        [Header("Spawn Positions")]
        [SerializeField] private Vector3 playerSpawnAreaCenter = new Vector3(-5f, 0f, 0f);
        [SerializeField] private Vector3 enemySpawnAreaCenter = new Vector3(5f, 0f, 0f);
        [SerializeField] private float spawnAreaWidth = 4f;
        [SerializeField] private float spawnAreaDepth = 4f;

        private List<UnitController> spawnedPlayerUnits = new List<UnitController>();
        private List<UnitController> spawnedEnemyUnits = new List<UnitController>();

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            // Create parent transforms if not set
            if (playerSpawnParent == null)
            {
                var playerParentObj = new GameObject("PlayerUnits");
                playerParentObj.transform.SetParent(transform);
                playerSpawnParent = playerParentObj.transform;
            }

            if (enemySpawnParent == null)
            {
                var enemyParentObj = new GameObject("EnemyUnits");
                enemyParentObj.transform.SetParent(transform);
                enemySpawnParent = enemyParentObj.transform;
            }
        }

        #endregion

        #region Spawn Methods

        /// <summary>
        /// Spawn a player unit at a random position in the player spawn area
        /// </summary>
        public UnitController SpawnPlayerUnit(UnitStatsSO stats = null)
        {
            return SpawnUnit(stats ?? defaultPlayerStats, UnitTeam.Player, playerSpawnParent, playerSpawnAreaCenter);
        }

        /// <summary>
        /// Spawn an enemy unit at a random position in the enemy spawn area
        /// </summary>
        public UnitController SpawnEnemyUnit(UnitStatsSO stats = null)
        {
            return SpawnUnit(stats ?? defaultEnemyStats, UnitTeam.Enemy, enemySpawnParent, enemySpawnAreaCenter);
        }

        /// <summary>
        /// Spawn multiple player units
        /// </summary>
        public List<UnitController> SpawnPlayerUnits(int count, UnitStatsSO stats = null)
        {
            List<UnitController> units = new List<UnitController>();
            
            for (int i = 0; i < count; i++)
            {
                var unit = SpawnPlayerUnit(stats);
                if (unit != null)
                {
                    units.Add(unit);
                }
            }

            return units;
        }

        /// <summary>
        /// Spawn multiple enemy units
        /// </summary>
        public List<UnitController> SpawnEnemyUnits(int count, UnitStatsSO stats = null)
        {
            List<UnitController> units = new List<UnitController>();
            
            for (int i = 0; i < count; i++)
            {
                var unit = SpawnEnemyUnit(stats);
                if (unit != null)
                {
                    units.Add(unit);
                }
            }

            return units;
        }

        private UnitController SpawnUnit(UnitStatsSO stats, UnitTeam team, Transform parent, Vector3 spawnCenter)
        {
            if (unitPrefab == null)
            {
                Debug.LogError("[UnitSpawner] Unit prefab not assigned!");
                return null;
            }

            if (stats == null)
            {
                Debug.LogError($"[UnitSpawner] No stats provided for {team} unit!");
                return null;
            }

            // Calculate random spawn position
            Vector3 spawnPosition = CalculateSpawnPosition(spawnCenter);

            // Instantiate unit
            GameObject unitObj = Instantiate(unitPrefab, spawnPosition, Quaternion.identity, parent);
            unitObj.name = $"{stats.unitName}_{team}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";

            // Setup unit controller
            UnitController unit = unitObj.GetComponent<UnitController>();
            if (unit == null)
            {
                unit = unitObj.AddComponent<UnitController>();
            }

            // Set unit properties via reflection or direct assignment
            SetUnitProperties(unit, stats, team);

            // Register with combat manager
            if (CombatManager.Instance != null)
            {
                if (team == UnitTeam.Player)
                {
                    CombatManager.Instance.RegisterPlayerUnit(unit);
                    spawnedPlayerUnits.Add(unit);
                }
                else
                {
                    CombatManager.Instance.RegisterEnemyUnit(unit);
                    spawnedEnemyUnits.Add(unit);
                }
            }

            Debug.Log($"[UnitSpawner] Spawned {team} unit: {stats.unitName} at {spawnPosition}");

            return unit;
        }

        private Vector3 CalculateSpawnPosition(Vector3 center)
        {
            // Random position within spawn area
            float randomX = Random.Range(-spawnAreaWidth / 2, spawnAreaWidth / 2);
            float randomZ = Random.Range(-spawnAreaDepth / 2, spawnAreaDepth / 2);

            return center + new Vector3(randomX, 0f, randomZ);
        }

        private void SetUnitProperties(UnitController unit, UnitStatsSO stats, UnitTeam team)
        {
            // Use reflection to set private fields
            var unitType = typeof(UnitController);
            
            var statsField = unitType.GetField("unitStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (statsField != null)
            {
                statsField.SetValue(unit, stats);
            }

            var teamField = unitType.GetField("team", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (teamField != null)
            {
                teamField.SetValue(unit, team);
            }

            // Trigger initialization
            var initMethod = unitType.GetMethod("InitializeFromStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (initMethod != null)
            {
                initMethod.Invoke(unit, null);
            }
        }

        #endregion

        #region Cleanup

        public void ClearAllUnits()
        {
            ClearPlayerUnits();
            ClearEnemyUnits();
        }

        public void ClearPlayerUnits()
        {
            foreach (var unit in spawnedPlayerUnits)
            {
                if (unit != null)
                {
                    Destroy(unit.gameObject);
                }
            }
            spawnedPlayerUnits.Clear();
        }

        public void ClearEnemyUnits()
        {
            foreach (var unit in spawnedEnemyUnits)
            {
                if (unit != null)
                {
                    Destroy(unit.gameObject);
                }
            }
            spawnedEnemyUnits.Clear();
        }

        #endregion

        #region Quick Setup Methods

        /// <summary>
        /// Setup a basic battle scenario with specified number of units on each side
        /// </summary>
        public void SetupBattle(int playerUnitCount, int enemyUnitCount)
        {
            ClearAllUnits();

            SpawnPlayerUnits(playerUnitCount);
            SpawnEnemyUnits(enemyUnitCount);

            Debug.Log($"[UnitSpawner] Battle setup complete - {playerUnitCount} player units vs {enemyUnitCount} enemy units");
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // Draw player spawn area
            Gizmos.color = Color.blue;
            DrawSpawnAreaGizmo(playerSpawnAreaCenter, spawnAreaWidth, spawnAreaDepth);

            // Draw enemy spawn area
            Gizmos.color = Color.red;
            DrawSpawnAreaGizmo(enemySpawnAreaCenter, spawnAreaWidth, spawnAreaDepth);
        }

        private void DrawSpawnAreaGizmo(Vector3 center, float width, float depth)
        {
            Vector3 size = new Vector3(width, 0.1f, depth);
            Gizmos.DrawWireCube(center, size);
        }

        #endregion
    }
}
