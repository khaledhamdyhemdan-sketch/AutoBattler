using UnityEngine;
using AutoBattler.Core;
using System.Collections.Generic;

namespace AutoBattler.Core
{
    public class LootManager : MonoBehaviour
    {
        public static LootManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void HandleLootDrop(ItemData[] possibleDrops, Vector3 position)
        {
            if (possibleDrops == null || possibleDrops.Length == 0) return;

            // Simple random drop logic (e.g. 30% chance)
            if (Random.value > 0.3f) return;

            ItemData droppedItem = possibleDrops[Random.Range(0, possibleDrops.Length)];
            Debug.Log($"[LootManager] Dropped item: {droppedItem.itemName}");

            // Auto-equip logic: Find a player unit with an empty slot
            AutoEquipToPlayerUnit(droppedItem);
        }

        private void AutoEquipToPlayerUnit(ItemData item)
        {
            // Get all player units from CombatManager
            var playerUnits = CombatManager.Instance.GetPlayerUnits();
            
            foreach (var unit in playerUnits)
            {
                if (unit != null && !unit.IsDead)
                {
                    if (unit.TryEquipItem(item))
                    {
                        // Successfully equipped
                        return;
                    }
                }
            }

            // If no unit could equip it, add to inventory
            InventoryManager.Instance?.AddItem(item);
        }
    }
}
