using UnityEngine;
using System.Collections.Generic;

public class AugmentManager : MonoBehaviour
{
    public static AugmentManager Instance { get; private set; }

    public List<AugmentData> activeAugments = new List<AugmentData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ApplyAugment(AugmentData augment)
    {
        activeAugments.Add(augment);
        Debug.Log($"Applied augment: {augment.augmentName}");
        // Logic to apply multipliers to units
    }
}
