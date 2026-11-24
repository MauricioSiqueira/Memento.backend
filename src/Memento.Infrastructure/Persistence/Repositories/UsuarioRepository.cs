using Memento.Application.Abstractions;
using Memento.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Memento.Infrastructure.Persistence.Repositories;

internal sealed class UsuarioRepository(AppDbContext db) : IUsuarioRepository
{
    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
        => db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Usuario?> ObterPorEmailAsync(string emailNormalizado, CancellationToken ct = default)
        => db.Usuarios.FirstOrDefaultAsync(u => u.Email == emailNormalizado, ct);

    public Task<bool> EmailEmUsoAsync(string emailNormalizado, CancellationToken ct = default)
        => db.Usuarios.AnyAsync(u => u.Email == emailNormalizado, ct);

    public Task<bool> NomeUsuarioEmUsoAsync(string nomeNormalizado, Guid? ignorarUsuarioId, CancellationToken ct = default)
        => db.Usuarios.AnyAsync(
            u => u.NomeUsuarioNormalizado == nomeNormalizado && (ignorarUsuarioId == null || u.Id != ignorarUsuarioId), ct);

    public void Adicionar(Usuario usuario) => db.Usuarios.Add(usuario);
}
