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

            // If type field is missing, infer from other fields
            if (msgType == null)
            {
                msgType = InferMessageType(json);
            }

            if (msgType == null)
            {
                string preview = json.Length > 80 ? json.Substring(0, 80) + "..." : json;
                Inferno.PrintMessage("[MessageRouter] No type field in: " + preview);
                return;
            }

            if (msgType == MessageType.Command)
            {
                var command = CommandMessage.FromJson(json);
                if (command != null)
                {
                    // Don't queue NONE commands — just ack them
                    if (command.IsNone())
                    {
                        return;
                    }

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
        }
        catch (Exception ex)
        {
            Inferno.PrintMessage("[MessageRouter] Error parsing message: " + ex.Message);
        }
    }

    private static string InferMessageType(string json)
    {
        if (json.Contains("\"commandId\"") && json.Contains("\"action\""))
        {
            return MessageType.Command;
        }
        if (json.Contains("\"queryId\"") && json.Contains("\"method\""))
        {
            return MessageType.Query;
        }
        if (json.Contains("\"PING\""))
        {
            return MessageType.Ping;
        }
        return null;
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
