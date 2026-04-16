using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace Api.Errors;

public sealed class GlobalExceptionHandlingMiddleware(
    RequestDelegate next,
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problemDetails = exception switch
        {
            UnauthorizedAccessException => ProblemDetailsDefaults.Create(
                StatusCodes.Status401Unauthorized,
                "Authentication failed."),
            ValidationException validationException => ProblemDetailsDefaults.Create(
                StatusCodes.Status400BadRequest,
                validationException.ValidationResult?.ErrorMessage ?? validationException.Message),
            KeyNotFoundException keyNotFoundException => ProblemDetailsDefaults.Create(
                StatusCodes.Status404NotFound,
                keyNotFoundException.Message),
            ArgumentException argumentException => ProblemDetailsDefaults.Create(
                StatusCodes.Status400BadRequest,
                argumentException.Message),
            InvalidOperationException invalidOperationException => ProblemDetailsDefaults.Create(
                StatusCodes.Status400BadRequest,
                invalidOperationException.Message),
            _ => ProblemDetailsDefaults.Create(StatusCodes.Status500InternalServerError)
        };

        if (problemDetails.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            logger.LogWarning(exception, "Request failed with {StatusCode} for {Method} {Path}", problemDetails.Status, context.Request.Method, context.Request.Path);
        }

        context.Response.Clear();
        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }
}
