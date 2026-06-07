using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MarqueeLight.Models;

namespace MarqueeLight;

public sealed class HttpServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly LightEngine _engine;
    private readonly MainForm _form;
    private readonly CancellationTokenSource _cts = new();
    private Task _serveTask = Task.CompletedTask;
    private bool _disposed;

    public HttpServer(int port, LightEngine engine, MainForm form)
    {
        _engine = engine;
        _form = form;
        _listener = new TcpListener(System.Net.IPAddress.Loopback, port);
    }

    public void Start()
    {
        _listener.Start();
        _serveTask = Task.Run(() => ServeAsync(_cts.Token));
    }

    private async Task ServeAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                using var client = await _listener.AcceptTcpClientAsync(ct);
                await using var stream = client.GetStream();
                await HandleRequest(stream, ct);
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    private async Task HandleRequest(NetworkStream stream, CancellationToken ct)
    {
        var buffer = new byte[4096];
        int bytesRead = await stream.ReadAsync(buffer, ct);
        if (bytesRead == 0) return;

        var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        var lines = request.Split('\n');
        if (lines.Length == 0) return;

        var requestLine = lines[0].Trim();
        var parts = requestLine.Split(' ');
        if (parts.Length < 2)
        {
            await Respond(stream, 400, "Bad Request");
            return;
        }

        var method = parts[0].ToUpperInvariant();
        var path = parts[1];

        try
        {
            if (method == "GET" && path == "/status")
                await HandleGetStatus(stream);
            else if (method == "POST" && path == "/color")
                await HandlePostColor(stream, buffer, bytesRead);
            else if (method == "POST" && path == "/start")
                await HandleStartStop(stream, start: true);
            else if (method == "POST" && path == "/stop")
                await HandleStartStop(stream, start: false);
            else if (method == "POST" && path == "/speed")
                await HandlePostSpeed(stream, buffer, bytesRead);
            else if (method == "POST" && path == "/mode")
                await HandlePostMode(stream, buffer, bytesRead);
            else
                await Respond(stream, 404, "Not Found");
        }
        catch (Exception ex)
        {
            await RespondJson(stream, 400, new { success = false, error = ex.Message });
        }
    }

    private async Task HandleGetStatus(NetworkStream stream)
    {
        var theme = _engine.CurrentTheme;
        await RespondJson(stream, 200, new
        {
            color = theme.Name,
            running = true,
            speed = (double)_engine.Speed,
            opacity = (double)_engine.Opacity,
            mode = _engine.Mode.ToString().ToLowerInvariant()
        });
    }

    private Task HandlePostColor(NetworkStream stream, byte[] buffer, int bytesRead)
    {
        var body = ExtractBody(buffer, bytesRead);
        using var doc = JsonDocument.Parse(body);
        var color = doc.RootElement.GetProperty("color").GetString() ?? "";
        if (!ColorTheme.All.TryGetValue(color, out var theme))
            return RespondJson(stream, 400, new { success = false, error = "invalid color. use red, yellow, green, or blue" });

        _engine.SetTheme(theme);
        _form.StartAnimation();
        return RespondJson(stream, 200, new { success = true, color = theme.Name });
    }

    private Task HandleStartStop(NetworkStream stream, bool start)
    {
        if (start)
            _form.StartAnimation();
        else
            _form.StopAnimation();
        return RespondJson(stream, 200, new { success = true, running = start });
    }

    private Task HandlePostSpeed(NetworkStream stream, byte[] buffer, int bytesRead)
    {
        var body = ExtractBody(buffer, bytesRead);
        using var doc = JsonDocument.Parse(body);
        var speed = doc.RootElement.GetProperty("speed").GetDouble();
        _engine.Speed = (float)Math.Clamp(speed, 0.1, 5.0);
        _form.StartAnimation();
        return RespondJson(stream, 200, new { success = true, speed = _engine.Speed });
    }

    private Task HandlePostMode(NetworkStream stream, byte[] buffer, int bytesRead)
    {
        var body = ExtractBody(buffer, bytesRead);
        using var doc = JsonDocument.Parse(body);
        var modeStr = doc.RootElement.GetProperty("mode").GetString() ?? "";
        if (!Enum.TryParse<LightMode>(modeStr, ignoreCase: true, out var mode))
            return RespondJson(stream, 400, new { success = false, error = "invalid mode. use steady, marquee, or blink" });

        _engine.Mode = mode;
        _form.StartAnimation();
        return RespondJson(stream, 200, new { success = true, mode = mode.ToString().ToLowerInvariant() });
    }

    private static string ExtractBody(byte[] buffer, int bytesRead)
    {
        var request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        var parts = request.Split("\r\n\r\n");
        return parts.Length > 1 ? parts[1].Trim() : "";
    }

    private static async Task Respond(NetworkStream stream, int code, string text)
    {
        var response = $"HTTP/1.1 {code} {(code == 200 ? "OK" : "Error")}\r\n" +
                       "Content-Type: text/plain\r\n" +
                       $"Content-Length: {Encoding.UTF8.GetByteCount(text)}\r\n" +
                       "Connection: close\r\n\r\n" + text;
        await stream.WriteAsync(Encoding.UTF8.GetBytes(response));
    }

    private static async Task RespondJson(NetworkStream stream, int code, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var response = $"HTTP/1.1 {code} {(code == 200 ? "OK" : "Error")}\r\n" +
                       "Content-Type: application/json\r\n" +
                       $"Content-Length: {Encoding.UTF8.GetByteCount(json)}\r\n" +
                       "Connection: close\r\n\r\n" + json;
        await stream.WriteAsync(Encoding.UTF8.GetBytes(response));
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _cts.Cancel();
            try { _listener?.Stop(); } catch { }
        }
    }
}
