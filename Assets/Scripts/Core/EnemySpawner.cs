using UnityEngine;
using System.Collections.Generic;
using AutoBattler.AI;
using AutoBattler.Data;

namespace AutoBattler.Core
{
    /// <summary>
    /// Spawns enemies based on wave number and difficulty scaling.
    /// Loads enemy models from Assets/Models/Enemies/ and configures them.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        public static EnemySpawner Instance { get; private set; }

        [Header("Spawn Settings")]
        [SerializeField] private Transform spawnParent;
        [SerializeField] private Vector3 spawnAreaCenter = new Vector3(0, 0, 10);
        [SerializeField] private float spawnAreaRadius = 5f;
        [SerializeField] private float spawnHeight = 0.5f;

        [Header("Enemy Configuration")]
        [SerializeField] private UnitStatsSO enemyBaseStats;
        [SerializeField] private float healthScalingPerWave = 1.15f;
        [SerializeField] private float damageScalingPerWave = 1.1f;

        [Header("Wave Settings")]
        [SerializeField] private int baseEnemyCount = 3;
        [SerializeField] private int enemiesPerWave = 2;
        [SerializeField] private float flyingEnemyChance = 0.3f;
        
        private RuntimeAnimatorController enemyAnimatorController;

        // Enemy model paths
        private string[] bigEnemyModels = new string[]
        {
            "Alien", "Birb", "BlueDemon", "Bunny", "Cactoro", "Demon", "Dino", "Fish",
            "Frog", "Monkroose", "MushroomKing", "Ninja", "Orc", "Orc_Skull", "Tribal", "Yeti"
        };

        private string[] flyingEnemyModels = new string[]
        {
            "Alpaking", "Alpaking_Evolved", "Armabee", "Armabee_Evolved", "Demon", "Dragon",
            "Dragon_Evolved", "Ghost", "Ghost_Skull", "Glub", "Glub_Evolved", "Goleling",
            "Goleling_Evolved", "Hywirl", "Pigeon", "Squidle", "Tribal"
        };

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            enemyAnimatorController = Resources.Load<RuntimeAnimatorController>("EnemyAnimator");
            if (enemyAnimatorController == null)
            {
                Debug.LogWarning("[EnemySpawner] Could not load EnemyAnimator from Resources. Please run Tools/Create Enemy Animator.");
            }

            // Auto-create spawn parent if not assigned
            if (spawnParent == null)
            {
                GameObject parentObj = new GameObject("EnemySpawnParent");
                spawnParent = parentObj.transform;
            }

            // Auto-load enemy stats if not assigned
            if (enemyBaseStats == null)
            {
                enemyBaseStats = Resources.Load<UnitStatsSO>("ScriptableObjects/GoblinStats");
                if (enemyBaseStats == null)
                {
                    Debug.LogWarning("[EnemySpawner] No enemy base stats assigned! Enemies may not work correctly.");
                }
            }
        }

        /// <summary>
        /// Spawn enemies for the given wave number
        /// </summary>
        public void SpawnWave(int waveNumber)
        {
            int enemyCount = baseEnemyCount + (enemiesPerWave * (waveNumber - 1));
            
            Debug.Log($"[EnemySpawner] Spawning wave {waveNumber} with {enemyCount} enemies");

            for (int i = 0; i < enemyCount; i++)
            {
                SpawnRandomEnemy(waveNumber);
            }
        }

        private void SpawnRandomEnemy(int waveNumber)
        {
            // Determine if this should be a flying enemy
            bool isFlying = Random.value < flyingEnemyChance;
            
            // Select random model
            string modelName;
            string modelPath;
            
            if (isFlying)
            {
                modelName = flyingEnemyModels[Random.Range(0, flyingEnemyModels.Length)];
                modelPath = $"Assets/Models/Enemies/Flying/{modelName}.fbx";
            }
            else
            {
                modelName = bigEnemyModels[Random.Range(0, bigEnemyModels.Length)];
                modelPath = $"Assets/Models/Enemies/Big/{modelName}.fbx";
            }

            // Load the model
#if UNITY_EDITOR
            GameObject modelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
#else
            // For build, you would need to use Resources or Addressables
            GameObject modelPrefab = null;
            Debug.LogError("[EnemySpawner] Runtime enemy loading not implemented. Use editor or implement Addressables.");
#endif

            if (modelPrefab == null)
            {
                Debug.LogWarning($"[EnemySpawner] Could not load enemy model: {modelPath}");
                return;
            }

            // Calculate spawn position
            Vector3 spawnPos = GetRandomSpawnPosition();

            // Instantiate the enemy
            GameObject enemyObj = Instantiate(modelPrefab, spawnPos, Quaternion.identity, spawnParent);
            enemyObj.name = $"{modelName}_Wave{waveNumber}";

            // Add required components
            SetupEnemyComponents(enemyObj, waveNumber, isFlying);

            Debug.Log($"[EnemySpawner] Spawned {modelName} ({(isFlying ? "Flying" : "Big")}) at {spawnPos}");
        }

        private void SetupEnemyComponents(GameObject enemyObj, int waveNumber, bool isFlying)
        {
            // Add UnitController if not present
            UnitController unitController = enemyObj.GetComponent<UnitController>();
            if (unitController == null)
            {
                unitController = enemyObj.AddComponent<UnitController>();
            }

            // Setup Animator
            Animator animator = enemyObj.GetComponent<Animator>();
            if (animator == null)
            {
                animator = enemyObj.AddComponent<Animator>();
            }
            if (animator.runtimeAnimatorController == null && enemyAnimatorController != null)
            {
                animator.runtimeAnimatorController = enemyAnimatorController;
            }

            // Create scaled stats for this wave
            UnitStatsSO scaledStats = ScriptableObject.CreateInstance<UnitStatsSO>();
            if (enemyBaseStats != null)
            {
                // Copy base stats
                scaledStats.unitName = enemyObj.name;
                scaledStats.maxHealth = Mathf.RoundToInt(enemyBaseStats.maxHealth * Mathf.Pow(healthScalingPerWave, waveNumber - 1));
                scaledStats.attackDamage = Mathf.RoundToInt(enemyBaseStats.attackDamage * Mathf.Pow(damageScalingPerWave, waveNumber - 1));
                scaledStats.attackSpeed = enemyBaseStats.attackSpeed;
                scaledStats.attackRange = enemyBaseStats.attackRange;
                scaledStats.moveSpeed = enemyBaseStats.moveSpeed;
                scaledStats.criticalChance = enemyBaseStats.criticalChance;
                scaledStats.criticalMultiplier = enemyBaseStats.criticalMultiplier;
                scaledStats.rotationSpeed = enemyBaseStats.rotationSpeed;
            }

            // Assign stats via reflection (since unitStats is private)
            var unitStatsField = typeof(UnitController).GetField("unitStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (unitStatsField != null)
            {
                unitStatsField.SetValue(unitController, scaledStats);
            }

            // Assign team
            var teamField = typeof(UnitController).GetField("team", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (teamField != null)
            {
                teamField.SetValue(unitController, UnitTeam.Enemy);
            }

            // Add AI component
            if (isFlying)
            {
                if (enemyObj.GetComponent<FlyingEnemyAI>() == null)
                {
                    enemyObj.AddComponent<FlyingEnemyAI>();
                }
            }
            else
            {
                if (enemyObj.GetComponent<BigEnemyAI>() == null)
                {
                    enemyObj.AddComponent<BigEnemyAI>();
                }
            }

            // Add material applier
            EnemyMaterialApplier materialApplier = enemyObj.GetComponent<EnemyMaterialApplier>();
            if (materialApplier == null)
            {
                materialApplier = enemyObj.AddComponent<EnemyMaterialApplier>();
            }
            
            // Set enemy type via reflection
            var enemyTypeField = typeof(EnemyMaterialApplier).GetField("enemyType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (enemyTypeField != null)
            {
                enemyTypeField.SetValue(materialApplier, isFlying ? EnemyMaterialApplier.EnemyType.Flying : EnemyMaterialApplier.EnemyType.Big);
            }

            // Add Rigidbody if not present
            Rigidbody rb = enemyObj.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = enemyObj.AddComponent<Rigidbody>();
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
            }

            // Add collider if not present
            if (enemyObj.GetComponent<Collider>() == null)
            {
                CapsuleCollider collider = enemyObj.AddComponent<CapsuleCollider>();
                collider.height = 2f;
                collider.radius = 0.5f;
                collider.center = new Vector3(0, 1f, 0);
            }

            // Register with combat manager
            CombatManager.Instance?.RegisterEnemyUnit(unitController);
        }

        private Vector3 GetRandomSpawnPosition()
        {
            // Random position within spawn area
            Vector2 randomCircle = Random.insideUnitCircle * spawnAreaRadius;
            Vector3 spawnPos = spawnAreaCenter + new Vector3(randomCircle.x, spawnHeight, randomCircle.y);
            return spawnPos;
        }

        /// <summary>
        /// Clear all spawned enemies
        /// </summary>
        public void ClearAllEnemies()
        {
            if (spawnParent != null)
            {
                foreach (Transform child in spawnParent)
                {
                    Destroy(child.gameObject);
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Visualize spawn area in editor
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(spawnAreaCenter, spawnAreaRadius);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnAreaCenter, 0.5f);
        }
#endif
    }
}
