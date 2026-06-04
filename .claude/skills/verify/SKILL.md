---
name: verify
description: Build and test the MarkdownToDocxGenerator solution and report results. Use before claiming a change is complete or before committing.
---

# Verify

Build and run the full test suite for this repo, then report the real outcome with evidence.

The solution is at `src/MarkdownToDocxGenerator.sln` (NOT the repo root) and the tests use **MSTest**.

## Steps

1. Build:
   ```bash
   dotnet build src/MarkdownToDocxGenerator.sln -warnaserror
   ```
   If the build fails, stop and report the compiler errors. Do not run tests.

2. Test:
   ```bash
   dotnet test src/MarkdownToDocxGenerator.sln
   ```

3. Report the actual result from the command output:
   - Build: succeeded / failed (with error count).
   - Tests: `Passed: N, Failed: M, Total: T`.
   - Quote any failures verbatim — never claim "passing" without the summary line in the output.

To run a single test instead of the suite:
```bash
dotnet test src/MarkdownToDocxGenerator.UnitTests/MarkdownToDocxGenerator.UnitTests.csproj --filter "FullyQualifiedName~<ClassName>.<MethodName>"
```

Do not declare success unless both the build and the test command exited 0 and the test summary shows `Failed: 0`.
