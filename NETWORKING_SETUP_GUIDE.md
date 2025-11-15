# Unity Client Messaging System - Setup Guide

Complete guide for setting up simple client-to-client communication in your Unity project.

## Table of Contents
- [Overview](#overview)
- [Quick Start (5 minutes)](#quick-start-5-minutes)
- [Components Reference](#components-reference)
- [Setting Up a Server](#setting-up-a-server)
- [Setting Up Clients](#setting-up-clients)
- [Network Discovery](#network-discovery)
- [Example Use Cases](#example-use-cases)
- [Troubleshooting](#troubleshooting)

---

## Overview

This networking system provides simple, reliable client-to-client communication through a relay server. It's perfect for:

- **Multiplayer games** - Position sync, game state, player actions
- **Chat systems** - Text communication between players
- **Collaborative tools** - Shared whiteboards, design tools
- **Testing/debugging** - Network communication testing

**Architecture:**
```
Client A  ←→  Server (Relay)  ←→  Client B
Client C  ←↗                  ↖←→  Client D
```

The server relays messages between clients - clients don't connect directly to each other.

---

## Quick Start (5 minutes)

### Step 1: Set Up the Server

1. Create a new GameObject in your scene: `GameObject > Create Empty`
2. Name it "NetworkServer"
3. Add the `SimpleServer` component
4. Configure settings:
   ```
   Server Port: 7777 (default)
   Max Clients: 10
   Auto Start: ✓ (checked)
   ```

**That's it for the server!** When you run this scene, it will start listening for connections.

### Step 2: Set Up a Client

1. Create another GameObject: `GameObject > Create Empty`
2. Name it "NetworkClient"
3. Add the `SimpleNetworkMessenger` component
4. Configure settings:
   ```
   Server Address: 127.0.0.1 (for local testing)
   Server Port: 7777
   Auto Connect: ✓ (checked)
   ```

### Step 3: Test the Connection

**Option A: Single Machine Testing**
1. Build your project (`File > Build Settings > Build`)
2. Run the built executable (this will be the server + client 1)
3. Run another instance from Unity Editor (client 2)
4. Both should connect to the server!

**Option B: Network Testing**
1. On Server PC: Set up server (Step 1), note the IP address
2. On Client PCs: Set up client (Step 2), set Server Address to server's IP
3. Run and connect!

---

## Components Reference

### SimpleServer

**Purpose:** Relay server that forwards messages between clients.

**Key Settings:**
- `Server Port` - Port to listen on (default: 7777)
- `Max Clients` - Maximum simultaneous connections
- `Auto Start` - Start listening on scene load

**Public Methods:**
```csharp
void StartServer()              // Start listening for connections
void StopServer()               // Stop server and disconnect all clients
List<string> GetConnectedClientIds()  // Get list of connected clients
```

**Inspector Status:**
- `Is Running` - Server active status
- `Connected Clients` - Current client count
- `Total Messages Relayed` - Message counter

---

### SimpleNetworkMessenger

**Purpose:** Client component for sending/receiving messages.

**Key Settings:**
- `Server Address` - IP address of server (e.g., "192.168.1.100")
- `Server Port` - Server port (default: 7777)
- `Auto Connect` - Connect automatically on Start
- `Client Id` - Unique identifier (auto-generated if empty)

**Public Methods:**
```csharp
void Connect()                           // Connect to server
void Disconnect()                        // Disconnect from server
void SendMessage(string type, string payload)  // Send message to all clients
void SendMessage<T>(string type, T data) // Send typed message
void SendPing()                          // Send ping test message
```

**Events:**
- `On Connected` - Fired when connected to server
- `On Disconnected` - Fired when disconnected
- `On Message Received` - Fired when message received from another client

**Example Usage:**
```csharp
public class MyScript : MonoBehaviour
{
    public SimpleNetworkMessenger messenger;

    void Start()
    {
        messenger.onMessageReceived.AddListener(OnMessage);
    }

    void OnMessage(NetworkMessage msg)
    {
        Debug.Log($"Received {msg.messageType} from {msg.senderId}");
    }

    void SendHello()
    {
        messenger.SendMessage("GREETING", "Hello World!");
    }
}
```

---

### NetworkDiscovery

**Purpose:** Find servers on local network automatically.

**Key Settings:**
- `Discovery Port` - UDP port for broadcasts (default: 7778)
- `Server Name` - Name to advertise (server mode)
- `Server Port` - Port to advertise (server mode)
- `Is Server Mode` - Enable to respond to discovery requests
- `Auto Start Server` - Start listening on scene load

**Public Methods:**
```csharp
void StartServer()                    // Listen for discovery requests (server mode)
void BroadcastDiscoveryRequest()      // Search for servers (client mode)
void Stop()                           // Stop listening
void ClearDiscoveredServers()         // Clear server list
List<ServerInfo> GetDiscoveredServers() // Get found servers
```

**Events:**
- `On Server Discovered` - Fired when a server is found

**Example: Auto-Find Servers**
```csharp
public class ServerFinder : MonoBehaviour
{
    public NetworkDiscovery discovery;
    public SimpleNetworkMessenger messenger;

    void Start()
    {
        discovery.onServerDiscovered.AddListener(OnServerFound);
        discovery.BroadcastDiscoveryRequest();
    }

    void OnServerFound(ServerInfo server)
    {
        Debug.Log($"Found server: {server.serverName} at {server.ipAddress}");

        // Auto-connect to first found server
        messenger.serverAddress = server.ipAddress;
        messenger.serverPort = server.port;
        messenger.Connect();
    }
}
```

---

## Setting Up a Server

### Basic Server Setup

```csharp
using UnityEngine;
using SimpleNetworking;

public class GameServer : MonoBehaviour
{
    public SimpleServer server;
    public NetworkDiscovery discovery;

    void Start()
    {
        // Start server
        server.StartServer();

        // Enable discovery (so clients can find us)
        discovery.isServerMode = true;
        discovery.serverName = "My Game Server";
        discovery.StartServer();

        Debug.Log("Server started!");
    }

    void OnApplicationQuit()
    {
        server.StopServer();
        discovery.Stop();
    }
}
```

### Dedicated Server Scene

For best results, create a dedicated server scene:

1. **Create new scene**: `File > New Scene`
2. **Add Server GameObject** with:
   - `SimpleServer` component
   - `NetworkDiscovery` component (server mode enabled)
3. **Add UI (optional)** to show:
   - Connected clients count
   - Messages relayed
   - Start/Stop buttons
4. **Build Settings**: Include this scene in build
5. **Server Build**: Build executable with this scene as default

**Headless Server (No Graphics):**
```bash
# Linux/Mac
./YourGame.x86_64 -batchmode -nographics

# Windows
YourGame.exe -batchmode -nographics
```

---

## Setting Up Clients

### Basic Client Setup

```csharp
using UnityEngine;
using SimpleNetworking;

public class GameClient : MonoBehaviour
{
    public SimpleNetworkMessenger messenger;

    void Start()
    {
        // Subscribe to events
        messenger.onConnected.AddListener(OnConnected);
        messenger.onDisconnected.AddListener(OnDisconnected);
        messenger.onMessageReceived.AddListener(OnMessageReceived);

        // Connect
        messenger.Connect();
    }

    void OnConnected()
    {
        Debug.Log("Connected to server!");
        // Send initial message
        messenger.SendMessage(MessageType.CONNECT, "Player joined");
    }

    void OnDisconnected()
    {
        Debug.Log("Disconnected from server");
    }

    void OnMessageReceived(NetworkMessage msg)
    {
        Debug.Log($"Received: {msg.messageType}");

        switch (msg.messageType)
        {
            case MessageType.CHAT:
                HandleChat(msg);
                break;
            case MessageType.POSITION_UPDATE:
                HandlePosition(msg);
                break;
        }
    }

    void HandleChat(NetworkMessage msg)
    {
        // Parse chat message
        ChatMessageData data = JsonUtility.FromJson<ChatMessageData>(msg.payload);
        Debug.Log($"{data.username}: {data.message}");
    }

    void HandlePosition(NetworkMessage msg)
    {
        // Handle position update
    }
}
```

---

## Network Discovery

### Finding Servers Automatically

Instead of manually entering IP addresses, use network discovery:

**Server Side:**
```csharp
public class MyServer : MonoBehaviour
{
    public NetworkDiscovery discovery;

    void Start()
    {
        // Enable server discovery
        discovery.isServerMode = true;
        discovery.serverName = "John's Game";
        discovery.serverPort = 7777;
        discovery.StartServer();
    }
}
```

**Client Side:**
```csharp
public class MyClient : MonoBehaviour
{
    public NetworkDiscovery discovery;
    public SimpleNetworkMessenger messenger;

    void Start()
    {
        // Listen for servers
        discovery.onServerDiscovered.AddListener(OnServerFound);
        discovery.BroadcastDiscoveryRequest();
    }

    void OnServerFound(ServerInfo server)
    {
        Debug.Log($"Found: {server.serverName} at {server.ipAddress}:{server.port}");

        // Connect to server
        messenger.serverAddress = server.ipAddress;
        messenger.serverPort = server.port;
        messenger.Connect();
    }

    // UI Button to refresh server list
    public void SearchForServers()
    {
        discovery.ClearDiscoveredServers();
        discovery.BroadcastDiscoveryRequest();
    }
}
```

---

## Example Use Cases

### 1. Simple Chat System

See `Assets/Scripts/Networking/Examples/ChatExample.cs`

**Setup:**
1. Add `SimpleNetworkMessenger` to a GameObject
2. Add `ChatExample` component to same GameObject
3. Connect UI elements (InputField, Text, Button)
4. Run and chat!

**Sending Messages:**
```csharp
chatExample.SendChatMessage("Hello everyone!");
```

### 2. Position Synchronization

See `Assets/Scripts/Networking/Examples/PositionSyncExample.cs`

**Setup:**
1. Add `SimpleNetworkMessenger` to player GameObject
2. Add `PositionSyncExample` component
3. Assign remote player prefab
4. Configure update rate and interpolation

**The component automatically:**
- Sends position updates at configured rate
- Creates remote player GameObjects for other clients
- Smoothly interpolates remote positions

### 3. Custom Message Types

**Define your message:**
```csharp
[System.Serializable]
public class PlayerActionData
{
    public string action;
    public float value;
}
```

**Send it:**
```csharp
PlayerActionData data = new PlayerActionData
{
    action = "jump",
    value = 5.0f
};

messenger.SendMessage("PLAYER_ACTION", data);
```

**Receive it:**
```csharp
void OnMessageReceived(NetworkMessage msg)
{
    if (msg.messageType == "PLAYER_ACTION")
    {
        PlayerActionData data = JsonUtility.FromJson<PlayerActionData>(msg.payload);
        Debug.Log($"Player did {data.action} with value {data.value}");
    }
}
```

---

## Troubleshooting

### Connection Issues

**Problem:** Client can't connect to server

**Solutions:**
1. **Check IP address**: Use `ipconfig` (Windows) or `ifconfig` (Mac/Linux) to get server IP
2. **Check port**: Ensure server port matches client port
3. **Firewall**: Add exception for Unity and your game executable
4. **Same network**: Ensure all devices on same local network
5. **Server running**: Verify server actually started (check console)

### Firewall Configuration

**Windows:**
```
Control Panel > System and Security > Windows Defender Firewall >
Advanced Settings > Inbound Rules > New Rule > Port > TCP > 7777
```

**Mac:**
```
System Preferences > Security & Privacy > Firewall > Firewall Options >
Add your Unity app
```

**Linux:**
```bash
sudo ufw allow 7777/tcp
sudo ufw allow 7778/udp
```

### Discovery Not Working

**Problem:** Clients can't find server via discovery

**Solutions:**
1. **UDP port**: Discovery uses port 7778 by default
2. **Broadcast permissions**: Some networks block UDP broadcasts
3. **Firewall**: Allow UDP on discovery port
4. **Network type**: Some corporate/school networks block broadcasts

**Alternative:** Use manual IP entry as fallback

### Messages Not Received

**Problem:** Messages sent but not received

**Solutions:**
1. **Check event subscription**: Ensure `onMessageReceived` listener added
2. **Message type**: Verify sender/receiver using same message type string
3. **JSON serialization**: Ensure data class is marked `[Serializable]`
4. **Client ID**: Messages are not echoed back to sender

### Performance Issues

**Problem:** Lag or poor performance

**Solutions:**
1. **Update rate**: Reduce position sync frequency (try 10-20 updates/sec)
2. **Message size**: Keep messages small, avoid sending large data
3. **Interpolation**: Use interpolation for smooth movement
4. **Compression**: Reduce precision of floats (round to 2 decimals)

**Example optimization:**
```csharp
// Instead of:
float x = 123.456789f;

// Use:
float x = Mathf.Round(123.456789f * 100f) / 100f;  // 123.46
```

---

## Network Testing Checklist

- [ ] Server starts without errors
- [ ] Client connects successfully
- [ ] Messages send and receive
- [ ] Multiple clients can connect
- [ ] Disconnection handled gracefully
- [ ] Discovery finds servers (if using)
- [ ] Firewall configured
- [ ] Works across network (not just localhost)
- [ ] Performance acceptable
- [ ] UI updates correctly

---

## Next Steps

1. **Read the README**: Full API reference at `Assets/Scripts/Networking/README.md`
2. **Study examples**: Check `ChatExample.cs` and `PositionSyncExample.cs`
3. **Build your feature**: Use the examples as templates
4. **Test thoroughly**: Use the deployment guide to test on multiple machines
5. **Deploy**: See `ROBOCOPY_DEPLOYMENT_GUIDE.md` for network deployment

---

## Getting Help

- Check the full README for API details
- Review example scripts in `Assets/Scripts/Networking/Examples/`
- Common issues covered in Troubleshooting section above
- Unity networking documentation: https://docs.unity3d.com/Manual/net-about.html

Good luck with your networked Unity project! 🎮
