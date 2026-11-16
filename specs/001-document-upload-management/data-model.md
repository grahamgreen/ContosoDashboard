# Data Model: Document Upload and Management

## Document (new)

| Field | Type | Rules |
|-------|------|-------|
| DocumentId | int, PK | identity |
| Title | string(255) | required |
| Description | string(2000)? | optional |
| Category | string(50) | required; one of Project Documents, Team Resources, Personal Files, Reports, Presentations, Other |
| Tags | string(500)? | comma-separated, normalized |
| OriginalFileName | string(255) | required; display/download name only, never used in paths |
| FileType | string(255) | required; server-assigned MIME |
| FileSize | long | 1 … 26,214,400 bytes |
| FilePath | string(500) | required, **unique**; relative `{userId}/{projectId\|personal}/{guid}.{ext}` |
| UploadedByUserId | int, FK → User | required (owner) |
| ProjectId | int?, FK → Project | optional; uploader must be member/manager |
| TaskId | int?, FK → TaskItem | optional; when set, ProjectId = task's ProjectId |
| UploadedDate | DateTime (UTC) | set on create |
| LastModifiedDate | DateTime (UTC) | set on create, metadata edit, or file replace |

Indexes: UploadedByUserId, ProjectId, TaskId, Category, UploadedDate; unique FilePath.
Delete behavior: User/Project/Task FKs `Restrict`; shares cascade with the document.

## DocumentShare (new)

| Field | Type | Rules |
|-------|------|-------|
| DocumentShareId | int, PK | |
| DocumentId | int, FK → Document | cascade delete |
| SharedWithUserId | int?, FK → User | exactly one of SharedWithUserId / SharedWithDepartment |
| SharedWithDepartment | string(100)? | team = department, evaluated at access time |
| SharedByUserId | int, FK → User | must be document owner |
| SharedDate | DateTime (UTC) | |

Unique: (DocumentId, SharedWithUserId) and (DocumentId, SharedWithDepartment).

## DocumentActivity (new, audit)

| Field | Type | Rules |
|-------|------|-------|
| DocumentActivityId | int, PK | |
| DocumentId | int? | nullable so records survive deletion (no FK) |
| DocumentTitle | string(255) | snapshot at action time |
| FileType | string(255) | snapshot (for "most uploaded types") |
| UserId | int, FK → User | actor |
| Action | string(20) | Upload, Download, Preview, Delete, Share, Update, Replace |
| Timestamp | DateTime (UTC) | indexed |

## Existing entities touched

- **NotificationType**: append `DocumentShared`, `DocumentAdded` (appended to keep existing values).
- **User.Department**: used as team; emitted as a claim at login.

## Access rules (derived from FR-018/FR-020)

| Action | Allowed for |
|--------|-------------|
| View / download / preview | owner; project member or manager (if ProjectId); direct share recipient; current member of shared department; TeamLead of owner's department; Administrator |
| Edit metadata / replace / share | owner |
| Delete | owner; manager of the document's project; Administrator |
| Reports | Administrator |
