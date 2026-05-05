using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Modules.Airports.Domain.Exceptions;
using Modules.Scenarios.Domain.Exceptions;

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
            // passes the request to the next middleware/endpoint
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            // only handle if we haven't started writing the response yet
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // maps each exception type to the correct http status code and message
        var problemDetails = exception switch
        {
            ScenarioConfigNotFoundException e => ProblemDetailsDefaults.Create(StatusCodes.Status404NotFound, e.Message),
            InvalidScenarioConfigException e => ProblemDetailsDefaults.Create(StatusCodes.Status422UnprocessableEntity, e.Message),
            AirportNotFoundException e => ProblemDetailsDefaults.Create(StatusCodes.Status404NotFound, e.Message),
            RunwayNotFoundException e => ProblemDetailsDefaults.Create(StatusCodes.Status404NotFound, e.Message),
            UnauthorizedAccessException => ProblemDetailsDefaults.Create(StatusCodes.Status401Unauthorized, "Authentication failed."),
            FluentValidation.ValidationException fve => ProblemDetailsDefaults.Create(
                StatusCodes.Status400BadRequest,
                string.Join(" ", fve.Errors.Select(e => e.ErrorMessage))),
            ValidationException validationException => ProblemDetailsDefaults.Create(
                StatusCodes.Status400BadRequest,
                validationException.ValidationResult?.ErrorMessage ?? validationException.Message),
            KeyNotFoundException keyNotFoundException => ProblemDetailsDefaults.Create(StatusCodes.Status404NotFound, keyNotFoundException.Message),
            ArgumentException argumentException => ProblemDetailsDefaults.Create(StatusCodes.Status400BadRequest, argumentException.Message),
            InvalidOperationException invalidOperationException => ProblemDetailsDefaults.Create(StatusCodes.Status400BadRequest, invalidOperationException.Message),
            _ => ProblemDetailsDefaults.Create(StatusCodes.Status500InternalServerError)
        };

        // 5xx = something broke on our side, log as error; 4xx = bad request, log as warning
        if (problemDetails.Status >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            logger.LogWarning(exception, "Request failed with {StatusCode} for {Method} {Path}", problemDetails.Status, context.Request.Method, context.Request.Path);

        context.Response.Clear();
        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        // writes the problem details json to the response
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }
}
