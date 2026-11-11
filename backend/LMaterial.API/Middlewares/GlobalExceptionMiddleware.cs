using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FluentValidation;
using LMaterial.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LMaterial.API.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        object response;
        int statusCode;
        var traceId = context.TraceIdentifier;

        switch (exception)
        {
            case DomainException domainEx:
                statusCode = (int)HttpStatusCode.BadRequest;
                response = new
                {
                    error = true,
                    message = domainEx.Message,
                    code = "DOMAIN_ERROR",
                    traceId
                };
                _logger.LogWarning("Domain Exception: {Message}", domainEx.Message);
                break;

            case ValidationException validationEx:
                statusCode = (int)HttpStatusCode.BadRequest;
                var errors = validationEx.Errors != null && validationEx.Errors.Any()
                    ? validationEx.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
                    : new[] { new { field = "General", message = validationEx.Message } };

                response = new
                {
                    error = true,
                    message = "Falha na validação dos dados enviados.",
                    code = "VALIDATION_ERROR",
                    errors,
                    traceId
                };
                _logger.LogWarning("Validation Exception: {Message}", validationEx.Message);
                break;

            case DbUpdateConcurrencyException concurrencyEx:
                statusCode = (int)HttpStatusCode.Conflict;
                response = new
                {
                    error = true,
                    message = "O registro foi modificado por outro usuário. Recarregue os dados e tente novamente.",
                    code = "CONCURRENCY_CONFLICT",
                    traceId
                };
                _logger.LogWarning(concurrencyEx, "Concurrency Conflict Exception");
                break;

            case UnauthorizedAccessException authEx:
                statusCode = (int)HttpStatusCode.Unauthorized;
                response = new
                {
                    error = true,
                    message = authEx.Message,
                    code = "UNAUTHORIZED",
                    traceId
                };
                _logger.LogWarning("Unauthorized Exception: {Message}", authEx.Message);
                break;

            default:
                statusCode = (int)HttpStatusCode.InternalServerError;
                response = new
                {
                    error = true,
                    message = _env.IsDevelopment() 
                        ? $"Erro interno: {exception.Message}" 
                        : "Ocorreu um erro inesperado no servidor.",
                    code = "INTERNAL_SERVER_ERROR",
                    traceId
                };
                _logger.LogError(exception, "Unhandled Exception: {Message}", exception.Message);
                break;
        }

        context.Response.StatusCode = statusCode;
        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}
