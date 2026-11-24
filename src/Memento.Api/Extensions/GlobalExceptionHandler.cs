using Memento.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Memento.Api.Extensions;

/// <summary>Converte exceções em ProblemDetails sem vazar stack trace ou detalhes internos.</summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        var (status, titulo) = exception switch
        {
            DomainException d => (StatusCodes.Status400BadRequest, d.Message),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Não autenticado."),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Requisição inválida."),
            _ => (StatusCodes.Status500InternalServerError, "Erro inesperado ao processar a requisição.")
        };

        if (status >= 500) logger.LogError(exception, "Erro não tratado em {Metodo} {Caminho}", http.Request.Method, http.Request.Path);

        http.Response.StatusCode = status;
        await http.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = status, Title = titulo }, options: null, contentType: "application/problem+json", ct);
        return true;
    }
}
