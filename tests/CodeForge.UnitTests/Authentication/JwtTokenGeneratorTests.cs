using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Models;
using CodeForge.Domain.Entities;
using CodeForge.Infrastructure.Authentication;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace CodeForge.UnitTests.Authentication
{
    public class JwtTokenGeneratorTests
    {
        private static JwtTokenGenerator CreateSut() => new(Options.Create(new JwtSettings
        {
            Secret = "unit_test_secret_key_that_is_long_enough_1234567890",
            Issuer = "CodeForgeTests",
            Audience = "CodeForgeTestsUsers",
            ExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7
        }));

        [Fact]
        public void HashToken_IsDeterministic_ForSameInput()
        {
            var sut = CreateSut();
            const string token = "some-refresh-token-value";

            sut.HashToken(token).Should().Be(sut.HashToken(token));
        }

        [Fact]
        public void HashToken_ProducesDifferentHashes_ForDifferentInputs()
        {
            var sut = CreateSut();

            sut.HashToken("token-a").Should().NotBe(sut.HashToken("token-b"));
        }

        [Fact]
        public void HashToken_DoesNotReturnPlaintext()
        {
            var sut = CreateSut();
            const string token = "plaintext-token";

            sut.HashToken(token).Should().NotBe(token);
        }

        [Fact]
        public void GenerateRefreshToken_ProducesUniqueValues()
        {
            var sut = CreateSut();

            sut.GenerateRefreshToken().Should().NotBe(sut.GenerateRefreshToken());
        }

        [Fact]
        public void GenerateToken_EmbedsUserClaims()
        {
            var sut = CreateSut();
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "student@codeforge.academy",
                FullName = "Test Student",
                Role = "student",
                PasswordHash = "x"
            };

            var token = sut.GenerateToken(user);

            // JWTs are three base64url segments separated by dots.
            token.Split('.').Should().HaveCount(3);
        }

        [Theory]
        [InlineData(false, "false")]
        [InlineData(true, "true")]
        public void GenerateToken_EmbedsMustChangePasswordClaim(bool mustChangePassword, string expected)
        {
            var sut = CreateSut();
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "student@codeforge.academy",
                FullName = "Test Student",
                Role = "student",
                PasswordHash = "x",
                MustChangePassword = mustChangePassword
            };

            var token = sut.GenerateToken(user);
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

            jwt.Claims.Single(c => c.Type == CustomClaimTypes.MustChangePassword).Value.Should().Be(expected);
        }
    }
}
