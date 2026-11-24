using Memento.Application.Abstractions;
using Memento.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Memento.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Sessao> Sessoes => Set<Sessao>();
    public DbSet<Album> Albuns => Set<Album>();
    public DbSet<AlbumUsuario> AlbunsUsuarios => Set<AlbumUsuario>();
    public DbSet<Arquivo> Arquivos => Set<Arquivo>();
    public DbSet<AlbumArquivo> AlbunsArquivos => Set<AlbumArquivo>();

    public Task<int> SalvarAsync(CancellationToken ct = default) => SaveChangesAsync(ct);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
