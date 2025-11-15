# Castle Crashers-Style Networking Architecture

Complete guide for implementing authoritative server networking for 8-player beat 'em up games.

## Table of Contents
- [Castle Crashers Analysis](#castle-crashers-analysis)
- [Networking Requirements](#networking-requirements)
- [Architecture Overview](#architecture-overview)
- [Data Flow](#data-flow)
- [Implementation Guide](#implementation-guide)
- [Performance Optimization](#performance-optimization)

---

## Castle Crashers Analysis

### Game Type: 2D Beat 'Em Up

**Core Features:**
- **8 simultaneous players** (4 in original, scaling to 8)
- **Side-scrolling combat** - Players move and attack together
- **Enemy waves** - Dozens of enemies on screen
- **Combos and attacks** - Fast-paced combat with special moves
- **Projectiles** - Arrows, magic, thrown weapons
- **Level progression** - Players advance through stages together
- **Item pickups** - Health, weapons, powerups

### Critical Networking Challenges

1. **Player Synchronization** (8 players)
   - Position, velocity, facing direction
   - Current animation state
   - Health, mana, combo count
   - ~10-20 updates/second per player

2. **Enemy Synchronization** (20-50 enemies)
   - AI-controlled, server authoritative
   - Position and animation
   - Health and state
   - ~5-10 updates/second per enemy

3. **Combat Events**
   - Attack hits (who hit whom)
   - Damage calculation
   - Knockback and status effects
   - Must be instant and reliable

4. **Projectiles** (10-30 active)
   - Short-lived objects
   - Collision detection
   - Can be client-predicted

5. **Game State**
   - Current level/wave
   - Score and progression
   - Boss phases

---

## Networking Requirements

### Why Simple Relay Won't Work

The simple relay server is **peer-to-peer style** where:
- ❌ No authority - any client can claim anything
- ❌ No validation - cheating is trivial
- ❌ Inconsistent state - clients see different game states
- ❌ No AI - enemies can't be controlled
- ❌ Poor scaling - 8 players = 56 connections worth of data

### What We Need: Authoritative Server

**Server Authority:**
- ✅ Server is source of truth
- ✅ Server runs game logic and AI
- ✅ Server validates all actions
- ✅ Clients send inputs, receive state
- ✅ Prevents cheating
- ✅ Consistent game state for all

**Architecture Change:**
```
SIMPLE RELAY:
Client A → Server → Client B (just forwards messages)
Problem: Who decides if attack hit?

AUTHORITATIVE:
Client A → Input → Server (runs game logic) → State → Client B
Solution: Server decides everything
```

---

## Architecture Overview

### High-Level Design

```
┌─────────────────────────────────────────────────────┐
│                  GAME SERVER                        │
│  ┌───────────────────────────────────────────────┐ │
│  │         Game State (Source of Truth)          │ │
│  │  - 8 Players (pos, health, animation)         │ │
│  │  - 30 Enemies (AI, pos, health)               │ │
│  │  - 15 Projectiles (pos, velocity)             │ │
│  │  - Level state (wave, boss phase)             │ │
│  └───────────────────────────────────────────────┘ │
│                      ↓                              │
│  ┌───────────────────────────────────────────────┐ │
│  │           Game Logic (60 FPS)                 │ │
│  │  - Process player inputs                      │ │
│  │  - Run enemy AI                               │ │
│  │  - Detect collisions                          │ │
│  │  - Apply damage/effects                       │ │
│  └───────────────────────────────────────────────┘ │
│                      ↓                              │
│  ┌───────────────────────────────────────────────┐ │
│  │      Snapshot System (20 FPS)                 │ │
│  │  - Create state snapshot                      │ │
│  │  - Prioritize important updates               │ │
│  │  - Delta compress data                        │ │
│  │  - Send to all clients                        │ │
│  └───────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────┘
           ↓          ↓          ↓          ↓
      Client 1   Client 2   Client 3 ... Client 8

Each Client:
  ┌──────────────────────────────────────┐
  │  Input → Send to Server (60 FPS)    │
  │  Receive State ← Apply (20 FPS)     │
  │  Client Prediction (immediate)       │
  │  Entity Interpolation (smooth)       │
  └──────────────────────────────────────┘
```

### Key Components

**Server Side:**
1. **AuthoritativeGameServer** - Main server logic
2. **GameState** - Current game state (players, enemies, etc.)
3. **EntityManager** - Manages all game entities
4. **SnapshotSystem** - Creates and sends state updates
5. **InputProcessor** - Processes client inputs
6. **CombatSystem** - Handles attacks and damage
7. **AIController** - Controls enemy behavior

**Client Side:**
1. **GameClient** - Connects to server, sends inputs
2. **ClientPrediction** - Predicts local player movement
3. **EntityInterpolator** - Smooths remote entity movement
4. **StateReconciliation** - Corrects prediction errors
5. **LocalPlayer** - Player character controller
6. **RemoteEntity** - Representation of server entities

---

## Data Flow

### Client → Server (Input)

**Sent 60 times/second (every frame):**
```json
{
  "type": "INPUT",
  "sequenceId": 12345,
  "timestamp": 1234567890,
  "inputs": {
    "moveX": 0.8,        // -1 to 1
    "moveY": 0.0,
    "jump": true,
    "attack": false,
    "special": false
  }
}
```

**Size:** ~50 bytes per input
**Bandwidth:** 50 bytes × 60/sec = 3 KB/sec per client

### Server → Client (Snapshot)

**Sent 20 times/second:**
```json
{
  "type": "SNAPSHOT",
  "serverTick": 6789,
  "timestamp": 1234567890,

  "players": [
    {
      "id": "player_1",
      "x": 123.5, "y": 45.2,
      "vx": 2.3, "vy": 0.0,
      "facing": 1,
      "anim": "attack_1",
      "health": 85,
      "combo": 3
    },
    // ... 7 more players
  ],

  "enemies": [
    {
      "id": "enemy_12",
      "type": "grunt",
      "x": 145.2, "y": 45.0,
      "anim": "walk",
      "health": 20
    },
    // ... 20-30 more enemies
  ],

  "events": [
    {
      "type": "DAMAGE",
      "attacker": "player_1",
      "target": "enemy_12",
      "damage": 15,
      "knockback": {"x": 5, "y": 2}
    }
  ]
}
```

**Size:** ~800-1500 bytes per snapshot (with compression)
**Bandwidth:** 1.5 KB × 20/sec = 30 KB/sec per client

**Total per client:** ~33 KB/sec (264 Kbps)
**For 8 clients:** ~264 KB/sec server upload (2.1 Mbps)

✅ **Easily handled by modern networks!**

---

## Implementation Guide

### 1. Entity System

All game objects (players, enemies, projectiles) are **entities**.

**Base Entity:**
```csharp
public abstract class NetworkEntity
{
    public string entityId;
    public EntityType type;
    public Vector2 position;
    public Vector2 velocity;
    public bool isAlive;

    public abstract void ServerUpdate(float deltaTime);
    public abstract EntitySnapshot CreateSnapshot();
}
```

**Player Entity:**
```csharp
public class PlayerEntity : NetworkEntity
{
    public string playerId;
    public int health;
    public int maxHealth;
    public float moveSpeed;
    public AnimationState currentAnim;
    public int comboCount;
    public bool isAttacking;

    // Input buffer
    private Queue<PlayerInput> inputBuffer;

    public override void ServerUpdate(float deltaTime)
    {
        // Process inputs
        while (inputBuffer.Count > 0)
        {
            var input = inputBuffer.Dequeue();
            ProcessInput(input, deltaTime);
        }

        // Apply physics
        position += velocity * deltaTime;

        // Update animation
        UpdateAnimation(deltaTime);
    }
}
```

**Enemy Entity:**
```csharp
public class EnemyEntity : NetworkEntity
{
    public EnemyType enemyType;
    public int health;
    public AIState currentState;
    public PlayerEntity target;

    public override void ServerUpdate(float deltaTime)
    {
        // AI logic
        switch (currentState)
        {
            case AIState.Idle:
                FindTarget();
                break;
            case AIState.Approach:
                MoveTowardsTarget(deltaTime);
                break;
            case AIState.Attack:
                ExecuteAttack(deltaTime);
                break;
        }
    }
}
```

### 2. Snapshot System

**Optimizations:**
- **Delta compression:** Only send changed data
- **Priority:** Send important entities first
- **Culling:** Don't send entities far from players
- **Quantization:** Reduce precision (e.g., 0.1 unit precision)

```csharp
public class SnapshotSystem
{
    private GameState lastSentState;

    public Snapshot CreateSnapshot(GameState currentState, string forClientId)
    {
        Snapshot snapshot = new Snapshot();
        snapshot.serverTick = currentState.tick;

        // Player who owns this client (full precision)
        var ownPlayer = GetPlayer(forClientId);
        snapshot.ownPlayer = CreateFullPlayerSnapshot(ownPlayer);

        // Other players (reduced precision)
        snapshot.otherPlayers = currentState.players
            .Where(p => p.playerId != forClientId)
            .Select(p => CreateReducedPlayerSnapshot(p))
            .ToList();

        // Enemies near this player (culled by distance)
        snapshot.enemies = currentState.enemies
            .Where(e => Vector2.Distance(e.position, ownPlayer.position) < 50f)
            .OrderBy(e => Vector2.Distance(e.position, ownPlayer.position))
            .Take(30) // Max 30 enemies
            .Select(e => CreateEnemySnapshot(e))
            .ToList();

        // Recent events
        snapshot.events = currentState.GetEventsSince(lastSentState?.tick ?? 0);

        return snapshot;
    }
}
```

### 3. Client Prediction

**Problem:** Waiting for server = 50ms+ lag feels terrible

**Solution:** Client predicts own movement immediately

```csharp
public class ClientPrediction
{
    private LocalPlayer localPlayer;
    private List<PredictedInput> pendingInputs = new List<PredictedInput>();

    public void ProcessLocalInput(PlayerInput input)
    {
        // Store for reconciliation
        pendingInputs.Add(new PredictedInput {
            sequenceId = input.sequenceId,
            input = input,
            resultPosition = localPlayer.position
        });

        // Apply immediately (predict)
        localPlayer.ApplyInput(input);

        // Send to server
        SendInputToServer(input);
    }

    public void OnServerSnapshot(PlayerSnapshot serverState)
    {
        // Find last acknowledged input
        int lastAcked = serverState.lastProcessedInput;

        // Remove acknowledged inputs
        pendingInputs.RemoveAll(i => i.sequenceId <= lastAcked);

        // Check if prediction was wrong
        if (Vector2.Distance(serverState.position, localPlayer.position) > 0.5f)
        {
            // Server correction - rewind and replay
            localPlayer.position = serverState.position;
            localPlayer.velocity = serverState.velocity;

            // Replay pending inputs
            foreach (var predicted in pendingInputs)
            {
                localPlayer.ApplyInput(predicted.input);
            }
        }
    }
}
```

### 4. Entity Interpolation

**For remote players and enemies:**

```csharp
public class EntityInterpolator
{
    private Vector2 fromPosition;
    private Vector2 toPosition;
    private float interpolationTime;
    private float timeSinceLastSnapshot;

    public void OnSnapshot(Vector2 newPosition)
    {
        fromPosition = currentPosition;
        toPosition = newPosition;
        timeSinceLastSnapshot = 0;
        interpolationTime = 1f / 20f; // 20 snapshots/sec
    }

    public void Update(float deltaTime)
    {
        timeSinceLastSnapshot += deltaTime;
        float t = timeSinceLastSnapshot / interpolationTime;

        currentPosition = Vector2.Lerp(fromPosition, toPosition, t);
    }
}
```

### 5. Combat System

**Server-authoritative damage:**

```csharp
public class ServerCombatSystem
{
    public void ProcessAttack(PlayerEntity attacker)
    {
        // Get attack hitbox
        Rect hitbox = attacker.GetAttackHitbox();

        // Check all enemies
        foreach (var enemy in gameState.enemies)
        {
            if (hitbox.Overlaps(enemy.GetHitbox()))
            {
                // Calculate damage
                int damage = CalculateDamage(attacker, enemy);

                // Apply damage
                enemy.health -= damage;

                // Apply knockback
                Vector2 knockback = CalculateKnockback(attacker, enemy);
                enemy.velocity += knockback;

                // Create event
                var damageEvent = new DamageEvent {
                    attacker = attacker.playerId,
                    target = enemy.entityId,
                    damage = damage,
                    knockback = knockback
                };

                gameState.AddEvent(damageEvent);

                // Check death
                if (enemy.health <= 0)
                {
                    enemy.isAlive = false;
                    gameState.AddEvent(new DeathEvent { entityId = enemy.entityId });
                }
            }
        }
    }
}
```

---

## Performance Optimization

### For 8 Players

**Challenges:**
- 8× input processing
- 8× snapshot generation (customized per client)
- Dozens of enemies with AI

**Solutions:**

**1. Fixed Timestep Server Loop**
```csharp
const float TICK_RATE = 60f;
const float TICK_DURATION = 1f / TICK_RATE;

void ServerLoop()
{
    float accumulator = 0f;

    while (running)
    {
        float frameStart = Time.realtimeSinceStartup;

        accumulator += Time.deltaTime;

        // Fixed update
        while (accumulator >= TICK_DURATION)
        {
            ProcessInputs();
            UpdateGameState(TICK_DURATION);
            accumulator -= TICK_DURATION;
            serverTick++;
        }

        // Send snapshots (every 3 ticks = 20/sec)
        if (serverTick % 3 == 0)
        {
            SendSnapshots();
        }

        // Maintain 60 FPS
        float elapsed = Time.realtimeSinceStartup - frameStart;
        if (elapsed < TICK_DURATION)
        {
            Thread.Sleep((int)((TICK_DURATION - elapsed) * 1000));
        }
    }
}
```

**2. Spatial Partitioning**

Don't check all enemies vs all players:

```csharp
// Divide world into grid
SpatialGrid grid = new SpatialGrid(cellSize: 10f);

// Update entity positions
grid.Update(players);
grid.Update(enemies);

// Fast collision queries
var nearbyEnemies = grid.GetEntitiesNear(player.position, radius: 5f);
```

**3. Object Pooling**

```csharp
// Don't create/destroy projectiles constantly
ObjectPool<ProjectileEntity> projectilePool;

// Spawn
var projectile = projectilePool.Get();
projectile.Initialize(position, velocity);

// Return when done
projectilePool.Return(projectile);
```

**4. Priority System**

```csharp
// Send most important updates first
var sortedEntities = entities
    .OrderBy(e => GetPriority(e, forPlayer))
    .ToList();

float GetPriority(Entity e, Player p)
{
    float distance = Vector2.Distance(e.position, p.position);
    float importance = e.type == EntityType.Player ? 100 : 50;
    return distance / importance; // Lower = higher priority
}
```

**5. Interest Management**

```csharp
// Only send entities the player can see
const float VISIBLE_RANGE = 40f;

var visibleEntities = entities
    .Where(e => Vector2.Distance(e.position, player.position) < VISIBLE_RANGE)
    .ToList();
```

---

## Bandwidth Analysis

### Per Client (8 players, 30 enemies, 20/sec snapshots)

**Upstream (Client → Server):**
- Inputs: 50 bytes × 60/sec = **3 KB/sec**

**Downstream (Server → Client):**
- Own player: 80 bytes (full precision)
- Other 7 players: 40 bytes × 7 = 280 bytes
- 30 enemies: 30 bytes × 30 = 900 bytes
- Events: ~200 bytes
- **Total: ~1.5 KB per snapshot**
- At 20/sec: **30 KB/sec**

**Total per client: ~33 KB/sec (264 Kbps)**

### Server Total (8 clients)

**Upstream (receiving):**
- 8 clients × 3 KB/sec = **24 KB/sec**

**Downstream (sending):**
- 8 clients × 30 KB/sec = **240 KB/sec (1.9 Mbps)**

✅ **Easily handled by modern internet (25+ Mbps typical)**

---

## Latency Handling

### Typical Latency: 50ms

**Without optimization:**
- Input → Server: 50ms
- Server processes: 16ms
- Server → Client: 50ms
- **Total: 116ms delay** ❌ Feels laggy!

**With optimization:**
- Client prediction: **0ms perceived** ✅
- Entity interpolation: Smooth ✅
- Server reconciliation: Invisible corrections ✅

---

## Comparison: Simple Relay vs Authoritative

| Feature | Simple Relay | Authoritative |
|---------|--------------|---------------|
| **Players** | 2-4 | 8+ |
| **Enemies** | None (client-side) | Unlimited (server AI) |
| **Cheating** | Easy | Prevented |
| **Consistency** | Poor | Perfect |
| **AI** | Client-side only | Server-controlled |
| **Bandwidth** | High (P2P style) | Optimized |
| **Latency** | High | Low (prediction) |
| **Use Case** | Simple prototypes | Real games |

---

## Next Steps

See the implementation files:
1. `AuthoritativeGameServer.cs` - Main server
2. `GameState.cs` - Game state management
3. `SnapshotSystem.cs` - State snapshots
4. `ClientPrediction.cs` - Client-side prediction
5. `CombatSystem.cs` - Server-side combat

---

## Recommended Reading

- **Valve's Networking Guide**: Source Engine multiplayer networking
- **Gabriel Gambetta's Fast-Paced Multiplayer**: Client-side prediction series
- **Glenn Fiedler's Networking Articles**: Snapshot compression and interpolation

Good luck building your beat 'em up! 🎮⚔️
