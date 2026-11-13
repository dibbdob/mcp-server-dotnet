#!/usr/bin/env bash
# The whole gate. The tests start the real server and talk to it over the protocol.
set -euo pipefail
cd "$(dirname "$0")"

echo "== build (warnings are errors)"
dotnet build Ticketdesk.Mcp.slnx --nologo -v quiet -clp:ErrorsOnly

echo "== tests, against the running server"
out="$(mktemp)"
trap 'rm -f "$out"' EXIT
if ! dotnet test --project tests/Ticketdesk.Mcp.Tests --no-build >"$out" 2>&1; then
  # The summary says how many failed, not which. Name them, so a failure that happens only sometimes can
  # still be traced to its test.
  grep -E '^failed ' "$out" || true
  tail -n 6 "$out"
  exit 1
fi
tail -n 6 "$out"

echo "== build passed"
