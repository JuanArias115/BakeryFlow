using System.Net;
using System.Text.Json;
using BakeryFlow.Application.Common.Exceptions;
using FluentValidation;

namespace BakeryFlow.Api.Common;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await WriteAsync(context, exception.Message, exception.Errors.Select(x => x.ErrorMessage));
        }
        catch (NotFoundException exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            await WriteAsync(context, exception.Message);
        }
        catch (BusinessRuleException exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await WriteAsync(context, exception.Message);
        }
        catch (AppException exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await WriteAsync(context, exception.Message);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await WriteAsync(context, "Ocurrió un error interno.");
        }
    }

    private static async Task WriteAsync(HttpContext context, string message, IEnumerable<string>? errors = null)
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            success = false,
            message,
            errors = errors?.ToArray()
        }));
    }
}
