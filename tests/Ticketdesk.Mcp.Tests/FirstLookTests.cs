namespace Ticketdesk.Mcp.Tests;

using ModelContextProtocol.Protocol;

public sealed class FirstLookTests
{
    [Test]
    public async Task The_server_lists_its_tools()
    {
        await using var client = await Server.Start();

        var tools = await client.ListToolsAsync();

        await Assert.That(tools.Select(t => t.Name)).IsNotEmpty();
    }

    [Test]
    public async Task What_a_host_sees_when_a_ticket_does_not_exist()
    {
        await using var client = await Server.Start();
        var name = (await client.ListToolsAsync()).Select(t => t.Name).Single(n => n.Contains("get", StringComparison.OrdinalIgnoreCase));

        var result = await client.CallToolAsync(name, new Dictionary<string, object?> { ["id"] = "T-9999" });

        Console.WriteLine($"tools: {string.Join(", ", (await client.ListToolsAsync()).Select(t => $"{t.Name}: {t.Description}"))}");
        Console.WriteLine($"isError={result.IsError} text={string.Join(" ", result.Content.OfType<TextContentBlock>().Select(c => c.Text))}");
        await Assert.That(result.IsError).IsTrue();
    }
}
