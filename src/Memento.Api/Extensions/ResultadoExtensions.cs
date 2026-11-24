using Memento.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Memento.Api.Extensions;

/// <summary>Traduz o <see cref="Resultado"/> da camada de aplicação para respostas HTTP (RFC 7807).</summary>
public static class ResultadoExtensions
{
    public static IActionResult ParaActionResult(this ControllerBase controller, Resultado resultado, Func<IActionResult>? sucesso = null)
        => resultado.Sucesso ? (sucesso?.Invoke() ?? controller.NoContent()) : controller.ParaErro(resultado.Erro!);

    public static IActionResult ParaActionResult<T>(this ControllerBase controller, Resultado<T> resultado, Func<T, IActionResult>? sucesso = null)
        => resultado.Sucesso
            ? (sucesso?.Invoke(resultado.Valor) ?? controller.Ok(resultado.Valor))
            : controller.ParaErro(resultado.Erro!);

    public static IActionResult ParaErro(this ControllerBase controller, Erro erro)
    {
        var status = erro.Tipo switch
        {
            TipoErro.Validacao => StatusCodes.Status400BadRequest,
            TipoErro.NaoAutenticado => StatusCodes.Status401Unauthorized,
            TipoErro.Proibido => StatusCodes.Status403Forbidden,
            TipoErro.NaoEncontrado => StatusCodes.Status404NotFound,
            TipoErro.Conflito => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        if (erro.Detalhes is { Count: > 0 })
        {
            var erros = erro.Detalhes.ToDictionary(d => ParaCamelCase(d.Key), d => d.Value);
            return new ObjectResult(new ValidationProblemDetails(erros)
            {
                Status = status,
                Title = erro.Mensagem
            })
            { StatusCode = status, ContentTypes = { "application/problem+json" } };
        }

        return controller.Problem(statusCode: status, title: erro.Mensagem, type: erro.Codigo);
    }

    private static string ParaCamelCase(string nome)
        => string.IsNullOrEmpty(nome) ? nome : char.ToLowerInvariant(nome[0]) + nome[1..];
}
