namespace ContosoDashboard.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public LocalFileStorageService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configured = configuration["FileStorage:RootPath"] ?? "AppData/uploads";
        _rootPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, configured));
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
    {
        var fullPath = ResolvePath(fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        // CreateNew: never overwrite an existing file
        await using var target = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await fileStream.CopyToAsync(target);
        return fileName;
    }

    public Task DeleteAsync(string filePath)
    {
        var fullPath = ResolvePath(filePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }

    public Task<Stream> DownloadAsync(string filePath)
    {
        var fullPath = ResolvePath(filePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Stored file not found.", filePath);
        }
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult(stream);
    }

    // Local storage has no direct URL; files are served only by the authorized
    // /documents/{id}/download endpoint. Azure returns a time-limited SAS URL instead.
    public Task<string> GetUrlAsync(string filePath, TimeSpan expiration) =>
        throw new NotSupportedException("Local storage files are served through /documents/{id}/download.");

    private string ResolvePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException("A relative storage path is required.", nameof(relativePath));
        }

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));
        if (!fullPath.StartsWith(_rootPath + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Storage path escapes the storage root.");
        }
        return fullPath;
    }
}
