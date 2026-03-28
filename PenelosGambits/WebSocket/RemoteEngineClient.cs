using System;
using System.Drawing;

public class RemoteEngineClient
{
    private readonly MessageRouter _messageRouter;
    private readonly CommandExecutor _commandExecutor;
    private readonly QueryHandler _queryHandler;
    private int _port;

    public RemoteEngineClient(int port)
    {
        _port = port;
        _messageRouter = new MessageRouter();
        _commandExecutor = new CommandExecutor(_messageRouter);
        _queryHandler = new QueryHandler(_messageRouter);
    }

    public void Start()
    {
        WebSocket.Port = _port;
        WebSocket.OnMessageReceived += OnMessage;
        WebSocket.OnClientConnected += OnConnected;
        WebSocket.OnClientDisconnected += OnDisconnected;
        WebSocket.Start();
        Inferno.PrintMessage("[WS] Server started on port " + _port, Color.Green);
    }

    public void Stop()
    {
        _messageRouter.ClearQueues();
        WebSocket.OnMessageReceived -= OnMessage;
        WebSocket.OnClientConnected -= OnConnected;
        WebSocket.OnClientDisconnected -= OnDisconnected;
        WebSocket.Stop();
        Inferno.PrintMessage("[WS] Server stopped", Color.Yellow);
    }

    public void SendStateUpdate(Environment environment)
    {
        if (environment == null) return;
        var msg = new StateUpdateMessage(environment);
        _messageRouter.SendStateUpdate(msg);
    }

    public void ProcessPendingQueries()
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

    public bool ExecuteNextCommand()
    {
        if (!_messageRouter.HasPendingCommands()) return false;

        var command = _messageRouter.DequeueCommand();
        if (command == null) return false;

        return _commandExecutor.Execute(command);
    }

    private void OnMessage(string message)
    {
        _messageRouter.HandleRawMessage(message);
    }

    private void OnConnected()
    {
        Inferno.PrintMessage("[WS] Engine connected (clients: " + WebSocket.ClientCount + ")", Color.Green);
        var connect = new ConnectMessage(Inferno.UnitName("player"), Inferno.GetSpec("player"));
        _messageRouter.SendConnect(connect);
    }

    private void OnDisconnected()
    {
        Inferno.PrintMessage("[WS] Engine disconnected (clients: " + WebSocket.ClientCount + ")", Color.Orange);
    }
}
