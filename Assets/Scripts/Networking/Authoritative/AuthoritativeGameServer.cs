using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace SimpleNetworking.Authoritative
{
    /// <summary>
    /// Authoritative game server for Castle Crashers-style games.
    /// Runs game logic at fixed tick rate and sends snapshots to clients.
    /// </summary>
    public class AuthoritativeGameServer : SimpleServer
    {
        [Header("Game Server Settings")]
        [Tooltip("Server tick rate (game updates per second)")]
        public int tickRate = 60;

        [Tooltip("Snapshot send rate (per second)")]
        public int snapshotRate = 20;

        [Tooltip("Auto-spawn waves")]
        public bool autoSpawnWaves = true;

        [Tooltip("Spawn first wave on start")]
        public bool spawnInitialWave = true;

        [Header("Game Status")]
        [SerializeField]
        private int currentTick = 0;

        [SerializeField]
        private int currentWave = 0;

        [SerializeField]
        private int totalPlayers = 0;

        [SerializeField]
        private int aliveEnemies = 0;

        // Game systems
        private GameState gameState;
        private CombatSystem combatSystem;
        private SnapshotSystem snapshotSystem;

        // Timing
        private Thread gameLoopThread;
        private bool gameLoopRunning = false;
        private float fixedDeltaTime;

        // Snapshot timing
        private int ticksPerSnapshot;
        private int snapshotCounter = 0;

        // Player input buffers
        private Dictionary<string, Queue<PlayerInput>> inputBuffers = new Dictionary<string, Queue<PlayerInput>>();
        private readonly object inputLock = new object();

        void Awake()
        {
            // Initialize game systems
            gameState = new GameState();
            combatSystem = new CombatSystem(gameState);
            snapshotSystem = new SnapshotSystem(gameState);

            fixedDeltaTime = 1f / tickRate;
            ticksPerSnapshot = tickRate / snapshotRate;

            Debug.Log($"[AuthServer] Initialized - Tick: {tickRate}Hz, Snapshots: {snapshotRate}Hz");
        }

        /// <summary>
        /// Start the game server
        /// </summary>
        public new void StartServer()
        {
            // Start network server
            base.StartServer();

            // Start game loop
            StartGameLoop();

            // Spawn initial wave
            if (spawnInitialWave && autoSpawnWaves)
            {
                Invoke(nameof(SpawnWave), 2f);
            }
        }

        /// <summary>
        /// Stop the game server
        /// </summary>
        public new void StopServer()
        {
            StopGameLoop();
            base.StopServer();
        }

        /// <summary>
        /// Start the authoritative game loop
        /// </summary>
        private void StartGameLoop()
        {
            if (gameLoopRunning) return;

            gameLoopRunning = true;
            gameLoopThread = new Thread(GameLoop);
            gameLoopThread.IsBackground = true;
            gameLoopThread.Start();

            Debug.Log("[AuthServer] Game loop started");
        }

        /// <summary>
        /// Stop the game loop
        /// </summary>
        private void StopGameLoop()
        {
            gameLoopRunning = false;

            if (gameLoopThread != null && gameLoopThread.IsAlive)
            {
                gameLoopThread.Join(1000);
            }

            Debug.Log("[AuthServer] Game loop stopped");
        }

        /// <summary>
        /// Main game loop - runs at fixed tick rate
        /// </summary>
        private void GameLoop()
        {
            float accumulator = 0f;
            DateTime lastTime = DateTime.UtcNow;

            while (gameLoopRunning)
            {
                DateTime currentTime = DateTime.UtcNow;
                float deltaTime = (float)(currentTime - lastTime).TotalSeconds;
                lastTime = currentTime;

                accumulator += deltaTime;

                // Fixed timestep updates
                while (accumulator >= fixedDeltaTime)
                {
                    Tick();
                    accumulator -= fixedDeltaTime;
                }

                // Sleep to maintain tick rate
                float sleepTime = fixedDeltaTime - accumulator;
                if (sleepTime > 0)
                {
                    Thread.Sleep((int)(sleepTime * 1000f));
                }
            }
        }

        /// <summary>
        /// Single game tick
        /// </summary>
        private void Tick()
        {
            // Process all player inputs
            ProcessPlayerInputs();

            // Update game state
            gameState.Update(fixedDeltaTime);

            // Process combat
            combatSystem.ProcessCombat();

            // Send snapshots
            snapshotCounter++;
            if (snapshotCounter >= ticksPerSnapshot)
            {
                SendSnapshots();
                snapshotCounter = 0;
            }

            // Update display values
            currentTick = gameState.serverTick;
            currentWave = gameState.currentWave;
            totalPlayers = gameState.GetPlayerCount();
            aliveEnemies = gameState.GetAliveEnemyCount();

            // Auto-spawn waves
            if (autoSpawnWaves && aliveEnemies == 0 && totalPlayers > 0)
            {
                // Delay before next wave
                if (gameState.serverTick % (tickRate * 5) == 0) // Every 5 seconds
                {
                    SpawnWave();
                }
            }
        }

        /// <summary>
        /// Process all queued player inputs
        /// </summary>
        private void ProcessPlayerInputs()
        {
            lock (inputLock)
            {
                foreach (var kvp in inputBuffers)
                {
                    string playerId = kvp.Key;
                    Queue<PlayerInput> inputs = kvp.Value;

                    PlayerEntity player = gameState.GetPlayer(playerId);
                    if (player != null)
                    {
                        // Process all inputs for this player
                        while (inputs.Count > 0)
                        {
                            PlayerInput input = inputs.Dequeue();
                            player.QueueInput(input);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Send snapshots to all clients
        /// </summary>
        private void SendSnapshots()
        {
            var clientIds = GetConnectedClientIds();

            foreach (string clientId in clientIds)
            {
                // Create optimized snapshot for this client
                GameSnapshot snapshot = snapshotSystem.CreateSnapshot(clientId);

                // Send to client
                string json = snapshot.ToJson();
                SendMessageToClient(clientId, "SNAPSHOT", json);
            }
        }

        /// <summary>
        /// Handle player joining
        /// </summary>
        public void OnPlayerJoin(string playerId, string playerName)
        {
            // Add player to game state
            PlayerEntity player = gameState.AddPlayer(playerId, playerName);

            // Create input buffer
            lock (inputLock)
            {
                if (!inputBuffers.ContainsKey(playerId))
                {
                    inputBuffers[playerId] = new Queue<PlayerInput>();
                }
            }

            Debug.Log($"[AuthServer] Player {playerName} joined game");

            // Send initial snapshot
            GameSnapshot snapshot = snapshotSystem.CreateSnapshot(playerId);
            SendMessageToClient(playerId, "SNAPSHOT", snapshot.ToJson());
        }

        /// <summary>
        /// Handle player leaving
        /// </summary>
        public void OnPlayerLeave(string playerId)
        {
            gameState.RemovePlayer(playerId);

            lock (inputLock)
            {
                inputBuffers.Remove(playerId);
            }

            Debug.Log($"[AuthServer] Player left game");
        }

        /// <summary>
        /// Handle player input
        /// </summary>
        public void OnPlayerInput(string playerId, PlayerInput input)
        {
            lock (inputLock)
            {
                if (inputBuffers.ContainsKey(playerId))
                {
                    inputBuffers[playerId].Enqueue(input);
                }
            }
        }

        /// <summary>
        /// Spawn a wave of enemies
        /// </summary>
        public void SpawnWave()
        {
            gameState.SpawnWave();
        }

        /// <summary>
        /// Send message to specific client
        /// </summary>
        private void SendMessageToClient(string clientId, string messageType, string payload)
        {
            // This would integrate with the base SimpleServer
            // Implementation depends on how SimpleServer tracks clients
            NetworkMessage msg = new NetworkMessage(clientId, messageType, payload);
            // Send via base server...
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying || gameState == null) return;

            // Draw players
            Gizmos.color = Color.green;
            foreach (var player in gameState.players.Values)
            {
                if (player.isAlive)
                {
                    Vector3 pos = new Vector3(player.position.x, player.position.y, 0);
                    Gizmos.DrawWireCube(pos, Vector3.one);
                }
            }

            // Draw enemies
            Gizmos.color = Color.red;
            foreach (var enemy in gameState.enemies.Values)
            {
                if (enemy.isAlive)
                {
                    Vector3 pos = new Vector3(enemy.position.x, enemy.position.y, 0);
                    Gizmos.DrawWireSphere(pos, 0.5f);
                }
            }
        }
    }
}
