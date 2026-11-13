namespace Ticketdesk.Mcp.Tests;

using System.Text.Json;
using ModelContextProtocol.Client;

/// <summary>The checked-in host configuration starts the server it describes, and leaks no secret.</summary>
public sealed class HostConfigurationTests
{
    private static string Root()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(root.FullName, "Ticketdesk.Mcp.slnx")))
        {
            root = root.Parent!;
        }

        return root.FullName;
    }

    [Test]
    public async Task The_project_entry_starts_a_server_with_the_four_tools()
    {
        // Run exactly what .mcp.json says, from the repository root, as Claude Code would.
        var entry = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(Root(), ".mcp.json")))
            .RootElement.GetProperty("mcpServers").GetProperty("ticketdesk");
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "ticketdesk",
            Command = entry.GetProperty("command").GetString()!,
            Arguments = [.. entry.GetProperty("args").EnumerateArray().Select(a => a.GetString()!)],
            WorkingDirectory = Root(),
        });

        await using var client = await McpClient.CreateAsync(transport);

        await Assert.That((await client.ListToolsAsync()).Count).IsEqualTo(4);
    }

    [Test]
    public async Task The_shared_entry_reads_its_token_from_the_environment_and_never_holds_one()
    {
        var text = await File.ReadAllTextAsync(Path.Combine(Root(), "docs", "hosts", "shared-http.mcp.json"));
        var header = JsonDocument.Parse(text).RootElement.GetProperty("mcpServers").GetProperty("ticketdesk")
            .GetProperty("headers").GetProperty("Authorization").GetString();

        await Assert.That(header).IsEqualTo("Bearer ${TICKETDESK_MCP_TOKEN}");
    }
}
