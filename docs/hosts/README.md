# Connecting a host

## Claude Code, in this repository (stdio, project scope)

`.mcp.json` at the repository root is checked in, so everyone who opens the repository in Claude Code is
offered the `ticketdesk` server. Claude Code asks before using a project-scoped server the first time.
Build first, because the entry runs the built server:

```bash
./build.sh
claude mcp list
```

## Claude Code, anywhere (stdio, user scope)

To have the server in every project on this machine, add it at user scope with an absolute path:

```bash
claude mcp add --scope user ticketdesk -- dotnet /absolute/path/to/src/Ticketdesk.Mcp/bin/Debug/net10.0/Ticketdesk.Mcp.dll
```

## A shared server over HTTP

Start it once, with a long random token:

```bash
export TICKETDESK_MCP_TOKEN="$(openssl rand -hex 32)"
dotnet src/Ticketdesk.Mcp/bin/Debug/net10.0/Ticketdesk.Mcp.dll --http --port 5077
```

Each person's host then connects with the same token in its own environment.
[`shared-http.mcp.json`](shared-http.mcp.json) is the entry; `${TICKETDESK_MCP_TOKEN}` is expanded from
the environment when Claude Code reads the file, so the token itself is never written into a checked-in
file. Or, at user scope:

```bash
claude mcp add --scope user --transport http ticketdesk http://127.0.0.1:5077/mcp --header "Authorization: Bearer $TICKETDESK_MCP_TOKEN"
```

A browser-based host must also be listed in `TICKETDESK_MCP_ALLOWED_ORIGINS` (comma-separated), or the
server refuses its requests.

## Which scope

| Scope | Where it is written | Who gets it |
|---|---|---|
| project | `.mcp.json`, checked in | everyone working in this repository, after approving it |
| user | your own Claude Code config | you, in every project |
| local | your own config, for this project only | you, here |
