using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutoBattler.Core
{
    /// <summary>
    /// Manages permanent unlocks (units, abilities, synergies) across all runs.
    /// Players unlock new content by achieving milestones (waves survived, enemies killed, etc).
    /// This is the meta-progression system that keeps players returning for "one more run".
    /// </summary>
    public class UnlockManager : MonoBehaviour
    {
        [SerializeField] private List<UnitUnlockData> unitUnlocks = new();
        
        // Persistent player stats (used to check unlock conditions)
        private PlayerProgressData playerProgress;
        
        // Events
        public event Action<string> OnUnitUnlocked;     // unitID
        
        // Singleton
        public static UnlockManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            LoadPlayerProgress();
        }

        private void Start()
        {
            if (RunManager.Instance != null)
            {
                RunManager.Instance.OnRunEnded += HandleRunEnded;
            }
        }

        private void OnDestroy()
        {
            if (RunManager.Instance != null)
            {
                RunManager.Instance.OnRunEnded -= HandleRunEnded;
            }
        }

        private void HandleRunEnded()
        {
            var stats = RunManager.Instance.GetRunStatistics();
            CheckRunCompletionUnlocks(stats);
        }
        
        // ==================== UNLOCK CHECKS ====================
        
        /// <summary>Checks if a unit is unlocked for the player.</summary>
        public bool IsUnitUnlocked(string unitID)
        {
            return playerProgress.unlockedUnitIDs.Contains(unitID);
        }
        
        /// <summary>Gets all currently unlocked units the player can select in UnitSelection.</summary>
        public List<string> GetUnlockedUnits()
        {
            return new List<string>(playerProgress.unlockedUnitIDs);
        }
        
        // ==================== UNLOCK TRIGGERS ====================
        
        /// <summary>Called at the end of each run to check for new unlocks.</summary>
        public void CheckRunCompletionUnlocks(RunStatistics runStats)
        {
            // Update persistent stats
            playerProgress.totalWavesSurvived += runStats.wavesSurvived;
            playerProgress.totalEnemiesDefeated += runStats.enemiesDefeated;
            playerProgress.totalRunsCompleted += 1;
            playerProgress.longestRunWaves = Mathf.Max(playerProgress.longestRunWaves, runStats.wavesSurvived);
            
            // Check unit unlocks
            CheckUnitUnlocks(runStats);
            
            SavePlayerProgress();
            
            Debug.Log($"[UnlockManager] Progress updated. Total waves: {playerProgress.totalWavesSurvived}, " +
                     $"Total kills: {playerProgress.totalEnemiesDefeated}, Runs: {playerProgress.totalRunsCompleted}");
        }
        
        /// <summary>Checks if any units should be unlocked based on run statistics.</summary>
        private void CheckUnitUnlocks(RunStatistics runStats)
        {
            foreach (var unitUnlock in unitUnlocks)
            {
                if (IsUnitUnlocked(unitUnlock.unitID))
                    continue; // Already unlocked
                
                if (unitUnlock.condition.IsMet(playerProgress, runStats))
                {
                    UnlockUnit(unitUnlock.unitID);
                    OnUnitUnlocked?.Invoke(unitUnlock.unitID);
                    Debug.Log($"[UnlockManager] Unit unlocked: {unitUnlock.unitID}");
                }
            }
        }
        
        // ==================== PRIVATE UNLOCK METHODS ====================
        
        private void UnlockUnit(string unitID)
        {
            if (!playerProgress.unlockedUnitIDs.Contains(unitID))
            {
                playerProgress.unlockedUnitIDs.Add(unitID);
            }
        }
        
        // ==================== PERSISTENCE ====================
        
        private void LoadPlayerProgress()
        {
            string json = PlayerPrefs.GetString("PlayerProgress", "");
            
            if (string.IsNullOrEmpty(json))
            {
                // First time player - start with 5 common units
                playerProgress = CreateDefaultProgress();
            }
            else
            {
                try
                {
                    playerProgress = JsonUtility.FromJson<PlayerProgressData>(json);
                    if (playerProgress == null)
                        playerProgress = CreateDefaultProgress();
                }
                catch
                {
                    Debug.LogWarning("[UnlockManager] Failed to load saved progress, creating new");
                    playerProgress = CreateDefaultProgress();
                }
            }
        }
        
        private void SavePlayerProgress()
        {
            try
            {
                string json = JsonUtility.ToJson(playerProgress);
                PlayerPrefs.SetString("PlayerProgress", json);
                PlayerPrefs.Save();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[UnlockManager] Failed to save progress: {ex.Message}");
            }
        }
        
        private PlayerProgressData CreateDefaultProgress()
        {
            var progress = new PlayerProgressData();
            
            // Start with 5 common units
            progress.unlockedUnitIDs.AddRange(new[]
            {
                "Knight",      // Common tank
                "Archer",      // Common ranged DPS
                "Mage",        // Common magic DPS
                "Cleric",      // Common healer
                "Rogue"        // Common melee DPS
            });
            
            Debug.Log("[UnlockManager] Created default progress with 5 units");
            return progress;
        }
        
        public void ResetProgress()
        {
            playerProgress = CreateDefaultProgress();
            SavePlayerProgress();
            Debug.Log("[UnlockManager] Progress reset");
        }
    }
    
    // ==================== DATA STRUCTURES ====================
    
    /// <summary>Defines a unit that can be unlocked.</summary>
    [System.Serializable]
    public class UnitUnlockData
    {
        public string unitID;
        public string displayName;
        public UnlockCondition condition;
    }
    
    /// <summary>Defines a condition that must be met to unlock something.</summary>
    [System.Serializable]
    public class UnlockCondition
    {
        public enum ConditionType
        {
            WavesSurvived,     // Survive X waves total
            EnemiesDefeated,   // Kill X enemies total
            RunsCompleted,     // Complete X runs
            LongestRun         // Survive X waves in a single run
        }
        
        public ConditionType type;
        public int threshold;
        
        /// <summary>Checks if this condition is met.</summary>
        public bool IsMet(PlayerProgressData progress, RunStatistics currentRun)
        {
            return type switch
            {
                ConditionType.WavesSurvived => progress.totalWavesSurvived >= threshold,
                ConditionType.EnemiesDefeated => progress.totalEnemiesDefeated >= threshold,
                ConditionType.RunsCompleted => progress.totalRunsCompleted >= threshold,
                ConditionType.LongestRun => progress.longestRunWaves >= threshold,
                _ => false
            };
        }
    }
    
    /// <summary>Persistent player progress data saved to PlayerPrefs.</summary>
    [System.Serializable]
    public class PlayerProgressData
    {
        // Unlocked content
        public List<string> unlockedUnitIDs = new();
        
        // Cumulative stats (used for unlock conditions)
        public int totalWavesSurvived = 0;
        public int totalEnemiesDefeated = 0;
        public int totalRunsCompleted = 0;
        public int longestRunWaves = 0;
    }
}
