using UnityEngine;

namespace AutoBattler.Core
{
    /// <summary>
    /// Setup helper for PersistentManagers scene.
    /// Ensures EnemySpawner GameObject exists with proper configuration.
    /// </summary>
    public class PersistentManagersSetup : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureEnemySpawnerExists()
        {
            // Check if EnemySpawner already exists
            EnemySpawner existingSpawner = Object.FindFirstObjectByType<EnemySpawner>();
            if (existingSpawner != null)
            {
                Debug.Log("[PersistentManagersSetup] EnemySpawner already exists in scene");
                return;
            }

            // Create EnemySpawner GameObject
            GameObject spawnerObj = new GameObject("EnemySpawner");
            EnemySpawner spawner = spawnerObj.AddComponent<EnemySpawner>();

            // Configure spawn area (center field of view, slightly forward)
            spawner.GetType().GetField("spawnAreaCenter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(spawner, new Vector3(0, 0.5f, 10));

            spawner.GetType().GetField("spawnAreaRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(spawner, 5f);

            spawner.GetType().GetField("spawnHeight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(spawner, 0.5f);

            // Wave settings
            spawner.GetType().GetField("baseEnemyCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(spawner, 3);

            spawner.GetType().GetField("enemiesPerWave", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(spawner, 2);

            spawner.GetType().GetField("flyingEnemyChance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(spawner, 0.3f);

            Debug.Log("[PersistentManagersSetup] EnemySpawner GameObject created and configured");
        }
    }
}
