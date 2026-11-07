# Initial Roadmap

These milestones establish the product incrementally without committing to speculative infrastructure.

## Current status — 8 November 2026

Milestones 1–4 are implemented. The solution restores and builds without warnings, all 25 tests pass, and the Avalonia app launches on Windows. Hands-on visual and interaction review of Find and Replace remains before milestone 4 is considered verified. Cross-platform validation and release preparation remain ahead.

1. **Repository foundation** — establish shared .NET settings, formatting conventions, project boundaries, and architecture documentation.
2. **Core regex model** — define the minimal expression, options, input, and match result types needed by the tester; add focused xUnit coverage.
3. **Regex evaluation** — integrate `System.Text.RegularExpressions` in Core, including clear handling of invalid expressions and common options.
4. **Split tester workflow** — build the Avalonia layout with an expression editor and a test input/results pane, backed by view models.
5. **Cross-platform polish** — verify Windows, Linux, and macOS startup and interaction; refine keyboard access, resizing, and error presentation.
6. **Release readiness** — document usage, verify packaging and licensing, and prepare initial open-source releases.

Each milestone should remain small and keep Core independently testable. Additional regex engines can be evaluated when there is a concrete user need.
