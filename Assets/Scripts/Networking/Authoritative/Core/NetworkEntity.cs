using System;
using UnityEngine;

namespace SimpleNetworking.Authoritative
{
    /// <summary>
    /// Type of network entity
    /// </summary>
    public enum EntityType
    {
        Player,
        Enemy,
        Projectile,
        Item,
        Boss
    }

    /// <summary>
    /// Base class for all networked game entities.
    /// Server-authoritative: server controls all entity state.
    /// </summary>
    [Serializable]
    public abstract class NetworkEntity
    {
        // Identity
        public string entityId;
        public EntityType entityType;
        public string ownerId; // For player entities

        // Transform
        public Vector2 position;
        public Vector2 velocity;
        public int facingDirection = 1; // 1 = right, -1 = left

        // State
        public bool isAlive = true;
        public int health;
        public int maxHealth;

        // Animation
        public string currentAnimation;
        public float animationTime;

        // Timing
        public long spawnTime;

        public NetworkEntity(string id, EntityType type)
        {
            this.entityId = id;
            this.entityType = type;
            this.spawnTime = DateTime.UtcNow.Ticks;
        }

        /// <summary>
        /// Server-side update logic
        /// </summary>
        public abstract void ServerUpdate(float deltaTime);

        /// <summary>
        /// Create a snapshot of current state for network transmission
        /// </summary>
        public abstract EntitySnapshot CreateSnapshot();

        /// <summary>
        /// Apply damage to this entity
        /// </summary>
        public virtual void TakeDamage(int damage, Vector2 knockback)
        {
            health -= damage;
            velocity += knockback;

            if (health <= 0)
            {
                health = 0;
                isAlive = false;
                OnDeath();
            }
        }

        /// <summary>
        /// Called when entity dies
        /// </summary>
        protected virtual void OnDeath()
        {
            currentAnimation = "death";
        }

        /// <summary>
        /// Get collision bounds for this entity
        /// </summary>
        public virtual Rect GetBounds()
        {
            return new Rect(position.x - 0.5f, position.y - 1f, 1f, 2f);
        }

        /// <summary>
        /// Get age in seconds
        /// </summary>
        public float GetAge()
        {
            return (DateTime.UtcNow.Ticks - spawnTime) / (float)TimeSpan.TicksPerSecond;
        }
    }

    /// <summary>
    /// Snapshot data for network transmission
    /// </summary>
    [Serializable]
    public class EntitySnapshot
    {
        public string entityId;
        public EntityType entityType;

        // Transform (quantized for bandwidth)
        public float posX;
        public float posY;
        public float velX;
        public float velY;
        public int facing;

        // State
        public int health;
        public string anim;

        public EntitySnapshot() { }

        public EntitySnapshot(NetworkEntity entity, bool fullPrecision = false)
        {
            entityId = entity.entityId;
            entityType = entity.entityType;

            if (fullPrecision)
            {
                posX = entity.position.x;
                posY = entity.position.y;
                velX = entity.velocity.x;
                velY = entity.velocity.y;
            }
            else
            {
                // Quantize to 0.1 units (reduce bandwidth)
                posX = Mathf.Round(entity.position.x * 10f) / 10f;
                posY = Mathf.Round(entity.position.y * 10f) / 10f;
                velX = Mathf.Round(entity.velocity.x * 10f) / 10f;
                velY = Mathf.Round(entity.velocity.y * 10f) / 10f;
            }

            facing = entity.facingDirection;
            health = entity.health;
            anim = entity.currentAnimation;
        }

        public Vector2 GetPosition() => new Vector2(posX, posY);
        public Vector2 GetVelocity() => new Vector2(velX, velY);
    }
}
