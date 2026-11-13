namespace Ticketdesk.Mcp.Tests;

using System.Diagnostics;
using System.Text;
using ModelContextProtocol.Client;

/// <summary>The built server, started in HTTP mode on a port it chose itself, stopped when disposed.</summary>
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

    /// <summary>
    /// Start the server on a port the operating system chooses as the server binds it (port 0), and wait for
    /// the server to say which. A port chosen here and released before the server bound it could be taken by
    /// something else in between, which made these tests fail now and then.
    /// </summary>
    public static async Task<HttpServer> Start(string allowedOrigins = "")
    {
        var process = Launch(["--http", "--port", "0"],
            new() { ["TICKETDESK_MCP_TOKEN"] = Token, ["TICKETDESK_MCP_ALLOWED_ORIGINS"] = allowedOrigins });
        Drain(process.StandardOutput);

        var seen = new StringBuilder();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            while (await process.StandardError.ReadLineAsync(timeout.Token) is { } line)
            {
                if (line.StartsWith(Listening, StringComparison.Ordinal))
                {
                    Drain(process.StandardError);
                    return new HttpServer(process, new Uri(line[Listening.Length..]));
                }

                seen.AppendLine(line);
            }
        }
        catch (OperationCanceledException)
        {
        }

        process.Kill(entireProcessTree: true);
        throw new InvalidOperationException($"The HTTP server did not say it was listening. It wrote:\n{seen}");
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

    private const string Listening = "Listening on ";

    /// <summary>
    /// Keep reading what the server writes. Nobody else reads it, and a pipe nobody empties fills up and makes
    /// the server wait, mid-request, for a reader that never comes.
    /// </summary>
    private static void Drain(StreamReader output) => _ = output.BaseStream.CopyToAsync(Stream.Null);

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
