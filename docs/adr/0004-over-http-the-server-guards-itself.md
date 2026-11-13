# 0004 Over HTTP, the server guards itself

Status: accepted

## Context

Over stdio, the only process that can talk to the server is the host that started it. That is the right
transport for one person's tools. A team that wants one server, kept up to date in one place, needs
Streamable HTTP, and over HTTP anything that can reach the port can send it requests: another process
on the machine, a container, or a web page open in the user's browser, which can make requests to
`localhost` and, with DNS rebinding, read the answers.

## Decision

`--http` serves the same tools over Streamable HTTP at `/mcp`, through `ModelContextProtocol.AspNetCore`.
Before a request reaches the protocol, `HttpGuard` checks:

1. **Origin.** A request with an `Origin` header is refused (403) unless that origin is listed in
   `TICKETDESK_MCP_ALLOWED_ORIGINS`. A host that is not a browser sends no `Origin` and is unaffected.
2. **Bearer token.** Every request must carry the token the server was started with, compared in
   constant time; anything else is refused (401) with `WWW-Authenticate: Bearer`.

The server binds to `127.0.0.1` only, and refuses to start in HTTP mode without a token of at least 32
characters, so the open configuration cannot be the one you get by accident.

Stdio mode sends every log line to standard error, because standard output carries the protocol.

## Consequences

- The same tools and tests serve both transports; only the guard is HTTP-specific, and it is tested by
  sending raw requests, so it is checked independently of any client's behaviour.
- A static token is the simplest credential that is not none. A server serving people outside one team
  would use OAuth, which the SDK supports, behind a reverse proxy that terminates TLS.
- Loopback only means a team member reaches it through whatever puts it on the network (a proxy, a
  tunnel, a sidecar), and that layer owns TLS.
