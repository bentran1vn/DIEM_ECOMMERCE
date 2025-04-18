using System.Text.Json;
using DiemEcommerce.Domain.Exceptions;

namespace DiemEcommerce.API.Middlewares;

internal sealed class ExceptionHandlingMiddleware : IMiddleware
{
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger)
        => _logger = logger;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception e)
        {
            _logger.LogError(e, e.Message);

            await HandleExceptionAsync(context, e);
        }
    }

    private async Task HandleExceptionAsync(HttpContext httpContext, Exception exception)
    {
        var statusCode = GetStatusCode(exception);

        _logger.LogError("Handling exception: {ExceptionType}, Status Code: {StatusCode}, Message: {Message}", 
            exception.GetType().Name, statusCode, exception.Message);

        var response = new
        {
            title = GetTitle(exception),
            status = statusCode,
            detail = exception.Message,
            errors = GetErrors(exception),
        };

        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = statusCode;

        // Log request details
        var request = httpContext.Request;
        _logger.LogInformation("Request Method: {Method}, Path: {Path}, ContentType: {ContentType}", 
            request.Method, request.Path, request.ContentType);

        // Log form data if present
        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync();
            foreach (var key in form.Keys)
            {
                _logger.LogInformation("Form Data - Key: {Key}, Value: {Value}", key, form[key]);
            }
        }

        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private static int GetStatusCode(Exception exception) =>
        exception switch
        {
            IdentityException.TokenException => StatusCodes.Status401Unauthorized,
            BadRequestException => StatusCodes.Status400BadRequest,
            NotFoundException => StatusCodes.Status404NotFound,
            Application.Exceptions.ValidationException => StatusCodes.Status422UnprocessableEntity,
            FluentValidation.ValidationException => StatusCodes.Status422UnprocessableEntity,
            FormatException => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };

    private static string GetTitle(Exception exception) =>
        exception switch
        {
            DomainException applicationException => applicationException.Title,
            _ => "Server Error"
        };

    private static IReadOnlyCollection<Application.Exceptions.ValidationError>? GetErrors(Exception exception)
    {
        IReadOnlyCollection<Application.Exceptions.ValidationError> errors = null;

        if (exception is Application.Exceptions.ValidationException validationException)
        {
            errors = validationException.Errors;
        }

        return errors;
    }

}