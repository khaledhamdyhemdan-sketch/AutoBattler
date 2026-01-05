using System;
using UnityEngine;

namespace AutoBattler.Core
{
    /// <summary>
    /// Manages a single run's progression and statistics.
    /// Tracks waves survived, enemies defeated, time alive, and run completion status.
    /// This is the foundation for roguelike meta-progression.
    /// </summary>
    public class RunManager : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        
        // Run state
        private int currentWave = 0;
        private int enemiesDefeated = 0;
        private float survivalTime = 0f;
        private int totalDamageDealt = 0;
        private int goldCollected = 0;
        private bool isRunActive = false;
        
        // Events
        public event Action<int> OnWaveSurvived;  // Fired when player survives a wave
        public event Action OnRunEnded;           // Fired when run terminates
        public event Action OnRunStarted;         // Fired when run begins
        
        // Singleton
        public static RunManager Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
        
        private void OnEnable()
        {
            if (gameManager == null)
                gameManager = GameManager.Instance;
            
            if (gameManager != null)
            {
                // Subscribe to game phase changes
                gameManager.OnPhaseChanged += HandlePhaseChanged;
            }
        }
        
        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.OnPhaseChanged -= HandlePhaseChanged;
            }
        }
        
        private void Update()
        {
            if (isRunActive)
            {
                survivalTime += Time.deltaTime;
            }
        }
        
        // ==================== PUBLIC API ====================
        
        /// <summary>Starts a new run with stats reset to 0.</summary>
        public void StartRun()
        {
            currentWave = 0;
            enemiesDefeated = 0;
            survivalTime = 0f;
            totalDamageDealt = 0;
            goldCollected = 0;
            isRunActive = true;
            
            OnRunStarted?.Invoke();
            Debug.Log("[RunManager] Run started");
        }
        
        /// <summary>Ends the current run (called on squad wipe or player quit).</summary>
        public void EndRun()
        {
            if (!isRunActive)
                return;
            
            isRunActive = false;
            OnRunEnded?.Invoke();
            
            Debug.Log($"[RunManager] Run ended. Stats: {GetRunStatistics()}");
        }
        
        // ==================== EVENT HANDLERS ====================
        
        /// <summary>Called when game phase changes to track waves and run completion.</summary>
        private void HandlePhaseChanged(GamePhase newPhase)
        {
            if (newPhase == GamePhase.Combat)
            {
                // Sync wave number with game round
                if (gameManager != null)
                {
                    currentWave = gameManager.CurrentRound;
                }
            }
            else if (newPhase == GamePhase.Results)
            {
                // Wave survived
                OnWaveSurvived?.Invoke(currentWave);
            }
        }
        
        // ==================== GETTERS ====================
        
        public int CurrentWave => currentWave;
        public int EnemiesDefeated => enemiesDefeated;
        public float SurvivalTime => survivalTime;
        public int TotalDamageDealt => totalDamageDealt;
        public int GoldCollected => goldCollected;
        public bool IsRunActive => isRunActive;
        
        /// <summary>Gets a summary of the current run's statistics.</summary>
        public RunStatistics GetRunStatistics()
        {
            return new RunStatistics
            {
                wavesSurvived = currentWave,
                enemiesDefeated = enemiesDefeated,
                survivalTime = survivalTime,
                totalDamageDealt = totalDamageDealt,
                goldCollected = goldCollected,
                completionDate = DateTime.Now
            };
        }
        
        /// <summary>Adds damage to the run total (called by CombatManager).</summary>
        public void AddDamage(int damage)
        {
            if (!isRunActive) return;
            totalDamageDealt += damage;
        }
        
        /// <summary>Adds gold to the run total (called by GameManager).</summary>
        public void AddGold(int amount)
        {
            if (!isRunActive) return;
            goldCollected += amount;
        }
        
        /// <summary>Increments enemies defeated (called from CombatManager).</summary>
        public void IncrementEnemiesDefeated()
        {
            if (!isRunActive) return;
            enemiesDefeated++;
        }
    }
    
    /// <summary>Immutable data structure representing a completed run's statistics.</summary>
    [System.Serializable]
    public class RunStatistics
    {
        public int wavesSurvived;
        public int enemiesDefeated;
        public float survivalTime;
        public int totalDamageDealt;
        public int goldCollected;
        public DateTime completionDate;
        
        public override string ToString()
        {
            return $"Run Stats: {wavesSurvived} waves, {enemiesDefeated} kills, " +
                   $"{(int)survivalTime}s, {totalDamageDealt} damage, {goldCollected} gold";
        }
    }
}
