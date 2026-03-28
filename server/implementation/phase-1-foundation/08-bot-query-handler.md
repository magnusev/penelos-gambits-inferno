# Step 08 — Bot: Query Handler

## What to do

Create `QueryHandler.cs` that receives a `QueryMessage`, looks up the answer via `Inferno.*`,
and sends back a `QueryResponseMessage`.

1. Create `PenelosGambits/Common/QueryHandler.cs`:
   ```csharp
   public static class QueryHandler
   {
       public static QueryResponseMessage Handle(QueryMessage query)
       {
           switch (query.Method)
           {
               case "Health":
                   return HandleHealth(query);
               case "HasBuff":
                   return HandleHasBuff(query);
               case "HasDebuff":
                   return HandleHasDebuff(query);
               case "CanCast":
                   return HandleCanCast(query);
               case "SpellCooldown":
                   return HandleSpellCooldown(query);
               default:
                   return new QueryResponseMessage(query.QueryId, false, null);
           }
       }
       // ... implement each method using Inferno.* API
   }
   ```

2. Wire it into `rotation.cs` via the `OnQueryReceived` callback:
   ```csharp
   private void OnQueryReceived(QueryMessage query)
   {
       var response = QueryHandler.Handle(query);
       _messageRouter.SendQueryResponse(response);
   }
   ```

3. Start with 5 methods: `Health`, `HasBuff`, `HasDebuff`, `CanCast`, `SpellCooldown`.
   More can be added incrementally as gambits need them.

## Files to create / change

- `PenelosGambits/Common/QueryHandler.cs` (new)
- `PenelosGambits/rotation.cs` (wire up)

## Manual test — Checkpoint (Full Phase 1 Verification)

At this point the entire Phase 1 communication loop should work:

1. Start bot, start engine.
2. Engine receives STATE_UPDATE every tick (verified in Step 06).
3. From engine code (or a test), send a QUERY:
   ```json
   {"type":"QUERY","queryId":"q-1","method":"Health","params":{"unit":"player"}}
   ```
4. Bot should respond:
   ```json
   {"type":"QUERY_RESPONSE","queryId":"q-1","result":true,"data":{"health":95}}
   ```
5. Send a COMMAND:
   ```json
   {"type":"COMMAND","commandId":"c-1","action":"NONE"}
   ```
6. Bot responds with EXECUTION_RESULT success.
7. Stop engine, restart it. Everything reconnects and works again.
8. Repeat steps 3-6 after reconnect.

**Pass**: full round-trip works: STATE_UPDATE, QUERY/RESPONSE, COMMAND/RESULT, reconnect.

This completes Phase 1.
