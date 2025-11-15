using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SimpleNetworking.Authoritative
{
    /// <summary>
    /// Authoritative game client for Castle Crashers-style games.
    /// Connects to server, sends inputs, receives snapshots, handles prediction.
    /// </summary>
    public class AuthoritativeGameClient : SimpleNetworkMessenger
    {
        [Header("Client Settings")]
        [Tooltip("Player name")]
        public string playerName = "Player";

        [Tooltip("Local player prefab")]
        public GameObject localPlayerPrefab;

        [Tooltip("Remote player prefab")]
        public GameObject remotePlayerPrefab;

        [Tooltip("Enemy prefab")]
        public GameObject enemyPrefab;

        [Header("Status")]
        [SerializeField]
        private bool inGame = false;

        [SerializeField]
        private int currentWave = 0;

        [SerializeField]
        private int playerCount = 0;

        [SerializeField]
        private int enemyCount = 0;

        [Header("Events")]
        public UnityEvent onGameJoined;
        public UnityEvent<int> onWaveChanged;

        // Client state
        private GameObject localPlayerObject;
        private ClientPrediction localPrediction;

        // Remote entities
        private Dictionary<string, GameObject> remotePlayers = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> remoteEnemies = new Dictionary<string, GameObject>();

        // Input
        private int inputSequence = 0;

        void Start()
        {
            // Subscribe to messages
            onMessageReceived.AddListener(OnNetworkMessage);
            onConnected.AddListener(OnConnectedToServer);
            onDisconnected.AddListener(OnDisconnectedFromServer);

            if (autoConnect)
            {
                Connect();
            }
        }

        void FixedUpdate()
        {
            if (inGame && localPlayerObject != null)
            {
                // Gather local input
                PlayerInput input = GatherInput();

                // Process with prediction
                if (localPrediction != null)
                {
                    localPrediction.ProcessLocalInput(input);
                }
            }
        }

        /// <summary>
        /// Gather player input
        /// </summary>
        private PlayerInput GatherInput()
        {
            PlayerInput input = new PlayerInput
            {
                sequenceId = inputSequence++,
                deltaTime = Time.fixedDeltaTime
            };

            // Movement input (WASD or Arrow Keys)
            input.moveX = Input.GetAxis("Horizontal");
            input.moveY = Input.GetAxis("Vertical");

            // Action inputs
            input.jump = Input.GetButton("Jump");
            input.attack = Input.GetButtonDown("Fire1");
            input.special = Input.GetButtonDown("Fire2");
            input.interact = Input.GetKeyDown(KeyCode.E);

            return input;
        }

        /// <summary>
        /// Send input to server
        /// </summary>
        public void SendInput(PlayerInput input)
        {
            if (!IsConnected) return;

            InputMessage inputMsg = new InputMessage(clientId, input);
            string json = JsonUtility.ToJson(inputMsg);
            SendMessage("INPUT", json);
        }

        /// <summary>
        /// Handle network messages
        /// </summary>
        private void OnNetworkMessage(NetworkMessage msg)
        {
            switch (msg.messageType)
            {
                case "SNAPSHOT":
                    OnSnapshotReceived(msg.payload);
                    break;

                case "GAME_JOINED":
                    OnGameJoined(msg.payload);
                    break;
            }
        }

        /// <summary>
        /// Handle snapshot from server
        /// </summary>
        private void OnSnapshotReceived(string json)
        {
            try
            {
                GameSnapshot snapshot = GameSnapshot.FromJson(json);
                ApplySnapshot(snapshot);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameClient] Failed to parse snapshot: {e.Message}");
            }
        }

        /// <summary>
        /// Apply snapshot to game world
        /// </summary>
        private void ApplySnapshot(GameSnapshot snapshot)
        {
            // Update wave info
            if (snapshot.currentWave != currentWave)
            {
                currentWave = snapshot.currentWave;
                onWaveChanged?.Invoke(currentWave);
            }

            playerCount = snapshot.players.Count;
            enemyCount = snapshot.enemies.Count;

            // Update players
            HashSet<string> activePlayers = new HashSet<string>();
            foreach (var playerSnapshot in snapshot.players)
            {
                activePlayers.Add(playerSnapshot.entityId);

                // Local player
                if (playerSnapshot.entityId == clientId)
                {
                    if (localPrediction != null)
                    {
                        localPrediction.OnServerSnapshot(playerSnapshot);
                    }
                }
                // Remote players
                else
                {
                    UpdateRemotePlayer(playerSnapshot);
                }
            }

            // Remove disconnected players
            var toRemove = new List<string>();
            foreach (var kvp in remotePlayers)
            {
                if (!activePlayers.Contains(kvp.Key))
                {
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (var id in toRemove)
            {
                Destroy(remotePlayers[id]);
                remotePlayers.Remove(id);
            }

            // Update enemies
            HashSet<string> activeEnemies = new HashSet<string>();
            foreach (var enemySnapshot in snapshot.enemies)
            {
                activeEnemies.Add(enemySnapshot.entityId);
                UpdateRemoteEnemy(enemySnapshot);
            }

            // Remove dead/despawned enemies
            toRemove.Clear();
            foreach (var kvp in remoteEnemies)
            {
                if (!activeEnemies.Contains(kvp.Key))
                {
                    toRemove.Add(kvp.Key);
                }
            }
            foreach (var id in toRemove)
            {
                Destroy(remoteEnemies[id]);
                remoteEnemies.Remove(id);
            }

            // Process events
            foreach (var gameEvent in snapshot.events)
            {
                HandleGameEvent(gameEvent);
            }
        }

        /// <summary>
        /// Update remote player
        /// </summary>
        private void UpdateRemotePlayer(PlayerSnapshot snapshot)
        {
            if (!remotePlayers.ContainsKey(snapshot.entityId))
            {
                // Spawn remote player
                GameObject playerObj = Instantiate(remotePlayerPrefab);
                playerObj.name = $"Player_{snapshot.playerName}";

                // Add interpolator
                var interpolator = playerObj.AddComponent<EntityInterpolator>();

                remotePlayers[snapshot.entityId] = playerObj;

                Debug.Log($"[GameClient] Spawned remote player: {snapshot.playerName}");
            }

            // Update interpolator
            GameObject remotePlayer = remotePlayers[snapshot.entityId];
            var interp = remotePlayer.GetComponent<EntityInterpolator>();
            if (interp != null)
            {
                interp.OnSnapshot(snapshot);
            }
        }

        /// <summary>
        /// Update remote enemy
        /// </summary>
        private void UpdateRemoteEnemy(EnemySnapshot snapshot)
        {
            if (!remoteEnemies.ContainsKey(snapshot.entityId))
            {
                // Spawn enemy
                GameObject enemyObj = Instantiate(enemyPrefab);
                enemyObj.name = $"Enemy_{snapshot.enemyType}_{snapshot.entityId}";

                // Add interpolator
                var interpolator = enemyObj.AddComponent<EntityInterpolator>();

                remoteEnemies[snapshot.entityId] = enemyObj;
            }

            // Update interpolator
            GameObject enemy = remoteEnemies[snapshot.entityId];
            var interp = enemy.GetComponent<EntityInterpolator>();
            if (interp != null)
            {
                interp.OnSnapshot(snapshot);
            }
        }

        /// <summary>
        /// Handle game events
        /// </summary>
        private void HandleGameEvent(GameEvent gameEvent)
        {
            switch (gameEvent.eventType)
            {
                case GameEventType.PlayerDamaged:
                case GameEventType.EnemyDamaged:
                    HandleDamageEvent((DamageEvent)gameEvent);
                    break;

                case GameEventType.WaveComplete:
                    Debug.Log($"[GameClient] Wave {currentWave} complete!");
                    break;
            }
        }

        /// <summary>
        /// Handle damage event (play effects, etc.)
        /// </summary>
        private void HandleDamageEvent(DamageEvent damageEvent)
        {
            // Play hit effect, sound, etc.
            Debug.Log($"[GameClient] Damage: {damageEvent.attackerId} hit {damageEvent.targetId} for {damageEvent.damage}");
        }

        /// <summary>
        /// Join game on server
        /// </summary>
        private void OnConnectedToServer()
        {
            Debug.Log("[GameClient] Connected to server, joining game...");

            // Send join request
            SendMessage("JOIN_GAME", playerName);
        }

        /// <summary>
        /// Handle successful game join
        /// </summary>
        private void OnGameJoined(string payload)
        {
            Debug.Log($"[GameClient] Joined game!");

            // Spawn local player
            SpawnLocalPlayer();

            inGame = true;
            onGameJoined?.Invoke();
        }

        /// <summary>
        /// Spawn local player
        /// </summary>
        private void SpawnLocalPlayer()
        {
            if (localPlayerPrefab != null)
            {
                localPlayerObject = Instantiate(localPlayerPrefab);
                localPlayerObject.name = "LocalPlayer";

                // Add prediction component
                localPrediction = localPlayerObject.AddComponent<ClientPrediction>();

                Debug.Log("[GameClient] Local player spawned");
            }
        }

        /// <summary>
        /// Handle disconnection
        /// </summary>
        private void OnDisconnectedFromServer()
        {
            Debug.Log("[GameClient] Disconnected from server");

            // Cleanup
            if (localPlayerObject != null)
            {
                Destroy(localPlayerObject);
            }

            foreach (var player in remotePlayers.Values)
            {
                Destroy(player);
            }
            remotePlayers.Clear();

            foreach (var enemy in remoteEnemies.Values)
            {
                Destroy(enemy);
            }
            remoteEnemies.Clear();

            inGame = false;
        }
    }
}
