using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SimpleNetworking.Authoritative
{
    /// <summary>
    /// Enemy type definitions
    /// </summary>
    public enum EnemyType
    {
        Grunt,      // Basic melee enemy
        Archer,     // Ranged enemy
        Brute,      // Heavy tank enemy
        Boss        // Boss enemy
    }

    /// <summary>
    /// AI state machine states
    /// </summary>
    public enum AIState
    {
        Idle,
        Patrol,
        Approach,
        Attack,
        Retreat,
        Stunned
    }

    /// <summary>
    /// Server-controlled enemy entity with AI
    /// </summary>
    public class EnemyEntity : NetworkEntity
    {
        // Enemy configuration
        public EnemyType enemyType;
        public AIState aiState;

        // Stats (vary by type)
        public float moveSpeed;
        public int attackDamage;
        public float detectionRange;
        public float attackRange;
        public float attackCooldown;

        // AI state
        public PlayerEntity targetPlayer;
        private float stateTimer;
        private float attackTimer;
        private Vector2 patrolTarget;

        // Combat
        public bool isAttacking;
        private const float ATTACK_DURATION = 0.4f;

        public EnemyEntity(string id, EnemyType type, Vector2 spawnPos)
            : base(id, EntityType.Enemy)
        {
            this.enemyType = type;
            this.position = spawnPos;
            this.aiState = AIState.Idle;

            ConfigureByType(type);
        }

        private void ConfigureByType(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Grunt:
                    maxHealth = 30;
                    moveSpeed = 4f;
                    attackDamage = 10;
                    detectionRange = 10f;
                    attackRange = 1.5f;
                    attackCooldown = 2f;
                    currentAnimation = "grunt_idle";
                    break;

                case EnemyType.Archer:
                    maxHealth = 20;
                    moveSpeed = 3f;
                    attackDamage = 8;
                    detectionRange = 15f;
                    attackRange = 8f;
                    attackCooldown = 3f;
                    currentAnimation = "archer_idle";
                    break;

                case EnemyType.Brute:
                    maxHealth = 80;
                    moveSpeed = 2f;
                    attackDamage = 25;
                    detectionRange = 8f;
                    attackRange = 2f;
                    attackCooldown = 3f;
                    currentAnimation = "brute_idle";
                    break;

                case EnemyType.Boss:
                    maxHealth = 500;
                    moveSpeed = 3f;
                    attackDamage = 40;
                    detectionRange = 20f;
                    attackRange = 3f;
                    attackCooldown = 1.5f;
                    currentAnimation = "boss_idle";
                    break;
            }

            health = maxHealth;
        }

        public override void ServerUpdate(float deltaTime)
        {
            if (!isAlive) return;

            // Update timers
            stateTimer -= deltaTime;
            attackTimer -= deltaTime;

            // Run AI
            UpdateAI(deltaTime);

            // Apply velocity
            position += velocity * deltaTime;

            // Apply friction
            velocity *= 0.85f;

            // Update animation
            UpdateAnimation();
        }

        private void UpdateAI(float deltaTime)
        {
            switch (aiState)
            {
                case AIState.Idle:
                    IdleState(deltaTime);
                    break;

                case AIState.Patrol:
                    PatrolState(deltaTime);
                    break;

                case AIState.Approach:
                    ApproachState(deltaTime);
                    break;

                case AIState.Attack:
                    AttackState(deltaTime);
                    break;

                case AIState.Retreat:
                    RetreatState(deltaTime);
                    break;

                case AIState.Stunned:
                    StunnedState(deltaTime);
                    break;
            }
        }

        private void IdleState(float deltaTime)
        {
            // Look for nearby players
            if (targetPlayer == null || !targetPlayer.isAlive)
            {
                targetPlayer = FindNearestPlayer();
            }

            if (targetPlayer != null)
            {
                float distance = Vector2.Distance(position, targetPlayer.position);
                if (distance <= detectionRange)
                {
                    TransitionToState(AIState.Approach);
                }
            }

            // Randomly start patrolling
            if (stateTimer <= 0)
            {
                if (UnityEngine.Random.value > 0.5f)
                {
                    TransitionToState(AIState.Patrol);
                }
                stateTimer = 2f;
            }
        }

        private void PatrolState(float deltaTime)
        {
            // Check for players
            if (targetPlayer != null && targetPlayer.isAlive)
            {
                float distance = Vector2.Distance(position, targetPlayer.position);
                if (distance <= detectionRange)
                {
                    TransitionToState(AIState.Approach);
                    return;
                }
            }

            // Move towards patrol target
            if (Vector2.Distance(position, patrolTarget) < 1f || stateTimer <= 0)
            {
                // Pick new patrol point
                patrolTarget = position + new Vector2(
                    UnityEngine.Random.Range(-10f, 10f),
                    0f
                );
                stateTimer = 5f;
            }

            Vector2 direction = (patrolTarget - position).normalized;
            velocity = direction * moveSpeed * 0.5f; // Half speed when patrolling
            facingDirection = direction.x > 0 ? 1 : -1;
        }

        private void ApproachState(float deltaTime)
        {
            if (targetPlayer == null || !targetPlayer.isAlive)
            {
                targetPlayer = FindNearestPlayer();
                if (targetPlayer == null)
                {
                    TransitionToState(AIState.Idle);
                    return;
                }
            }

            float distance = Vector2.Distance(position, targetPlayer.position);

            // Too far, lose interest
            if (distance > detectionRange * 1.5f)
            {
                TransitionToState(AIState.Idle);
                return;
            }

            // In attack range
            if (distance <= attackRange)
            {
                TransitionToState(AIState.Attack);
                return;
            }

            // Move towards target
            Vector2 direction = (targetPlayer.position - position).normalized;
            velocity = direction * moveSpeed;
            facingDirection = direction.x > 0 ? 1 : -1;
        }

        private void AttackState(float deltaTime)
        {
            if (targetPlayer == null || !targetPlayer.isAlive)
            {
                TransitionToState(AIState.Idle);
                return;
            }

            float distance = Vector2.Distance(position, targetPlayer.position);

            // Target moved away
            if (distance > attackRange * 1.5f)
            {
                TransitionToState(AIState.Approach);
                return;
            }

            // Face target
            facingDirection = (targetPlayer.position.x > position.x) ? 1 : -1;

            // Execute attack
            if (attackTimer <= 0 && !isAttacking)
            {
                isAttacking = true;
                attackTimer = attackCooldown;
                stateTimer = ATTACK_DURATION;
                currentAnimation = GetAttackAnimation();
            }

            // Finish attack
            if (isAttacking && stateTimer <= 0)
            {
                isAttacking = false;
            }
        }

        private void RetreatState(float deltaTime)
        {
            if (targetPlayer == null || !targetPlayer.isAlive)
            {
                TransitionToState(AIState.Idle);
                return;
            }

            // Move away from target
            Vector2 direction = (position - targetPlayer.position).normalized;
            velocity = direction * moveSpeed;
            facingDirection = -direction.x > 0 ? 1 : -1;

            // Retreat for a bit, then approach again
            if (stateTimer <= 0)
            {
                TransitionToState(AIState.Approach);
            }
        }

        private void StunnedState(float deltaTime)
        {
            velocity *= 0.9f; // Slow down

            if (stateTimer <= 0)
            {
                TransitionToState(AIState.Idle);
            }
        }

        private void TransitionToState(AIState newState)
        {
            aiState = newState;

            switch (newState)
            {
                case AIState.Patrol:
                    stateTimer = 5f;
                    patrolTarget = position + new Vector2(UnityEngine.Random.Range(-10f, 10f), 0f);
                    break;

                case AIState.Retreat:
                    stateTimer = 2f;
                    break;

                case AIState.Stunned:
                    stateTimer = 1f;
                    break;
            }
        }

        private void UpdateAnimation()
        {
            string prefix = enemyType.ToString().ToLower();

            if (!isAlive)
            {
                currentAnimation = $"{prefix}_death";
            }
            else if (isAttacking)
            {
                currentAnimation = $"{prefix}_attack";
            }
            else if (aiState == AIState.Stunned)
            {
                currentAnimation = $"{prefix}_stunned";
            }
            else if (velocity.magnitude > 0.1f)
            {
                currentAnimation = $"{prefix}_walk";
            }
            else
            {
                currentAnimation = $"{prefix}_idle";
            }
        }

        public override void TakeDamage(int damage, Vector2 knockback)
        {
            base.TakeDamage(damage, knockback);

            // Stun on heavy hit
            if (knockback.magnitude > 5f)
            {
                TransitionToState(AIState.Stunned);
            }
        }

        /// <summary>
        /// Get attack hitbox
        /// </summary>
        public Rect GetAttackHitbox()
        {
            if (!isAttacking) return new Rect(0, 0, 0, 0);

            float hitboxWidth = attackRange;
            float hitboxHeight = 2f;
            float hitboxOffsetX = facingDirection * (attackRange / 2f);

            return new Rect(
                position.x + hitboxOffsetX - hitboxWidth / 2f,
                position.y - hitboxHeight / 2f,
                hitboxWidth,
                hitboxHeight
            );
        }

        /// <summary>
        /// Find nearest living player
        /// </summary>
        private PlayerEntity FindNearestPlayer()
        {
            // This will be called by the server with access to all players
            // Implementation provided by GameState
            return null; // Placeholder
        }

        public void SetPlayerList(List<PlayerEntity> players)
        {
            // Helper method for AI to access players
            if (players == null || players.Count == 0) return;

            targetPlayer = players
                .Where(p => p.isAlive)
                .OrderBy(p => Vector2.Distance(position, p.position))
                .FirstOrDefault();
        }

        private string GetAttackAnimation()
        {
            return $"{enemyType.ToString().ToLower()}_attack";
        }

        public override EntitySnapshot CreateSnapshot()
        {
            var snapshot = new EnemySnapshot
            {
                entityId = entityId,
                entityType = entityType,
                posX = Mathf.Round(position.x * 10f) / 10f,
                posY = Mathf.Round(position.y * 10f) / 10f,
                velX = Mathf.Round(velocity.x * 10f) / 10f,
                velY = Mathf.Round(velocity.y * 10f) / 10f,
                facing = facingDirection,
                health = health,
                anim = currentAnimation,

                // Enemy-specific
                enemyType = enemyType,
                aiState = aiState,
                isAttacking = isAttacking
            };

            return snapshot;
        }
    }

    /// <summary>
    /// Extended snapshot for enemies
    /// </summary>
    [Serializable]
    public class EnemySnapshot : EntitySnapshot
    {
        public EnemyType enemyType;
        public AIState aiState;
        public bool isAttacking;
    }
}
