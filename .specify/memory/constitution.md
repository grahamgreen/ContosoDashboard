# ContosoDashboard Constitution

## Core Principles

### I. Training-First, Offline-Only

ContosoDashboard is a training application. Every feature MUST run fully offline on a single
developer machine with no cloud subscriptions, external services, or network calls at runtime.

- Infrastructure MUST be local: a local relational database via EF Core (SQL Server LocalDB on
  x64 Windows, SQLite on ARM64/macOS) and the local filesystem for file storage.
- No Azure SDKs or other cloud client libraries MAY be referenced by the training build.
- Code MUST stay simple enough to teach; known training-only shortcuts MUST be documented in the
  README "Known Limitations" section rather than silently left in.

Rationale: Learners must be able to run the lab anywhere, at no cost, without credentials.

### II. Cloud-Ready Infrastructure Abstractions

Every infrastructure dependency (file storage, and any future messaging, scanning, or identity
integration) MUST sit behind an interface registered through dependency injection.

- Business logic, pages, and the database schema MUST NOT depend on a concrete infrastructure
  implementation. Example: `IFileStorageService` with `LocalFileStorageService` for training and
  a future `AzureBlobStorageService` for production.
- Migrating to Azure (Azure SQL, Blob Storage, Microsoft Entra ID) MUST require only a new
  implementation plus configuration, with no changes to services, pages, or schema.
- Stored paths and keys MUST be relative and portable (e.g., `{userId}/{projectId}/{guid}.{ext}`).

Rationale: Teaches correct separation of concerns and keeps the production migration path cheap.

### III. Security by Default (Defense in Depth)

All features MUST enforce authorization at every layer, even though authentication is mocked.

- Every page that shows non-public data MUST carry `[Authorize]` (or a role policy).
- Every service method that reads or mutates user-owned data MUST verify the caller's identity and
  role/membership server-side (IDOR protection); UI hiding alone is never sufficient.
- Role hierarchy is fixed: Employee < TeamLead < ProjectManager < Administrator.
- User-supplied file names MUST NOT be used in filesystem paths; files MUST be stored outside
  `wwwroot` with GUID-based names and served only through an authorized endpoint.
- Uploads MUST be validated against an extension/MIME allowlist and a size limit before storage.
- Existing security headers (CSP, X-Frame-Options, nosniff, Referrer-Policy) MUST NOT be weakened.

Rationale: The app exists to teach secure patterns; a feature that bypasses them is a defect.

### IV. Consistent Layered Architecture

New features MUST follow the existing Models → Data (EF Core `ApplicationDbContext`) → Services →
Pages (Blazor Server / Razor Pages) structure.

- Entities use integer primary keys, consistent with `User` and `Project`.
- Simple enumerations that users see (e.g., categories) MAY be stored as text for readability.
- Services expose async methods; data access uses `async`/`await` and eager loading (`Include`)
  to avoid N+1 queries; frequently filtered columns MUST be indexed.
- No major rewrites of existing features; changes to existing files MUST be the minimum needed to
  integrate the new feature.

Rationale: Consistency keeps the codebase teachable and reviews predictable.

### V. Spec-Driven, Incrementally Deliverable Work

All non-trivial changes MUST flow through the Spec Kit sequence: constitution → specify →
clarify → plan → tasks → implement.

- Specifications describe *what* and *why*; technology choices belong in the plan.
- Work MUST be organized into independently testable user stories, prioritized (P1, P2, ...), so
  that an MVP (the P1 story) can be built, demonstrated, and committed on its own.
- Every user story MUST have Given/When/Then acceptance scenarios that are verified (manually or
  with automated tests) before the story is marked complete.
- The application MUST build without errors and start cleanly after each completed phase.

Rationale: Small, verified increments make AI-assisted implementation reviewable and safe.

## Technology & Platform Constraints

- **Runtime**: ASP.NET Core on the installed LTS .NET SDK (currently .NET 10), Blazor Server UI,
  Razor Pages for login/logout, Bootstrap 5.3 and Bootstrap Icons for styling.
- **Data**: Entity Framework Core; schema created with `EnsureCreated()` for training. Seed data
  MUST keep the four mock users (Administrator, Project Manager, Team Lead, Employee).
- **Authentication**: Cookie-based mock authentication with claims NameIdentifier, Name, Email,
  Role, and Department. Features MUST NOT introduce real identity providers in the training build.
- **Storage**: Local filesystem under a non-web-accessible app data directory.
- **Dependencies**: New NuGet packages require a documented justification in the plan; prefer
  framework-provided capabilities.
- **Performance targets** (training scale): list pages under 2 s for 500 rows; search under 2 s.

## Development Workflow & Quality Gates

- Each Spec Kit artifact (constitution, spec, plan, tasks) is committed separately so reviewers can
  trace decisions; feature work happens on the feature branch created by `/speckit.specify`.
- Before merging: the project builds, the app starts, the user-story acceptance scenarios pass, and
  no new compiler errors are introduced.
- Reviews MUST check compliance with Principles I–V; violations MUST be listed with justification
  in the plan's Complexity Tracking table or fixed.
- Documentation (README) MUST be updated when a feature adds pages, configuration, or limitations.

## Governance

This constitution supersedes other practices for ContosoDashboard. Plans generated by
`/speckit.plan` MUST include a Constitution Check against these principles.

- **Amendments**: proposed via pull request that edits this file, states the rationale, and
  updates dependent Spec Kit templates or guidance if affected.
- **Versioning**: semantic versioning. MAJOR for removing or redefining a principle, MINOR for a new
  principle or materially expanded guidance, PATCH for clarifications and wording.
- **Compliance**: every feature review verifies the Constitution Check; unresolved violations block
  merge.

**Version**: 1.0.0 | **Ratified**: 2026-11-16 | **Last Amended**: 2026-11-16
