namespace Ticketdesk.Mcp.Tests;

using ModelContextProtocol.Client;

/// <summary>
/// Starts the built server as its own process and connects to it over stdio, the way an MCP host such as
/// Claude Code does. Nothing is faked: the tests see what a host would see.
/// </summary>
public static class Server
{
    public static async Task<McpClient> Start()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(root.FullName, "Ticketdesk.Mcp.slnx")))
        {
            root = root.Parent ?? throw new InvalidOperationException("Could not find Ticketdesk.Mcp.slnx.");
        }

        var dll = Path.Combine(root.FullName, "src", "Ticketdesk.Mcp", "bin", "Debug", "net10.0", "Ticketdesk.Mcp.dll");
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "ticketdesk",
            Command = "dotnet",
            Arguments = [dll],
        });
        return await McpClient.CreateAsync(transport);
    }
}
