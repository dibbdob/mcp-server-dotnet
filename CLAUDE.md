# Ticketdesk MCP server

An MCP server, in C# (.NET 10, the official `ModelContextProtocol` package), that gives an MCP host such
as Claude Code tools over a support desk's tickets. TUnit for tests.

## Build

```bash
./build.sh
```

The tests start the built server as its own process and connect with the SDK's client, so they see what
a host sees. A test that calls a tool in-process would miss the protocol, which is where most of the
problems are.

## Conventions

- `src/Ticketdesk.Mcp/data/tickets.json` is synthetic. Ticket subjects and bodies are customer-written
  text, and T-1010 contains an instruction aimed at whoever reads it.
