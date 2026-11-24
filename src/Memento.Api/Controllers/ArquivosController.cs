using Memento.Api.Extensions;
using Memento.Application.Arquivos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Memento.Api.Controllers;

[ApiController]
[Route("api/arquivos")]
public class ArquivosController(ArquivoService arquivos) : ControllerBase
{
    /// <summary>Serve o conteúdo do blob (container privado) para quem tem permissão.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Obter(Guid id, CancellationToken ct)
    {
        var resultado = await arquivos.ObterConteudoAsync(User.UsuarioId(), id, ct);
        if (!resultado.Sucesso) return this.ParaErro(resultado.Erro!);

        var arquivo = resultado.Valor;
        Response.Headers.CacheControl = "private, max-age=86400";
        return File(arquivo.Conteudo, arquivo.ContentType, lastModified: null,
            entityTag: new EntityTagHeaderValue($"\"{arquivo.Hash}\""), enableRangeProcessing: true);
    }
}
