using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

namespace AutoBattler.Core
{
    /// <summary>
    /// Manages combat simulation with Burst-optimized pathfinding and targeting
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        [Header("Combat Settings")]
        [SerializeField] private float targetUpdateInterval = 0.5f;

        // Unit tracking
        private List<UnitController> playerUnits = new List<UnitController>();
        private List<UnitController> enemyUnits = new List<UnitController>();
        private bool combatActive = false;
        private float targetUpdateTimer;

        // Combat result
        public bool PlayerVictory { get; private set; }

        // Events for UI updates
        public event System.Action<int, int> OnUnitCountsChanged;
        public event System.Action OnCombatEnded;

        // Public accessors
        public int PlayerUnitCount => playerUnits.Count(u => u != null && !u.IsDead);
        public int EnemyUnitCount => enemyUnits.Count(u => u != null && !u.IsDead);
        public List<UnitController> GetPlayerUnits() => playerUnits;
        public List<UnitController> GetEnemyUnits() => enemyUnits;

        public void RestoreAllPlayerHealth()
        {
            foreach (var unit in playerUnits)
            {
                if (unit != null)
                {
                    unit.RestoreHealth();
                }
            }
        }

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
        }

        private void Update()
        {
            if (combatActive)
            {
                UpdateTargeting();
                CheckCombatEnd();
            }
        }

        private void CheckCombatEnd()
        {
            bool playerHasUnits = playerUnits.Any(u => u != null && !u.IsDead);
            bool enemyHasUnits = enemyUnits.Any(u => u != null && !u.IsDead);

            if (!playerHasUnits || !enemyHasUnits)
            {
                EndCombat();
                OnCombatEnded?.Invoke();
            }
        }

        #endregion

        #region Combat Control

        public void StartCombat()
        {
            combatActive = true;
            PlayerVictory = false;
            targetUpdateTimer = 0f;

            // Initial target assignment
            AssignTargetsToAllUnits();

            Debug.Log($"[CombatManager] Combat Started - Player Units: {playerUnits.Count}, Enemy Units: {enemyUnits.Count}");
        }

        public void EndCombat()
        {
            if (!combatActive) return;

            combatActive = false;

            // Determine winner
            bool playerHasUnits = playerUnits.Any(u => u != null && !u.IsDead);
            bool enemyHasUnits = enemyUnits.Any(u => u != null && !u.IsDead);

            PlayerVictory = playerHasUnits && !enemyHasUnits;

            Debug.Log($"[CombatManager] Combat Ended - Player Victory: {PlayerVictory}");

            // Clear dead units
            CleanupDeadUnits();
        }

        #endregion

        #region Unit Registration

        public void RegisterPlayerUnit(UnitController unit)
        {
            if (!playerUnits.Contains(unit))
            {
                playerUnits.Add(unit);
                Debug.Log($"[CombatManager] Registered player unit: {unit.UnitStats.unitName}");
                NotifyUnitCountChanged();
            }
        }

        public void RegisterEnemyUnit(UnitController unit)
        {
            if (!enemyUnits.Contains(unit))
            {
                enemyUnits.Add(unit);
                Debug.Log($"[CombatManager] Registered enemy unit: {unit.UnitStats.unitName}");
                NotifyUnitCountChanged();
            }
        }

        public void OnUnitDied(UnitController unit)
        {
            Debug.Log($"[CombatManager] Unit died: {unit.UnitStats.unitName}");
            
            // Track for roguelike progression (only count enemy kills)
            if (unit != null && enemyUnits.Contains(unit) && RunManager.Instance != null)
            {
                RunManager.Instance.IncrementEnemiesDefeated();
            }

            // Check for combat end condition
            if (combatActive)
            {
                CheckCombatEnd();
            }
        }

        #endregion

        #region Targeting System

        private void UpdateTargeting()
        {
            targetUpdateTimer -= Time.deltaTime;

            if (targetUpdateTimer <= 0f)
            {
                AssignTargetsToAllUnits();
                targetUpdateTimer = targetUpdateInterval;
            }
        }

        private void AssignTargetsToAllUnits()
        {
            // Assign targets to player units
            foreach (var unit in playerUnits)
            {
                if (unit != null && !unit.IsDead)
                {
                    var target = GetNearestEnemy(unit, UnitTeam.Player);
                    if (target != null)
                    {
                        unit.SetTarget(target);
                    }
                }
            }

            // Assign targets to enemy units
            foreach (var unit in enemyUnits)
            {
                if (unit != null && !unit.IsDead)
                {
                    var target = GetNearestEnemy(unit, UnitTeam.Enemy);
                    if (target != null)
                    {
                        unit.SetTarget(target);
                    }
                }
            }
        }

        public UnitController GetNearestEnemy(UnitController unit, UnitTeam team)
        {
            List<UnitController> enemyList = team == UnitTeam.Player ? enemyUnits : playerUnits;

            UnitController nearestEnemy = null;
            float nearestDistance = float.MaxValue;

            foreach (var enemy in enemyList)
            {
                if (enemy != null && !enemy.IsDead)
                {
                    float distance = Vector3.Distance(unit.transform.position, enemy.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = enemy;
                    }
                }
            }

            return nearestEnemy;
        }

        #endregion

        #region Cleanup

        private void CleanupDeadUnits()
        {
            playerUnits.RemoveAll(u => u == null || u.IsDead);
            enemyUnits.RemoveAll(u => u == null || u.IsDead);
            NotifyUnitCountChanged();
        }

        public void ClearAllUnits()
        {
            playerUnits.Clear();
            enemyUnits.Clear();
            NotifyUnitCountChanged();
        }

        private void NotifyUnitCountChanged()
        {
            OnUnitCountsChanged?.Invoke(PlayerUnitCount, EnemyUnitCount);
        }

        #endregion
    }
}
