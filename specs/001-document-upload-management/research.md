# Research: Document Upload and Management

## R1. File storage location and layout
- **Decision**: `LocalFileStorageService` writes to `{ContentRoot}/AppData/uploads` (configurable
  `FileStorage:RootPath`). Relative path `{userId}/{projectId|personal}/{guid}.{ext}` is generated
  before any I/O and stored in `Document.FilePath` (unique).
- **Rationale**: Outside `wwwroot` so files are only reachable through authorized endpoints; GUID
  names prevent collisions and path traversal; same relative path works as an Azure blob name.
- **Alternatives**: `wwwroot/uploads` (rejected: publicly downloadable); storing bytes in the DB
  (rejected: bloats DB, breaks Blob migration path).

## R2. Upload sequence and failure handling
- **Decision**: validate → authorize → scan (in memory) → generate path → save file → insert row;
  if the insert fails, delete the saved file.
- **Rationale**: Satisfies FR-009 (no orphans either way) and the stakeholder's "path first, file
  second, DB third" guidance.

## R3. Blazor file handling
- **Decision**: `InputFile` with `multiple`; capture name/size/type first, copy
  `OpenReadStream(25 MB)` into a `MemoryStream` in 64 KB chunks updating a per-file progress value,
  clear the `IBrowserFile` reference, reset `InputFile` via `@key`.
- **Rationale**: Avoids disposed-stream errors; gives a real progress indicator.
- **Alternatives**: Separate MVC upload controller + JS fetch (rejected: more moving parts).

## R4. Serving downloads and previews
- **Decision**: Minimal-API endpoints `GET /documents/{id}/download` (attachment) and
  `GET /documents/{id}/preview` (inline, PDF/images only) with `.RequireAuthorization()`; both call
  `IDocumentService` for the access check and audit.
- **Rationale**: Components cannot write file responses; endpoints share cookie auth. Preview opens
  in a new tab because `X-Frame-Options: DENY` forbids iframes and Principle III forbids weakening it.

## R5. File-type validation
- **Decision**: Extension allowlist `.pdf .doc .docx .xls .xlsx .ppt .pptx .txt .jpg .jpeg .png`,
  server-assigned MIME type from the extension (browser MIME ignored), plus a magic-byte check.
- **Rationale**: Browser-supplied content types are unreliable; magic bytes catch renamed files.

## R6. Malware scanning offline
- **Decision**: `IMalwareScanner` with `SignatureMalwareScanner`: rejects the EICAR test signature
  and content whose magic bytes don't match the extension. Production swaps in a real scanner
  (e.g., Defender for Storage) via DI.
- **Rationale**: Principle I forbids external services; interface keeps Principle II.

## R7. Team definition and Department claim
- **Decision**: Team = `User.Department`. Add a `Department` claim at login. Team shares store the
  department name and are evaluated at access time (clarification Q3).

## R8. Search and list performance
- **Decision**: Server-side EF queries with `Contains` (→ SQL `LIKE`) on Title, Description, Tags,
  uploader DisplayName, project Name; indexes on `UploadedByUserId`, `ProjectId`, `TaskId`,
  `Category`, `UploadedDate`; `AsNoTracking`; paged/limited lists.
- **Rationale**: Comfortably < 2 s at 500 rows; no full-text engine needed offline.

## R9. Database provider and schema creation
- **Decision**: Keep `EnsureCreated()`. Existing local databases must be deleted once so the new
  tables are created (documented in quickstart).
- **Alternatives**: Introduce EF migrations (rejected for training scope; noted as follow-up).

## R10. Tags storage
- **Decision**: Comma-separated string (max 500 chars), normalized (trimmed, lower-case, de-duped).
- **Rationale**: Simplest model that supports `LIKE` search; no tag management in scope.
