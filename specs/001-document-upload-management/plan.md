# Implementation Plan: Document Upload and Management

**Branch**: `001-document-upload-management` | **Date**: 2026-11-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-document-upload-management/spec.md`

## Summary

Add a document library to ContosoDashboard: users upload PDF/Office/text/image files (≤ 25 MB)
with title, category, optional description/project/tags; browse, sort, filter, and search the
documents they may access; download, preview, edit, replace, delete, and share them; see them on
project and task pages and on the dashboard; and administrators review an activity audit.

Technical approach (see [research.md](./research.md)): three new EF Core entities (`Document`,
`DocumentShare`, `DocumentActivity`) in the existing `ApplicationDbContext`; files stored on the
local filesystem outside `wwwroot` behind `IFileStorageService` (`LocalFileStorageService`) with
GUID paths `{userId}/{projectId|personal}/{guid}.{ext}`; a pluggable `IMalwareScanner`
(signature/magic-byte check offline); a `DocumentService` that owns validation, authorization, the
upload sequence (path → file → DB row, with compensation), notifications, and audit; Blazor Server
pages for the UI; and two authorized minimal-API endpoints that stream files for download/preview.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (ASP.NET Core 10)

**Primary Dependencies**: ASP.NET Core Blazor Server, Razor Pages, EF Core 10 (Sqlite provider on
ARM64/macOS, SqlServer/LocalDB on x64 Windows), Bootstrap 5.3 + Bootstrap Icons. No new NuGet
packages.

**Storage**: Relational DB via EF Core (`EnsureCreated`); files on local disk under
`AppData/uploads` (configurable `FileStorage:RootPath`), outside `wwwroot`.

**Testing**: No test project exists. Acceptance verified with the scenarios in
[quickstart.md](./quickstart.md) (manual or browser automation); build must have 0 errors.

**Target Platform**: Local developer machine (Windows/macOS/Linux), modern desktop browsers.

**Project Type**: Server-rendered web application (single ASP.NET Core project).

**Performance Goals**: list/search ≤ 2 s for 500 documents; preview ≤ 3 s; 25 MB upload ≤ 30 s.

**Constraints**: Fully offline; no Azure SDKs; 25 MB per file; files never under `wwwroot`;
security headers unchanged (`X-Frame-Options: DENY` ⇒ preview opens in a new tab, not an iframe).

**Scale/Scope**: Training scale — 4 seeded users, a few projects, hundreds of documents.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Gate | Status |
|-----------|------|--------|
| I. Training-First, Offline-Only | No cloud SDKs, local DB + filesystem only | ✅ Pass |
| II. Cloud-Ready Abstractions | Storage and scanning behind interfaces, DI-registered; portable relative paths | ✅ Pass |
| III. Security by Default | `[Authorize]` on pages; service-level checks on every operation; GUID paths outside `wwwroot`; allowlist + size limit; authorized download endpoint; headers unchanged | ✅ Pass |
| IV. Consistent Layered Architecture | Models → Data → Services → Pages; int keys; text category; async + `Include`; indexes on filter columns | ✅ Pass |
| V. Spec-Driven, Incremental | Stories P1–P7 independently testable; MVP = US1; quickstart scenarios per story | ✅ Pass |

**Post-design re-check**: ✅ Pass. The only cross-cutting changes to existing files are additive
(DbSets, two `NotificationType` values, a Department claim at login, nav link, DI registrations,
endpoint mapping, small sections on project/task/dashboard pages).

## Project Structure

### Documentation (this feature)

```text
specs/001-document-upload-management/
├── plan.md              # This file
├── research.md          # Phase 0 decisions
├── data-model.md        # Phase 1 entities
├── quickstart.md        # Phase 1 validation guide
├── contracts/
│   ├── http-endpoints.md       # Download/preview endpoints
│   └── service-interfaces.md   # IFileStorageService, IMalwareScanner, IDocumentService
├── checklists/requirements.md
└── tasks.md             # /speckit.tasks output
```

### Source Code (repository root)

```text
ContosoDashboard/
├── Models/
│   ├── Document.cs                 # NEW
│   ├── DocumentShare.cs            # NEW
│   ├── DocumentActivity.cs         # NEW
│   ├── DocumentCategories.cs       # NEW (allowed category strings)
│   └── Notification.cs             # + DocumentShared, DocumentAdded
├── Data/ApplicationDbContext.cs    # + DbSets, indexes, relationships
├── Services/
│   ├── IFileStorageService.cs      # NEW
│   ├── LocalFileStorageService.cs  # NEW
│   ├── IMalwareScanner.cs          # NEW (+ SignatureMalwareScanner)
│   ├── DocumentValidation.cs       # NEW (allowlist, size, MIME map)
│   └── DocumentService.cs          # NEW (IDocumentService + implementation)
├── Endpoints/DocumentEndpoints.cs  # NEW (download/preview)
├── Pages/
│   ├── Documents.razor             # NEW (upload + My Documents + Shared with Me + search)
│   ├── DocumentReports.razor       # NEW (Administrator)
│   ├── ProjectDetails.razor        # + Project Documents section
│   ├── Tasks.razor                 # + task documents
│   ├── Index.razor                 # + Recent Documents widget, count card
│   └── Login.cshtml.cs             # + Department claim
├── Shared/NavMenu.razor            # + Documents link (+ Reports for admins)
├── Program.cs                      # + DI, options, endpoint mapping
└── appsettings.json                # + FileStorage:RootPath
```

**Structure Decision**: Keep the existing single-project layered layout; add one `Endpoints/`
folder for the minimal-API file endpoints (needed because Blazor components cannot stream file
responses directly).

## Complexity Tracking

No constitution violations; table intentionally empty.
