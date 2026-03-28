using System;
using System.Collections.Generic;

public class CommandExecutor
{
    private readonly MessageRouter _router;
    private static readonly HashSet<string> AllowedActions = new HashSet<string> { "CAST", "MACRO", "NONE" };

    public CommandExecutor(MessageRouter router)
    {
        _router = router;
    }

    public bool Execute(CommandMessage command)
    {
        if (command == null)
        {
            return false;
        }

        string action = command.Action;
        if (action == null || !AllowedActions.Contains(action))
        {
            SendResult(command, false, "Unknown action: " + action);
            return false;
        }

        try
        {
            if (command.IsNone())
            {
                SendResult(command, true, null);
                return false;
            }

            if (command.IsCast())
            {
                return ExecuteCast(command);
            }

            if (command.IsMacro())
            {
                return ExecuteMacro(command);
            }

            SendResult(command, false, "Unhandled action: " + action);
            return false;
        }
        catch (Exception ex)
        {
            Inferno.PrintMessage("[CommandExecutor] Error: " + ex.Message);
            SendResult(command, false, "Exception: " + ex.Message);
            return false;
        }
    }

    private bool ExecuteCast(CommandMessage command)
    {
        string spell = command.Spell;
        if (string.IsNullOrEmpty(spell))
        {
            SendResult(command, false, "CAST missing spell name");
            return false;
        }

        string target = command.Target;
        string castUnit = (target != null) ? target : "target";

        if (!Inferno.CanCast(spell, castUnit))
        {
            SendResult(command, false, "Cannot cast " + spell + " on " + castUnit);
            return false;
        }

        // Targeted cast on a specific unit (not current target):
        // Tick 1: focus the unit, queue the @focus macro for next tick
        // Tick 2: ActionQueuer fires the "/cast [@focus] Spell" macro
        if (target != null && target != "target")
        {
            Inferno.Cast("focus_" + target);

            // Use the registered @focus macro if available, otherwise queue the raw spell
            string macroName = SpellMacroRegistry.GetMacroName(spell);
            ActionQueuer.QueueAction(macroName != null ? macroName : spell);

            SendResult(command, true, null);
            return true;
        }

        // Direct cast on current target or self
        Inferno.Cast(spell);
        SendResult(command, true, null);
        return true;
    }

    private bool ExecuteMacro(CommandMessage command)
    {
        string macro = command.Macro;
        if (string.IsNullOrEmpty(macro))
        {
            SendResult(command, false, "MACRO missing macro name");
            return false;
        }

        Inferno.Cast(macro);
        Inferno.PrintMessage("[CMD] Macro: " + macro);
        SendResult(command, true, null);
        return true;
    }

    private void SendResult(CommandMessage command, bool success, string error)
    {
        var result = new ExecutionResultMessage(command.CommandId, success, error);
        _router.SendExecutionResult(result);
    }
}
