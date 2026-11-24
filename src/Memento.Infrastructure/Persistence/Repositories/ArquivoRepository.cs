using Memento.Application.Abstractions;
using Memento.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Memento.Infrastructure.Persistence.Repositories;

internal sealed class ArquivoRepository(AppDbContext db) : IArquivoRepository
{
    public Task<Arquivo?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
        => db.Arquivos.FirstOrDefaultAsync(a => a.Id == id, ct);

    public void Adicionar(Arquivo arquivo) => db.Arquivos.Add(arquivo);

    public void Remover(Arquivo arquivo) => db.Arquivos.Remove(arquivo);

    public async Task<bool> UsuarioPodeAcessarAsync(Guid arquivoId, Guid usuarioId, CancellationToken ct = default)
    {
        // 1) quem enviou
        if (await db.Arquivos.AnyAsync(a => a.Id == arquivoId && a.EnviadoPorId == usuarioId, ct)) return true;

        // 2) membro de um álbum que contém o arquivo
        var emAlbumDoUsuario = await db.AlbunsArquivos.AnyAsync(
            aa => aa.ArquivoId == arquivoId
                  && db.AlbunsUsuarios.Any(au => au.AlbumId == aa.AlbumId && au.UsuarioId == usuarioId), ct);
        if (emAlbumDoUsuario) return true;

        // 3) foto de perfil de si mesmo ou de alguém com quem divide ao menos um álbum
        return await db.Usuarios.AnyAsync(
            u => u.FotoPerfilArquivoId == arquivoId
                 && (u.Id == usuarioId
                     || db.AlbunsUsuarios.Any(dele => dele.UsuarioId == u.Id
                         && db.AlbunsUsuarios.Any(meu => meu.AlbumId == dele.AlbumId && meu.UsuarioId == usuarioId))), ct);
    }
}
