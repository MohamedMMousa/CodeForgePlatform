namespace CodeForge.Application.Common.Models
{
    public class StorageSettings
    {
        public const string SectionName = "Storage";

        /// <summary>
        /// "Local" (default) writes to PrivateStorage/ on the container's own disk — fine
        /// for local dev, but destroyed on every deploy/restart on a host with no
        /// persistent volume (e.g. Render's free tier). "R2" stores the same files in
        /// Cloudflare R2 instead; set this in production. See LocalFileStorageService /
        /// R2FileStorageService, both implementations of IFileStorageService.
        /// </summary>
        public string Provider { get; set; } = "Local";

        public string R2AccountId { get; set; } = string.Empty;
        public string R2Bucket { get; set; } = string.Empty;
        public string R2AccessKeyId { get; set; } = string.Empty;
        public string R2SecretAccessKey { get; set; } = string.Empty;
    }
}
