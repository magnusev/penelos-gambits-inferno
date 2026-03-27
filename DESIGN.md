# Remote Rotation Server Design Document

## Overview

**Goal**: Create a client-server architecture where the WoW rotation client sends game state to a remote server, which makes decisions and sends back commands (targeting, casting, queries).

**Current Architecture**: 
- Client: Inferno WoW bot framework running rotation logic locally
- Proposed: Client sends state → Server decides → Client executes commands

---

## A) Is WebSocket a Good Match?

### ✅ **YES - WebSocket is Excellent for This Use Case**

**Why WebSocket is Perfect:**

1. **Bidirectional Communication**
   - Client can send state updates
   - Server can send commands back
   - Server can query client for additional info
   - Real-time request/response pattern

2. **Low Latency**
   - Persistent connection (no connection overhead per message)
   - Critical for game rotation timing (100-200ms tick rate)
   - No HTTP handshake overhead

3. **Stateful Connection**
   - Server knows which client is which
   - Can track game state over time
   - Easy to implement "session" per character

4. **Simple Protocol**
   - Text-based JSON messages
   - Easy to debug
   - Language agnostic (server can be Python, Node.js, C#, etc.)

### ❌ **Alternatives Considered & Why They're Worse:**

| Protocol | Pros | Cons | Verdict |
|----------|------|------|---------|
| **HTTP REST** | Simple, stateless | Too much overhead per request, no push from server | ❌ Too slow |
| **gRPC** | Fast, typed | Complex setup, overkill for simple commands | ❌ Overengineered |
| **TCP Sockets** | Fastest | Need to implement protocol, framing, reconnection | ❌ Too low-level |
| **UDP** | Lowest latency | No reliability, packet loss, need ack system | ❌ Unreliable |
| **WebSocket** | Fast, bidirectional, reliable, simple | Need keep-alive handling | ✅ **BEST CHOICE** |

---

## B) Design Decisions & Architecture

### 1. **Communication Pattern: Request-Response with Query Support**

```
┌─────────────┐                    ┌─────────────┐
│   Client    │                    │   Server    │
│  (Inferno)  │                    │  (Python?)  │
└──────┬──────┘                    └──────┬──────┘
       │                                  │
       │  1. STATE_UPDATE                 │
       │  {environment, cooldowns, etc}   │
       ├─────────────────────────────────>│
       │                                  │
       │          2. QUERY_REQUEST        │
       │  "HasDebuff(boss1, Flame Shock)" │
       │<─────────────────────────────────┤
       │                                  │
       │  3. QUERY_RESPONSE               │
       │  {result: true, stacks: 1}       │
       ├─────────────────────────────────>│
       │                                  │
       │  4. COMMAND_RESPONSE             │
       │  {action: "CAST", spell: "..."}  │
       │<─────────────────────────────────┤
       │                                  │
       │  5. EXECUTION_RESULT             │
       │  {success: true}                 │
       ├─────────────────────────────────>│
```

### 2. **Message Types**

#### **Client → Server Messages**

```csharp
// 1. State Update (every tick)
{
    "type": "STATE_UPDATE",
    "timestamp": 1234567890,
    "player": {
        "health": 100,
        "power": 80,
        "powerType": 0,
        "spec": "Enhancement",
        "casting": null,
        "position": {"x": 0, "y": 0, "z": 0}
    },
    "target": {
        "exists": true,
        "name": "Boss",
        "health": 75,
        "casting": "Fireball"
    },
    "group": {
        "type": "raid",
        "size": 20,
        "members": [...]
    },
    "bosses": [
        {"unitId": "boss1", "name": "Boss", "health": 75}
    ],
    "globalCooldown": 0,
    "combatTime": 45000
}

// 2. Query Response
{
    "type": "QUERY_RESPONSE",
    "queryId": "q123",
    "result": true,
    "data": {...}
}

// 3. Execution Result
{
    "type": "EXECUTION_RESULT",
    "commandId": "cmd456",
    "success": true,
    "error": null
}

// 4. Connection Init
{
    "type": "CONNECT",
    "character": "MyCharacter",
    "realm": "MyRealm",
    "spec": "Enhancement Shaman"
}
```

#### **Server → Client Messages**

```csharp
// 1. Query Request
{
    "type": "QUERY",
    "queryId": "q123",
    "method": "HasDebuff",
    "params": {
        "unit": "boss1",
        "debuff": "Flame Shock",
        "byPlayer": true
    }
}

// 2. Command Response
{
    "type": "COMMAND",
    "commandId": "cmd456",
    "action": "CAST",
    "spell": "Lava Burst",
    "target": "boss1"
}

// 3. Macro Command
{
    "type": "COMMAND",
    "commandId": "cmd457",
    "action": "MACRO",
    "macro": "focus_raid5"
}

// 4. No Action
{
    "type": "COMMAND",
    "commandId": "cmd458",
    "action": "NONE"
}

// 5. Keep Alive
{
    "type": "PING"
}
```

---

## 3. **Critical Design Decisions**

### Decision 1: **Synchronous vs Asynchronous Query Pattern**

**Option A: Synchronous (Blocking)**
```csharp
// Client waits for server response before continuing
StateUpdate → [Wait for Command] → Execute → NextTick
```
✅ Simple to implement
✅ Guaranteed order
❌ Adds latency to every tick
❌ Server delay = rotation freeze

**Option B: Asynchronous (Non-Blocking)**
```csharp
// Client sends state and continues, executes command when received
StateUpdate (async) → Continue Tick
Command Received → Queue for Next Tick
```
✅ No blocking
✅ Server can take time to decide
❌ More complex state management
❌ Commands might arrive out of order

**RECOMMENDATION: Hybrid Approach**
```csharp
public override bool CombatTick()
{
    // Send state update (fire and forget)
    SendStateUpdate();
    
    // Check if we have pending command from server
    var command = GetPendingCommand();
    if (command != null)
    {
        ExecuteCommand(command);
        return true;
    }
    
    // Fallback to local logic if no command
    return LocalFallback();
}
```

### Decision 2: **Query Strategy**

**Problem**: Server needs to query client for info not in state update (e.g., specific debuff stacks on raid member 17)

**Option A: Include Everything in State Update**
- Send ALL possible info every tick
- ❌ Huge payload (1000+ data points)
- ❌ Wasteful bandwidth
- ✅ No round trips

**Option B: On-Demand Queries**
- Server requests specific info as needed
- ✅ Small payloads
- ✅ Flexible
- ❌ Adds latency (round trip)
- ❌ Complex request/response matching

**Option C: Smart State + Queries**
- Send "important" state every tick (target, player, bosses)
- Server queries for edge cases
- ✅ Balance of efficiency and flexibility
- ✅ Most decisions don't need queries

**RECOMMENDATION: Option C - Smart State with Query Support**

```csharp
// State update includes "hot" data
- Player stats
- Target info
- Boss list
- Group size/composition
- Cooldowns for main spells

// Query for "cold" data
- Specific raid member debuffs
- Distance between two units
- Threat on specific mob
```

### Decision 3: **Connection Handling**

**Must Handle:**
1. **Initial Connection**
   - Send character info
   - Wait for server ACK before sending state

2. **Disconnection**
   - Fall back to local rotation
   - Queue commands while reconnecting
   - Reconnect automatically

3. **Server Timeout**
   - If no response in 5 seconds, use fallback
   - Don't freeze rotation waiting for server

**Implementation:**
```csharp
public class RemoteRotationClient
{
    private WebSocketState _state;
    private DateTime _lastResponse;
    private Queue<Command> _pendingCommands;
    private bool _useLocalFallback;
    
    public bool CombatTick()
    {
        // Check connection health
        if ((DateTime.Now - _lastResponse).TotalSeconds > 5)
        {
            _useLocalFallback = true;
        }
        
        // Send state if connected
        if (_state == Connected)
        {
            SendStateUpdate();
        }
        
        // Execute pending commands
        if (_pendingCommands.Count > 0)
        {
            var cmd = _pendingCommands.Dequeue();
            return ExecuteCommand(cmd);
        }
        
        // Fallback to local logic
        if (_useLocalFallback)
        {
            return LocalCombatTick();
        }
        
        return false;
    }
}
```

### Decision 4: **State Update Frequency**

**Options:**
1. **Every Tick** (10-20 times per second)
   - ❌ Too much data
   - ❌ Server overwhelmed
   
2. **Throttled** (2-3 times per second)
   - ✅ Reasonable load
   - ✅ Still responsive
   - Can send immediately on important events

3. **On Change** (only when state changes)
   - ✅ Minimal bandwidth
   - ❌ Complex change detection
   - ❌ Server doesn't know client is alive

**RECOMMENDATION: Throttled with Event Priority**
```csharp
private DateTime _lastStateUpdate;
private const int STATE_UPDATE_MS = 333; // 3 per second

public bool CombatTick()
{
    bool forceUpdate = false;
    
    // Force update on important events
    if (TargetChanged() || BossAppeared() || PhaseChange())
    {
        forceUpdate = true;
    }
    
    // Regular throttled update
    if (forceUpdate || (DateTime.Now - _lastStateUpdate).TotalMilliseconds > STATE_UPDATE_MS)
    {
        SendStateUpdate();
        _lastStateUpdate = DateTime.Now;
    }
    
    // ... rest of logic
}
```

---

## 4. **Data Serialization**

### **Format: JSON**

**Pros:**
- ✅ Human readable (easy debugging)
- ✅ Every language supports it
- ✅ Schema-less (flexible)

**Cons:**
- ❌ Larger than binary
- ❌ Slower to parse

**Alternatives:**
- **MessagePack**: Binary JSON (faster, smaller)
- **Protobuf**: Typed schema (fastest)

**RECOMMENDATION: Start with JSON, optimize later if needed**

### **C# 5.0 Serialization (No Modern Features)**

```csharp
// Cannot use: System.Text.Json (too new)
// Must use: Manual string building or JSON.NET (if available)

public class StateUpdateMessage
{
    public string ToJson()
    {
        // Manual JSON building (C# 5.0 compatible)
        var sb = new StringBuilder();
        sb.Append("{");
        sb.Append("\"type\":\"STATE_UPDATE\",");
        sb.Append("\"timestamp\":" + timestamp + ",");
        sb.Append("\"player\":{");
        sb.Append("\"health\":" + player.Health + ",");
        sb.Append("\"power\":" + player.Power);
        sb.Append("}");
        sb.Append("}");
        return sb.ToString();
    }
}

// OR use a simple JSON library
// (Check if Inferno framework has one available)
```

---

## 5. **Error Handling Strategy**

### **Categories of Errors:**

1. **Network Errors** (connection lost)
   - → Fall back to local rotation
   - → Attempt reconnection every 5 seconds
   - → Log disconnect event

2. **Server Errors** (server crash, slow response)
   - → Timeout after 5 seconds
   - → Use local rotation
   - → Don't queue more than 10 commands

3. **Invalid Commands** (server sends bad command)
   - → Log error
   - → Skip command
   - → Send error response to server

4. **Serialization Errors** (malformed JSON)
   - → Log error
   - → Skip message
   - → Send PING to verify connection

### **Fallback Rotation**

**CRITICAL**: Always have a working local rotation as fallback

```csharp
public class RemoteRotation : Rotation
{
    private LocalRotation _fallback;
    private RemoteClient _client;
    
    public override bool CombatTick()
    {
        if (_client.IsConnected && _client.IsHealthy)
        {
            return RemoteCombatTick();
        }
        else
        {
            // Use fallback rotation
            return _fallback.CombatTick();
        }
    }
}
```

---

## 6. **Security Considerations**

### **Threats:**

1. **Man-in-the-Middle** (someone intercepts commands)
   - → Use WSS (WebSocket Secure) with TLS
   - → Server certificate validation

2. **Command Injection** (malicious server sends harmful commands)
   - → Whitelist allowed commands
   - → Validate all parameters
   - → Limit command rate

3. **Data Leakage** (sensitive character info sent)
   - → Don't send account info
   - → Don't send personal data
   - → Only send game state

### **Implementation:**

```csharp
public class SecureRemoteClient
{
    private HashSet<string> _allowedCommands = new HashSet<string>
    {
        "CAST", "MACRO", "NONE"
    };
    
    private bool ValidateCommand(Command cmd)
    {
        // Whitelist check
        if (!_allowedCommands.Contains(cmd.Action))
        {
            return false;
        }
        
        // Validate spell name
        if (cmd.Action == "CAST")
        {
            if (string.IsNullOrEmpty(cmd.Spell))
            {
                return false;
            }
            // Verify spell exists in spellbook
            if (!Spellbook.Contains(cmd.Spell))
            {
                return false;
            }
        }
        
        return true;
    }
}
```

---

## 7. **Testing Strategy**

### **Test Scenarios:**

1. **Local Echo Server** (for development)
   - Client connects to localhost
   - Server echoes state back
   - Test message format

2. **Mock Server Responses**
   - Test command execution
   - Test query handling
   - Test error cases

3. **Latency Simulation**
   - Add artificial delay
   - Test timeout handling
   - Test fallback behavior

4. **Disconnect Testing**
   - Kill server mid-combat
   - Test reconnection
   - Verify no rotation freeze

### **Debug Tools:**

```csharp
public class DebugSettings
{
    public bool LogAllMessages = false;
    public bool LogStateUpdates = false;
    public bool LogCommands = true;
    public bool SimulateLatency = false;
    public int LatencyMs = 0;
}
```

---

## 8. **Performance Considerations**

### **Metrics to Monitor:**

1. **Round Trip Time** (RTT)
   - Target: < 100ms
   - Warning: > 200ms
   - Critical: > 500ms

2. **Message Size**
   - State update: < 10KB
   - Query: < 1KB
   - Command: < 1KB

3. **Message Rate**
   - State updates: 2-3 per second
   - Commands: 0-10 per second
   - Queries: 0-5 per second

### **Optimization Tips:**

1. **Compress State Updates**
   - Only send changed values
   - Use abbreviations (hp, pow, etc)
   - Remove whitespace from JSON

2. **Batch Queries**
   - If server needs 5 queries, send them together
   - Single round trip instead of 5

3. **Cache Query Results**
   - Cache debuff status for 500ms
   - Don't query same thing twice in one tick

---

## 9. **Implementation Phases**

### **Phase 1: Basic WebSocket Connection** (Week 1)
- ✅ Already have WebSocket class
- Extend to handle JSON messages
- Add message types (STATE_UPDATE, COMMAND)
- Test with echo server

### **Phase 2: State Serialization** (Week 1-2)
- Create message classes
- Implement ToJson() methods
- Send Environment data
- Test with logging

### **Phase 3: Command Execution** (Week 2)
- Parse incoming commands
- Execute CAST commands
- Execute MACRO commands
- Handle errors

### **Phase 4: Query System** (Week 2-3)
- Implement query dispatcher
- Handle QUERY messages
- Send responses
- Test round-trip

### **Phase 5: Fallback & Resilience** (Week 3)
- Implement local fallback rotation
- Connection health monitoring
- Timeout handling
- Reconnection logic

### **Phase 6: Optimization** (Week 4)
- Reduce payload sizes
- Throttle updates
- Cache frequently used data
- Performance tuning

---

## 10. **Server Implementation Notes**

### **Recommended Server Stack:**

**Option A: Python + FastAPI**
```python
# Fast to develop, great AI/ML libraries for decision making
from fastapi import FastAPI, WebSocket
import asyncio

app = FastAPI()

@app.websocket("/rotation")
async def rotation_endpoint(websocket: WebSocket):
    await websocket.accept()
    while True:
        data = await websocket.receive_json()
        # Make decision
        command = decide_action(data)
        await websocket.send_json(command)
```

**Option B: Node.js + Express**
```javascript
// Fast, easy to deploy
const WebSocket = require('ws');
const wss = new WebSocket.Server({ port: 8080 });

wss.on('connection', (ws) => {
    ws.on('message', (data) => {
        const state = JSON.parse(data);
        const command = decideAction(state);
        ws.send(JSON.stringify(command));
    });
});
```

**Option C: C# + ASP.NET Core**
```csharp
// Same language as client
public class RotationHub : Hub
{
    public async Task SendState(StateUpdate state)
    {
        var command = DecideAction(state);
        await Clients.Caller.SendAsync("ReceiveCommand", command);
    }
}
```

### **Server Architecture:**

```
┌─────────────────────────────────────────────────┐
│                 Server                          │
├─────────────────────────────────────────────────┤
│                                                 │
│  ┌──────────────┐      ┌──────────────┐       │
│  │  WebSocket   │      │   Decision   │       │
│  │   Handler    │─────>│    Engine    │       │
│  └──────────────┘      └──────────────┘       │
│         │                      │                │
│         │                      v                │
│         │              ┌──────────────┐       │
│         │              │   AI Model   │       │
│         │              │   or Rules   │       │
│         │              └──────────────┘       │
│         │                      │                │
│         │<─────────────────────┘                │
│         │                                       │
│  ┌──────────────┐      ┌──────────────┐       │
│  │    Query     │      │    State     │       │
│  │   Handler    │      │    Store     │       │
│  └──────────────┘      └──────────────┘       │
│                                                 │
└─────────────────────────────────────────────────┘
```

---

## 11. **Configuration File**

### **Client Configuration (Settings)**

```csharp
public class RemoteRotationSettings
{
    // Connection
    public string ServerUrl = "ws://localhost:8082/rotation";
    public int ConnectionTimeoutSeconds = 5;
    public int ResponseTimeoutSeconds = 5;
    public bool AutoReconnect = true;
    public int ReconnectIntervalSeconds = 5;
    
    // Performance
    public int StateUpdateIntervalMs = 333; // 3 per second
    public int MaxQueuedCommands = 10;
    public bool ThrottleStateUpdates = true;
    
    // Fallback
    public bool EnableLocalFallback = true;
    public string FallbackRotation = "Basic";
    
    // Security
    public bool UseTLS = false;
    public bool ValidateServerCertificate = true;
    
    // Debug
    public bool LogMessages = false;
    public bool LogStateUpdates = false;
    public bool LogCommands = true;
    public bool LogErrors = true;
}
```

---

## 12. **Final Recommendations**

### **DO:**
✅ Use WebSocket (perfect for this use case)
✅ Implement fallback rotation (never freeze the bot)
✅ Throttle state updates (2-3 per second is enough)
✅ Use JSON for simplicity (optimize later if needed)
✅ Handle disconnections gracefully
✅ Validate all incoming commands
✅ Monitor latency and timeout appropriately
✅ Test with mock server first

### **DON'T:**
❌ Send state every tick (too much)
❌ Block rotation waiting for server (timeout instead)
❌ Trust server commands blindly (validate)
❌ Send sensitive account info
❌ Implement custom binary protocol (JSON is fine)
❌ Forget error handling
❌ Skip the fallback rotation

### **Next Steps:**

1. Extend existing WebSocket class to handle JSON messages
2. Create message type classes (StateUpdate, Command, Query)
3. Implement basic echo test server
4. Test state serialization
5. Implement command execution
6. Add fallback rotation
7. Deploy and test in-game

---

## File Structure

```
PenelosGambits/
├── Common/
│   ├── Environment.cs (already exists)
│   ├── messages/
│   │   ├── StateUpdate.cs
│   │   ├── Command.cs
│   │   ├── Query.cs
│   │   └── MessageBase.cs
│   └── utilities/
│       ├── TargetingMacros.cs (already exists)
│       └── JsonSerializer.cs
├── WebSocket/
│   ├── WebSocket.cs (already exists)
│   └── RemoteClient.cs (new)
├── rotation.cs (modify to use remote)
└── FallbackRotation.cs (safety net)
```

---

**End of Design Document**

**Ready to proceed?** Let me know which phase you want to start with!
