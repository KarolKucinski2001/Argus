# Implementation review fixes

The full implementation review was triaged after the initial three-phase
implementation.

- F1: Fixed provider-result identity, history, date-range, and granularity
  validation before caching; added rejection and non-caching coverage.
- F2: Fixed provider-boundary exception normalization with stable categories;
  unexpected programming failures remain distinguishable.
- F3: Fixed request-budget enforcement with a process-local rolling
  one-minute window and documented the scale-out limitation.
- F4: Fixed empty-refresh handling to preserve expired evidence with explicit
  stale and upstream-unavailable signals.
- F5: Fixed missing deterministic coverage for over-limit, invalid-range,
  unavailable-provider, and options-validation paths.
- F6: Accepted as an intentional boundary decision; derived performance
  periods remain deferred to the analysis layer and are documented in the
  plan and market-data contract.
- F7: Fixed approved-but-unavailable provider activation with a fail-closed
  503 response.
- F8: Fixed asset ID, class, and display fallback canonicalization before
  provider and cache use.
