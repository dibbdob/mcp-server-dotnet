# 0001 A tool is chosen by its description

Status: accepted

## Context

A model deciding which tool to call has the tool's name, its description and its input schema, and
nothing else. The first server exposed `get`, `search` and `update` with empty descriptions, which a test
recorded through the protocol. A model given those has to guess what `update` updates, whether `search`
returns whole tickets, and what an `id` looks like.

## Decision

- **Names are a verb and a noun:** `search_tickets`, `get_ticket`, `add_internal_note`,
  `get_response_deadline`. `update` became `add_internal_note` because that is all it does; a name that
  promises more invites the model to try more.
- **Each description says what the tool does, what it returns, and when to use it instead of another.**
  `search_tickets` says it returns summaries and points at `get_ticket` for the text; `add_internal_note`
  says the customer never sees it and that it is not a way to reply.
- **Every argument is described,** with an example where the format matters (`T-1001`), and a closed set
  of values is an enum in the schema, not free text.
- **Search returns summaries, not bodies.** A list of twelve full tickets fills the context with text the
  model did not ask for; a list of ids and subjects lets it choose what to read.
- **The server's instructions say that ticket text is customer-written,** so a host can tell the model so
  before any tool is called.
- **Domain logic lives in the server.** The deadline in business hours is computed by
  `get_response_deadline`, not left for the model to work out from a priority and a timestamp.

The build checks the names, the length and content of each description, every argument's description,
the status enum and the server instructions, all through the protocol.

## Consequences

- The checks keep the next tool honest: one added without a description fails the build.
- A description is a prompt, and long descriptions cost context in every conversation that loads the
  server. Four tools with a paragraph each is a deliberate size; forty would need tool search instead.
