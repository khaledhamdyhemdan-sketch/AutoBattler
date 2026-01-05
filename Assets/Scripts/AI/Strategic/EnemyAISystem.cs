using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutoBattler.Core.VoidGame.AI
{
    /// <summary>
    /// AI Action Types for strategic decision making
    /// </summary>
    public enum AIActionType
    {
        Attack,
        Defend,
        Retreat,
        Flank,
        Kite,
        Support,
        Position,
        UseAbility,
        Taunt,
        Focus
    }

    /// <summary>
    /// Situation Assessment Data
    /// </summary>
    [Serializable]
    public struct SituationAssessment
    {
        public float healthPercent;
        public float allyHealthPercent;
        public float enemyHealthPercent;
        public int nearbyEnemies;
        public int nearbyAllies;
        public bool isOutnumbered;
        public bool hasLowHealth;
        public bool alliesNeedHealing;
        public GameUnit closestEnemy;
        public GameUnit lowestHealthAlly;
        public GameUnit highestThreatEnemy;
        public float threatLevel;
        public bool inDanger;
    }

    /// <summary>
    /// AI Personality Configuration
    /// Affects decision-making weights for different strategies
    /// </summary>
    [Serializable]
    public struct AIPersonality
    {
        [Range(0f, 1f)] public float aggression;      // Tendency to attack vs defend
        [Range(0f, 1f)] public float caution;         // Tendency to retreat when threatened
        [Range(0f, 1f)] public float coordination;    // Tendency to work with allies
        [Range(0f, 1f)] public float positioning;     // Emphasis on tactical positioning

        public static AIPersonality Default => new AIPersonality
        {
            aggression = 0.5f,
            caution = 0.5f,
            coordination = 0.5f,
            positioning = 0.5f
        };

        public static AIPersonality Aggressive => new AIPersonality
        {
            aggression = 0.9f,
            caution = 0.2f,
            coordination = 0.3f,
            positioning = 0.3f
        };

        public static AIPersonality Defensive => new AIPersonality
        {
            aggression = 0.3f,
            caution = 0.8f,
            coordination = 0.6f,
            positioning = 0.7f
        };

        public static AIPersonality Tactical => new AIPersonality
        {
            aggression = 0.5f,
            caution = 0.5f,
            coordination = 0.7f,
            positioning = 0.8f
        };

        public static AIPersonality Berserker => new AIPersonality
        {
            aggression = 1.0f,
            caution = 0.0f,
            coordination = 0.1f,
            positioning = 0.1f
        };

        public static AIPersonality Support => new AIPersonality
        {
            aggression = 0.2f,
            caution = 0.7f,
            coordination = 0.9f,
            positioning = 0.6f
        };
    }

    /// <summary>
    /// AI Strategy Result - Contains chosen action and target
    /// </summary>
    [Serializable]
    public struct AIStrategy
    {
        public float priority;
        public AIActionType action;
        public GameUnit target;
        public Position targetPosition;
        public string reason;
        public Ability abilityToUse;

        public override string ToString()
        {
            return $"[{action}] Priority: {priority:F2}, Target: {target?.name ?? "None"}, Reason: {reason}";
        }
    }

    /// <summary>
    /// Enemy AI System - Advanced AI with personalities and strategic decision making
    /// Converted from TypeScript EnemyAISystem.ts
    /// </summary>
    public static class EnemyAISystem
    {
        #region Personality Presets by Unit Type

        private static readonly Dictionary<UnitType, AIPersonality> PersonalityPresets = new Dictionary<UnitType, AIPersonality>
        {
            { UnitType.Tank, new AIPersonality { aggression = 0.6f, caution = 0.3f, coordination = 0.7f, positioning = 0.8f } },
            { UnitType.Dps, new AIPersonality { aggression = 0.8f, caution = 0.4f, coordination = 0.5f, positioning = 0.6f } },
            { UnitType.Healer, new AIPersonality { aggression = 0.2f, caution = 0.8f, coordination = 0.9f, positioning = 0.7f } },
            { UnitType.EnemyMelee, new AIPersonality { aggression = 0.7f, caution = 0.3f, coordination = 0.4f, positioning = 0.4f } },
            { UnitType.EnemyRanged, new AIPersonality { aggression = 0.5f, caution = 0.6f, coordination = 0.5f, positioning = 0.7f } },
            { UnitType.EnemyFlying, new AIPersonality { aggression = 0.6f, caution = 0.5f, coordination = 0.3f, positioning = 0.8f } },
            { UnitType.EnemyBoss, new AIPersonality { aggression = 0.8f, caution = 0.2f, coordination = 0.2f, positioning = 0.5f } },
        };

        /// <summary>
        /// Get personality for a unit type
        /// </summary>
        public static AIPersonality GetPersonalityForType(UnitType type)
        {
            if (PersonalityPresets.TryGetValue(type, out var personality))
                return personality;
            return AIPersonality.Default;
        }

        #endregion

        #region Situation Assessment

        /// <summary>
        /// Assess the current situation for a unit
        /// </summary>
        public static SituationAssessment AssessSituation(GameUnit unit, List<GameUnit> allies, List<GameUnit> enemies)
        {
            var assessment = new SituationAssessment();

            // Unit health
            assessment.healthPercent = (float)unit.stats.hp / unit.stats.maxHp;
            assessment.hasLowHealth = assessment.healthPercent <= 0.3f;

            // Ally health average
            float totalAllyHealth = 0f;
            int allyCount = 0;
            foreach (var ally in allies)
            {
                if (!ally.IsDead && ally.id != unit.id)
                {
                    totalAllyHealth += (float)ally.stats.hp / ally.stats.maxHp;
                    allyCount++;
                }
            }
            assessment.allyHealthPercent = allyCount > 0 ? totalAllyHealth / allyCount : 1f;

            // Enemy health average
            float totalEnemyHealth = 0f;
            int enemyCount = 0;
            foreach (var enemy in enemies)
            {
                if (!enemy.IsDead)
                {
                    totalEnemyHealth += (float)enemy.stats.hp / enemy.stats.maxHp;
                    enemyCount++;
                }
            }
            assessment.enemyHealthPercent = enemyCount > 0 ? totalEnemyHealth / enemyCount : 0f;

            // Nearby counts
            assessment.nearbyEnemies = SensorySystem.GetEnemiesInRange(unit, enemies, 5f).Count;
            assessment.nearbyAllies = SensorySystem.GetAlliesInRange(unit, allies, 5f).Count;
            assessment.isOutnumbered = assessment.nearbyEnemies > assessment.nearbyAllies + 1;

            // Ally needs healing
            assessment.alliesNeedHealing = SensorySystem.GetLowestHealthAllyNeedingHealing(unit, allies, 0.7f) != null;
            assessment.lowestHealthAlly = SensorySystem.GetLowestHealthAlly(unit, allies);

            // Enemy targeting
            assessment.closestEnemy = SensorySystem.GetClosestEnemy(unit, enemies);
            assessment.highestThreatEnemy = SensorySystem.GetHighestThreatEnemy(unit, enemies);
            assessment.threatLevel = SensorySystem.GetTotalThreatLevel(unit, enemies);

            // Danger assessment
            assessment.inDanger = SensorySystem.IsInDanger(unit, enemies);

            return assessment;
        }

        #endregion

        #region Strategy Generation

        /// <summary>
        /// Generate possible strategies based on situation and personality
        /// </summary>
        public static List<AIStrategy> GenerateStrategies(
            GameUnit unit,
            AIPersonality personality,
            SituationAssessment situation,
            List<GameUnit> allies,
            List<GameUnit> enemies)
        {
            var strategies = new List<AIStrategy>();

            // Attack Strategy
            if (situation.closestEnemy != null)
            {
                float attackPriority = personality.aggression * 0.5f;
                
                // Bonus for low enemy health
                attackPriority += (1f - situation.enemyHealthPercent) * 0.2f;
                
                // Penalty for being outnumbered
                if (situation.isOutnumbered)
                    attackPriority -= personality.caution * 0.3f;
                
                // Bonus for high health
                attackPriority += situation.healthPercent * 0.2f;

                strategies.Add(new AIStrategy
                {
                    action = AIActionType.Attack,
                    priority = Mathf.Clamp01(attackPriority),
                    target = situation.closestEnemy,
                    targetPosition = situation.closestEnemy.position,
                    reason = "Attack nearest enemy"
                });
            }

            // Focus High Threat Strategy
            if (situation.highestThreatEnemy != null && situation.highestThreatEnemy != situation.closestEnemy)
            {
                float focusPriority = personality.aggression * 0.4f + personality.coordination * 0.3f;
                focusPriority += situation.threatLevel / 1000f * 0.2f; // Higher threat = higher priority

                strategies.Add(new AIStrategy
                {
                    action = AIActionType.Focus,
                    priority = Mathf.Clamp01(focusPriority),
                    target = situation.highestThreatEnemy,
                    targetPosition = situation.highestThreatEnemy.position,
                    reason = "Focus high threat target"
                });
            }

            // Retreat Strategy
            if (situation.hasLowHealth || situation.inDanger)
            {
                float retreatPriority = personality.caution * 0.6f;
                retreatPriority += (1f - situation.healthPercent) * 0.4f;
                
                if (situation.isOutnumbered)
                    retreatPriority += 0.2f;

                var retreatPos = SensorySystem.GetRetreatPosition(unit, enemies, 5f);
                strategies.Add(new AIStrategy
                {
                    action = AIActionType.Retreat,
                    priority = Mathf.Clamp01(retreatPriority),
                    target = null,
                    targetPosition = retreatPos,
                    reason = situation.hasLowHealth ? "Low health retreat" : "Tactical retreat"
                });
            }

            // Defend/Tank Strategy (for tanks)
            if (unit.type == UnitType.Tank && situation.lowestHealthAlly != null)
            {
                float defendPriority = personality.coordination * 0.5f + (1f - personality.aggression) * 0.3f;
                defendPriority += (1f - situation.allyHealthPercent) * 0.2f;

                // Position between ally and threat
                var threat = SensorySystem.GetClosestEnemy(situation.lowestHealthAlly, enemies);
                if (threat != null)
                {
                    var defendPos = new Position(
                        (situation.lowestHealthAlly.position.x + threat.position.x) / 2f,
                        (situation.lowestHealthAlly.position.z + threat.position.z) / 2f
                    );

                    strategies.Add(new AIStrategy
                    {
                        action = AIActionType.Defend,
                        priority = Mathf.Clamp01(defendPriority),
                        target = situation.lowestHealthAlly,
                        targetPosition = defendPos,
                        reason = "Protect wounded ally"
                    });
                }
            }

            // Support/Heal Strategy (for healers)
            if (unit.type == UnitType.Healer && situation.alliesNeedHealing)
            {
                float supportPriority = personality.coordination * 0.6f + (1f - situation.allyHealthPercent) * 0.4f;

                strategies.Add(new AIStrategy
                {
                    action = AIActionType.Support,
                    priority = Mathf.Clamp01(supportPriority),
                    target = situation.lowestHealthAlly,
                    targetPosition = situation.lowestHealthAlly?.position ?? unit.position,
                    reason = "Heal wounded ally"
                });
            }

            // Flank Strategy
            if (situation.closestEnemy != null && personality.positioning > 0.5f)
            {
                float flankPriority = personality.positioning * 0.4f + personality.aggression * 0.2f;
                
                // Bonus if we have allies (flanking works better with team)
                if (situation.nearbyAllies > 0)
                    flankPriority += personality.coordination * 0.2f;

                var flankPos = SensorySystem.GetFlankingPosition(unit, situation.closestEnemy, 3f);
                strategies.Add(new AIStrategy
                {
                    action = AIActionType.Flank,
                    priority = Mathf.Clamp01(flankPriority),
                    target = situation.closestEnemy,
                    targetPosition = flankPos,
                    reason = "Flank enemy"
                });
            }

            // Kite Strategy (for ranged)
            if ((unit.type == UnitType.EnemyRanged || unit.type == UnitType.EnemyFlying || 
                 unit.type == UnitType.Dps) && situation.nearbyEnemies > 0)
            {
                float distance = situation.closestEnemy != null ? 
                    SensorySystem.GetDistance(unit, situation.closestEnemy) : float.MaxValue;
                
                if (distance < 4f) // Too close for ranged
                {
                    float kitePriority = personality.caution * 0.5f + personality.positioning * 0.3f;
                    kitePriority += (4f - distance) / 4f * 0.2f; // Closer = higher priority to kite

                    var kitePos = SensorySystem.GetOptimalRangedPosition(unit, situation.closestEnemy, 7f);
                    strategies.Add(new AIStrategy
                    {
                        action = AIActionType.Kite,
                        priority = Mathf.Clamp01(kitePriority),
                        target = situation.closestEnemy,
                        targetPosition = kitePos,
                        reason = "Maintain range"
                    });
                }
            }

            // Ability Usage Strategy
            if (unit.abilities != null && unit.abilities.Count > 0)
            {
                foreach (var ability in unit.abilities)
                {
                    // Skip if on cooldown (would need currentTime to check properly)
                    if (ability.lastUsed > 0 && ability.cooldown > 0)
                        continue; // Simplified check

                    float abilityPriority = 0.5f;
                    GameUnit abilityTarget = null;

                    switch (ability.targetType)
                    {
                        case AbilityTarget.Enemy:
                            abilityTarget = situation.closestEnemy;
                            abilityPriority = personality.aggression * 0.6f;
                            break;
                        case AbilityTarget.Ally:
                            abilityTarget = situation.lowestHealthAlly;
                            abilityPriority = personality.coordination * 0.6f;
                            if (situation.alliesNeedHealing)
                                abilityPriority += 0.3f;
                            break;
                        case AbilityTarget.Self:
                            abilityTarget = unit;
                            if (situation.hasLowHealth)
                                abilityPriority += 0.4f;
                            break;
                    }

                    if (abilityTarget != null)
                    {
                        strategies.Add(new AIStrategy
                        {
                            action = AIActionType.UseAbility,
                            priority = Mathf.Clamp01(abilityPriority),
                            target = abilityTarget,
                            targetPosition = abilityTarget.position,
                            abilityToUse = ability,
                            reason = $"Use ability: {ability.name}"
                        });
                    }
                }
            }

            return strategies;
        }

        /// <summary>
        /// Select the best strategy from available options
        /// </summary>
        public static AIStrategy SelectBestStrategy(List<AIStrategy> strategies)
        {
            if (strategies == null || strategies.Count == 0)
            {
                return new AIStrategy
                {
                    action = AIActionType.Attack,
                    priority = 0f,
                    reason = "No valid strategies"
                };
            }

            // Sort by priority (descending)
            strategies.Sort((a, b) => b.priority.CompareTo(a.priority));

            // Add some randomness to avoid predictable behavior
            // Consider top 3 strategies weighted by priority
            int considerCount = Mathf.Min(3, strategies.Count);
            float totalWeight = 0f;
            
            for (int i = 0; i < considerCount; i++)
            {
                totalWeight += strategies[i].priority;
            }

            if (totalWeight <= 0f)
                return strategies[0];

            float roll = UnityEngine.Random.value * totalWeight;
            float cumulative = 0f;

            for (int i = 0; i < considerCount; i++)
            {
                cumulative += strategies[i].priority;
                if (roll <= cumulative)
                    return strategies[i];
            }

            return strategies[0];
        }

        #endregion

        #region Main Evaluation

        /// <summary>
        /// Evaluate AI for a unit and return the best strategy
        /// </summary>
        public static AIStrategy EvaluateAI(
            GameUnit unit,
            List<GameUnit> allies,
            List<GameUnit> enemies,
            AIPersonality? customPersonality = null)
        {
            // Get personality (custom or based on type)
            var personality = customPersonality ?? GetPersonalityForType(unit.type);

            // Assess current situation
            var situation = AssessSituation(unit, allies, enemies);

            // Generate possible strategies
            var strategies = GenerateStrategies(unit, personality, situation, allies, enemies);

            // Select best strategy
            return SelectBestStrategy(strategies);
        }

        /// <summary>
        /// Execute a strategy and return the resulting movement/action
        /// </summary>
        public static void ExecuteStrategy(
            AIContext context,
            AIStrategy strategy,
            out Position targetPosition,
            out GameUnit attackTarget,
            out Ability abilityToUse)
        {
            targetPosition = context.unit.position;
            attackTarget = null;
            abilityToUse = null;

            switch (strategy.action)
            {
                case AIActionType.Attack:
                case AIActionType.Focus:
                    attackTarget = strategy.target;
                    targetPosition = strategy.targetPosition;
                    break;

                case AIActionType.Retreat:
                case AIActionType.Kite:
                case AIActionType.Position:
                case AIActionType.Flank:
                    targetPosition = strategy.targetPosition;
                    break;

                case AIActionType.Defend:
                    targetPosition = strategy.targetPosition;
                    // Tank might also attack nearby threats
                    if (context.enemies != null)
                    {
                        var nearbyEnemy = SensorySystem.GetEnemiesInRange(context.unit, context.enemies, 2f);
                        if (nearbyEnemy.Count > 0)
                            attackTarget = nearbyEnemy[0];
                    }
                    break;

                case AIActionType.Support:
                    targetPosition = strategy.targetPosition;
                    // For healers, the "target" is the ally to heal
                    attackTarget = strategy.target; // Repurposed as heal target
                    break;

                case AIActionType.UseAbility:
                    attackTarget = strategy.target;
                    abilityToUse = strategy.abilityToUse;
                    targetPosition = strategy.targetPosition;
                    break;

                case AIActionType.Taunt:
                    // Draw aggro, stay in position
                    attackTarget = strategy.target;
                    break;
            }
        }

        #endregion
    }

    /// <summary>
    /// AI Controller Component - Manages AI behavior for a game unit
    /// </summary>
    public class AIController
    {
        public GameUnit Unit { get; private set; }
        public AIPersonality Personality { get; set; }
        public BehaviorTree BehaviorTree { get; private set; }
        
        private AIContext context;
        private float lastEvaluationTime;
        private float evaluationInterval = 0.1f; // Evaluate every 100ms
        private AIStrategy currentStrategy;

        public AIController(GameUnit unit, AIPersonality? personality = null)
        {
            Unit = unit;
            Personality = personality ?? EnemyAISystem.GetPersonalityForType(unit.type);
            BehaviorTree = UnitBehaviorFactory.CreateBehaviorForUnitType(unit.type);
            context = new AIContext();
        }

        /// <summary>
        /// Update AI each frame
        /// </summary>
        public void Update(float currentTime, float deltaTime, List<GameUnit> allies, List<GameUnit> enemies)
        {
            // Update context
            context.unit = Unit;
            context.allies = allies;
            context.enemies = enemies;
            context.currentTime = currentTime;
            context.delta = deltaTime;

            // Periodic strategic evaluation
            if (currentTime - lastEvaluationTime >= evaluationInterval)
            {
                currentStrategy = EnemyAISystem.EvaluateAI(Unit, allies, enemies, Personality);
                lastEvaluationTime = currentTime;
            }

            // Tick behavior tree for moment-to-moment decisions
            BehaviorTree?.Tick(context);
        }

        public AIStrategy GetCurrentStrategy() => currentStrategy;
    }
}
