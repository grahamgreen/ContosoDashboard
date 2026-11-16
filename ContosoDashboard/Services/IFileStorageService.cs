namespace ContosoDashboard.Services;

// Storage abstraction: LocalFileStorageService for training, AzureBlobStorageService in production.
public interface IFileStorageService
{
    // fileName is the generated relative storage path; returns the stored path.
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType);
    Task DeleteAsync(string filePath);
    Task<Stream> DownloadAsync(string filePath);
    Task<string> GetUrlAsync(string filePath, TimeSpan expiration);
}
