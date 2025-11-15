using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SimpleNetworking.Authoritative
{
    /// <summary>
    /// Complete game state snapshot for network transmission
    /// </summary>
    [Serializable]
    public class GameSnapshot
    {
        public int serverTick;
        public long timestamp;

        // Entity snapshots
        public List<PlayerSnapshot> players;
        public List<EnemySnapshot> enemies;
        public List<EntitySnapshot> projectiles;

        // Events since last snapshot
        public List<GameEvent> events;

        // Game state
        public int currentWave;
        public int aliveEnemyCount;

        public GameSnapshot()
        {
            timestamp = DateTime.UtcNow.Ticks;
            players = new List<PlayerSnapshot>();
            enemies = new List<EnemySnapshot>();
            projectiles = new List<EntitySnapshot>();
            events = new List<GameEvent>();
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static GameSnapshot FromJson(string json)
        {
            return JsonUtility.FromJson<GameSnapshot>(json);
        }
    }

    /// <summary>
    /// Creates optimized snapshots for network transmission
    /// </summary>
    public class SnapshotSystem
    {
        private GameState gameState;
        private int lastSnapshotTick = 0;

        // Per-client optimization
        private const float INTEREST_RADIUS = 40f;  // Only send nearby entities
        private const int MAX_ENEMIES_PER_SNAPSHOT = 30;

        public SnapshotSystem(GameState state)
        {
            this.gameState = state;
        }

        /// <summary>
        /// Create snapshot optimized for a specific client
        /// </summary>
        public GameSnapshot CreateSnapshot(string forPlayerId)
        {
            var snapshot = new GameSnapshot
            {
                serverTick = gameState.serverTick,
                currentWave = gameState.currentWave,
                aliveEnemyCount = gameState.GetAliveEnemyCount()
            };

            // Get the player this snapshot is for
            PlayerEntity ownPlayer = gameState.GetPlayer(forPlayerId);

            if (ownPlayer != null)
            {
                // Own player - full precision
                snapshot.players.Add((PlayerSnapshot)ownPlayer.CreateSnapshot());

                // Other players - reduced precision
                foreach (var player in gameState.players.Values)
                {
                    if (player.ownerId != forPlayerId && player.isAlive)
                    {
                        snapshot.players.Add((PlayerSnapshot)player.CreateSnapshot());
                    }
                }

                // Enemies - prioritize by distance
                var sortedEnemies = gameState.enemies.Values
                    .Where(e => e.isAlive)
                    .OrderBy(e => Vector2.Distance(e.position, ownPlayer.position))
                    .Take(MAX_ENEMIES_PER_SNAPSHOT)
                    .ToList();

                foreach (var enemy in sortedEnemies)
                {
                    // Only send if within interest radius
                    if (Vector2.Distance(enemy.position, ownPlayer.position) < INTEREST_RADIUS)
                    {
                        snapshot.enemies.Add((EnemySnapshot)enemy.CreateSnapshot());
                    }
                }
            }
            else
            {
                // Player not in game yet - send all players
                foreach (var player in gameState.players.Values)
                {
                    if (player.isAlive)
                    {
                        snapshot.players.Add((PlayerSnapshot)player.CreateSnapshot());
                    }
                }

                // Send some enemies
                var enemies = gameState.enemies.Values
                    .Where(e => e.isAlive)
                    .Take(MAX_ENEMIES_PER_SNAPSHOT)
                    .ToList();

                foreach (var enemy in enemies)
                {
                    snapshot.enemies.Add((EnemySnapshot)enemy.CreateSnapshot());
                }
            }

            // Events since last snapshot
            snapshot.events = gameState.GetEventsSince(lastSnapshotTick);

            lastSnapshotTick = gameState.serverTick;

            return snapshot;
        }

        /// <summary>
        /// Create broadcast snapshot for all clients (less optimized)
        /// </summary>
        public GameSnapshot CreateBroadcastSnapshot()
        {
            var snapshot = new GameSnapshot
            {
                serverTick = gameState.serverTick,
                currentWave = gameState.currentWave,
                aliveEnemyCount = gameState.GetAliveEnemyCount()
            };

            // All players
            foreach (var player in gameState.players.Values)
            {
                if (player.isAlive)
                {
                    snapshot.players.Add((PlayerSnapshot)player.CreateSnapshot());
                }
            }

            // Limited enemies
            var enemies = gameState.enemies.Values
                .Where(e => e.isAlive)
                .Take(MAX_ENEMIES_PER_SNAPSHOT)
                .ToList();

            foreach (var enemy in enemies)
            {
                snapshot.enemies.Add((EnemySnapshot)enemy.CreateSnapshot());
            }

            // Recent events
            snapshot.events = gameState.GetEventsSince(lastSnapshotTick);
            lastSnapshotTick = gameState.serverTick;

            return snapshot;
        }

        /// <summary>
        /// Estimate snapshot size in bytes
        /// </summary>
        public int EstimateSnapshotSize(GameSnapshot snapshot)
        {
            // Rough estimation
            int size = 100; // Base overhead
            size += snapshot.players.Count * 80;      // ~80 bytes per player
            size += snapshot.enemies.Count * 30;      // ~30 bytes per enemy
            size += snapshot.projectiles.Count * 25;  // ~25 bytes per projectile
            size += snapshot.events.Count * 40;       // ~40 bytes per event

            return size;
        }
    }
}
