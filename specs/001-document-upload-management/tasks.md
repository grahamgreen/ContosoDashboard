# Tasks: Document Upload and Management

**Input**: Design documents from `/specs/001-document-upload-management/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Not requested in the spec; each story is verified with the matching scenario in
[quickstart.md](./quickstart.md).

**Organization**: Tasks are grouped by user story so each story can be implemented and tested
independently. Paths are relative to the repository root.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: User story the task belongs to (US1…US7)

---

## Phase 1: Setup

- [ ] T001 Add `AppData/` to `.gitignore` so uploaded files are never committed
- [ ] T002 [P] Add `"FileStorage": { "RootPath": "AppData/uploads" }` to `ContosoDashboard/appsettings.json`
- [ ] T003 [P] Create `ContosoDashboard/Endpoints/` folder for minimal-API endpoints

---

## Phase 2: Foundational (blocks all stories)

- [ ] T004 [P] Create `DocumentCategories` constants (Project Documents, Team Resources, Personal Files, Reports, Presentations, Other) in `ContosoDashboard/Models/DocumentCategories.cs`
- [ ] T005 [P] Create `Document` entity in `ContosoDashboard/Models/Document.cs`: DocumentId int PK; Title string(255) required; Description string(2000)?; Category string(50) required; Tags string(500)?; OriginalFileName string(255) required; FileType string(255) required; FileSize long; FilePath string(500) required unique; UploadedByUserId int FK; ProjectId int?; TaskId int?; UploadedDate, LastModifiedDate UTC
- [ ] T006 [P] Create `DocumentShare` entity in `ContosoDashboard/Models/DocumentShare.cs` (exactly one of SharedWithUserId / SharedWithDepartment string(100)?; SharedByUserId; SharedDate)
- [ ] T007 [P] Create `DocumentActivity` entity in `ContosoDashboard/Models/DocumentActivity.cs` (DocumentId int? without FK; DocumentTitle string(255); FileType string(255); UserId; Action string(20); Timestamp)
- [ ] T008 Append `DocumentShared`, `DocumentAdded` to `NotificationType` in `ContosoDashboard/Models/Notification.cs`
- [ ] T009 Register DbSets, relationships (Restrict on User/Project/Task, Cascade shares), unique FilePath, and indexes on UploadedByUserId, ProjectId, TaskId, Category, UploadedDate, Activity.Timestamp in `ContosoDashboard/Data/ApplicationDbContext.cs`
- [ ] T010 [P] Define `IFileStorageService` (UploadAsync, DeleteAsync, DownloadAsync, GetUrlAsync) in `ContosoDashboard/Services/IFileStorageService.cs`
- [ ] T011 Implement `LocalFileStorageService` (root from `FileStorage:RootPath` under content root, rejects rooted/`..` paths, creates directories) in `ContosoDashboard/Services/LocalFileStorageService.cs`
- [ ] T012 [P] Define `IMalwareScanner` + `ScanResult` and implement `SignatureMalwareScanner` (EICAR signature, magic bytes per extension) in `ContosoDashboard/Services/MalwareScanner.cs`
- [ ] T013 [P] Implement `DocumentValidation` (extension allowlist, extension→MIME map, 25 MB = 26,214,400 bytes limit, previewable types, tag normalization) in `ContosoDashboard/Services/DocumentValidation.cs`
- [ ] T014 Add `Department` claim at login in `ContosoDashboard/Pages/Login.cshtml.cs`
- [ ] T015 Register `IFileStorageService`, `IMalwareScanner`, `IDocumentService` in DI in `ContosoDashboard/Program.cs`

**Checkpoint**: project builds; database recreated with new tables.

---

## Phase 3: User Story 1 — Upload and view my own documents (P1) 🎯 MVP

**Goal**: Employees upload documents with metadata and see/download them in My Documents.

**Independent Test**: quickstart.md → US1.

- [ ] T016 [US1] Create `IDocumentService` and request/result DTOs (`DocumentUploadRequest`, `UploadResult`, `DocumentQuery`) in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T017 [US1] Implement `DocumentService.UploadAsync`: validate size/type/title/category → check project membership if ProjectId → scan → generate `{userId}/{projectId|personal}/{guid}.{ext}` → save file → insert row (delete file if insert fails) → log `Upload` activity, in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T018 [US1] Implement `GetMyDocumentsAsync` (owner's documents with Project, AsNoTracking, newest first) in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T019 [US1] Implement access check `CanViewAsync` and `OpenForReadAsync` (log `Download`/`Preview`) in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T020 [US1] Map `GET /documents/{id}/download` (attachment, UTF-8 filename, 404 when no access) with `RequireAuthorization()` in `ContosoDashboard/Endpoints/DocumentEndpoints.cs` and call it from `Program.cs`
- [ ] T021 [US1] Create `/documents` page with `[Authorize]`: upload form (`InputFile multiple` with `@key`, Title, Category select, Description, Project select of user's projects, Tags), per-file progress bar, per-file success/error messages, in `ContosoDashboard/Pages/Documents.razor`
- [ ] T022 [US1] Add My Documents table (title, category, upload date, size, project, Download link) to `ContosoDashboard/Pages/Documents.razor`
- [ ] T023 [US1] Add Documents link to `ContosoDashboard/Shared/NavMenu.razor`
- [ ] T024 [US1] Validate US1 with quickstart.md scenarios (upload, limits, unsupported type, multi-file, download, cross-user denial)

**Checkpoint**: MVP complete and demonstrable.

---

## Phase 4: User Story 2 — Organize and find documents (P2)

- [ ] T025 [US2] Add sort (title, date, category, size) and filters (category, project, date range) to `GetMyDocumentsAsync` via `DocumentQuery` in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T026 [US2] Implement `SearchAsync` over Title, Description, Tags, uploader DisplayName, project Name, restricted by access rules, in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T027 [US2] Add sort headers, filter bar, and search box/results to `ContosoDashboard/Pages/Documents.razor`
- [ ] T028 [US2] Seed/verify 500-document list and search under 2 s (quickstart US2)

## Phase 5: User Story 3 — Project documents (P3)

- [ ] T029 [US3] Implement `GetProjectDocumentsAsync` (members/manager only) in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T030 [US3] Notify other project members (`DocumentAdded`) after project upload in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T031 [US3] Add Project Documents section with upload for members to `ContosoDashboard/Pages/ProjectDetails.razor`

## Phase 6: User Story 4 — Manage documents (P4)

- [ ] T032 [US4] Implement `UpdateMetadataAsync` and `ReplaceFileAsync` (owner only, logs Update/Replace) in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T033 [US4] Implement `DeleteAsync` (owner, project manager, admin; removes file + row; logs Delete) in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T034 [US4] Map `GET /documents/{id}/preview` (inline PDF/JPEG/PNG) in `ContosoDashboard/Endpoints/DocumentEndpoints.cs`
- [ ] T035 [US4] Add Edit modal, Replace, Preview (new tab), and Delete confirmation to `ContosoDashboard/Pages/Documents.razor`

## Phase 7: User Story 5 — Share documents (P5)

- [ ] T036 [US5] Implement `ShareAsync` (owner only, no duplicates, notify `DocumentShared`) and `GetSharedWithMeAsync` in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T037 [US5] Add Share dialog (user or department) and Shared with Me tab to `ContosoDashboard/Pages/Documents.razor`

## Phase 8: User Story 6 — Task and dashboard integration (P6)

- [ ] T038 [US6] Implement `GetTaskDocumentsAsync` and task uploads that set ProjectId from the task in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T039 [US6] Add task documents panel with upload to `ContosoDashboard/Pages/Tasks.razor`
- [ ] T040 [US6] Add Recent Documents widget (last 5) and document count card to `ContosoDashboard/Pages/Index.razor`

## Phase 9: User Story 7 — Activity audit and reports (P7)

- [ ] T041 [US7] Implement `GetReportAsync` (Administrator only: top file types, top uploaders, recent activity) in `ContosoDashboard/Services/DocumentService.cs`
- [ ] T042 [US7] Create `/documents/reports` page with `[Authorize(Policy = "Administrator")]` in `ContosoDashboard/Pages/DocumentReports.razor` and admin-only nav link

## Phase 10: Polish & Cross-Cutting

- [ ] T043 [P] Update `README.md` (Documents feature, AppData storage, DB reset, SQLite on ARM/macOS)
- [ ] T044 [P] Review all `DocumentService` methods for IDOR (every method checks caller access)
- [ ] T045 Run full quickstart.md validation for all stories

---

## Dependencies & Execution Order

- Setup (P1) → Foundational (P2) → user stories. US1 is the MVP and a prerequisite for US2–US7
  (they reuse `DocumentService` and `Documents.razor`). After US1, US2, US3, US6, US7 are
  independent of each other; US5 depends only on US1; US4's preview endpoint extends US1's
  endpoints file.
- Within a story: service methods → endpoints → page UI → validation.

## Parallel Opportunities

- Phase 2: T004–T007, T010, T012, T013 touch different files and can run together.
- After US1: US3 (ProjectDetails.razor), US6 (Tasks.razor, Index.razor), US7
  (DocumentReports.razor) can proceed in parallel; their `DocumentService` edits are separate
  methods.

## Implementation Strategy

1. **MVP first**: T001–T024 (Setup + Foundational + US1). Stop, validate, demo, commit.
2. Add stories in priority order (US2 → US7), validating each with quickstart.md.
3. Finish with Phase 10 polish.
