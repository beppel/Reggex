# Roadmap

These milestones establish the product incrementally without committing to speculative infrastructure.

## Current status — 18 November 2026

Milestones 1–4 are implemented. The Core suite has 37 passing tests and the Desktop suite has 32 (69 total); run both explicitly because the Desktop test project is not included in `Reggex.slnx`. Find, Replace, Split, C# generation (including `[GeneratedRegex]`), match/capture details, and keyboard shortcuts are implemented. Manual UI acceptance and Linux/macOS runtime validation remain outstanding; publish targets below are plans, not verified release artifacts.

Run the suites from the repository root:

```sh
dotnet test src/Reggex.Core.Tests
dotnet test src/Reggex.Desktop.Tests
```

1. **Repository foundation — complete**: shared .NET settings, formatting conventions, project boundaries, and architecture documentation.
2. **Core regex model — complete**: request, options, results, and focused xUnit coverage.
3. **Regex evaluation — complete**: .NET engine, invalid-pattern/options and timeout handling, common options, replace, and split.
4. **Desktop workflows — implemented**: Find, Replace, Split, virtualized results, source highlighting, code generation, and usability features. Complete hands-on UI acceptance during milestone 5.
5. **Cross-platform polish — planned**:
   - Run both test suites and verify app startup on Windows, Linux, and macOS.
   - On Linux, cover both X11 and Wayland sessions; on each platform check file dialogs, clipboard behavior, fonts/icons, resizing, keyboard shortcuts, error presentation, and Find/Replace/Split interactions.
   - Verify match highlighting, invisible-character markers, and generated-code output at common display scales.
   - Add automated platform smoke coverage where practical; record any platform-specific limitations.
6. **Release readiness — planned**:
   - Verify license notices, release documentation, and platform-specific packaging prerequisites.
   - Produce and run self-contained single-file publish candidates for `win-x64`, `linux-x64`, and `osx-arm64`:

     ```sh
     dotnet publish src/Reggex.Desktop -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
     dotnet publish src/Reggex.Desktop -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
     dotnet publish src/Reggex.Desktop -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true
     ```

   - Evaluate Native AOT separately for each RID with `-p:PublishAot=true`; first confirm Avalonia and all dependencies are compatible, resolve trimming/AOT warnings, and smoke-test each output before declaring support.
     ```sh
     dotnet publish src/Reggex.Desktop -c Release -r win-x64 --self-contained true -p:PublishAot=true
     dotnet publish src/Reggex.Desktop -c Release -r linux-x64 --self-contained true -p:PublishAot=true
     dotnet publish src/Reggex.Desktop -c Release -r osx-arm64 --self-contained true -p:PublishAot=true
     ```
   - Record artifact size, startup behavior, signing, and distribution steps. Do not treat these publish commands as validated until the cross-platform checks pass.

Keep Core independently testable. Additional regex engines can be evaluated when there is a concrete user need.
