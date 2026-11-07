# Architecture

Reggex is organized as a small cross-platform desktop application with a UI-independent core. The current projects establish the boundary; regex functionality will be added in later milestones.

## Project responsibilities

- **Reggex.Core** contains application-domain behavior and shared types. It targets .NET 10 and must not depend on Avalonia, desktop APIs, or presentation packages.
- **Reggex.Desktop** owns the Avalonia application, platform startup, views, view models, and composition of the application. It may depend on Core.
- **Reggex.Core.Tests** contains xUnit tests for Core and references Core directly, so its behavior can be exercised without starting a UI.

All source and test projects remain under `src/`. Repository documentation and shared build configuration live at the root or under `documentation/`.

## Dependency direction and separation

The allowed project dependency direction is:

```text
Reggex.Desktop ──> Reggex.Core <── Reggex.Core.Tests
```

Core stays independent of the UI and can be built, tested, or reused without Avalonia. Desktop is the composition root: it connects presentation state to Core behavior. No additional architectural layers are needed until a concrete requirement calls for them.

## MVVM

The desktop UI uses Avalonia with CommunityToolkit.Mvvm. Views render state and forward user actions; view models expose observable state and commands; Core owns reusable application behavior and types. UI framework details stay in Desktop. Keep view models focused on presentation and avoid moving domain rules into views.

## Cross-platform strategy

The desktop project uses Avalonia's platform detection and desktop lifetime rather than platform-specific UI implementations. Target Windows, Linux, and macOS through Avalonia and .NET 10, keeping platform-specific integration at the application boundary when needed. Regex behavior uses `System.Text.RegularExpressions` initially and should not rely on operating-system-specific assumptions.

## Testing strategy

Use xUnit in `Reggex.Core.Tests` for deterministic unit tests of Core behavior. Tests should not need a display server or desktop lifetime. Add UI-level tests only when an actual UI behavior warrants them, while keeping the core testable in isolation.

## Future regex engine extensibility

Start with `System.Text.RegularExpressions` behind a small, concrete boundary only when a second engine or a real need for engine substitution arises. Keep engine-specific options and result details from leaking into unrelated Core behavior. Avoid speculative plugin systems or abstraction layers before there is another engine to support.
