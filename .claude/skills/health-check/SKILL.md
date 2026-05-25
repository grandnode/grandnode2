---
name: health-check
description: Run post-development health checks on GrandNode2 — build, format, tests, warnings, analyzers. Use after implementing a feature or fixing a bug to verify the codebase is healthy before committing or creating a PR.
---

# GrandNode2 Post-Development Health Check

Run these steps in order after making backend changes. Stop and fix any failures before proceeding.

---

## 1. Build

```powershell
dotnet build GrandNode.sln --configuration Release 2>&1 | Select-String -Pattern "warning|error|Error" | Select-Object -First 30
```

**Clean output looks like:**
```
    10 Warning(s)
    0 Error(s)
```

**Known-acceptable warnings (pre-existing, do not block PR):**
- `NU1902` — SixLabors.ImageSharp 2.1.10, MailKit 4.11.0, OpenTelemetry.Exporter 1.11.2 (moderate severity advisories)
- `NU1903` — AutoMapper 14.0.0 (high severity advisory, tracked upstream)

**Blocking:** Any `error` line, or any `warning` that is not in the known-acceptable list above. Fix before continuing.

> `TreatWarningsAsErrors` is NOT set in this project — warnings do not fail the build, but new warnings you introduce are still blocking for PR.

---

## 2. Format

**Check only (do not modify files):**
```powershell
dotnet format --verify-no-changes GrandNode.sln 2>&1 | Select-Object -First 20
```

**Auto-fix (apply changes to disk):**
```powershell
dotnet format GrandNode.sln
```

**Passing output:** No `error WHITESPACE` or `error` lines. Exit code 0.

**Failing output example:**
```
src\Web\Grand.Web\Controllers\AccountController.cs(73,57): error WHITESPACE: Fix whitespace formatting.
```

Format violations exist in the current codebase (pre-existing). Only fix violations in files you touched. Run auto-fix scoped to your changed files if possible, then verify the check passes for your files. Do not bulk-fix the whole solution in your PR.

---

## 3. Tests — Affected Module Only (fast path)

Map your changed Business module to its test project:

| Changed module | Test project path |
|---|---|
| Grand.Business.Catalog | `src/Tests/Grand.Business.Catalog.Tests/` |
| Grand.Business.Checkout | `src/Tests/Grand.Business.Checkout.Tests/` |
| Grand.Business.Customers | `src/Tests/Grand.Business.Customers.Tests/` |
| Grand.Business.Common | `src/Tests/Grand.Business.Common.Tests/` |
| Grand.Business.Marketing | `src/Tests/Grand.Business.Marketing.Tests/` |
| Grand.Business.Messages | `src/Tests/Grand.Business.Messages.Tests/` |
| Grand.Business.Cms | `src/Tests/Grand.Business.Cms.Tests/` |
| Grand.Business.Authentication | `src/Tests/Grand.Business.Authentication.Tests/` |
| Grand.Business.Storage | `src/Tests/Grand.Business.Storage.Tests/` |
| Grand.Data / Grand.Domain | `src/Tests/Grand.Data.Tests/` or `src/Tests/Grand.Domain.Tests/` |
| Grand.Infrastructure | `src/Tests/Grand.Infrastructure.Tests/` |
| Grand.SharedKernel | `src/Tests/Grand.SharedKernel.Tests/` |
| Grand.Modules.* | `src/Tests/Grand.Modules.Tests/` |
| Grand.Web.Admin | `src/Tests/Grand.Web.Admin.Tests/` |
| Grand.Web.Common | `src/Tests/Grand.Web.Common.Tests/` |

```powershell
dotnet test src/Tests/Grand.Business.Catalog.Tests/Grand.Business.Catalog.Tests.csproj
```

**Passing output ends with:**
```
Passed! - Failed: 0, Passed: N, Skipped: 0, Total: N
```

## 4. Tests — Full Suite (pre-PR gate, mirrors CI)

CI runs each project individually (no `--no-build`, no parallel flag). Replicate exactly:

```powershell
dotnet test GrandNode.sln --configuration Release
```

Or run per-project to match CI behavior exactly (see `.github/workflows/aspnetcore.yml`). Full suite requires MongoDB running on `localhost:27017`.

**Start MongoDB if needed:**
```powershell
docker run -d -p 27017:27017 --name mongodb mongo
```

---

## 5. Grep for New Warnings You Introduced

After a clean build, isolate warnings not in the known-acceptable list:

```powershell
dotnet build GrandNode.sln --configuration Release 2>&1 |
  Select-String "warning" |
  Where-Object { $_ -notmatch "NU1902|NU1903" }
```

**Passing:** No output (no new warnings).

---

## Pre-PR Checklist

Work through this before marking a PR ready:

- [ ] `dotnet build GrandNode.sln --configuration Release` — 0 errors, no new warnings beyond known NU19xx advisories
- [ ] `dotnet format --verify-no-changes GrandNode.sln` — no violations in files you modified
- [ ] Affected module test project — 0 failures
- [ ] Full `dotnet test GrandNode.sln` passes (requires local MongoDB on 27017)
- [ ] New command/query handlers have corresponding unit tests in the matching test project
- [ ] No `.Result` or `.Wait()` calls introduced (use `await`)
- [ ] No direct MongoDB driver usage — all data access through `IRepository<T>`
- [ ] FluentValidation validator added for any new command that accepts user input
