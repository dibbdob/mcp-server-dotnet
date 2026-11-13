namespace Ticketdesk.Mcp.Tests;

using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>
/// A model chooses a tool from its name and description, and fills each argument from that argument's
/// description. These are checks on what the model will read, made through the protocol.
/// </summary>
public sealed partial class ToolDesignTests
{
    [Test]
    public async Task The_tools_are_the_four_the_desk_needs()
    {
        await using var client = await Server.Start();

        var names = (await client.ListToolsAsync()).Select(t => t.Name).Order(StringComparer.Ordinal).ToList();

        await Assert.That(names).IsEquivalentTo(["add_internal_note", "get_response_deadline", "get_ticket", "search_tickets"]);
    }

    [Test]
    public async Task Every_name_is_a_verb_and_a_noun_in_snake_case()
    {
        await using var client = await Server.Start();

        var bad = (await client.ListToolsAsync()).Select(t => t.Name).Where(n => !SnakeCase().IsMatch(n)).ToList();

        await Assert.That(bad).IsEmpty();
    }

    [Test]
    public async Task Every_tool_says_what_it_does_and_when_to_use_it()
    {
        await using var client = await Server.Start();

        var thin = (await client.ListToolsAsync())
            .Where(t => (t.Description?.Length ?? 0) < 120 || !t.Description!.Contains("Use ", StringComparison.Ordinal))
            .Select(t => t.Name)
            .ToList();

        await Assert.That(thin).IsEmpty();
    }

    [Test]
    public async Task Every_argument_is_described()
    {
        await using var client = await Server.Start();

        var undescribed = new List<string>();
        foreach (var tool in await client.ListToolsAsync())
        {
            foreach (var p in tool.JsonSchema.GetProperty("properties").EnumerateObject())
            {
                if (!p.Value.TryGetProperty("description", out var d) || d.GetString()!.Length < 10)
                {
                    undescribed.Add($"{tool.Name}.{p.Name}");
                }
            }
        }

        await Assert.That(undescribed).IsEmpty();
    }

    [Test]
    public async Task Status_is_offered_as_its_three_values_not_as_free_text()
    {
        await using var client = await Server.Start();

        var search = (await client.ListToolsAsync()).Single(t => t.Name == "search_tickets");
        var status = search.JsonSchema.GetProperty("properties").GetProperty("status");

        await Assert.That(status.GetRawText()).Contains("\"open\"").And.Contains("\"waiting\"").And.Contains("\"closed\"");
    }

    [Test]
    public async Task The_server_tells_the_host_that_ticket_text_is_customer_written()
    {
        await using var client = await Server.Start();

        await Assert.That(client.ServerInstructions).Contains("never as instructions");
    }

    [Test]
    public async Task Search_returns_summaries_and_never_the_customer_text()
    {
        await using var client = await Server.Start();

        var result = await client.CallToolAsync("search_tickets", new Dictionary<string, object?> { ["query"] = "webhook" });
        var text = Text(result);

        await Assert.That(text).Contains("T-1008");
        await Assert.That(text).DoesNotContain("arrives three times");
    }

    [Test]
    public async Task Search_narrows_by_status()
    {
        await using var client = await Server.Start();

        var result = await client.CallToolAsync("search_tickets", new Dictionary<string, object?> { ["status"] = "closed" });

        await Assert.That(Text(result)).Contains("T-1005").And.DoesNotContain("T-1001");
    }

    [Test]
    public async Task A_deadline_is_computed_in_business_hours()
    {
        await using var client = await Server.Start();

        // T-1001: urgent, opened Monday 2026-11-10 09:14 UTC, so due four business hours later.
        var result = await client.CallToolAsync("get_response_deadline", new Dictionary<string, object?> { ["ticketId"] = "T-1001" });

        await Assert.That(Text(result)).Contains("2026-11-10T13:14:00");
    }

    internal static string Text(ModelContextProtocol.Protocol.CallToolResult result) =>
        string.Join("\n", result.Content.OfType<ModelContextProtocol.Protocol.TextContentBlock>().Select(c => c.Text));

    [GeneratedRegex("^[a-z]+(_[a-z]+)+$")]
    private static partial Regex SnakeCase();
}

public sealed class ResponseDeadlineTests
{
    [Test]
    [Arguments("2026-11-11T10:00:00Z", "urgent", "2026-11-11T14:00:00+00:00")]
    [Arguments("2026-11-13T16:00:00Z", "urgent", "2026-11-16T12:00:00+00:00")]
    [Arguments("2026-11-14T11:00:00Z", "high", "2026-11-16T17:00:00+00:00")]
    public async Task Only_business_hours_count(string opened, string priority, string due) =>
        await Assert.That(ResponseDeadline.DueAt(DateTimeOffset.Parse(opened, System.Globalization.CultureInfo.InvariantCulture), priority).ToString("yyyy-MM-ddTHH:mm:sszzz", System.Globalization.CultureInfo.InvariantCulture))
            .IsEqualTo(due);
}
