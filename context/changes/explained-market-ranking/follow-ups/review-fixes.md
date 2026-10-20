# Implementation Review Follow-ups

## F1 — Activate an approved grounded AI provider

- **Status**: Deferred
- **Reason**: The S-03 plan requires real grounded AI, but no approved provider, SDK, endpoint contract, or secret-resolution mechanism was specified.
- **Required next step**: Select the approved provider and define its configuration/secret boundary, then implement a provider-neutral adapter that replaces `UnavailableAnalysisSummaryGenerator` only when valid configuration is present.
- **Current behavior**: The application fails closed to an unavailable summary while preserving ranking and deterministic evidence.
