namespace CodeForge.Application.Certificates.Common
{
    /// <summary>
    /// Generates the human-facing serial number and the opaque public verification code
    /// for a certificate. Uniqueness is ultimately guaranteed by unique DB indexes; the
    /// randomness here just makes collisions astronomically unlikely and the codes
    /// unguessable. The verification code (not the serial) is what the public lookup
    /// endpoint accepts, so it must be non-sequential.
    /// </summary>
    public static class CertificateCodeGenerator
    {
        public static string NewSerialNumber(int year)
        {
            // e.g. CF-2026-8F3A21 — short, readable, unique per the DB index.
            var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
            return $"CF-{year}-{suffix}";
        }

        public static string NewVerificationCode()
        {
            // 32-char opaque token used by the public verify endpoint.
            return Guid.NewGuid().ToString("N");
        }
    }
}
