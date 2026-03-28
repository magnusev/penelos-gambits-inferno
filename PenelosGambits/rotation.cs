﻿using System.Collections.Generic;
using System.Drawing;

namespace InfernoWow.Modules
{
    public class ProtectionPaladinRotation : Rotation
    {
        private List<string> UtilitySpells = new List<string> { "Devotion Aura" };

        private Environment _environment;
        private MessageRouter _messageRouter;
        private CommandExecutor _commandExecutor;
        private QueryHandler _queryHandler;
        
        public override void LoadSettings()
        {
            _messageRouter = new MessageRouter();
            _commandExecutor = new CommandExecutor(_messageRouter);
            _queryHandler = new QueryHandler(_messageRouter);

            WebSocket.Port = 8082;
            WebSocket.OnMessageReceived += OnWebSocketMessage;
            WebSocket.OnClientConnected += OnClientConnected;
            WebSocket.OnClientDisconnected += OnClientDisconnected;
            WebSocket.Start();
            Inferno.PrintMessage("[WS] Server started on ws://localhost:8082/", Color.Green);
        }

        private void OnWebSocketMessage(string message)
        {
            _messageRouter.HandleRawMessage(message);
        }

        private void OnClientConnected()
        {
            Inferno.PrintMessage("[WS] Engine connected (clients: " + WebSocket.ClientCount + ")", Color.Green);
            var connect = new ConnectMessage(Inferno.UnitName("player"), Inferno.GetSpec("player"));
            _messageRouter.SendConnect(connect);
        }

        private void OnClientDisconnected()
        {
            Inferno.PrintMessage("[WS] Engine disconnected (clients: " + WebSocket.ClientCount + ")", Color.Orange);
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
            Inferno.PrintMessage("[WS] Server stopped", Color.Yellow);
        }

        public override bool CombatTick()
        {
            RefreshEnvironment();
            ProcessPendingQueries();
            SendStateUpdate();

            if (_messageRouter.HasPendingCommands())
            {
                var command = _messageRouter.DequeueCommand();
                if (command != null)
                {
                    return _commandExecutor.Execute(command);
                }
            }

            return false;
        }

        public override bool OutOfCombatTick()
        {
            RefreshEnvironment();
            ProcessPendingQueries();

            if (!Inferno.HasBuff("Devotion Aura") && Inferno.CanCast("Devotion Aura"))
            {
                Inferno.Cast("Devotion Aura");
                return true;
            }

            SendStateUpdate();
            return false;
        }

        private void ProcessPendingQueries()
        {
            while (_messageRouter.HasPendingQueries())
            {
                var query = _messageRouter.DequeueQuery();
                if (query != null)
                {
                    _queryHandler.HandleQuery(query);
                }
            }
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