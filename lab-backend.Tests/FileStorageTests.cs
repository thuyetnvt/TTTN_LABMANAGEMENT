using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using LabManagementAPI.Services;
using Xunit;

namespace LabManagementAPI.Tests;

public sealed class FileStorageTests
{
    [Fact]
    public async Task Save_rejects_mismatched_declared_mime_type()
    {
        var root = CreateTempRoot();
        try
        {
            var storage = new LocalFileStorage(new TestEnvironment(root));
            await using var stream = new MemoryStream(PngSignature());
            var file = new FormFile(stream, 0, stream.Length, "file", "photo.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            await Assert.ThrowsAsync<InvalidDataException>(() => storage.SaveAsync(
                file,
                "evidence",
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png" },
                1024));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Save_uses_random_safe_path_for_valid_file()
    {
        var root = CreateTempRoot();
        try
        {
            var storage = new LocalFileStorage(new TestEnvironment(root));
            await using var stream = new MemoryStream(PngSignature());
            var file = new FormFile(stream, 0, stream.Length, "file", "../photo.png")
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

            var saved = await storage.SaveAsync(
                file,
                "evidence/../safe",
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png" },
                1024);

            Assert.True(storage.IsSafePath(saved.StoredPath));
            var storedFilePath = Path.Combine(
                root,
                "uploads",
                saved.StoredPath.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(storedFilePath));
            Assert.EndsWith(".png", saved.StoredPath, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("photo", Path.GetFileName(saved.StoredPath), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task OpenRead_and_delete_only_operate_on_managed_file()
    {
        var root = CreateTempRoot();
        try
        {
            var storage = new LocalFileStorage(new TestEnvironment(root));
            await using var input = new MemoryStream(PngSignature());
            var saved = await storage.SaveAsync(
                new FormFile(input, 0, input.Length, "file", "photo.png")
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "image/png"
                },
                "evidence",
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png" },
                1024);

            await using (var read = await storage.OpenReadAsync(saved.StoredPath))
            {
                Assert.NotNull(read);
                Assert.Equal(PngSignature().Length, read!.Length);
            }
            await storage.DeleteAsync(saved.StoredPath);
            Assert.False(File.Exists(saved.StoredPath));
            Assert.Null(await storage.OpenReadAsync(Path.Combine(root, "outside.png")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "lab-management-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static byte[] PngSignature() =>
    [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private sealed class TestEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "LabManagement.Tests";
        public string EnvironmentName { get; set; } = "Testing";
        public string WebRootPath { get; set; } = root;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
