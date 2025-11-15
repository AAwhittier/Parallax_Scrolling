using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleNetworking.Authoritative
{
    /// <summary>
    /// Player character entity with combat abilities
    /// </summary>
    public class PlayerEntity : NetworkEntity
    {
        // Player info
        public string playerName;
        public int playerIndex; // 0-7 for 8 players

        // Stats
        public float moveSpeed = 8f;
        public float jumpForce = 10f;
        public int attackDamage = 15;

        // State
        public bool isGrounded;
        public bool isAttacking;
        public float attackCooldown;
        public int comboCount;
        public float comboTimer;

        // Input buffer
        private Queue<PlayerInput> inputBuffer = new Queue<PlayerInput>();
        private int lastProcessedInput = 0;

        // Constants
        private const float ATTACK_DURATION = 0.3f;
        private const float ATTACK_COOLDOWN = 0.5f;
        private const float COMBO_WINDOW = 1.5f;
        private const float GRAVITY = 20f;
        private const float GROUND_Y = 0f;

        public PlayerEntity(string id, string playerId, string name, int index)
            : base(id, EntityType.Player)
        {
            this.ownerId = playerId;
            this.playerName = name;
            this.playerIndex = index;
            this.maxHealth = 100;
            this.health = maxHealth;
            this.currentAnimation = "idle";
        }

        /// <summary>
        /// Add input to buffer for processing
        /// </summary>
        public void QueueInput(PlayerInput input)
        {
            inputBuffer.Enqueue(input);
        }

        public override void ServerUpdate(float deltaTime)
        {
            // Process all queued inputs
            while (inputBuffer.Count > 0)
            {
                var input = inputBuffer.Dequeue();
                ProcessInput(input, deltaTime);
                lastProcessedInput = input.sequenceId;
            }

            // Update timers
            if (attackCooldown > 0)
                attackCooldown -= deltaTime;

            if (comboTimer > 0)
            {
                comboTimer -= deltaTime;
                if (comboTimer <= 0)
                    comboCount = 0;
            }

            // Apply gravity
            if (!isGrounded)
            {
                velocity.y -= GRAVITY * deltaTime;
            }

            // Apply velocity
            position += velocity * deltaTime;

            // Ground collision
            if (position.y <= GROUND_Y)
            {
                position.y = GROUND_Y;
                velocity.y = 0;
                isGrounded = true;
            }
            else
            {
                isGrounded = false;
            }

            // Apply friction
            velocity.x *= 0.9f;

            // Update animation
            UpdateAnimation();
        }

        private void ProcessInput(PlayerInput input, float deltaTime)
        {
            if (!isAlive) return;

            // Movement
            if (!isAttacking)
            {
                Vector2 moveInput = input.GetMoveVector();
                velocity.x = moveInput.x * moveSpeed;

                // Update facing direction
                if (moveInput.x != 0)
                {
                    facingDirection = moveInput.x > 0 ? 1 : -1;
                }
            }

            // Jump
            if (input.jump && isGrounded && !isAttacking)
            {
                velocity.y = jumpForce;
                isGrounded = false;
            }

            // Attack
            if (input.attack && attackCooldown <= 0)
            {
                ExecuteAttack();
            }
        }

        private void ExecuteAttack()
        {
            isAttacking = true;
            attackCooldown = ATTACK_COOLDOWN;
            velocity.x = 0; // Stop movement during attack

            // Combo system
            comboCount++;
            comboTimer = COMBO_WINDOW;

            if (comboCount > 3)
                comboCount = 1;

            currentAnimation = $"attack_{comboCount}";
        }

        private void UpdateAnimation()
        {
            if (isAttacking)
            {
                if (attackCooldown <= ATTACK_COOLDOWN - ATTACK_DURATION)
                {
                    isAttacking = false;
                }
                return;
            }

            if (!isGrounded)
            {
                currentAnimation = "jump";
            }
            else if (Mathf.Abs(velocity.x) > 0.1f)
            {
                currentAnimation = "run";
            }
            else
            {
                currentAnimation = "idle";
            }
        }

        public override EntitySnapshot CreateSnapshot()
        {
            var snapshot = new EntitySnapshot(this, fullPrecision: true);

            // Add player-specific data
            var playerSnapshot = new PlayerSnapshot
            {
                entityId = entityId,
                entityType = entityType,
                posX = snapshot.posX,
                posY = snapshot.posY,
                velX = snapshot.velX,
                velY = snapshot.velY,
                facing = snapshot.facing,
                health = snapshot.health,
                anim = snapshot.anim,

                // Player-specific
                playerName = playerName,
                playerIndex = playerIndex,
                isAttacking = isAttacking,
                comboCount = comboCount,
                lastProcessedInput = lastProcessedInput
            };

            return playerSnapshot;
        }

        /// <summary>
        /// Get attack hitbox for combat system
        /// </summary>
        public Rect GetAttackHitbox()
        {
            if (!isAttacking) return new Rect(0, 0, 0, 0);

            float hitboxWidth = 1.5f;
            float hitboxHeight = 2f;
            float hitboxOffsetX = facingDirection * 1f;

            return new Rect(
                position.x + hitboxOffsetX - hitboxWidth / 2f,
                position.y - hitboxHeight / 2f,
                hitboxWidth,
                hitboxHeight
            );
        }

        /// <summary>
        /// Get current attack damage (varies by combo)
        /// </summary>
        public int GetAttackDamage()
        {
            return attackDamage * comboCount; // More damage in combo
        }
    }

    /// <summary>
    /// Extended snapshot for players with additional data
    /// </summary>
    [Serializable]
    public class PlayerSnapshot : EntitySnapshot
    {
        public string playerName;
        public int playerIndex;
        public bool isAttacking;
        public int comboCount;
        public int lastProcessedInput;
    }
}
