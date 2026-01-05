using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutoBattler.Core
{
    /// <summary>
    /// Manages achievement system using the Observer pattern.
    /// UI systems listen to achievements independently (zero coupling to combat code).
    /// 
    /// Design Pattern Reference: Game Programming Patterns - Observer Pattern
    /// https://gameprogrammingpatterns.com/observer.html
    /// </summary>
    public class AchievementSystem : MonoBehaviour
    {
        [SerializeField] private List<AchievementDefinition> achievements = new();
        
        private Dictionary<string, AchievementProgress> achievementProgress = new();
        
        // Events (one-way: achievements notify UI, no business logic depends on achievements)
        public event Action<Achievement> OnAchievementUnlocked;
        public event Action<string, int> OnProgressUpdated; // achievementID, newProgress
        
        // Singleton
        public static AchievementSystem Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            InitializeAchievements();
        }
        
        private void OnEnable()
        {
            if (RunManager.Instance != null)
            {
                RunManager.Instance.OnWaveSurvived += CheckWaveAchievements;
                RunManager.Instance.OnRunEnded += CheckRunCompletionAchievements;
            }
        }
        
        private void OnDisable()
        {
            if (RunManager.Instance != null)
            {
                RunManager.Instance.OnWaveSurvived -= CheckWaveAchievements;
                RunManager.Instance.OnRunEnded -= CheckRunCompletionAchievements;
            }
        }
        
        // ==================== ACHIEVEMENT CHECKS ====================
        // Each method independently checks relevant achievements
        
        private void CheckWaveAchievements(int wave)
        {
            // Wave survival achievements
            CheckAndUnlock("Survive5Waves", (wave >= 5) ? 1 : 0);
            CheckAndUnlock("Survive10Waves", (wave >= 10) ? 1 : 0);
            CheckAndUnlock("Survive15Waves", (wave >= 15) ? 1 : 0);
            CheckAndUnlock("Survive20Waves", (wave >= 20) ? 1 : 0);
        }
        
        private void CheckRunCompletionAchievements()
        {
            // Run completion achievements
            IncrementAndCheck("RunsCompleted", 1);
        }
        
        // ==================== INTERNAL HELPERS ====================
        
        private void InitializeAchievements()
        {
            foreach (var definition in achievements)
            {
                achievementProgress[definition.id] = new AchievementProgress
                {
                    id = definition.id,
                    currentProgress = 0,
                    target = definition.targetProgress,
                    isUnlocked = false
                };
            }
            
            Debug.Log($"[AchievementSystem] Initialized {achievements.Count} achievements");
        }
        
        private void CheckAndUnlock(string achievementID, int progressValue)
        {
            if (!achievementProgress.ContainsKey(achievementID))
            {
                Debug.LogWarning($"[AchievementSystem] Achievement {achievementID} not found in definitions");
                return;
            }
            
            var progress = achievementProgress[achievementID];
            
            if (progress.isUnlocked)
                return; // Already unlocked
            
            if (progressValue > 0 && progress.currentProgress < progress.target)
            {
                progress.currentProgress = progressValue;
                OnProgressUpdated?.Invoke(achievementID, progress.currentProgress);
                
                if (progress.currentProgress >= progress.target)
                {
                    UnlockAchievement(achievementID);
                }
            }
        }
        
        private void IncrementAndCheck(string achievementID, int amount = 1)
        {
            if (!achievementProgress.ContainsKey(achievementID))
            {
                Debug.LogWarning($"[AchievementSystem] Achievement {achievementID} not found");
                return;
            }
            
            var progress = achievementProgress[achievementID];
            
            if (progress.isUnlocked)
                return; // Already unlocked
            
            progress.currentProgress += amount;
            OnProgressUpdated?.Invoke(achievementID, progress.currentProgress);
            
            if (progress.currentProgress >= progress.target)
            {
                UnlockAchievement(achievementID);
            }
        }
        
        private void UnlockAchievement(string achievementID)
        {
            if (!achievementProgress.ContainsKey(achievementID))
                return;
            
            var progress = achievementProgress[achievementID];
            if (progress.isUnlocked)
                return;
            
            progress.isUnlocked = true;
            
            // Find the definition
            var definition = achievements.Find(a => a.id == achievementID);
            if (definition != null)
            {
                var achievement = new Achievement
                {
                    id = definition.id,
                    displayName = definition.displayName,
                    description = definition.description,
                    icon = definition.icon,
                    unlockedTime = DateTime.Now
                };
                
                OnAchievementUnlocked?.Invoke(achievement);
                Debug.Log($"[AchievementSystem] Achievement unlocked: {achievement.displayName}");
            }
        }
        
        // ==================== PUBLIC API ====================
        
        public bool IsAchievementUnlocked(string achievementID)
        {
            return achievementProgress.ContainsKey(achievementID) &&
                   achievementProgress[achievementID].isUnlocked;
        }
        
        public int GetAchievementProgress(string achievementID)
        {
            return achievementProgress.ContainsKey(achievementID)
                ? achievementProgress[achievementID].currentProgress
                : 0;
        }
        
        public List<string> GetUnlockedAchievementIDs()
        {
            var unlocked = new List<string>();
            foreach (var kvp in achievementProgress)
            {
                if (kvp.Value.isUnlocked)
                    unlocked.Add(kvp.Key);
            }
            return unlocked;
        }
        
        public void ResetAllAchievements()
        {
            achievementProgress.Clear();
            InitializeAchievements();
            Debug.Log("[AchievementSystem] All achievements reset");
        }
    }
    
    // ==================== DATA STRUCTURES ====================
    
    /// <summary>Definition of an achievement (configured in inspector).</summary>
    [System.Serializable]
    public class AchievementDefinition
    {
        public string id;
        public string displayName;
        public string description;
        public Sprite icon;
        public int targetProgress = 1; // How many times must condition be met?
    }
    
    /// <summary>Runtime progress tracking for an achievement.</summary>
    public class AchievementProgress
    {
        public string id;
        public int currentProgress;
        public int target;
        public bool isUnlocked;
    }
    
    /// <summary>A completed/unlocked achievement.</summary>
    [System.Serializable]
    public class Achievement
    {
        public string id;
        public string displayName;
        public string description;
        public Sprite icon;
        public DateTime unlockedTime;
    }
}
