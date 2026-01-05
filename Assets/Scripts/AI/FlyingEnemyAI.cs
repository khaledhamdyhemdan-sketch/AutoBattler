using UnityEngine;
using AutoBattler.Core;

namespace AutoBattler.AI
{
    /// <summary>
    /// Flying Enemy AI - Fast, evasive, ranged attacker.
    /// Hovers at range, kites away from threats, pecks/strikes from distance.
    /// </summary>
    public class FlyingEnemyAI : EnemyAI_Base
    {
        [Header("Flying Enemy Settings")]
        [SerializeField] private float preferredAttackRange = 6f;
        [SerializeField] private float hoveringHeight = 2f;
        [SerializeField] private float hoverSpeed = 1.5f;
        [SerializeField] private float evadeDistance = 3f;
        
        private float hoverTimer = 0f;
        private float hoverInterval = 1.5f;
        
        protected override void UpdateAI()
        {
            targetEnemy = FindNearestEnemy();
            
            if (targetEnemy == null)
            {
                // Hover in place when no target
                HoverInPlace();
                return;
            }
            
            float distanceToTarget = Vector3.Distance(transform.position, targetEnemy.transform.position);
            
            // Too close - evade backward
            if (distanceToTarget < evadeDistance)
            {
                Vector3 awayDirection = (transform.position - targetEnemy.transform.position).normalized;
                MoveTowards(transform.position + awayDirection * evadeDistance);
            }
            // Perfect range - attack while hovering
            else if (distanceToTarget <= preferredAttackRange)
            {
                HoverNearTarget(targetEnemy);
                TryAttackTarget(targetEnemy);
            }
            // Too far - approach to preferred range
            else if (distanceToTarget < detectionRange)
            {
                MoveTowards(targetEnemy.transform.position);
            }
        }
        
        private void HoverNearTarget(UnitController target)
        {
            // Hover around target at preferred range
            hoverTimer += Time.deltaTime;
            
            if (hoverTimer >= hoverInterval)
            {
                hoverTimer = 0f;
                
                // Circle around target
                Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
                Vector3 sideVector = Vector3.Cross(directionToTarget, Vector3.up);
                Vector3 hoverPos = target.transform.position + (sideVector * 3f) + (Vector3.up * hoveringHeight);
                
                MoveTowards(hoverPos);
            }
        }
        
        private void HoverInPlace()
        {
            // Gentle hovering animation
            transform.position += Vector3.up * Mathf.Sin(Time.time * hoverSpeed) * Time.deltaTime * 0.5f;
        }
    }
}
