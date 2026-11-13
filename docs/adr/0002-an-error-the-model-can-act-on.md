# 0002 An error the model can act on

Status: accepted

## Context

Asking the first server for a ticket that did not exist returned "An error occurred invoking 'get'."
The SDK turns any unhandled exception in a tool into a result with `isError` set and that generic text,
which is the right behaviour for the unexpected, because the exception's message might carry a path, a
connection string or a stack, and the wrong behaviour for the expected. A model told only that something
failed retries the same call, gives up, or invents an answer.

## Decision

Expected failures are thrown as `McpException`, whose message the SDK passes to the model as the tool
result's text, with `isError` set. Each message says what was wrong and what to do next:

- not an id: "'1001' is not a ticket id. Ids look like T-1001; use search_tickets to find one."
- no such ticket: "There is no ticket T-9999. Use search_tickets to find the right id."
- an empty note, or one over 2000 characters, with its length and the limit.

They are tool results, not protocol errors, so the model sees them in the conversation and can correct
its next call. Validation happens before any write, so a refused note writes nothing.

Unexpected failures are left to the SDK's generic message. Nothing internal reaches the model.

## Consequences

- A model that passes `1001`, or misremembers an id, recovers in one step without a person.
- The rule for a new tool is: anything a caller could get wrong is an `McpException` with a next step;
  anything else is a bug and stays generic.
- Ids are matched case-insensitively (`t-1002` works), because a model's casing is not worth a failure.
