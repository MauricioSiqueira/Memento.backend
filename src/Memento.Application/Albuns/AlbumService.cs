using FluentValidation;
using Memento.Application.Abstractions;
using Memento.Application.Arquivos;
using Memento.Application.Common;
using Memento.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Memento.Application.Albuns;

public sealed class AlbumService(
    IAlbumRepository albuns,
    IArquivoRepository arquivosRepositorio,
    ArquivoService arquivos,
    IUnitOfWork unitOfWork,
    IValidator<CriarAlbumRequest> validadorCriar,
    IValidator<RenomearAlbumRequest> validadorRenomear,
    IOptions<UploadOptions> opcoesUpload,
    TimeProvider relogio)
{
    // Não ser membro responde igual a "não existe": não revela álbuns alheios.
    private static readonly Erro AlbumNaoEncontrado = Erro.NaoEncontrado("Álbum não encontrado.");
    private static readonly Erro ApenasDono = Erro.Proibido("Apenas o dono do álbum pode fazer isso.");

    public Task<IReadOnlyList<AlbumResumoDto>> ListarAsync(Guid usuarioId, CancellationToken ct = default)
        => albuns.ListarResumosAsync(usuarioId, ct);

    public async Task<Resultado<AlbumDetalheDto>> ObterAsync(Guid usuarioId, Guid albumId, CancellationToken ct = default)
    {
        var detalhe = await albuns.ObterDetalheAsync(albumId, usuarioId, ct);
        return detalhe is null ? AlbumNaoEncontrado : detalhe;
    }

    public async Task<Resultado<AlbumDetalheDto>> CriarAsync(Guid usuarioId, CriarAlbumRequest request, CancellationToken ct = default)
    {
        if (validadorCriar.ValidarOuErro(request) is { } invalido) return invalido;

        var album = Album.Criar(request.Nome, usuarioId, relogio.GetUtcNow());
        albuns.Adicionar(album);
        await unitOfWork.SalvarAsync(ct);
        return await ObterAsync(usuarioId, album.Id, ct);
    }

    public async Task<Resultado<AlbumDetalheDto>> RenomearAsync(
        Guid usuarioId, Guid albumId, RenomearAlbumRequest request, CancellationToken ct = default)
    {
        if (validadorRenomear.ValidarOuErro(request) is { } invalido) return invalido;

        var (album, membro, erro) = await ObterComoMembroAsync(albumId, usuarioId, ct);
        if (erro is not null) return erro;
        if (!membro!.EhDono) return ApenasDono;

        album!.Renomear(request.Nome);
        await unitOfWork.SalvarAsync(ct);
        return await ObterAsync(usuarioId, albumId, ct);
    }

    /// <summary>Exclui o álbum e todas as suas fotos (banco primeiro, blobs depois).</summary>
    public async Task<Resultado> ExcluirAsync(Guid usuarioId, Guid albumId, CancellationToken ct = default)
    {
        var (album, membro, erro) = await ObterComoMembroAsync(albumId, usuarioId, ct);
        if (erro is not null) return erro;
        if (!membro!.EhDono) return ApenasDono;

        var fotos = await albuns.ListarArquivosDoAlbumAsync(albumId, ct);
        foreach (var arquivo in fotos) arquivosRepositorio.Remover(arquivo);
        albuns.Remover(album!);
        await unitOfWork.SalvarAsync(ct);

        await arquivos.DescartarBlobsAsync(fotos.Select(f => f.Id));
        return Resultado.Ok();
    }

    public async Task<Resultado> DefinirFavoritoAsync(Guid usuarioId, Guid albumId, bool favorito, CancellationToken ct = default)
    {
        var (_, membro, erro) = await ObterComoMembroAsync(albumId, usuarioId, ct);
        if (erro is not null) return erro;

        membro!.DefinirFavorito(favorito);
        await unitOfWork.SalvarAsync(ct);
        return Resultado.Ok();
    }

    // ---- Fotos ----

    public async Task<Resultado<IReadOnlyList<FotoDto>>> AdicionarFotosAsync(
        Guid usuarioId, Guid albumId, IReadOnlyList<ArquivoUpload> uploads, CancellationToken ct = default)
    {
        var (_, _, erro) = await ObterComoMembroAsync(albumId, usuarioId, ct);
        if (erro is not null) return erro;

        if (uploads.Count == 0) return Erro.Validacao("arquivos", "Envie ao menos uma foto.");
        if (uploads.Count > opcoesUpload.Value.MaxArquivosPorRequisicao)
            return Erro.Validacao("arquivos", $"Envie no máximo {opcoesUpload.Value.MaxArquivosPorRequisicao} fotos por vez.");

        var enviados = new List<Arquivo>();
        foreach (var upload in uploads)
        {
            var envio = await arquivos.EnviarAsync(upload, usuarioId, ct);
            if (!envio.Sucesso)
            {
                // Tudo ou nada: descarta o que já subiu nesta requisição.
                await arquivos.DescartarBlobsAsync(enviados.Select(a => a.Id));
                return envio.Erro!;
            }
            enviados.Add(envio.Valor);
        }

        var agora = relogio.GetUtcNow();
        // Um milissegundo de diferença por foto preserva a ordem de envio (capa = primeira foto enviada).
        var adicionadoEm = enviados.Select((_, i) => agora.AddMilliseconds(i)).ToList();
        for (var i = 0; i < enviados.Count; i++)
            albuns.AdicionarFoto(AlbumArquivo.Criar(albumId, enviados[i].Id, usuarioId, adicionadoEm[i]));

        try
        {
            await unitOfWork.SalvarAsync(ct);
        }
        catch
        {
            await arquivos.DescartarBlobsAsync(enviados.Select(a => a.Id));
            throw;
        }

        return enviados
            .Select((a, i) => new FotoDto(a.Id, ArquivoUrls.De(a.Id), usuarioId, adicionadoEm[i]))
            .ToList();
    }

    /// <summary>O dono remove qualquer foto; os demais, só as que eles mesmos enviaram.</summary>
    public async Task<Resultado> RemoverFotoAsync(Guid usuarioId, Guid albumId, Guid arquivoId, CancellationToken ct = default)
    {
        var (_, membro, erro) = await ObterComoMembroAsync(albumId, usuarioId, ct);
        if (erro is not null) return erro;

        var foto = await albuns.ObterFotoAsync(albumId, arquivoId, ct);
        if (foto is null) return Erro.NaoEncontrado("Foto não encontrada neste álbum.");
        if (!membro!.EhDono && foto.AdicionadoPorId != usuarioId)
            return Erro.Proibido("Você só pode remover as fotos que enviou.");

        albuns.RemoverFoto(foto);
        arquivosRepositorio.Remover(foto.Arquivo);
        await unitOfWork.SalvarAsync(ct);

        await arquivos.DescartarBlobsAsync([arquivoId]);
        return Resultado.Ok();
    }

    // ---- Membros e convite ----

    public async Task<Resultado<IReadOnlyList<MembroDto>>> ListarMembrosAsync(Guid usuarioId, Guid albumId, CancellationToken ct = default)
    {
        var (_, _, erro) = await ObterComoMembroAsync(albumId, usuarioId, ct);
        if (erro is not null) return erro;

        return Resultado<IReadOnlyList<MembroDto>>.Ok(await albuns.ListarMembrosAsync(albumId, ct));
    }

    /// <summary>O dono remove qualquer membro; cada membro pode sair removendo a si mesmo. O dono não sai.</summary>
    public async Task<Resultado> RemoverMembroAsync(Guid usuarioId, Guid albumId, Guid alvoId, CancellationToken ct = default)
    {
        var (album, membro, erro) = await ObterComoMembroAsync(albumId, usuarioId, ct);
        if (erro is not null) return erro;

        if (alvoId != usuarioId && !membro!.EhDono) return ApenasDono;

        var alvo = album!.ObterMembro(alvoId);
        if (alvo is null) return Erro.NaoEncontrado("Participante não encontrado neste álbum.");
        if (alvo.EhDono) return Erro.Conflito("O dono não pode sair do álbum. Exclua o álbum para encerrá-lo.");

        album.RemoverMembro(alvoId);
        await unitOfWork.SalvarAsync(ct);
        return Resultado.Ok();
    }

    public async Task<Resultado<ConviteDto>> ObterConviteAsync(Guid usuarioId, Guid albumId, CancellationToken ct = default)
    {
        var (album, _, erro) = await ObterComoMembroAsync(albumId, usuarioId, ct);
        if (erro is not null) return erro;

        return new ConviteDto(ConviteLink.Montar(album!.TokenConvite));
    }

    /// <summary>Gera um novo token: o link (e QR Code) anterior deixa de funcionar.</summary>
    public async Task<Resultado<ConviteDto>> RegenerarConviteAsync(Guid usuarioId, Guid albumId, CancellationToken ct = default)
    {
        var (album, membro, erro) = await ObterComoMembroAsync(albumId, usuarioId, ct);
        if (erro is not null) return erro;
        if (!membro!.EhDono) return ApenasDono;

        var token = album!.RegenerarConvite();
        await unitOfWork.SalvarAsync(ct);
        return new ConviteDto(ConviteLink.Montar(token));
    }

    /// <summary>Fluxo do "escanear QR Code": lê o link e adiciona o usuário como Membro. Idempotente.</summary>
    public async Task<Resultado<AlbumDetalheDto>> EntrarPorLinkAsync(Guid usuarioId, EntrarAlbumRequest request, CancellationToken ct = default)
    {
        var token = ConviteLink.ExtrairToken(request?.Link);
        if (token is null) return Erro.Validacao("link", "Este não é um convite válido do Memento.");

        var album = await albuns.ObterPorTokenConviteAsync(token, ct);
        if (album is null) return Erro.NaoEncontrado("Convite inválido ou expirado.");

        if (album.ObterMembro(usuarioId) is null)
        {
            album.AdicionarMembro(usuarioId, relogio.GetUtcNow());
            await unitOfWork.SalvarAsync(ct);
        }

        return await ObterAsync(usuarioId, album.Id, ct);
    }

    private async Task<(Album? Album, AlbumUsuario? Membro, Erro? Erro)> ObterComoMembroAsync(
        Guid albumId, Guid usuarioId, CancellationToken ct)
    {
        var album = await albuns.ObterComMembrosAsync(albumId, ct);
        var membro = album?.ObterMembro(usuarioId);
        return membro is null ? (null, null, AlbumNaoEncontrado) : (album, membro, null);
    }
}
