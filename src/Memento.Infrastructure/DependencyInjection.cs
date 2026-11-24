using Azure.Storage.Blobs;
using Memento.Application.Abstractions;
using Memento.Infrastructure.Auth;
using Memento.Infrastructure.Persistence;
using Memento.Infrastructure.Persistence.Repositories;
using Memento.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Memento.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        // A connection string é resolvida na criação do contexto (e não aqui), respeitando overrides de configuração tardios.
        services.AddDbContext<AppDbContext>((sp, o) => o.UseNpgsql(
            sp.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("ConnectionStrings:Postgres não configurada.")));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<ISessaoRepository, SessaoRepository>();
        services.AddScoped<IAlbumRepository, AlbumRepository>();
        services.AddScoped<IArquivoRepository, ArquivoRepository>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Secao));
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<ISenhaHasher, SenhaHasher>();

        services.Configure<BlobStorageOptions>(configuration.GetSection(BlobStorageOptions.Secao));
        services.AddSingleton(sp => new BlobServiceClient(sp.GetRequiredService<IOptions<BlobStorageOptions>>().Value.ConnectionString));
        services.AddSingleton<IArquivoStorage, BlobArquivoStorage>();

        return services;
    }
}
