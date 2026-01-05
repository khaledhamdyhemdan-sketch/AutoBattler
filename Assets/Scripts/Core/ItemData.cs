using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "AutoBattler/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public int bonusHealth;
    public int bonusDamage;
    public float bonusAttackSpeed;
    public Sprite icon;
}
