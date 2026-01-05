using UnityEngine;
using AutoBattler.Core;

namespace AutoBattler.Data
{
    /// <summary>
    /// ScriptableObject for unit abilities with configurable parameters
    /// </summary>
    [CreateAssetMenu(fileName = "Ability", menuName = "AutoBattler/Data/Ability", order = 2)]
    public class AbilitySO : ScriptableObject
    {
        [Header("Ability Info")]
        public string abilityName;
        [TextArea(3, 5)]
        public string description;
        public Sprite icon;

        [Header("Targeting")]
        public AbilityTargetType targetType;
        public float range = 5.0f;
        public int maxTargets = 1;

        [Header("Damage & Effects")]
        public int baseDamage = 0;
        public float damageScaling = 1.0f; // Scales with unit's attack damage
        public DamageType damageType;
        public StatusEffect[] statusEffects;

        [Header("Cooldown")]
        public float cooldown = 5.0f;
        public float manaCost = 0f;

        [Header("Visual Effects")]
        public GameObject castEffect;
        public GameObject projectilePrefab;
        public GameObject impactEffect;
        public AudioClip castSound;

        public virtual void Execute(UnitController caster, UnitController target)
        {
            // Override in specific ability implementations
            Debug.Log($"{caster.name} used {abilityName} on {target.name}");
        }
    }

    public enum AbilityTargetType
    {
        Self,
        SingleEnemy,
        SingleAlly,
        AllEnemies,
        AllAllies,
        AreaOfEffect,
        LowestHealthAlly,
        HighestHealthEnemy
    }

    public enum DamageType
    {
        Physical,
        Magical,
        True // Ignores armor/resistance
    }

    [System.Serializable]
    public struct StatusEffect
    {
        public StatusEffectType type;
        public float duration;
        public float value; // Damage per second, slow percentage, etc.
    }

    public enum StatusEffectType
    {
        Stun,
        Slow,
        DamageOverTime,
        HealOverTime,
        Shield,
        AttackSpeedBuff,
        AttackSpeedDebuff
    }
}
