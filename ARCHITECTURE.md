# Architecture

How MarkdownToDocxGenerator turns Markdown into Word documents.

## Solution layout (`src/`)

| Project | Type | TFMs | Role |
|---|---|---|---|
| `MarkdownToDocxGenerator` | library | `netstandard2.0;net9.0;net10.0` | The engine. This is the published NuGet package. |
| `MarkdownToDocxGenerator.Console` | exe | `net10.0` | Thin CLI front-end; also the canonical usage example. |
| `MarkdownToDocxGenerator.UnitTests` | MSTest | `net10.0` | Tests (parser-model mapping, DI, end-to-end). |

> The solution file is `src/MarkdownToDocxGenerator.sln` — **not** the repo root.

## The pipeline

```
Markdown text
   │  Markdig (UseAdvancedExtensions + soft-break-as-hard-break)
   ▼
Markdig AST (blocks + inlines)
   │  MdToOxmlEngine — maps each AST node to a ReportEngine model element
   ▼
Report model  (Report → Document → Page → Paragraph/Label/Table/Hyperlink/Image …)
   │  OpenXMLSDK.Engine WordManager.AppendSubDocument(...)
   ▼
.docx  (written to a file path, or returned as a Stream)
```

### Key types

- **`MdToOxmlEngine`** (`MdToOxmlEngine.cs`) — the heart. `Transform(string content, string rootFolder)` parses Markdown with Markdig and walks the AST, producing a `Report` (the OpenXMLSDK.Engine document model). It never touches the filesystem for output — it only builds the in-memory model. This is the most unit-testable surface.
- **`MdReportGenenerator`** (`MdReportGenenerator.cs`) — orchestrator. Reads `.md` files from a folder (or a list of strings), calls the engine per document, then drives `WordManager` to render and save. Two entry points:
  - `Transform(outputPath, rootFolder, templatePath?, preHook?, postHook?)` → writes a file. Collects `*.md` from `rootFolder` **sorted, case-insensitively** (deterministic page order). With a template, `OpenDocFromTemplate(templatePath, outputPath, true)` + `SaveDoc()` writes the file; **without** a template (`WordManager.New()`/`SaveDoc()` take no path) it persists `GetMemoryStream()` to `outputPath` explicitly.
  - `TransformWithStream(List<string> contents, templateStream?, preHook?, postHook?)` → returns a `Stream` (for web/cloud, no disk).
- **`ServiceBuilderExtensions.RegisterMarkdownToDocxGenerator(asSingleton)`** — DI registration for both `MdToOxmlEngine` and `MdReportGenenerator`. `true` = singletons, `false` = transient.
- **`WordManager`** (from `OpenXMLSDK.Engine`, external) — the OOXML writer. Owns `New()` / `OpenDocFromTemplate(...)` / `AppendSubDocument(...)` / `SaveDoc()` / `GetMemoryStream()`.

## Markdown → OOXML mapping (in `MdToOxmlEngine`)

| Markdown | Produces |
|---|---|
| Heading `#`..`######` | `Paragraph` with `ParagraphStyleId = "Titre{level}"` |
| Paragraph text | `Paragraph` of `Label`s |
| `**bold**` (emphasis `*` ×2) | `Label { Bold = true }` |
| Fenced code block | one `Paragraph` per line, `Shading = "EAEAEA"`, preserved spacing |
| List item | `Label` prefixed with `1.` (ordered) or the bullet char |
| Pipe table | `Table` with styled header row + zebra-striped first column |
| Link, **absolute** URL | `Hyperlink { WebSiteUri }` (underlined) |
| Link, **relative/invalid** URL | falls back to a plain `Label` (see #25) — never a broken hyperlink |
| Image `![](path)` | `Image` resolved against `rootFolder`; optional `width=`/`height=` parsed from a trailing HTML tag |
| Hard line break inside a paragraph (soft breaks are promoted to hard via `UseSoftlineBreakAsHardlineBreak`) | starts a **new `Paragraph`** — OpenXMLSDK.Engine has no `<w:br/>` model element, so a line break = paragraph split (not a true in-paragraph break) |
| Extra blank lines between top-level blocks | re-inserted as empty `Paragraph`s by `AppendPreservedBlankLines` using `block.Line`; one blank line is the implicit paragraph separator, so only `(blanks − 1)` become visible empty lines |

Unrecognized blocks/inlines are logged via `ILogger` and skipped (non-fatal).

## Extension points

- **Template**: pass a `.dotx` path/stream. `Titre{n}` paragraph styles and bookmarks (`version`, `creationDate`, `projectName0..9`) come from the template — without one, those styles are unstyled.
- **Pre/post hooks**: `Action<WordManager>` invoked before/after the markdown content is appended, for custom document manipulation.

## Gotchas

- **`ILogger` must be registered before** `RegisterMarkdownToDocxGenerator` — both engine types depend on `ILogger<>`.
- **Linux/containers need `libgdiplus`** for image processing.
- **Console arg contract**: reads `Environment.GetCommandLineArgs()`, so it expects exactly 6 positional args (`rootFolder templatePath version outputPath projectName projectIndex`) and silently `return 0`s on any missing/invalid one.
- **Class name `MdReportGenenerator`** is misspelled but is public API — renaming is a breaking change for package consumers.
- The library project does not enable `Nullable`/`ImplicitUsings`; the console and test projects do.
