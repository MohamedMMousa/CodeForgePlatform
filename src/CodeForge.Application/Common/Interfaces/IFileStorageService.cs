namespace CodeForge.Application.Common.Interfaces
{
    /// <summary>
    /// Private file storage — never web-servable directly. Save* methods return an
    /// opaque storage key (not a URL); callers must go through an authorized API
    /// endpoint that calls Open*Async to stream the content back.
    /// </summary>
    public interface IFileStorageService
    {
        Task<string> SavePaymentProofAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default);

        Task<(string StorageKey, int SizeKb)> SaveCourseMaterialAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default);

        Task<(Stream Stream, string ContentType)> OpenPaymentProofAsync(
            string storageKey,
            CancellationToken cancellationToken = default);

        Task<(Stream Stream, string ContentType)> OpenMaterialAsync(
            string storageKey,
            CancellationToken cancellationToken = default);
    }
}
