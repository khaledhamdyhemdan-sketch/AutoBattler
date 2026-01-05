using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using System.Collections.Generic;

namespace AutoBattler.Core
{
    /// <summary>
    /// Unity 6.3 GameManager using Building Blocks pattern for phase management
    /// Supports Preparation and Combat phases with async transitions
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Phase Configuration")]
        [SerializeField] private float preparationDuration = 30f;
        [SerializeField] private float combatDuration = 60f;
        [SerializeField] private bool autoProgressPhases = true;

        [Header("Round Settings")]
        [SerializeField] private int startingGold = 10;
        [SerializeField] private int goldPerRound = 5;
        [SerializeField] private int maxPlayerHealth = 100;

        [Header("References")]
        [SerializeField] private ShopManager shopManager;
        [SerializeField] private CombatManager combatManager;
        [SerializeField] private BattlefieldGrid battlefieldGrid;

        // Phase state
        private GamePhase currentPhase = GamePhase.Initialization;
        private float phaseTimer;
        private int currentRound = 1;
        
        // Player state
        private int playerGold;
        private int playerHealth;
        private int playerLevel = 1;
        private int playerExperience = 0;

        // Events for Building Blocks integration
        public event System.Action<GamePhase> OnPhaseChanged;
        public event System.Action<int> OnRoundChanged;
        public event System.Action<int> OnGoldChanged;
        public event System.Action<int> OnHealthChanged;
        public event System.Action<int> OnLevelChanged;

        // Public properties
        public float PhaseTimeRemaining => phaseTimer;
        public float PhaseTimer => phaseTimer; // Alias for compatibility
        public GamePhase CurrentPhase => currentPhase;
        public int CurrentRound => currentRound;
        public int PlayerGold => playerGold;
        public int PlayerHealth => playerHealth;
        public int PlayerLevel => playerLevel;
        public int PlayerExperience => playerExperience;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializeGameState();
        }

        private void Start()
        {
            // Don't auto-transition - let SceneLoader control initial phase
            // TransitionToPhase will be called by SceneLoader
        }

        private void Update()
        {
            UpdatePhaseTimer();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Initialization

        private void InitializeGameState()
        {
            currentRound = 1;
            playerGold = startingGold;
            playerHealth = maxPlayerHealth;
            playerLevel = 1;
            playerExperience = 0;

            RunManager.Instance?.StartRun();

            Debug.Log("[GameManager] Game state initialized");
        }

        #endregion

        #region Phase Management

        public void TransitionToPhase(GamePhase newPhase)
        {
            // Exit current phase
            ExitPhase(currentPhase);

            // Update phase
            GamePhase previousPhase = currentPhase;
            currentPhase = newPhase;

            Debug.Log($"[GameManager] Phase Transition: {previousPhase} → {newPhase}");

            // Enter new phase
            EnterPhase(newPhase);

            // Notify listeners
            OnPhaseChanged?.Invoke(newPhase);
        }

        private void EnterPhase(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.MainMenu:
                    EnterMainMenuPhase();
                    break;

                case GamePhase.Selection:
                    EnterSelectionPhase();
                    break;

                case GamePhase.Preparation:
                    EnterPreparationPhase();
                    break;

                case GamePhase.Combat:
                    EnterCombatPhase();
                    break;

                case GamePhase.Results:
                    EnterResultsPhase();
                    break;

                case GamePhase.GameOver:
                    EnterGameOverPhase();
                    break;
            }
        }

        private void ExitPhase(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.MainMenu:
                    // No exit actions needed for main menu
                    break;

                case GamePhase.Preparation:
                    ExitPreparationPhase();
                    break;

                case GamePhase.Combat:
                    ExitCombatPhase();
                    break;
            }
        }

        #endregion

        #region Main Menu Phase

        private void EnterMainMenuPhase()
        {
            phaseTimer = 0f;
            autoProgressPhases = false; // Don't auto-progress in main menu
            Debug.Log("[GameManager] Main Menu Phase");
        }

        #endregion

        #region Selection Phase

        private void EnterSelectionPhase()
        {
            InitializeGameState();

            phaseTimer = 0f; // No timer for selection, wait for player
            autoProgressPhases = false;
            
            if (UnitSelectionManager.Instance != null)
            {
                UnitSelectionManager.Instance.RefreshAvailableUnits();
            }
            
            Debug.Log("[GameManager] Selection Phase - Choose your squad");
        }

        #endregion

        #region Preparation Phase

        private void EnterPreparationPhase()
        {
            // Ensure references are set up
            if (combatManager == null) combatManager = CombatManager.Instance;

            // Restore player health
            if (combatManager != null)
            {
                combatManager.RestoreAllPlayerHealth();
            }

            phaseTimer = preparationDuration;
            autoProgressPhases = true; // Re-enable auto-progress when entering gameplay

            // Add gold for new round
            AddGold(goldPerRound);

            // Initialize shop
            if (shopManager != null)
            {
                shopManager.RefreshShop(playerLevel);
            }

            // Enable unit placement
            if (battlefieldGrid != null)
            {
                battlefieldGrid.EnablePlacement(true);
            }

            Debug.Log($"[GameManager] Preparation Phase - Round {currentRound} - Gold: {playerGold}");
        }

        private void ExitPreparationPhase()
        {
            // Disable unit placement
            if (battlefieldGrid != null)
            {
                battlefieldGrid.EnablePlacement(false);
            }

            Debug.Log("[GameManager] Exiting Preparation Phase");
        }

        #endregion

        #region Combat Phase

        private void EnterCombatPhase()
        {
            phaseTimer = combatDuration;

            // Spawn enemy units (AI opponent)
            int enemyCount = Mathf.Min(playerLevel + 2, 10); // Scale enemy count with player level
            UnitSpawner.Instance?.SpawnEnemyUnits(enemyCount);
            Debug.Log($"[GameManager] Spawned {enemyCount} enemy units for combat");

            // Lock the battlefield
            if (battlefieldGrid != null)
            {
                battlefieldGrid.LockBattlefield();
            }

            // Start combat
            if (combatManager != null)
            {
                combatManager.OnCombatEnded += HandleCombatEnded;
                combatManager.StartCombat();
            }

            Debug.Log("[GameManager] Combat Phase Started");
        }

        private void ExitCombatPhase()
        {
            // Stop combat
            if (combatManager != null)
            {
                combatManager.OnCombatEnded -= HandleCombatEnded;
                combatManager.EndCombat();
            }

            // Clear enemy units after combat
            UnitSpawner.Instance?.ClearEnemyUnits();

            Debug.Log("[GameManager] Combat Phase Ended");
        }

        private void HandleCombatEnded()
        {
            Debug.Log("[GameManager] Combat ended early via event");
            TransitionToPhase(GamePhase.Results);
        }

        #endregion

        #region Results Phase

        private void EnterResultsPhase()
        {
            phaseTimer = 3f; // Short results display

            // Calculate results
            bool victory = combatManager != null && combatManager.PlayerVictory;

            if (!victory)
            {
                // Check for total wipe (no units left)
                bool totalWipe = combatManager != null && combatManager.PlayerUnitCount == 0;
                
                if (totalWipe)
                {
                    Debug.Log("[GameManager] Total Wipe! All units lost.");
                    TakeDamage(playerHealth); // Instant kill on total wipe
                }
                else
                {
                    // Player took damage
                    int damage = CalculateDamageFromLoss();
                    TakeDamage(damage);
                }
            }

            Debug.Log($"[GameManager] Results Phase - Victory: {victory}");
        }

        private int CalculateDamageFromLoss()
        {
            // Damage scales with round number
            return Mathf.Max(1, currentRound / 2);
        }

        #endregion

        #region Game Over

        private void EnterGameOverPhase()
        {
            Debug.Log("[GameManager] Game Over!");
            
            RunManager.Instance?.EndRun();

            // Disable all gameplay
            if (battlefieldGrid != null)
            {
                battlefieldGrid.EnablePlacement(false);
            }

            // Show game over UI
            // TODO: Integrate with UI Toolkit Game Over screen
        }

        #endregion

        #region Timer Management

        private void UpdatePhaseTimer()
        {
            if (!autoProgressPhases) return;

            if (phaseTimer > 0)
            {
                phaseTimer -= Time.deltaTime;

                if (phaseTimer <= 0)
                {
                    OnPhaseTimerExpired();
                }
            }
        }

        private void OnPhaseTimerExpired()
        {
            switch (currentPhase)
            {
                case GamePhase.Preparation:
                    TransitionToPhase(GamePhase.Combat);
                    break;

                case GamePhase.Combat:
                    TransitionToPhase(GamePhase.Results);
                    break;

                case GamePhase.Results:
                    AdvanceRound();
                    break;
            }
        }

        private void AdvanceRound()
        {
            currentRound++;
            OnRoundChanged?.Invoke(currentRound);

            // Check win condition (e.g., survived 30 rounds)
            if (currentRound > 30)
            {
                TransitionToPhase(GamePhase.GameOver);
            }
            else
            {
                TransitionToPhase(GamePhase.Preparation);
            }
        }

        #endregion

        #region Player Resources

        public void AddGold(int amount)
        {
            playerGold += amount;
            OnGoldChanged?.Invoke(playerGold);
            Debug.Log($"[GameManager] Gold: {playerGold} (+{amount})");
        }

        public bool SpendGold(int amount)
        {
            if (playerGold >= amount)
            {
                playerGold -= amount;
                OnGoldChanged?.Invoke(playerGold);
                Debug.Log($"[GameManager] Gold: {playerGold} (-{amount})");
                return true;
            }

            Debug.LogWarning($"[GameManager] Insufficient gold. Have: {playerGold}, Need: {amount}");
            return false;
        }

        public void TakeDamage(int damage)
        {
            playerHealth -= damage;
            OnHealthChanged?.Invoke(playerHealth);

            Debug.Log($"[GameManager] Health: {playerHealth} (-{damage})");

            if (playerHealth <= 0)
            {
                playerHealth = 0; // Clamp to 0
                TransitionToPhase(GamePhase.GameOver);
            }
        }

        public void GainExperience(int amount)
        {
            playerExperience += amount;

            // Check for level up (4 XP per level as standard in auto-battlers)
            int xpForNextLevel = playerLevel * 4;
            if (playerExperience >= xpForNextLevel)
            {
                playerLevel++;
                playerExperience -= xpForNextLevel;
                OnLevelChanged?.Invoke(playerLevel);
                Debug.Log($"[GameManager] Level Up! Now level {playerLevel}");
            }
        }

        #endregion

        #region Public API

        // Properties already defined above with events
        // public GamePhase CurrentPhase => currentPhase;
        // etc.

        /// <summary>
        /// Force immediate phase transition (for manual control)
        /// </summary>
        public void ForcePhaseTransition(GamePhase phase)
        {
            TransitionToPhase(phase);
        }

        /// <summary>
        /// Pause/resume automatic phase progression
        /// </summary>
        public void SetAutoProgress(bool enabled)
        {
            autoProgressPhases = enabled;
        }

        #endregion
    }

    /// <summary>
    /// Game phase enumeration for state machine
    /// </summary>
    public enum GamePhase
    {
        Initialization,
        MainMenu,     // Main menu (new for multi-scene)
        Selection,    // Select units for squad
        Preparation,  // Placement, unit management
        Combat,       // Battle simulation
        Results,      // Show results, calculate rewards
        GameOver
    }
}
