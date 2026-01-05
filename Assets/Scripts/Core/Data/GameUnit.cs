using UnityEngine;
using System;
using System.Collections.Generic;

namespace AutoBattler.Core.VoidGame
{
    /// <summary>
    /// GameUnit represents a unit in combat - converted from TypeScript
    /// This is the data model used by the combat and AI systems
    /// </summary>
    [Serializable]
    public class GameUnit
    {
        public string id;
        public string name;
        public UnitType type;
        public List<UnitTrait> traits = new List<UnitTrait>();
        public Position position;
        public Position initialPosition;
        public UnitStats baseStats;
        public UnitStats stats;
        public DamageType damageType;
        public bool isEnemy;
        public string targetId;
        public float lastTargetCheck;
        public AIState aiState;
        public AnimationState animationState;
        public float lastAttackTime;
        public float lastHitTime;
        public float? deadTime;
        public float maxCooldown;
        public string abilityId;
        public float abilityCooldown;
        public List<Position> currentPath;
        public int pathIndex;
        public float lastPathUpdateTime;
        public bool isCasting;
        public float castEndTime;
        public Dictionary<GearCategory, string> equipment = new Dictionary<GearCategory, string>();
        public List<StatusEffect> statusEffects = new List<StatusEffect>();
        public List<Ability> abilities = new List<Ability>();
        public UnitRoundStats roundStats;
        public float scale = 1f;
        public float rotation;

        // Unity integration
        public UnitController unitController;
        public Transform transform;
        public Transform unityTransform; // Alias for transform

        public bool IsDead => deadTime.HasValue || stats.hp <= 0;

        public GameUnit()
        {
            id = Guid.NewGuid().ToString();
            traits = new List<UnitTrait>();
            statusEffects = new List<StatusEffect>();
            equipment = new Dictionary<GearCategory, string>();
            roundStats = new UnitRoundStats();
        }

        public GameUnit(UnitController controller) : this()
        {
            unitController = controller;
            if (controller != null)
            {
                transform = controller.transform;
                position = new Position(controller.transform.position);
                initialPosition = position;
            }
        }

        /// <summary>
        /// Sync position from Unity transform
        /// </summary>
        public void SyncFromTransform()
        {
            if (transform != null)
            {
                position = new Position(transform.position);
            }
        }

        /// <summary>
        /// Sync position from Unity transform (alias)
        /// </summary>
        public void SyncFromUnityTransform()
        {
            SyncFromTransform();
        }

        /// <summary>
        /// Sync position to Unity transform
        /// </summary>
        public void SyncToTransform()
        {
            if (transform != null)
            {
                Vector3 pos = position.ToVector3(transform.position.y);
                transform.position = pos;
            }
        }

        /// <summary>
        /// Sync position to Unity transform (alias)
        /// </summary>
        public void SyncToUnityTransform()
        {
            SyncToTransform();
        }

        /// <summary>
        /// Check if unit has a specific status effect
        /// </summary>
        public bool HasStatusEffect(StatusEffectType effectType)
        {
            return statusEffects.Exists(e => e.type == effectType);
        }

        /// <summary>
        /// Get status effect by type
        /// </summary>
        public StatusEffect? GetStatusEffect(StatusEffectType effectType)
        {
            int index = statusEffects.FindIndex(e => e.type == effectType);
            if (index >= 0)
                return statusEffects[index];
            return null;
        }

        /// <summary>
        /// Remove expired status effects
        /// </summary>
        public void CleanupExpiredEffects(float currentTime)
        {
            statusEffects.RemoveAll(e => e.IsExpired(currentTime));
        }

        /// <summary>
        /// Reset stats to base values
        /// </summary>
        public void ResetStats()
        {
            stats = baseStats;
        }

        /// <summary>
        /// Reset for new round
        /// </summary>
        public void ResetForRound()
        {
            stats.hp = stats.maxHp;
            stats.mana = 0;
            deadTime = null;
            aiState = AIState.Idle;
            targetId = null;
            isCasting = false;
            statusEffects.Clear();
            roundStats.Reset();
            position = initialPosition;
            SyncToTransform();
        }

        /// <summary>
        /// Create from UnitController stats
        /// </summary>
        public static GameUnit FromUnitController(UnitController controller, bool isEnemy)
        {
            if (controller == null) return null;

            var unit = new GameUnit(controller)
            {
                name = controller.UnitStats?.unitName ?? "Unknown",
                isEnemy = isEnemy,
                damageType = DamageType.Physical, // Default
                aiState = AIState.Idle,
                animationState = AnimationState.Idle
            };

            // Map UnitController stats to GameUnit stats
            if (controller.UnitStats != null)
            {
                unit.baseStats = new UnitStats
                {
                    maxHp = controller.UnitStats.maxHealth,
                    hp = controller.UnitStats.maxHealth,
                    damage = controller.UnitStats.attackDamage,
                    attackSpeed = controller.UnitStats.attackSpeed,
                    attackRange = controller.UnitStats.attackRange,
                    moveSpeed = controller.UnitStats.moveSpeed,
                    critChance = controller.UnitStats.criticalChance,
                    dodgeChance = 0f,
                    lifesteal = 0f,
                    armor = 0,
                    magicResist = 0,
                    elementalResist = 0,
                    cooldownReduction = 0f,
                    mana = 0f,
                    maxMana = 100f
                };
                unit.stats = unit.baseStats;
            }

            // Determine unit type based on controller
            unit.type = DetermineUnitType(controller, isEnemy);

            return unit;
        }

        private static UnitType DetermineUnitType(UnitController controller, bool isEnemy)
        {
            if (controller.UnitStats == null)
                return isEnemy ? UnitType.EnemyMelee : UnitType.Dps;

            string name = controller.UnitStats.unitName?.ToLower() ?? "";

            if (isEnemy)
            {
                if (name.Contains("boss")) return UnitType.EnemyBoss;
                if (name.Contains("armored") || name.Contains("tank")) return UnitType.EnemyArmored;
                if (name.Contains("ranged") || name.Contains("archer")) return UnitType.EnemyRanged;
                if (name.Contains("fast") || name.Contains("swift")) return UnitType.EnemyFast;
                if (name.Contains("cultist")) return UnitType.EnemyCultist;
                if (name.Contains("scuttler")) return UnitType.EnemyScuttler;
                return UnitType.EnemyMelee;
            }
            else
            {
                if (name.Contains("tank") || name.Contains("warrior")) return UnitType.Tank;
                if (name.Contains("healer") || name.Contains("priest")) return UnitType.Healer;
                if (name.Contains("mage") || name.Contains("wizard")) return UnitType.Mage;
                if (name.Contains("archer") || name.Contains("ranger")) return UnitType.Archer;
                if (name.Contains("assassin") || name.Contains("rogue")) return UnitType.Assassin;
                if (name.Contains("paladin")) return UnitType.Paladin;
                return UnitType.Dps;
            }
        }
    }
}
