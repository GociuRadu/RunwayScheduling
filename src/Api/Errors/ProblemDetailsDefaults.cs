using Microsoft.AspNetCore.Mvc;

namespace Api.Errors;

internal static class ProblemDetailsDefaults
{
    public static ProblemDetails Create(int statusCode, string? detail = null)
    {
        var (title, defaultDetail) = statusCode switch
        {
            StatusCodes.Status400BadRequest => ("Invalid request", "One or more request values are invalid."),
            StatusCodes.Status401Unauthorized => ("Unauthorized", "Authentication is required or the session has expired."),
            StatusCodes.Status403Forbidden => ("Forbidden", "You are not allowed to access this resource."),
            StatusCodes.Status404NotFound => ("Not found", "The requested resource was not found."),
            StatusCodes.Status409Conflict => ("Conflict", "The request could not be completed because of a conflict."),
            StatusCodes.Status429TooManyRequests => ("Too many requests", "Too many requests were sent. Try again later."),
            _ => ("Internal server error", "An unexpected error occurred.")
        };

        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail ?? defaultDetail
        };
    }
}
