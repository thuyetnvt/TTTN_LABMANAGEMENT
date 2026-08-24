using Microsoft.AspNetCore.Http;

namespace LabManagementAPI.Services;

public sealed class LocalFileStorage : IFileStorage
{
    private static readonly IReadOnlyDictionary<string, string> ContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };

    private readonly string _rootDirectory;

    public LocalFileStorage(IWebHostEnvironment environment)
    {
        _rootDirectory = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "uploads"));
        Directory.CreateDirectory(_rootDirectory);
    }

    public async Task<StoredFile> SaveAsync(
        IFormFile file,
        string folder,
        IReadOnlySet<string> allowedExtensions,
        long maxBytes,
        CancellationToken cancellationToken = default)
    {
        var originalName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(originalName).ToLowerInvariant();
        if (file.Length <= 0 || file.Length > maxBytes)
            throw new InvalidDataException($"File rỗng hoặc vượt quá {maxBytes / (1024 * 1024)} MB.");
        if (!allowedExtensions.Contains(extension) || !ContentTypes.ContainsKey(extension))
            throw new InvalidDataException("Định dạng file không được hỗ trợ.");
        if (!string.IsNullOrWhiteSpace(file.ContentType)
            && !string.Equals(file.ContentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(file.ContentType, ContentTypes[extension], StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("MIME type không khớp với phần mở rộng file.");

        await using var input = file.OpenReadStream();
        var header = new byte[12];
        var read = await input.ReadAsync(header, cancellationToken);
        if (!IsValidSignature(extension, header, read))
            throw new InvalidDataException("Nội dung file không khớp với phần mở rộng.");
        input.Position = 0;

        var safeFolder = string.Join(Path.DirectorySeparatorChar,
            folder.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
                .Select(Path.GetFileName)
                .Where(value => !string.IsNullOrWhiteSpace(value)));
        var directory = Path.GetFullPath(Path.Combine(_rootDirectory, safeFolder));
        if (!directory.StartsWith(_rootDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            throw new InvalidDataException("Thư mục lưu file không hợp lệ.");
        Directory.CreateDirectory(directory);
        var storedPath = Path.Combine(directory, $"{Guid.NewGuid():N}{extension}");
        await using var output = new FileStream(storedPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await input.CopyToAsync(output, cancellationToken);
        return new StoredFile(originalName, storedPath, ContentTypes[extension], file.Length);
    }

    public bool IsSafePath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        return fullPath.StartsWith(_rootDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && File.Exists(fullPath);
    }

    public void Delete(string path)
    {
        if (IsSafePath(path)) File.Delete(Path.GetFullPath(path));
    }

    private static bool IsValidSignature(string extension, byte[] header, int read)
    {
        return extension switch
        {
            ".pdf" => read >= 4 && header[..4].SequenceEqual(new byte[] { 0x25, 0x50, 0x44, 0x46 }),
            ".png" => read >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".jpg" or ".jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".webp" => read >= 12
                && header[..4].SequenceEqual("RIFF"u8.ToArray())
                && header[8..12].SequenceEqual("WEBP"u8.ToArray()),
            ".doc" => read >= 4 && header[..4].SequenceEqual(new byte[] { 0xD0, 0xCF, 0x11, 0xE0 }),
            ".docx" => read >= 2 && header[0] == 0x50 && header[1] == 0x4B,
            _ => false
        };
    }
}
