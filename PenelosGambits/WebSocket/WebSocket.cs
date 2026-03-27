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

    public static void Start()
    {
        if (IsRunning) return;

        _cts = new CancellationTokenSource();
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://localhost:" + Port + "/");
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
                        ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None)
                           .GetAwaiter().GetResult();
                }
                catch { /* best-effort */ }
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

    public static async Task BroadcastAsync(string message)
    {
        var data = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(data);

        List<System.Net.WebSockets.WebSocket> snapshot;
        lock (_lock) { snapshot = new List<System.Net.WebSockets.WebSocket>(_clients); }

        foreach (var ws in snapshot)
        {
            try
            {
                if (ws.State == WebSocketState.Open)
                    await ws.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch
            {
                RemoveClient(ws);
            }
        }
    }

    /// <summary>
    /// Fire-and-forget convenience wrapper for broadcast.
    /// </summary>
    public static void Broadcast(string message)
    {
        Task.Run(() => BroadcastAsync(message));
    }

    /// <summary>
    /// Event raised when a text message is received from any client.
    /// </summary>
    public static event Action<string> OnMessageReceived;

    // ─── internals ───────────────────────────────────────────

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
            catch (HttpListenerException ex)
            {
                if (ct.IsCancellationRequested)
                {
                    break;
                }
            }
            catch
            {
                // transient error – keep accepting
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

            lock (_lock) { _clients.Add(ws); }

            var buf = new byte[4096];

            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buf), ct);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    var text = Encoding.UTF8.GetString(buf, 0, result.Count);
                    if (OnMessageReceived != null)
                    {
                        OnMessageReceived(text);
                    }

                    await ws.SendAsync(new ArraySegment<byte>(buf, 0, result.Count),
                        WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }
        catch { /* client disconnected */ }
        finally
        {
            if (ws != null) RemoveClient(ws);
        }
    }

    private static void RemoveClient(System.Net.WebSockets.WebSocket ws)
    {
        lock (_lock) { _clients.Remove(ws); }
        try { ws.Dispose(); } catch { }
    }
}