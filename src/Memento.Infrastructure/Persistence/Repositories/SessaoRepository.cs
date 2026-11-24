using Memento.Application.Abstractions;
using Memento.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Memento.Infrastructure.Persistence.Repositories;

internal sealed class SessaoRepository(AppDbContext db) : ISessaoRepository
{
    public Task<Sessao?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
        => db.Sessoes.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<Sessao?> ObterPorRefreshHashAsync(string hash, CancellationToken ct = default)
        => db.Sessoes.FirstOrDefaultAsync(
            s => s.RefreshTokenHash == hash || s.RefreshTokenAnteriorHash == hash, ct);

    public void Adicionar(Sessao sessao) => db.Sessoes.Add(sessao);
}
