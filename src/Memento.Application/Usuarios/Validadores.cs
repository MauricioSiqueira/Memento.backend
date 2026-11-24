using FluentValidation;

namespace Memento.Application.Usuarios;

public static class RegrasNomeUsuario
{
    public const int Minimo = 3;
    public const int Maximo = 50;

    public static IRuleBuilderOptions<T, string> NomeUsuarioValido<T>(this IRuleBuilder<T, string> regra)
        => regra
            .NotEmpty().WithMessage("Informe um nome de usuário.")
            .MinimumLength(Minimo).WithMessage($"O nome de usuário deve ter pelo menos {Minimo} caracteres.")
            .MaximumLength(Maximo).WithMessage($"O nome de usuário deve ter no máximo {Maximo} caracteres.")
            .Must(n => n is null || !n.Any(char.IsWhiteSpace)).WithMessage("O nome de usuário não pode conter espaços.");
}

public sealed class AtualizarPerfilRequestValidator : AbstractValidator<AtualizarPerfilRequest>
{
    public AtualizarPerfilRequestValidator() => RuleFor(x => x.NomeUsuario).NomeUsuarioValido();
}
