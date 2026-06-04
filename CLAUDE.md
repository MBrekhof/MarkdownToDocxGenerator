# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A .NET library that converts Markdown files into DOCX (Word) documents using Markdig (parsing) and OpenXMLSDK.Engine (Word generation). Ships as a NuGet package; also has a console front-end and an MSTest test project.

## Layout & build

- **The solution lives in `src/`, not the repo root:** `src/MarkdownToDocxGenerator.sln`. All `dotnet` commands must target that path.
- Three projects under `src/`:
  - `MarkdownToDocxGenerator` — the library. Multi-targets `netstandard2.0;net9.0;net10.0`, `LangVersion=preview`.
  - `MarkdownToDocxGenerator.Console` — console front-end, `net10.0`.
  - `MarkdownToDocxGenerator.UnitTests` — **MSTest** (not xUnit/NUnit), `net10.0`.

```bash
dotnet build src/MarkdownToDocxGenerator.sln
dotnet test  src/MarkdownToDocxGenerator.sln
# single test:
dotnet test src/MarkdownToDocxGenerator.UnitTests/MarkdownToDocxGenerator.UnitTests.csproj --filter "FullyQualifiedName~SampleCallUnitTests.Transform"
```

Run **build + test before claiming work is done** (or use `/verify`).

## Gotchas

- **DI registration method is `RegisterMarkdownToDocxGenerator(asSingleton)`** — NOT `AddMarkdownToDocxGenerator()` (the README is outdated). You must register logging *before* calling it. See `Console/Program.cs` for the canonical usage.
- **Console arg contract:** it reads `Environment.GetCommandLineArgs()`, so it expects exactly **6 positional args** (`arguments.Length == 7` including the exe): `rootFolder templatePath version outputPath projectName projectIndex`. On any missing/invalid arg it logs a warning and **silently `return 0`** — no error. Don't assume a non-zero exit signals failure.
- **Linux/containers need `libgdiplus`** for image processing; pure-managed assumptions will fail on non-Windows.
- The library project does **not** enable `Nullable`/`ImplicitUsings` (console + tests do). It uses file-scoped namespaces in `Extensions/`.

## Conventions

- **Don't bump the preview/RC dependencies on your own** (`OpenXMLSDK.Engine` preview, `Microsoft.Extensions.* 10.0.0-rc.1`, `LangVersion=preview`) — surface version changes and ask before applying.
- **Don't remove or break existing public APIs** without flagging it — this is a published library and consumers depend on its surface (see `CONTRIBUTING.md`).
- No `.editorconfig` exists; follow the style already in the surrounding code.

## CI

`.github/workflows/ci.yml` runs on push/PR to `main`, delegating to an external reusable workflow (`mathieumack/MyGithubActions`) that builds/tests on .NET 9 + 10 and can publish to NuGet.
