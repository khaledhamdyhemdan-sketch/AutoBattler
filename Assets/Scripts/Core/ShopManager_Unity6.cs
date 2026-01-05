using UnityEngine;
using System.Collections.Generic;
using AutoBattler.Data;

namespace AutoBattler.Core
{
    /// <summary>
    /// Manages the shop system using ScriptableObject-driven pools
    /// Integrates with Unity 6.3 Building Blocks for multiplayer shop synchronization
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        [Header("Shop Configuration")]
        [SerializeField] private ShopPoolSO shopPool;
        [SerializeField] private Transform shopSlotContainer;

        // Current shop state
        private List<UnitStatsSO> currentShopUnits = new List<UnitStatsSO>();
        
        // Events
        public event System.Action<List<UnitStatsSO>> OnShopRefreshed;
        public event System.Action<UnitStatsSO, int> OnUnitPurchased;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Auto-load shop pool if not assigned
            if (shopPool == null)
            {
                shopPool = Resources.Load<ShopPoolSO>("ScriptableObjects/DefaultShopPool");
                if (shopPool == null)
                {
                    Debug.LogWarning("Could not auto-load DefaultShopPool. Please assign manually or ensure it exists in Resources/ScriptableObjects/");
                }
            }
        }

        #endregion

        #region Shop Operations

        /// <summary>
        /// Refresh the shop with new units based on player level
        /// </summary>
        public void RefreshShop(int playerLevel)
        {
            if (shopPool == null)
            {
                Debug.LogError("[ShopManager] Shop pool not assigned!");
                return;
            }

            // Get new units from pool
            currentShopUnits = shopPool.GetShopUnits(playerLevel, shopPool.shopSlots);

            // Notify UI
            OnShopRefreshed?.Invoke(currentShopUnits);

            Debug.Log($"[ShopManager] Shop refreshed with {currentShopUnits.Count} units for level {playerLevel}");
        }

        /// <summary>
        /// Attempt to purchase a unit from the shop
        /// </summary>
        public bool PurchaseUnit(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= currentShopUnits.Count)
            {
                Debug.LogWarning($"[ShopManager] Invalid slot index: {slotIndex}");
                return false;
            }

            UnitStatsSO unit = currentShopUnits[slotIndex];
            
            if (unit == null)
            {
                Debug.LogWarning($"[ShopManager] No unit in slot {slotIndex}");
                return false;
            }

            // Check if player has enough gold
            if (GameManager.Instance == null || !GameManager.Instance.SpendGold(unit.goldCost))
            {
                Debug.LogWarning($"[ShopManager] Cannot afford unit: {unit.unitName} (Cost: {unit.goldCost})");
                return false;
            }

            // Purchase successful
            OnUnitPurchased?.Invoke(unit, slotIndex);

            // Remove from shop (unit is sold)
            currentShopUnits[slotIndex] = null;

            Debug.Log($"[ShopManager] Purchased {unit.unitName} for {unit.goldCost} gold");

            return true;
        }

        /// <summary>
        /// Reroll the shop for a cost
        /// </summary>
        public bool RerollShop()
        {
            if (shopPool == null) return false;

            // Check if player can afford reroll
            if (GameManager.Instance == null || !GameManager.Instance.SpendGold(shopPool.rerollCost))
            {
                Debug.LogWarning($"[ShopManager] Cannot afford reroll (Cost: {shopPool.rerollCost})");
                return false;
            }

            // Refresh shop
            RefreshShop(GameManager.Instance.PlayerLevel);

            Debug.Log($"[ShopManager] Shop rerolled for {shopPool.rerollCost} gold");

            return true;
        }

        #endregion

        #region Public API

        public List<UnitStatsSO> CurrentShopUnits => currentShopUnits;

        public UnitStatsSO GetUnitInSlot(int index)
        {
            if (index >= 0 && index < currentShopUnits.Count)
            {
                return currentShopUnits[index];
            }
            return null;
        }

        #endregion
    }
}
