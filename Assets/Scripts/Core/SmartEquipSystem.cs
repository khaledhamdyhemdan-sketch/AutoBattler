using UnityEngine;
using System.Collections.Generic;
using AutoBattler.Data;
using AutoBattler.Core.VoidGame;
using System.Linq;

namespace AutoBattler.Core
{
    public static class SmartEquipSystem
    {
        public static float CalculateUtility(UnitStatsSO unitStats, EquipmentSO equipment)
        {
            if (unitStats == null || equipment == null) return 0f;

            float utility = 0f;

            // 1. Class Compatibility (Major weight)
            if (equipment.suitableClasses != null && equipment.suitableClasses.Contains(unitStats.unitType))
            {
                utility += 100f;
            }

            // 2. Stat Weighting based on Unit Type
            switch (unitStats.unitType)
            {
                case UnitType.Tank:
                case UnitType.Paladin:
                case UnitType.EnemyArmored:
                    utility += equipment.bonusHealth * 2.0f;
                    utility += equipment.bonusDamage * 0.5f;
                    break;

                case UnitType.Mage:
                case UnitType.Healer:
                case UnitType.EnemyCultist:
                    utility += equipment.bonusDamage * 1.5f; // Magic Power scaling
                    utility += equipment.bonusCritChance * 30f;
                    utility += equipment.bonusAttackRange * 2f;
                    break;

                case UnitType.Dps:
                case UnitType.Assassin:
                case UnitType.EnemyFast:
                    utility += equipment.bonusDamage * 1.2f;
                    utility += equipment.bonusAttackSpeed * 25f;
                    utility += equipment.bonusCritChance * 50f;
                    break;

                case UnitType.Archer:
                case UnitType.EnemyRanged:
                    utility += equipment.bonusDamage * 1.0f;
                    utility += equipment.bonusAttackRange * 10f;
                    utility += equipment.bonusAttackSpeed * 20f;
                    break;
            }

            // Generic weights for all
            utility += equipment.bonusMoveSpeed * 5f;

            return utility;
        }

        public static UnitController FindBestUnit(EquipmentSO equipment, List<UnitController> candidates)
        {
            if (equipment == null || candidates == null || candidates.Count == 0) return null;

            UnitController bestUnit = null;
            float maxUtility = -1f;

            foreach (var unit in candidates)
            {
                if (unit == null || unit.UnitStats == null) continue;

                float utility = CalculateUtility(unit.UnitStats, equipment);
                
                // Bias: favor units with fewer items? (Optional, but let's keep it simple for now)
                // Actually, let's just use raw utility.

                if (utility > maxUtility)
                {
                    maxUtility = utility;
                    bestUnit = unit;
                }
            }

            return bestUnit;
        }

        public static bool AutoEquip(EquipmentSO equipment, List<UnitController> candidates)
        {
            UnitController bestUnit = FindBestUnit(equipment, candidates);
            if (bestUnit != null)
            {
                // Note: UnitController currently uses ItemData, we need to adapt it 
                // or ensure EquipmentSO can be converted.
                // For now, we'll just log it as per the plan.
                
                Debug.Log($"[SmartEquip] Best unit for {equipment.equipmentName} is {bestUnit.UnitStats.unitName} (Utility: {CalculateUtility(bestUnit.UnitStats, equipment)})");
                
                // In a real implementation, we would call a method on UnitController to equip it.
                // bestUnit.TryEquipEquipment(equipment);
                return true;
            }

            return false;
        }
    }
}