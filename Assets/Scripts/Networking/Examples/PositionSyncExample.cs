using System.Collections.Generic;
using UnityEngine;

namespace SimpleNetworking.Examples
{
    /// <summary>
    /// Transform data for network synchronization
    /// </summary>
    [System.Serializable]
    public class TransformData
    {
        public float posX;
        public float posY;
        public float posZ;
        public float rotX;
        public float rotY;
        public float rotZ;
        public float rotW;

        public TransformData() { }

        public TransformData(Vector3 position, Quaternion rotation)
        {
            posX = position.x;
            posY = position.y;
            posZ = position.z;
            rotX = rotation.x;
            rotY = rotation.y;
            rotZ = rotation.z;
            rotW = rotation.w;
        }

        public Vector3 GetPosition()
        {
            return new Vector3(posX, posY, posZ);
        }

        public Quaternion GetRotation()
        {
            return new Quaternion(rotX, rotY, rotZ, rotW);
        }
    }

    /// <summary>
    /// Example of synchronizing object positions between clients.
    /// Useful for multiplayer games where players need to see each other's positions.
    /// </summary>
    public class PositionSyncExample : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Reference to the network messenger")]
        public SimpleNetworkMessenger messenger;

        [Header("Sync Settings")]
        [Tooltip("How often to send position updates (times per second)")]
        public float updateRate = 10f;

        [Tooltip("Enable position synchronization")]
        public bool syncPosition = true;

        [Tooltip("Enable rotation synchronization")]
        public bool syncRotation = false;

        [Header("Remote Player Settings")]
        [Tooltip("Prefab to spawn for remote players")]
        public GameObject remotePlayerPrefab;

        [Tooltip("Parent transform for remote players")]
        public Transform remotePlayersParent;

        [Header("Interpolation")]
        [Tooltip("Smooth remote player movement")]
        public bool useInterpolation = true;

        [Tooltip("Interpolation speed")]
        public float interpolationSpeed = 10f;

        // Internal state
        private float sendTimer = 0f;
        private Dictionary<string, RemotePlayer> remotePlayers = new Dictionary<string, RemotePlayer>();

        // Remote player data
        private class RemotePlayer
        {
            public string clientId;
            public GameObject gameObject;
            public Vector3 targetPosition;
            public Quaternion targetRotation;
        }

        void Start()
        {
            // Get messenger if not assigned
            if (messenger == null)
            {
                messenger = GetComponent<SimpleNetworkMessenger>();
            }

            // Subscribe to message events
            if (messenger != null)
            {
                messenger.onMessageReceived.AddListener(OnMessageReceived);
                messenger.onDisconnected.AddListener(OnDisconnected);
            }
        }

        void Update()
        {
            // Send position updates
            if (messenger != null && messenger.IsConnected)
            {
                sendTimer += Time.deltaTime;
                if (sendTimer >= 1f / updateRate)
                {
                    SendTransformUpdate();
                    sendTimer = 0f;
                }
            }

            // Interpolate remote players
            if (useInterpolation)
            {
                InterpolateRemotePlayers();
            }
        }

        /// <summary>
        /// Send transform update to other clients
        /// </summary>
        private void SendTransformUpdate()
        {
            if (!syncPosition && !syncRotation) return;

            Vector3 pos = syncPosition ? transform.position : Vector3.zero;
            Quaternion rot = syncRotation ? transform.rotation : Quaternion.identity;

            TransformData data = new TransformData(pos, rot);
            messenger.SendMessage(MessageType.TRANSFORM_UPDATE, data);
        }

        /// <summary>
        /// Handle received messages
        /// </summary>
        private void OnMessageReceived(NetworkMessage message)
        {
            if (message.messageType == MessageType.TRANSFORM_UPDATE)
            {
                try
                {
                    TransformData data = JsonUtility.FromJson<TransformData>(message.payload);
                    UpdateRemotePlayer(message.senderId, data);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[PositionSync] Failed to parse transform data: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Update or create remote player
        /// </summary>
        private void UpdateRemotePlayer(string clientId, TransformData data)
        {
            // Get or create remote player
            if (!remotePlayers.ContainsKey(clientId))
            {
                CreateRemotePlayer(clientId);
            }

            RemotePlayer player = remotePlayers[clientId];
            if (player != null && player.gameObject != null)
            {
                if (useInterpolation)
                {
                    player.targetPosition = data.GetPosition();
                    player.targetRotation = data.GetRotation();
                }
                else
                {
                    player.gameObject.transform.position = data.GetPosition();
                    player.gameObject.transform.rotation = data.GetRotation();
                }
            }
        }

        /// <summary>
        /// Create a new remote player
        /// </summary>
        private void CreateRemotePlayer(string clientId)
        {
            if (remotePlayerPrefab == null)
            {
                Debug.LogWarning("[PositionSync] No remote player prefab assigned");
                return;
            }

            GameObject go = Instantiate(remotePlayerPrefab);
            go.name = $"RemotePlayer_{clientId}";

            if (remotePlayersParent != null)
            {
                go.transform.SetParent(remotePlayersParent);
            }

            RemotePlayer player = new RemotePlayer
            {
                clientId = clientId,
                gameObject = go,
                targetPosition = go.transform.position,
                targetRotation = go.transform.rotation
            };

            remotePlayers[clientId] = player;
            Debug.Log($"[PositionSync] Created remote player: {clientId}");
        }

        /// <summary>
        /// Interpolate remote players towards their target positions
        /// </summary>
        private void InterpolateRemotePlayers()
        {
            foreach (var player in remotePlayers.Values)
            {
                if (player.gameObject == null) continue;

                if (syncPosition)
                {
                    player.gameObject.transform.position = Vector3.Lerp(
                        player.gameObject.transform.position,
                        player.targetPosition,
                        interpolationSpeed * Time.deltaTime
                    );
                }

                if (syncRotation)
                {
                    player.gameObject.transform.rotation = Quaternion.Slerp(
                        player.gameObject.transform.rotation,
                        player.targetRotation,
                        interpolationSpeed * Time.deltaTime
                    );
                }
            }
        }

        /// <summary>
        /// Handle disconnection - clean up remote players
        /// </summary>
        private void OnDisconnected()
        {
            foreach (var player in remotePlayers.Values)
            {
                if (player.gameObject != null)
                {
                    Destroy(player.gameObject);
                }
            }
            remotePlayers.Clear();
        }

        /// <summary>
        /// Remove a specific remote player
        /// </summary>
        public void RemoveRemotePlayer(string clientId)
        {
            if (remotePlayers.ContainsKey(clientId))
            {
                if (remotePlayers[clientId].gameObject != null)
                {
                    Destroy(remotePlayers[clientId].gameObject);
                }
                remotePlayers.Remove(clientId);
            }
        }
    }
}
