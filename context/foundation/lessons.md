# Lessons Learned

> Append-only register of recurring rules and patterns. Re-read at start by /10x-frame, /10x-research, /10x-plan, /10x-plan-review, /10x-implement, /10x-impl-review.

## Grep for references before deleting a file

- **Context**: Any phase that deletes or renames a script, module, or file under wwwroot/js, Analysis/, or Pages/.
- **Problem**: Deleting analysis-chart.js left tests/js/analysis-chart.selectPoints.test.js reading the removed file; the .NET suite stayed green, so only the no-mistakes review caught the broken Node test.
- **Rule**: Before deleting or renaming a file, grep the whole repo (including tests/js and non-.NET tests) for its name and remove or update every reference in the same commit.
- **Applies to**: plan, implement, impl-review

## Restore break-check files with a fresh timestamp

- **Context**: Break-checks during /10x-implement and /10x-impl-review triage that temporarily revert a source file and then restore it (PowerShell on Windows).
- **Problem**: Restoring with Copy-Item keeps the original timestamp, so MSBuild treats the project as up to date and dotnet test runs stale binaries — a break-check can look red/green for the wrong code.
- **Rule**: Restore break-check files with [IO.File]::WriteAllText (or touch the file) so the timestamp changes, and run tests in Release because the IDE locks Debug outputs.
- **Applies to**: implement, impl-review