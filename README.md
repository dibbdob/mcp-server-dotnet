# An MCP server in C#

An MCP server, built on the official C# SDK (`ModelContextProtocol` 2.4.0, .NET 10), that gives a host
such as Claude Code four tools over a support desk's tickets: search them, read one, add an internal
note, and work out when a reply is due in business hours. It runs over stdio for one person, or over
Streamable HTTP for a team.

The tickets are the same fictional desk as the sibling repositories: the deadline rules are the same as
the first one's, and the tickets are the kind the second one extracts.

## The history is the point

Each stage is a branch merged into `main` with a merge message written like a pull request. The first
stage includes a test that records what a host actually saw, through the protocol, and each stage after
it answers something that left unguarded.

```bash
git log --first-parent --reverse --format='%h %s' main
git log --graph --format='%h %s%n%b' main
```

| # | Stage | The problem it answers |
|---|---|---|
| 1 | A stdio server tested through a real client | (what a host sees: tools named `get`, `search`, `update` with no descriptions, and "An error occurred invoking 'get'.") |
| 2 | `tool-design` | A model cannot choose between tools it cannot tell apart |
| 3 | `actionable-errors` | A failed call told the model nothing it could act on |
| 4 | `annotations-and-untrusted-text` | A host could not tell which tools write, and customer text looked like the desk's own |
| 5 | `http-transport` | One person's stdio server cannot be shared, and an HTTP one is reachable by anything on the port |
| 6 | `host-configuration` | Nobody's host knew the server existed |
| 7 | `server-announces-its-port` | The HTTP tests failed now and then: a port picked and released by the test could be taken before the server bound it |

## What it demonstrates

| Area | Where to look |
|---|---|
| **Tool design**: verb-noun names, descriptions that say when to use each tool, every argument described, enums | [`TicketTools`](src/Ticketdesk.Mcp/TicketTools.cs), [ADR 0001](docs/adr/0001-a-tool-is-chosen-by-its-description.md), [tests](tests/Ticketdesk.Mcp.Tests/ToolDesignTests.cs) |
| **Tool boundaries**: summaries from search, full text from get, domain logic on the server, no destructive tool offered | [`TicketTools`](src/Ticketdesk.Mcp/TicketTools.cs), [`ResponseDeadline`](src/Ticketdesk.Mcp/ResponseDeadline.cs) |
| **Structured errors**: `isError` results with a next step; unexpected failures kept generic | [ADR 0002](docs/adr/0002-an-error-the-model-can-act-on.md), [tests](tests/Ticketdesk.Mcp.Tests/ErrorTests.cs) |
| **Tool annotations**: read-only, destructive, idempotent, open-world hints | [ADR 0003](docs/adr/0003-tell-the-host-what-it-needs-to-keep-the-model-safe.md), [tests](tests/Ticketdesk.Mcp.Tests/HostSafetyTests.cs) |
| **Untrusted content**: customer text returned apart and labelled; server instructions say so | [`TicketView`](src/Ticketdesk.Mcp/TicketTools.cs), ticket T-1010 in [the data](src/Ticketdesk.Mcp/data/tickets.json) |
| **Bounded results**: a limit, a count of what was left out, and what to do about it | [`SearchTickets`](src/Ticketdesk.Mcp/TicketTools.cs) |
| **Transports**: stdio (logging kept off standard output) and Streamable HTTP | [`Program`](src/Ticketdesk.Mcp/Program.cs) |
| **HTTP security**: Origin allowlist against DNS rebinding, constant-time bearer token, loopback only, fail closed | [`HttpGuard`](src/Ticketdesk.Mcp/HttpGuard.cs), [ADR 0004](docs/adr/0004-over-http-the-server-guards-itself.md), [tests](tests/Ticketdesk.Mcp.Tests/HttpTransportTests.cs) |
| **Host configuration**: project-scope `.mcp.json`, user scope, a shared HTTP entry with the token from the environment | [`.mcp.json`](.mcp.json), [`docs/hosts`](docs/hosts/README.md), [tests](tests/Ticketdesk.Mcp.Tests/HostConfigurationTests.cs) |
| **Testing through the protocol**: every test starts the real server and talks to it with the SDK's client | [`Server`](tests/Ticketdesk.Mcp.Tests/Server.cs), [`HttpServer`](tests/Ticketdesk.Mcp.Tests/HttpServer.cs) |

## Run it

Needs the .NET 10 SDK.

```bash
./build.sh
```

37 tests, each against the running server over stdio or HTTP, with warnings as errors.

Then open this folder in Claude Code. It offers the `ticketdesk` server from `.mcp.json` and asks you to
approve it the first time. For other hosts, user scope and a shared HTTP server, see
[`docs/hosts`](docs/hosts/README.md).

## What has and has not been verified

- **Verified by the build, through the protocol:** the tool list and every description and schema; each
  error a caller can cause; the annotations; customer text labelled and kept apart; search limits; the
  same tools over HTTP; the guard's 401 and 403, by raw request; the refusal to serve HTTP without a
  token; and that the command in `.mcp.json` starts the server. A mutation removing the Origin check
  failed a test.
- **Verified with Claude Code:** `claude mcp list` finds the project server and lists it as pending
  approval.
- **Not yet verified:** a model actually using the tools in a session, which needs the server approved;
  and any host other than Claude Code.

## Layout

```
.mcp.json                 the project-scope entry Claude Code reads
docs/adr/                 why each decision was made
docs/hosts/               connecting hosts, scopes, the shared HTTP entry
src/Ticketdesk.Mcp/       the server: tools, store, deadline rules, HTTP guard
  data/tickets.json       twelve synthetic tickets
tests/Ticketdesk.Mcp.Tests/   every test, against the running server
build.sh                  the gate
```
