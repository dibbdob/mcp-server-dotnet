namespace Ticketdesk.Mcp;

using ModelContextProtocol.Server;

[McpServerToolType]
public sealed class TicketTools(TicketStore store)
{
    [McpServerTool]
    public Ticket Get(string id) => store.Get(id);

    [McpServerTool]
    public IReadOnlyList<Ticket> Search(string q) =>
        [.. store.All().Where(t => t.Subject.Contains(q, StringComparison.OrdinalIgnoreCase) || t.Body.Contains(q, StringComparison.OrdinalIgnoreCase))];

    [McpServerTool]
    public Ticket Update(string id, string note) => store.AddNote(id, note);
}
