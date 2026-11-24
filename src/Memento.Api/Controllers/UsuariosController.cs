using Memento.Api.Extensions;
using Memento.Application.Usuarios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Memento.Api.Controllers;

[ApiController]
[Route("api/usuarios/me")]
public class UsuariosController(UsuarioService usuarios) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Obter(CancellationToken ct)
        => this.ParaActionResult(await usuarios.ObterAsync(User.UsuarioId(), ct));

    [HttpPut]
    public async Task<IActionResult> Atualizar([FromBody] AtualizarPerfilRequest request, CancellationToken ct)
        => this.ParaActionResult(await usuarios.AtualizarPerfilAsync(User.UsuarioId(), request, ct));

    /// <summary>Multipart com o campo <c>arquivo</c>. Substitui a foto atual (e apaga o blob antigo).</summary>
    [HttpPut("foto")]
    [EnableRateLimiting(PoliticasRateLimit.Upload)]
    [RequestSizeLimit(LimitesUpload.CorpoMaximoBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimitesUpload.CorpoMaximoBytes)]
    public async Task<IActionResult> TrocarFoto(IFormFile? arquivo, CancellationToken ct)
    {
        if (arquivo is null)
            return this.ParaErro(Memento.Application.Common.Erro.Validacao("arquivo", "Envie o arquivo no campo 'arquivo'."));

        await using var upload = arquivo.OpenReadStream();
        return this.ParaActionResult(await usuarios.TrocarFotoPerfilAsync(
            User.UsuarioId(), new(arquivo.FileName, arquivo.Length, upload), ct));
    }

    [HttpDelete("foto")]
    public async Task<IActionResult> RemoverFoto(CancellationToken ct)
        => this.ParaActionResult(await usuarios.RemoverFotoPerfilAsync(User.UsuarioId(), ct));
}
