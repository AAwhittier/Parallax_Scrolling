using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace SimpleNetworking
{
    /// <summary>
    /// Server information discovered on the network
    /// </summary>
    [Serializable]
    public class ServerInfo
    {
        public string serverName;
        public string ipAddress;
        public int port;
        public long timestamp;

        public ServerInfo(string serverName, string ipAddress, int port)
        {
            this.serverName = serverName;
            this.ipAddress = ipAddress;
            this.port = port;
            this.timestamp = DateTime.UtcNow.Ticks;
        }

        public float GetAgeSeconds()
        {
            return (DateTime.UtcNow.Ticks - timestamp) / (float)TimeSpan.TicksPerSecond;
        }
    }

    /// <summary>
    /// Network discovery for finding servers on local network using UDP broadcast.
    /// Can operate as both client (searching for servers) and server (responding to searches).
    /// </summary>
    public class NetworkDiscovery : MonoBehaviour
    {
        [Header("Discovery Settings")]
        [Tooltip("UDP port for discovery broadcasts")]
        public int discoveryPort = NetworkConstants.DEFAULT_DISCOVERY_PORT;

        [Tooltip("Server name to broadcast (only used in server mode)")]
        public string serverName = "Unity Game Server";

        [Tooltip("Server port to advertise (only used in server mode)")]
        public int serverPort = NetworkConstants.DEFAULT_SERVER_PORT;

        [Header("Server Mode")]
        [Tooltip("Enable to respond to discovery requests")]
        public bool isServerMode = false;

        [Tooltip("Auto-start server on Start")]
        public bool autoStartServer = false;

        [Header("Status")]
        [SerializeField]
        private bool isListening = false;

        [SerializeField]
        private List<ServerInfo> discoveredServers = new List<ServerInfo>();

        [Header("Events")]
        public ServerDiscoveredEvent onServerDiscovered;

        // Internal state
        private UdpClient udpClient;
        private Thread listenThread;
        private bool shouldStop = false;
        private Queue<ServerInfo> serverQueue = new Queue<ServerInfo>();
        private readonly object queueLock = new object();

        public bool IsListening => isListening;
        public List<ServerInfo> DiscoveredServers => new List<ServerInfo>(discoveredServers);

        void Start()
        {
            if (isServerMode && autoStartServer)
            {
                StartServer();
            }
        }

        void Update()
        {
            ProcessServerQueue();
        }

        void OnDestroy()
        {
            Stop();
        }

        void OnApplicationQuit()
        {
            Stop();
        }

        /// <summary>
        /// Start listening for discovery broadcasts (server mode)
        /// </summary>
        public void StartServer()
        {
            if (isListening)
            {
                Debug.LogWarning("[NetworkDiscovery] Already listening");
                return;
            }

            try
            {
                udpClient = new UdpClient(discoveryPort);
                udpClient.EnableBroadcast = true;

                shouldStop = false;
                listenThread = new Thread(ListenForRequests);
                listenThread.IsBackground = true;
                listenThread.Start();

                isListening = true;
                Debug.Log($"[NetworkDiscovery] Server listening on port {discoveryPort}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkDiscovery] Failed to start server: {e.Message}");
            }
        }

        /// <summary>
        /// Broadcast a discovery request to find servers
        /// </summary>
        public void BroadcastDiscoveryRequest()
        {
            try
            {
                using (UdpClient client = new UdpClient())
                {
                    client.EnableBroadcast = true;

                    // Create discovery request message
                    string request = MessageType.DISCOVER_REQUEST;
                    byte[] data = Encoding.UTF8.GetBytes(request);

                    // Broadcast to all devices on network
                    IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, discoveryPort);
                    client.Send(data, data.Length, endPoint);

                    Debug.Log($"[NetworkDiscovery] Broadcasting discovery request on port {discoveryPort}");
                }

                // Start temporary listener for responses
                if (!isListening && !isServerMode)
                {
                    StartTemporaryListener();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkDiscovery] Broadcast failed: {e.Message}");
            }
        }

        /// <summary>
        /// Start a temporary listener to receive discovery responses
        /// </summary>
        private void StartTemporaryListener()
        {
            if (isListening) return;

            try
            {
                udpClient = new UdpClient(discoveryPort);
                udpClient.EnableBroadcast = true;

                shouldStop = false;
                listenThread = new Thread(ListenForResponses);
                listenThread.IsBackground = true;
                listenThread.Start();

                isListening = true;

                // Auto-stop after timeout
                Invoke(nameof(Stop), NetworkConstants.DISCOVERY_TIMEOUT);
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkDiscovery] Failed to start listener: {e.Message}");
            }
        }

        /// <summary>
        /// Listen for discovery requests (server mode)
        /// </summary>
        private void ListenForRequests()
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, discoveryPort);

            while (!shouldStop)
            {
                try
                {
                    byte[] data = udpClient.Receive(ref remoteEndPoint);
                    string message = Encoding.UTF8.GetString(data);

                    if (message == MessageType.DISCOVER_REQUEST)
                    {
                        Debug.Log($"[NetworkDiscovery] Received discovery request from {remoteEndPoint.Address}");

                        // Send response
                        ServerInfo info = new ServerInfo(serverName, GetLocalIPAddress(), serverPort);
                        string response = JsonUtility.ToJson(info);
                        byte[] responseData = Encoding.UTF8.GetBytes(response);

                        udpClient.Send(responseData, responseData.Length, remoteEndPoint);

                        Debug.Log($"[NetworkDiscovery] Sent discovery response to {remoteEndPoint.Address}");
                    }
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
                        Debug.LogError($"[NetworkDiscovery] Listen error: {e.Message}");
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Listen for discovery responses (client mode)
        /// </summary>
        private void ListenForResponses()
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, discoveryPort);

            while (!shouldStop)
            {
                try
                {
                    byte[] data = udpClient.Receive(ref remoteEndPoint);
                    string json = Encoding.UTF8.GetString(data);

                    // Try to parse as ServerInfo
                    try
                    {
                        ServerInfo serverInfo = JsonUtility.FromJson<ServerInfo>(json);
                        if (serverInfo != null && !string.IsNullOrEmpty(serverInfo.serverName))
                        {
                            lock (queueLock)
                            {
                                serverQueue.Enqueue(serverInfo);
                            }
                        }
                    }
                    catch
                    {
                        // Not a server info message, ignore
                    }
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
                        Debug.LogError($"[NetworkDiscovery] Listen error: {e.Message}");
                    }
                    break;
                }
            }
        }

        /// <summary>
        /// Process discovered servers on main thread
        /// </summary>
        private void ProcessServerQueue()
        {
            lock (queueLock)
            {
                while (serverQueue.Count > 0)
                {
                    ServerInfo serverInfo = serverQueue.Dequeue();

                    // Check if server already discovered
                    bool isDuplicate = discoveredServers.Exists(s =>
                        s.ipAddress == serverInfo.ipAddress && s.port == serverInfo.port);

                    if (!isDuplicate)
                    {
                        discoveredServers.Add(serverInfo);
                        Debug.Log($"[NetworkDiscovery] Discovered server: {serverInfo.serverName} at {serverInfo.ipAddress}:{serverInfo.port}");
                        onServerDiscovered?.Invoke(serverInfo);
                    }
                }
            }
        }

        /// <summary>
        /// Stop listening and cleanup
        /// </summary>
        public void Stop()
        {
            if (!isListening) return;

            shouldStop = true;
            udpClient?.Close();

            if (listenThread != null && listenThread.IsAlive)
            {
                listenThread.Join(1000);
            }

            isListening = false;
            Debug.Log("[NetworkDiscovery] Stopped listening");
        }

        /// <summary>
        /// Clear discovered servers list
        /// </summary>
        public void ClearDiscoveredServers()
        {
            discoveredServers.Clear();
        }

        /// <summary>
        /// Get the local IP address
        /// </summary>
        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkDiscovery] Failed to get local IP: {e.Message}");
            }

            return "127.0.0.1";
        }
    }

    /// <summary>
    /// UnityEvent for ServerInfo
    /// </summary>
    [Serializable]
    public class ServerDiscoveredEvent : UnityEvent<ServerInfo> { }
}
