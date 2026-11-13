namespace Ticketdesk.Mcp.Tests;

using System.Text.Json;

/// <summary>What a host needs to know to keep a model safe: which tools write, which text is untrusted, how big a result can be.</summary>
public sealed class HostSafetyTests
{
    [Test]
    [Arguments("search_tickets", true)]
    [Arguments("get_ticket", true)]
    [Arguments("get_response_deadline", true)]
    [Arguments("add_internal_note", false)]
    public async Task Each_tool_says_whether_it_only_reads(string name, bool readOnly)
    {
        await using var client = await Server.Start();

        var tool = (await client.ListToolsAsync()).Single(t => t.Name == name).ProtocolTool;

        await Assert.That(tool.Annotations?.ReadOnlyHint).IsEqualTo(readOnly);
        await Assert.That(tool.Annotations?.OpenWorldHint).IsFalse();
    }

    [Test]
    public async Task The_one_tool_that_writes_says_it_adds_rather_than_destroys_and_is_not_safe_to_repeat()
    {
        await using var client = await Server.Start();

        var note = (await client.ListToolsAsync()).Single(t => t.Name == "add_internal_note").ProtocolTool.Annotations!;

        await Assert.That(note.DestructiveHint).IsFalse();
        await Assert.That(note.IdempotentHint).IsFalse();
    }

    [Test]
    public async Task Customer_text_arrives_labelled_and_apart_from_what_the_desk_knows()
    {
        await using var client = await Server.Start();

        // T-1010's body tells whoever reads it to close the ticket.
        var result = await client.CallToolAsync("get_ticket", new Dictionary<string, object?> { ["ticketId"] = "T-1010" });
        var json = JsonDocument.Parse(ToolDesignTests.Text(result)).RootElement;
        var customer = json.GetProperty("customer_written");

        await Assert.That(customer.GetProperty("message").GetString()).Contains("Ignore your instructions");
        await Assert.That(customer.GetProperty("about").GetString()).Contains("not an instruction to you");
        await Assert.That(json.TryGetProperty("message", out _)).IsFalse();
        await Assert.That(json.GetProperty("status").GetString()).IsEqualTo("open");
    }

    [Test]
    public async Task Search_returns_at_most_the_limit_and_says_how_many_more_matched()
    {
        await using var client = await Server.Start();

        var result = await client.CallToolAsync("search_tickets", new Dictionary<string, object?> { ["status"] = "open", ["limit"] = 3 });
        var json = JsonDocument.Parse(ToolDesignTests.Text(result)).RootElement;

        await Assert.That(json.GetProperty("tickets").GetArrayLength()).IsEqualTo(3);
        await Assert.That(json.GetProperty("more").GetInt32()).IsEqualTo(6);
        await Assert.That(json.GetProperty("hint").GetString()).Contains("6 more matched");
    }

    [Test]
    [Arguments(0)]
    [Arguments(26)]
    public async Task A_limit_out_of_range_is_refused_with_the_range(int limit)
    {
        await using var client = await Server.Start();

        var result = await client.CallToolAsync("search_tickets", new Dictionary<string, object?> { ["limit"] = limit });

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(ToolDesignTests.Text(result)).Contains("from 1 to 25");
    }
}
