using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace SimpleNetworking
{
    /// <summary>
    /// Simple network messenger for client-to-client communication through a server.
    /// Handles TCP connection, message sending, and receiving.
    /// </summary>
    public class SimpleNetworkMessenger : MonoBehaviour
    {
        [Header("Connection Settings")]
        [Tooltip("Server IP address to connect to")]
        public string serverAddress = "127.0.0.1";

        [Tooltip("Server port to connect to")]
        public int serverPort = NetworkConstants.DEFAULT_SERVER_PORT;

        [Tooltip("Auto-connect on Start")]
        public bool autoConnect = false;

        [Tooltip("Unique client ID (auto-generated if empty)")]
        public string clientId;

        [Header("Status")]
        [SerializeField, Tooltip("Current connection status")]
        private bool isConnected = false;

        [SerializeField, Tooltip("Number of messages sent")]
        private int messagesSent = 0;

        [SerializeField, Tooltip("Number of messages received")]
        private int messagesReceived = 0;

        [Header("Events")]
        public UnityEvent onConnected;
        public UnityEvent onDisconnected;
        public MessageEvent onMessageReceived;

        // Internal state
        private TcpClient tcpClient;
        private NetworkStream stream;
        private Thread receiveThread;
        private Queue<NetworkMessage> messageQueue = new Queue<NetworkMessage>();
        private readonly object queueLock = new object();
        private bool shouldStop = false;

        public bool IsConnected => isConnected;
        public int MessagesSent => messagesSent;
        public int MessagesReceived => messagesReceived;

        void Awake()
        {
            // Generate unique client ID if not set
            if (string.IsNullOrEmpty(clientId))
            {
                clientId = System.Guid.NewGuid().ToString().Substring(0, 8);
            }
        }

        void Start()
        {
            if (autoConnect)
            {
                Connect();
            }
        }

        void Update()
        {
            // Process received messages on main thread
            ProcessMessageQueue();
        }

        void OnDestroy()
        {
            Disconnect();
        }

        void OnApplicationQuit()
        {
            Disconnect();
        }

        /// <summary>
        /// Connect to the server
        /// </summary>
        public void Connect()
        {
            if (isConnected)
            {
                Debug.LogWarning($"[NetworkMessenger] Already connected to {serverAddress}:{serverPort}");
                return;
            }

            try
            {
                Debug.Log($"[NetworkMessenger] Connecting to {serverAddress}:{serverPort}...");

                tcpClient = new TcpClient();
                tcpClient.Connect(serverAddress, serverPort);
                stream = tcpClient.GetStream();
                isConnected = true;

                // Send connect message
                SendMessage(MessageType.CONNECT, "");

                // Start receive thread
                shouldStop = false;
                receiveThread = new Thread(ReceiveMessages);
                receiveThread.IsBackground = true;
                receiveThread.Start();

                Debug.Log($"[NetworkMessenger] Connected as client: {clientId}");
                onConnected?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkMessenger] Connection failed: {e.Message}");
                isConnected = false;
            }
        }

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        public void Disconnect()
        {
            if (!isConnected) return;

            try
            {
                // Send disconnect message
                SendMessage(MessageType.DISCONNECT, "");

                // Stop receive thread
                shouldStop = true;

                // Close connections
                stream?.Close();
                tcpClient?.Close();

                // Wait for thread to finish
                if (receiveThread != null && receiveThread.IsAlive)
                {
                    receiveThread.Join(1000);
                }

                Debug.Log($"[NetworkMessenger] Disconnected from server");
                isConnected = false;
                onDisconnected?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkMessenger] Disconnect error: {e.Message}");
            }
        }

        /// <summary>
        /// Send a message to all other clients
        /// </summary>
        public void SendMessage(string messageType, string payload)
        {
            if (!isConnected)
            {
                Debug.LogWarning("[NetworkMessenger] Cannot send message: not connected");
                return;
            }

            try
            {
                NetworkMessage message = new NetworkMessage(clientId, messageType, payload);
                string json = message.ToJson();
                byte[] data = Encoding.UTF8.GetBytes(json);

                // Prefix message with length (4 bytes)
                byte[] lengthPrefix = BitConverter.GetBytes(data.Length);

                stream.Write(lengthPrefix, 0, lengthPrefix.Length);
                stream.Write(data, 0, data.Length);
                stream.Flush();

                messagesSent++;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkMessenger] Send error: {e.Message}");
                HandleDisconnection();
            }
        }

        /// <summary>
        /// Send a typed message
        /// </summary>
        public void SendMessage<T>(string messageType, T data) where T : class
        {
            string json = JsonUtility.ToJson(data);
            SendMessage(messageType, json);
        }

        /// <summary>
        /// Background thread to receive messages
        /// </summary>
        private void ReceiveMessages()
        {
            byte[] lengthBuffer = new byte[4];

            while (!shouldStop && isConnected)
            {
                try
                {
                    // Read message length
                    int bytesRead = stream.Read(lengthBuffer, 0, 4);
                    if (bytesRead == 0) break;

                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

                    // Read message data
                    byte[] buffer = new byte[messageLength];
                    int totalRead = 0;

                    while (totalRead < messageLength)
                    {
                        bytesRead = stream.Read(buffer, totalRead, messageLength - totalRead);
                        if (bytesRead == 0) break;
                        totalRead += bytesRead;
                    }

                    if (totalRead == messageLength)
                    {
                        string json = Encoding.UTF8.GetString(buffer);
                        NetworkMessage message = NetworkMessage.FromJson(json);

                        // Queue message for main thread processing
                        lock (queueLock)
                        {
                            messageQueue.Enqueue(message);
                        }

                        messagesReceived++;
                    }
                }
                catch (Exception e)
                {
                    if (!shouldStop)
                    {
                        Debug.LogError($"[NetworkMessenger] Receive error: {e.Message}");
                        HandleDisconnection();
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Process received messages on main thread
        /// </summary>
        private void ProcessMessageQueue()
        {
            lock (queueLock)
            {
                while (messageQueue.Count > 0)
                {
                    NetworkMessage message = messageQueue.Dequeue();

                    // Don't process our own messages
                    if (message.senderId == clientId) continue;

                    // Invoke event
                    onMessageReceived?.Invoke(message);
                }
            }
        }

        /// <summary>
        /// Handle unexpected disconnection
        /// </summary>
        private void HandleDisconnection()
        {
            if (isConnected)
            {
                isConnected = false;
                onDisconnected?.Invoke();
            }
        }

        /// <summary>
        /// Send a ping to test connection
        /// </summary>
        public void SendPing()
        {
            SendMessage(MessageType.PING, DateTime.UtcNow.Ticks.ToString());
        }
    }

    /// <summary>
    /// UnityEvent for NetworkMessage
    /// </summary>
    [Serializable]
    public class MessageEvent : UnityEvent<NetworkMessage> { }
}
