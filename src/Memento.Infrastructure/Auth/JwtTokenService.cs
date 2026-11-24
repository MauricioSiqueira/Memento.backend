using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Memento.Application.Abstractions;
using Memento.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Memento.Infrastructure.Auth;

internal sealed class JwtTokenService(IOptions<JwtOptions> opcoes, TimeProvider relogio) : ITokenService
{
    private readonly JwtOptions _opcoes = opcoes.Value;
    private readonly JwtSecurityTokenHandler _handler = new() { SetDefaultTimesOnTokenCreation = false };

    public TimeSpan ValidadeRefreshToken => TimeSpan.FromDays(_opcoes.RefreshTokenDays);

    public AccessToken GerarAccessToken(Usuario usuario, Guid sessaoId)
    {
        var agora = relogio.GetUtcNow();
        var expira = agora.AddMinutes(_opcoes.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new(JwtRegisteredClaimNames.UniqueName, usuario.NomeUsuario),
            new(ClaimNomes.Sessao, sessaoId.ToString())
        };

        var credenciais = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opcoes.Key)), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _opcoes.Issuer, _opcoes.Audience, claims,
            notBefore: agora.UtcDateTime, expires: expira.UtcDateTime, signingCredentials: credenciais);

        return new AccessToken(_handler.WriteToken(token), expira);
    }

    public string GerarRefreshToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public string HashRefreshToken(string refreshToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken))).ToLowerInvariant();
}

/// <summary>Nomes das claims customizadas do access token.</summary>
public static class ClaimNomes
{
    /// <summary>Id da <c>Sessao</c> que emitiu o token (usado no logout).</summary>
    public const string Sessao = "sid";
}
