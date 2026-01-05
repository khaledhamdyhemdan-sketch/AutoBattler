using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutoBattler.Core.VoidGame.AI
{
    /// <summary>
    /// Unit Behavior Factory - Creates behavior trees for different unit types
    /// Converted from TypeScript UnitBehavior.ts
    /// </summary>
    public static class UnitBehaviorFactory
    {
        #region Combat Constants

        private const float LOW_HEALTH_THRESHOLD = 0.3f;
        private const float RETREAT_DISTANCE = 5f;
        private const float MELEE_ATTACK_RANGE = 2f;
        private const float RANGED_ATTACK_RANGE = 8f;
        private const float RANGED_TOO_CLOSE_RANGE = 3f;
        private const float HEALER_HEAL_RANGE = 10f;
        private const float HEALER_SAFE_DISTANCE = 6f;
        private const float TANK_AGGRO_RANGE = 5f;

        #endregion

        #region Helper Condition Functions

        /// <summary>
        /// Check if unit has low health
        /// </summary>
        public static bool HasLowHealth(AIContext context)
        {
            return (float)context.unit.stats.hp / context.unit.stats.maxHp <= LOW_HEALTH_THRESHOLD;
        }

        /// <summary>
        /// Check if there's an enemy within attack range
        /// </summary>
        public static bool HasEnemyInRange(AIContext context, float range)
        {
            var enemies = SensorySystem.GetEnemiesInRange(context.unit, context.enemies, range);
            return enemies.Count > 0;
        }

        /// <summary>
        /// Check if enemy is too close (for ranged units)
        /// </summary>
        public static bool IsEnemyTooClose(AIContext context, float minRange)
        {
            var closest = SensorySystem.GetClosestEnemy(context.unit, context.enemies);
            if (closest == null)
                return false;

            return SensorySystem.GetDistance(context.unit, closest) < minRange;
        }

        /// <summary>
        /// Check if there's an ally that needs healing
        /// </summary>
        public static bool HasAllyNeedingHealing(AIContext context, float healthThreshold = 0.7f)
        {
            var needsHealing = SensorySystem.GetLowestHealthAllyNeedingHealing(
                context.unit, context.allies, healthThreshold);
            return needsHealing != null;
        }

        /// <summary>
        /// Check if any ability is off cooldown
        /// </summary>
        public static bool HasAbilityReady(AIContext context)
        {
            if (context.unit.abilities == null)
                return false;

            foreach (var ability in context.unit.abilities)
            {
                if (context.currentTime >= ability.lastUsed + ability.cooldown)
                    return true;
            }

            return false;
        }

        #endregion

        #region Helper Action Functions

        /// <summary>
        /// Move toward target position
        /// </summary>
        public static NodeState MoveToPosition(AIContext context, Position targetPos, float stopDistance = 0.5f)
        {
            float distance = SensorySystem.GetDistance(context.unit.position, targetPos);
            
            if (distance <= stopDistance)
                return NodeState.Success;

            var direction = SensorySystem.GetDirection(context.unit.position, targetPos);
            float moveSpeed = context.unit.stats.speed * context.delta;
            
            context.unit.position = new Position(
                context.unit.position.x + direction.x * moveSpeed,
                context.unit.position.z + direction.z * moveSpeed
            );

            // Update Unity transform if available
            context.unit.SyncToUnityTransform();

            return NodeState.Running;
        }

        /// <summary>
        /// Attack the target
        /// </summary>
        public static NodeState AttackTarget(AIContext context, GameUnit target)
        {
            if (target == null || target.IsDead)
                return NodeState.Failure;

            // Check if we can attack (attack speed cooldown)
            float attackInterval = 1f / context.unit.stats.attackSpeed;
            if (context.currentTime < context.unit.lastAttackTime + attackInterval)
                return NodeState.Running;

            // Execute the attack
            context.unit.lastAttackTime = context.currentTime;
            
            if (context.combat != null)
            {
                bool isCrit = UnityEngine.Random.value < context.unit.stats.critChance;
                context.combat.ApplyDamage(
                    context.unit, 
                    target, 
                    context.unit.stats.damage, 
                    context.unit.damageType,
                    context.currentTime,
                    isCrit
                );
            }

            return NodeState.Success;
        }

        /// <summary>
        /// Retreat from enemies
        /// </summary>
        public static NodeState Retreat(AIContext context, float distance)
        {
            var retreatPos = SensorySystem.GetRetreatPosition(context.unit, context.enemies, distance);
            return MoveToPosition(context, retreatPos);
        }

        /// <summary>
        /// Move to optimal ranged attack position
        /// </summary>
        public static NodeState MaintainRange(AIContext context, float optimalRange)
        {
            var target = SensorySystem.GetClosestEnemy(context.unit, context.enemies);
            if (target == null)
                return NodeState.Failure;

            var optimalPos = SensorySystem.GetOptimalRangedPosition(context.unit, target, optimalRange);
            return MoveToPosition(context, optimalPos);
        }

        /// <summary>
        /// Heal an ally
        /// </summary>
        public static NodeState HealAlly(AIContext context, GameUnit target, int healAmount)
        {
            if (target == null || target.IsDead)
                return NodeState.Failure;

            if (context.combat != null)
            {
                context.combat.ApplyHeal(context.unit, target, healAmount);
            }

            return NodeState.Success;
        }

        /// <summary>
        /// Use an ability
        /// </summary>
        public static NodeState UseAbility(AIContext context, Ability ability, GameUnit target = null, Position? position = null)
        {
            if (ability == null)
                return NodeState.Failure;

            // Check cooldown
            if (context.currentTime < ability.lastUsed + ability.cooldown)
                return NodeState.Failure;

            // Execute ability
            ability.lastUsed = context.currentTime;
            
            if (context.combat != null)
            {
                context.combat.ExecuteAbility(context.unit, ability, target, position);
            }

            return NodeState.Success;
        }

        #endregion

        #region Behavior Tree Factories

        /// <summary>
        /// Create melee unit behavior tree
        /// </summary>
        public static BehaviorTree CreateMeleeBehavior()
        {
            var root = new SelectorNode(
                // Priority 1: Retreat if low health
                new SequenceNode(
                    new ConditionNode(HasLowHealth),
                    new ActionNode(ctx => Retreat(ctx, RETREAT_DISTANCE))
                ),
                
                // Priority 2: Attack enemy in range
                new SequenceNode(
                    new ConditionNode(ctx => HasEnemyInRange(ctx, MELEE_ATTACK_RANGE)),
                    new ActionNode(ctx => {
                        var target = SensorySystem.GetClosestEnemy(ctx.unit, ctx.enemies);
                        return AttackTarget(ctx, target);
                    })
                ),
                
                // Priority 3: Move to nearest enemy
                new ActionNode(ctx => {
                    var target = SensorySystem.GetClosestEnemy(ctx.unit, ctx.enemies);
                    if (target == null)
                        return NodeState.Failure;
                    return MoveToPosition(ctx, target.position, MELEE_ATTACK_RANGE * 0.8f);
                })
            );

            return new BehaviorTree("Melee", root);
        }

        /// <summary>
        /// Create ranged unit behavior tree
        /// </summary>
        public static BehaviorTree CreateRangedBehavior()
        {
            var root = new SelectorNode(
                // Priority 1: Retreat if low health
                new SequenceNode(
                    new ConditionNode(HasLowHealth),
                    new ActionNode(ctx => Retreat(ctx, RETREAT_DISTANCE))
                ),
                
                // Priority 2: Kite if enemy too close
                new SequenceNode(
                    new ConditionNode(ctx => IsEnemyTooClose(ctx, RANGED_TOO_CLOSE_RANGE)),
                    new ActionNode(ctx => MaintainRange(ctx, RANGED_ATTACK_RANGE * 0.8f))
                ),
                
                // Priority 3: Attack enemy in range
                new SequenceNode(
                    new ConditionNode(ctx => HasEnemyInRange(ctx, RANGED_ATTACK_RANGE)),
                    new ActionNode(ctx => {
                        var target = SensorySystem.GetClosestEnemy(ctx.unit, ctx.enemies);
                        return AttackTarget(ctx, target);
                    })
                ),
                
                // Priority 4: Move to optimal range
                new ActionNode(ctx => {
                    var target = SensorySystem.GetClosestEnemy(ctx.unit, ctx.enemies);
                    if (target == null)
                        return NodeState.Failure;
                    return MaintainRange(ctx, RANGED_ATTACK_RANGE * 0.7f);
                })
            );

            return new BehaviorTree("Ranged", root);
        }

        /// <summary>
        /// Create healer unit behavior tree
        /// </summary>
        public static BehaviorTree CreateHealerBehavior()
        {
            var root = new SelectorNode(
                // Priority 1: Retreat if in danger
                new SequenceNode(
                    new ConditionNode(ctx => IsEnemyTooClose(ctx, HEALER_SAFE_DISTANCE)),
                    new ActionNode(ctx => Retreat(ctx, RETREAT_DISTANCE))
                ),
                
                // Priority 2: Heal low health ally
                new SequenceNode(
                    new ConditionNode(ctx => HasAllyNeedingHealing(ctx, 0.6f)),
                    new ActionNode(ctx => {
                        var target = SensorySystem.GetLowestHealthAllyNeedingHealing(ctx.unit, ctx.allies, 0.6f);
                        if (target == null)
                            return NodeState.Failure;
                        
                        float distance = SensorySystem.GetDistance(ctx.unit, target);
                        if (distance > HEALER_HEAL_RANGE)
                        {
                            return MoveToPosition(ctx, target.position, HEALER_HEAL_RANGE * 0.8f);
                        }
                        
                        return HealAlly(ctx, target, ctx.unit.stats.damage); // Using damage as heal power
                    })
                ),
                
                // Priority 3: Attack if safe and no healing needed
                new SequenceNode(
                    new ConditionNode(ctx => HasEnemyInRange(ctx, RANGED_ATTACK_RANGE)),
                    new InverterNode(new ConditionNode(ctx => IsEnemyTooClose(ctx, HEALER_SAFE_DISTANCE))),
                    new ActionNode(ctx => {
                        var target = SensorySystem.GetClosestEnemy(ctx.unit, ctx.enemies);
                        return AttackTarget(ctx, target);
                    })
                ),
                
                // Priority 4: Stay near allies
                new ActionNode(ctx => {
                    var allyCenter = SensorySystem.GetGroupCenter(ctx.allies);
                    return MoveToPosition(ctx, allyCenter, 2f);
                })
            );

            return new BehaviorTree("Healer", root);
        }

        /// <summary>
        /// Create tank unit behavior tree
        /// </summary>
        public static BehaviorTree CreateTankBehavior()
        {
            var root = new SelectorNode(
                // Priority 1: Use taunt ability if available and enemies nearby
                new SequenceNode(
                    new ConditionNode(HasAbilityReady),
                    new ConditionNode(ctx => HasEnemyInRange(ctx, TANK_AGGRO_RANGE)),
                    new ActionNode(ctx => {
                        var ability = ctx.unit.abilities?.Find(a => 
                            a.abilityType == AbilityType.Buff && 
                            ctx.currentTime >= a.lastUsed + a.cooldown);
                        
                        if (ability == null)
                            return NodeState.Failure;
                        
                        return UseAbility(ctx, ability);
                    })
                ),
                
                // Priority 2: Attack enemy in range
                new SequenceNode(
                    new ConditionNode(ctx => HasEnemyInRange(ctx, MELEE_ATTACK_RANGE)),
                    new ActionNode(ctx => {
                        // Target the enemy threatening our allies most
                        var target = SensorySystem.GetHighestThreatEnemy(ctx.unit, ctx.enemies);
                        if (target == null)
                            target = SensorySystem.GetClosestEnemy(ctx.unit, ctx.enemies);
                        return AttackTarget(ctx, target);
                    })
                ),
                
                // Priority 3: Intercept enemies heading toward allies
                new ActionNode(ctx => {
                    // Find enemy targeting our weakest ally
                    var weakestAlly = SensorySystem.GetLowestHealthAlly(ctx.unit, ctx.allies);
                    if (weakestAlly == null)
                    {
                        // No ally needs protection, move to nearest enemy
                        var enemy = SensorySystem.GetClosestEnemy(ctx.unit, ctx.enemies);
                        if (enemy == null)
                            return NodeState.Failure;
                        return MoveToPosition(ctx, enemy.position, MELEE_ATTACK_RANGE * 0.8f);
                    }
                    
                    // Position between ally and nearest enemy to ally
                    var threatToAlly = SensorySystem.GetClosestEnemy(weakestAlly, ctx.enemies);
                    if (threatToAlly == null)
                        return NodeState.Failure;
                    
                    // Calculate intercept position
                    var midpoint = new Position(
                        (weakestAlly.position.x + threatToAlly.position.x) / 2f,
                        (weakestAlly.position.z + threatToAlly.position.z) / 2f
                    );
                    
                    return MoveToPosition(ctx, midpoint, MELEE_ATTACK_RANGE * 0.5f);
                })
            );

            return new BehaviorTree("Tank", root);
        }

        /// <summary>
        /// Create enemy melee behavior tree
        /// </summary>
        public static BehaviorTree CreateEnemyMeleeBehavior()
        {
            var root = new SelectorNode(
                // Priority 1: Attack if in range
                new SequenceNode(
                    new ConditionNode(ctx => HasEnemyInRange(ctx, MELEE_ATTACK_RANGE)),
                    new ActionNode(ctx => {
                        // Target lowest health player unit for aggression
                        var target = SensorySystem.GetLowestHealthAlly(ctx.unit, ctx.enemies);
                        if (target == null)
                            target = SensorySystem.GetClosestEnemy(ctx.unit, ctx.enemies);
                        return AttackTarget(ctx, target);
                    })
                ),
                
                // Priority 2: Charge toward target
                new ActionNode(ctx => {
                    var target = SensorySystem.GetLowestHealthAlly(ctx.unit, ctx.enemies);
                    if (target == null)
                        target = SensorySystem.GetClosestEnemy(ctx.unit, ctx.enemies);
                    if (target == null)
                        return NodeState.Failure;
                    return MoveToPosition(ctx, target.position, MELEE_ATTACK_RANGE * 0.8f);
                })
            );

            return new BehaviorTree("EnemyMelee", root);
        }

        /// <summary>
        /// Create enemy ranged behavior tree
        /// </summary>
        public static BehaviorTree CreateEnemyRangedBehavior()
        {
            var root = new SelectorNode(
                // Priority 1: Kite if too close
                new SequenceNode(
                    new ConditionNode(ctx => IsEnemyTooClose(ctx, RANGED_TOO_CLOSE_RANGE)),
                    new ActionNode(ctx => MaintainRange(ctx, RANGED_ATTACK_RANGE * 0.7f))
                ),
                
                // Priority 2: Attack if in range
                new SequenceNode(
                    new ConditionNode(ctx => HasEnemyInRange(ctx, RANGED_ATTACK_RANGE)),
                    new ActionNode(ctx => {
                        // Target healers first, then lowest health
                        var healer = ctx.enemies?.Find(e => e.type == UnitType.Healer && !e.IsDead);
                        var target = healer ?? SensorySystem.GetLowestHealthAlly(ctx.unit, ctx.enemies);
                        if (target == null)
                            target = SensorySystem.GetClosestEnemy(ctx.unit, ctx.enemies);
                        return AttackTarget(ctx, target);
                    })
                ),
                
                // Priority 3: Move to attack range
                new ActionNode(ctx => {
                    var target = SensorySystem.GetClosestEnemy(ctx.unit, ctx.enemies);
                    if (target == null)
                        return NodeState.Failure;
                    return MaintainRange(ctx, RANGED_ATTACK_RANGE * 0.7f);
                })
            );

            return new BehaviorTree("EnemyRanged", root);
        }

        /// <summary>
        /// Create behavior tree based on unit type
        /// </summary>
        public static BehaviorTree CreateBehaviorForUnitType(UnitType unitType)
        {
            return unitType switch
            {
                UnitType.Tank => CreateTankBehavior(),
                UnitType.Dps => CreateMeleeBehavior(),
                UnitType.Healer => CreateHealerBehavior(),
                UnitType.EnemyMelee => CreateEnemyMeleeBehavior(),
                UnitType.EnemyRanged => CreateEnemyRangedBehavior(),
                UnitType.EnemyFlying => CreateEnemyRangedBehavior(), // Flying uses ranged behavior
                UnitType.EnemyBoss => CreateEnemyMeleeBehavior(), // Boss uses enhanced melee
                _ => CreateMeleeBehavior()
            };
        }

        #endregion
    }
}
