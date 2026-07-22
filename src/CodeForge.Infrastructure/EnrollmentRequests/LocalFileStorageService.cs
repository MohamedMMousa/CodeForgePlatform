using CodeForge.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;

namespace CodeForge.Infrastructure.EnrollmentRequests
{
    /// <summary>
    /// Stores files under a directory outside wwwroot (see docs/ARCHITECTURE.md §1 —
    /// "isolate volatile/external concerns behind interfaces") so they can never be
    /// reached by ASP.NET's static file middleware. All access goes through
    /// Open*Async, called only from authorized API endpoints (materials require
    /// CourseContentAuthorization.EnsureCanView; payment proofs require AdminOnly).
    /// </summary>
    public class LocalFileStorageService : IFileStorageService
    {
        private static readonly Dictionary<string, string> ExtensionByContentType = new()
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
            ["application/pdf"] = ".pdf"
        };

        private static readonly Dictionary<string, string> ContentTypeByExtension = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp",
            [".zip"] = "application/zip",
            [".ppt"] = "application/vnd.ms-powerpoint",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };

        private readonly string _paymentProofDirectory;
        private readonly string _materialsDirectory;

        public LocalFileStorageService(IHostEnvironment environment)
        {
            _paymentProofDirectory = Path.Combine(
                environment.ContentRootPath, "PrivateStorage", "payment-proofs");

            _materialsDirectory = Path.Combine(
                environment.ContentRootPath, "PrivateStorage", "materials");
        }

        public async Task<string> SavePaymentProofAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(_paymentProofDirectory);

            var extension = ExtensionByContentType.TryGetValue(contentType, out var knownExtension)
                ? knownExtension
                : Path.GetExtension(fileName);

            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(_paymentProofDirectory, storedFileName);

            await using var outputStream = File.Create(fullPath);
            await fileStream.CopyToAsync(outputStream, cancellationToken);

            return storedFileName;
        }

        public async Task<(string StorageKey, int SizeKb)> SaveCourseMaterialAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(_materialsDirectory);

            var extension = Path.GetExtension(fileName);
            var storedFileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(_materialsDirectory, storedFileName);

            await using var outputStream = File.Create(fullPath);
            await fileStream.CopyToAsync(outputStream, cancellationToken);

            var sizeKb = (int)Math.Ceiling(outputStream.Length / 1024.0);

            return (storedFileName, sizeKb);
        }

        public Task<(Stream Stream, string ContentType)> OpenPaymentProofAsync(
            string storageKey, CancellationToken cancellationToken = default)
            => OpenAsync(_paymentProofDirectory, storageKey);

        public Task<(Stream Stream, string ContentType)> OpenMaterialAsync(
            string storageKey, CancellationToken cancellationToken = default)
            => OpenAsync(_materialsDirectory, storageKey);

        private static Task<(Stream Stream, string ContentType)> OpenAsync(string directory, string storageKey)
        {
            // storageKey is DB-supplied; strip any directory component defensively so it
            // can never escape the storage directory (path traversal).
            var safeFileName = Path.GetFileName(storageKey);
            var fullPath = Path.Combine(directory, safeFileName);

            if (!File.Exists(fullPath))
            {
                throw new KeyNotFoundException("File was not found.");
            }

            var contentType = ContentTypeByExtension.TryGetValue(Path.GetExtension(safeFileName), out var known)
                ? known
                : "application/octet-stream";

            Stream stream = File.OpenRead(fullPath);
            return Task.FromResult((stream, contentType));
        }
    }
}
