using UnityEngine;
using System.Collections.Generic;
using AutoBattler.Data;

namespace AutoBattler.Core
{
    public class UnitSelectionManager : MonoBehaviour
    {
        public static UnitSelectionManager Instance { get; private set; }

        [Header("Selection Configuration")]
        [SerializeField] private ShopPoolSO unitPool;
        [SerializeField] private int maxSquadSize = 5;

        private List<UnitStatsSO> availableUnits = new List<UnitStatsSO>();
        private List<UnitStatsSO> selectedSquad = new List<UnitStatsSO>();

        public event System.Action<List<UnitStatsSO>> OnPoolRefreshed;
        public event System.Action<List<UnitStatsSO>> OnSquadChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (unitPool == null)
            {
                unitPool = Resources.Load<ShopPoolSO>("ScriptableObjects/DefaultShopPool");
            }
        }

        public void RefreshAvailableUnits()
        {
            if (unitPool == null) return;
            
            // Reuse ShopPool logic to get random units
            availableUnits = unitPool.GetShopUnits(1, 8); 
            OnPoolRefreshed?.Invoke(availableUnits);
        }

        public bool SelectUnit(int index)
        {
            if (index < 0 || index >= availableUnits.Count) return false;
            if (selectedSquad.Count >= maxSquadSize)
            {
                Debug.LogWarning("[UnitSelectionManager] Squad is full!");
                return false;
            }

            UnitStatsSO unit = availableUnits[index];
            if (unit == null) return false;

            selectedSquad.Add(unit);
            availableUnits[index] = null; // Remove from pool

            OnSquadChanged?.Invoke(selectedSquad);
            OnPoolRefreshed?.Invoke(availableUnits);

            Debug.Log($"[UnitSelectionManager] Selected {unit.unitName}");
            return true;
        }

        public void DeselectUnit(UnitStatsSO unit)
        {
            if (selectedSquad.Remove(unit))
            {
                OnSquadChanged?.Invoke(selectedSquad);
            }
        }

        public void ClearSquad()
        {
            selectedSquad.Clear();
            OnSquadChanged?.Invoke(selectedSquad);
            RefreshAvailableUnits(); // Refresh pool when squad is cleared
        }

        public List<UnitStatsSO> GetSelectedSquad() => selectedSquad;
        
        public List<UnitStatsSO> GetAvailableUnits() => availableUnits;
    }
}
