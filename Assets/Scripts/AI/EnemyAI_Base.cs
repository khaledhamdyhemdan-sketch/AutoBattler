using UnityEngine;
using AutoBattler.Core;

namespace AutoBattler.AI
{
    /// <summary>
    /// Base class for all enemy AI behaviors.
    /// Provides foundational pathfinding, targeting, and combat logic.
    /// </summary>
    public abstract class EnemyAI_Base : MonoBehaviour
    {
        [Header("AI Settings")]
        [SerializeField] protected float stoppingDistance = 0.5f;
        [SerializeField] protected float updateInterval = 0.5f;
        [SerializeField] protected float detectionRange = 10f;
        
        protected UnitController unitController;
        protected CombatManager combatManager;
        protected float updateTimer;
        protected UnitController targetEnemy;
        
        protected virtual void Awake()
        {
            unitController = GetComponent<UnitController>();
            combatManager = CombatManager.Instance;
        }
        
        protected virtual void Update()
        {
            if (unitController == null || unitController.IsDead)
                return;
            
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                UpdateAI();
                updateTimer = 0f;
            }
        }
        
        /// <summary>Core AI logic - override in derived classes.</summary>
        protected abstract void UpdateAI();
        
        /// <summary>Find nearest enemy player unit within detection range.</summary>
        protected UnitController FindNearestEnemy()
        {
            UnitController nearest = null;
            float closestDistance = detectionRange;
            
            if (combatManager == null)
                return null;
            
            var playerUnits = combatManager.GetPlayerUnits();
            if (playerUnits == null || playerUnits.Count == 0)
                return null;
            
            foreach (var unit in playerUnits)
            {
                if (unit == null || unit.IsDead)
                    continue;
                
                float distance = Vector3.Distance(transform.position, unit.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    nearest = unit;
                }
            }
            
            return nearest;
        }
        
        /// <summary>Move towards target position.</summary>
        protected void MoveTowards(Vector3 targetPos)
        {
            if (unitController == null)
                return;
            
            unitController.MoveToPosition(targetPos);
        }
        
        /// <summary>Attack the target unit if in range.</summary>
        protected bool TryAttackTarget(UnitController target)
        {
            if (target == null || unitController == null)
                return false;
            
            float distance = Vector3.Distance(transform.position, target.transform.position);
            if (distance <= unitController.UnitStats.attackRange)
            {
                // Set target which will make the unit attack automatically
                unitController.SetTarget(target);
                return true;
            }
            
            return false;
        }
    }
}
