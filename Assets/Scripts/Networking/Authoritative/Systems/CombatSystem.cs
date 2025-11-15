using System.Collections.Generic;
using UnityEngine;

namespace SimpleNetworking.Authoritative
{
    /// <summary>
    /// Server-authoritative combat system.
    /// Handles all damage calculations and collision detection.
    /// </summary>
    public class CombatSystem
    {
        private GameState gameState;

        public CombatSystem(GameState state)
        {
            this.gameState = state;
        }

        /// <summary>
        /// Process all combat interactions
        /// </summary>
        public void ProcessCombat()
        {
            // Players attacking enemies
            foreach (var player in gameState.players.Values)
            {
                if (player.isAlive && player.isAttacking)
                {
                    ProcessPlayerAttack(player);
                }
            }

            // Enemies attacking players
            foreach (var enemy in gameState.enemies.Values)
            {
                if (enemy.isAlive && enemy.isAttacking)
                {
                    ProcessEnemyAttack(enemy);
                }
            }
        }

        /// <summary>
        /// Process player attack against enemies
        /// </summary>
        private void ProcessPlayerAttack(PlayerEntity player)
        {
            Rect attackHitbox = player.GetAttackHitbox();

            foreach (var enemy in gameState.enemies.Values)
            {
                if (!enemy.isAlive) continue;

                // Check collision
                if (attackHitbox.Overlaps(enemy.GetBounds()))
                {
                    // Calculate damage
                    int damage = player.GetAttackDamage();

                    // Calculate knockback
                    Vector2 knockback = CalculateKnockback(
                        player.position,
                        enemy.position,
                        player.facingDirection,
                        baseForce: 5f + (player.comboCount * 2f)
                    );

                    // Apply damage
                    enemy.TakeDamage(damage, knockback);

                    // Create event
                    var damageEvent = new DamageEvent(gameState.serverTick)
                    {
                        attackerId = player.entityId,
                        targetId = enemy.entityId,
                        damage = damage,
                        knockback = knockback
                    };
                    damageEvent.eventType = GameEventType.EnemyDamaged;
                    gameState.AddEvent(damageEvent);

                    // Check death
                    if (!enemy.isAlive)
                    {
                        gameState.KillEnemy(enemy.entityId);
                    }

                    Debug.Log($"[Combat] {player.playerName} hit {enemy.enemyType} for {damage} damage");
                }
            }
        }

        /// <summary>
        /// Process enemy attack against players
        /// </summary>
        private void ProcessEnemyAttack(EnemyEntity enemy)
        {
            Rect attackHitbox = enemy.GetAttackHitbox();

            foreach (var player in gameState.players.Values)
            {
                if (!player.isAlive) continue;

                // Check collision
                if (attackHitbox.Overlaps(player.GetBounds()))
                {
                    // Calculate damage
                    int damage = enemy.attackDamage;

                    // Calculate knockback
                    Vector2 knockback = CalculateKnockback(
                        enemy.position,
                        player.position,
                        enemy.facingDirection,
                        baseForce: 3f
                    );

                    // Apply damage
                    player.TakeDamage(damage, knockback);

                    // Create event
                    var damageEvent = new DamageEvent(gameState.serverTick)
                    {
                        attackerId = enemy.entityId,
                        targetId = player.entityId,
                        damage = damage,
                        knockback = knockback
                    };
                    damageEvent.eventType = GameEventType.PlayerDamaged;
                    gameState.AddEvent(damageEvent);

                    // Check death
                    if (!player.isAlive)
                    {
                        var deathEvent = new GameEvent(GameEventType.PlayerDied, gameState.serverTick);
                        gameState.AddEvent(deathEvent);
                    }

                    Debug.Log($"[Combat] {enemy.enemyType} hit {player.playerName} for {damage} damage");
                }
            }
        }

        /// <summary>
        /// Calculate knockback vector
        /// </summary>
        private Vector2 CalculateKnockback(Vector2 attackerPos, Vector2 targetPos, int facingDir, float baseForce)
        {
            // Direction from attacker to target
            Vector2 direction = (targetPos - attackerPos).normalized;

            // If no clear direction, use facing direction
            if (direction.magnitude < 0.1f)
            {
                direction = new Vector2(facingDir, 0);
            }

            // Horizontal knockback
            Vector2 knockback = new Vector2(direction.x * baseForce, 0);

            // Add slight upward force
            knockback.y = baseForce * 0.5f;

            return knockback;
        }

        /// <summary>
        /// Check area of effect damage
        /// </summary>
        public void ApplyAreaDamage(Vector2 center, float radius, int damage, string attackerId, EntityType targetType)
        {
            var targets = gameState.GetEntitiesInRange(center, radius, targetType);

            foreach (var target in targets)
            {
                // Don't hit self
                if (target.entityId == attackerId) continue;

                // Calculate knockback from center
                Vector2 direction = (target.position - center).normalized;
                Vector2 knockback = direction * 8f;

                // Apply damage
                target.TakeDamage(damage, knockback);

                // Create event
                var damageEvent = new DamageEvent(gameState.serverTick)
                {
                    attackerId = attackerId,
                    targetId = target.entityId,
                    damage = damage,
                    knockback = knockback
                };

                if (target.entityType == EntityType.Player)
                    damageEvent.eventType = GameEventType.PlayerDamaged;
                else
                    damageEvent.eventType = GameEventType.EnemyDamaged;

                gameState.AddEvent(damageEvent);
            }
        }
    }
}
