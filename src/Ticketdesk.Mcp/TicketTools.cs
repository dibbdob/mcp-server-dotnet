namespace Ticketdesk.Mcp;

using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using ModelContextProtocol;
using ModelContextProtocol.Server;

/// <summary>What a search returns for each match: enough to choose a ticket, not its full text.</summary>
public sealed record TicketSummary(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("priority")] string Priority);

[JsonConverter(typeof(JsonStringEnumConverter<TicketStatus>))]
public enum TicketStatus
{
    [JsonStringEnumMemberName("open")] Open,
    [JsonStringEnumMemberName("waiting")] Waiting,
    [JsonStringEnumMemberName("closed")] Closed,
}

/// <summary>
/// The tools a model sees. Each name says what it does to what, each description says when to use it and
/// when to use another, and every parameter is described, because the description is all the model has.
/// </summary>
[McpServerToolType]
public sealed partial class TicketTools(TicketStore store)
{
    [McpServerTool(Name = "search_tickets", Title = "Search tickets")]
    [Description(
        "Find support tickets whose subject or body contains the given words, optionally narrowed by status. " +
        "Returns each match's id, subject, status and priority, not its text: call get_ticket with an id to read one. " +
        "Use this when you do not already know a ticket's id.")]
    public IReadOnlyList<TicketSummary> SearchTickets(
        [Description("Words to look for in the subject or body. Leave empty to list tickets by status alone.")] string query = "",
        [Description("Only tickets in this status. Omit for every status.")] TicketStatus? status = null) =>
        [.. store.All()
            .Where(t => status is null || t.Status == Name(status.Value))
            .Where(t => query.Length == 0 ||
                        t.Subject.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        t.Body.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(t => new TicketSummary(t.Id, t.Subject, t.Status, t.Priority))];

    [McpServerTool(Name = "get_ticket", Title = "Read a ticket")]
    [Description(
        "Read one support ticket in full: the customer's subject and message, its status, plan, priority, when it " +
        "was opened, and the team's internal notes. Use this once you have an id; use search_tickets to find one.")]
    public Ticket GetTicket(
        [Description("The ticket's id, such as T-1001.")] string ticketId) => this.Require(ticketId);

    [McpServerTool(Name = "add_internal_note", Title = "Add an internal note")]
    [Description(
        "Add a note to a ticket that only the support team can see. The customer never sees it, and it does not " +
        "change the ticket's status or priority. Use it to record what you found. It is not a way to reply to " +
        "the customer.")]
    public Ticket AddInternalNote(
        [Description("The ticket's id, such as T-1001.")] string ticketId,
        [Description("The note, in plain text, at most 2000 characters. Say what you found and how you know it.")] string note)
    {
        var ticket = this.Require(ticketId);
        if (string.IsNullOrWhiteSpace(note))
        {
            throw new McpException("The note is empty. Write what you found, then call add_internal_note again.");
        }

        if (note.Length > MaxNoteLength)
        {
            throw new McpException($"The note is {note.Length} characters; the limit is {MaxNoteLength}. Shorten it and try again.");
        }

        return store.AddNote(ticket.Id, note.Trim());
    }

    [McpServerTool(Name = "get_response_deadline", Title = "When a reply is due")]
    [Description(
        "When the customer must have a first response by, from the ticket's priority and the time it was opened, " +
        "counting business hours only (Monday to Friday, 09:00 to 17:00 UTC). Use it to tell whether a ticket is " +
        "overdue or how long is left.")]
    public object GetResponseDeadline(
        [Description("The ticket's id, such as T-1001.")] string ticketId)
    {
        var ticket = this.Require(ticketId);
        var due = ResponseDeadline.DueAt(ticket.OpenedAt, ticket.Priority);
        return new { ticket_id = ticket.Id, priority = ticket.Priority, opened_at = ticket.OpenedAt, due_at = due };
    }

    public const int MaxNoteLength = 2000;

    /// <summary>
    /// The ticket, or an error the model can act on. A thrown McpException becomes a tool result with
    /// isError set and this message, so the model can correct itself. Any other exception becomes a
    /// generic message with no detail, which is right for the unexpected and useless for the expected.
    /// </summary>
    private Ticket Require(string ticketId)
    {
        if (!TicketId().IsMatch(ticketId))
        {
            throw new McpException($"'{ticketId}' is not a ticket id. Ids look like T-1001; use search_tickets to find one.");
        }

        return store.Find(ticketId)
            ?? throw new McpException($"There is no ticket {ticketId}. Use search_tickets to find the right id.");
    }

    private static string Name(TicketStatus status) => status.ToString().ToLowerInvariant();

    [GeneratedRegex("^T-[0-9]{4}$", RegexOptions.IgnoreCase)]
    private static partial Regex TicketId();
}
