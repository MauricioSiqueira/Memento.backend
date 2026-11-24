using System.Collections.Concurrent;
using Memento.Application.Abstractions;
using Memento.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Memento.Tests.Integration;

/// <summary>Storage em memória: os testes não dependem do Azurite.</summary>
public sealed class StorageEmMemoria : IArquivoStorage
{
    public ConcurrentDictionary<Guid, byte[]> Blobs { get; } = new();

    public async Task EnviarAsync(Guid arquivoId, Stream conteudo, string contentType, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await conteudo.CopyToAsync(ms, ct);
        Blobs[arquivoId] = ms.ToArray();
    }

    public Task<Stream?> AbrirLeituraAsync(Guid arquivoId, CancellationToken ct = default)
        => Task.FromResult<Stream?>(Blobs.TryGetValue(arquivoId, out var b) ? new MemoryStream(b) : null);

    public Task ExcluirAsync(Guid arquivoId, CancellationToken ct = default)
    {
        Blobs.TryRemove(arquivoId, out _);
        return Task.CompletedTask;
    }
}

/// <summary>
/// API real (controllers, JWT, rate limit, EF) contra o PostgreSQL local, num banco próprio
/// (<c>memento_tests</c>) recriado a cada execução, e com o storage em memória.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string ConnectionString = "Host=localhost;Port=5432;Database=memento_tests;Username=mauriciobernardo";

    public StorageEmMemoria Storage { get; } = new();

    public ApiFactory()
    {
        using var escopo = Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureDeleted();
        db.Database.Migrate();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = ConnectionString,
            ["RateLimit:Auth:PermitLimit"] = "1000",
            ["RateLimit:Upload:PermitLimit"] = "1000",
            ["RateLimit:Api:PermitLimit"] = "100000"
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IArquivoStorage>();
            services.AddSingleton<IArquivoStorage>(Storage);
        });
    }
}

[CollectionDefinition(Nome)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Nome = "api";
}
