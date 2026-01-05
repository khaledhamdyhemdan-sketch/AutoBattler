using UnityEngine;
using System.Collections.Generic;
using AutoBattler.Core;
using AutoBattler.Data;
using AutoBattler.Core.VoidGame;

namespace AutoBattler.Testing
{
    public class SmartEquipTestRunner : MonoBehaviour
    {
        public EquipmentSO sword;
        public EquipmentSO staff;
        public UnitStatsSO mageStats;
        public UnitStatsSO warriorStats;

        private void Start()
        {
            RunTests();
        }

        [ContextMenu("Run Smart Equip Tests")]
        public void RunTests()
        {
            Debug.Log("=== SMART EQUIP SYSTEM TESTS ===");

            if (mageStats == null || warriorStats == null || sword == null || staff == null)
            {
                Debug.LogError("✗ Test assets not assigned!");
                return;
            }

            // Test 1: Mage utility for Staff vs Sword
            float mageStaffUtility = SmartEquipSystem.CalculateUtility(mageStats, staff);
            float mageSwordUtility = SmartEquipSystem.CalculateUtility(mageStats, sword);

            if (mageStaffUtility > mageSwordUtility)
            {
                Debug.Log($"✓ Mage Test: Staff ({mageStaffUtility}) > Sword ({mageSwordUtility})");
            }
            else
            {
                Debug.LogError($"✗ Mage Test Failed: Staff ({mageStaffUtility}) <= Sword ({mageSwordUtility})");
            }

            // Test 2: Warrior utility for Sword vs Staff
            float warriorSwordUtility = SmartEquipSystem.CalculateUtility(warriorStats, sword);
            float warriorStaffUtility = SmartEquipSystem.CalculateUtility(warriorStats, staff);

            if (warriorSwordUtility > warriorStaffUtility)
            {
                Debug.Log($"✓ Warrior Test: Sword ({warriorSwordUtility}) > Staff ({warriorStaffUtility})");
            }
            else
            {
                Debug.LogError($"✗ Warrior Test Failed: Sword ({warriorSwordUtility}) <= Staff ({warriorStaffUtility})");
            }

            Debug.Log("=== TESTS COMPLETE ===");
        }
    }
}
