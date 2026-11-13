namespace Ticketdesk.Mcp.Tests;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using ModelContextProtocol.Client;

/// <summary>The built server, started in HTTP mode on a free loopback port, stopped when disposed.</summary>
public sealed class HttpServer : IAsyncDisposable
{
    public const string Token = "test-token-0123456789abcdefghijklmnopqrstuv";

    private readonly Process process;

    private HttpServer(Process process, Uri endpoint)
    {
        this.process = process;
        this.Endpoint = endpoint;
    }

    public Uri Endpoint { get; }

    public static Process Launch(string[] args, Dictionary<string, string?> environment)
    {
        var info = new ProcessStartInfo("dotnet") { RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false };
        info.ArgumentList.Add(Dll());
        foreach (var a in args)
        {
            info.ArgumentList.Add(a);
        }

        foreach (var (k, v) in environment)
        {
            info.Environment[k] = v;
        }

        return Process.Start(info)!;
    }

    public static async Task<HttpServer> Start(string allowedOrigins = "")
    {
        var port = FreePort();
        var process = Launch(["--http", "--port", port.ToString(System.Globalization.CultureInfo.InvariantCulture)],
            new() { ["TICKETDESK_MCP_TOKEN"] = Token, ["TICKETDESK_MCP_ALLOWED_ORIGINS"] = allowedOrigins });
        var endpoint = new Uri($"http://127.0.0.1:{port}/mcp");

        for (var i = 0; i < 100; i++)
        {
            try
            {
                using var probe = new TcpClient();
                await probe.ConnectAsync(IPAddress.Loopback, port);
                return new HttpServer(process, endpoint);
            }
            catch (SocketException)
            {
                await Task.Delay(100);
            }
        }

        process.Kill();
        throw new InvalidOperationException("The HTTP server did not start listening.");
    }

    public Task<McpClient> Connect() =>
        McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = this.Endpoint,
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = new Dictionary<string, string> { ["Authorization"] = $"Bearer {Token}" },
        }));

    public async ValueTask DisposeAsync()
    {
        this.process.Kill(entireProcessTree: true);
        await this.process.WaitForExitAsync();
        this.process.Dispose();
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string Dll()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(root.FullName, "Ticketdesk.Mcp.slnx")))
        {
            root = root.Parent!;
        }

        return Path.Combine(root.FullName, "src", "Ticketdesk.Mcp", "bin", "Debug", "net10.0", "Ticketdesk.Mcp.dll");
    }
}
