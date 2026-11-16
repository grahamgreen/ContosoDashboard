# Quickstart & Validation: Document Upload and Management

## Prerequisites
- .NET 10 SDK; repo on branch `001-document-upload-management`.
- Reset the local database once so the new tables are created (`EnsureCreated` does not alter
  existing databases):
  - SQLite (macOS/ARM): delete `ContosoDashboard/ContosoDashboard.db`
  - LocalDB (x64 Windows): `sqllocaldb stop mssqllocaldb; sqllocaldb delete mssqllocaldb`

## Run
```bash
cd ContosoDashboard
dotnet build      # expect 0 errors
dotnet run        # http://localhost:5000
```
Uploaded files land in `ContosoDashboard/AppData/uploads/` (git-ignored).

## US1 (MVP) — Upload and view my documents
1. Log in as **Ni Kang (Employee)** → **Documents**.
2. Choose a PDF < 25 MB, Title "Q4 Report", Category **Personal Files**, Upload →
   success message; row shows title, category, date, size, project "—".
3. Download → same bytes, original file name.
4. Upload a > 25 MB file → error mentions 25 MB; upload `test.exe` → unsupported-type error;
   empty title → blocked.
5. Select 3 files → each listed with its file name as default title.
6. Log in as **Floris Kregel** → GET `/documents/{id}/download` of Ni's personal doc → 404.

## US2 — Sort, filter, search
Sort by size/date/title/category; filter by category/project/date; search by tag and uploader;
documents without access never appear.

## US3 — Project documents
As **Camille Nicole (PM)** upload to "ContosoDashboard Development"; Ni (member) sees it on the
project page and gets a notification; a non-member gets 404.

## US4 — Manage
Edit metadata, replace file, preview PDF (new tab), delete with confirmation.

## US5 — Share
Share with a user and with "Engineering"; recipients notified; visible under Shared with Me.

## US6 — Tasks & dashboard
Upload from a task (auto-linked to its project); dashboard shows last 5 and the count.

## US7 — Audit
As **System Administrator** open Documents → Reports; actions from above are listed; other roles
are denied.
