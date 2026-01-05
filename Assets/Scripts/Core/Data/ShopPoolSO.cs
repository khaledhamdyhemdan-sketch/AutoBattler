using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace AutoBattler.Data
{
    /// <summary>
    /// ScriptableObject that defines the shop pool with tier-based unit distribution
    /// Compatible with Unity 6.3 Building Blocks system
    /// </summary>
    [CreateAssetMenu(fileName = "ShopPool", menuName = "AutoBattler/Data/Shop Pool", order = 3)]
    public class ShopPoolSO : ScriptableObject
    {
        [Header("Shop Configuration")]
        public int shopSlots = 5;
        public int rerollCost = 2;

        [Header("Tier-based Unit Pools")]
        public TierPool[] tierPools;

        [Header("Player Level Progression")]
        public LevelOdds[] levelOddsTable;

        /// <summary>
        /// Get units for a specific player level with weighted random selection
        /// </summary>
        public List<UnitStatsSO> GetShopUnits(int playerLevel, int count)
        {
            var units = new List<UnitStatsSO>();
            var odds = GetOddsForLevel(playerLevel);

            for (int i = 0; i < count; i++)
            {
                int tier = SelectTierByOdds(odds);
                var unit = GetRandomUnitFromTier(tier);
                if (unit != null)
                {
                    units.Add(unit);
                }
            }

            return units;
        }

        private LevelOdds GetOddsForLevel(int level)
        {
            if (levelOddsTable == null || levelOddsTable.Length == 0)
            {
                // Only log once to avoid spamming
                if (Time.frameCount % 300 == 0) 
                    Debug.LogWarning("[ShopPoolSO] Level odds table is empty. Using default odds.");
                return new LevelOdds { tierChances = new float[] { 100f, 0f, 0f, 0f, 0f } };
            }
            level = Mathf.Clamp(level, 1, levelOddsTable.Length);
            return levelOddsTable[level - 1];
        }

        private int SelectTierByOdds(LevelOdds odds)
        {
            float roll = Random.Range(0f, 100f);
            float cumulative = 0f;

            for (int i = 0; i < odds.tierChances.Length; i++)
            {
                cumulative += odds.tierChances[i];
                if (roll <= cumulative)
                {
                    return i + 1; // Tier starts at 1
                }
            }

            return 1; // Fallback to tier 1
        }

        private UnitStatsSO GetRandomUnitFromTier(int tier)
        {
            if (tierPools == null || tierPools.Length == 0)
            {
                Debug.LogWarning("[ShopPoolSO] Tier pools are empty!");
                return null;
            }
            
            var pool = tierPools.FirstOrDefault(p => p.tier == tier);
            if (pool.units == null || pool.units.Length == 0) return null;

            return pool.units[Random.Range(0, pool.units.Length)];
        }

        /// <summary>
        /// Validate shop pool configuration in the editor
        /// </summary>
        private void OnValidate()
        {
            if (levelOddsTable == null) return;

            // Ensure odds add up to 100% for each level
            foreach (var levelOdd in levelOddsTable)
            {
                if (levelOdd.tierChances == null) continue;

                float total = 0f;
                foreach (var chance in levelOdd.tierChances)
                {
                    total += chance;
                }

                if (Mathf.Abs(total - 100f) > 0.01f)
                {
                    Debug.LogWarning($"Level {levelOdd.level} tier chances don't add up to 100% (currently {total}%)");
                }
            }
        }
    }

    [System.Serializable]
    public struct TierPool
    {
        public int tier;
        public UnitStatsSO[] units;
    }

    [System.Serializable]
    public struct LevelOdds
    {
        public int level;
        [Tooltip("Percentage chance for each tier (should add up to 100)")]
        public float[] tierChances; // [Tier1%, Tier2%, Tier3%, Tier4%, Tier5%]
    }
}
