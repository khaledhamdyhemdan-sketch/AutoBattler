using UnityEngine;
using System.Collections.Generic;

namespace AutoBattler.Data
{
    /// <summary>
    /// Database of all equipment available in the Roguelike mode
    /// </summary>
    [CreateAssetMenu(fileName = "EquipmentDatabase", menuName = "AutoBattler/Data/Equipment Database", order = 3)]
    public class EquipmentDatabaseSO : ScriptableObject
    {
        public List<EquipmentSO> allEquipment;

        public List<EquipmentSO> GetRandomEquipment(int count)
        {
            List<EquipmentSO> result = new List<EquipmentSO>();
            if (allEquipment == null || allEquipment.Count == 0) return result;

            List<EquipmentSO> pool = new List<EquipmentSO>(allEquipment);
            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int index = Random.Range(0, pool.Count);
                result.Add(pool[index]);
                pool.RemoveAt(index);
            }

            return result;
        }
    }
}
