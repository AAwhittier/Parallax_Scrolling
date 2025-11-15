using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SimpleNetworking.Authoritative
{
    /// <summary>
    /// Game event types
    /// </summary>
    public enum GameEventType
    {
        PlayerDamaged,
        EnemyDamaged,
        PlayerDied,
        EnemyDied,
        PlayerJoined,
        PlayerLeft,
        WaveComplete,
        BossSpawned
    }

    /// <summary>
    /// Base game event
    /// </summary>
    [Serializable]
    public class GameEvent
    {
        public GameEventType eventType;
        public long timestamp;
        public int serverTick;

        public GameEvent(GameEventType type, int tick)
        {
            eventType = type;
            serverTick = tick;
            timestamp = DateTime.UtcNow.Ticks;
        }
    }

    /// <summary>
    /// Damage event
    /// </summary>
    [Serializable]
    public class DamageEvent : GameEvent
    {
        public string attackerId;
        public string targetId;
        public int damage;
        public Vector2 knockback;

        public DamageEvent(int tick) : base(GameEventType.PlayerDamaged, tick) { }
    }

    /// <summary>
    /// Authoritative game state - server's source of truth
    /// </summary>
    public class GameState
    {
        // Tick counter
        public int serverTick = 0;

        // Entities
        public Dictionary<string, PlayerEntity> players = new Dictionary<string, PlayerEntity>();
        public Dictionary<string, EnemyEntity> enemies = new Dictionary<string, EnemyEntity>();
        public Dictionary<string, NetworkEntity> projectiles = new Dictionary<string, NetworkEntity>();

        // Events (for this tick)
        public List<GameEvent> events = new List<GameEvent>();
        private List<GameEvent> eventHistory = new List<GameEvent>();
        private const int EVENT_HISTORY_SIZE = 300; // ~5 seconds at 60 ticks/sec

        // Wave system
        public int currentWave = 1;
        public int enemiesKilledThisWave = 0;
        public int enemiesSpawnedThisWave = 0;

        // Entity ID counter
        private int nextEntityId = 1000;

        /// <summary>
        /// Update all entities
        /// </summary>
        public void Update(float deltaTime)
        {
            // Clear events from last tick
            events.Clear();

            // Update all players
            foreach (var player in players.Values)
            {
                if (player.isAlive)
                {
                    player.ServerUpdate(deltaTime);
                }
            }

            // Update all enemies
            var alivePlayers = players.Values.Where(p => p.isAlive).ToList();
            foreach (var enemy in enemies.Values.ToList())
            {
                if (enemy.isAlive)
                {
                    enemy.SetPlayerList(alivePlayers);
                    enemy.ServerUpdate(deltaTime);
                }
                else
                {
                    // Remove dead enemies after delay
                    if (enemy.GetAge() > 2f)
                    {
                        enemies.Remove(enemy.entityId);
                    }
                }
            }

            // Update projectiles
            foreach (var projectile in projectiles.Values.ToList())
            {
                projectile.ServerUpdate(deltaTime);

                // Remove old projectiles
                if (!projectile.isAlive || projectile.GetAge() > 5f)
                {
                    projectiles.Remove(projectile.entityId);
                }
            }

            // Increment tick
            serverTick++;
        }

        /// <summary>
        /// Add a new player
        /// </summary>
        public PlayerEntity AddPlayer(string playerId, string playerName)
        {
            int playerIndex = players.Count;
            string entityId = GenerateEntityId();

            var player = new PlayerEntity(entityId, playerId, playerName, playerIndex);

            // Spawn position (spread out players)
            player.position = new Vector2(playerIndex * 2f, 0f);

            players[playerId] = player;

            AddEvent(new GameEvent(GameEventType.PlayerJoined, serverTick));

            Debug.Log($"[GameState] Player joined: {playerName} (Total: {players.Count})");

            return player;
        }

        /// <summary>
        /// Remove a player
        /// </summary>
        public void RemovePlayer(string playerId)
        {
            if (players.ContainsKey(playerId))
            {
                players.Remove(playerId);
                AddEvent(new GameEvent(GameEventType.PlayerLeft, serverTick));
                Debug.Log($"[GameState] Player left (Remaining: {players.Count})");
            }
        }

        /// <summary>
        /// Get player by ID
        /// </summary>
        public PlayerEntity GetPlayer(string playerId)
        {
            players.TryGetValue(playerId, out PlayerEntity player);
            return player;
        }

        /// <summary>
        /// Spawn an enemy
        /// </summary>
        public EnemyEntity SpawnEnemy(EnemyType type, Vector2 position)
        {
            string entityId = GenerateEntityId();
            var enemy = new EnemyEntity(entityId, type, position);
            enemies[entityId] = enemy;

            enemiesSpawnedThisWave++;

            Debug.Log($"[GameState] Spawned {type} at {position}");

            return enemy;
        }

        /// <summary>
        /// Kill an enemy
        /// </summary>
        public void KillEnemy(string enemyId)
        {
            if (enemies.TryGetValue(enemyId, out EnemyEntity enemy))
            {
                enemy.isAlive = false;
                enemiesKilledThisWave++;

                AddEvent(new GameEvent(GameEventType.EnemyDied, serverTick));

                // Check wave complete
                if (enemiesKilledThisWave >= enemiesSpawnedThisWave)
                {
                    OnWaveComplete();
                }
            }
        }

        /// <summary>
        /// Wave complete logic
        /// </summary>
        private void OnWaveComplete()
        {
            AddEvent(new GameEvent(GameEventType.WaveComplete, serverTick));
            Debug.Log($"[GameState] Wave {currentWave} complete!");

            // Start next wave after delay
            currentWave++;
            enemiesKilledThisWave = 0;
            enemiesSpawnedThisWave = 0;
        }

        /// <summary>
        /// Spawn a wave of enemies
        /// </summary>
        public void SpawnWave()
        {
            int enemyCount = 5 + (currentWave * 2); // Scale with wave number

            for (int i = 0; i < enemyCount; i++)
            {
                // Random enemy type
                EnemyType type = (EnemyType)UnityEngine.Random.Range(0, 3);

                // Random spawn position (ahead of players)
                float spawnX = UnityEngine.Random.Range(10f, 30f);
                Vector2 spawnPos = new Vector2(spawnX, 0f);

                SpawnEnemy(type, spawnPos);
            }

            Debug.Log($"[GameState] Spawned wave {currentWave} with {enemyCount} enemies");
        }

        /// <summary>
        /// Add event to current tick
        /// </summary>
        public void AddEvent(GameEvent gameEvent)
        {
            events.Add(gameEvent);

            // Add to history
            eventHistory.Add(gameEvent);
            if (eventHistory.Count > EVENT_HISTORY_SIZE)
            {
                eventHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// Get events since a specific tick
        /// </summary>
        public List<GameEvent> GetEventsSince(int tick)
        {
            return eventHistory.Where(e => e.serverTick > tick).ToList();
        }

        /// <summary>
        /// Get all living entities within range
        /// </summary>
        public List<NetworkEntity> GetEntitiesInRange(Vector2 center, float radius, EntityType? typeFilter = null)
        {
            List<NetworkEntity> result = new List<NetworkEntity>();

            // Check players
            if (typeFilter == null || typeFilter == EntityType.Player)
            {
                foreach (var player in players.Values)
                {
                    if (player.isAlive && Vector2.Distance(player.position, center) <= radius)
                    {
                        result.Add(player);
                    }
                }
            }

            // Check enemies
            if (typeFilter == null || typeFilter == EntityType.Enemy)
            {
                foreach (var enemy in enemies.Values)
                {
                    if (enemy.isAlive && Vector2.Distance(enemy.position, center) <= radius)
                    {
                        result.Add(enemy);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Generate unique entity ID
        /// </summary>
        private string GenerateEntityId()
        {
            return $"entity_{nextEntityId++}";
        }

        /// <summary>
        /// Get total entity count
        /// </summary>
        public int GetEntityCount()
        {
            return players.Count + enemies.Count + projectiles.Count;
        }

        /// <summary>
        /// Get player count
        /// </summary>
        public int GetPlayerCount()
        {
            return players.Count;
        }

        /// <summary>
        /// Get alive enemy count
        /// </summary>
        public int GetAliveEnemyCount()
        {
            return enemies.Values.Count(e => e.isAlive);
        }
    }
}
