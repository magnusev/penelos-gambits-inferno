﻿﻿using System.Collections.Generic;

namespace InfernoWow.Modules
{
    public class ProtectionPaladinRotation : Rotation
    {
        private List<string> UtilitySpells = new List<string> { "Devotion Aura" };

        private Environment _environment;
        private MessageRouter _messageRouter;
        private bool _logMessages = true;
        
        public override void LoadSettings()
        {
            _messageRouter = new MessageRouter();

            WebSocket.Port = 8082;
            WebSocket.OnMessageReceived += OnWebSocketMessage;
            WebSocket.Start();
            Inferno.PrintMessage("WebSocket server started on ws://localhost:8082/");

            _messageRouter.OnCommandReceived += OnCommandReceived;
            _messageRouter.OnQueryReceived += OnQueryReceived;
        }

        private void OnWebSocketMessage(string message)
        {
            if (_logMessages)
            {
                Inferno.PrintMessage("[WS Raw] " + message);
            }

            _messageRouter.HandleRawMessage(message);
        }

        private void OnClientConnected()
        {
            var connect = new ConnectMessage(Inferno.UnitName("player"), Inferno.GetSpec("player"));
            _messageRouter.SendConnect(connect);
            Inferno.PrintMessage("[WS] Client connected — sent CONNECT");
        }

        private void OnCommandReceived(CommandMessage command)
        {
            Inferno.PrintMessage("[WS CMD] " + command.Action + " - " + (command.Spell != null ? command.Spell : command.Macro != null ? command.Macro : "NONE"));
        }

        private void OnQueryReceived(QueryMessage query)
        {
            Inferno.PrintMessage("[WS QUERY] " + query.Method + " (id: " + query.QueryId + ")");
        }

        public override void Initialize()
        {
            Inferno.PrintMessage("Penelos Gambits Loader");
            foreach (string s in UtilitySpells)
            {
                Spellbook.Add(s);
            }
            
            foreach (var macro in TargetingMacros.macros)
            {
                Macros.Add(macro.Key, macro.Value);
            }
        }

        public override void OnStop()
        {
            _messageRouter.ClearQueues();
            WebSocket.Stop();
            Inferno.PrintMessage("WebSocket server stopped");
        }

        public override bool CombatTick()
        {
            RefreshEnvironment();
            SendStateUpdate();

            if (_messageRouter.HasPendingCommands())
            {
                var command = _messageRouter.DequeueCommand();
                if (command != null)
                {
                    Inferno.PrintMessage("[Phase1] Received command: " + command.Action);
                    var result = new ExecutionResultMessage(command.CommandId, true, null);
                    _messageRouter.SendExecutionResult(result);
                    return true;
                }
            }

            return false;
        }

        public override bool OutOfCombatTick()
        {
            RefreshEnvironment();

            if (!Inferno.HasBuff("Devotion Aura") && Inferno.CanCast("Devotion Aura"))
            {
                Inferno.Cast("Devotion Aura");
                return true;
            }

            SendStateUpdate();
            return false;
        }

        private void SendStateUpdate()
        {
            if (_environment == null) return;
            
            var stateUpdate = new StateUpdateMessage(_environment);
            _messageRouter.SendStateUpdate(stateUpdate);
        }

        private void RefreshEnvironment()
        {
            _environment = new Environment(GetOldBosses());
        }
        
        private List<Boss> GetOldBosses()
        {
            if (_environment == null) return new List<Boss>();
            return _environment.Bosses;
        }

    }
}