using Memento.Api.Extensions;
using Memento.Application.Abstractions;
using Memento.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Memento.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    [HttpPost("cadastro")]
    [AllowAnonymous]
    [EnableRateLimiting(PoliticasRateLimit.Auth)]
    public async Task<IActionResult> Cadastrar([FromBody] CadastroRequest request, CancellationToken ct)
        => this.ParaActionResult(await auth.CadastrarAsync(request, ct), usuario => Created("/api/usuarios/me", usuario));

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(PoliticasRateLimit.Auth)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
        => this.ParaActionResult(await auth.LoginAsync(request, Cliente(), ct));

    /// <summary>Anônima de propósito: quando o cliente precisa renovar, o access token já expirou.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(PoliticasRateLimit.Auth)]
    public async Task<IActionResult> Renovar([FromBody] RefreshRequest request, CancellationToken ct)
        => this.ParaActionResult(await auth.RenovarAsync(request, ct));

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (User.SessaoId() is { } sessaoId) await auth.SairAsync(sessaoId, ct);
        return NoContent();
    }

    private ClienteInfo Cliente()
        => new(Request.Headers.UserAgent.ToString(), HttpContext.Connection.RemoteIpAddress?.ToString());
}
