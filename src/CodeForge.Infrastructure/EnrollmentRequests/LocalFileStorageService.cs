using CodeForge.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;

namespace CodeForge.Infrastructure.EnrollmentRequests
{
    public class LocalFileStorageService : IFileStorageService
    {
        private static readonly Dictionary<string, string> ExtensionByContentType = new()
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
            ["application/pdf"] = ".pdf"
        };

        private readonly string _paymentProofDirectory;
        private readonly string _materialsDirectory;

        public LocalFileStorageService(IHostEnvironment environment)
        {
            _paymentProofDirectory = Path.Combine(
                environment.ContentRootPath,
                "wwwroot",
                "uploads",
                "payment-proofs");

            _materialsDirectory = Path.Combine(
                environment.ContentRootPath,
                "wwwroot",
                "uploads",
                "materials");
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

            return $"/uploads/payment-proofs/{storedFileName}";
        }

        public async Task<(string Url, int SizeKb)> SaveCourseMaterialAsync(
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

            return ($"/uploads/materials/{storedFileName}", sizeKb);
        }
    }
}
