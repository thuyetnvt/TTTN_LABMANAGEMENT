using Microsoft.AspNetCore.Http;

namespace LabManagementAPI.Services;

public sealed record StoredFile(
    string OriginalFileName,
    string StoredPath,
    string ContentType,
    long Length);

public interface IFileStorage
{
    Task<StoredFile> SaveAsync(
        IFormFile file,
        string folder,
        IReadOnlySet<string> allowedExtensions,
        long maxBytes,
        CancellationToken cancellationToken = default);

    bool IsSafePath(string path);
    Task<Stream?> OpenReadAsync(string path, CancellationToken cancellationToken = default);
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);
}
