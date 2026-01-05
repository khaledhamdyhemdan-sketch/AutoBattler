using UnityEngine;
using System;
using System.Collections.Generic;

namespace AutoBattler.Core.VoidGame
{
    #region Enums

    public enum UnitType
    {
        // Player Units
        Tank,
        Dps,
        Healer,
        Assassin,
        Mage,
        Archer,
        Paladin,

        // Enemy Units
        EnemyMelee,
        EnemyRanged,
        EnemyFast,
        EnemyArmored,
        EnemyCultist,
        EnemyBoss,
        EnemyScuttler,
        EnemyMeshy,
        EnemyFlying
    }

    public enum UnitTrait
    {
        Void,
        Iron,
        Mystic,
        Stalker,
        Celestial,
        Undead,
        Noble
    }

    public enum DamageType
    {
        Physical,
        Magical,
        Elemental
    }

    public enum AIState
    {
        Idle,
        Moving,
        Attacking,
        Casting,
        Fleeing
    }

    public enum AnimationState
    {
        Idle,
        Run,
        Attack,
        Death
    }

    public enum StatusEffectType
    {
        Might,
        Shield,
        Haste,
        Regen,
        Weakness,
        Stun,
        Burn,
        Slow,
        Poison
    }

    public enum GearEffectType
    {
        Cleave,
        Burn,
        Freeze,
        Shred,
        HealOnKill,
        ManaGain
    }

    public enum GearCategory
    {
        MainHand,
        OffHand,
        Chest,
        Head,
        Relic
    }

    public enum GearRarity
    {
        Common,
        Grand,
        Rare,
        Arcane,
        Heroic,
        Legendary,
        Mythic,
        Celestial,
        Divine
    }

    public enum AbilityTarget
    {
        Self,
        Enemy,
        Ally,
        Ground
    }

    public enum AbilityShape
    {
        Circle,
        Cone,
        Line,
        Single
    }

    public enum VfxType
    {
        Slam,
        Beam,
        Nova,
        Projectile,
        Smite,
        Whirlwind,
        Teleport
    }

    public enum GamePhase
    {
        Menu,
        TeamSelection,
        Planning,
        Battling,
        Reward,
        GameOver,
        Meta
    }

    public enum GameMode
    {
        Standard,
        Hardcore
    }

    public enum ProjectileType
    {
        Arrow,
        Magic,
        Rock,
        Bolt,
        Heal,
        Fire
    }

    public enum AbilityType
    {
        Damage,
        Heal,
        Buff,
        Debuff,
        Summon,
        Utility
    }

    public enum AIStrategyAction
    {
        Attack,
        Defend,
        Retreat,
        Flank,
        Kite,
        Support,
        Position
    }

    #endregion

    #region Structs

    [Serializable]
    public struct Position
    {
        public float x;
        public float z;

        public Position(float x, float z)
        {
            this.x = x;
            this.z = z;
        }

        public Position(Vector3 v)
        {
            this.x = v.x;
            this.z = v.z;
        }

        public Vector3 ToVector3(float y = 0f) => new Vector3(x, y, z);

        public float DistanceTo(Position other)
        {
            float dx = x - other.x;
            float dz = z - other.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public static Position operator +(Position a, Position b) => new Position(a.x + b.x, a.z + b.z);
        public static Position operator -(Position a, Position b) => new Position(a.x - b.x, a.z - b.z);
        public static Position operator *(Position a, float scalar) => new Position(a.x * scalar, a.z * scalar);

        public Position Normalized()
        {
            float length = Mathf.Sqrt(x * x + z * z);
            if (length > 0)
                return new Position(x / length, z / length);
            return this;
        }

        public override string ToString() => $"({x:F2}, {z:F2})";
    }

    [Serializable]
    public struct UnitStats
    {
        public int maxHp;
        public int hp;
        public int damage;
        public float attackSpeed;
        public float attackRange;
        public float moveSpeed;
        public float critChance;
        public float critMultiplier;
        public float dodgeChance;
        public float lifesteal;
        public int armor;
        public int magicResist;
        public int elementalResist;
        public float cooldownReduction;
        public float mana;
        public float maxMana;

        // Aliases for compatibility
        public float speed { get => moveSpeed; set => moveSpeed = value; }
        public float range { get => attackRange; set => attackRange = value; }

        public static UnitStats Default => new UnitStats
        {
            maxHp = 100,
            hp = 100,
            damage = 10,
            attackSpeed = 1f,
            attackRange = 1.5f,
            moveSpeed = 3f,
            critChance = 0.05f,
            dodgeChance = 0f,
            lifesteal = 0f,
            armor = 0,
            magicResist = 0,
            elementalResist = 0,
            cooldownReduction = 0f,
            mana = 0f,
            maxMana = 100f
        };
    }

    [Serializable]
    public struct UnitRoundStats
    {
        public int damageDealt;
        public int healingDone;
        public int damageTaken;

        public void Reset()
        {
            damageDealt = 0;
            healingDone = 0;
            damageTaken = 0;
        }
    }

    [Serializable]
    public struct StatusEffect
    {
        public string id;
        public StatusEffectType type;
        public float value;
        public float endTime;
        public float startTime;
        public float lastTick;

        public bool IsExpired(float currentTime) => currentTime >= endTime;
    }

    [Serializable]
    public struct GearEffect
    {
        public GearEffectType type;
        public float value;
    }

    // AIPersonality and AIStrategy are defined in AutoBattler.Core.VoidGame.AI.EnemyAISystem

    [Serializable]
    public struct DamageResult
    {
        public int actualDamage;
        public bool isDead;
        public bool wasDodged;
        public bool wasCrit;
        
        // Alias for convenience
        public int damageDealt => actualDamage;
    }

    [Serializable]
    public struct CombatPopup
    {
        public string id;
        public string value;
        public CombatPopupType type;
        public DamageType? damageType;
        public Position position;
        public float createdAt;
    }

    public enum CombatPopupType
    {
        Damage,
        Heal,
        Crit,
        Dodge,
        Status,
        Loot
    }

    #endregion

    #region Classes

    [Serializable]
    public class Gear
    {
        public string id;
        public string name;
        public string description;
        public GearRarity rarity;
        public GearCategory category;
        public UnitStats stats;
        public UnitStats? multipliers;
        public string visualId;
        public GearEffect? effect;
    }

    [Serializable]
    public class Ability
    {
        public string id;
        public string name;
        public string description;
        public float manaCost;
        public float cooldown;
        public float range;
        public float castTime;
        public float damageScale;
        public float healScale;
        public AbilityTarget targetType;
        public AbilityShape shape;
        public float radius;
        public Color color;
        public VfxType vfxType;
        public AbilityType abilityType;
        public float lastUsed; // Track cooldown
    }

    [Serializable]
    public class Blessing
    {
        public string id;
        public string name;
        public string description;
        public string type; // "stat", "synergy", "ability"
        public string targetId;
        public GearRarity rarity;
        public BlessingPayload payload;
    }

    [Serializable]
    public class BlessingPayload
    {
        public string stat;
        public float value;
        public UnitTrait? trait;
        public string effect; // "bounce", "splash", "cleave", "giant"
    }

    [Serializable]
    public class Projectile
    {
        public string id;
        public string ownerId;
        public string targetId;
        public Position position;
        public int damage;
        public DamageType damageType;
        public float speed;
        public bool isEnemy;
        public bool isCrit;
        public float lifesteal;
        public ProjectileType type;
        public int bouncesRemaining;
    }

    [Serializable]
    public class AbilityVisual
    {
        public string id;
        public VfxType type;
        public Position position;
        public Position? targetPosition;
        public Color color;
        public float radius;
        public float duration;
        public float createdAt;
    }

    #endregion
}
