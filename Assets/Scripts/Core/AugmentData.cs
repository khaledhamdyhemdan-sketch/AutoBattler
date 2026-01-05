using UnityEngine;

[CreateAssetMenu(fileName = "NewAugmentData", menuName = "AutoBattler/Augment Data")]
public class AugmentData : ScriptableObject
{
    public string augmentName;
    public string description;
    public float healthMultiplier = 1.0f;
    public float damageMultiplier = 1.0f;
    public float attackSpeedMultiplier = 1.0f;
}
