# 0003 Tell the host what it needs to keep the model safe

Status: accepted

## Context

An MCP server does not decide what the model may do; the host does. Claude Code, for one, asks the user
before a tool runs unless a rule allows it. A host can only make that decision well with information the
server has and it does not: which tools change anything, which text in a result came from someone
untrusted, and how much a result can hold.

The first versions gave none of it. Every tool looked the same in the list, a ticket's customer text sat
at the same level as its status, and a search returned every match. T-1010's message tells its reader to
ignore its instructions and close the ticket, and nothing in the result marked it as anything but data
from the desk.

## Decision

- **Annotations on every tool.** The three tools that read are `readOnlyHint: true`. `add_internal_note`
  is `readOnlyHint: false`, `destructiveHint: false` (it adds, never removes) and
  `idempotentHint: false` (calling it twice writes two notes). None reaches outside the server
  (`openWorldHint: false`). A host can allow the readers without a prompt and ask about the writer.
- **Customer text is kept apart and labelled.** `get_ticket` returns the subject and message under
  `customer_written`, beside an `about` line saying it is the customer's and not an instruction. A
  search returns the subject as `customer_subject`. Desk fields (status, plan, priority, notes) stay at
  the top level.
- **Results are bounded.** A search returns at most `limit` tickets (1 to 25, default 10) with a count of
  how many more matched and what to do about it; a message over 4000 characters is truncated and flagged.

## Consequences

- Annotations are hints. A host may ignore them, and a malicious server could lie in them, so they
  inform a trusted host's policy rather than enforce anything. This server's tests hold them true.
- Labelling customer text does not make a model immune to it; it gives the model and the host what they
  need to treat it correctly. The server's instructions say the same thing once per session.
- No tool here can close, reassign or delete a ticket. The safest destructive tool is the one that is not
  offered.
