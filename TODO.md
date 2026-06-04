# TODO

Working backlog. Newest decisions at top; check items off as done.

## Now / decisions needed

- [ ] **Commit this session's work** — `README.md` fix, `CLAUDE.md`, `.claude/` (verify skill), `ARCHITECTURE.md`, `TODO.md`, `SESSION_HANDOFF.md`, and the two new test files. Decide: one commit or split (docs vs. tests vs. tooling). On a branch per usual.
- [ ] **Docker branch** — `origin/copilot/support-for-docker-image` is worth adopting (good Dockerfile incl. `libgdiplus`, solid GHCR workflow, compose + examples). It's 1 commit behind `main`. Plan: rebase onto `main`, open a PR, and **smoke-test the Linux image actually produces a .docx** (System.Drawing.Common + libgdiplus image embedding is the risk).
- [ ] **Delete dead branch** — `origin/copilot/propose-new-features-or-improvements` is an empty "Initial plan" commit (no changes). Delete it.

## Code / quality

- [ ] **Expand test coverage further** — currently 19 tests. Not yet covered: image embedding (`![](images/..)` resolution against `rootFolder`), ordered/unordered lists, multi-file ordering (the `.order` convention referenced in git history), `width=`/`height=` HTML-tag image sizing, pre/post hooks.
- [ ] **Fix garbled comments** in `SampleCallUnitTests*.cs` ("the wode does not gÉnÉrate exception" — mojibake). Low priority.
- [ ] **(Team decision) Add an `.editorconfig`** — the repo has none. Adding one would make `dotnet format`/analyzers useful and could revive a format-on-edit hook (dropped this session because whitespace-only formatting without an editorconfig is a near-no-op). Holding off as it's a style decision.
- [ ] **Consider** whether the `MdReportGenenerator` misspelling is worth an `[Obsolete]` alias + correctly-spelled type (breaking-change-safe path) — public API, so not a silent rename.

## Cross-repo (not this repo)

- [ ] **mcpRoslyn** — downward solution-discovery fix is committed on branch `fix/downward-solution-discovery` (in `C:\projects\mcpRoslyn`), not yet merged to its `main`. Merge with `git merge --ff-only` when ready.
