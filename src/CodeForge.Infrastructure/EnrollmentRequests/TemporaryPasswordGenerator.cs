using System.Security.Cryptography;
using CodeForge.Application.Common.Interfaces;

namespace CodeForge.Infrastructure.EnrollmentRequests
{
    public class TemporaryPasswordGenerator : ITemporaryPasswordGenerator
    {
        private const string Characters = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@$?";

        public string Generate()
        {
            return RandomNumberGenerator.GetString(Characters, 14);
        }
    }
}
