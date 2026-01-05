using UnityEngine;
using System;

namespace AutoBattler.Core.VoidGame
{
    /// <summary>
    /// CombatEngine - Converted from TypeScript
    /// Handles damage calculation, healing, and status effect application
    /// </summary>
    public static class CombatEngine
    {
        /// <summary>
        /// Calculate effective damage after mitigation
        /// </summary>
        /// <param name="source">Attacking unit</param>
        /// <param name="target">Target unit</param>
        /// <param name="baseDamage">Base damage amount</param>
        /// <param name="damageType">Type of damage</param>
        /// <returns>Calculated damage after mitigation</returns>
        public static int CalculateDamage(GameUnit source, GameUnit target, int baseDamage, DamageType damageType)
        {
            int mitigation = damageType switch
            {
                DamageType.Physical => target.stats.armor,
                DamageType.Magical => target.stats.magicResist,
                DamageType.Elemental => target.stats.elementalResist,
                _ => 0
            };

            // Damage reduction formula: damage * (100 / (100 + armor))
            float effectiveDamage = baseDamage * (100f / (100f + Mathf.Max(0, mitigation)));
            return Mathf.FloorToInt(effectiveDamage);
        }

        /// <summary>
        /// Apply damage to a target
        /// </summary>
        /// <param name="source">Attacking unit (can be null)</param>
        /// <param name="target">Target unit</param>
        /// <param name="damage">Damage amount to apply</param>
        /// <param name="damageType">Type of damage</param>
        /// <param name="currentTime">Current game time</param>
        /// <param name="isCrit">Whether this is a critical hit</param>
        /// <returns>Result of the damage application</returns>
        public static DamageResult ApplyDamage(
            GameUnit source,
            GameUnit target,
            int damage,
            DamageType damageType,
            float currentTime,
            bool isCrit = false)
        {
            var result = new DamageResult();

            // Already dead check
            if (target.deadTime.HasValue || target.stats.hp <= 0)
            {
                result.isDead = true;
                return result;
            }

            // Dodge check
            if (UnityEngine.Random.value < target.stats.dodgeChance)
            {
                result.wasDodged = true;
                result.actualDamage = 0;
                return result;
            }

            // Apply damage
            result.wasCrit = isCrit;
            target.stats.hp -= damage;
            result.actualDamage = damage;

            // Track stats
            if (source != null)
            {
                source.roundStats.damageDealt += damage;
            }
            target.roundStats.damageTaken += damage;

            // Check for death
            if (target.stats.hp <= 0)
            {
                target.deadTime = currentTime;
                target.stats.hp = 0;
                result.isDead = true;
            }

            return result;
        }

        /// <summary>
        /// Apply healing to a target
        /// </summary>
        /// <param name="source">Source of healing (can be null)</param>
        /// <param name="target">Target to heal</param>
        /// <param name="amount">Healing amount</param>
        /// <returns>Actual amount healed</returns>
        public static int ApplyHeal(GameUnit source, GameUnit target, int amount)
        {
            // Dead units can't be healed
            if (target.deadTime.HasValue || target.stats.hp <= 0)
                return 0;

            int oldHp = target.stats.hp;
            target.stats.hp = Mathf.Min(target.stats.maxHp, target.stats.hp + amount);
            int actualHeal = target.stats.hp - oldHp;

            // Track healing stats
            if (source != null)
            {
                source.roundStats.healingDone += actualHeal;
            }

            return actualHeal;
        }

        /// <summary>
        /// Apply a status effect to a target
        /// </summary>
        /// <param name="target">Target unit</param>
        /// <param name="effectType">Type of status effect</param>
        /// <param name="value">Effect value/strength</param>
        /// <param name="duration">Duration in seconds</param>
        /// <param name="currentTime">Current game time</param>
        public static void ApplyStatusEffect(
            GameUnit target,
            StatusEffectType effectType,
            float value,
            float duration,
            float currentTime)
        {
            if (target.statusEffects == null)
                target.statusEffects = new System.Collections.Generic.List<StatusEffect>();

            // Check for existing effect of same type
            int existingIndex = target.statusEffects.FindIndex(e => e.type == effectType);

            if (existingIndex >= 0)
            {
                // Refresh/stack existing effect
                var existing = target.statusEffects[existingIndex];
                existing.endTime = Mathf.Max(existing.endTime, currentTime + duration);
                existing.value = Mathf.Max(existing.value, value);
                target.statusEffects[existingIndex] = existing;
            }
            else
            {
                // Add new effect
                var newEffect = new StatusEffect
                {
                    id = Guid.NewGuid().ToString().Substring(0, 9),
                    type = effectType,
                    value = value,
                    startTime = currentTime,
                    endTime = currentTime + duration,
                    lastTick = currentTime
                };
                target.statusEffects.Add(newEffect);
            }
        }

        /// <summary>
        /// Remove a status effect from a target
        /// </summary>
        /// <param name="target">Target unit</param>
        /// <param name="effectType">Type of effect to remove</param>
        public static void RemoveStatusEffect(GameUnit target, StatusEffectType effectType)
        {
            target.statusEffects?.RemoveAll(e => e.type == effectType);
        }

        /// <summary>
        /// Process tick-based status effects (DoTs/HoTs)
        /// </summary>
        /// <param name="unit">Unit with status effects</param>
        /// <param name="currentTime">Current game time</param>
        /// <param name="tickInterval">Time between ticks</param>
        public static void ProcessStatusEffectTicks(GameUnit unit, float currentTime, float tickInterval = 1f)
        {
            if (unit.statusEffects == null || unit.statusEffects.Count == 0)
                return;

            for (int i = unit.statusEffects.Count - 1; i >= 0; i--)
            {
                var effect = unit.statusEffects[i];

                // Remove expired effects
                if (effect.IsExpired(currentTime))
                {
                    unit.statusEffects.RemoveAt(i);
                    continue;
                }

                // Process tick-based effects
                if (currentTime - effect.lastTick >= tickInterval)
                {
                    effect.lastTick = currentTime;
                    unit.statusEffects[i] = effect;

                    switch (effect.type)
                    {
                        case StatusEffectType.Burn:
                        case StatusEffectType.Poison:
                            // Apply damage over time
                            int dotDamage = Mathf.RoundToInt(effect.value);
                            unit.stats.hp -= dotDamage;
                            unit.roundStats.damageTaken += dotDamage;
                            
                            if (unit.stats.hp <= 0 && !unit.deadTime.HasValue)
                            {
                                unit.deadTime = currentTime;
                                unit.stats.hp = 0;
                            }
                            break;

                        case StatusEffectType.Regen:
                            // Apply healing over time
                            int hotHeal = Mathf.RoundToInt(effect.value);
                            ApplyHeal(null, unit, hotHeal);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Get modified stat value considering status effects
        /// </summary>
        public static float GetModifiedStat(GameUnit unit, string statName)
        {
            if (unit.statusEffects == null)
                return 0f;

            float modifier = 0f;

            foreach (var effect in unit.statusEffects)
            {
                switch (effect.type)
                {
                    case StatusEffectType.Might:
                        if (statName == "damage")
                            modifier += effect.value;
                        break;

                    case StatusEffectType.Haste:
                        if (statName == "attackSpeed" || statName == "moveSpeed")
                            modifier += effect.value;
                        break;

                    case StatusEffectType.Weakness:
                        if (statName == "damage")
                            modifier -= effect.value;
                        break;

                    case StatusEffectType.Slow:
                        if (statName == "moveSpeed" || statName == "attackSpeed")
                            modifier -= effect.value;
                        break;

                    case StatusEffectType.Shield:
                        if (statName == "armor")
                            modifier += effect.value;
                        break;
                }
            }

            return modifier;
        }

        /// <summary>
        /// Check if unit is stunned
        /// </summary>
        public static bool IsStunned(GameUnit unit)
        {
            return unit.HasStatusEffect(StatusEffectType.Stun);
        }

        /// <summary>
        /// Calculate critical hit damage
        /// </summary>
        public static int CalculateCritDamage(int baseDamage, float critMultiplier = 2f)
        {
            return Mathf.RoundToInt(baseDamage * critMultiplier);
        }

        /// <summary>
        /// Roll for critical hit
        /// </summary>
        public static bool RollCrit(GameUnit unit)
        {
            return UnityEngine.Random.value < unit.stats.critChance;
        }

        /// <summary>
        /// Calculate lifesteal healing
        /// </summary>
        public static int CalculateLifesteal(int damageDealt, float lifestealPercent)
        {
            return Mathf.RoundToInt(damageDealt * lifestealPercent);
        }

        /// <summary>
        /// Full attack flow: damage calculation, crit check, apply damage, lifesteal
        /// </summary>
        public static DamageResult ExecuteAttack(
            GameUnit source,
            GameUnit target,
            float currentTime)
        {
            if (source == null || target == null)
                return new DamageResult();

            // Calculate base damage with modifiers
            int baseDamage = source.stats.damage;
            baseDamage += Mathf.RoundToInt(GetModifiedStat(source, "damage"));

            // Crit check
            bool isCrit = RollCrit(source);
            if (isCrit)
            {
                baseDamage = CalculateCritDamage(baseDamage);
            }

            // Calculate final damage after mitigation
            int finalDamage = CalculateDamage(source, target, baseDamage, source.damageType);

            // Apply damage
            var result = ApplyDamage(source, target, finalDamage, source.damageType, currentTime, isCrit);

            // Apply lifesteal if damage was dealt
            if (result.actualDamage > 0 && source.stats.lifesteal > 0)
            {
                int lifestealAmount = CalculateLifesteal(result.actualDamage, source.stats.lifesteal);
                ApplyHeal(null, source, lifestealAmount);
            }

            return result;
        }
    }
}
