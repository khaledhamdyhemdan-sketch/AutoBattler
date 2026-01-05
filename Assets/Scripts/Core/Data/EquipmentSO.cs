using UnityEngine;
using AutoBattler.Core.VoidGame;

namespace AutoBattler.Data
{
    /// <summary>
    /// ScriptableObject for equipment items in the Roguelike mode
    /// </summary>
    [CreateAssetMenu(fileName = "Equipment", menuName = "AutoBattler/Data/Equipment", order = 2)]
    public class EquipmentSO : ScriptableObject
    {
        [Header("Basic Info")]
        public string equipmentName;
        public Sprite icon;
        public GearRarity rarity = GearRarity.Common;
        public GearCategory category = GearCategory.Relic;

        [Header("Stats Boosts")]
        public int bonusHealth;
        public int bonusDamage;
        public float bonusAttackSpeed;
        public float bonusAttackRange;
        public float bonusCritChance;
        public float bonusMoveSpeed;

        [Header("Class Compatibility")]
        [Tooltip("The classes that this item is best suited for.")]
        public UnitType[] suitableClasses;

        [Header("Lore")]
        [TextArea(3, 10)]
        public string description;
    }
}
