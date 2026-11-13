namespace Ticketdesk.Mcp.Tests;

/// <summary>
/// What a model sees when a call cannot succeed. An error it can act on says what was wrong and what to do
/// next, and comes back as a tool result, not a protocol failure, so the model can correct itself.
/// </summary>
public sealed class ErrorTests
{
    private static async Task<ModelContextProtocol.Protocol.CallToolResult> Call(string tool, Dictionary<string, object?> args)
    {
        await using var client = await Server.Start();
        return await client.CallToolAsync(tool, args);
    }

    [Test]
    public async Task A_ticket_that_does_not_exist_is_named_and_the_next_step_suggested()
    {
        var result = await Call("get_ticket", new() { ["ticketId"] = "T-9999" });

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(ToolDesignTests.Text(result)).Contains("There is no ticket T-9999").And.Contains("search_tickets");
    }

    [Test]
    [Arguments("1001")]
    [Arguments("ticket 1001")]
    [Arguments("T-10O1")]
    public async Task Something_that_is_not_an_id_is_told_what_an_id_looks_like(string id)
    {
        var result = await Call("get_ticket", new() { ["ticketId"] = id });

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(ToolDesignTests.Text(result)).Contains("Ids look like T-1001");
    }

    [Test]
    public async Task An_empty_note_is_refused_and_nothing_is_written()
    {
        await using var client = await Server.Start();

        var result = await client.CallToolAsync("add_internal_note", new Dictionary<string, object?> { ["ticketId"] = "T-1002", ["note"] = "   " });
        var ticket = await client.CallToolAsync("get_ticket", new Dictionary<string, object?> { ["ticketId"] = "T-1002" });

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(ToolDesignTests.Text(result)).Contains("The note is empty");
        await Assert.That(ToolDesignTests.Text(ticket)).Contains("\"notes\":[]");
    }

    [Test]
    public async Task A_note_over_the_limit_is_refused_with_its_length_and_the_limit()
    {
        var result = await Call("add_internal_note", new() { ["ticketId"] = "T-1002", ["note"] = new string('x', 2001) });

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(ToolDesignTests.Text(result)).Contains("2001 characters").And.Contains("2000");
    }

    [Test]
    public async Task A_note_is_written_when_it_is_valid()
    {
        await using var client = await Server.Start();

        var result = await client.CallToolAsync("add_internal_note", new Dictionary<string, object?> { ["ticketId"] = "t-1002", ["note"] = "Reproduced on the staging dashboard." });

        await Assert.That(result.IsError).IsNotEqualTo(true);
        await Assert.That(ToolDesignTests.Text(result)).Contains("Reproduced on the staging dashboard.");
    }
}
