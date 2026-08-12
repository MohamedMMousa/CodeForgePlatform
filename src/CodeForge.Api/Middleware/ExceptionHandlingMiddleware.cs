using System.Text.Json;
using CodeForge.Application.Common.Constants;
using CodeForge.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CodeForge.Api.Middleware
{
    /// <summary>
    /// Centralizes translation of exceptions thrown by MediatR handlers (and the
    /// FluentValidation pipeline) into a consistent ProblemDetails JSON envelope,
    /// replacing the per-controller try/catch mapping that used to be duplicated.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception exception)
            {
                await HandleExceptionAsync(context, exception);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning(
                    exception,
                    "Response has already started; cannot write error envelope for {Path}.",
                    context.Request.Path);
                throw exception;
            }

            ProblemDetails problem = exception switch
            {
                ValidationException validationException => BuildValidationProblem(validationException),
                PasswordChangeRequiredException => BuildForbiddenWithCode(
                    AuthErrorCodes.PasswordChangeRequired, exception.Message),
                CsrfValidationException => BuildForbiddenWithCode(
                    AuthErrorCodes.CsrfValidationFailed, exception.Message),
                UnauthorizedAccessException => BuildProblem(
                    StatusCodes.Status401Unauthorized, "Unauthorized", exception.Message),
                KeyNotFoundException => BuildProblem(
                    StatusCodes.Status404NotFound, "Not Found", exception.Message),
                InvalidOperationException => BuildProblem(
                    StatusCodes.Status400BadRequest, "Bad Request", exception.Message),
                _ => BuildProblem(
                    StatusCodes.Status500InternalServerError,
                    "Server Error",
                    "An unexpected error occurred.")
            };

            if (problem.Status == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception for {Path}.", context.Request.Path);
            }

            context.Response.Clear();
            context.Response.StatusCode = problem.Status!.Value;
            context.Response.ContentType = "application/problem+json";

            var payload = JsonSerializer.Serialize(problem, problem.GetType(), SerializerOptions);
            await context.Response.WriteAsync(payload);
        }

        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private static ValidationProblemDetails BuildValidationProblem(ValidationException exception)
        {
            // Group once and de-duplicate on the failures themselves, so `errors` and
            // `errorCodes` stay index-aligned per property — the frontend pairs them
            // positionally to localize each message.
            var failuresByProperty = exception.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .GroupBy(error => error.ErrorMessage)
                        .Select(duplicates => duplicates.First())
                        .ToArray());

            var errors = failuresByProperty.ToDictionary(
                entry => entry.Key,
                entry => entry.Value.Select(failure => failure.ErrorMessage).ToArray());

            var errorCodes = failuresByProperty.ToDictionary(
                entry => entry.Key,
                entry => entry.Value.Select(failure => failure.ErrorCode ?? string.Empty).ToArray());

            var problem = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Failed"
            };

            problem.Extensions["errorCodes"] = errorCodes;
            return problem;
        }

        private static ProblemDetails BuildProblem(int status, string title, string detail) => new()
        {
            Status = status,
            Title = title,
            Detail = detail
        };

        private static ProblemDetails BuildForbiddenWithCode(string code, string detail)
        {
            var problem = BuildProblem(StatusCodes.Status403Forbidden, "Forbidden", detail);
            problem.Extensions["code"] = code;
            return problem;
        }
    }
}
