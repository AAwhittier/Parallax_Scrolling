# Simple Unity Client Messaging System

A lightweight, modular networking system for Unity that enables simple client-to-client communication through a relay server.

## Features

✅ **Simple to Use** - Add components, configure, and start communicating
✅ **Modular Design** - Use only what you need
✅ **No External Dependencies** - Pure C# with Unity's built-in libraries
✅ **Network Discovery** - Automatically find servers on LAN
✅ **Reliable** - TCP-based messaging with automatic reconnection
✅ **Flexible** - Send any serializable data
✅ **Thread-Safe** - Background networking with main-thread callbacks
✅ **Well-Documented** - Comprehensive guides and examples

## Quick Start

### 1. Server Setup (30 seconds)

```csharp
// Add to a GameObject in your scene
using SimpleNetworking;

public class MyServer : MonoBehaviour
{
    void Start()
    {
        // Add SimpleServer component
        var server = gameObject.AddComponent<SimpleServer>();
        server.serverPort = 7777;
        server.StartServer();
    }
}
```

### 2. Client Setup (30 seconds)

```csharp
// Add to another GameObject
using SimpleNetworking;

public class MyClient : MonoBehaviour
{
    SimpleNetworkMessenger messenger;

    void Start()
    {
        messenger = gameObject.AddComponent<SimpleNetworkMessenger>();
        messenger.serverAddress = "127.0.0.1"; // localhost for testing
        messenger.serverPort = 7777;
        messenger.onMessageReceived.AddListener(OnMessage);
        messenger.Connect();
    }

    void OnMessage(NetworkMessage msg)
    {
        Debug.Log($"Received: {msg.messageType} from {msg.senderId}");
    }

    void SendHello()
    {
        messenger.SendMessage("GREETING", "Hello, world!");
    }
}
```

**That's it!** You now have client-to-client messaging working.

## Architecture

```
┌──────────┐         ┌──────────┐         ┌──────────┐
│ Client A │ ←─────→ │  Server  │ ←─────→ │ Client B │
└──────────┘         │ (Relay)  │         └──────────┘
                     └────┬─────┘
                          │
                     ┌────┴─────┐
                     │ Client C │
                     └──────────┘
```

**How it works:**
1. Server listens for client connections
2. Clients connect to server
3. When a client sends a message, server relays it to all other clients
4. Clients receive messages in real-time

## Components

### SimpleServer

**Purpose:** Acts as a relay server for client messages.

**Location:** `Assets/Scripts/Networking/SimpleServer.cs`

**Key Features:**
- Handles multiple simultaneous client connections
- Broadcasts messages to all connected clients (except sender)
- Thread-safe client management
- Automatic cleanup on disconnect

**Properties:**
```csharp
int serverPort               // Port to listen on (default: 7777)
int maxClients              // Maximum simultaneous connections (default: 10)
bool autoStart              // Start server on scene load
bool IsRunning              // Is server currently running
int ConnectedClients        // Current number of connected clients
int TotalMessagesRelayed    // Total messages relayed
```

**Methods:**
```csharp
void StartServer()                    // Start listening for connections
void StopServer()                     // Stop server and disconnect all clients
List<string> GetConnectedClientIds()  // Get list of connected client IDs
```

**Example:**
```csharp
SimpleServer server = GetComponent<SimpleServer>();
server.serverPort = 7777;
server.maxClients = 20;
server.StartServer();

// Later...
Debug.Log($"Connected clients: {server.ConnectedClients}");
```

---

### SimpleNetworkMessenger

**Purpose:** Client component for sending and receiving messages.

**Location:** `Assets/Scripts/Networking/SimpleNetworkMessenger.cs`

**Key Features:**
- TCP-based reliable messaging
- Automatic message queuing
- Thread-safe message handling
- UnityEvents for connection status and messages

**Properties:**
```csharp
string serverAddress        // Server IP address (e.g., "192.168.1.100")
int serverPort             // Server port (default: 7777)
bool autoConnect           // Connect automatically on Start
string clientId            // Unique client identifier (auto-generated)
bool IsConnected           // Current connection status
int MessagesSent           // Total messages sent
int MessagesReceived       // Total messages received
```

**Methods:**
```csharp
void Connect()                              // Connect to server
void Disconnect()                           // Disconnect from server
void SendMessage(string type, string data)  // Send message with payload
void SendMessage<T>(string type, T data)    // Send typed message
void SendPing()                             // Send ping message
```

**Events:**
```csharp
UnityEvent onConnected           // Fired when connected to server
UnityEvent onDisconnected        // Fired when disconnected
MessageEvent onMessageReceived   // Fired when message received
```

**Example:**
```csharp
SimpleNetworkMessenger messenger = GetComponent<SimpleNetworkMessenger>();

// Subscribe to events
messenger.onConnected.AddListener(() => Debug.Log("Connected!"));
messenger.onMessageReceived.AddListener(OnMessage);

// Connect
messenger.serverAddress = "192.168.1.100";
messenger.Connect();

// Send messages
messenger.SendMessage("CHAT", "Hello everyone!");

// Send typed data
PlayerData data = new PlayerData { name = "Alice", score = 100 };
messenger.SendMessage("PLAYER_DATA", data);
```

---

### NetworkDiscovery

**Purpose:** Discover servers on the local network using UDP broadcasts.

**Location:** `Assets/Scripts/Networking/NetworkDiscovery.cs`

**Key Features:**
- Automatic server discovery on LAN
- UDP broadcast-based discovery
- Can operate as server (respond to requests) or client (search for servers)
- Non-blocking discovery

**Properties:**
```csharp
int discoveryPort         // UDP port for discovery (default: 7778)
string serverName         // Server name to advertise (server mode)
int serverPort           // Server port to advertise (server mode)
bool isServerMode        // Enable server mode (respond to requests)
bool autoStartServer     // Auto-start in server mode
bool IsListening         // Current listening status
List<ServerInfo> DiscoveredServers  // List of discovered servers
```

**Methods:**
```csharp
void StartServer()                  // Start listening for discovery requests (server mode)
void BroadcastDiscoveryRequest()    // Broadcast discovery request (client mode)
void Stop()                         // Stop listening
void ClearDiscoveredServers()       // Clear discovered servers list
List<ServerInfo> GetDiscoveredServers()  // Get list of found servers
```

**Events:**
```csharp
ServerDiscoveredEvent onServerDiscovered  // Fired when server found
```

**Example (Server Mode):**
```csharp
NetworkDiscovery discovery = GetComponent<NetworkDiscovery>();
discovery.isServerMode = true;
discovery.serverName = "My Game Server";
discovery.serverPort = 7777;
discovery.StartServer();
// Now responds to discovery requests from clients
```

**Example (Client Mode):**
```csharp
NetworkDiscovery discovery = GetComponent<NetworkDiscovery>();
SimpleNetworkMessenger messenger = GetComponent<SimpleNetworkMessenger>();

// Listen for server discoveries
discovery.onServerDiscovered.AddListener(server => {
    Debug.Log($"Found: {server.serverName} at {server.ipAddress}:{server.port}");

    // Auto-connect to first server
    messenger.serverAddress = server.ipAddress;
    messenger.serverPort = server.port;
    messenger.Connect();
});

// Start searching
discovery.BroadcastDiscoveryRequest();
```

---

### NetworkMessage

**Purpose:** Base message class for network transmission.

**Location:** `Assets/Scripts/Networking/Core/NetworkMessage.cs`

**Properties:**
```csharp
string senderId      // Unique identifier of sender
string messageType   // Type of message (e.g., "CHAT", "POSITION_UPDATE")
long timestamp       // UTC timestamp when message was created
string payload       // JSON-serialized message data
```

**Methods:**
```csharp
string ToJson()                        // Serialize message to JSON
NetworkMessage FromJson(string json)   // Deserialize from JSON
float GetAgeSeconds()                  // Get message age in seconds
```

**Example:**
```csharp
// Creating a message
NetworkMessage msg = new NetworkMessage("client_123", "CHAT", "Hello!");

// Serializing
string json = msg.ToJson();

// Deserializing
NetworkMessage received = NetworkMessage.FromJson(json);
Debug.Log($"From {received.senderId}: {received.payload}");
```

---

### MessageType Constants

**Purpose:** Predefined message type constants.

**Location:** `Assets/Scripts/Networking/Core/MessageType.cs`

**System Messages:**
```csharp
MessageType.CONNECT         // Client connected
MessageType.DISCONNECT      // Client disconnected
MessageType.PING           // Ping message
MessageType.PONG           // Pong response
MessageType.HEARTBEAT      // Heartbeat/keep-alive
```

**Discovery Messages:**
```csharp
MessageType.DISCOVER_REQUEST   // Discovery request
MessageType.DISCOVER_RESPONSE  // Discovery response
```

**Communication Messages:**
```csharp
MessageType.CHAT              // Chat message
MessageType.WHISPER           // Private message
```

**Game State Messages:**
```csharp
MessageType.POSITION_UPDATE   // Position update
MessageType.ROTATION_UPDATE   // Rotation update
MessageType.TRANSFORM_UPDATE  // Full transform update
MessageType.STATE_UPDATE      // Game state update
```

**Custom Messages:**
```csharp
MessageType.COMMAND           // Command message
MessageType.EVENT            // Event message
MessageType.CUSTOM           // Custom message type
```

**Example:**
```csharp
// Using predefined types
messenger.SendMessage(MessageType.CHAT, "Hello!");
messenger.SendMessage(MessageType.POSITION_UPDATE, posData);

// Defining custom types
const string MY_CUSTOM_TYPE = "PLAYER_JUMP";
messenger.SendMessage(MY_CUSTOM_TYPE, jumpData);
```

---

## Examples

### Chat System

**Full example in:** `Assets/Scripts/Networking/Examples/ChatExample.cs`

```csharp
using SimpleNetworking;
using SimpleNetworking.Examples;

public class Chat : MonoBehaviour
{
    SimpleNetworkMessenger messenger;

    void Start()
    {
        messenger = GetComponent<SimpleNetworkMessenger>();
        messenger.onMessageReceived.AddListener(OnMessage);
        messenger.Connect();
    }

    public void SendChat(string message)
    {
        ChatMessageData data = new ChatMessageData {
            username = "Player1",
            message = message
        };
        messenger.SendMessage(MessageType.CHAT, data);
    }

    void OnMessage(NetworkMessage msg)
    {
        if (msg.messageType == MessageType.CHAT)
        {
            ChatMessageData data = JsonUtility.FromJson<ChatMessageData>(msg.payload);
            Debug.Log($"{data.username}: {data.message}");
        }
    }
}
```

---

### Position Synchronization

**Full example in:** `Assets/Scripts/Networking/Examples/PositionSyncExample.cs`

```csharp
using SimpleNetworking;
using SimpleNetworking.Examples;

public class PositionSync : MonoBehaviour
{
    SimpleNetworkMessenger messenger;
    float updateRate = 10f; // Updates per second
    float timer = 0f;

    void Start()
    {
        messenger = GetComponent<SimpleNetworkMessenger>();
        messenger.onMessageReceived.AddListener(OnMessage);
        messenger.Connect();
    }

    void Update()
    {
        // Send position updates
        timer += Time.deltaTime;
        if (timer >= 1f / updateRate)
        {
            TransformData data = new TransformData(transform.position, transform.rotation);
            messenger.SendMessage(MessageType.TRANSFORM_UPDATE, data);
            timer = 0f;
        }
    }

    void OnMessage(NetworkMessage msg)
    {
        if (msg.messageType == MessageType.TRANSFORM_UPDATE)
        {
            TransformData data = JsonUtility.FromJson<TransformData>(msg.payload);
            // Update remote player position
            UpdateRemotePlayer(msg.senderId, data.GetPosition(), data.GetRotation());
        }
    }
}
```

---

### Custom Message Types

```csharp
// Define your data class
[System.Serializable]
public class PlayerActionData
{
    public string playerName;
    public string action;
    public float value;
    public Vector3 position;
}

// Send custom message
void PlayerJumped()
{
    PlayerActionData data = new PlayerActionData {
        playerName = "Alice",
        action = "jump",
        value = 5.0f,
        position = transform.position
    };

    messenger.SendMessage("PLAYER_ACTION", data);
}

// Receive custom message
void OnMessage(NetworkMessage msg)
{
    if (msg.messageType == "PLAYER_ACTION")
    {
        PlayerActionData data = JsonUtility.FromJson<PlayerActionData>(msg.payload);
        Debug.Log($"{data.playerName} did {data.action} at {data.position}");
    }
}
```

---

## Setup Guides

### Complete Setup Tutorial
See **[NETWORKING_SETUP_GUIDE.md](../../../NETWORKING_SETUP_GUIDE.md)** for step-by-step instructions including:
- Server setup
- Client setup
- Network discovery
- Troubleshooting
- Testing workflows

### Deployment Guide
See **[ROBOCOPY_DEPLOYMENT_GUIDE.md](../../../ROBOCOPY_DEPLOYMENT_GUIDE.md)** for deploying builds to test PCs:
- Robocopy basics
- Automated deployment scripts
- Network distribution
- Testing workflows

---

## Best Practices

### 1. Message Size

Keep messages small for better performance:

```csharp
// ✅ Good - Small, focused data
[Serializable]
public class PositionData {
    public float x, y, z;
}

// ❌ Avoid - Large, unnecessary data
[Serializable]
public class EverythingData {
    public string playerName;
    public Vector3[] allPositions; // 1000 elements
    public Texture2D texture; // Can't serialize anyway
}
```

### 2. Update Rates

Don't send too frequently:

```csharp
// ✅ Good - 10-20 updates/second
public float updateRate = 10f;

// ❌ Avoid - Every frame (60+ updates/second)
void Update() {
    SendPosition(); // Too frequent!
}
```

### 3. Message Types

Use constants for message types:

```csharp
// ✅ Good - Use constants
messenger.SendMessage(MessageType.CHAT, data);

// ❌ Avoid - String literals (typo-prone)
messenger.SendMessage("CHATT", data); // Typo!
```

### 4. Error Handling

Always check connection status:

```csharp
// ✅ Good - Check before sending
if (messenger.IsConnected)
{
    messenger.SendMessage(MessageType.CHAT, "Hello");
}

// ❌ Avoid - Sending without checking
messenger.SendMessage(MessageType.CHAT, "Hello"); // May fail if disconnected
```

### 5. Cleanup

Unsubscribe from events when done:

```csharp
void OnDestroy()
{
    if (messenger != null)
    {
        messenger.onMessageReceived.RemoveListener(OnMessage);
        messenger.Disconnect();
    }
}
```

---

## Performance Considerations

### Network Bandwidth

**Typical message sizes:**
- Small message (position): ~100 bytes
- Medium message (chat): ~200 bytes
- Large message (state): ~1KB

**Bandwidth calculation:**
```
10 clients × 10 updates/sec × 100 bytes = 10 KB/sec
```

This is minimal bandwidth - system handles 100+ clients easily on LAN.

### CPU Usage

- **Server**: Minimal CPU usage, mostly I/O bound
- **Client**: Negligible impact, messages processed on background thread

### Memory

- **Per client**: ~50KB overhead
- **Messages**: Queued on main thread, processed immediately

---

## Limitations

### What This System IS

✅ Simple client-to-client messaging
✅ Chat systems, basic multiplayer
✅ Position synchronization
✅ Game state updates
✅ LAN/local network communication

### What This System IS NOT

❌ Full game networking solution (use Unity Netcode, Mirror, or Photon)
❌ Authoritative server with physics
❌ Anti-cheat or security-focused
❌ Internet/WAN communication (no NAT punch-through)
❌ Voice/video streaming

**For production multiplayer games**, consider:
- **Unity Netcode for GameObjects** - Official Unity solution
- **Mirror** - Popular open-source networking
- **Photon Unity Networking (PUN)** - Cloud-based solution

This system is perfect for **prototypes, testing, local multiplayer, and learning**.

---

## Troubleshooting

### Can't Connect

**Check:**
1. Server is running (`IsRunning = true`)
2. Correct IP address (use `ipconfig` or `ifconfig`)
3. Correct port (must match server)
4. Firewall allows connections
5. On same network

### Messages Not Received

**Check:**
1. Event listener subscribed (`onMessageReceived.AddListener`)
2. Message type matches (case-sensitive)
3. Data is serializable (`[Serializable]` attribute)
4. Not filtering own messages (messages from same `clientId` ignored)

### Discovery Not Working

**Check:**
1. UDP port 7778 allowed in firewall
2. Network allows UDP broadcasts (some don't)
3. Server has discovery enabled (`isServerMode = true`)
4. Using correct discovery port

---

## API Reference

### Complete API

**SimpleServer:**
```csharp
// Properties
int serverPort
int maxClients
bool autoStart
bool IsRunning (readonly)
int ConnectedClients (readonly)
int TotalMessagesRelayed (readonly)

// Methods
void StartServer()
void StopServer()
List<string> GetConnectedClientIds()
```

**SimpleNetworkMessenger:**
```csharp
// Properties
string serverAddress
int serverPort
bool autoConnect
string clientId
bool IsConnected (readonly)
int MessagesSent (readonly)
int MessagesReceived (readonly)

// Methods
void Connect()
void Disconnect()
void SendMessage(string messageType, string payload)
void SendMessage<T>(string messageType, T data)
void SendPing()

// Events
UnityEvent onConnected
UnityEvent onDisconnected
MessageEvent onMessageReceived
```

**NetworkDiscovery:**
```csharp
// Properties
int discoveryPort
string serverName
int serverPort
bool isServerMode
bool autoStartServer
bool IsListening (readonly)
List<ServerInfo> DiscoveredServers (readonly)

// Methods
void StartServer()
void BroadcastDiscoveryRequest()
void Stop()
void ClearDiscoveredServers()
List<ServerInfo> GetDiscoveredServers()

// Events
ServerDiscoveredEvent onServerDiscovered
```

---

## License

This networking system is part of the Unity project and free to use and modify.

---

## Support

For questions or issues:
1. Check the **[Setup Guide](../../../NETWORKING_SETUP_GUIDE.md)**
2. Review the **Example Scripts** in `Examples/`
3. Read the **Troubleshooting** section above

---

## Version History

**v1.0.0** - Initial release
- Simple server/client messaging
- Network discovery
- Example scripts
- Complete documentation

---

Built with ❤️ for Unity developers
