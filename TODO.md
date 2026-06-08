# TODO

Working backlog. Newest decisions at top; check items off as done.

## Now / decisions needed

- [x] **Commit this session's work** — done: `chore/onboarding-docs-and-tests` (`d2d3208`).
- [x] **`working` integration branch** — created off `main` and **pushed to `origin/working`** (`9bb2278`). Combines three in-flight fix branches so we can consume a usable local version *without waiting for the PRs to merge*:
  - `fix/engine-thread-safety` — `MdToOxmlEngine` thread-safe (per-call `rootFolder`).
  - `chore/onboarding-docs-and-tests` — docs/tooling, README DI fix, tests.
  - `fixes` — inline `` `code` `` rendering + deps moved **RC/preview → stable** (`Microsoft.Extensions.* 10.0.8`, `OpenXMLSDK.Engine 2022.10313.0`).
  - Conflicts resolved: kept both `GetCodeInlineLabel` + thread-safe `GetLinkInlineText(…, rootFolder)`; took `fixes`' superset test files; made `SingleImage` return `Image?` under merged `Nullable=enable`. Verified: `-warnaserror` build clean, **27/27 tests pass**.
  - **Maintenance:** `working` is an integration branch, not a PR target. When these PRs land on `main`, rebuild `working` (re-merge remaining unmerged branches onto the new `main`) or just switch back to `main`. Docker was intentionally **excluded**.
- [ ] **Docker branch** — `origin/copilot/support-for-docker-image` is worth adopting (good Dockerfile incl. `libgdiplus`, solid GHCR workflow, compose + examples). It's 1 commit behind `main`. Plan: rebase onto `main`, open a PR, and **smoke-test the Linux image actually produces a .docx** (System.Drawing.Common + libgdiplus image embedding is the risk). Not in `working`.
- [ ] **Delete dead branch** — `origin/copilot/propose-new-features-or-improvements` is an empty "Initial plan" commit (no changes). Delete it.

## Code / quality

- [ ] **Expand test coverage further** — 27 tests on `working` (19 onboarding + 8 inline-code from `fixes`). Not yet covered: image embedding (`![](images/..)` resolution against `rootFolder`), ordered/unordered lists, multi-file ordering (the `.order` convention referenced in git history), `width=`/`height=` HTML-tag image sizing, pre/post hooks.
- [ ] **Fix garbled comments** in `SampleCallUnitTests*.cs` ("the wode does not gÉnÉrate exception" — mojibake). Low priority.
- [ ] **(Team decision) Add an `.editorconfig`** — the repo has none. Adding one would make `dotnet format`/analyzers useful and could revive a format-on-edit hook (dropped this session because whitespace-only formatting without an editorconfig is a near-no-op). Holding off as it's a style decision.
- [ ] **Consider** whether the `MdReportGenenerator` misspelling is worth an `[Obsolete]` alias + correctly-spelled type (breaking-change-safe path) — public API, so not a silent rename.

## Cross-repo (not this repo)

- [ ] **mcpRoslyn** — downward solution-discovery fix is committed on branch `fix/downward-solution-discovery` (in `C:\projects\mcpRoslyn`), not yet merged to its `main`. Merge with `git merge --ff-only` when ready.
