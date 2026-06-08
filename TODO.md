# TODO

Working backlog. Newest decisions at top; check items off as done.

## Now / decisions needed

- [x] **Commit this session's work** — done: `chore/onboarding-docs-and-tests` (`d2d3208`).
- [x] **`working` integration branch** — created off `main` and **pushed to `origin/working`** (`9bb2278`). Combines three in-flight fix branches so we can consume a usable local version *without waiting for the PRs to merge*:
  - `fix/engine-thread-safety` — `MdToOxmlEngine` thread-safe (per-call `rootFolder`).
  - `chore/onboarding-docs-and-tests` — docs/tooling, README DI fix, tests.
  - `fixes` — inline `` `code` `` rendering + deps moved **RC/preview → stable** (`Microsoft.Extensions.* 10.0.8`, `OpenXMLSDK.Engine 2022.10313.0`).
  - Conflicts resolved: kept both `GetCodeInlineLabel` + thread-safe `GetLinkInlineText(…, rootFolder)`; took `fixes`' superset test files; made `SingleImage` return `Image?` under merged `Nullable=enable`. Verified: `-warnaserror` build clean, **27/27 tests pass**.
  - Now also includes **`fix/line-breaks`** (`81762c1`, off `main`, pushed to `origin/fix/line-breaks`) — see below.
  - **Maintenance:** `working` is an integration branch, not a PR target. When these PRs land on `main`, rebuild `working` (re-merge remaining unmerged branches onto the new `main`) or just switch back to `main`. Docker was intentionally **excluded**.
- [x] **Fix lost line breaks / empty lines** — branch `fix/line-breaks` (off `main`), merged into `working` (`b8fb3be`). Two losses in `MdToOxmlEngine.Transform`:
  1. Hard line breaks inside a paragraph were emitted as empty runs (no break) → lines ran together. Now a `LineBreakInline` starts a new paragraph (no `<w:br/>` model exists in OpenXMLSDK.Engine).
  2. Runs of blank lines between blocks were collapsed. `AppendPreservedBlankLines` re-inserts the *extra* blanks (beyond the single separator) as empty paragraphs, via `block.Line`. Normal single-blank case unchanged.
  - **Design choice:** preserved blanks = (blank lines above block − 1). Line breaks render as paragraph splits, not true `<w:br/>` (slightly more vertical spacing). Covered by `LineBreakTests` (3 tests). Verified on `working`: `-warnaserror` clean, **30/30 pass**.
- [x] **Fix empty-heading crash + non-deterministic file ordering** — branch `fix/heading-and-ordering` (off `main`, `origin/fix/heading-and-ordering`), merged into `working` (`e5949bd`).
  1. An ATX heading with no content (`# `, `#`, `### `) crashed with `NullReferenceException` (handler dereferenced `FirstOrDefault()`). Now emits an empty styled paragraph.
  2. `MdReportGenenerator.Transform` (file-based) collected `*.md` via `Directory.GetFiles` in unspecified order and matched `.md` case-sensitively. Now `OrdinalIgnoreCase` filter + sorted → deterministic across platforms, includes `.MD`.
  - Covered by `HeadingEdgeTests` + `ReportGeneratorFileTests`. Verified on `working`: `-warnaserror` clean, **33/33 pass**.
- [x] **Fix file-based `Transform` silently not saving without a template** — branch `fix/no-template-save` (off `main`, `origin/fix/no-template-save`), merged into `working` (`cee8082`). `WordManager.New()`/`SaveDoc()` take no path (no `SaveAs` exists), so the no-template branch wrote nothing to `outputPath` and didn't throw. Now persists `GetMemoryStream()` bytes to `outputPath` after `SaveDoc`/`CloseDoc` (mirrors `TransformWithStream`). Covered by `ReportGeneratorNoTemplateTests`. Verified on `working`: `-warnaserror` clean, **34/34 pass**.
- [ ] **Docker branch** — `origin/copilot/support-for-docker-image` is worth adopting (good Dockerfile incl. `libgdiplus`, solid GHCR workflow, compose + examples). It's 1 commit behind `main`. Plan: rebase onto `main`, open a PR, and **smoke-test the Linux image actually produces a .docx** (System.Drawing.Common + libgdiplus image embedding is the risk). Not in `working`.
- [ ] **Delete dead branch** — `origin/copilot/propose-new-features-or-improvements` is an empty "Initial plan" commit (no changes). Delete it.

## Code / quality

- [ ] **Lower-severity review items (deferred)** — `ReportsReader` `.order` branch has no `File.Exists` guard before `File.ReadAllText` (throws on a missing listed file); sub-folder detection uses `filePath.Replace(".md", "")` (replaces all occurrences).
- [ ] **Expand test coverage further** — 34 tests on `working` (19 onboarding + 8 inline-code + 3 line-break + 3 heading/ordering + 1 no-template save). Not yet covered: image embedding (`![](images/..)` resolution against `rootFolder`), ordered/unordered lists, multi-file ordering (the `.order` convention referenced in git history), `width=`/`height=` HTML-tag image sizing, pre/post hooks.
- [ ] **Fix garbled comments** in `SampleCallUnitTests*.cs` ("the wode does not gÉnÉrate exception" — mojibake). Low priority.
- [ ] **(Team decision) Add an `.editorconfig`** — the repo has none. Adding one would make `dotnet format`/analyzers useful and could revive a format-on-edit hook (dropped this session because whitespace-only formatting without an editorconfig is a near-no-op). Holding off as it's a style decision.
- [ ] **Consider** whether the `MdReportGenenerator` misspelling is worth an `[Obsolete]` alias + correctly-spelled type (breaking-change-safe path) — public API, so not a silent rename.

## Cross-repo (not this repo)

- [ ] **mcpRoslyn** — downward solution-discovery fix is committed on branch `fix/downward-solution-discovery` (in `C:\projects\mcpRoslyn`), not yet merged to its `main`. Merge with `git merge --ff-only` when ready.
