using Memento.Application.Abstractions;
using Memento.Application.Albuns;
using Memento.Application.Arquivos;
using Memento.Domain.Entities;
using Memento.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Memento.Infrastructure.Persistence.Repositories;

internal sealed class AlbumRepository(AppDbContext db) : IAlbumRepository
{
    public Task<Album?> ObterComMembrosAsync(Guid id, CancellationToken ct = default)
        => db.Albuns.Include(a => a.Membros).FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<Album?> ObterPorTokenConviteAsync(string token, CancellationToken ct = default)
        => db.Albuns.Include(a => a.Membros).FirstOrDefaultAsync(a => a.TokenConvite == token, ct);

    public void Adicionar(Album album) => db.Albuns.Add(album);

    public void Remover(Album album) => db.Albuns.Remove(album);

    public Task<AlbumArquivo?> ObterFotoAsync(Guid albumId, Guid arquivoId, CancellationToken ct = default)
        => db.AlbunsArquivos.Include(f => f.Arquivo)
            .FirstOrDefaultAsync(f => f.AlbumId == albumId && f.ArquivoId == arquivoId, ct);

    public void AdicionarFoto(AlbumArquivo foto) => db.AlbunsArquivos.Add(foto);

    public void RemoverFoto(AlbumArquivo foto) => db.AlbunsArquivos.Remove(foto);

    public async Task<IReadOnlyList<Arquivo>> ListarArquivosDoAlbumAsync(Guid albumId, CancellationToken ct = default)
        => await db.AlbunsArquivos.Where(f => f.AlbumId == albumId).Select(f => f.Arquivo).ToListAsync(ct);

    public async Task<IReadOnlyList<AlbumResumoDto>> ListarResumosAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var linhas = await db.AlbunsUsuarios
            .Where(au => au.UsuarioId == usuarioId)
            .Select(au => new
            {
                au.AlbumId,
                au.Favorito,
                au.Papel,
                Nome = db.Albuns.Where(a => a.Id == au.AlbumId).Select(a => a.Nome).First(),
                CriadoEm = db.Albuns.Where(a => a.Id == au.AlbumId).Select(a => a.CriadoEm).First(),
                TotalFotos = db.AlbunsArquivos.Count(f => f.AlbumId == au.AlbumId),
                TotalMembros = db.AlbunsUsuarios.Count(m => m.AlbumId == au.AlbumId),
                Capa = db.AlbunsArquivos.Where(f => f.AlbumId == au.AlbumId)
                    .OrderBy(f => f.AdicionadoEm).ThenBy(f => f.ArquivoId)
                    .Select(f => (Guid?)f.ArquivoId).FirstOrDefault()
            })
            .OrderByDescending(x => x.CriadoEm)
            .ToListAsync(ct);

        return linhas
            .Select(x => new AlbumResumoDto(
                x.AlbumId, x.Nome, x.Favorito, x.Papel == PapelAlbum.Dono,
                x.TotalFotos, x.TotalMembros, ArquivoUrls.De(x.Capa), x.CriadoEm))
            .ToList();
    }

    public async Task<AlbumDetalheDto?> ObterDetalheAsync(Guid albumId, Guid usuarioId, CancellationToken ct = default)
    {
        var cabecalho = await db.AlbunsUsuarios
            .Where(au => au.AlbumId == albumId && au.UsuarioId == usuarioId)
            .Select(au => new
            {
                au.Favorito,
                au.Papel,
                Nome = db.Albuns.Where(a => a.Id == albumId).Select(a => a.Nome).First(),
                CriadoEm = db.Albuns.Where(a => a.Id == albumId).Select(a => a.CriadoEm).First(),
                TotalMembros = db.AlbunsUsuarios.Count(m => m.AlbumId == albumId)
            })
            .FirstOrDefaultAsync(ct);
        if (cabecalho is null) return null;

        var fotos = await db.AlbunsArquivos
            .Where(f => f.AlbumId == albumId)
            .OrderBy(f => f.AdicionadoEm).ThenBy(f => f.ArquivoId)
            .Select(f => new { f.ArquivoId, f.AdicionadoPorId, f.AdicionadoEm })
            .ToListAsync(ct);

        var fotosDto = fotos
            .Select(f => new FotoDto(f.ArquivoId, ArquivoUrls.De(f.ArquivoId), f.AdicionadoPorId, f.AdicionadoEm))
            .ToList();

        return new AlbumDetalheDto(
            albumId, cabecalho.Nome, cabecalho.Favorito, cabecalho.Papel == PapelAlbum.Dono, cabecalho.TotalMembros,
            fotosDto.FirstOrDefault()?.Url, cabecalho.CriadoEm, fotosDto);
    }

    public async Task<IReadOnlyList<MembroDto>> ListarMembrosAsync(Guid albumId, CancellationToken ct = default)
    {
        var linhas = await db.AlbunsUsuarios
            .Where(au => au.AlbumId == albumId)
            .Join(db.Usuarios, au => au.UsuarioId, u => u.Id,
                (au, u) => new { u.Id, u.NomeUsuario, u.FotoPerfilArquivoId, au.Papel, au.EntrouEm })
            .OrderBy(x => x.Papel).ThenBy(x => x.EntrouEm)
            .ToListAsync(ct);

        return linhas
            .Select(x => new MembroDto(x.Id, x.NomeUsuario, ArquivoUrls.De(x.FotoPerfilArquivoId), x.Papel.ToString(), x.EntrouEm))
            .ToList();
    }
}
