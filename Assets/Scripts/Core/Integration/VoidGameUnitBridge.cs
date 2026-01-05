using UnityEngine;
using AutoBattler.Core.VoidGame;
using AutoBattler.Core.VoidGame.AI;

namespace AutoBattler.Core.VoidGame
{
    /// <summary>
    /// VoidGame Unit Bridge - Attaches to units to integrate VoidGame AI/Combat with existing UnitController
    /// Add this component alongside UnitController to enable VoidGame systems
    /// </summary>
    [RequireComponent(typeof(UnitController))]
    public class VoidGameUnitBridge : MonoBehaviour
    {
        #region Configuration

        [Header("Unit Configuration")]
        [SerializeField] private bool isPlayerUnit = true;
        [SerializeField] private UnitType unitType = UnitType.Dps;
        [SerializeField] private DamageType damageType = DamageType.Physical;

        [Header("AI Configuration")]
        [SerializeField] private bool useVoidGameAI = true;
        [SerializeField] private bool overridePersonality = false;
        [SerializeField] private AIPersonality customPersonality;

        [Header("Debug")]
        [SerializeField] private bool showAIDebug = false;

        #endregion

        #region References

        private UnitController unitController;
        private GameUnit gameUnit;
        private AIController aiController;
        private BehaviorTree behaviorTree;

        #endregion

        #region Properties

        public GameUnit GameUnit => gameUnit;
        public AIController AIController => aiController;
        public bool IsPlayerUnit => isPlayerUnit;
        public UnitType UnitType => unitType;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            unitController = GetComponent<UnitController>();
        }

        private void Start()
        {
            RegisterWithVoidGame();
        }

        private void Update()
        {
            if (gameUnit == null || !useVoidGameAI)
                return;

            // Sync position from Unity transform
            gameUnit.SyncFromUnityTransform();

            // Update AI if not managed centrally
            if (aiController != null && VoidGameManager.Instance == null)
            {
                UpdateAI();
            }
        }

        private void OnDestroy()
        {
            if (VoidGameManager.Instance != null && gameUnit != null)
            {
                VoidGameManager.Instance.UnregisterUnit(gameUnit);
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Register this unit with the VoidGame system
        /// </summary>
        private void RegisterWithVoidGame()
        {
            // Create GameUnit from UnitController stats
            gameUnit = CreateGameUnitFromController();

            // Register with manager if available
            if (VoidGameManager.Instance != null)
            {
                VoidGameManager.Instance.RegisterUnit(unitController, isPlayerUnit);
            }

            // Create AI controller
            if (useVoidGameAI)
            {
                var personality = overridePersonality ? customPersonality : EnemyAISystem.GetPersonalityForType(unitType);
                aiController = new AIController(gameUnit, personality);
                behaviorTree = UnitBehaviorFactory.CreateBehaviorForUnitType(unitType);
            }

            Debug.Log($"[VoidGameUnitBridge] Initialized unit: {gameUnit.name} as {unitType}");
        }

        /// <summary>
        /// Create a GameUnit from the attached UnitController
        /// </summary>
        private GameUnit CreateGameUnitFromController()
        {
            var unit = new GameUnit
            {
                id = gameObject.GetInstanceID().ToString(),
                name = gameObject.name,
                type = unitType,
                damageType = damageType,
                position = new Position(transform.position.x, transform.position.z),
                rotation = transform.rotation.eulerAngles.y,
                unityTransform = transform,
                unitController = unitController
            };

            // Copy stats from UnitController's StatsSO if available
            if (unitController != null)
            {
                var statsProp = unitController.GetType().GetField("stats", 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance);
                    
                if (statsProp != null)
                {
                    var stats = statsProp.GetValue(unitController);
                    if (stats != null)
                    {
                        // Try to extract stats via reflection or use defaults
                        unit.stats = GetStatsFromController(stats);
                    }
                }
            }

            // Use defaults if stats couldn't be extracted
            if (unit.stats.maxHp == 0)
            {
                unit.stats = GetDefaultStats(unitType);
            }

            return unit;
        }

        /// <summary>
        /// Extract stats from the UnitController's stats object
        /// </summary>
        private UnitStats GetStatsFromController(object statsObj)
        {
            var stats = new UnitStats();
            var type = statsObj.GetType();

            try
            {
                // Try to get common stat properties
                var maxHpProp = type.GetProperty("MaxHP") ?? type.GetProperty("maxHp") ?? type.GetProperty("MaxHealth");
                var damageProp = type.GetProperty("Damage") ?? type.GetProperty("damage") ?? type.GetProperty("AttackDamage");
                var speedProp = type.GetProperty("Speed") ?? type.GetProperty("speed") ?? type.GetProperty("MoveSpeed");
                var attackSpeedProp = type.GetProperty("AttackSpeed") ?? type.GetProperty("attackSpeed");
                var armorProp = type.GetProperty("Armor") ?? type.GetProperty("armor") ?? type.GetProperty("Defense");
                var rangeProp = type.GetProperty("Range") ?? type.GetProperty("range") ?? type.GetProperty("AttackRange");

                if (maxHpProp != null)
                    stats.maxHp = (int)System.Convert.ChangeType(maxHpProp.GetValue(statsObj), typeof(int));
                if (damageProp != null)
                    stats.damage = (int)System.Convert.ChangeType(damageProp.GetValue(statsObj), typeof(int));
                if (speedProp != null)
                    stats.speed = (float)System.Convert.ChangeType(speedProp.GetValue(statsObj), typeof(float));
                if (attackSpeedProp != null)
                    stats.attackSpeed = (float)System.Convert.ChangeType(attackSpeedProp.GetValue(statsObj), typeof(float));
                if (armorProp != null)
                    stats.armor = (int)System.Convert.ChangeType(armorProp.GetValue(statsObj), typeof(int));
                if (rangeProp != null)
                    stats.range = (float)System.Convert.ChangeType(rangeProp.GetValue(statsObj), typeof(float));

                stats.hp = stats.maxHp;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[VoidGameUnitBridge] Failed to extract stats: {e.Message}");
            }

            return stats;
        }

        /// <summary>
        /// Get default stats based on unit type
        /// </summary>
        private UnitStats GetDefaultStats(UnitType type)
        {
            return type switch
            {
                UnitType.Tank => new UnitStats
                {
                    maxHp = 200, hp = 200, damage = 15, armor = 30,
                    magicResist = 20, speed = 2f, attackSpeed = 0.8f, range = 1.5f
                },
                UnitType.Dps => new UnitStats
                {
                    maxHp = 100, hp = 100, damage = 35, armor = 10,
                    magicResist = 10, speed = 3f, attackSpeed = 1.2f, range = 1.5f,
                    critChance = 0.2f, critMultiplier = 1.5f
                },
                UnitType.Healer => new UnitStats
                {
                    maxHp = 80, hp = 80, damage = 20, armor = 5,
                    magicResist = 25, speed = 2.5f, attackSpeed = 0.6f, range = 8f
                },
                UnitType.EnemyMelee => new UnitStats
                {
                    maxHp = 80, hp = 80, damage = 20, armor = 15,
                    magicResist = 10, speed = 3f, attackSpeed = 1f, range = 1.5f
                },
                UnitType.EnemyRanged => new UnitStats
                {
                    maxHp = 60, hp = 60, damage = 25, armor = 5,
                    magicResist = 5, speed = 2.5f, attackSpeed = 0.8f, range = 7f
                },
                UnitType.EnemyFlying => new UnitStats
                {
                    maxHp = 50, hp = 50, damage = 22, armor = 5,
                    magicResist = 10, speed = 4f, attackSpeed = 1f, range = 5f
                },
                UnitType.EnemyBoss => new UnitStats
                {
                    maxHp = 500, hp = 500, damage = 50, armor = 40,
                    magicResist = 30, speed = 2f, attackSpeed = 0.6f, range = 2f
                },
                _ => new UnitStats
                {
                    maxHp = 100, hp = 100, damage = 20, armor = 10,
                    magicResist = 10, speed = 3f, attackSpeed = 1f, range = 1.5f
                }
            };
        }

        #endregion

        #region AI Update

        /// <summary>
        /// Update AI when not managed by VoidGameManager
        /// </summary>
        private void UpdateAI()
        {
            if (aiController == null)
                return;

            // Get allies and enemies
            var allBridges = FindObjectsByType<VoidGameUnitBridge>(FindObjectsSortMode.None);
            var allies = new System.Collections.Generic.List<GameUnit>();
            var enemies = new System.Collections.Generic.List<GameUnit>();

            foreach (var bridge in allBridges)
            {
                if (bridge == this || bridge.gameUnit == null || bridge.gameUnit.IsDead)
                    continue;

                if (bridge.isPlayerUnit == this.isPlayerUnit)
                    allies.Add(bridge.gameUnit);
                else
                    enemies.Add(bridge.gameUnit);
            }

            aiController.Update(Time.time, Time.deltaTime, allies, enemies);

            if (showAIDebug)
            {
                var strategy = aiController.GetCurrentStrategy();
                Debug.Log($"[{gameUnit.name}] Strategy: {strategy.action} - {strategy.reason}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Set the unit type at runtime
        /// </summary>
        public void SetUnitType(UnitType type)
        {
            unitType = type;
            if (gameUnit != null)
                gameUnit.type = type;
            
            if (useVoidGameAI)
            {
                behaviorTree = UnitBehaviorFactory.CreateBehaviorForUnitType(type);
                var personality = overridePersonality ? customPersonality : EnemyAISystem.GetPersonalityForType(type);
                aiController = new AIController(gameUnit, personality);
            }
        }

        /// <summary>
        /// Set custom personality
        /// </summary>
        public void SetPersonality(AIPersonality personality)
        {
            customPersonality = personality;
            overridePersonality = true;
            if (aiController != null)
            {
                aiController.Personality = personality;
            }
        }

        /// <summary>
        /// Get current AI strategy
        /// </summary>
        public AIStrategy GetCurrentStrategy()
        {
            return aiController?.GetCurrentStrategy() ?? default;
        }

        /// <summary>
        /// Apply damage to this unit
        /// </summary>
        public DamageResult TakeDamage(GameUnit source, int amount, DamageType type)
        {
            if (gameUnit == null)
                return new DamageResult();

            return CombatEngine.ApplyDamage(source, gameUnit, amount, type, Time.time);
        }

        /// <summary>
        /// Apply healing to this unit
        /// </summary>
        public int ReceiveHeal(GameUnit source, int amount)
        {
            if (gameUnit == null)
                return 0;

            return CombatEngine.ApplyHeal(source, gameUnit, amount);
        }

        /// <summary>
        /// Apply status effect to this unit
        /// </summary>
        public void ApplyStatusEffect(StatusEffectType effectType, float value, float duration)
        {
            if (gameUnit == null)
                return;

            CombatEngine.ApplyStatusEffect(gameUnit, effectType, value, duration, Time.time);
        }

        #endregion

        #region Debug

        private void OnDrawGizmos()
        {
            if (!showAIDebug || aiController == null)
                return;

            var strategy = aiController.GetCurrentStrategy();

            // Draw line to target position
            Gizmos.color = strategy.action switch
            {
                AIActionType.Attack => Color.red,
                AIActionType.Retreat => Color.yellow,
                AIActionType.Support => Color.green,
                AIActionType.Defend => Color.blue,
                AIActionType.Flank => Color.magenta,
                AIActionType.Kite => Color.cyan,
                _ => Color.white
            };

            Vector3 targetPos = new Vector3(strategy.targetPosition.x, 0.5f, strategy.targetPosition.z);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, targetPos);
            Gizmos.DrawWireSphere(targetPos, 0.3f);
        }

        #endregion
    }
}
