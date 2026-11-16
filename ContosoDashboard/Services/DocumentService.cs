using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public record DocumentUploadRequest(
    string FileName,
    long FileSize,
    string Title,
    string Category,
    string? Description,
    int? ProjectId,
    string? Tags);

public record UploadResult(bool Success, string? Error = null, Document? Document = null)
{
    public static UploadResult Fail(string error) => new(false, error);
}

public class DocumentQuery
{
    public string SortBy { get; set; } = "date";
    public bool Descending { get; set; } = true;
}

public interface IDocumentService
{
    Task<UploadResult> UploadAsync(DocumentUploadRequest request, Stream content, int userId);
    Task<List<Document>> GetMyDocumentsAsync(int userId, DocumentQuery? query = null);
    Task<bool> CanViewAsync(int documentId, int userId);
    Task<(Document Document, Stream Content)?> OpenForReadAsync(int documentId, int userId, bool preview);
}

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _storage;
    private readonly IMalwareScanner _scanner;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        ApplicationDbContext context,
        IFileStorageService storage,
        IMalwareScanner scanner,
        ILogger<DocumentService> logger)
    {
        _context = context;
        _storage = storage;
        _scanner = scanner;
        _logger = logger;
    }

    public async Task<UploadResult> UploadAsync(DocumentUploadRequest request, Stream content, int userId)
    {
        // 1. Validate
        var title = request.Title?.Trim() ?? string.Empty;
        if (title.Length == 0) return UploadResult.Fail("Title is required.");
        if (title.Length > 255) return UploadResult.Fail("Title must be 255 characters or fewer.");
        if (!DocumentCategories.IsValid(request.Category)) return UploadResult.Fail("Please choose a category.");
        if (request.Description?.Length > 2000) return UploadResult.Fail("Description must be 2000 characters or fewer.");

        var extension = Path.GetExtension(request.FileName);
        if (!DocumentValidation.IsAllowedExtension(extension))
        {
            return UploadResult.Fail($"Unsupported file type. Supported types: {DocumentValidation.SupportedTypesDescription}.");
        }
        if (request.FileSize <= 0 || content.Length == 0) return UploadResult.Fail("The file is empty.");
        if (request.FileSize > DocumentValidation.MaxFileSizeBytes || content.Length > DocumentValidation.MaxFileSizeBytes)
        {
            return UploadResult.Fail("The file exceeds the 25 MB size limit.");
        }

        // 2. Authorize project association
        if (request.ProjectId.HasValue && !await IsProjectMemberAsync(request.ProjectId.Value, userId))
        {
            return UploadResult.Fail("You can only upload documents to projects you belong to.");
        }

        // 3. Scan
        var scan = await _scanner.ScanAsync(content, extension);
        if (!scan.IsClean) return UploadResult.Fail($"The file was rejected by the security scan: {scan.Reason}");

        // 4. Generate a unique path before any write; user file names never reach the filesystem
        var folder = request.ProjectId?.ToString() ?? "personal";
        var relativePath = $"{userId}/{folder}/{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var mimeType = DocumentValidation.GetMimeType(extension);

        // 5. Save file
        content.Position = 0;
        await _storage.UploadAsync(content, relativePath, mimeType);

        // 6. Save metadata; remove the file if the database write fails (no orphans)
        var now = DateTime.UtcNow;
        var document = new Document
        {
            Title = title,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Category = request.Category,
            Tags = DocumentValidation.NormalizeTags(request.Tags),
            OriginalFileName = Path.GetFileName(request.FileName),
            FileType = mimeType,
            FileSize = content.Length,
            FilePath = relativePath,
            UploadedByUserId = userId,
            ProjectId = request.ProjectId,
            UploadedDate = now,
            LastModifiedDate = now
        };

        try
        {
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Saving document metadata failed; removing stored file {Path}", relativePath);
            _context.Entry(document).State = EntityState.Detached;
            await _storage.DeleteAsync(relativePath);
            return UploadResult.Fail("The document could not be saved. Please try again.");
        }

        await LogActivityAsync(document, userId, DocumentActions.Upload);
        return new UploadResult(true, Document: document);
    }

    public async Task<List<Document>> GetMyDocumentsAsync(int userId, DocumentQuery? query = null)
    {
        query ??= new DocumentQuery();
        var documents = _context.Documents
            .AsNoTracking()
            .Include(d => d.Project)
            .Where(d => d.UploadedByUserId == userId);

        documents = (query.SortBy, query.Descending) switch
        {
            ("title", false) => documents.OrderBy(d => d.Title),
            ("title", true) => documents.OrderByDescending(d => d.Title),
            ("category", false) => documents.OrderBy(d => d.Category),
            ("category", true) => documents.OrderByDescending(d => d.Category),
            ("size", false) => documents.OrderBy(d => d.FileSize),
            ("size", true) => documents.OrderByDescending(d => d.FileSize),
            (_, false) => documents.OrderBy(d => d.UploadedDate),
            _ => documents.OrderByDescending(d => d.UploadedDate),
        };

        return await documents.ToListAsync();
    }

    // FR-020: owner, project member/manager, direct or department share, Team Lead of the
    // owner's department, or Administrator. Evaluated from the database, never from the UI.
    public async Task<bool> CanViewAsync(int documentId, int userId)
    {
        var document = await _context.Documents.AsNoTracking()
            .Include(d => d.UploadedByUser)
            .Include(d => d.Shares)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);
        return document != null && await CanViewAsync(document, userId);
    }

    public async Task<(Document Document, Stream Content)?> OpenForReadAsync(int documentId, int userId, bool preview)
    {
        var document = await _context.Documents.AsNoTracking()
            .Include(d => d.UploadedByUser)
            .Include(d => d.Shares)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId);

        if (document == null || !await CanViewAsync(document, userId)) return null;
        if (preview && !DocumentValidation.IsPreviewable(document.FileType)) return null;

        Stream stream;
        try
        {
            stream = await _storage.DownloadAsync(document.FilePath);
        }
        catch (FileNotFoundException)
        {
            _logger.LogWarning("Stored file missing for document {DocumentId}", documentId);
            return null;
        }

        await LogActivityAsync(document, userId, preview ? DocumentActions.Preview : DocumentActions.Download);
        return (document, stream);
    }

    private async Task<bool> CanViewAsync(Document document, int userId)
    {
        if (document.UploadedByUserId == userId) return true;

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return false;
        if (user.Role == UserRole.Administrator) return true;

        if (document.ProjectId.HasValue && await IsProjectMemberAsync(document.ProjectId.Value, userId)) return true;

        if (document.Shares.Any(s => s.SharedWithUserId == userId)) return true;

        if (!string.IsNullOrEmpty(user.Department))
        {
            if (document.Shares.Any(s => s.SharedWithDepartment == user.Department)) return true;

            if (user.Role == UserRole.TeamLead && document.UploadedByUser?.Department == user.Department) return true;
        }

        return false;
    }

    private Task<bool> IsProjectMemberAsync(int projectId, int userId) =>
        _context.Projects.AnyAsync(p => p.ProjectId == projectId &&
            (p.ProjectManagerId == userId || p.ProjectMembers.Any(pm => pm.UserId == userId)));

    private async Task LogActivityAsync(Document document, int userId, string action)
    {
        _context.DocumentActivities.Add(new DocumentActivity
        {
            DocumentId = document.DocumentId,
            DocumentTitle = document.Title,
            FileType = document.FileType,
            UserId = userId,
            Action = action,
            Timestamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
    }
}
