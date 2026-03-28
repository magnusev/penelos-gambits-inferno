# Step 02 — Bot: Send CONNECT When Engine Connects

## Problem

`ConnectMessage` class exists but is never sent automatically when a new WebSocket client connects.
The engine has no way to know which character/spec it's controlling.

## What to do

1. In `WebSocket.cs` `HandleClientAsync()`, after accepting the WebSocket connection and adding
   the client to `_clients`, fire a new event or call a callback so the rotation can react.
2. In `rotation.cs`, when a new client connects, broadcast a `ConnectMessage` with character
   name and spec:
   ```csharp
   var connect = new ConnectMessage(Inferno.UnitName("player"), Inferno.GetSpec("player"));
   _messageRouter.SendConnect(connect);
   ```
3. The `SendConnect` method already exists on `MessageRouter`.

## Files to change

- `PenelosGambits/WebSocket/WebSocket.cs` (add connection event)
- `PenelosGambits/rotation.cs` (send CONNECT on new connection)

## How to verify

Not a standalone checkpoint. Verified together with Step 04 (engine receives CONNECT).
