using System.Text;
using System.Text.Json;
using CodeForge.Api.Middleware;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CodeForge.UnitTests.Middleware
{
    public class ExceptionHandlingMiddlewareTests
    {
        private static async Task<(int status, string body)> InvokeWith(Exception exception)
        {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            RequestDelegate next = _ => throw exception;
            var middleware = new ExceptionHandlingMiddleware(
                next, NullLogger<ExceptionHandlingMiddleware>.Instance);

            await middleware.InvokeAsync(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync();
            return (context.Response.StatusCode, body);
        }

        [Fact]
        public async Task ValidationException_MapsTo400_WithErrors()
        {
            var failures = new[] { new ValidationFailure("Email", "Email is required.") };

            var (status, body) = await InvokeWith(new ValidationException(failures));

            status.Should().Be(StatusCodes.Status400BadRequest);
            body.Should().Contain("Email is required.");
        }

        [Fact]
        public async Task UnauthorizedAccessException_MapsTo401()
        {
            var (status, _) = await InvokeWith(new UnauthorizedAccessException("nope"));
            status.Should().Be(StatusCodes.Status401Unauthorized);
        }

        [Fact]
        public async Task KeyNotFoundException_MapsTo404()
        {
            var (status, _) = await InvokeWith(new KeyNotFoundException("missing"));
            status.Should().Be(StatusCodes.Status404NotFound);
        }

        [Fact]
        public async Task InvalidOperationException_MapsTo400()
        {
            var (status, _) = await InvokeWith(new InvalidOperationException("bad state"));
            status.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task UnexpectedException_MapsTo500_WithoutLeakingMessage()
        {
            var (status, body) = await InvokeWith(new Exception("secret internal detail"));

            status.Should().Be(StatusCodes.Status500InternalServerError);
            body.Should().NotContain("secret internal detail");
        }

        [Fact]
        public async Task Problem_IsSerializedAsProblemJson()
        {
            var (_, body) = await InvokeWith(new KeyNotFoundException("missing"));

            // Should be valid JSON with a ProblemDetails-style "status" field.
            using var document = JsonDocument.Parse(body);
            document.RootElement.GetProperty("status").GetInt32().Should().Be(404);
        }
    }
}
