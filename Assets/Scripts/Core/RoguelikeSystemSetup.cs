using UnityEngine;

namespace AutoBattler.Core
{
    /// <summary>
    /// Automatic setup script for roguelike progression systems.
    /// Add this to your persistent managers scene to initialize all roguelike systems.
    /// 
    /// SETUP INSTRUCTIONS:
    /// 1. In your persistent managers scene, create an empty GameObject named "RoguelikeSystemsManager"
    /// 2. Add this script as a component
    /// 3. That's it! All systems will auto-initialize on scene load.
    /// 
    /// This will create:
    /// - RunManager (tracks current run progress)
    /// - UnlockManager (persistent meta-progression unlocks)
    /// - AchievementSystem (achievements with observer pattern)
    /// 
    /// These will automatically hook into GameManager's phase system with ZERO additional setup needed.
    /// </summary>
    public class RoguelikeSystemSetup : MonoBehaviour
    {
        [SerializeField] private bool createRunManager = true;
        [SerializeField] private bool createUnlockManager = true;
        [SerializeField] private bool createAchievementSystem = true;
        
        private void Awake()
        {
            // Ensure GameManager exists first
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[RoguelikeSystemSetup] GameManager not found. Roguelike systems require GameManager to be initialized first.");
                return;
            }
            
            // Create RunManager if needed
            if (createRunManager && RunManager.Instance == null)
            {
                var runManagerGO = new GameObject("RunManager");
                var runManager = runManagerGO.AddComponent<RunManager>();
                Debug.Log("[RoguelikeSystemSetup] Created RunManager");
            }
            
            // Create UnlockManager if needed
            if (createUnlockManager && UnlockManager.Instance == null)
            {
                var unlockManagerGO = new GameObject("UnlockManager");
                var unlockManager = unlockManagerGO.AddComponent<UnlockManager>();
                Debug.Log("[RoguelikeSystemSetup] Created UnlockManager");
            }
            
            // Create AchievementSystem if needed
            if (createAchievementSystem && AchievementSystem.Instance == null)
            {
                var achievementGO = new GameObject("AchievementSystem");
                var achievement = achievementGO.AddComponent<AchievementSystem>();
                Debug.Log("[RoguelikeSystemSetup] Created AchievementSystem");
            }
            
            Debug.Log("[RoguelikeSystemSetup] Roguelike progression systems initialized successfully!");
        }
    }
}
