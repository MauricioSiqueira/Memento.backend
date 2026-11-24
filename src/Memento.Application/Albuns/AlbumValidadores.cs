using FluentValidation;
using Memento.Domain.Entities;

namespace Memento.Application.Albuns;

internal static class RegrasAlbum
{
    public static IRuleBuilderOptions<T, string> NomeValido<T>(this IRuleBuilder<T, string> regra)
        => regra
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Informe o nome do álbum.")
            .Must(n => n is null || n.Trim().Length <= Album.TamanhoMaximoNome)
            .WithMessage($"O nome do álbum deve ter no máximo {Album.TamanhoMaximoNome} caracteres.");
}

public sealed class CriarAlbumRequestValidator : AbstractValidator<CriarAlbumRequest>
{
    public CriarAlbumRequestValidator() => RuleFor(x => x.Nome).NomeValido();
}

public sealed class RenomearAlbumRequestValidator : AbstractValidator<RenomearAlbumRequest>
{
    public RenomearAlbumRequestValidator() => RuleFor(x => x.Nome).NomeValido();
}
