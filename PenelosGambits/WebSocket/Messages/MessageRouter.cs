using System;
using System.Collections.Generic;

public class MessageRouter
{
    private readonly object _lock = new object();
    private readonly Queue<CommandMessage> _commandQueue = new Queue<CommandMessage>();
    private readonly Queue<QueryMessage> _queryQueue = new Queue<QueryMessage>();

    public event Action<CommandMessage> OnCommandReceived;
    public event Action<QueryMessage> OnQueryReceived;

    public void HandleRawMessage(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            string msgType = MessageBase.ParseType(json);
            if (msgType == null)
            {
                Inferno.PrintMessage("[MessageRouter] Unknown message - no type field");
                return;
            }

            if (msgType == MessageType.Command)
            {
                var command = CommandMessage.FromJson(json);
                if (command != null)
                {
                    lock (_lock)
                    {
                        _commandQueue.Enqueue(command);
                    }

                    if (OnCommandReceived != null)
                    {
                        OnCommandReceived(command);
                    }
                }
            }
            else if (msgType == MessageType.Query)
            {
                var query = QueryMessage.FromJson(json);
                if (query != null)
                {
                    lock (_lock)
                    {
                        _queryQueue.Enqueue(query);
                    }

                    if (OnQueryReceived != null)
                    {
                        OnQueryReceived(query);
                    }
                }
            }
            else if (msgType == MessageType.Ping)
            {
                SendPong();
            }
            else
            {
                Inferno.PrintMessage("[MessageRouter] Unhandled message type: " + msgType);
            }
        }
        catch (Exception ex)
        {
            Inferno.PrintMessage("[MessageRouter] Error parsing message: " + ex.Message);
        }
    }

    public CommandMessage DequeueCommand()
    {
        lock (_lock)
        {
            if (_commandQueue.Count > 0)
            {
                return _commandQueue.Dequeue();
            }
        }
        return null;
    }

    public QueryMessage DequeueQuery()
    {
        lock (_lock)
        {
            if (_queryQueue.Count > 0)
            {
                return _queryQueue.Dequeue();
            }
        }
        return null;
    }

    public bool HasPendingCommands()
    {
        lock (_lock)
        {
            return _commandQueue.Count > 0;
        }
    }

    public bool HasPendingQueries()
    {
        lock (_lock)
        {
            return _queryQueue.Count > 0;
        }
    }

    public int PendingCommandCount()
    {
        lock (_lock)
        {
            return _commandQueue.Count;
        }
    }

    public void ClearQueues()
    {
        lock (_lock)
        {
            _commandQueue.Clear();
            _queryQueue.Clear();
        }
    }

    public void SendStateUpdate(StateUpdateMessage stateUpdate)
    {
        WebSocket.Broadcast(stateUpdate.ToJson());
    }

    public void SendQueryResponse(QueryResponseMessage response)
    {
        WebSocket.Broadcast(response.ToJson());
    }

    public void SendExecutionResult(ExecutionResultMessage result)
    {
        WebSocket.Broadcast(result.ToJson());
    }

    public void SendConnect(ConnectMessage connect)
    {
        WebSocket.Broadcast(connect.ToJson());
    }

    private void SendPong()
    {
        WebSocket.Broadcast("{\"type\":\"PONG\"}");
    }
}
