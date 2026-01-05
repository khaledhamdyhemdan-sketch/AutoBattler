using UnityEngine;
using Unity.Mathematics;

namespace AutoBattler.Data
{
    /// <summary>
    /// Unity 6.3 ScriptableObject for unit statistics with Burst-compatible data
    /// </summary>
    [CreateAssetMenu(fileName = "UnitStats", menuName = "AutoBattler/Data/Unit Stats", order = 1)]
    public class UnitStatsSO : ScriptableObject
    {
        [Header("Basic Info")]
        public string unitName;
        public int tier = 1; // For shop pool filtering
        public Sprite icon;
        public GameObject visualPrefab;

        [Header("Combat Stats")]
        public int maxHealth = 100;
        public int attackDamage = 10;
        public float attackRange = 1.5f;
        public float attackSpeed = 1.0f; // Attacks per second
        public float criticalChance = 0.1f;
        public float criticalMultiplier = 2.0f;

        [Header("Movement")]
        public float moveSpeed = 3.0f;
        public float rotationSpeed = 10.0f;

        [Header("Abilities")]
        public AbilitySO[] abilities;

        [Header("Cost & Rarity")]
        public int goldCost = 1;
        public UnitRarity rarity = UnitRarity.Common;

        [Header("Visual Effects")]
        public Material outlineMaterial; // For silhouettes
        public ParticleSystem spawnEffect;
        public ParticleSystem deathEffect;

        [Header("Loot")]
        public ItemData[] possibleDrops;

        /// <summary>
        /// Convert to Burst-compatible struct for performance-critical operations
        /// </summary>
        public UnitStatsData ToStatsData()
        {
            return new UnitStatsData
            {
                maxHealth = maxHealth,
                attackDamage = attackDamage,
                attackRange = attackRange,
                attackSpeed = attackSpeed,
                moveSpeed = moveSpeed,
                criticalChance = criticalChance,
                criticalMultiplier = criticalMultiplier,
                rotationSpeed = rotationSpeed
            };
        }
    }

    public enum UnitRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    /// <summary>
    /// Burst-compatible struct for runtime unit stats
    /// </summary>
    [System.Serializable]
    public struct UnitStatsData
    {
        public int maxHealth;
        public int attackDamage;
        public float attackRange;
        public float attackSpeed;
        public float moveSpeed;
        public float criticalChance;
        public float criticalMultiplier;
        public float rotationSpeed;
    }
}
