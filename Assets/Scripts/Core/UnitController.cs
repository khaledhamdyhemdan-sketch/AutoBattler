using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using AutoBattler.Data;
using AutoBattler.UI;
using System.Collections.Generic;

namespace AutoBattler.Core
{
    /// <summary>
    /// Unity 6.3 UnitController using TransformHandle and Burst-compatible logic
    /// Replaces deprecated Physics.autoSyncTransforms with Physics.SyncTransforms()
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class UnitController : MonoBehaviour
    {
        [Header("Unit Configuration")]
        [SerializeField] private UnitStatsSO unitStats;
        [SerializeField] private UnitTeam team;

        [Header("Visual References")]
        [SerializeField] private Renderer mainRenderer;
        [SerializeField] private Transform modelTransform;
        [SerializeField] private Animator animator;
        [SerializeField] private HealthBarUI healthBar;

        [Header("Gear")]
        [SerializeField] private List<ItemData> equippedItems = new List<ItemData>();
        private const int MAX_GEAR_SLOTS = 3;

        // Runtime state
        private UnitStatsData stats;
        private int currentHealth;
        private float currentMana;
        private UnitController currentTarget;
        private float3 targetPosition;
        private bool isMoving;
        private float attackCooldown;

        // Transform caching for Burst jobs
        private float3 position;
        private quaternion rotation;
        private Rigidbody rb;

        // Status effects
        private NativeList<ActiveStatusEffect> activeEffects;

        // Ability system
        private float[] abilityCooldowns;

        #region Unity Lifecycle

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            // Auto-assign references if not set
            if (mainRenderer == null)
                mainRenderer = GetComponentInChildren<Renderer>();
            if (modelTransform == null)
                modelTransform = transform;

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            // Auto-create health bar if not assigned
            if (healthBar == null)
            {
                healthBar = GetComponentInChildren<HealthBarUI>();
                if (healthBar == null)
                {
                    // Create a child GameObject for the health bar
                    GameObject healthBarObj = new GameObject("HealthBar");
                    healthBarObj.transform.SetParent(transform);
                    healthBarObj.transform.localPosition = new Vector3(0, 2, 0);
                    healthBar = healthBarObj.AddComponent<HealthBarUI>();
                }
            }

            // Auto-load unit stats if not assigned
            if (unitStats == null)
            {
                // Try to load based on team and tag
                string statsName = team == UnitTeam.Player ? "WarriorStats" : "GoblinStats";
                unitStats = Resources.Load<UnitStatsSO>($"ScriptableObjects/{statsName}");
                
                if (unitStats == null)
                {
                    Debug.LogWarning($"Could not auto-load {statsName}. Please assign manually or ensure it exists in Resources/ScriptableObjects/");
                }
            }

            if (unitStats != null)
            {
                // InitializeFromStats(); // Moved to Start to allow external configuration
            }
        }

        private void Start()
        {
            if (unitStats != null)
            {
                InitializeFromStats();
            }

            activeEffects = new NativeList<ActiveStatusEffect>(Allocator.Persistent);
            
            if (unitStats != null && unitStats.abilities != null)
            {
                abilityCooldowns = new float[unitStats.abilities.Length];
            }
        }

        private void Update()
        {
            UpdateCooldowns(Time.deltaTime);
            UpdateStatusEffects(Time.deltaTime);
            
            if (currentTarget != null)
            {
                UpdateCombatBehavior();
            }
        }

        private void FixedUpdate()
        {
            if (isMoving)
            {
                UpdateMovement(Time.fixedDeltaTime);
            }
        }

        private void OnDestroy()
        {
            if (activeEffects.IsCreated)
            {
                activeEffects.Dispose();
            }
        }

        #endregion

        #region Initialization

        private void InitializeFromStats()
        {
            stats = unitStats.ToStatsData();
            ApplyItemStats();
            
            currentHealth = stats.maxHealth;
            currentMana = 0f;

            if (healthBar != null)
            {
                healthBar.SetMaxHealth(stats.maxHealth);
                healthBar.SetHealth(currentHealth);
            }

            Debug.Log($"[UnitController] {unitStats.unitName} initialized - HP: {currentHealth}/{stats.maxHealth}");
        }

        private void ApplyItemStats()
        {
            // Reset to base stats first
            stats = unitStats.ToStatsData();

            foreach (var item in equippedItems)
            {
                if (item == null) continue;
                stats.maxHealth += item.bonusHealth;
                stats.attackDamage += item.bonusDamage;
                stats.attackSpeed += item.bonusAttackSpeed;
            }
        }

        public bool TryEquipItem(ItemData item)
        {
            if (equippedItems.Count >= MAX_GEAR_SLOTS)
            {
                Debug.Log($"[UnitController] {unitStats.unitName} gear slots full!");
                return false;
            }

            equippedItems.Add(item);
            ApplyItemStats();
            
            // Update health bar if max health changed
            if (healthBar != null)
            {
                healthBar.SetMaxHealth(stats.maxHealth);
            }

            Debug.Log($"[UnitController] {unitStats.unitName} equipped {item.itemName}");
            return true;
        }

        public void RestoreHealth()
        {
            currentHealth = stats.maxHealth;
            currentMana = 0f;
            if (healthBar != null)
            {
                healthBar.SetHealth(currentHealth);
            }
        }

        public List<ItemData> GetEquippedItems()
        {
            return equippedItems;
        }

        #endregion

        #region Movement (Burst-Compatible)

        private void UpdateMovement(float deltaTime)
        {
            position = transform.position;

            // Create result array for job
            NativeArray<MoveResult> resultArray = new NativeArray<MoveResult>(1, Allocator.Temp);

            // Create job for movement calculation
            var moveJob = new MoveToTargetJob
            {
                currentPosition = position,
                targetPosition = targetPosition,
                moveSpeed = stats.moveSpeed,
                deltaTime = deltaTime,
                stoppingDistance = stats.attackRange * 0.9f,
                result = resultArray
            };

            // Execute job
            moveJob.Execute(0);
            var result = resultArray[0];
            resultArray.Dispose();

            // Apply result
            if (result.hasReachedTarget)
            {
                isMoving = false;
                if (animator != null) animator.SetBool("IsMoving", false);
                OnReachedTarget();
            }
            else
            {
                transform.position = result.newPosition;
                
                if (animator != null)
                {
                    animator.SetBool("IsMoving", true);
                    animator.SetFloat("Speed", stats.moveSpeed);
                }

                // Rotate towards movement direction
                if (math.lengthsq(result.moveDirection) > 0.01f)
                {
                    quaternion targetRotation = quaternion.LookRotation(result.moveDirection, math.up());
                    transform.rotation = math.slerp(transform.rotation, targetRotation, stats.rotationSpeed * deltaTime);
                }

                // Unity 6.3: Use Physics.SyncTransforms instead of deprecated autoSyncTransforms
                Physics.SyncTransforms();
            }
        }

        /// <summary>
        /// Burst-compiled job for efficient movement calculations
        /// </summary>
        [BurstCompile]
        private struct MoveToTargetJob : IJobFor
        {
            public float3 currentPosition;
            public float3 targetPosition;
            public float moveSpeed;
            public float deltaTime;
            public float stoppingDistance;
            [NativeDisableParallelForRestriction]
            public NativeArray<MoveResult> result;

            public void Execute(int index)
            {
                MoveResult moveResult = new MoveResult();
                
                float3 direction = targetPosition - currentPosition;
                float distance = math.length(direction);

                if (distance <= stoppingDistance)
                {
                    moveResult.hasReachedTarget = true;
                    moveResult.newPosition = currentPosition;
                    moveResult.moveDirection = float3.zero;
                }
                else
                {
                    moveResult.hasReachedTarget = false;
                    moveResult.moveDirection = math.normalize(direction);
                    moveResult.newPosition = currentPosition + moveResult.moveDirection * moveSpeed * deltaTime;
                }

                result[0] = moveResult;
            }
        }

        private struct MoveResult
        {
            public bool hasReachedTarget;
            public float3 newPosition;
            public float3 moveDirection;
        }

        #endregion

        #region Combat Behavior

        private void UpdateCombatBehavior()
        {
            if (currentTarget == null || currentTarget.IsDead)
            {
                FindNewTarget();
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.transform.position);

            if (distanceToTarget > stats.attackRange)
            {
                // Move towards target
                MoveToPosition(currentTarget.transform.position);
            }
            else
            {
                // In range - attack
                isMoving = false;
                TryAttack();
            }
        }

        private void TryAttack()
        {
            if (attackCooldown <= 0f && currentTarget != null)
            {
                PerformAttack();
                attackCooldown = 1f / stats.attackSpeed; // Convert attacks per second to cooldown
            }
        }

        private void PerformAttack()
        {
            if (currentTarget == null) return;

            // Calculate damage with critical chance
            int damage = stats.attackDamage;
            bool isCritical = UnityEngine.Random.value < stats.criticalChance;
            
            if (isCritical)
            {
            if (animator != null) animator.SetTrigger("Attack");

                damage = Mathf.RoundToInt(damage * stats.criticalMultiplier);
                Debug.Log($"[UnitController] {unitStats.unitName} CRITICAL HIT!");
            }

            // Apply damage
            currentTarget.TakeDamage(damage, this);

            // Rotate to face target
            Vector3 directionToTarget = (currentTarget.transform.position - transform.position).normalized;
            if (directionToTarget != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(directionToTarget);
                Physics.SyncTransforms(); // Unity 6.3 sync
            }

            Debug.Log($"[UnitController] {unitStats.unitName} attacked for {damage} damage");
        }

        #endregion

        #region Damage & Health

        public void TakeDamage(int damage, UnitController attacker)
        {
            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);

            if (healthBar != null)
            {
                healthBar.SetHealth(currentHealth);
            }

            Debug.Log($"[UnitController] {unitStats.unitName} took {damage} damage - HP: {currentHealth}/{stats.maxHealth}");

            if (currentHealth <= 0)
            {
                Die();
            }
            else
            {
                if (animator != null) animator.SetTrigger("Hit");
            }
        }

        public void Heal(int amount)
        {
            currentHealth += amount;
            currentHealth = Mathf.Min(currentHealth, stats.maxHealth);

            if (healthBar != null)
            {
                healthBar.SetHealth(currentHealth);
            }
        }

        private void Die()
        {
            Debug.Log($"[UnitController] {unitStats.unitName} died");

            if (animator != null) animator.SetTrigger("Die");

            // Spawn death effect
            if (unitStats.deathEffect != null)
            {
                Instantiate(unitStats.deathEffect, transform.position, Quaternion.identity);
            }

            // Drop loot if enemy
            if (team == UnitTeam.Enemy && unitStats.possibleDrops != null)
            {
                LootManager.Instance?.HandleLootDrop(unitStats.possibleDrops, transform.position);
            }

            // Notify combat manager
            CombatManager.Instance?.OnUnitDied(this);

            // Destroy game object
            Destroy(gameObject);
        }

        #endregion

        #region Target Management

        public void SetTarget(UnitController target)
        {
            currentTarget = target;
            
            if (target != null)
            {
                MoveToPosition(target.transform.position);
            }
        }

        private void FindNewTarget()
        {
            // Let combat manager assign targets
            currentTarget = CombatManager.Instance?.GetNearestEnemy(this, team);
        }

        private void OnReachedTarget()
        {
            // Target position reached - ready to attack if target still valid
            if (currentTarget != null)
            {
                Debug.Log($"[UnitController] {unitStats.unitName} reached target position");
            }
        }

        #endregion

        #region Abilities

        public void UseAbility(int abilityIndex)
        {
            if (unitStats.abilities == null || abilityIndex >= unitStats.abilities.Length)
            {
                return;
            }

            if (abilityCooldowns[abilityIndex] > 0f)
            {
                Debug.LogWarning($"[UnitController] Ability {abilityIndex} on cooldown");
                return;
            }

            AbilitySO ability = unitStats.abilities[abilityIndex];
            
            if (currentMana < ability.manaCost)
            {
                Debug.LogWarning($"[UnitController] Not enough mana for ability");
                return;
            }

            // Execute ability
            UnitController target = FindAbilityTarget(ability.targetType);
            if (target != null)
            {
                ability.Execute(this, target);
                abilityCooldowns[abilityIndex] = ability.cooldown;
                currentMana -= ability.manaCost;
            }
        }

        private UnitController FindAbilityTarget(AbilityTargetType targetType)
        {
            // Simplified target finding - expand based on target type
            return currentTarget;
        }

        #endregion

        #region Status Effects

        public void ApplyStatusEffect(StatusEffect effect)
        {
            activeEffects.Add(new ActiveStatusEffect
            {
                type = effect.type,
                remainingDuration = effect.duration,
                value = effect.value
            });

            Debug.Log($"[UnitController] {unitStats.unitName} afflicted with {effect.type}");
        }

        private void UpdateStatusEffects(float deltaTime)
        {
            for (int i = activeEffects.Length - 1; i >= 0; i--)
            {
                var effect = activeEffects[i];
                effect.remainingDuration -= deltaTime;

                // Apply effect
                switch (effect.type)
                {
                    case StatusEffectType.DamageOverTime:
                        TakeDamage(Mathf.RoundToInt(effect.value * deltaTime), null);
                        break;
                    case StatusEffectType.HealOverTime:
                        Heal(Mathf.RoundToInt(effect.value * deltaTime));
                        break;
                }

                // Remove expired effects
                if (effect.remainingDuration <= 0)
                {
                    activeEffects.RemoveAtSwapBack(i);
                }
                else
                {
                    activeEffects[i] = effect;
                }
            }
        }

        private struct ActiveStatusEffect
        {
            public StatusEffectType type;
            public float remainingDuration;
            public float value;
        }

        #endregion

        #region Cooldowns

        private void UpdateCooldowns(float deltaTime)
        {
            // Attack cooldown
            if (attackCooldown > 0)
            {
                attackCooldown -= deltaTime;
            }

            // Ability cooldowns
            if (abilityCooldowns != null)
            {
                for (int i = 0; i < abilityCooldowns.Length; i++)
                {
                    if (abilityCooldowns[i] > 0)
                    {
                        abilityCooldowns[i] -= deltaTime;
                    }
                }
            }
        }

        #endregion

        #region Public API

        public void MoveToPosition(Vector3 targetPos)
        {
            targetPosition = targetPos;
            isMoving = true;
        }

        public bool IsDead => currentHealth <= 0;
        public UnitTeam Team => team;
        public UnitStatsSO UnitStats => unitStats;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => stats.maxHealth;

        #endregion

        #region Visual Effects

        /// <summary>
        /// Apply silhouette outline material (for unit selection/highlighting)
        /// </summary>
        public void SetOutlineEnabled(bool enabled)
        {
            if (mainRenderer != null && unitStats.outlineMaterial != null)
            {
                // Unity 6.3: Use material property blocks for efficiency
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                mainRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat("_OutlineEnabled", enabled ? 1f : 0f);
                mainRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        #endregion
    }

    public enum UnitTeam
    {
        Player,
        Enemy
    }
}
