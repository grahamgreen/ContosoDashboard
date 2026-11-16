# Service Contracts

```csharp
public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType); // fileName = generated relative path; returns stored path
    Task DeleteAsync(string filePath);
    Task<Stream> DownloadAsync(string filePath);
    Task<string> GetUrlAsync(string filePath, TimeSpan expiration);
}

public interface IMalwareScanner
{
    Task<ScanResult> ScanAsync(Stream content, string extension); // ScanResult(bool IsClean, string? Reason)
}

public interface IDocumentService
{
    Task<UploadResult> UploadAsync(DocumentUploadRequest request, Stream content, int userId);
    Task<List<Document>> GetMyDocumentsAsync(int userId, DocumentQuery query);
    Task<List<Document>> GetSharedWithMeAsync(int userId);
    Task<List<Document>> SearchAsync(int userId, string term);
    Task<List<Document>> GetProjectDocumentsAsync(int projectId, int userId);
    Task<List<Document>> GetTaskDocumentsAsync(int taskId, int userId);
    Task<List<Document>> GetRecentAsync(int userId, int count = 5);
    Task<int> GetCountAsync(int userId);
    Task<(Document doc, Stream content)?> OpenForReadAsync(int documentId, int userId, bool preview);
    Task<bool> UpdateMetadataAsync(int documentId, DocumentMetadata metadata, int userId);
    Task<UploadResult> ReplaceFileAsync(int documentId, string fileName, Stream content, int userId);
    Task<bool> DeleteAsync(int documentId, int userId);
    Task<bool> ShareAsync(int documentId, int? withUserId, string? withDepartment, int userId);
    Task<DocumentReport> GetReportAsync(int userId); // Administrator only
}
```

Every method resolves the caller's role/department from the database (not the UI) and returns
`null`/`false`/empty or throws `UnauthorizedAccessException` when access is denied.
