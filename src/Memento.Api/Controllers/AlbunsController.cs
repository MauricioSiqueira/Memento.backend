using Memento.Api.Extensions;
using Memento.Application.Albuns;
using Memento.Application.Arquivos;
using Memento.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Memento.Api.Controllers;

[ApiController]
[Route("api/albuns")]
public class AlbunsController(AlbumService albuns) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken ct)
        => Ok(await albuns.ListarAsync(User.UsuarioId(), ct));

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarAlbumRequest request, CancellationToken ct)
        => this.ParaActionResult(
            await albuns.CriarAsync(User.UsuarioId(), request, ct),
            album => CreatedAtAction(nameof(Obter), new { id = album.Id }, album));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id, CancellationToken ct)
        => this.ParaActionResult(await albuns.ObterAsync(User.UsuarioId(), id, ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Renomear(Guid id, [FromBody] RenomearAlbumRequest request, CancellationToken ct)
        => this.ParaActionResult(await albuns.RenomearAsync(User.UsuarioId(), id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Excluir(Guid id, CancellationToken ct)
        => this.ParaActionResult(await albuns.ExcluirAsync(User.UsuarioId(), id, ct));

    /// <summary>Favorito é pessoal: cada participante tem o seu.</summary>
    [HttpPut("{id:guid}/favorito")]
    public async Task<IActionResult> Favoritar(Guid id, [FromBody] FavoritoRequest request, CancellationToken ct)
        => this.ParaActionResult(await albuns.DefinirFavoritoAsync(User.UsuarioId(), id, request.Favorito, ct));

    // ---- Fotos ----

    /// <summary>Multipart com um ou mais campos <c>arquivos</c>.</summary>
    [HttpPost("{id:guid}/fotos")]
    [EnableRateLimiting(PoliticasRateLimit.Upload)]
    [RequestSizeLimit(LimitesUpload.CorpoMaximoBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimitesUpload.CorpoMaximoBytes)]
    public async Task<IActionResult> AdicionarFotos(Guid id, [FromForm] List<IFormFile>? arquivos, CancellationToken ct)
    {
        var enviados = arquivos ?? [];
        var streams = new List<Stream>();
        try
        {
            var uploads = new List<ArquivoUpload>();
            foreach (var arquivo in enviados)
            {
                var stream = arquivo.OpenReadStream();
                streams.Add(stream);
                uploads.Add(new ArquivoUpload(arquivo.FileName, arquivo.Length, stream));
            }

            return this.ParaActionResult(
                await albuns.AdicionarFotosAsync(User.UsuarioId(), id, uploads, ct),
                fotos => StatusCode(StatusCodes.Status201Created, fotos));
        }
        finally
        {
            foreach (var stream in streams) await stream.DisposeAsync();
        }
    }

    [HttpDelete("{id:guid}/fotos/{arquivoId:guid}")]
    public async Task<IActionResult> RemoverFoto(Guid id, Guid arquivoId, CancellationToken ct)
        => this.ParaActionResult(await albuns.RemoverFotoAsync(User.UsuarioId(), id, arquivoId, ct));

    // ---- Membros e convite ----

    [HttpGet("{id:guid}/membros")]
    public async Task<IActionResult> ListarMembros(Guid id, CancellationToken ct)
        => this.ParaActionResult(await albuns.ListarMembrosAsync(User.UsuarioId(), id, ct));

    [HttpDelete("{id:guid}/membros/{usuarioId:guid}")]
    public async Task<IActionResult> RemoverMembro(Guid id, Guid usuarioId, CancellationToken ct)
        => this.ParaActionResult(await albuns.RemoverMembroAsync(User.UsuarioId(), id, usuarioId, ct));

    [HttpGet("{id:guid}/convite")]
    public async Task<IActionResult> ObterConvite(Guid id, CancellationToken ct)
        => this.ParaActionResult(await albuns.ObterConviteAsync(User.UsuarioId(), id, ct));

    [HttpPost("{id:guid}/convite/regenerar")]
    public async Task<IActionResult> RegenerarConvite(Guid id, CancellationToken ct)
        => this.ParaActionResult(await albuns.RegenerarConviteAsync(User.UsuarioId(), id, ct));

    /// <summary>"Escanear QR Code": recebe o texto lido do QR (o link de convite) e entra no álbum.</summary>
    [HttpPost("entrar")]
    public async Task<IActionResult> Entrar([FromBody] EntrarAlbumRequest request, CancellationToken ct)
        => this.ParaActionResult(await albuns.EntrarPorLinkAsync(User.UsuarioId(), request, ct));
}
