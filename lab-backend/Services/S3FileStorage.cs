using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;

namespace LabManagementAPI.Services;

public sealed class S3FileStorage : IFileStorage
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

    private readonly IAmazonS3 _client;
    private readonly string _bucketName;
    private readonly string _keyPrefix;

    public S3FileStorage(IConfiguration configuration)
    {
        var section = configuration.GetSection("Storage:S3");
        _bucketName = section["BucketName"]?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_bucketName))
        {
            throw new InvalidOperationException("Storage:S3:BucketName là bắt buộc khi dùng S3.");
        }

        _keyPrefix = NormalizePrefix(section["KeyPrefix"] ?? "labmanagement");
        var config = new AmazonS3Config
        {
            ForcePathStyle = section.GetValue("ForcePathStyle", true),
            RegionEndpoint = RegionEndpoint.GetBySystemName(section["Region"]?.Trim() ?? "us-east-1")
        };
        var serviceUrl = section["ServiceUrl"]?.Trim();
        if (!string.IsNullOrWhiteSpace(serviceUrl)) config.ServiceURL = serviceUrl;

        var accessKey = section["AccessKey"]?.Trim();
        var secretKey = section["SecretKey"]?.Trim();
        if (string.IsNullOrWhiteSpace(accessKey) != string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException("Storage:S3:AccessKey và SecretKey phải được cấu hình cùng nhau.");
        }

        _client = string.IsNullOrWhiteSpace(accessKey)
            ? new AmazonS3Client(config)
            : new AmazonS3Client(accessKey, secretKey, config);
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
        await using var content = new MemoryStream(capacity: checked((int)Math.Min(file.Length, maxBytes)));
        await input.CopyToAsync(content, cancellationToken);
        var bytes = content.GetBuffer().AsSpan(0, checked((int)content.Length));
        if (!IsValidSignature(extension, bytes))
            throw new InvalidDataException("Nội dung file không khớp với phần mở rộng.");

        var key = BuildKey(folder, extension);
        content.Position = 0;
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = content,
            ContentType = ContentTypes[extension]
        }, cancellationToken);
        return new StoredFile(originalName, key, ContentTypes[extension], file.Length);
    }

    public bool IsSafePath(string path)
    {
        var normalized = path.Trim().Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(normalized)) return false;
        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Any(part => part is "." or "..")) return false;
        var prefix = string.IsNullOrWhiteSpace(_keyPrefix) ? string.Empty : $"{_keyPrefix}/";
        return normalized.StartsWith(prefix, StringComparison.Ordinal)
            && normalized.Length > prefix.Length;
    }

    public async Task<Stream?> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!IsSafePath(path)) return null;
        try
        {
            var response = await _client.GetObjectAsync(
                new GetObjectRequest { BucketName = _bucketName, Key = path.Replace('\\', '/') },
                cancellationToken);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!IsSafePath(path)) return;
        await _client.DeleteObjectAsync(
            new DeleteObjectRequest { BucketName = _bucketName, Key = path.Replace('\\', '/') },
            cancellationToken);
    }

    private string BuildKey(string folder, string extension)
    {
        var safeFolder = string.Join('/', folder.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
            .Select(Path.GetFileName)
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        var segments = new[] { _keyPrefix, safeFolder, $"{Guid.NewGuid():N}{extension}" }
            .Where(value => !string.IsNullOrWhiteSpace(value));
        return string.Join('/', segments);
    }

    private static string NormalizePrefix(string prefix)
        => string.Join('/', prefix.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Where(value => value is not "." and not ".."));

    private static bool IsValidSignature(string extension, ReadOnlySpan<byte> header)
        => extension switch
        {
            ".pdf" => header.Length >= 4 && header[..4].SequenceEqual("%PDF"u8),
            ".png" => header.Length >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".jpg" or ".jpeg" => header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".webp" => header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8),
            ".doc" => header.Length >= 4 && header[..4].SequenceEqual(new byte[] { 0xD0, 0xCF, 0x11, 0xE0 }),
            ".docx" => header.Length >= 2 && header[..2].SequenceEqual("PK"u8),
            _ => false
        };
}
