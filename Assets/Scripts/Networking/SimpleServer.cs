using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace SimpleNetworking
{
    /// <summary>
    /// Connected client information
    /// </summary>
    public class ConnectedClient
    {
        public string clientId;
        public TcpClient tcpClient;
        public NetworkStream stream;
        public Thread receiveThread;
        public DateTime connectedTime;

        public ConnectedClient(TcpClient client)
        {
            this.tcpClient = client;
            this.stream = client.GetStream();
            this.connectedTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Simple relay server for client-to-client communication.
    /// Receives messages from clients and broadcasts to all other connected clients.
    /// </summary>
    public class SimpleServer : MonoBehaviour
    {
        [Header("Server Settings")]
        [Tooltip("Port to listen on")]
        public int serverPort = NetworkConstants.DEFAULT_SERVER_PORT;

        [Tooltip("Maximum number of clients")]
        public int maxClients = 10;

        [Tooltip("Auto-start server on Start")]
        public bool autoStart = false;

        [Header("Status")]
        [SerializeField]
        private bool isRunning = false;

        [SerializeField]
        private int connectedClients = 0;

        [SerializeField]
        private int totalMessagesRelayed = 0;

        // Internal state
        private TcpListener tcpListener;
        private Thread listenThread;
        private Dictionary<string, ConnectedClient> clients = new Dictionary<string, ConnectedClient>();
        private readonly object clientsLock = new object();
        private bool shouldStop = false;

        public bool IsRunning => isRunning;
        public int ConnectedClients => connectedClients;
        public int TotalMessagesRelayed => totalMessagesRelayed;

        void Start()
        {
            if (autoStart)
            {
                StartServer();
            }
        }

        void OnDestroy()
        {
            StopServer();
        }

        void OnApplicationQuit()
        {
            StopServer();
        }

        /// <summary>
        /// Start the server
        /// </summary>
        public void StartServer()
        {
            if (isRunning)
            {
                Debug.LogWarning("[SimpleServer] Server already running");
                return;
            }

            try
            {
                tcpListener = new TcpListener(IPAddress.Any, serverPort);
                tcpListener.Start();

                shouldStop = false;
                listenThread = new Thread(ListenForClients);
                listenThread.IsBackground = true;
                listenThread.Start();

                isRunning = true;
                Debug.Log($"[SimpleServer] Server started on port {serverPort}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleServer] Failed to start server: {e.Message}");
            }
        }

        /// <summary>
        /// Stop the server
        /// </summary>
        public void StopServer()
        {
            if (!isRunning) return;

            Debug.Log("[SimpleServer] Stopping server...");

            shouldStop = true;
            tcpListener?.Stop();

            // Disconnect all clients
            lock (clientsLock)
            {
                foreach (var client in clients.Values)
                {
                    DisconnectClient(client);
                }
                clients.Clear();
            }

            // Wait for listen thread
            if (listenThread != null && listenThread.IsAlive)
            {
                listenThread.Join(1000);
            }

            isRunning = false;
            connectedClients = 0;
            Debug.Log("[SimpleServer] Server stopped");
        }

        /// <summary>
        /// Listen for incoming client connections
        /// </summary>
        private void ListenForClients()
        {
            while (!shouldStop)
            {
                try
                {
                    // Wait for client connection
                    TcpClient tcpClient = tcpListener.AcceptTcpClient();

                    // Check max clients
                    if (clients.Count >= maxClients)
                    {
                        Debug.LogWarning("[SimpleServer] Max clients reached, rejecting connection");
                        tcpClient.Close();
                        continue;
                    }

                    // Create client wrapper
                    ConnectedClient client = new ConnectedClient(tcpClient);

                    // Start receive thread for this client
                    client.receiveThread = new Thread(() => HandleClient(client));
                    client.receiveThread.IsBackground = true;
                    client.receiveThread.Start();

                    Debug.Log($"[SimpleServer] Client connected from {((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address}");
                }
                catch (SocketException)
                {
                    // Socket closed, exit thread
                    break;
                }
                catch (Exception e)
                {
                    if (!shouldStop)
                    {
                        Debug.LogError($"[SimpleServer] Accept error: {e.Message}");
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Handle messages from a specific client
        /// </summary>
        private void HandleClient(ConnectedClient client)
        {
            byte[] lengthBuffer = new byte[4];
            bool clientRegistered = false;

            try
            {
                while (!shouldStop && client.tcpClient.Connected)
                {
                    // Read message length
                    int bytesRead = client.stream.Read(lengthBuffer, 0, 4);
                    if (bytesRead == 0) break;

                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

                    // Read message data
                    byte[] buffer = new byte[messageLength];
                    int totalRead = 0;

                    while (totalRead < messageLength)
                    {
                        bytesRead = client.stream.Read(buffer, totalRead, messageLength - totalRead);
                        if (bytesRead == 0) break;
                        totalRead += bytesRead;
                    }

                    if (totalRead == messageLength)
                    {
                        string json = Encoding.UTF8.GetString(buffer);
                        NetworkMessage message = NetworkMessage.FromJson(json);

                        // Register client on first message
                        if (!clientRegistered)
                        {
                            client.clientId = message.senderId;
                            lock (clientsLock)
                            {
                                clients[client.clientId] = client;
                                connectedClients = clients.Count;
                            }
                            clientRegistered = true;
                            Debug.Log($"[SimpleServer] Client registered: {client.clientId}");
                        }

                        // Handle disconnect message
                        if (message.messageType == MessageType.DISCONNECT)
                        {
                            Debug.Log($"[SimpleServer] Client {client.clientId} disconnecting");
                            break;
                        }

                        // Broadcast message to all other clients
                        BroadcastMessage(message, client.clientId);
                        totalMessagesRelayed++;
                    }
                }
            }
            catch (Exception e)
            {
                if (!shouldStop)
                {
                    Debug.LogError($"[SimpleServer] Client handler error: {e.Message}");
                }
            }
            finally
            {
                // Remove client
                if (clientRegistered)
                {
                    lock (clientsLock)
                    {
                        if (clients.ContainsKey(client.clientId))
                        {
                            clients.Remove(client.clientId);
                            connectedClients = clients.Count;
                        }
                    }
                    Debug.Log($"[SimpleServer] Client disconnected: {client.clientId}");
                }

                DisconnectClient(client);
            }
        }

        /// <summary>
        /// Broadcast message to all clients except sender
        /// </summary>
        private void BroadcastMessage(NetworkMessage message, string senderClientId)
        {
            byte[] data = Encoding.UTF8.GetBytes(message.ToJson());
            byte[] lengthPrefix = BitConverter.GetBytes(data.Length);

            lock (clientsLock)
            {
                List<string> disconnectedClients = new List<string>();

                foreach (var kvp in clients)
                {
                    // Skip sender
                    if (kvp.Key == senderClientId) continue;

                    try
                    {
                        NetworkStream stream = kvp.Value.stream;
                        stream.Write(lengthPrefix, 0, lengthPrefix.Length);
                        stream.Write(data, 0, data.Length);
                        stream.Flush();
                    }
                    catch (Exception)
                    {
                        // Mark for removal
                        disconnectedClients.Add(kvp.Key);
                    }
                }

                // Remove disconnected clients
                foreach (string clientId in disconnectedClients)
                {
                    if (clients.ContainsKey(clientId))
                    {
                        DisconnectClient(clients[clientId]);
                        clients.Remove(clientId);
                    }
                }

                connectedClients = clients.Count;
            }
        }

        /// <summary>
        /// Disconnect a client
        /// </summary>
        private void DisconnectClient(ConnectedClient client)
        {
            try
            {
                client.stream?.Close();
                client.tcpClient?.Close();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SimpleServer] Disconnect error: {e.Message}");
            }
        }

        /// <summary>
        /// Get list of connected client IDs
        /// </summary>
        public List<string> GetConnectedClientIds()
        {
            lock (clientsLock)
            {
                return new List<string>(clients.Keys);
            }
        }
    }
}
