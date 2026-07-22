namespace CodeForge.Application.Common.Models
{
    public class EmailSettings
    {
        public const string SectionName = "EmailSettings";

        /// <summary>
        /// When false (default), no SMTP host is configured and a dev logging sender is used
        /// instead of attempting real delivery.
        /// </summary>
        public bool Enabled { get; set; } = false;

        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = "no-reply@codeforge.academy";
        public string FromName { get; set; } = "CodeForge Academy";

        /// <summary>
        /// Base URL of the front-end, used to build user-facing links (e.g. password reset).
        /// </summary>
        public string FrontendBaseUrl { get; set; } = "http://localhost:3000";
    }
}
