using FluentValidation;

namespace Memento.Application.Common;

internal static class ValidacaoExtensions
{
    /// <summary>Valida e devolve o erro de validação agrupado por campo, ou null se válido.</summary>
    public static Erro? ValidarOuErro<T>(this IValidator<T> validator, T instancia)
    {
        var resultado = validator.Validate(instancia);
        if (resultado.IsValid) return null;

        var detalhes = resultado.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
        return Erro.Validacao(detalhes);
    }
}
