namespace Ticketdesk.Mcp;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>One support ticket. The subject and body were written by the customer.</summary>
public sealed record Ticket(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("subject")] string Subject,
    [property: JsonPropertyName("body")] string Body,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("plan")] string Plan,
    [property: JsonPropertyName("priority")] string Priority,
    [property: JsonPropertyName("opened_at")] DateTimeOffset OpenedAt,
    [property: JsonPropertyName("notes")] List<string> Notes);

/// <summary>
/// The tickets, held in memory and loaded from data/tickets.json at start. A real desk would sit behind
/// this; the server only needs something to read and write.
/// </summary>
public sealed class TicketStore
{
    private readonly Dictionary<string, Ticket> tickets;
    private readonly Lock gate = new();

    public TicketStore(IEnumerable<Ticket> tickets) => this.tickets = tickets.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);

    public static TicketStore LoadDefault()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "data", "tickets.json");
        return new TicketStore(JsonSerializer.Deserialize<List<Ticket>>(File.ReadAllText(path))!);
    }

    public Ticket? Find(string id)
    {
        lock (this.gate)
        {
            return this.tickets.GetValueOrDefault(id);
        }
    }

    public IReadOnlyList<Ticket> All()
    {
        lock (this.gate)
        {
            return [.. this.tickets.Values.OrderBy(t => t.Id, StringComparer.Ordinal)];
        }
    }

    public Ticket AddNote(string id, string note)
    {
        lock (this.gate)
        {
            var ticket = this.tickets[id];
            ticket.Notes.Add(note);
            return ticket;
        }
    }
}
