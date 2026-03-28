# Step 07 — Bot: Execute Commands (CAST / MACRO)

## Problem

`CombatTick()` dequeues `CommandMessage` but only logs it. It never actually casts a spell
or runs a macro.

## What to do

1. In `rotation.cs` `CombatTick()`, after dequeuing a command, execute it:
   ```csharp
   var command = _messageRouter.DequeueCommand();
   if (command != null)
   {
       bool success = false;
       string error = null;

       try
       {
           if (command.IsCast())
           {
               if (command.Target != null)
                   Inferno.Cast(command.Spell, command.Target);
               else
                   Inferno.Cast(command.Spell);
               success = true;
           }
           else if (command.IsMacro())
           {
               Inferno.RunMacro(command.Macro);
               success = true;
           }
           else if (command.IsNone())
           {
               success = true; // intentional no-op
           }
       }
       catch (Exception ex)
       {
           error = ex.Message;
       }

       var result = new ExecutionResultMessage(command.CommandId, success, error);
       _messageRouter.SendExecutionResult(result);
   }
   ```

2. Ensure spells referenced by the engine are in the `Spellbook` and macros are in `Macros`.
   For Phase 1 testing, add a few test spells.

## Files to change

- `PenelosGambits/rotation.cs`

## How to verify

Tested together with Step 08 (or by sending a COMMAND manually via websocat).
You can test now by connecting a WebSocket client and sending:
```json
{"type":"COMMAND","commandId":"test-1","action":"NONE"}
```
Bot should respond with:
```json
{"type":"EXECUTION_RESULT","commandId":"test-1","success":true,"error":null}
```
