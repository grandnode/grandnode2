---
name: project-build-baseline
description: Baseline build and format state for GrandNode2 — known pre-existing warnings and format violations so agents can distinguish new issues from old ones
metadata:
  type: project
---

Build baseline as of 2026-05-25 (.NET SDK 9.0.100, Release config):

- 0 errors
- 10 warnings, all NU19xx NuGet vulnerability advisories — all pre-existing and acceptable:
  - NU1903 AutoMapper 14.0.0 (high severity, GHSA-rvv3-g6hj-g44x) — affects Grand.Infrastructure
  - NU1902 SixLabors.ImageSharp 2.1.10 (moderate, GHSA-rxmq-m78w-7wmc) — affects Grand.Business.Common, Grand.Web.Admin
  - NU1902 MailKit 4.11.0 (moderate, GHSA-9j88-vvj5-vhgr) — affects Grand.Business.Messages
  - NU1902 OpenTelemetry.Exporter.OpenTelemetryProtocol 1.11.2 (moderate, GHSA-4625-4j76-fww9) — affects Aspire.ServiceDefaults
- TreatWarningsAsErrors is NOT set anywhere in .props files

Format baseline (`dotnet format --verify-no-changes GrandNode.sln`):
- Returns exit code 255 (violations present)
- Pre-existing WHITESPACE violations in Grand.Web project files: PersonalizedProducts.cs, AccountController.cs, and others
- Do NOT bulk-fix in a PR — only fix files you touched

**Why:** These are known pre-existing issues in the repo. New agents must know which warnings/violations are old vs. introduced by their changes.

**How to apply:** When running health checks, filter out NU1902/NU1903 warnings. For format, only enforce on modified files, not the whole solution.
