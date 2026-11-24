namespace Memento.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string Secao = "Jwt";

    public string Issuer { get; set; } = "Memento.Api";
    public string Audience { get; set; } = "Memento.App";
    /// <summary>Chave HMAC (HS256) com no mínimo 32 caracteres.</summary>
    public string Key { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
}
