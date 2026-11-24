using Memento.Application.Usuarios;

namespace Memento.Application.Auth;

public sealed record CadastroRequest(
    string NomeUsuario,
    string Email,
    string Senha,
    string ConfirmarSenha,
    DateOnly DataNascimento);

public sealed record LoginRequest(string Email, string Senha);

public sealed record RefreshRequest(string RefreshToken);

public sealed record TokensDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiraEm,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiraEm,
    UsuarioDto Usuario);
