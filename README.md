# Reggex

Reggex is a desktop application for building and testing .NET regular expressions. Edit a pattern and sample text side by side, inspect every match and its groups, and see matching text highlighted in a separate preview.

## Features

- **Find** evaluates a pattern against editable sample text and lists all matches with their positions, lengths, and captured groups.
- **Match highlighting** keeps the source text editable while showing a read-only preview. Select a match to emphasize it in the preview; zero-length matches are shown with a position marker.
- **Replace** previews the result using .NET replacement syntax, including named groups.
- **Regex options** include Ignore case, Multiline, Singleline, Explicit capture, Ignore pattern whitespace, Right to left, and ECMAScript.
- **Immediate feedback** updates as you edit, reports invalid patterns, and limits regex execution to 500 ms.

Reggex uses .NET's `System.Text.RegularExpressions` engine. Pattern syntax and behavior follow .NET regular expressions.

## Requirements

- .NET 10 SDK

## Run

From the repository root:

```sh
dotnet run --project src/Reggex.Desktop
```

## Test

Run the Core test suite with:

```sh
dotnet test src/Reggex.Core.Tests
```

## Project structure

- `src/Reggex.Core` contains the regex engine, request and result models, and execution behavior. It has no UI dependency.
- `src/Reggex.Desktop` contains the Avalonia desktop interface and its MVVM presentation logic.
- `src/Reggex.Core.Tests` contains unit tests for the Core behavior.
- `documentation` contains the architecture notes and project roadmap.

The UI is built with Avalonia and CommunityToolkit.Mvvm. The project is designed for cross-platform desktop use; platform packaging and validation are still part of the roadmap.
