#!/usr/bin/env bash
# The whole gate. The tests start the real server and talk to it over the protocol.
set -euo pipefail
cd "$(dirname "$0")"

echo "== build (warnings are errors)"
dotnet build Ticketdesk.Mcp.slnx --nologo -v quiet -clp:ErrorsOnly

echo "== tests, against the running server"
dotnet test --project tests/Ticketdesk.Mcp.Tests --no-build 2>&1 | tail -n 6

echo "== build passed"
