I've created a comprehensive DESIGN.md file that addresses all your questions:
A) Is WebSocket a Good Match?
✅ YES - WebSocket is perfect for your use case because:
Bidirectional communication (client sends state, server sends commands AND queries back)
Low latency (~100-200ms round trip is acceptable for rotation)
Persistent connection (no reconnection overhead)
Simple protocol (text-based JSON)
B) Key Design Decisions:
Communication Pattern: Hybrid async approach
Client sends state updates (3 per second)
Server responds with commands asynchronously
Server can query client for additional info
Client has local fallback if server is slow/disconnected
Message Types:
STATE_UPDATE (client → server)
COMMAND (server → client) - CAST, MACRO, NONE
QUERY (server → client) - request for specific info
QUERY_RESPONSE (client → server)
Critical Features:
Fallback rotation (never freeze the bot waiting for server)
Connection health monitoring (timeout after 5 seconds)
Command validation (whitelist, verify spells exist)
Throttled updates (not every tick, just 2-3 per second)
Implementation Phases:
Phase 1: Basic WebSocket JSON messaging
Phase 2: State serialization
Phase 3: Command execution
Phase 4: Query system
Phase 5: Fallback & resilience
Phase 6: Optimization
The document includes code examples, architecture diagrams, security considerations, performance metrics, and server implementation recommendations (Python/Node.js/C#).
Ready to start implementation? I recommend starting with Phase 1: extending your existing WebSocket class to handle JSON messages and creating the message type classes.