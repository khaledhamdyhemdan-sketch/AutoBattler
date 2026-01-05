using UnityEngine;

namespace AutoBattler.AI
{
    /// <summary>
    /// Automatically applies the correct enemy atlas material on Awake.
    /// Attach this to enemy prefabs to ensure proper texturing.
    /// </summary>
    public class EnemyMaterialApplier : MonoBehaviour
    {
        [Header("Material Assignment")]
        [SerializeField] private EnemyType enemyType = EnemyType.Big;
        
        [Header("Material Paths (Auto-loaded from Resources)")]
        private Material bigEnemyMaterial;
        private Material flyingEnemyMaterial;
        
        public enum EnemyType
        {
            Big,
            Flying
        }
        
        private void Awake()
        {
            LoadMaterials();
            ApplyMaterial();
        }
        
        private void LoadMaterials()
        {
        // Try to load materials from Assets path directly
#if UNITY_EDITOR
        bigEnemyMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Enemies/BigEnemyMaterial.mat");
        flyingEnemyMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Enemies/FlyingEnemyMaterial.mat");
#else
        // For builds, load from Resources
        bigEnemyMaterial = Resources.Load<Material>("Materials/Enemies/BigEnemyMaterial");
        flyingEnemyMaterial = Resources.Load<Material>("Materials/Enemies/FlyingEnemyMaterial");
#endif
        
        if (bigEnemyMaterial == null || flyingEnemyMaterial == null)
        {
            Debug.LogWarning("[EnemyMaterialApplier] Could not load materials! Enemies will appear purple.");
        }
    }
    
    private void ApplyMaterial()
    {
        Material materialToApply = enemyType == EnemyType.Big ? bigEnemyMaterial : flyingEnemyMaterial;
        
        if (materialToApply == null)
        {
            Debug.LogWarning($"[EnemyMaterialApplier] Could not load {enemyType} enemy material!");
            return;
        }
        
        // Apply to all renderers in this GameObject and children
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            Material[] materials = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = materialToApply;
            }
            renderer.sharedMaterials = materials;
        }
        
        Debug.Log($"[EnemyMaterialApplier] Applied {enemyType} material to {gameObject.name} ({renderers.Length} renderers)");
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// Editor-only: Apply material immediately in edit mode
    /// </summary>
    [ContextMenu("Apply Material Now")]
    private void ApplyMaterialEditor()
    {
        LoadMaterials();
        ApplyMaterial();
        UnityEditor.EditorUtility.SetDirty(gameObject);
    }
#endif
}
}
