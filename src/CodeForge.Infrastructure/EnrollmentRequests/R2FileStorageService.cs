using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using CodeForge.Application.Common.Interfaces;
using CodeForge.Application.Common.Models;
using Microsoft.Extensions.Options;

namespace CodeForge.Infrastructure.EnrollmentRequests
{
    /// <summary>
    /// S3-compatible storage against Cloudflare R2 — selected via Storage:Provider = "R2"
    /// (see LocalFileStorageService for the default "Local" implementation and why R2
    /// matters in production: Render's free tier has no persistent disk, so anything
    /// LocalFileStorageService writes vanishes on the next deploy/restart while the DB
    /// rows referencing it survive). R2 is S3-compatible so no repository/model changes
    /// were needed — this is only a second implementation of the existing
    /// IFileStorageService interface, selected in DependencyInjection.cs.
    ///
    /// StorageKey returned to callers is a plain object key (e.g. "materials/{guid}.pdf"),
    /// never a URL — same contract as LocalFileStorageService's file names, so DB rows and
    /// callers don't need to know which provider is active.
    /// </summary>
    public class R2FileStorageService : IFileStorageService
    {
        private const string PaymentProofPrefix = "payment-proofs/";
        private const string MaterialsPrefix = "materials/";

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

        private readonly IAmazonS3 _s3;
        private readonly string _bucket;

        public R2FileStorageService(IOptions<StorageSettings> options)
        {
            var settings = options.Value;
            _bucket = settings.R2Bucket;

            var config = new AmazonS3Config
            {
                ServiceURL = $"https://{settings.R2AccountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true,
                // R2's S3 compatibility layer doesn't implement the AWS SDK's default
                // request/response checksum headers (aws/aws-sdk-net#3610 — a header like
                // x-amz-checksum-crc32 comes back "not implemented") or chunked/streaming
                // SigV4 payload signing (see DisablePayloadSigning below) — both need to
                // be turned off explicitly rather than left at the SDK's defaults.
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
            };

            _s3 = new AmazonS3Client(
                new BasicAWSCredentials(settings.R2AccessKeyId, settings.R2SecretAccessKey),
                config);
        }

        public Task<string> SavePaymentProofAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default)
            => SaveAsync(PaymentProofPrefix, fileStream, fileName, contentType, cancellationToken);

        public async Task<(string StorageKey, int SizeKb)> SaveCourseMaterialAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            var sizeKb = (int)Math.Ceiling(fileStream.Length / 1024.0);
            var storedFileName = await SaveAsync(MaterialsPrefix, fileStream, fileName, contentType, cancellationToken);
            return (storedFileName, sizeKb);
        }

        public Task<(Stream Stream, string ContentType)> OpenPaymentProofAsync(
            string storageKey, CancellationToken cancellationToken = default)
            => OpenAsync(PaymentProofPrefix, storageKey, cancellationToken);

        public Task<(Stream Stream, string ContentType)> OpenMaterialAsync(
            string storageKey, CancellationToken cancellationToken = default)
            => OpenAsync(MaterialsPrefix, storageKey, cancellationToken);

        private async Task<string> SaveAsync(
            string prefix,
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken)
        {
            var extension = ExtensionByContentType.TryGetValue(contentType, out var knownExtension)
                ? knownExtension
                : Path.GetExtension(fileName);

            var storedFileName = $"{Guid.NewGuid():N}{extension}";

            var request = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = prefix + storedFileName,
                InputStream = fileStream,
                ContentType = contentType,
                AutoCloseStream = false,
                // See the AmazonS3Config comment above — without this, R2 rejects the
                // upload with "STREAMING-AWS4-HMAC-SHA256-PAYLOAD not implemented".
                DisablePayloadSigning = true
            };

            await _s3.PutObjectAsync(request, cancellationToken);

            return storedFileName;
        }

        private async Task<(Stream Stream, string ContentType)> OpenAsync(
            string prefix, string storageKey, CancellationToken cancellationToken)
        {
            // storageKey is DB-supplied; strip any directory component defensively so a
            // caller can never read outside its own prefix (key traversal) — the same
            // defense LocalFileStorageService applies to filesystem paths.
            var safeFileName = Path.GetFileName(storageKey);

            GetObjectResponse response;
            try
            {
                response = await _s3.GetObjectAsync(_bucket, prefix + safeFileName, cancellationToken);
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                throw new KeyNotFoundException("File was not found.");
            }

            var contentType = ContentTypeByExtension.TryGetValue(Path.GetExtension(safeFileName), out var known)
                ? known
                : "application/octet-stream";

            return (response.ResponseStream, contentType);
        }
    }
}
