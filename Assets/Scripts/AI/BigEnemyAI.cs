using UnityEngine;
using AutoBattler.Core;

namespace AutoBattler.AI
{
    /// <summary>
    /// Big Enemy AI - Slow, tanky, high-damage bruiser type.
    /// Charges forward to engage, holds ground, heavy attacks.
    /// </summary>
    public class BigEnemyAI : EnemyAI_Base
    {
        [Header("Big Enemy Settings")]
        [SerializeField] private float chargeRange = 8f;
        [SerializeField] private bool isCharging = false;
        [SerializeField] private float minimumAttackDistance = 1.5f;
        
        protected override void UpdateAI()
        {
            targetEnemy = FindNearestEnemy();
            
            if (targetEnemy == null)
            {
                isCharging = false;
                return;
            }
            
            float distanceToTarget = Vector3.Distance(transform.position, targetEnemy.transform.position);
            
            // Within minimum attack distance - attack
            if (distanceToTarget <= minimumAttackDistance)
            {
                isCharging = false;
                TryAttackTarget(targetEnemy);
            }
            // Within charge range - charge forward
            else if (distanceToTarget <= chargeRange && !isCharging)
            {
                isCharging = true;
                MoveTowards(targetEnemy.transform.position);
            }
            // Far away but within detection - approach slowly
            else if (distanceToTarget < detectionRange)
            {
                isCharging = false;
                MoveTowards(targetEnemy.transform.position);
            }
        }
    }
}
