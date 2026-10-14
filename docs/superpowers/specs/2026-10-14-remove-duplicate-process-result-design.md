# Remove Duplicate Process Result Design

## Context

The landing page currently presents an illustrative ranking in the hero and a
second, similar ranking panel below the "Jak to działa" process cards. The
second panel repeats the same visual story and makes the process section feel
crowded rather than clearer.

## Decision

Remove the secondary `process-result` panel. Keep the hero ranking as the
landing page's single illustrative result and leave the process section as
three workflow cards:

1. Wybierz aktywa
2. Sprawdź dane
3. Zrozum czynniki

The existing `#how-it-works` anchor, educational disclaimer, illustrative
label in the hero, and responsive workflow grid remain unchanged.

## Scope

- Remove the `process-result` markup from `Pages/Index.cshtml`.
- Remove the process-result tokens, classes, layout rules, and mobile rules from
  `wwwroot/css/site.css`.
- Update the landing contract test to assert the three workflow steps and stop
  requiring the removed second result panel.
- Do not change the hero ranking, authentication links, backend behavior, or
  visual design system.

## Alternatives considered

- Keep both panels: rejected because it duplicates the ranking and competes for
  attention.
- Replace the second panel with another visual connector: deferred because the
  existing three-card process is sufficient once the duplicate is removed.

## Validation

- Run the targeted authentication contract tests.
- Verify the landing page at `1440x900` and `390x844`.
- Confirm that the hero remains the only illustrative ranking and the process
  cards remain readable without horizontal overflow.
