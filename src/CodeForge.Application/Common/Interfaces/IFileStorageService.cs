namespace CodeForge.Application.Common.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SavePaymentProofAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default);

        Task<(string Url, int SizeKb)> SaveCourseMaterialAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            CancellationToken cancellationToken = default);
    }
}
