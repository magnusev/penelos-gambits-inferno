﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public static class WebSocket
{
    private static HttpListener _listener;
    private static CancellationTokenSource _cts;
    private static readonly List<System.Net.WebSockets.WebSocket> _clients = new List<System.Net.WebSockets.WebSocket>();
    private static readonly object _lock = new object();
    private static int _port = 8080;

    public static int Port 
    { 
        get { return _port; } 
        set { _port = value; } 
    }
    
    public static bool IsRunning 
    { 
        get { return _listener != null && _listener.IsListening; } 
    }

    public static event Action<string> OnMessageReceived;
    public static event Action OnClientConnected;
    public static event Action OnClientDisconnected;

    public static int ClientCount
    {
        get { lock (_lock) { return _clients.Count; } }
    }

    public static void Start()
    {
        if (IsRunning) return;

        _cts = new CancellationTokenSource();
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://+:" + Port + "/");
        _listener.Start();

        Task.Run(() => AcceptLoopAsync(_cts.Token));
    }

    public static void Stop()
    {
        if (_cts != null)
        {
            _cts.Cancel();
        }

        lock (_lock)
        {
            foreach (var ws in _clients)
            {
                try
                {
                    if (ws.State == WebSocketState.Open)
                    {
                        ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None)
                          .GetAwaiter().GetResult();
                    }
                }
                catch { }
            }
            _clients.Clear();
        }

        if (_listener != null)
        {
            _listener.Stop();
            _listener.Close();
            _listener = null;
        }
    }

    public static void Broadcast(string message)
    {
        Task.Run(() => BroadcastAsync(message));
    }

    public static async Task BroadcastAsync(string message)
    {
        var data = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(data);

        List<System.Net.WebSockets.WebSocket> snapshot;
        lock (_lock)
        {
            snapshot = new List<System.Net.WebSockets.WebSocket>(_clients);
        }

        foreach (var ws in snapshot)
        {
            try
            {
                if (ws.State == WebSocketState.Open)
                {
                    await ws.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            catch
            {
                RemoveClient(ws);
            }
        }
    }

    private static async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var ctx = await _listener.GetContextAsync();

                if (ctx.Request.IsWebSocketRequest)
                {
                    Task.Run(() => HandleClientAsync(ctx, ct), ct);
                }
                else
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.Close();
                }
            }
            catch (HttpListenerException)
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }
            }
            catch
            {
            }
        }
    }

    private static async Task HandleClientAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        System.Net.WebSockets.WebSocket ws = null;
        try
        {
            var wsCtx = await ctx.AcceptWebSocketAsync(null);
            ws = wsCtx.WebSocket;

            lock (_lock)
            {
                _clients.Add(ws);
            }

            if (OnClientConnected != null)
            {
                OnClientConnected();
            }

            var buf = new byte[8192];
            var messageBuffer = new StringBuilder();

            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buf), ct);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    messageBuffer.Append(Encoding.UTF8.GetString(buf, 0, result.Count));

                    if (result.EndOfMessage)
                    {
                        var text = messageBuffer.ToString();
                        messageBuffer.Clear();

                        if (OnMessageReceived != null)
                        {
                            OnMessageReceived(text);
                        }
                    }
                }
            }
        }
        catch
        {
        }
        finally
        {
            if (ws != null)
            {
                RemoveClient(ws);
            }
        }
    }

    private static void RemoveClient(System.Net.WebSockets.WebSocket ws)
    {
        lock (_lock)
        {
            _clients.Remove(ws);
        }

        if (OnClientDisconnected != null)
        {
            OnClientDisconnected();
        }
        
        try
        {
            ws.Dispose();
        }
        catch
        {
        }
    }
}