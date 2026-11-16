# HTTP Endpoints

All require an authenticated cookie (`RequireAuthorization()`); access rules per data-model.md.

## GET /documents/{id}/download
- 200: file stream, `Content-Type` = stored FileType, `Content-Disposition: attachment;
  filename*=UTF-8''<OriginalFileName>`; logs `Download`.
- 404: document missing **or** caller lacks access (no existence leak).
- 302 → /login when unauthenticated.

## GET /documents/{id}/preview
- 200: inline stream for `application/pdf`, `image/jpeg`, `image/png`; logs `Preview`.
- 404: missing, no access, or not previewable.

## Blazor routes (UI contract)
| Route | Purpose | Auth |
|-------|---------|------|
| `/documents` | Upload, My Documents (sort/filter), Shared with Me, search | any role |
| `/documents/reports` | Activity reports | Administrator |
| `/projects/{id}` | + Project Documents section | project member |
| `/tasks` | + task documents panel | assigned/created |
| `/` | + Recent Documents widget, document count card | any role |
