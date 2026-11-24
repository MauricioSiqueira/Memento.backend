using System.Text.RegularExpressions;
using FluentValidation;
using Memento.Application.Usuarios;

namespace Memento.Application.Auth;

// Espelha as regras do app (Memento/src/features/auth/utils/validacao.ts).
public sealed partial class CadastroRequestValidator : AbstractValidator<CadastroRequest>
{
    public const int SenhaMinimo = 8;
    public const int SenhaMaximo = 16;
    private const int AnoMinimoNascimento = 1900;

    public CadastroRequestValidator(TimeProvider relogio)
    {
        RuleFor(x => x.NomeUsuario).NomeUsuarioValido();

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Informe seu e-mail.")
            .MaximumLength(254).WithMessage("O e-mail é muito longo.")
            .Must(e => string.IsNullOrWhiteSpace(e) || RegexEmail().IsMatch(e.Trim()))
            .WithMessage("Informe um e-mail válido.");

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("Informe uma senha.")
            .Must(s => string.IsNullOrEmpty(s) || (s.Length >= SenhaMinimo && s.Length <= SenhaMaximo))
            .WithMessage($"A senha deve ter entre {SenhaMinimo} e {SenhaMaximo} caracteres.")
            .Must(s => string.IsNullOrEmpty(s) || s.Length < SenhaMinimo || s.Length > SenhaMaximo || RegexSenhaComplexa().IsMatch(s))
            .WithMessage("A senha deve ter letra maiúscula, minúscula, número e símbolo.");

        RuleFor(x => x.ConfirmarSenha)
            .NotEmpty().WithMessage("Confirme sua senha.")
            .Equal(x => x.Senha).When(x => !string.IsNullOrEmpty(x.ConfirmarSenha)).WithMessage("As senhas não coincidem.");

        RuleFor(x => x.DataNascimento)
            .Must(d => d != default).WithMessage("Informe sua data de nascimento.")
            .Must(d => d == default || d <= DateOnly.FromDateTime(relogio.GetUtcNow().UtcDateTime))
            .WithMessage("A data de nascimento não pode ser no futuro.")
            .Must(d => d == default || d.Year >= AnoMinimoNascimento)
            .WithMessage($"Informe um ano a partir de {AnoMinimoNascimento}.");
    }

    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$")]
    private static partial Regex RegexEmail();

    [GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9\s]).+$")]
    private static partial Regex RegexSenhaComplexa();
}

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Informe seu e-mail.");
        RuleFor(x => x.Senha).NotEmpty().WithMessage("Informe sua senha.");
    }
}

public sealed class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
        => RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Informe o refresh token.");
}
