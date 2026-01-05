using System.Collections.Generic;
using UnityEngine;

namespace AutoBattler.Core.VoidGame
{
    /// <summary>
    /// VoidGame Manager - Main integration point for the ported game systems
    /// Bridges between the VoidGame AI/Combat systems and existing Unity components
    /// </summary>
    public class VoidGameManager : MonoBehaviour
    {
        #region Singleton

        private static VoidGameManager instance;
        public static VoidGameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<VoidGameManager>();
                    if (instance == null)
                    {
                        var go = new GameObject("VoidGameManager");
                        instance = go.AddComponent<VoidGameManager>();
                    }
                }
                return instance;
            }
        }

        #endregion

        #region Configuration

        [Header("Combat Settings")]
        [SerializeField] private float combatTickRate = 0.1f; // 10 times per second
        [SerializeField] private bool useAdvancedAI = true;
        [SerializeField] private bool debugLogging = false;

        [Header("Arena Settings")]
        [SerializeField] private Vector2 arenaBounds = new Vector2(20f, 20f);
        [SerializeField] private GameMode gameMode = GameMode.Standard;

        #endregion

        #region State

        private List<GameUnit> allUnits = new List<GameUnit>();
        private List<GameUnit> playerUnits = new List<GameUnit>();
        private List<GameUnit> enemyUnits = new List<GameUnit>();
        private List<AI.AIController> aiControllers = new List<AI.AIController>();
        
        private float lastCombatTick;
        private int currentRound;
        private bool combatActive;

        #endregion

        #region Events

        public System.Action<GameUnit, GameUnit, DamageResult> OnDamageDealt;
        public System.Action<GameUnit, GameUnit, int> OnHealApplied;
        public System.Action<GameUnit, StatusEffect> OnStatusEffectApplied;
        public System.Action<GameUnit> OnUnitDied;
        public System.Action<bool> OnCombatEnded; // true = player won

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        private void Update()
        {
            if (!combatActive)
                return;

            if (Time.time - lastCombatTick >= combatTickRate)
            {
                ProcessCombatTick();
                lastCombatTick = Time.time;
            }
        }

        #endregion

        #region Unit Registration

        /// <summary>
        /// Register a unit with the VoidGame system
        /// </summary>
        public GameUnit RegisterUnit(UnitController unitController, bool isPlayerUnit)
        {
            var gameUnit = GameUnit.FromUnitController(unitController, !isPlayerUnit);
            
            allUnits.Add(gameUnit);
            if (isPlayerUnit)
                playerUnits.Add(gameUnit);
            else
                enemyUnits.Add(gameUnit);

            // Create AI controller if using advanced AI
            if (useAdvancedAI)
            {
                var aiController = new AI.AIController(gameUnit);
                aiControllers.Add(aiController);
            }

            if (debugLogging)
                Debug.Log($"[VoidGame] Registered unit: {gameUnit.name} ({gameUnit.type}) - Player: {isPlayerUnit}");

            return gameUnit;
        }

        /// <summary>
        /// Unregister a unit
        /// </summary>
        public void UnregisterUnit(GameUnit unit)
        {
            allUnits.Remove(unit);
            playerUnits.Remove(unit);
            enemyUnits.Remove(unit);
            
            aiControllers.RemoveAll(c => c.Unit == unit);
        }

        /// <summary>
        /// Clear all units
        /// </summary>
        public void ClearAllUnits()
        {
            allUnits.Clear();
            playerUnits.Clear();
            enemyUnits.Clear();
            aiControllers.Clear();
        }

        #endregion

        #region Combat Control

        /// <summary>
        /// Start combat
        /// </summary>
        public void StartCombat(int round = 1)
        {
            currentRound = round;
            combatActive = true;
            lastCombatTick = Time.time;

            // Reset round stats
            foreach (var unit in allUnits)
            {
                unit.ResetForRound();
            }

            if (debugLogging)
                Debug.Log($"[VoidGame] Combat started - Round {round}");
        }

        /// <summary>
        /// End combat
        /// </summary>
        public void EndCombat()
        {
            combatActive = false;
            
            bool playerWon = enemyUnits.TrueForAll(u => u.IsDead);
            
            if (debugLogging)
                Debug.Log($"[VoidGame] Combat ended - Player won: {playerWon}");

            OnCombatEnded?.Invoke(playerWon);
        }

        /// <summary>
        /// Process a combat tick
        /// </summary>
        private void ProcessCombatTick()
        {
            float currentTime = Time.time;
            float deltaTime = combatTickRate;

            // Process status effect ticks
            foreach (var unit in allUnits)
            {
                if (unit.IsDead)
                    continue;

                // Process DoT/HoT effects
                CombatEngine.ProcessStatusEffectTicks(unit, currentTime);

                // Cleanup expired effects
                unit.CleanupExpiredEffects(currentTime);

                // Sync with Unity transform
                unit.SyncFromUnityTransform();
            }

            // Update AI controllers
            if (useAdvancedAI)
            {
                foreach (var aiController in aiControllers)
                {
                    if (aiController.Unit.IsDead)
                        continue;

                    bool isPlayerUnit = playerUnits.Contains(aiController.Unit);
                    var allies = isPlayerUnit ? playerUnits : enemyUnits;
                    var enemies = isPlayerUnit ? enemyUnits : playerUnits;

                    aiController.Update(currentTime, deltaTime, allies, enemies);
                }
            }

            // Check for combat end conditions
            if (playerUnits.TrueForAll(u => u.IsDead) || enemyUnits.TrueForAll(u => u.IsDead))
            {
                EndCombat();
            }
        }

        #endregion

        #region Combat Actions

        /// <summary>
        /// Execute an attack from source to target
        /// </summary>
        public DamageResult ExecuteAttack(GameUnit source, GameUnit target)
        {
            var result = CombatEngine.ExecuteAttack(source, target, Time.time);
            
            if (result.damageDealt > 0)
            {
                OnDamageDealt?.Invoke(source, target, result);
                
                if (target.IsDead)
                {
                    OnUnitDied?.Invoke(target);
                }
            }

            return result;
        }

        /// <summary>
        /// Apply healing from source to target
        /// </summary>
        public int ApplyHeal(GameUnit source, GameUnit target, int amount)
        {
            int healed = CombatEngine.ApplyHeal(source, target, amount);
            
            if (healed > 0)
            {
                OnHealApplied?.Invoke(source, target, healed);
            }

            return healed;
        }

        /// <summary>
        /// Apply a status effect to a target
        /// </summary>
        public void ApplyStatusEffect(GameUnit target, StatusEffectType effectType, float value, float duration)
        {
            CombatEngine.ApplyStatusEffect(target, effectType, value, duration, Time.time);
            
            int effectIndex = target.statusEffects.FindIndex(e => e.type == effectType);
            if (effectIndex >= 0)
            {
                OnStatusEffectApplied?.Invoke(target, target.statusEffects[effectIndex]);
            }
        }

        #endregion

        #region Queries

        /// <summary>
        /// Get all alive player units
        /// </summary>
        public List<GameUnit> GetAlivePlayerUnits()
        {
            return playerUnits.FindAll(u => !u.IsDead);
        }

        /// <summary>
        /// Get all alive enemy units
        /// </summary>
        public List<GameUnit> GetAliveEnemyUnits()
        {
            return enemyUnits.FindAll(u => !u.IsDead);
        }

        /// <summary>
        /// Get unit by ID
        /// </summary>
        public GameUnit GetUnitById(string id)
        {
            return allUnits.Find(u => u.id == id);
        }

        /// <summary>
        /// Get closest enemy for a unit
        /// </summary>
        public GameUnit GetClosestEnemy(GameUnit unit)
        {
            bool isPlayerUnit = playerUnits.Contains(unit);
            var enemies = isPlayerUnit ? enemyUnits : playerUnits;
            return AI.SensorySystem.GetClosestEnemy(unit, enemies);
        }

        /// <summary>
        /// Get the AI strategy for a unit
        /// </summary>
        public AI.AIStrategy? GetUnitStrategy(GameUnit unit)
        {
            var controller = aiControllers.Find(c => c.Unit == unit);
            return controller?.GetCurrentStrategy();
        }

        #endregion

        #region Debug

        [ContextMenu("Log Unit Status")]
        private void LogUnitStatus()
        {
            Debug.Log("=== VoidGame Unit Status ===");
            Debug.Log($"Player Units: {playerUnits.Count}");
            foreach (var unit in playerUnits)
            {
                Debug.Log($"  {unit.name} - HP: {unit.stats.hp}/{unit.stats.maxHp} - Dead: {unit.IsDead}");
            }
            Debug.Log($"Enemy Units: {enemyUnits.Count}");
            foreach (var unit in enemyUnits)
            {
                Debug.Log($"  {unit.name} - HP: {unit.stats.hp}/{unit.stats.maxHp} - Dead: {unit.IsDead}");
            }
        }

        #endregion
    }
}
